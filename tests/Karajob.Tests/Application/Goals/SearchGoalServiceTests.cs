using Karajob.Application.Common;
using Karajob.Application.Goals;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Goals;

public class SearchGoalServiceTests : SearchGoalServiceFixture
{
  [Fact]
  public async Task GetAsync_SeededOwner_ReturnsDefaults()
  {
    var details = await Service.GetAsync();

    Assert.Null(details.RoleId);
    Assert.Empty(details.LocationIds);
    Assert.Null(details.Industries);
    Assert.Null(details.MustHave);
    Assert.Null(details.Deadline);
    Assert.Null(details.DesiredMonthlyIncome);
    Assert.Null(details.IncomeCurrency);
    Assert.Equal(10, details.WeeklyApplicationTarget);
    Assert.Equal(10, details.MarketThresholdPerWeek);
    Assert.Equal(30, details.ConversionThresholdPermille);
    Assert.Equal(30, details.MinSampleSize);
    Assert.Equal(14, details.MaturationDays);
  }

  [Fact]
  public async Task GetAsync_UserWithoutGoal_ThrowsInvalidOperation()
  {
    var user = await PersistenceFixtures.AddUserAsync(Database);

    await Assert.ThrowsAsync<InvalidOperationException>(() => ServiceFor(user.Id).GetAsync());
  }

