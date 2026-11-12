using Karajob.Domain.Companies;

namespace Karajob.Tests.Domain.Companies;

public class ProjectTests
{
  [Fact]
  public void Ctor_EmptyCompanyId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Project(Guid.Empty, "Game", ProjectStatus.Announced));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void Ctor_BlankName_Throws(string name)
  {
    Assert.ThrowsAny<ArgumentException>(() => new Project(Guid.NewGuid(), name, ProjectStatus.Announced));
  }

  [Fact]
  public void Ctor_Valid_SetsFields()
  {
    var companyId = Guid.NewGuid();

    var project = new Project(companyId, " Game ", ProjectStatus.InDevelopment);

    Assert.NotEqual(Guid.Empty, project.Id);
    Assert.Equal(companyId, project.CompanyId);
    Assert.Equal("Game", project.Name);
    Assert.Equal(ProjectStatus.InDevelopment, project.Status);
  }
}
