using Karajob.Domain.Hypotheses;

namespace Karajob.Tests.Domain.Hypotheses;

public class HypothesisTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

  private static Hypothesis Create() => new(Guid.NewGuid(), "Gamedev EU", "Unity Developer", "en", Now);

  [Fact]
  public void Ctor_Valid_SetsFieldsAndDefaults()
  {
    var userId = Guid.NewGuid();

    var hypothesis = new Hypothesis(userId, "Gamedev EU", "Unity Developer", "en", Now);

    Assert.NotEqual(Guid.Empty, hypothesis.Id);
    Assert.Equal(userId, hypothesis.UserId);
    Assert.Equal("Gamedev EU", hypothesis.Name);
    Assert.Equal("Unity Developer", hypothesis.Role);
    Assert.Equal("en", hypothesis.Language);
    Assert.Equal(Now, hypothesis.CreatedAt);
    Assert.Equal(HypothesisStatus.Draft, hypothesis.Status);
    Assert.Empty(hypothesis.Locations);
    Assert.Empty(hypothesis.Industries);
    Assert.Equal(WorkFormat.None, hypothesis.WorkFormat);
    Assert.Null(hypothesis.StartedAt);
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.Empty, "n", "r", "en", Now));
  }

  [Theory]
  [InlineData("", "r", "en")]
  [InlineData("   ", "r", "en")]
  [InlineData("n", "", "en")]
  [InlineData("n", "   ", "en")]
  [InlineData("n", "r", "")]
  [InlineData("n", "r", "   ")]
  public void Ctor_BlankTextField_Throws(string name, string role, string language)
  {
    Assert.ThrowsAny<ArgumentException>(() => new Hypothesis(Guid.NewGuid(), name, role, language, Now));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void Ctor_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    Assert.ThrowsAny<ArgumentException>(() =>
      new Hypothesis(Guid.NewGuid(), "n", "r", "en", new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void StartedAt_SetNonUtc_Throws(DateTimeKind kind)
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() => hypothesis.StartedAt = new DateTime(2026, 11, 12, 10, 0, 0, kind));
  }

  [Fact]
  public void StartedAt_SetUtc_Succeeds()
  {
    var hypothesis = Create();

    hypothesis.StartedAt = Now;

    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void StartedAt_SetNull_Succeeds()
  {
    var hypothesis = Create();
    hypothesis.StartedAt = Now;

    hypothesis.StartedAt = null;

    Assert.Null(hypothesis.StartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Active)]
  [InlineData(HypothesisStatus.Expanded)]
  public void ChangeStatus_ToActiveOrExpandedWithoutStart_SetsStartedAt(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Equal(status, hypothesis.Status);
    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Theory]
  [InlineData(HypothesisStatus.Draft)]
  [InlineData(HypothesisStatus.Paused)]
  [InlineData(HypothesisStatus.Disabled)]
  public void ChangeStatus_ToNonRunningStatus_LeavesStartedAtNull(HypothesisStatus status)
  {
    var hypothesis = Create();

    hypothesis.ChangeStatus(status, Now);

    Assert.Equal(status, hypothesis.Status);
    Assert.Null(hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ReactivateAfterPause_KeepsOriginalStartedAt()
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);
    hypothesis.ChangeStatus(HypothesisStatus.Paused, Now.AddDays(1));

    hypothesis.ChangeStatus(HypothesisStatus.Active, Now.AddDays(2));

    Assert.Equal(HypothesisStatus.Active, hypothesis.Status);
    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveToExpanded_KeepsStartedAt()
  {
    var hypothesis = Create();
    hypothesis.ChangeStatus(HypothesisStatus.Active, Now);

    hypothesis.ChangeStatus(HypothesisStatus.Expanded, Now.AddDays(3));

    Assert.Equal(Now, hypothesis.StartedAt);
  }

  [Fact]
  public void ChangeStatus_ActiveWithNonUtcNow_Throws()
  {
    var hypothesis = Create();

    Assert.ThrowsAny<ArgumentException>(() =>
      hypothesis.ChangeStatus(HypothesisStatus.Active, new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Local)));
  }
}