  [Fact]
  public async Task UpdateAsync_AllFields_PersistsEveryField()
  {
    var deadline = new DateOnly(2027, 3, 1);
    var input = Input(
      roleId: SeedData.UnityDeveloperRoleId,
      locationIds: [SeedData.EuLocationId, SeedData.SerbiaLocationId],
      industries: "Gamedev, Fintech",
      mustHave: "Remote",
      weeklyApplicationTarget: 15,
      deadline: deadline,
      desiredMonthlyIncome: 6000,
      incomeCurrency: "EUR",
      marketThresholdPerWeek: 20,
      conversionThresholdPermille: 45,
      minSampleSize: 50,
      maturationDays: 21);

    await Service.UpdateAsync(input);

    var details = await Service.GetAsync();
    Assert.Equal(SeedData.UnityDeveloperRoleId, details.RoleId);
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.SerbiaLocationId }.Order(), details.LocationIds.Order());
    Assert.Equal("Gamedev, Fintech", details.Industries);
    Assert.Equal("Remote", details.MustHave);
    Assert.Equal(15, details.WeeklyApplicationTarget);
    Assert.Equal(deadline, details.Deadline);
    Assert.Equal(6000, details.DesiredMonthlyIncome);
    Assert.Equal("EUR", details.IncomeCurrency);
    Assert.Equal(20, details.MarketThresholdPerWeek);
    Assert.Equal(45, details.ConversionThresholdPermille);
    Assert.Equal(50, details.MinSampleSize);
    Assert.Equal(21, details.MaturationDays);
  }

  [Fact]
  public async Task UpdateAsync_Valid_ReturnsSameDetailsAsFollowingGet()
  {
    var input = Input(
      roleId: SeedData.UnrealDeveloperRoleId,
      locationIds: [SeedData.UsaLocationId],
      industries: "Games",
      deadline: new DateOnly(2027, 1, 15),
      desiredMonthlyIncome: 4000,
      incomeCurrency: "usd");

    var returned = await Service.UpdateAsync(input);

    var loaded = await Service.GetAsync();
    Assert.Equal(loaded.RoleId, returned.RoleId);
    Assert.Equal(loaded.LocationIds.Order(), returned.LocationIds.Order());
    Assert.Equal(loaded with { LocationIds = [], PlatformIds = [] }, returned with { LocationIds = [], PlatformIds = [] });
  }

  [Fact]
  public async Task UpdateAsync_PaddedTexts_AreTrimmed()
  {
    await Service.UpdateAsync(Input(industries: "  Gamedev  ", mustHave: " Remote only\t"));

    var details = await Service.GetAsync();
    Assert.Equal("Gamedev", details.Industries);
    Assert.Equal("Remote only", details.MustHave);
  }

  [Fact]
  public async Task UpdateAsync_BlankTexts_StoredAsNull()
  {
    await Service.UpdateAsync(Input(industries: "Gamedev", mustHave: "Remote"));

    await Service.UpdateAsync(Input(industries: "   ", mustHave: ""));

    var details = await Service.GetAsync();
    Assert.Null(details.Industries);
    Assert.Null(details.MustHave);
  }

  [Fact]
  public async Task UpdateAsync_LowerCaseCurrency_StoredUpperCase()
  {
    await Service.UpdateAsync(Input(desiredMonthlyIncome: 3000, incomeCurrency: "eur"));

    var details = await Service.GetAsync();
    Assert.Equal("EUR", details.IncomeCurrency);
  }

  [Fact]
  public async Task UpdateAsync_DuplicateLocationIds_StoredOnce()
  {
    await Service.UpdateAsync(Input(locationIds: [SeedData.EuLocationId, SeedData.EuLocationId, SeedData.UsaLocationId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.EuLocationId, SeedData.UsaLocationId }.Order(), details.LocationIds.Order());
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(2, context.SearchGoalLocations.Count());
  }

  [Fact]
  public async Task UpdateAsync_SecondUpdateWithOtherLocations_ReplacesLocations()
  {
    await Service.UpdateAsync(Input(locationIds: [SeedData.EuLocationId, SeedData.UsaLocationId]));

    await Service.UpdateAsync(Input(locationIds: [SeedData.UsaLocationId, SeedData.SerbiaLocationId]));

    var details = await Service.GetAsync();
    Assert.Equal(new[] { SeedData.UsaLocationId, SeedData.SerbiaLocationId }.Order(), details.LocationIds.Order());
  }

  [Fact]
  public async Task UpdateAsync_EmptyLocations_ClearsLocations()
  {
    await Service.UpdateAsync(Input(locationIds: [SeedData.EuLocationId]));

    await Service.UpdateAsync(Input(locationIds: []));

    Assert.Empty((await Service.GetAsync()).LocationIds);
  }

  [Fact]
  public async Task UpdateAsync_ClearingRoleIncomeAndDeadline_StoresNulls()
  {
    await Service.UpdateAsync(Input(
      roleId: SeedData.UnityDeveloperRoleId,
      deadline: new DateOnly(2027, 3, 1),
      desiredMonthlyIncome: 6000,
      incomeCurrency: "EUR"));

    await Service.UpdateAsync(Input());

    var details = await Service.GetAsync();
    Assert.Null(details.RoleId);
    Assert.Null(details.Deadline);
    Assert.Null(details.DesiredMonthlyIncome);
    Assert.Null(details.IncomeCurrency);
  }

  [Fact]
  public async Task UpdateAsync_UnknownRole_FailsOnRoleId()
  {
    await AssertInvalidAsync(Input(roleId: Guid.NewGuid()), nameof(SearchGoalInput.RoleId));
  }

  [Fact]
  public async Task UpdateAsync_UnknownLocation_FailsOnLocationIds()
  {
    await AssertInvalidAsync(
      Input(locationIds: [SeedData.EuLocationId, Guid.NewGuid()]),
      nameof(SearchGoalInput.LocationIds));
  }

  [Fact]
  public async Task UpdateAsync_NegativeWeeklyTarget_FailsOnWeeklyApplicationTarget()
  {
    await AssertInvalidAsync(Input(weeklyApplicationTarget: -1), nameof(SearchGoalInput.WeeklyApplicationTarget));
  }

  [Fact]
  public async Task UpdateAsync_NegativeMarketThreshold_FailsOnMarketThresholdPerWeek()
  {
    await AssertInvalidAsync(Input(marketThresholdPerWeek: -1), nameof(SearchGoalInput.MarketThresholdPerWeek));
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(1001)]
  public async Task UpdateAsync_ConversionOutOfRange_FailsOnConversionThresholdPermille(int permille)
  {
    await AssertInvalidAsync(Input(conversionThresholdPermille: permille), nameof(SearchGoalInput.ConversionThresholdPermille));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-3)]
  public async Task UpdateAsync_MinSampleBelowOne_FailsOnMinSampleSize(int value)
  {
    await AssertInvalidAsync(Input(minSampleSize: value), nameof(SearchGoalInput.MinSampleSize));
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(366)]
  public async Task UpdateAsync_MaturationOutOfRange_FailsOnMaturationDays(int value)
  {
    await AssertInvalidAsync(Input(maturationDays: value), nameof(SearchGoalInput.MaturationDays));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-500)]
  public async Task UpdateAsync_NonPositiveIncome_FailsOnDesiredMonthlyIncome(long amount)
  {
    await AssertInvalidAsync(
      Input(desiredMonthlyIncome: amount, incomeCurrency: "EUR"),
      nameof(SearchGoalInput.DesiredMonthlyIncome));
  }

  [Fact]
  public async Task UpdateAsync_IncomeWithoutCurrency_FailsOnIncomeCurrency()
  {
    await AssertInvalidAsync(Input(desiredMonthlyIncome: 5000), nameof(SearchGoalInput.IncomeCurrency));
  }

  [Theory]
  [InlineData("EU")]
  [InlineData("EURO")]
  [InlineData("E1R")]
  [InlineData("РУБ")]
  public async Task UpdateAsync_InvalidCurrency_FailsOnIncomeCurrency(string currency)
  {
    await AssertInvalidAsync(
      Input(desiredMonthlyIncome: 5000, incomeCurrency: currency),
      nameof(SearchGoalInput.IncomeCurrency));
  }

  [Fact]
  public async Task UpdateAsync_CurrencyWithoutIncome_FailsOnDesiredMonthlyIncome()
  {
    await AssertInvalidAsync(Input(incomeCurrency: "EUR"), nameof(SearchGoalInput.DesiredMonthlyIncome));
  }

  [Fact]
  public async Task UpdateAsync_SeveralInvalidFields_CollectsAllErrorsAndSavesNothing()
  {
    var input = Input(
      roleId: Guid.NewGuid(),
      locationIds: [Guid.NewGuid()],
      industries: "Should not be saved",
      weeklyApplicationTarget: -1,
      marketThresholdPerWeek: -1,
      conversionThresholdPermille: 2000,
      minSampleSize: 0,
      maturationDays: 400);

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(input));

    string[] expected =
    [
      nameof(SearchGoalInput.RoleId),
      nameof(SearchGoalInput.LocationIds),
      nameof(SearchGoalInput.WeeklyApplicationTarget),
      nameof(SearchGoalInput.MarketThresholdPerWeek),
      nameof(SearchGoalInput.ConversionThresholdPermille),
      nameof(SearchGoalInput.MinSampleSize),
      nameof(SearchGoalInput.MaturationDays),
    ];
    Assert.Equal(expected.Order(), exception.Errors.Keys.Order());
    Assert.Null((await Service.GetAsync()).Industries);
  }

  [Fact]
  public async Task UpdateAsync_Invalid_DoesNotChangeStoredGoal()
  {
    await Service.UpdateAsync(Input(industries: "Gamedev", weeklyApplicationTarget: 12));

    await Assert.ThrowsAsync<ValidationFailedException>(() =>
      Service.UpdateAsync(Input(industries: "Other", weeklyApplicationTarget: -5)));

    var details = await Service.GetAsync();
    Assert.Equal("Gamedev", details.Industries);
    Assert.Equal(12, details.WeeklyApplicationTarget);
  }

  [Fact]
  public async Task UpdateAsync_BoundaryValues_AreAccepted()
  {
    await Service.UpdateAsync(Input(
      weeklyApplicationTarget: 0,
      marketThresholdPerWeek: 0,
      conversionThresholdPermille: 1000,
      minSampleSize: 1,
      maturationDays: 365));

    var details = await Service.GetAsync();
    Assert.Equal(0, details.WeeklyApplicationTarget);
    Assert.Equal(0, details.MarketThresholdPerWeek);
    Assert.Equal(1000, details.ConversionThresholdPermille);
    Assert.Equal(1, details.MinSampleSize);
    Assert.Equal(365, details.MaturationDays);
  }

  [Fact]
  public async Task UpdateAsync_OtherProfile_DoesNotChangeFirstProfileGoal()
  {
    var secondId = await CreateProfileAsync();
    var second = ServiceFor(secondId);

    await second.UpdateAsync(Input(
      roleId: SeedData.UnityDeveloperRoleId,
      locationIds: [SeedData.EuLocationId],
      industries: "Second only",
      weeklyApplicationTarget: 99));

    var owner = await Service.GetAsync();
    Assert.Null(owner.RoleId);
    Assert.Empty(owner.LocationIds);
    Assert.Null(owner.Industries);
    Assert.Equal(10, owner.WeeklyApplicationTarget);
    Assert.Equal(99, (await second.GetAsync()).WeeklyApplicationTarget);
  }

  [Fact]
  public async Task UpdateAsync_OwnerProfile_DoesNotChangeSecondProfileGoal()
  {
    var secondId = await CreateProfileAsync();

    await Service.UpdateAsync(Input(industries: "Owner only", weeklyApplicationTarget: 7));

    var second = await ServiceFor(secondId).GetAsync();
    Assert.Null(second.Industries);
    Assert.Equal(10, second.WeeklyApplicationTarget);
  }

  private async Task AssertInvalidAsync(SearchGoalInput input, string key)
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(input));

    Assert.Contains(key, exception.Errors.Keys);
  }
}
