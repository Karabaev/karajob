using Karajob.Application.Common;
using Karajob.Application.Companies;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Karajob.Tests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Companies;

public class CompanyServiceTests : CompanyServiceFixture
{
  [Fact]
  public async Task CreateAsync_ValidInput_ReturnsNormalizedDetails()
  {
    var details = await Service.CreateAsync(Input(
      slug: "Pixel-Forge",
      website: "https://PixelForge.example",
      description: "Indie studio",
      domains: ["https://www.Example.com/path", "alpha.io"],
      aliases: ["  Pixel   FORGE Games ", "PF"]));

    Assert.Equal("pixel-forge", details.Slug);
    Assert.Equal("Pixel Forge", details.Name);
    Assert.Equal("https://pixelforge.example", details.Website);
    Assert.Equal("Indie studio", details.Description);
    Assert.Equal(["alpha.io", "example.com"], details.Domains);
    Assert.Equal(["pf", "pixel forge games"], details.Aliases);
    Assert.Empty(details.Projects);
    Assert.Null(details.Note);
    Assert.False(details.IsBlacklisted);
  }

  [Fact]
  public async Task CreateAsync_ValidInput_PersistsCompanyWithDomainsAndAliases()
  {
    var details = await Service.CreateAsync(Input(domains: ["example.com"], aliases: ["pf"]));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var company = await context.Companies.Include(c => c.Domains).Include(c => c.Aliases).SingleAsync(c => c.Id == details.Id);
    Assert.Equal("pixel-forge", company.Slug);
    Assert.Equal(["example.com"], company.Domains.Select(d => d.Value));
    Assert.Equal(["pf"], company.Aliases.Select(a => a.Value));
  }

  [Fact]
  public async Task CreateAsync_BlankWebsiteAndDescription_StoredAsNull()
  {
    var details = await Service.CreateAsync(Input(website: "  ", description: ""));

    Assert.Null(details.Website);
    Assert.Null(details.Description);
  }

  [Fact]
  public async Task CreateAsync_BlankDomainAndAliasEntries_AreIgnored()
  {
    var details = await Service.CreateAsync(Input(domains: ["example.com", "", "  "], aliases: [" ", "pf"]));

    Assert.Equal(["example.com"], details.Domains);
    Assert.Equal(["pf"], details.Aliases);
  }

  [Fact]
  public async Task CreateAsync_DuplicateDomainsAndAliases_AreCollapsed()
  {
    var details = await Service.CreateAsync(Input(
      domains: ["example.com", "https://www.EXAMPLE.com/x"],
      aliases: ["PF", "pf", " pf "]));

    Assert.Equal(["example.com"], details.Domains);
    Assert.Equal(["pf"], details.Aliases);
  }

