using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class SalaryRangeTests
{
  [Fact]
  public void Ctor_MinMaxAndLowercaseCurrency_SetsValuesAndUppercasesCurrency()
  {
    var range = new SalaryRange(100000, 200000, "eur");

    Assert.Equal(100000, range.Min);
    Assert.Equal(200000, range.Max);
    Assert.Equal("EUR", range.Currency);
  }

  [Fact]
  public void Ctor_MinOnly_Succeeds()
  {
    var range = new SalaryRange(100000, null, "USD");

    Assert.Equal(100000, range.Min);
    Assert.Null(range.Max);
  }

  [Fact]
  public void Ctor_MaxOnly_Succeeds()
  {
    var range = new SalaryRange(null, 200000, "USD");

    Assert.Null(range.Min);
    Assert.Equal(200000, range.Max);
  }

  [Fact]
  public void Ctor_BothNull_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new SalaryRange(null, null, "USD"));
  }

  [Fact]
  public void Ctor_MinGreaterThanMax_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new SalaryRange(200, 100, "USD"));
  }

  [Fact]
  public void Ctor_NegativeMin_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new SalaryRange(-1, 100, "USD"));
  }

  [Fact]
  public void Ctor_NegativeMax_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new SalaryRange(null, -1, "USD"));
  }

  [Theory]
  [InlineData("")]
  [InlineData("eu")]
  [InlineData("euro")]
  [InlineData("e1r")]
  public void Ctor_InvalidCurrency_Throws(string currency)
  {
    Assert.ThrowsAny<ArgumentException>(() => new SalaryRange(1, 2, currency));
  }

  [Fact]
  public void Equals_SameValues_AreEqual()
  {
    Assert.Equal(new SalaryRange(100, 200, "EUR"), new SalaryRange(100, 200, "eur"));
  }
}
