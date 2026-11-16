using Karajob.Application.Catalogs;
using Karajob.Application.Common;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceCurrencyTests : VacancyServiceFixture
{
  [Theory]
  [InlineData("GBP")]
  [InlineData("gbp")]
  public async Task CreateAsync_SalaryCurrencyNotInCatalog_FailsOnSalaryWithMessage(string currency)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(salaryMin: 3000, salaryMax: 5000, salaryCurrency: currency)));

    Assert.Equal("Валюта не найдена", exception.Errors["Salary"]);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task UpdateAsync_SalaryCurrencyNotInCatalog_FailsOnSalaryAndKeepsOldSalary()
  {
    var created = await Service.CreateAsync(Input(salaryMin: 3000, salaryMax: 5000, salaryCurrency: "USD"));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(salaryMin: 3000, salaryMax: 5000, salaryCurrency: "GBP")));

    Assert.Equal("Валюта не найдена", exception.Errors["Salary"]);
    Assert.Equal("USD", (await Service.FindAsync(created.Id))!.SalaryCurrency);
  }

  [Theory]
  [InlineData("RUB", "RUB")]
  [InlineData("eur", "EUR")]
  [InlineData(" usd ", "USD")]
  public async Task CreateAsync_CatalogSalaryCurrency_IsAcceptedAndStoredUpperCase(string input, string expected)
  {
    var created = await Service.CreateAsync(Input(salaryMin: 3000, salaryCurrency: input));

    Assert.Equal(expected, created.SalaryCurrency);
    Assert.Equal(expected, (await Service.FindAsync(created.Id))!.SalaryCurrency);
  }

  [Fact]
  public async Task CreateAsync_CurrencyAddedToCatalog_IsAcceptedForSalary()
  {
    var catalogs = TestDbContextFactory.CreateCatalogService(Database, SeedData.OwnerId);
    await catalogs.CreateAsync(CatalogKind.Currency, new CatalogInput("Фунт стерлингов", "GBP"));

    var created = await Service.CreateAsync(Input(salaryMin: 3000, salaryCurrency: "gbp"));

    Assert.Equal("GBP", created.SalaryCurrency);
  }

  [Fact]
  public async Task CreateAsync_CurrencyWithoutSalaryValues_DoesNotRequireCatalogLookup()
  {
    var created = await Service.CreateAsync(Input(salaryCurrency: "GBP"));

    Assert.Null(created.SalaryCurrency);
  }
}
