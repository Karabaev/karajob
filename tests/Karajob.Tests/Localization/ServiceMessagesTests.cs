using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Application.Users;
using Karajob.Application.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Application;
using Karajob.Tests.Application.Tracks;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Localization;

public class ServiceMessagesTests : IAsyncLifetime
{
  private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

  private SqliteTestDatabase _database = null!;
  private CatalogService _catalogs = null!;
  private UserService _users = null!;
  private VacancyService _vacancies = null!;

  public async Task InitializeAsync()
  {
    _database = await SqliteTestDatabase.CreateAsync();
    var clock = new FixedTimeProvider(Now);
    _catalogs = TestDbContextFactory.CreateCatalogService(_database, SeedData.OwnerId);
    _users = TestDbContextFactory.CreateUserService(_database, SeedData.OwnerId, clock);
    _vacancies = TestDbContextFactory.CreateVacancyService(_database, SeedData.OwnerId, clock);
  }

  public async Task DisposeAsync()
  {
    await _database.DisposeAsync();
  }

  [Fact]
  public async Task Catalog_BlankName_EnglishCulture_ReportsEnglishMessage()
  {
    using var _ = TestCulture.Use(TestCulture.English);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => _catalogs.CreateAsync(CatalogKind.Role, new CatalogInput("  ")));

    Assert.Equal("Name is required", exception.Errors[nameof(CatalogInput.Name)]);
  }

  [Fact]
  public async Task Catalog_BlankName_RussianCulture_ReportsRussianMessage()
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => _catalogs.CreateAsync(CatalogKind.Role, new CatalogInput("  ")));

    Assert.Equal("Укажите название", exception.Errors[nameof(CatalogInput.Name)]);
  }

  [Fact]
  public async Task Catalog_DuplicateName_RussianCulture_ReportsRussianMessage()
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => _catalogs.CreateAsync(CatalogKind.Engine, new CatalogInput("Unity")));

    Assert.Equal("Такая запись уже есть", exception.Errors[nameof(CatalogInput.Name)]);
  }

  [Fact]
  public async Task User_BlankNameAndInvalidColor_EnglishCulture_ReportsEnglishMessages()
  {
    using var _ = TestCulture.Use(TestCulture.English);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => _users.CreateAsync(new UserInput(" ", "red")));

    Assert.Equal("Enter a name.", exception.Errors[nameof(UserInput.Name)]);
    Assert.Equal("Choose a color.", exception.Errors[nameof(UserInput.Color)]);
  }

  [Fact]
  public async Task User_BlankNameAndInvalidColor_RussianCulture_ReportsRussianMessages()
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => _users.CreateAsync(new UserInput(" ", "red")));

    Assert.Equal("Введите имя.", exception.Errors[nameof(UserInput.Name)]);
    Assert.Equal("Выберите цвет.", exception.Errors[nameof(UserInput.Color)]);
  }

  [Fact]
  public async Task Vacancy_QuickAddInvalidUrl_EnglishCulture_ReportsEnglishMessage()
  {
    using var _ = TestCulture.Use(TestCulture.English);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => _vacancies.QuickAddAsync(new QuickAddInput("not a url", null)));

    Assert.Equal("Invalid vacancy link", exception.Errors[nameof(QuickAddInput.Url)]);
  }

  [Fact]
  public async Task Vacancy_QuickAddInvalidUrl_RussianCulture_ReportsRussianMessage()
  {
    using var _ = TestCulture.Use(TestCulture.Russian);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => _vacancies.QuickAddAsync(new QuickAddInput("not a url", null)));

    Assert.Equal("Некорректная ссылка на вакансию", exception.Errors[nameof(QuickAddInput.Url)]);
  }
}
