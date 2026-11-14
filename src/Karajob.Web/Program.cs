using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Hypotheses;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Files;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Users;
using Karajob.Web.Components;
using Karajob.Web.Cvs;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// In Docker the default key ring lives inside the container and is lost on recreation.
var keysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(keysDirectory))
{
  builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
}

var databaseOptions = builder.Configuration.GetRequiredSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
  ?? throw new InvalidOperationException($"Configuration section '{DatabaseOptions.SectionName}' is empty.");
builder.Services.AddKarajobPersistence(databaseOptions);
// Single seeded user until profile selection lands (Phase 0.5).
builder.Services.AddSingleton<ICurrentUser>(new FixedCurrentUser(SeedData.OwnerId));
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<HypothesisService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<VacancyService>();

var cvOptions = builder.Configuration.GetRequiredSection(CvOptions.SectionName).Get<CvOptions>()
  ?? throw new InvalidOperationException($"Configuration section '{CvOptions.SectionName}' is empty.");
if (cvOptions.MaxFileSizeMb <= 0)
{
  throw new InvalidOperationException($"{CvOptions.SectionName}:{nameof(CvOptions.MaxFileSizeMb)} must be positive.");
}
builder.Services.AddSingleton(cvOptions);
var fileStorageOptions = builder.Configuration.GetRequiredSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
  ?? throw new InvalidOperationException($"Configuration section '{FileStorageOptions.SectionName}' is empty.");
builder.Services.AddKarajobFileStorage(new FileStorageOptions
{
  RootDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, fileStorageOptions.RootDirectory))
});
builder.Services.AddScoped<CvService>();

var app = builder.Build();

// Before Run: Kestrel is not listening yet, so no request ever sees an unmigrated schema.
try
{
  await app.Services.InitializeKarajobDatabaseAsync();
}
catch
{
  // Disposing flushes the console logger, so the critical entry with the backup path reaches the log.
  await app.DisposeAsync();
  throw;
}

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();
app.MapCvEndpoints();

await app.RunAsync();
