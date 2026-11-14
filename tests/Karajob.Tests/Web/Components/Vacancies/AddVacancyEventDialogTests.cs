using Bunit;
using Karajob.Domain.Applications;
using Karajob.Web.Components.Vacancies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Vacancies;

public class AddVacancyEventDialogTests : VacancyWebFixture
{
  [Fact]
  public async Task Save_NoteWithoutApplication_AddsNoteToVacancyAndCloses()
  {
    var created = await Vacancies.CreateAsync(Input("https://example.com/jobs/1"));
    var parameters = new DialogParameters<AddVacancyEventDialog>
    {
      { d => d.VacancyId, created.Id },
      { d => d.HasApplication, false },
    };
    var (provider, reference) = await ShowDialogAsync(parameters);
    provider.WaitForElement(".event-note textarea, .event-note input").Input("Позвонил рекрутер");

    provider.Find(".event-save").Click();

    var result = await reference.Result!.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(result!.Canceled);
    var found = await Vacancies.FindAsync(created.Id);
    var item = Assert.Single(found!.Timeline);
    Assert.Equal(ApplicationEventType.Note, item.Type);
    Assert.Equal("Позвонил рекрутер", item.Note);
  }
}