  [Fact]
  public async Task CreateAsync_ReservedSlug_ReportsSlugErrorAndSavesNothing()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(slug: "New")));

    Assert.Equal([nameof(CompanyInput.Slug)], exception.Errors.Keys);
    Assert.Empty(await Service.SearchAsync(null));
  }

  [Fact]
  public async Task CreateAsync_MultipleInvalidFields_ReportsAllErrorsAtOnce()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(slug: "not a slug!", name: " ", domains: ["nodots"])));

    Assert.Equal(3, exception.Errors.Count);
    Assert.Contains(nameof(CompanyInput.Slug), exception.Errors.Keys);
    Assert.Contains(nameof(CompanyInput.Name), exception.Errors.Keys);
    Assert.Contains(nameof(CompanyInput.Domains), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_InvalidInput_SavesNothing()
  {
    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(name: "")));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.Companies.ToListAsync());
  }

  [Fact]
  public async Task CreateAsync_BlankSlug_ReportsSlugError()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(slug: "")));

    Assert.Contains(nameof(CompanyInput.Slug), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_NonHttpWebsite_ReportsWebsiteError()
  {
    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(website: "ftp://example.com")));

    Assert.Contains(nameof(CompanyInput.Website), exception.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsync_SlugUsedByAnotherCompany_ReportsSlugErrorWithOtherName()
  {
    await Service.CreateAsync(Input(slug: "taken", name: "Taken Studio"));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(Input(slug: "TAKEN", name: "Other")));

    Assert.Contains("Taken Studio", exception.Errors[nameof(CompanyInput.Slug)]);
  }

  [Fact]
  public async Task CreateAsync_DomainUsedByAnotherCompany_ReportsDomainsErrorWithOtherName()
  {
    await Service.CreateAsync(Input(slug: "taken", name: "Taken Studio", domains: ["example.com"]));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(slug: "other", name: "Other", domains: ["https://www.example.com"])));

    Assert.Contains("Taken Studio", exception.Errors[nameof(CompanyInput.Domains)]);
  }

  [Fact]
  public async Task CreateAsync_AliasUsedByAnotherCompany_ReportsAliasesErrorWithOtherName()
  {
    await Service.CreateAsync(Input(slug: "taken", name: "Taken Studio", aliases: ["pf"]));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.CreateAsync(Input(slug: "other", name: "Other", aliases: ["PF"])));

    Assert.Contains("Taken Studio", exception.Errors[nameof(CompanyInput.Aliases)]);
  }

  [Fact]
  public async Task UpdateAsync_SameSlugDomainsAndAliases_DoesNotConflictWithItself()
  {
    var created = await Service.CreateAsync(Input(domains: ["example.com"], aliases: ["pf"]));

    var updated = await Service.UpdateAsync(created.Id, Input(name: "Pixel Forge Games", domains: ["example.com"], aliases: ["pf"]));

    Assert.Equal("Pixel Forge Games", updated.Name);
    Assert.Equal(created.Id, updated.Id);
  }

  [Fact]
  public async Task UpdateAsync_ChangedDomainsAndAliases_SyncsPersistedRows()
  {
    var created = await Service.CreateAsync(Input(domains: ["keep.com", "remove.com"], aliases: ["keep", "remove"]));

    await Service.UpdateAsync(created.Id, Input(domains: ["keep.com", "added.com"], aliases: ["keep", "added"]));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var company = await context.Companies.Include(c => c.Domains).Include(c => c.Aliases).SingleAsync(c => c.Id == created.Id);
    Assert.Equal(["added.com", "keep.com"], company.Domains.Select(d => d.Value).Order());
    Assert.Equal(["added", "keep"], company.Aliases.Select(a => a.Value).Order());
    Assert.Equal(2, await context.CompanyDomains.CountAsync());
    Assert.Equal(2, await context.CompanyAliases.CountAsync());
  }

  [Fact]
  public async Task UpdateAsync_KeptDomain_KeepsSameRowId()
  {
    var created = await Service.CreateAsync(Input(domains: ["keep.com"]));
    await using var before = Database.CreateContext(SeedData.OwnerId);
    var rowId = (await before.CompanyDomains.SingleAsync()).Id;

    await Service.UpdateAsync(created.Id, Input(domains: ["keep.com", "added.com"]));

    await using var after = Database.CreateContext(SeedData.OwnerId);
    Assert.Contains(await after.CompanyDomains.ToListAsync(), d => d.Id == rowId);
  }

  [Fact]
  public async Task UpdateAsync_NewSlug_ChangesSlug()
  {
    var created = await Service.CreateAsync(Input());

    var updated = await Service.UpdateAsync(created.Id, Input(slug: "New-Slug"));

    Assert.Equal("new-slug", updated.Slug);
  }

  [Fact]
  public async Task UpdateAsync_SlugUsedByAnotherCompany_ReportsSlugErrorWithOtherName()
  {
    await Service.CreateAsync(Input(slug: "taken", name: "Taken Studio"));
    var created = await Service.CreateAsync(Input(slug: "mine", name: "Mine"));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(slug: "taken", name: "Mine")));

    Assert.Contains("Taken Studio", exception.Errors[nameof(CompanyInput.Slug)]);
  }

  [Fact]
  public async Task UpdateAsync_DomainUsedByAnotherCompany_ReportsDomainsError()
  {
    await Service.CreateAsync(Input(slug: "taken", name: "Taken Studio", domains: ["example.com"]));
    var created = await Service.CreateAsync(Input(slug: "mine", name: "Mine"));

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.UpdateAsync(created.Id, Input(slug: "mine", name: "Mine", domains: ["example.com"])));

    Assert.Contains("Taken Studio", exception.Errors[nameof(CompanyInput.Domains)]);
  }

  [Fact]
  public async Task UpdateAsync_InvalidInput_LeavesCompanyUnchanged()
  {
    var created = await Service.CreateAsync(Input(domains: ["example.com"]));

    await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, Input(name: "New", domains: ["bad"])));

    var details = await Service.FindBySlugAsync("pixel-forge");
    Assert.Equal("Pixel Forge", details!.Name);
    Assert.Equal(["example.com"], details.Domains);
  }

  [Fact]
  public async Task UpdateAsync_UnknownId_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(Guid.NewGuid(), Input()));
  }

  [Fact]
  public async Task SearchAsync_BlankQuery_ReturnsAllOrderedByName()
  {
    await Service.CreateAsync(Input(slug: "b", name: "Bravo"));
    await Service.CreateAsync(Input(slug: "a", name: "Alpha"));
    await Service.CreateAsync(Input(slug: "c", name: "Charlie"));

    var all = await Service.SearchAsync(null);
    var blank = await Service.SearchAsync("  ");

    Assert.Equal(["Alpha", "Bravo", "Charlie"], all.Select(c => c.Name));
    Assert.Equal(["Alpha", "Bravo", "Charlie"], blank.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_ByNameCaseInsensitive_FindsCompany()
  {
    await Service.CreateAsync(Input(slug: "pixel-forge", name: "Pixel Forge"));
    await Service.CreateAsync(Input(slug: "other", name: "Other"));

    var result = await Service.SearchAsync("xEL fo");

    Assert.Equal(["Pixel Forge"], result.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_ByCyrillicName_FindsCompany()
  {
    await Service.CreateAsync(Input(slug: "lesta-igry", name: "Леста Игры"));
    await Service.CreateAsync(Input(slug: "other", name: "Other"));

    var result = await Service.SearchAsync("лест");

    Assert.Equal(["Леста Игры"], result.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_BySlug_FindsCompany()
  {
    await Service.CreateAsync(Input(slug: "zzz-slug", name: "Alpha"));
    await Service.CreateAsync(Input(slug: "other", name: "Other"));

    var result = await Service.SearchAsync("ZZZ-sl");

    Assert.Equal(["Alpha"], result.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_ByDomain_FindsCompany()
  {
    await Service.CreateAsync(Input(slug: "a", name: "Alpha", domains: ["findme.example"]));
    await Service.CreateAsync(Input(slug: "b", name: "Bravo", domains: ["other.example"]));

    var result = await Service.SearchAsync("findme");

    Assert.Equal(["Alpha"], result.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_ByAlias_FindsCompany()
  {
    await Service.CreateAsync(Input(slug: "a", name: "Alpha", aliases: ["secret alias"]));
    await Service.CreateAsync(Input(slug: "b", name: "Bravo"));

    var result = await Service.SearchAsync("SECRET");

    Assert.Equal(["Alpha"], result.Select(c => c.Name));
  }

  [Fact]
  public async Task SearchAsync_NoMatch_ReturnsEmpty()
  {
    await Service.CreateAsync(Input());

    Assert.Empty(await Service.SearchAsync("zzzzzz"));
  }

  [Fact]
  public async Task SearchAsync_BlacklistedByCurrentUser_FlagsListItem()
  {
    var created = await Service.CreateAsync(Input());
    await Service.SaveNoteAsync(created.Id, "bad", true);

    var result = await Service.SearchAsync(null);

    Assert.True(Assert.Single(result).IsBlacklisted);
  }

  [Fact]
  public async Task SearchAsync_BlacklistedByAnotherUser_IsNotFlagged()
  {
    var created = await Service.CreateAsync(Input());
    var second = await PersistenceFixtures.AddUserAsync(Database);
    await ServiceFor(second.Id).SaveNoteAsync(created.Id, "bad", true);

    var result = await Service.SearchAsync(null);

    Assert.False(Assert.Single(result).IsBlacklisted);
  }

  [Fact]
  public async Task SearchAsync_ListItem_ContainsDomains()
  {
    await Service.CreateAsync(Input(domains: ["b.com", "a.com"]));

    var item = Assert.Single(await Service.SearchAsync(null));

    Assert.Equal(["a.com", "b.com"], item.Domains.Order());
  }

  [Fact]
  public async Task IconUrl_WebsiteSet_UsesWebsiteHost()
  {
    await Service.CreateAsync(Input(website: "https://pixelforge.example", domains: ["a.com"]));

    var item = Assert.Single(await Service.SearchAsync(null));
    var details = await Service.FindBySlugAsync("pixel-forge");

    Assert.Equal("https://pixelforge.example/favicon.ico", item.IconUrl);
    Assert.Equal("https://pixelforge.example/favicon.ico", details!.IconUrl);
  }

  [Fact]
  public async Task IconUrl_NoWebsite_UsesAlphabeticallyFirstDomain()
  {
    await Service.CreateAsync(Input(domains: ["zeta.com", "alpha.com"]));

    var item = Assert.Single(await Service.SearchAsync(null));
    var details = await Service.FindBySlugAsync("pixel-forge");

    Assert.Equal("https://alpha.com/favicon.ico", item.IconUrl);
    Assert.Equal("https://alpha.com/favicon.ico", details!.IconUrl);
  }

  [Fact]
  public async Task IconUrl_NoWebsiteAndNoDomains_IsNull()
  {
    await Service.CreateAsync(Input());

    var item = Assert.Single(await Service.SearchAsync(null));
    var details = await Service.FindBySlugAsync("pixel-forge");

    Assert.Null(item.IconUrl);
    Assert.Null(details!.IconUrl);
  }

  [Fact]
  public async Task FindBySlugAsync_DifferentCase_FindsCompany()
  {
    await Service.CreateAsync(Input(domains: ["example.com"], aliases: ["pf"]));

    var details = await Service.FindBySlugAsync("PIXEL-Forge");

    Assert.Equal("Pixel Forge", details!.Name);
    Assert.Equal(["example.com"], details.Domains);
    Assert.Equal(["pf"], details.Aliases);
  }

  [Fact]
  public async Task FindBySlugAsync_UnknownSlug_ReturnsNull()
  {
    Assert.Null(await Service.FindBySlugAsync("nope"));
  }

  [Theory]
  [InlineData("not a slug!")]
  [InlineData("")]
  [InlineData("  ")]
  public async Task FindBySlugAsync_InvalidSlug_ReturnsNull(string slug)
  {
    await Service.CreateAsync(Input());

    Assert.Null(await Service.FindBySlugAsync(slug));
  }

  [Fact]
  public async Task FindBySlugAsync_ReturnsProjectsOrderedByName()
  {
    var created = await Service.CreateAsync(Input());
    await Service.AddProjectAsync(created.Id, NewProject(name: "Zeta"));
    await Service.AddProjectAsync(created.Id, NewProject(name: "Alpha"));

    var details = await Service.FindBySlugAsync("pixel-forge");

    Assert.Equal(["Alpha", "Zeta"], details!.Projects.Select(p => p.Name));
  }

  [Fact]
  public async Task FindBySlugAsync_ReturnsOnlyCurrentUsersNote()
  {
    var created = await Service.CreateAsync(Input());
    var second = await PersistenceFixtures.AddUserAsync(Database);
    await Service.SaveNoteAsync(created.Id, "mine", false);
    await ServiceFor(second.Id).SaveNoteAsync(created.Id, "theirs", true);

    var mine = await Service.FindBySlugAsync("pixel-forge");
    var theirs = await ServiceFor(second.Id).FindBySlugAsync("pixel-forge");

    Assert.Equal("mine", mine!.Note);
    Assert.False(mine.IsBlacklisted);
    Assert.Equal("theirs", theirs!.Note);
    Assert.True(theirs.IsBlacklisted);
  }

  [Fact]
  public async Task DeleteAsync_CompanyWithoutDependencies_DeletesCompanyDomainsAliasesAndAllNotes()
  {
    var created = await Service.CreateAsync(Input(domains: ["example.com"], aliases: ["pf"]));
    var second = await PersistenceFixtures.AddUserAsync(Database);
    await Service.SaveNoteAsync(created.Id, "mine", false);
    await ServiceFor(second.Id).SaveNoteAsync(created.Id, "theirs", true);

    await Service.DeleteAsync(created.Id);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.Companies.ToListAsync());
    Assert.Empty(await context.CompanyDomains.ToListAsync());
    Assert.Empty(await context.CompanyAliases.ToListAsync());
    Assert.Empty(await context.UserCompanyNotes.IgnoreQueryFilters().ToListAsync());
  }

  [Fact]
  public async Task DeleteAsync_CompanyWithProject_ThrowsValidationFailedAndKeepsCompany()
  {
    var created = await Service.CreateAsync(Input());
    await Service.AddProjectAsync(created.Id, NewProject());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindBySlugAsync("pixel-forge"));
  }

  [Fact]
  public async Task DeleteAsync_CompanyReferencedByAnotherUsersVacancy_ThrowsValidationFailedAndKeepsCompany()
  {
    var created = await Service.CreateAsync(Input());
    var second = await PersistenceFixtures.AddUserAsync(Database);
    var vacancy = new Vacancy(second.Id, "https://example.com/jobs/1", VacancySource.Manual, PersistenceFixtures.Moment)
    {
      CompanyId = created.Id,
    };
    await using (var context = Database.CreateContext(second.Id))
    {
      context.Vacancies.Add(vacancy);
      await context.SaveChangesAsync();
    }

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.DeleteAsync(created.Id));

    Assert.Contains(string.Empty, exception.Errors.Keys);
    Assert.NotNull(await Service.FindBySlugAsync("pixel-forge"));
  }

  [Fact]
  public async Task DeleteAsync_UnknownId_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteAsync(Guid.NewGuid()));
  }
}
