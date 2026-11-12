using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Hypotheses;
using Karajob.Domain.Users;

namespace Karajob.Tests.Domain;

public class RequiredStringSetterTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  public static TheoryData<string, Action<string>> BlankSetters => new()
  {
    { "User.Name", value => new User("Anna", Now).Name = value },
    { "Company.Name", value => new Company("ubisoft", "Ubisoft").Name = value },
    { "Project.Name", value => new Project(Guid.NewGuid(), "Game", ProjectStatus.Released).Name = value },
    { "CvDocument.Name", value => NewCv().Name = value },
    { "CvDocument.Language", value => NewCv().Language = value },
    { "Hypothesis.Name", value => NewHypothesis().Name = value },
    { "Hypothesis.Role", value => NewHypothesis().Role = value },
    { "Hypothesis.Language", value => NewHypothesis().Language = value },
  };

  [Theory]
  [MemberData(nameof(BlankSetters))]
  public void Setter_Blank_Throws(string property, Action<string> set)
  {
    Assert.False(string.IsNullOrEmpty(property));
    Assert.ThrowsAny<ArgumentException>(() => set(""));
    Assert.ThrowsAny<ArgumentException>(() => set("   "));
  }

  [Fact]
  public void Setter_Value_IsTrimmed()
  {
    var hypothesis = NewHypothesis();

    hypothesis.Role = "  Unity Developer ";

    Assert.Equal("Unity Developer", hypothesis.Role);
  }

  private static CvDocument NewCv()
  {
    return new CvDocument(Guid.NewGuid(), "Unity CV", "en", Now);
  }

  private static Hypothesis NewHypothesis()
  {
    return new Hypothesis(Guid.NewGuid(), "Unity RU", "Unity Developer", "ru", Now);
  }
}
