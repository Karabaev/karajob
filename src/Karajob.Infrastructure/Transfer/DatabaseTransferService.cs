using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Karajob.Application.Files;
using Karajob.Application.Transfer;
using Karajob.Domain.Cvs;
using Karajob.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Karajob.Infrastructure.Transfer;

public class DatabaseTransferService : IDatabaseTransfer
{
  public const string DataEntryName = "database.json";
  public const string FilesPrefix = "files/";
  public const int FormatVersion = 1;

  private const string FormatVersionField = "formatVersion";
  private const string ExportedAtField = "exportedAtUtc";
  private const string SchemaField = "schemaMigration";
  private const string TablesField = "tables";

  private static readonly MethodInfo ReadAllMethod = typeof(DatabaseTransferService)
    .GetMethod(nameof(ReadAllTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
  private static readonly MethodInfo DeleteAllMethod = typeof(DatabaseTransferService)
    .GetMethod(nameof(DeleteAllTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

  private readonly IDbContextFactory<KarajobDbContext> contextFactory;
  private readonly IFileStorage fileStorage;
  private readonly DatabaseOptions databaseOptions;
  private readonly TimeProvider timeProvider;

  public async Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
  {
    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    var entityTypes = SortByDependency(context.Model.GetEntityTypes());
    var schema = await GetSchemaAsync(context, cancellationToken);
    var storagePaths = new List<string>();

    using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
    var dataEntry = archive.CreateEntry(DataEntryName, CompressionLevel.Optimal);
    await using (var dataStream = dataEntry.Open())
    await using (var writer = new Utf8JsonWriter(dataStream, new JsonWriterOptions { Indented = true }))
    {
      writer.WriteStartObject();
      writer.WriteNumber(FormatVersionField, FormatVersion);
      writer.WriteString(ExportedAtField, timeProvider.GetUtcNow().UtcDateTime);
      writer.WriteString(SchemaField, schema);
      writer.WriteStartObject(TablesField);
      foreach (var entityType in entityTypes)
      {
        writer.WriteStartArray(TableName(entityType));
        foreach (var entity in await ReadAllAsync(context, entityType, cancellationToken))
        {
          ToJson(context, entityType, entity).WriteTo(writer);
          if (entity is CvVersion version)
          {
            storagePaths.Add(version.StoragePath);
          }
        }

        writer.WriteEndArray();
        context.ChangeTracker.Clear();
      }

      writer.WriteEndObject();
      writer.WriteEndObject();
    }

    foreach (var path in storagePaths)
    {
      await CopyFileAsync(archive, path, cancellationToken);
    }
  }

  public async Task<ImportSummary> ImportAsync(Stream source, CancellationToken cancellationToken = default)
  {
    await using var spool = new FileStream(
      Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
    await source.CopyToAsync(spool, cancellationToken);
    spool.Position = 0;

    using var archive = OpenArchive(spool);
    var root = ReadData(archive);

    await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
    var schema = await GetSchemaAsync(context, cancellationToken);
    if ((int?)root[FormatVersionField] != FormatVersion)
    {
      throw Failure(DatabaseTransferFailure.UnsupportedFormatVersion, $"Unsupported export format version '{root[FormatVersionField]}'.");
    }

    if ((string?)root[SchemaField] != schema)
    {
      throw Failure(DatabaseTransferFailure.SchemaMismatch, $"The export was made on schema '{root[SchemaField]}', this database is on '{schema}'.");
    }

    var entityTypes = SortByDependency(context.Model.GetEntityTypes());
    var summary = new Dictionary<string, int>();
    var importedPaths = new List<string>();
    foreach (var entityType in entityTypes)
    {
      var rows = root[TablesField]?[TableName(entityType)]?.AsArray()
        ?? throw Failure(DatabaseTransferFailure.InvalidData, $"Table '{TableName(entityType)}' is missing.");
      summary[TableName(entityType)] = rows.Count;
      foreach (var row in rows)
      {
        var entity = FromJson(context, entityType, row);
        if (entity is CvVersion version)
        {
          importedPaths.Add(version.StoragePath);
        }
      }
    }

    foreach (var path in importedPaths)
    {
      if (archive.GetEntry(FilesPrefix + path) is null)
      {
        throw Failure(DatabaseTransferFailure.CvFileMissing, $"CV file '{path}' is missing from the archive.");
      }
    }

    var oldPaths = await context.CvVersions.IgnoreQueryFilters().AsNoTracking()
      .Select(v => v.StoragePath).ToListAsync(cancellationToken);
    BackupBeforeImport();

    await using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken))
    {
      foreach (var entityType in Enumerable.Reverse(entityTypes))
      {
        await DeleteAllAsync(context, entityType, cancellationToken);
      }

      await context.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
    }

    foreach (var path in oldPaths.Concat(importedPaths).Distinct())
    {
      fileStorage.Delete(path);
    }

    foreach (var path in importedPaths)
    {
      await using var content = archive.GetEntry(FilesPrefix + path)!.Open();
      await fileStorage.SaveAsync(path, content, cancellationToken);
    }

    return new ImportSummary(summary, importedPaths.Count);
  }

  private void BackupBeforeImport()
  {
    if (databaseOptions.Provider != DatabaseOptions.SqliteProvider) return;

    var databasePath = Path.GetFullPath(new SqliteConnectionStringBuilder(databaseOptions.ConnectionString).DataSource);
    var directory = Path.Combine(Path.GetDirectoryName(databasePath)!, DatabaseInitializer.BackupsFolderName);
    Directory.CreateDirectory(directory);
    var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
    SqliteBackup.Copy(databaseOptions.ConnectionString!, Path.Combine(directory, $"before-import-{stamp}.db"));
  }

  private async Task CopyFileAsync(ZipArchive archive, string path, CancellationToken cancellationToken)
  {
    Stream content;
    try
    {
      content = fileStorage.OpenRead(path);
    }
    catch (FileNotFoundException exception)
    {
      throw new DatabaseTransferException(DatabaseTransferFailure.CvFileMissing, $"CV file '{path}' is missing from the storage.", exception);
    }

    await using (content)
    {
      var entry = archive.CreateEntry(FilesPrefix + path, CompressionLevel.Optimal);
      await using var entryStream = entry.Open();
      await content.CopyToAsync(entryStream, cancellationToken);
    }
  }

  private static ZipArchive OpenArchive(Stream stream)
  {
    try
    {
      return new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
    }
    catch (InvalidDataException exception)
    {
      throw new DatabaseTransferException(DatabaseTransferFailure.NotAnArchive, "The file is not a valid export archive.", exception);
    }
  }

  private static JsonNode ReadData(ZipArchive archive)
  {
    var entry = archive.GetEntry(DataEntryName)
      ?? throw Failure(DatabaseTransferFailure.DataFileMissing, $"'{DataEntryName}' is missing from the archive.");
    try
    {
      using var stream = entry.Open();
      return JsonNode.Parse(stream) ?? throw Failure(DatabaseTransferFailure.InvalidData, $"'{DataEntryName}' is empty.");
    }
    catch (Exception exception) when (exception is JsonException or InvalidDataException)
    {
      throw new DatabaseTransferException(DatabaseTransferFailure.InvalidData, $"'{DataEntryName}' is not valid JSON.", exception);
    }
  }

  private static async Task<string> GetSchemaAsync(KarajobDbContext context, CancellationToken cancellationToken)
  {
    return (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault()
      ?? throw new InvalidOperationException("The database has no applied migrations.");
  }

  private static string TableName(IEntityType entityType)
  {
    return entityType.ClrType.Name;
  }

  private static List<IEntityType> SortByDependency(IEnumerable<IEntityType> entityTypes)
  {
    var remaining = entityTypes.OrderBy(TableName, StringComparer.Ordinal).ToList();
    var sorted = new List<IEntityType>();
    while (remaining.Count > 0)
    {
      var ready = remaining
        .Where(e => e.GetForeignKeys().All(fk => fk.PrincipalEntityType == e || sorted.Contains(fk.PrincipalEntityType)))
        .ToList();
      if (ready.Count == 0)
      {
        throw new InvalidOperationException($"Foreign key cycle between: {string.Join(", ", remaining.Select(TableName))}.");
      }

      sorted.AddRange(ready);
      remaining.RemoveAll(ready.Contains);
    }

    return sorted;
  }

  private static JsonObject ToJson(KarajobDbContext context, IEntityType entityType, object entity)
  {
    var entry = context.Entry(entity);
    var row = new JsonObject();
    foreach (var property in entityType.GetProperties())
    {
      row[property.Name] = JsonSerializer.SerializeToNode(entry.Property(property.Name).CurrentValue, property.ClrType);
    }

    foreach (var complex in entityType.GetComplexProperties())
    {
      row[complex.Name] = JsonSerializer.SerializeToNode(entry.ComplexProperty(complex.Name).CurrentValue, complex.ClrType);
    }

    return row;
  }

  private static object FromJson(KarajobDbContext context, IEntityType entityType, JsonNode? row)
  {
    var entity = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(entityType.ClrType);
    var entry = context.Entry(entity);
    try
    {
      var json = row?.AsObject() ?? throw Failure(DatabaseTransferFailure.InvalidData, $"A row of '{TableName(entityType)}' is not an object.");
      foreach (var property in entityType.GetProperties())
      {
        if (!json.TryGetPropertyValue(property.Name, out var value))
        {
          throw Failure(DatabaseTransferFailure.InvalidData, $"'{TableName(entityType)}.{property.Name}' is missing.");
        }

        entry.Property(property.Name).CurrentValue = value.Deserialize(property.ClrType);
      }

      foreach (var complex in entityType.GetComplexProperties())
      {
        json.TryGetPropertyValue(complex.Name, out var value);
        entry.ComplexProperty(complex.Name).CurrentValue = value.Deserialize(complex.ClrType);
      }
    }
    catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or ArgumentException)
    {
      throw new DatabaseTransferException(DatabaseTransferFailure.InvalidData, $"Invalid row in '{TableName(entityType)}': {exception.Message}", exception);
    }

    entry.State = EntityState.Added;
    return entity;
  }

  private static DatabaseTransferException Failure(DatabaseTransferFailure failure, string message)
  {
    return new DatabaseTransferException(failure, message);
  }

  private static async Task<List<object>> ReadAllAsync(KarajobDbContext context, IEntityType entityType, CancellationToken cancellationToken)
  {
    var task = (Task<List<object>>)ReadAllMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [context, cancellationToken])!;
    var rows = await task;
    var key = entityType.FindPrimaryKey()!.Properties.Single().Name;
    return rows.OrderBy(r => context.Entry(r).Property(key).CurrentValue!.ToString(), StringComparer.Ordinal).ToList();
  }

  private static Task DeleteAllAsync(KarajobDbContext context, IEntityType entityType, CancellationToken cancellationToken)
  {
    return (Task)DeleteAllMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [context, cancellationToken])!;
  }

  private static async Task<List<object>> ReadAllTypedAsync<T>(KarajobDbContext context, CancellationToken cancellationToken)
    where T : class
  {
    var rows = await context.Set<T>().IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().ToListAsync(cancellationToken);
    return rows.Cast<object>().ToList();
  }

  private static Task DeleteAllTypedAsync<T>(KarajobDbContext context, CancellationToken cancellationToken)
    where T : class
  {
    return context.Set<T>().IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
  }

  public DatabaseTransferService(
    IDbContextFactory<KarajobDbContext> contextFactory,
    IFileStorage fileStorage,
    DatabaseOptions databaseOptions,
    TimeProvider timeProvider)
  {
    this.contextFactory = contextFactory;
    this.fileStorage = fileStorage;
    this.databaseOptions = databaseOptions;
    this.timeProvider = timeProvider;
  }
}
