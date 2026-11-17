using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using Karajob.Domain.Tracks;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Karajob.Web.Components.Vacancies;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class MoveToBoardDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Render_ListsOnlyNonDisabledTracks()
  {
    await CreateTrackAsync("Active one");
    var disabled = await CreateTrackAsync("Disabled one");
    await DisableAsync(disabled);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var items = provider.WaitForElements(".move-to-board-track");
    Assert.Single(items);
    Assert.Contains("Active one", items[0].TextContent);
  }

  [Fact]
  public async Task Render_Track_ShowsLanguagePlatformsAndWorkFormat()
  {
    await CreateTrackAsync(
      "Detailed one",
      platformIds: [SeedData.PcPlatformId, SeedData.MobilePlatformId],
      workFormat: WorkFormat.Hybrid,
      languageId: SeedData.EnglishLanguageId);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var item = Assert.Single(provider.WaitForElements(".move-to-board-track"));
    Assert.Contains("Detailed one", item.TextContent);
    Assert.Contains("EN · Mobile, PC · Hybrid", item.TextContent);
  }

  [Fact]
  public async Task Render_VacancyWithRole_ListsOnlyTracksOfThatRole()
  {
    await CreateTrackAsync("Unity one");
    await CreateTrackAsync("Unreal one", SeedData.UnrealDeveloperRoleId);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", roleId: SeedData.UnityDeveloperRoleId));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var items = provider.WaitForElements(".move-to-board-track");
    Assert.Single(items);
    Assert.Contains("Unity one", items[0].TextContent);
  }

  [Fact]
  public async Task Render_VacancyWithoutRole_ListsTracksOfAllRoles()
  {
    await CreateTrackAsync("Unity one");
    await CreateTrackAsync("Unreal one", SeedData.UnrealDeveloperRoleId);
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    provider.WaitForAssertion(() => Assert.Equal(2, provider.FindAll(".move-to-board-track").Count));
  }

  [Fact]
  public async Task Render_NothingSelected_SubmitIsDisabled()
  {
    await CreateTrackAsync();
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));

    var (provider, _) = await ShowAsync(vacancy.Id);

    provider.WaitForElement(".move-to-board-track");
    Assert.True(provider.Find(".move-to-board-submit").HasAttribute("disabled"));
  }

  [Fact]
  public async Task Render_AlreadyMatchedTrack_IsPreChecked()
  {
    var matched = await CreateTrackAsync("Matched");
    await CreateTrackAsync("Other");
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1", trackIds: [matched]));

    var (provider, _) = await ShowAsync(vacancy.Id);

    var items = provider.WaitForElements(".move-to-board-track");
    Assert.True(IsChecked(items.Single(i => i.TextContent.Contains("Matched"))));
    Assert.False(IsChecked(items.Single(i => i.TextContent.Contains("Other"))));
    Assert.False(provider.Find(".move-to-board-submit").HasAttribute("disabled"));
  }

  [Fact]
  public async Task Submit_AfterCheckingTrack_MovesVacancyToTargetedAndCloses()
  {
    var trackId = await CreateTrackAsync("Pick me");
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowAsync(vacancy.Id);
    provider.WaitForElements(".move-to-board-track").Single().QuerySelector("input")!.Change(true);
    provider.WaitForAssertion(() => Assert.False(provider.Find(".move-to-board-submit").HasAttribute("disabled")));

    provider.Find(".move-to-board-submit").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var found = await Vacancies.FindAsync(vacancy.Id);
    Assert.Equal(VacancyStage.Targeted, found!.Stage);
    Assert.Equal([trackId], found.TrackIds);
  }

  [Fact]
  public async Task Submit_VacancyLeftInboxMeanwhile_ShowsErrorAndStaysOpen()
  {
    await CreateTrackAsync("Pick me");
    var vacancy = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var (provider, reference) = await ShowAsync(vacancy.Id);
    provider.WaitForElements(".move-to-board-track").Single().QuerySelector("input")!.Change(true);
    await Vacancies.DismissAsync(vacancy.Id, new Karajob.Application.Vacancies.DismissInput(DismissReason.Other, null));

    provider.Find(".move-to-board-submit").Click();

    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll(".mud-alert")));
    Assert.False(reference.Result!.IsCompleted);
  }

  private static bool IsChecked(IElement item)
  {
    return ((IHtmlInputElement)item.QuerySelector("input")!).IsChecked;
  }

  private async Task DisableAsync(Guid trackId)
  {
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var track = await context.Tracks.SingleAsync(h => h.Id == trackId);
    track.Status = TrackStatus.Disabled;
    await context.SaveChangesAsync();
  }

  private Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(Guid vacancyId)
  {
    return ShowDialogAsync(new DialogParameters<MoveToBoardDialog> { { d => d.VacancyId, vacancyId } });
  }
}
