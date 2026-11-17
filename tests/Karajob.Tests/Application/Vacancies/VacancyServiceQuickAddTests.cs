using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Tests.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceQuickAddTests : VacancyServiceFixture
{
  [Fact]
  public async Task QuickAddAsync_NewUrl_CreatesInboxVacancyWithManualSource()
  {
    var result = await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/42", null));

    Assert.False(result.AlreadyExisted);
    Assert.Equal(VacancyStage.Inbox, result.Stage);
    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("https://example.com/jobs/42", stored.Url);
    Assert.Equal(VacancyStage.Inbox, stored.Stage);
    Assert.Equal(VacancySource.Manual, stored.Source);
    Assert.Null(stored.CompanyId);
    Assert.Null(stored.RoleId);
    Assert.Equal(Now, stored.DiscoveredAt);
    Assert.Empty(stored.Matches);
  }

  [Fact]
  public async Task QuickAddAsync_UrlWithTrackingAndWww_StoresNormalizedUrl()
  {
    var result = await Service.QuickAddAsync(new QuickAddInput("  http://www.Example.com/jobs/42/?utm_source=x  ", null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("https://example.com/jobs/42", stored.Url);
  }

  [Fact]
  public async Task QuickAddAsync_NoteWithWhitespace_StoresTrimmedNote()
  {
    var result = await Service.QuickAddAsync(new QuickAddInput(DefaultUrl, "  call back  "));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("call back", stored.Notes);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task QuickAddAsync_BlankNote_StoresNullNotes(string? note)
  {
    var result = await Service.QuickAddAsync(new QuickAddInput(DefaultUrl, note));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Null(stored.Notes);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not a url")]
  [InlineData("ftp://example.com/jobs/1")]
  public async Task QuickAddAsync_InvalidUrl_FailsWithUrlErrorAndCreatesNothing(string url)
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.QuickAddAsync(new QuickAddInput(url, null)));

    Assert.Contains("Url", ex.Errors.Keys);
    Assert.Equal(0, await CountVacanciesAsync());
  }

  [Fact]
  public async Task QuickAddAsync_DuplicateUrlAfterNormalization_ReturnsExistingWithoutChanges()
  {
    var first = await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/1", "original"));

    var second = await Service.QuickAddAsync(new QuickAddInput("http://www.EXAMPLE.com/jobs/1/?utm_source=x", "other"));

    Assert.True(second.AlreadyExisted);
    Assert.Equal(first.VacancyId, second.VacancyId);
    Assert.Equal(VacancyStage.Inbox, second.Stage);
    Assert.Equal(1, await CountVacanciesAsync());
    var stored = await LoadAsync(first.VacancyId);
    Assert.Equal("original", stored.Notes);
  }

  [Fact]
  public async Task QuickAddAsync_DuplicateOfTargetedVacancy_ReturnsItsStage()
  {
    var trackId = await CreateTrackAsync();
    var created = await Service.CreateAsync(Input(trackIds: [trackId]));
    await MutateAsync(created.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    var result = await Service.QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    Assert.True(result.AlreadyExisted);
    Assert.Equal(created.Id, result.VacancyId);
    Assert.Equal(VacancyStage.Targeted, result.Stage);
    Assert.Equal(1, await CountVacanciesAsync());
  }

  [Fact]
  public async Task QuickAddAsync_SameUrlOwnedByAnotherUser_CreatesNewVacancy()
  {
    var user = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(user.Id).QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    var result = await Service.QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    Assert.False(result.AlreadyExisted);
  }

  [Fact]
  public async Task CountAsync_CountsOnlyRequestedStage()
  {
    var trackId = await CreateTrackAsync();
    await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/1", null));
    await Service.QuickAddAsync(new QuickAddInput("https://example.com/jobs/2", null));
    var targeted = await Service.CreateAsync(Input("https://example.com/jobs/3", trackIds: [trackId]));
    await MutateAsync(targeted.Id, v => v.MoveTo(VacancyStage.Targeted, Now.AddHours(1)));

    Assert.Equal(2, await Service.CountAsync(VacancyStage.Inbox));
    Assert.Equal(1, await Service.CountAsync(VacancyStage.Targeted));
    Assert.Equal(0, await Service.CountAsync(VacancyStage.Applied));
  }

  [Fact]
  public async Task CountAsync_IgnoresOtherUsersVacancies()
  {
    var user = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(user.Id).QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    Assert.Equal(0, await Service.CountAsync(VacancyStage.Inbox));
  }
}
