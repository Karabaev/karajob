using Karajob.Domain.Vacancies;

namespace Karajob.Tests.Domain.Vacancies;

public class VacancyTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static Vacancy Create() => new(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual, Now);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndDefaults()
  {
    var userId = Guid.NewGuid();

    var vacancy = new Vacancy(userId, "https://www.Example.com/jobs/1?utm_source=x", VacancySource.Manual, Now);

    Assert.NotEqual(Guid.Empty, vacancy.Id);
    Assert.Equal(userId, vacancy.UserId);
    Assert.Equal(VacancyStage.Inbox, vacancy.Stage);
    Assert.Null(vacancy.DismissReason);
    Assert.Equal("https://example.com/jobs/1", vacancy.Url);
    Assert.Equal(VacancySource.Manual, vacancy.Source);
    Assert.Equal(Now, vacancy.DiscoveredAt);
    Assert.Empty(vacancy.Matches);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new Vacancy(Guid.Empty, "https://example.com/jobs/1", VacancySource.Manual, Now));
  }

  [Fact]
  public void Ctor_InvalidUrl_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Vacancy(Guid.NewGuid(), "not a url", VacancySource.Manual, Now));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcDiscoveredAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new Vacancy(Guid.NewGuid(), "https://example.com/jobs/1", VacancySource.Manual,
        new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void PublishedAt_SetNonUtc_Throws(DateTimeKind kind)
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.PublishedAt = new DateTime(2026, 11, 12, 10, 0, 0, kind));
  }

  [Fact]
  public void PublishedAt_SetUtc_Succeeds()
  {
    var vacancy = Create();

    vacancy.PublishedAt = Now;

    Assert.Equal(Now, vacancy.PublishedAt);
  }

  [Fact]
  public void ChangeUrl_Valid_NormalizesUrl()
  {
    var vacancy = Create();

    vacancy.ChangeUrl("http://www.example.com/jobs/2/?utm_source=x");

    Assert.Equal("https://example.com/jobs/2", vacancy.Url);
  }

  [Fact]
  public void ChangeUrl_Invalid_ThrowsAndKeepsOldUrl()
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.ChangeUrl("not a url"));
    Assert.Equal("https://example.com/jobs/1", vacancy.Url);
  }

  [Fact]
  public void SetMatch_NewHypothesis_AddsMatchWithAllFields()
  {
    var vacancy = Create();
    var hypothesisId = Guid.NewGuid();

    var match = vacancy.SetMatch(hypothesisId, 80, "fits", MatchAuthor.Human);

    Assert.Same(match, Assert.Single(vacancy.Matches));
    Assert.Equal(vacancy.Id, match.VacancyId);
    Assert.Equal(vacancy.UserId, match.UserId);
    Assert.Equal(hypothesisId, match.HypothesisId);
    Assert.Equal(80, match.Score);
    Assert.Equal("fits", match.Rationale);
    Assert.Equal(MatchAuthor.Human, match.MatchedBy);
  }

  [Fact]
  public void SetMatch_SameHypothesisAgain_UpdatesExistingMatch()
  {
    var vacancy = Create();
    var hypothesisId = Guid.NewGuid();
    var first = vacancy.SetMatch(hypothesisId, 80, "fits", MatchAuthor.Human);

    var second = vacancy.SetMatch(hypothesisId, 60, "meh", MatchAuthor.Ai);

    Assert.Same(first, second);
    Assert.Single(vacancy.Matches);
    Assert.Equal(60, second.Score);
    Assert.Equal("meh", second.Rationale);
    Assert.Equal(MatchAuthor.Ai, second.MatchedBy);
  }

  [Theory]
  [InlineData(null)]
  [InlineData(0)]
  [InlineData(100)]
  public void SetMatch_ValidScore_Succeeds(int? score)
  {
    var vacancy = Create();

    var match = vacancy.SetMatch(Guid.NewGuid(), score, null, MatchAuthor.Human);

    Assert.Equal(score, match.Score);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(101)]
  public void SetMatch_ScoreOutOfRange_Throws(int score)
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.SetMatch(Guid.NewGuid(), score, null, MatchAuthor.Human));
  }

  [Fact]
  public void SetMatch_EmptyHypothesisId_Throws()
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.SetMatch(Guid.Empty, 50, null, MatchAuthor.Human));
  }

  [Fact]
  public void RemoveMatch_ExistingHypothesis_RemovesMatch()
  {
    var vacancy = Create();
    var hypothesisId = Guid.NewGuid();
    vacancy.SetMatch(hypothesisId, 50, null, MatchAuthor.Human);

    vacancy.RemoveMatch(hypothesisId);

    Assert.Empty(vacancy.Matches);
  }
}
