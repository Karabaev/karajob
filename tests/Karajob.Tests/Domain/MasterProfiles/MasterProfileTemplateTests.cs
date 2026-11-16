using Karajob.Domain.MasterProfiles;

namespace Karajob.Tests.Domain.MasterProfiles;

public class MasterProfileTemplateTests
{
  [Fact]
  public void Sections_HasEightEntries()
  {
    Assert.Equal(8, MasterProfileTemplate.Sections.Count);
  }

  [Fact]
  public void Markdown_ContainsHeadingForEachSection()
  {
    foreach (var section in MasterProfileTemplate.Sections)
    {
      Assert.Contains($"## {section}", MasterProfileTemplate.Markdown);
    }
  }
}
