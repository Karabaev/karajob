using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Companies;

public class CompanyServiceNoteTests : CompanyServiceFixture
{
  [Fact]
  public async Task SaveNoteAsync_NoExistingNote_CreatesNote()
  {
    var company = await Service.CreateAsync(Input());

    await Service.SaveNoteAsync(company.Id, "good place", true);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var note = await context.UserCompanyNotes.SingleAsync();
    Assert.Equal(SeedData.OwnerId, note.UserId);
    Assert.Equal(company.Id, note.CompanyId);
    Assert.Equal("good place", note.Note);
    Assert.True(note.IsBlacklisted);
  }

  [Fact]
  public async Task SaveNoteAsync_ExistingNote_UpdatesInPlace()
  {
    var company = await Service.CreateAsync(Input());
    await Service.SaveNoteAsync(company.Id, "first", true);

    await Service.SaveNoteAsync(company.Id, "second", false);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var note = await context.UserCompanyNotes.SingleAsync();
    Assert.Equal("second", note.Note);
    Assert.False(note.IsBlacklisted);
  }

  [Fact]
  public async Task SaveNoteAsync_BlankNote_StoredAsNull()
  {
    var company = await Service.CreateAsync(Input());

    await Service.SaveNoteAsync(company.Id, "   ", true);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Null((await context.UserCompanyNotes.SingleAsync()).Note);
  }

  [Fact]
  public async Task SaveNoteAsync_OtherUsersNote_IsUnaffected()
  {
    var company = await Service.CreateAsync(Input());
    var second = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(second.Id).SaveNoteAsync(company.Id, "theirs", true);

    await Service.SaveNoteAsync(company.Id, "mine", false);

    await using var context = Database.CreateContext(second.Id);
    var theirs = await context.UserCompanyNotes.SingleAsync();
    Assert.Equal("theirs", theirs.Note);
    Assert.True(theirs.IsBlacklisted);
  }

  [Fact]
  public async Task SaveNoteAsync_UnknownCompany_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.SaveNoteAsync(Guid.NewGuid(), "x", false));
  }

  [Fact]
  public async Task SaveNoteAsync_Blacklist_IsVisibleInDetails()
  {
    var company = await Service.CreateAsync(Input());

    await Service.SaveNoteAsync(company.Id, "avoid", true);

    var details = await Service.FindBySlugAsync("pixel-forge");
    Assert.Equal("avoid", details!.Note);
    Assert.True(details.IsBlacklisted);
  }
}
