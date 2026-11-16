using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Domain.Goals;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Catalogs;

public class CatalogServiceCurrencyTests : CatalogServiceFixture
{
  private static DateTime Moment => PersistenceFixtures.Moment;

  [Fact]
  public async Task ListAsync_Currencies_ReturnsSeededCurrenciesWithCodeSortedByName()
  {
    var items = await Service.ListAsync(CatalogKind.Currency);

    Assert.Equal(
      [
        new CatalogListItem(SeedData.UsdCurrencyId, "Доллар США", "USD"),
        new CatalogListItem(SeedData.EurCurrencyId, "Евро", "EUR"),
        new CatalogListItem(SeedData.RubCurrencyId, "Российский рубль", "RUB"),
      ],
      items);
  }

  [Fact]
  public async Task CreateAsync_Currency_NormalizesCodeAndPersists()
  {
    var created = await Service.CreateAsync(CatalogKind.Currency, new CatalogInput("  Фунт стерлингов ", " gbp "));

    Assert.NotEqual(Guid.Empty, created.Id);
    Assert.Equal("Фунт стерлингов", created.Name);
    Assert.Equal("GBP", created.Code);
    Assert.Contains(await Service.ListAsync(CatalogKind.Currency), i => i.Id == created.Id && i.Code == "GBP");
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  [InlineData("US")]
  [InlineData("USDT")]
  [InlineData("U5D")]
  public async Task CreateAsync_CurrencyWithBlankOrInvalidCode_ThrowsValidationWithCodeKeyAndMessage(string? code)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Currency, new CatalogInput("Фунт стерлингов", code)));

    Assert.Equal("Код — 3 латинские буквы", exception.Errors[nameof(CatalogInput.Code)]);
    Assert.DoesNotContain(nameof(CatalogInput.Name), exception.Errors.Keys);
    Assert.Equal(3, (await Service.ListAsync(CatalogKind.Currency)).Count);
  }

  [Theory]
  [InlineData("USD")]
  [InlineData("usd")]
  public async Task CreateAsync_CurrencyWithDuplicateCode_ThrowsValidationWithCodeKeyAndMessage(string code)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Currency, new CatalogInput("Другой доллар", code)));

    Assert.Equal("Такой код уже есть", exception.Errors[nameof(CatalogInput.Code)]);
    Assert.DoesNotContain(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Theory]
  [InlineData("Евро")]
  [InlineData("  ЕВРО ")]
  public async Task CreateAsync_CurrencyWithDuplicateName_ThrowsValidationWithNameKeyAndMessage(string name)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Currency, new CatalogInput(name, "GBP")));

    Assert.Equal("Такая запись уже есть", exception.Errors[nameof(CatalogInput.Name)]);
    Assert.DoesNotContain(nameof(CatalogInput.Code), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_CurrencyWithBlankName_ThrowsValidationWithNameKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(CatalogKind.Currency, new CatalogInput("  ", "GBP")));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task RenameAsync_Currency_ChangesOnlyNameAndKeepsCode()
  {
    var renamed = await Service.RenameAsync(CatalogKind.Currency, SeedData.RubCurrencyId, "  Рубль ");

    Assert.Equal(SeedData.RubCurrencyId, renamed.Id);
    Assert.Equal("Рубль", renamed.Name);
    Assert.Equal("RUB", renamed.Code);
    Assert.Contains(await Service.ListAsync(CatalogKind.Currency), i => i.Id == renamed.Id && i.Name == "Рубль" && i.Code == "RUB");
  }

  [Fact]
  public async Task RenameAsync_CurrencyToAnotherCurrencysName_ThrowsValidationWithNameKey()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.RenameAsync(CatalogKind.Currency, SeedData.RubCurrencyId, "евро"));

    Assert.Contains(nameof(CatalogInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task RenameAsync_UnknownCurrency_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RenameAsync(CatalogKind.Currency, Guid.NewGuid(), "Новое"));
  }

  [Fact]
  public async Task DeleteAsync_UnusedCurrency_RemovesIt()
  {
    var created = await Service.CreateAsync(CatalogKind.Currency, new CatalogInput("Фунт стерлингов", "GBP"));

    await Service.DeleteAsync(CatalogKind.Currency, created.Id);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Currency), i => i.Id == created.Id);
  }

  [Fact]
  public async Task DeleteAsync_SeededCurrencyNotReferenced_RemovesIt()
  {
    await Service.DeleteAsync(CatalogKind.Currency, SeedData.EurCurrencyId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Currency), i => i.Id == SeedData.EurCurrencyId);
  }

  [Fact]
  public async Task DeleteAsync_UnknownCurrency_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(CatalogKind.Currency, Guid.NewGuid()));
  }

  [Fact]
  public async Task DeleteAsync_CurrencyUsedBySearchGoal_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await SetSearchGoalIncomeAsync(SeedData.OwnerId, "USD");

    await AssertDeleteBlockedAsync(SeedData.UsdCurrencyId);
  }

  [Fact]
  public async Task DeleteAsync_CurrencyUsedBySearchGoalOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddSearchGoalWithIncomeAsync(other.Id, "EUR");

    await AssertDeleteBlockedAsync(SeedData.EurCurrencyId);
  }

  [Fact]
  public async Task DeleteAsync_CurrencyUsedByVacancySalary_ThrowsValidationWithEmptyKeyAndKeepsIt()
  {
    await AddVacancyWithSalaryAsync(SeedData.OwnerId, "EUR");

    await AssertDeleteBlockedAsync(SeedData.EurCurrencyId);
  }

  [Fact]
  public async Task DeleteAsync_CurrencyUsedByVacancySalaryOfAnotherUser_ThrowsValidationWithEmptyKey()
  {
    var other = await PersistenceFixtures.AddUserAsync(Database);
    await AddVacancyWithSalaryAsync(other.Id, "USD");

    await AssertDeleteBlockedAsync(SeedData.UsdCurrencyId);
  }

  [Fact]
  public async Task DeleteAsync_CurrencyDifferentFromTheOnesInUse_RemovesIt()
  {
    await AddVacancyWithSalaryAsync(SeedData.OwnerId, "EUR");
    await SetSearchGoalIncomeAsync(SeedData.OwnerId, "USD");

    await Service.DeleteAsync(CatalogKind.Currency, SeedData.RubCurrencyId);

    Assert.DoesNotContain(await Service.ListAsync(CatalogKind.Currency), i => i.Id == SeedData.RubCurrencyId);
  }

  private async Task AssertDeleteBlockedAsync(Guid id)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(CatalogKind.Currency, id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.Contains(await Service.ListAsync(CatalogKind.Currency), i => i.Id == id);
  }

  private async Task SetSearchGoalIncomeAsync(Guid userId, string currency)
  {
    await using var context = Database.CreateContext(userId);
    var goal = await context.SearchGoals.SingleAsync();
    goal.SetDesiredIncome(5000, currency);
    await context.SaveChangesAsync();
  }

  private async Task AddSearchGoalWithIncomeAsync(Guid userId, string currency)
  {
    var goal = new SearchGoal(userId);
    goal.SetDesiredIncome(5000, currency);
    await using var context = Database.CreateContext(userId);
    context.SearchGoals.Add(goal);
    await context.SaveChangesAsync();
  }

  private async Task AddVacancyWithSalaryAsync(Guid userId, string currency)
  {
    var vacancy = new Vacancy(userId, "https://example.com/jobs/currency", VacancySource.Manual, Moment)
    {
      Salary = new SalaryRange(3000, 5000, currency),
    };
    await using var context = Database.CreateContext(userId);
    context.Vacancies.Add(vacancy);
    await context.SaveChangesAsync();
  }
}
