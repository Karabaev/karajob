using Karajob.Application.Common;
using Karajob.Application.Companies;
using Karajob.Domain.Companies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Companies;

public class CompanyServiceProjectTests : CompanyServiceFixture
{
  [Fact]
  public async Task AddProjectAsync_ValidInput_ReturnsAndPersistsNormalizedProject()
  {
    var company = await Service.CreateAsync(Input());

    var project = await Service.AddProjectAsync(company.Id, NewProject(
      name: "Dragon Quest",
      genre: "RPG",
      engine: "Unity",
      status: ProjectStatus.Released,
      url: "https://Example.com/Game"));

    Assert.Equal("Dragon Quest", project.Name);
    Assert.Equal("RPG", project.Genre);
    Assert.Equal("Unity", project.Engine);
    Assert.Equal(ProjectStatus.Released, project.Status);
    Assert.Equal("https://example.com/game", project.Url);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var stored = await context.Projects.SingleAsync();
    Assert.Equal(company.Id, stored.CompanyId);
    Assert.Equal(project.Id, stored.Id);
  }

  [Fact]
  public async Task AddProjectAsync_BlankOptionalStrings_StoredAsNull()
  {
    var company = await Service.CreateAsync(Input());

    var project = await Service.AddProjectAsync(company.Id, NewProject(genre: " ", engine: "", url: "  "));

    Assert.Null(project.Genre);
    Assert.Null(project.Engine);
    Assert.Null(project.Url);
  }

  [Fact]
  public async Task AddProjectAsync_BlankName_ReportsNameError()
  {
    var company = await Service.CreateAsync(Input());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.AddProjectAsync(company.Id, NewProject(name: " ")));

    Assert.Contains(nameof(ProjectInput.Name), exception.Errors.Keys);
  }

  [Fact]
  public async Task AddProjectAsync_InvalidUrl_ReportsUrlErrorAndSavesNothing()
  {
    var company = await Service.CreateAsync(Input());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.AddProjectAsync(company.Id, NewProject(url: "not a url")));

    Assert.Contains(nameof(ProjectInput.Url), exception.Errors.Keys);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Empty(await context.Projects.ToListAsync());
  }

  [Fact]
  public async Task AddProjectAsync_BlankNameAndInvalidUrl_ReportsBothErrors()
  {
    var company = await Service.CreateAsync(Input());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(
      () => Service.AddProjectAsync(company.Id, NewProject(name: "", url: "ftp://x")));

    Assert.Equal(2, exception.Errors.Count);
  }

  [Fact]
  public async Task AddProjectAsync_UnknownCompany_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.AddProjectAsync(Guid.NewGuid(), NewProject()));
  }

  [Fact]
  public async Task UpdateProjectAsync_ValidInput_UpdatesFields()
  {
    var company = await Service.CreateAsync(Input());
    var project = await Service.AddProjectAsync(company.Id, NewProject());

    var updated = await Service.UpdateProjectAsync(project.Id, NewProject(
      name: "Renamed",
      genre: "Action",
      engine: null,
      status: ProjectStatus.Announced,
      url: "HTTPS://Example.com/New"));

    Assert.Equal(project.Id, updated.Id);
    Assert.Equal("Renamed", updated.Name);
    Assert.Equal("Action", updated.Genre);
    Assert.Null(updated.Engine);
    Assert.Equal(ProjectStatus.Announced, updated.Status);
    Assert.Equal("https://example.com/new", updated.Url);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal("Renamed", (await context.Projects.SingleAsync()).Name);
  }

  [Fact]
  public async Task UpdateProjectAsync_BlankName_ReportsNameErrorAndKeepsProject()
  {
    var company = await Service.CreateAsync(Input());
    var project = await Service.AddProjectAsync(company.Id, NewProject());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateProjectAsync(project.Id, NewProject(name: "")));

    Assert.Contains(nameof(ProjectInput.Name), exception.Errors.Keys);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal("Dragon Quest", (await context.Projects.SingleAsync()).Name);
  }

  [Fact]
  public async Task UpdateProjectAsync_InvalidUrl_ReportsUrlError()
  {
    var company = await Service.CreateAsync(Input());
    var project = await Service.AddProjectAsync(company.Id, NewProject());

    var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateProjectAsync(project.Id, NewProject(url: "nope")));

    Assert.Contains(nameof(ProjectInput.Url), exception.Errors.Keys);
  }

  [Fact]
  public async Task UpdateProjectAsync_UnknownProject_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateProjectAsync(Guid.NewGuid(), NewProject()));
  }

  [Fact]
  public async Task DeleteProjectAsync_ExistingProject_RemovesOnlyThatProject()
  {
    var company = await Service.CreateAsync(Input());
    var doomed = await Service.AddProjectAsync(company.Id, NewProject(name: "Doomed"));
    await Service.AddProjectAsync(company.Id, NewProject(name: "Survivor"));

    await Service.DeleteProjectAsync(doomed.Id);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(["Survivor"], (await context.Projects.ToListAsync()).Select(p => p.Name));
  }

  [Fact]
  public async Task DeleteProjectAsync_UnknownProject_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.DeleteProjectAsync(Guid.NewGuid()));
  }
}
