using Bunit;
using Karajob.Application.Catalogs;
using Karajob.Application.Companies;
using Karajob.Domain.Companies;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Components.Companies;
using MudBlazor;

namespace Karajob.Tests.Web.Components.Companies;

public class ProjectDialogTests : CompanyWebFixture
{
  [Fact]
  public async Task Open_NewProject_ShowsEngineSelectWithCatalogEnginesAndNoSelection()
  {
    var (_, provider) = await OpenNewProjectDialogAsync();

    provider.WaitForAssertion(() =>
    {
      var select = EngineSelect(provider);
      Assert.Equal("Engine", select.Instance.Label);
      Assert.Null(select.Instance.Value);
      var ids = select.FindComponents<MudSelectItem<Guid?>>().Select(i => i.Instance.Value).ToList();
      Assert.Contains(SeedData.UnityEngineId, ids);
      Assert.Contains(SeedData.ProprietaryEngineId, ids);
    });
  }

  [Fact]
  public async Task Open_EngineAddedToCatalog_IsListedInEngineSelect()
  {
    var created = await Catalogs.CreateAsync(CatalogKind.Engine, new CatalogInput("Phaser"));

    var (_, provider) = await OpenNewProjectDialogAsync();

    provider.WaitForAssertion(() => Assert.Contains(
      EngineSelect(provider).FindComponents<MudSelectItem<Guid?>>(),
      i => i.Instance.Value == created.Id));
  }

  [Fact]
  public async Task Save_NewProjectWithEngine_PersistsEngineAndShowsItInSubtitle()
  {
    var (cut, provider) = await OpenNewProjectDialogAsync();
    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindComponents<MudTextField<string>>()));
    await provider.InvokeAsync(() => NameField(provider).Instance.ValueChanged.InvokeAsync("Dragon Quest"));
    await provider.InvokeAsync(() => EngineSelect(provider).Instance.ValueChanged.InvokeAsync(SeedData.GodotEngineId));

    provider.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

    var details = await WaitForProjectsAsync(1);
    var project = Assert.Single(details.Projects);
    Assert.Equal(SeedData.GodotEngineId, project.EngineId);
    Assert.Equal("Godot", project.EngineName);
    cut.WaitForAssertion(() => Assert.Contains("Godot", cut.Markup));
  }

  [Fact]
  public async Task Save_NewProjectWithoutEngine_PersistsNullEngine()
  {
    var (_, provider) = await OpenNewProjectDialogAsync();
    provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindComponents<MudTextField<string>>()));
    await provider.InvokeAsync(() => NameField(provider).Instance.ValueChanged.InvokeAsync("Dragon Quest"));

    provider.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

    var details = await WaitForProjectsAsync(1);
    Assert.Null(Assert.Single(details.Projects).EngineId);
  }

  [Fact]
  public async Task Open_ExistingProjectWithEngine_PreselectsEngine()
  {
    var company = await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    await Service.AddProjectAsync(company.Id, new ProjectInput("Dragon Quest", null, SeedData.UnrealEngineId, ProjectStatus.InDevelopment, null));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));

    cut.WaitForElement("button[aria-label='Edit project']").Click();

    provider.WaitForAssertion(() => Assert.Equal(SeedData.UnrealEngineId, EngineSelect(provider).Instance.Value));
  }

  [Fact]
  public async Task Save_ExistingProjectWithEngineChanged_ReplacesEngine()
  {
    var company = await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    await Service.AddProjectAsync(company.Id, new ProjectInput("Dragon Quest", null, SeedData.UnrealEngineId, ProjectStatus.InDevelopment, null));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));
    cut.WaitForElement("button[aria-label='Edit project']").Click();
    provider.WaitForAssertion(() => Assert.Equal(SeedData.UnrealEngineId, EngineSelect(provider).Instance.Value));
    await provider.InvokeAsync(() => EngineSelect(provider).Instance.ValueChanged.InvokeAsync(SeedData.DefoldEngineId));

    provider.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

    var deadline = DateTime.UtcNow.AddSeconds(5);
    var details = await Service.FindBySlugAsync("pixel-forge");
    while (DateTime.UtcNow < deadline && details!.Projects[0].EngineId != SeedData.DefoldEngineId)
    {
      await Task.Delay(50);
      details = await Service.FindBySlugAsync("pixel-forge");
    }

    Assert.Equal(SeedData.DefoldEngineId, details!.Projects[0].EngineId);
    Assert.Equal("Defold", details.Projects[0].EngineName);
  }

  private async Task<(IRenderedComponent<CompanyPage> Cut, IRenderedComponent<MudDialogProvider> Provider)> OpenNewProjectDialogAsync()
  {
    await Service.CreateAsync(Input("pixel-forge", "Pixel Forge"));
    Context.Render<MudPopoverProvider>();
    var provider = Context.Render<MudDialogProvider>();
    var cut = Context.Render<CompanyPage>(p => p.Add(c => c.Slug, "pixel-forge"));
    cut.WaitForAssertion(() => Assert.Equal("Pixel Forge", cut.Find(".company-name input").GetAttribute("value")));

    cut.FindAll("button").First(b => b.TextContent.Contains("+ Project")).Click();

    return (cut, provider);
  }

  private async Task<CompanyDetails> WaitForProjectsAsync(int count)
  {
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var details = await Service.FindBySlugAsync("pixel-forge");
    while (DateTime.UtcNow < deadline && details!.Projects.Count < count)
    {
      await Task.Delay(50);
      details = await Service.FindBySlugAsync("pixel-forge");
    }

    return details!;
  }

  private static IRenderedComponent<MudSelect<Guid?>> EngineSelect(IRenderedComponent<MudDialogProvider> provider)
  {
    return provider.FindComponents<MudSelect<Guid?>>().Single(c => c.Instance.Class?.Contains("project-engine") == true);
  }

  private static IRenderedComponent<MudTextField<string>> NameField(IRenderedComponent<MudDialogProvider> provider)
  {
    return provider.FindComponents<MudTextField<string>>().First(c => c.Instance.Label == "Name");
  }
}
