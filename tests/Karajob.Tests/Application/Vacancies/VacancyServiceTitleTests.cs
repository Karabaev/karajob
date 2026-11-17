using Karajob.Application.Common;
using Karajob.Application.Vacancies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;

namespace Karajob.Tests.Application.Vacancies;

public class VacancyServiceTitleTests : VacancyServiceFixture
{
  private readonly StubPageTitleReader _reader = new();

  private VacancyService TitledService()
  {
    return TestDbContextFactory.CreateVacancyService(Database, SeedData.OwnerId, Clock, _reader);
  }

  [Fact]
  public async Task QuickAddAsync_ReaderReturnsTitle_StoresTitleAndReadsNormalizedUrlOnce()
  {
    _reader.Title = "Senior Unity Dev";

    var result = await TitledService().QuickAddAsync(new QuickAddInput("http://www.Example.com/jobs/42/?utm_source=x", null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("Senior Unity Dev", stored.Title);
    Assert.Equal(["https://example.com/jobs/42"], _reader.RequestedUrls);
  }

  [Fact]
  public async Task QuickAddAsync_ReaderReturnsNull_CreatesVacancyWithoutTitle()
  {
    _reader.Title = null;

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Null(stored.Title);
  }

  [Fact]
  public async Task QuickAddAsync_ExplicitTitle_StoresTrimmedTitleWithoutCallingReader()
  {
    _reader.Title = "From page";

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null, "  Senior Unity Dev  "));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("Senior Unity Dev", stored.Title);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task QuickAddAsync_BlankTitle_StoresReaderTitle(string? title)
  {
    _reader.Title = "From page";

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null, title));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal("From page", stored.Title);
    Assert.Single(_reader.RequestedUrls);
  }

  [Fact]
  public async Task QuickAddAsync_ExplicitTitleTooLong_ClampsToMaxLength()
  {
    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null, new string('d', Vacancy.TitleMaxLength + 50)));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal(Vacancy.TitleMaxLength, stored.Title!.Length);
  }

  [Fact]
  public async Task QuickAddAsync_ExistingUrl_DoesNotCallReader()
  {
    var service = TitledService();
    await service.QuickAddAsync(new QuickAddInput(DefaultUrl, null));
    _reader.RequestedUrls.Clear();

    var second = await service.QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    Assert.True(second.AlreadyExisted);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task QuickAddAsync_InvalidUrl_FailsWithoutCallingReader()
  {
    await Assert.ThrowsAsync<ValidationFailedException>(() => TitledService().QuickAddAsync(new QuickAddInput("not a url", null)));

    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task CreateAsync_ExplicitTitle_StoresTrimmedTitleWithoutCallingReader()
  {
    _reader.Title = "From page";

    var created = await TitledService().CreateAsync(Input(title: "  Senior Unity Dev  "));

    var stored = await LoadAsync(created.Id);
    Assert.Equal("Senior Unity Dev", stored.Title);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task CreateAsync_BlankTitle_StoresReaderTitleForNormalizedUrl(string? title)
  {
    _reader.Title = "From page";

    var created = await TitledService().CreateAsync(Input("http://www.Example.com/jobs/7/?utm_source=x", title: title));

    var stored = await LoadAsync(created.Id);
    Assert.Equal("From page", stored.Title);
    Assert.Equal(["https://example.com/jobs/7"], _reader.RequestedUrls);
  }

  [Fact]
  public async Task CreateAsync_DuplicateUrl_DoesNotCallReader()
  {
    var service = TitledService();
    await service.CreateAsync(Input(title: "first"));
    _reader.RequestedUrls.Clear();

    await Assert.ThrowsAsync<ValidationFailedException>(() => service.CreateAsync(Input()));

    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task UpdateAsync_NewTitle_StoresTrimmedTitleWithoutCallingReader()
  {
    var service = TitledService();
    var created = await service.CreateAsync(Input(title: "Old"));

    await service.UpdateAsync(created.Id, Input(title: "  New title  "));

    var stored = await LoadAsync(created.Id);
    Assert.Equal("New title", stored.Title);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task UpdateAsync_BlankTitle_ClearsTitleWithoutCallingReader()
  {
    _reader.Title = "From page";
    var service = TitledService();
    var created = await service.CreateAsync(Input(title: "Old"));

    await service.UpdateAsync(created.Id, Input(title: "   "));

    var stored = await LoadAsync(created.Id);
    Assert.Null(stored.Title);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task QuickAddAsync_ReaderTitleTooLong_ClampsToMaxLength()
  {
    _reader.Title = new string('a', Vacancy.TitleMaxLength + 50);

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal(Vacancy.TitleMaxLength, stored.Title!.Length);
  }

  [Fact]
  public async Task CreateAsync_InputTitleTooLong_ClampsToMaxLength()
  {
    var created = await TitledService().CreateAsync(Input(title: new string('b', Vacancy.TitleMaxLength + 50)));

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Vacancy.TitleMaxLength, stored.Title!.Length);
  }

  [Fact]
  public async Task UpdateAsync_InputTitleTooLong_ClampsToMaxLength()
  {
    var service = TitledService();
    var created = await service.CreateAsync(Input(title: "short"));

    await service.UpdateAsync(created.Id, Input(title: new string('c', Vacancy.TitleMaxLength + 50)));

    var stored = await LoadAsync(created.Id);
    Assert.Equal(Vacancy.TitleMaxLength, stored.Title!.Length);
  }

  [Fact]
  public async Task QuickAddAsync_SurrogatePairAtCutPoint_DoesNotSplitIt()
  {
    _reader.Title = new string('a', Vacancy.TitleMaxLength - 1) + "😀" + "tail";

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Equal(new string('a', Vacancy.TitleMaxLength - 1), stored.Title);
  }

  [Fact]
  public async Task QuickAddAsync_ReaderReturnsWhitespace_StoresNullTitle()
  {
    _reader.Title = "   ";

    var result = await TitledService().QuickAddAsync(new QuickAddInput(DefaultUrl, null));

    var stored = await LoadAsync(result.VacancyId);
    Assert.Null(stored.Title);
  }

  [Fact]
  public async Task ListAsync_InboxVacancyWithTitle_ReturnsTitle()
  {
    var service = TitledService();
    await service.CreateAsync(Input(title: "Listed title"));

    var items = await service.ListAsync(VacancyStage.Inbox);

    Assert.Equal("Listed title", Assert.Single(items).Title);
  }

  [Fact]
  public async Task GetBoardAsync_TargetedVacancyWithTitle_ReturnsTitleOnCard()
  {
    var trackId = await CreateTrackAsync();
    var id = await CreateOnBoardAsync(VacancyStage.Targeted, trackId);
    await MutateAsync(id, v => v.Title = "Board title");

    var board = await Service.GetBoardAsync(null);

    Assert.Equal("Board title", Assert.Single(board.Cards, c => c.Id == id).Title);
  }

  [Fact]
  public async Task FindAsync_VacancyWithTitle_ReturnsTitle()
  {
    var service = TitledService();
    var created = await service.CreateAsync(Input(title: "Detail title"));

    var found = await service.FindAsync(created.Id);

    Assert.Equal("Detail title", found!.Title);
  }

  [Fact]
  public async Task FetchTitleAsync_ValidUrl_ReturnsReaderTitleForNormalizedUrl()
  {
    _reader.Title = "Fetched";

    var title = await TitledService().FetchTitleAsync("http://www.Example.com/jobs/9/?utm_source=x");

    Assert.Equal("Fetched", title);
    Assert.Equal(["https://example.com/jobs/9"], _reader.RequestedUrls);
  }

  [Fact]
  public async Task FetchTitleAsync_InvalidUrl_FailsWithUrlErrorWithoutCallingReader()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => TitledService().FetchTitleAsync("not a url"));

    Assert.Contains("Url", ex.Errors.Keys);
    Assert.Empty(_reader.RequestedUrls);
  }

  [Fact]
  public async Task FetchTitleAsync_ReaderReturnsNull_ReturnsNull()
  {
    _reader.Title = null;

    var title = await TitledService().FetchTitleAsync(DefaultUrl);

    Assert.Null(title);
  }
}
