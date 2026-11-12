using System.Xml.Linq;

namespace Karajob.Tests.Architecture;

public class ProjectReferenceTests
{
  [Fact]
  public void Domain_HasNoProjectPackageOrFrameworkReferences()
  {
    var project = LoadProject("Karajob.Domain");

    Assert.Empty(project.ProjectReferences);
    Assert.Empty(project.PackageReferences);
    Assert.Empty(project.FrameworkReferences);
  }

  [Fact]
  public void Application_ReferencesOnlyDomain()
  {
    var project = LoadProject("Karajob.Application");

    Assert.Equal(["Karajob.Domain"], project.ProjectReferences);
  }

  [Fact]
  public void Infrastructure_ReferencesOnlyApplicationAndDomain()
  {
    var project = LoadProject("Karajob.Infrastructure");

    Assert.Equal(["Karajob.Application", "Karajob.Domain"], project.ProjectReferences);
  }

  [Fact]
  public void Web_ReferencesApplicationInfrastructureAndDomain()
  {
    var project = LoadProject("Karajob.Web");

    Assert.Equal(["Karajob.Application", "Karajob.Domain", "Karajob.Infrastructure"], project.ProjectReferences);
  }

  private static ProjectInfo LoadProject(string name)
  {
    var path = Path.Combine(FindRepoRoot(), "src", name, name + ".csproj");
    Assert.True(File.Exists(path), $"Project file not found: {path}");

    var document = XDocument.Load(path);
    return new ProjectInfo(
      Includes(document, "ProjectReference", fileNameOnly: true),
      Includes(document, "PackageReference", fileNameOnly: false),
      Includes(document, "FrameworkReference", fileNameOnly: false));
  }

  private static string[] Includes(XDocument document, string element, bool fileNameOnly)
  {
    return document.Descendants(element)
      .Select(e => (string?)e.Attribute("Include"))
      .OfType<string>()
      .Select(include => fileNameOnly ? Path.GetFileNameWithoutExtension(include.Replace('\\', '/')) : include)
      .Order(StringComparer.Ordinal)
      .ToArray();
  }

  private static string FindRepoRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "Karajob.slnx")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException(
      $"Karajob.slnx not found in any parent directory of '{AppContext.BaseDirectory}'.");
  }

  private record ProjectInfo(
    string[] ProjectReferences,
    string[] PackageReferences,
    string[] FrameworkReferences);
}
