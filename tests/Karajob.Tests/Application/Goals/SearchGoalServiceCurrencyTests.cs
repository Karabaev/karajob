using Karajob.Application.Common;
using Karajob.Application.Goals;

namespace Karajob.Tests.Application.Goals;

public class SearchGoalServiceCurrencyTests : SearchGoalServiceFixture
{
  [Theory]
  [InlineData("GBP")]
  [InlineData("gbp")]
  [InlineData("JPY")]
  public async Task UpdateAsync_CurrencyNotInCatalog_FailsOnIncomeCurrencyWithMessage(string currency)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(Input(desiredMonthlyIncome: 5000, incomeCurrency: currency)));

    Assert.Equal("Валюта не найдена", exception.Errors[nameof(SearchGoalInput.IncomeCurrency)]);
    Assert.Null((await Service.GetAsync()).IncomeCurrency);
  }

  [Theory]
  [InlineData("RUB")]
  [InlineData("USD")]
  [InlineData("EUR")]
  public async Task UpdateAsync_CatalogCurrency_IsStored(string currency)
  {
    await Service.UpdateAsync(Input(desiredMonthlyIncome: 5000, incomeCurrency: currency));

    var details = await Service.GetAsync();
    Assert.Equal(5000, details.DesiredMonthlyIncome);
    Assert.Equal(currency, details.IncomeCurrency);
  }

  [Fact]
  public async Task UpdateAsync_LowerCaseCatalogCurrency_IsAcceptedAndStoredUpperCase()
  {
    var returned = await Service.UpdateAsync(Input(desiredMonthlyIncome: 5000, incomeCurrency: "usd"));

    Assert.Equal("USD", returned.IncomeCurrency);
    Assert.Equal("USD", (await Service.GetAsync()).IncomeCurrency);
  }

  [Fact]
  public async Task UpdateAsync_CurrencyWithSurroundingSpaces_IsAcceptedAndStoredUpperCase()
  {
    await Service.UpdateAsync(Input(desiredMonthlyIncome: 5000, incomeCurrency: " eur "));

    Assert.Equal("EUR", (await Service.GetAsync()).IncomeCurrency);
  }

  [Fact]
  public async Task UpdateAsync_NoIncomeAndNoCurrency_DoesNotRequireCatalogLookup()
  {
    await Service.UpdateAsync(Input());

    Assert.Null((await Service.GetAsync()).IncomeCurrency);
  }
}
