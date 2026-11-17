using Karajob.Application.Applications;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Application.Cvs;
using Karajob.Application.Goals;
using Karajob.Application.Tracks;
using Karajob.Application.MasterProfiles;
using Karajob.Application.Metrics;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Application.WeeklyReviews;
using Karajob.Infrastructure.Files;
using Karajob.Infrastructure.Persistence;
using Karajob.Infrastructure.Vacancies;
using Karajob.Web.Components;
using Karajob.Web.Components.Common;
using Karajob.Web.Cvs;
using Karajob.Web.Persistence;
using Karajob.Web.Users;
using Karajob.Web.Vacancies;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents()
  // Each keystroke in the master profile editor sends the whole text; the 32 KB default drops the circuit
  // at ~16k Cyrillic characters, while the profile has no length limit.
  .AddHubOptions(options => options.MaximumReceiveMessageSize = 2 * 1024 * 1024);
builder.Services.AddMudServices();
builder.Services.AddLocalization();

var defaultCulture = builder.Configuration["Localization:DefaultCulture"]
  ?? throw new InvalidOperationException("Configuration value 'Localization:DefaultCulture' is missing.");
var supportedCultures = new[] { "en", "ru" };
if (!supportedCultures.Contains(defaultCulture))
{
  throw new InvalidOperationException($"Localization:DefaultCulture must be one of: {string.Join(", ", supportedCultures)}.");
}

// In Docker the default key ring lives inside the container and is lost on recreation.
var keysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(keysDirectory))
{
  builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
}

var databaseOptions = builder.Configuration.GetRequiredSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
  ?? throw new InvalidOperationException($"Configuration section '{DatabaseOptions.SectionName}' is empty.");
builder.Services.AddKarajobPersistence(databaseOptions);
var backupOptions = builder.Configuration.GetRequiredSection(BackupOptions.SectionName).Get<BackupOptions>()
  ?? throw new InvalidOperationException($"Configuration section '{BackupOptions.SectionName}' is empty.");
builder.Services.AddKarajobDailyBackup(databaseOptions, backupOptions);
if (databaseOptions.Provider == DatabaseOptions.SqliteProvider)
{
  builder.Services.AddHostedService<DailyBackupHostedService>();
}
builder.Services.AddProfileAuthentication();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SearchGoalService>();
builder.Services.AddScoped<MasterProfileService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<TrackService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<VacancyService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddHttpClient<IPageTitleReader, HttpPageTitleReader>(client =>
{
  client.Timeout = TimeSpan.FromSeconds(5);
  client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Karajob/1.0)");
  client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
});
builder.Services.AddScoped<TrackMetricsService>();
builder.Services.AddScoped<TrackDecisionService>();
builder.Services.AddScoped<WeeklyReviewService>();
builder.Services.AddScoped<UserDates>();
builder.Services.AddScoped<InboxNotifier>();

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
var localizationOptions = new RequestLocalizationOptions()
  .SetDefaultCulture(defaultCulture)
  .AddSupportedCultures(supportedCultures)
  .AddSupportedUICultures(supportedCultures);
// The browser Accept-Language must not switch the language: it is a deployment setting.
localizationOptions.RequestCultureProviders.Clear();
app.UseRequestLocalization(localizationOptions);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();
app.MapCvEndpoints();
app.MapProfileEndpoints();

await app.RunAsync();
