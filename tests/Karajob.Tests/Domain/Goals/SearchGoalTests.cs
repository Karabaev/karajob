using Karajob.Domain.Goals;

namespace Karajob.Tests.Domain.Goals;

public class SearchGoalTests
{
  private static SearchGoal Create() => new(Guid.NewGuid());

  [Fact]
  public void Ctor_Valid_SetsUserIdAndGeneratesId()
  {
    var userId = Guid.NewGuid();

    var goal = new SearchGoal(userId);

    Assert.NotEqual(Guid.Empty, goal.Id);
    Assert.Equal(userId, goal.UserId);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new SearchGoal(Guid.Empty));
  }

  [Fact]
  public void Ctor_Valid_UsesDefaultThresholdsFromSearchGoalDefaults()
  {
    var goal = Create();

    Assert.Equal(SearchGoalDefaults.WeeklyApplicationTarget, goal.WeeklyApplicationTarget);
    Assert.Equal(SearchGoalDefaults.MarketThresholdPerWeek, goal.MarketThresholdPerWeek);
    Assert.Equal(SearchGoalDefaults.ConversionThresholdPermille, goal.ConversionThresholdPermille);
    Assert.Equal(SearchGoalDefaults.MinSampleSize, goal.MinSampleSize);
    Assert.Equal(SearchGoalDefaults.MaturationDays, goal.MaturationDays);
  }

  [Fact]
  public void Ctor_Valid_UsesSpecifiedDefaultLiterals()
  {
    var goal = Create();

    Assert.Equal(10, goal.WeeklyApplicationTarget);
    Assert.Equal(10, goal.MarketThresholdPerWeek);
    Assert.Equal(30, goal.ConversionThresholdPermille);
    Assert.Equal(30, goal.MinSampleSize);
    Assert.Equal(14, goal.MaturationDays);
  }

  [Fact]
  public void Ctor_Valid_LeavesOptionalFieldsEmpty()
  {
    var goal = Create();

    Assert.Null(goal.RoleId);
    Assert.Empty(goal.Locations);
    Assert.Null(goal.Industries);
    Assert.Null(goal.MustHave);
    Assert.Null(goal.Deadline);
    Assert.Null(goal.DesiredMonthlyIncome);
    Assert.Null(goal.IncomeCurrency);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(int.MinValue)]
  public void WeeklyApplicationTarget_Negative_Throws(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.WeeklyApplicationTarget = value);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(25)]
  public void WeeklyApplicationTarget_NonNegative_IsStored(int value)
  {
    var goal = Create();

    goal.WeeklyApplicationTarget = value;

    Assert.Equal(value, goal.WeeklyApplicationTarget);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(int.MinValue)]
  public void MarketThresholdPerWeek_Negative_Throws(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.MarketThresholdPerWeek = value);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(40)]
  public void MarketThresholdPerWeek_NonNegative_IsStored(int value)
  {
    var goal = Create();

    goal.MarketThresholdPerWeek = value;

    Assert.Equal(value, goal.MarketThresholdPerWeek);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(1001)]
  public void ConversionThresholdPermille_OutOfRange_Throws(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.ConversionThresholdPermille = value);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(45)]
  [InlineData(1000)]
  public void ConversionThresholdPermille_InRange_IsStored(int value)
  {
    var goal = Create();

    goal.ConversionThresholdPermille = value;

    Assert.Equal(value, goal.ConversionThresholdPermille);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-5)]
  public void MinSampleSize_BelowOne_Throws(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.MinSampleSize = value);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(100)]
  public void MinSampleSize_AtLeastOne_IsStored(int value)
  {
    var goal = Create();

    goal.MinSampleSize = value;

    Assert.Equal(value, goal.MinSampleSize);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(366)]
  public void MaturationDays_OutOfRange_Throws(int value)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.MaturationDays = value);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(21)]
  [InlineData(365)]
  public void MaturationDays_InRange_IsStored(int value)
  {
    var goal = Create();

    goal.MaturationDays = value;

    Assert.Equal(value, goal.MaturationDays);
  }

  [Fact]
  public void Industries_Padded_IsTrimmed()
  {
    var goal = Create();

    goal.Industries = "  Gamedev, Fintech  ";

    Assert.Equal("Gamedev, Fintech", goal.Industries);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void Industries_BlankOrNull_BecomesNull(string? value)
  {
    var goal = Create();
    goal.Industries = "Gamedev";

    goal.Industries = value;

    Assert.Null(goal.Industries);
  }

  [Fact]
  public void MustHave_Padded_IsTrimmed()
  {
    var goal = Create();

    goal.MustHave = "\tRemote only \n";

    Assert.Equal("Remote only", goal.MustHave);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void MustHave_BlankOrNull_BecomesNull(string? value)
  {
    var goal = Create();
    goal.MustHave = "Remote";

    goal.MustHave = value;

    Assert.Null(goal.MustHave);
  }

  [Fact]
  public void RoleId_EmptyGuid_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.RoleId = Guid.Empty);
  }

  [Fact]
  public void RoleId_ValidGuid_IsStored()
  {
    var goal = Create();
    var roleId = Guid.NewGuid();

    goal.RoleId = roleId;

    Assert.Equal(roleId, goal.RoleId);
  }

  [Fact]
  public void RoleId_Null_ClearsRole()
  {
    var goal = Create();
    goal.RoleId = Guid.NewGuid();

    goal.RoleId = null;

    Assert.Null(goal.RoleId);
  }

  [Fact]
  public void SetDesiredIncome_AmountAndCurrency_StoresBoth()
  {
    var goal = Create();

    goal.SetDesiredIncome(5000, "EUR");

    Assert.Equal(5000, goal.DesiredMonthlyIncome);
    Assert.Equal("EUR", goal.IncomeCurrency);
  }

  [Fact]
  public void SetDesiredIncome_LowerCaseCurrency_StoresUpperCase()
  {
    var goal = Create();

    goal.SetDesiredIncome(5000, "usd");

    Assert.Equal("USD", goal.IncomeCurrency);
  }

  [Fact]
  public void SetDesiredIncome_BothNull_ClearsBoth()
  {
    var goal = Create();
    goal.SetDesiredIncome(5000, "EUR");

    goal.SetDesiredIncome(null, null);

    Assert.Null(goal.DesiredMonthlyIncome);
    Assert.Null(goal.IncomeCurrency);
  }

  [Fact]
  public void SetDesiredIncome_AmountWithoutCurrency_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetDesiredIncome(5000, null));
  }

  [Fact]
  public void SetDesiredIncome_CurrencyWithoutAmount_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetDesiredIncome(null, "EUR"));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-100)]
  public void SetDesiredIncome_NonPositiveAmount_Throws(long amount)
  {
    var goal = Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => goal.SetDesiredIncome(amount, "EUR"));
  }

  [Theory]
  [InlineData("")]
  [InlineData("  ")]
  [InlineData("US")]
  [InlineData("USDD")]
  [InlineData("U1D")]
  [InlineData("РУБ")]
  public void SetDesiredIncome_InvalidCurrency_Throws(string currency)
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetDesiredIncome(5000, currency));
  }

  [Fact]
  public void SetDesiredIncome_InvalidAmount_KeepsPreviousIncome()
  {
    var goal = Create();
    goal.SetDesiredIncome(5000, "EUR");

    Assert.ThrowsAny<ArgumentException>(() => goal.SetDesiredIncome(-1, "USD"));

    Assert.Equal(5000, goal.DesiredMonthlyIncome);
    Assert.Equal("EUR", goal.IncomeCurrency);
  }

  [Fact]
  public void SetLocations_NewIds_AddsOneEntryPerIdOwnedByGoal()
  {
    var goal = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    goal.SetLocations([first, second]);

    Assert.Equal([first, second], goal.Locations.Select(l => l.LocationId));
    Assert.All(goal.Locations, l =>
    {
      Assert.NotEqual(Guid.Empty, l.Id);
      Assert.Equal(goal.UserId, l.UserId);
      Assert.Equal(goal.Id, l.SearchGoalId);
    });
  }

  [Fact]
  public void SetLocations_DuplicateIds_KeepsEachOnce()
  {
    var goal = Create();
    var id = Guid.NewGuid();

    goal.SetLocations([id, id, id]);

    Assert.Equal(id, Assert.Single(goal.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsMissingFromNewSet_AreRemoved()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetLocations([kept, Guid.NewGuid()]);

    goal.SetLocations([kept]);

    Assert.Equal(kept, Assert.Single(goal.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsThatStay_KeepSameEntryInstance()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetLocations([kept]);
    var original = Assert.Single(goal.Locations);

    goal.SetLocations([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(goal.Locations, l => l.LocationId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, goal.Locations.Count);
  }

  [Fact]
  public void SetLocations_Empty_ClearsLocations()
  {
    var goal = Create();
    goal.SetLocations([Guid.NewGuid(), Guid.NewGuid()]);

    goal.SetLocations([]);

    Assert.Empty(goal.Locations);
  }

  [Fact]
  public void SetLocations_EmptyGuid_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetLocations([Guid.NewGuid(), Guid.Empty]));
  }

  [Fact]
  public void SetPlatforms_NewIds_AddsOneEntryPerIdOwnedByGoal()
  {
    var goal = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    goal.SetPlatforms([first, second]);

    Assert.Equal([first, second], goal.Platforms.Select(p => p.PlatformId));
    Assert.All(goal.Platforms, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(goal.UserId, p.UserId);
      Assert.Equal(goal.Id, p.SearchGoalId);
    });
  }

  [Fact]
  public void SetPlatforms_DuplicateIds_KeepsEachOnce()
  {
    var goal = Create();
    var id = Guid.NewGuid();

    goal.SetPlatforms([id, id, id]);

    Assert.Equal(id, Assert.Single(goal.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsMissingFromNewSet_AreRemoved()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetPlatforms([kept, Guid.NewGuid()]);

    goal.SetPlatforms([kept]);

    Assert.Equal(kept, Assert.Single(goal.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsThatStay_KeepSameEntryInstance()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetPlatforms([kept]);
    var original = Assert.Single(goal.Platforms);

    goal.SetPlatforms([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(goal.Platforms, p => p.PlatformId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, goal.Platforms.Count);
  }

  [Fact]
  public void SetPlatforms_Empty_ClearsPlatforms()
  {
    var goal = Create();
    goal.SetPlatforms([Guid.NewGuid(), Guid.NewGuid()]);

    goal.SetPlatforms([]);

    Assert.Empty(goal.Platforms);
  }

  [Fact]
  public void SetPlatforms_EmptyGuid_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetPlatforms([Guid.NewGuid(), Guid.Empty]));
  }

  [Fact]
  public void SetEngines_NewIds_AddsOneEntryPerIdOwnedByGoal()
  {
    var goal = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    goal.SetEngines([first, second]);

    Assert.Equal([first, second], goal.Engines.Select(p => p.EngineId));
    Assert.All(goal.Engines, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(goal.UserId, p.UserId);
      Assert.Equal(goal.Id, p.SearchGoalId);
    });
  }

  [Fact]
  public void SetEngines_DuplicateIds_KeepsEachOnce()
  {
    var goal = Create();
    var id = Guid.NewGuid();

    goal.SetEngines([id, id, id]);

    Assert.Equal(id, Assert.Single(goal.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsMissingFromNewSet_AreRemoved()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetEngines([kept, Guid.NewGuid()]);

    goal.SetEngines([kept]);

    Assert.Equal(kept, Assert.Single(goal.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsThatStay_KeepSameEntryInstance()
  {
    var goal = Create();
    var kept = Guid.NewGuid();
    goal.SetEngines([kept]);
    var original = Assert.Single(goal.Engines);

    goal.SetEngines([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(goal.Engines, p => p.EngineId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, goal.Engines.Count);
  }

  [Fact]
  public void SetEngines_Empty_ClearsEngines()
  {
    var goal = Create();
    goal.SetEngines([Guid.NewGuid(), Guid.NewGuid()]);

    goal.SetEngines([]);

    Assert.Empty(goal.Engines);
  }

  [Fact]
  public void SetEngines_EmptyGuid_Throws()
  {
    var goal = Create();

    Assert.ThrowsAny<ArgumentException>(() => goal.SetEngines([Guid.NewGuid(), Guid.Empty]));
  }
}
