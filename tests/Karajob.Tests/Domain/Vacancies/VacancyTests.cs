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
  public void SetMatch_NewTrack_AddsMatchWithAllFields()
  {
    var vacancy = Create();
    var trackId = Guid.NewGuid();

    var match = vacancy.SetMatch(trackId, 80, "fits", MatchAuthor.Human);

    Assert.Same(match, Assert.Single(vacancy.Matches));
    Assert.Equal(vacancy.Id, match.VacancyId);
    Assert.Equal(vacancy.UserId, match.UserId);
    Assert.Equal(trackId, match.TrackId);
    Assert.Equal(80, match.Score);
    Assert.Equal("fits", match.Rationale);
    Assert.Equal(MatchAuthor.Human, match.MatchedBy);
  }

  [Fact]
  public void SetMatch_SameTrackAgain_UpdatesExistingMatch()
  {
    var vacancy = Create();
    var trackId = Guid.NewGuid();
    var first = vacancy.SetMatch(trackId, 80, "fits", MatchAuthor.Human);

    var second = vacancy.SetMatch(trackId, 60, "meh", MatchAuthor.Ai);

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
  public void SetMatch_EmptyTrackId_Throws()
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.SetMatch(Guid.Empty, 50, null, MatchAuthor.Human));
  }

  [Fact]
  public void RemoveMatch_ExistingTrack_RemovesMatch()
  {
    var vacancy = Create();
    var trackId = Guid.NewGuid();
    vacancy.SetMatch(trackId, 50, null, MatchAuthor.Human);

    vacancy.RemoveMatch(trackId);

    Assert.Empty(vacancy.Matches);
  }

  [Fact]
  public void SetPlatforms_NewIds_AddsOneEntryPerIdOwnedByVacancy()
  {
    var vacancy = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    vacancy.SetPlatforms([first, second]);

    Assert.Equal([first, second], vacancy.Platforms.Select(p => p.PlatformId));
    Assert.All(vacancy.Platforms, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(vacancy.UserId, p.UserId);
      Assert.Equal(vacancy.Id, p.VacancyId);
    });
  }

  [Fact]
  public void SetPlatforms_DuplicateIds_KeepsEachOnce()
  {
    var vacancy = Create();
    var id = Guid.NewGuid();

    vacancy.SetPlatforms([id, id, id]);

    Assert.Equal(id, Assert.Single(vacancy.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsMissingFromNewSet_AreRemoved()
  {
    var vacancy = Create();
    var kept = Guid.NewGuid();
    vacancy.SetPlatforms([kept, Guid.NewGuid()]);

    vacancy.SetPlatforms([kept]);

    Assert.Equal(kept, Assert.Single(vacancy.Platforms).PlatformId);
  }

  [Fact]
  public void SetPlatforms_IdsThatStay_KeepSameEntryInstance()
  {
    var vacancy = Create();
    var kept = Guid.NewGuid();
    vacancy.SetPlatforms([kept]);
    var original = Assert.Single(vacancy.Platforms);

    vacancy.SetPlatforms([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(vacancy.Platforms, p => p.PlatformId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, vacancy.Platforms.Count);
  }

  [Fact]
  public void SetPlatforms_Empty_ClearsPlatforms()
  {
    var vacancy = Create();
    vacancy.SetPlatforms([Guid.NewGuid(), Guid.NewGuid()]);

    vacancy.SetPlatforms([]);

    Assert.Empty(vacancy.Platforms);
  }

  [Fact]
  public void SetPlatforms_EmptyGuid_Throws()
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.SetPlatforms([Guid.NewGuid(), Guid.Empty]));
  }

  [Fact]
  public void SetEngines_NewIds_AddsOneEntryPerIdOwnedByVacancy()
  {
    var vacancy = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    vacancy.SetEngines([first, second]);

    Assert.Equal([first, second], vacancy.Engines.Select(p => p.EngineId));
    Assert.All(vacancy.Engines, p =>
    {
      Assert.NotEqual(Guid.Empty, p.Id);
      Assert.Equal(vacancy.UserId, p.UserId);
      Assert.Equal(vacancy.Id, p.VacancyId);
    });
  }

  [Fact]
  public void SetEngines_DuplicateIds_KeepsEachOnce()
  {
    var vacancy = Create();
    var id = Guid.NewGuid();

    vacancy.SetEngines([id, id, id]);

    Assert.Equal(id, Assert.Single(vacancy.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsMissingFromNewSet_AreRemoved()
  {
    var vacancy = Create();
    var kept = Guid.NewGuid();
    vacancy.SetEngines([kept, Guid.NewGuid()]);

    vacancy.SetEngines([kept]);

    Assert.Equal(kept, Assert.Single(vacancy.Engines).EngineId);
  }

  [Fact]
  public void SetEngines_IdsThatStay_KeepSameEntryInstance()
  {
    var vacancy = Create();
    var kept = Guid.NewGuid();
    vacancy.SetEngines([kept]);
    var original = Assert.Single(vacancy.Engines);

    vacancy.SetEngines([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(vacancy.Engines, p => p.EngineId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, vacancy.Engines.Count);
  }

  [Fact]
  public void SetEngines_Empty_ClearsEngines()
  {
    var vacancy = Create();
    vacancy.SetEngines([Guid.NewGuid(), Guid.NewGuid()]);

    vacancy.SetEngines([]);

    Assert.Empty(vacancy.Engines);
  }

  [Fact]
  public void SetEngines_EmptyGuid_Throws()
  {
    var vacancy = Create();

    Assert.ThrowsAny<ArgumentException>(() => vacancy.SetEngines([Guid.NewGuid(), Guid.Empty]));
  }
}
