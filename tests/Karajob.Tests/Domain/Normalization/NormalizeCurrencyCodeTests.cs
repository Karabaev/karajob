using Karajob.Domain.Normalization;

namespace Karajob.Tests.Domain.Normalization;

public class NormalizeCurrencyCodeTests
{
  [Theory]
  [InlineData("USD", "USD")]
  [InlineData(" usd ", "USD")]
  [InlineData("Eur", "EUR")]
  public void CurrencyCode_ValidInput_ReturnsTrimmedUpperCase(string input, string expected)
  {
    Assert.Equal(expected, Normalize.CurrencyCode(input));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("US")]
  [InlineData("USDT")]
  [InlineData("U5D")]
  [InlineData("РУБ")]
  [InlineData("U D")]
  public void CurrencyCode_InvalidInput_Throws(string input)
  {
    Assert.ThrowsAny<ArgumentException>(() => Normalize.CurrencyCode(input));
  }
}
