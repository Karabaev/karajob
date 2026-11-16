using System.Globalization;
using Karajob.Application.Resources;

namespace Karajob.Tests.Application.MasterProfiles;

public class MasterProfileTemplateTests
{
  private static readonly string[] EnglishSections =
    ["Contacts", "Experience", "Projects", "Achievements with numbers", "Skills", "Education", "Languages", "Interview stories (STAR)"];

  private static readonly string[] RussianSections =
  [
    "Контакты",
    "Опыт",
    "Проекты",
    "Достижения с цифрами",
    "Навыки",
    "Образование",
    "Языки",
    "Истории для интервью (STAR)",
  ];

  public static TheoryData<string, string[]> Cultures => new()
  {
    { "en", EnglishSections },
    { "ru", RussianSections },
  };

  [Theory]
  [MemberData(nameof(Cultures))]
  public void Template_HasHeadingForEachFixedSection(string culture, string[] sections)
  {
    var template = MasterProfileTestTemplate.In(CultureInfo.GetCultureInfo(culture));

    foreach (var section in sections)
    {
      Assert.Contains($"\n## {section}\n", template);
    }
  }

  [Fact]
  public void Template_RussianDiffersFromEnglish()
  {
    Assert.NotEqual(MasterProfileTestTemplate.English, MasterProfileTestTemplate.In(TestCulture.Russian));
  }

  [Fact]
  public void Template_ResourceExists()
  {
    Assert.False(TestLocalizer.For<MasterProfileMessages>()["Template"].ResourceNotFound);
  }
}
