using Karajob.Domain.MasterProfiles;

namespace Karajob.Tests.Domain.MasterProfiles;

public class MasterProfileTests
{
  private static readonly DateTime Now = new(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc);

  private static MasterProfile Create(string markdown = "# Profile") => new(Guid.NewGuid(), markdown, Now);

  [Fact]
  public void Ctor_Valid_CreatesSingleVersionWithMarkdown()
  {
    var profile = Create("# Profile");

    var version = Assert.Single(profile.Versions);
    Assert.Equal("# Profile", version.Markdown);
    Assert.Same(version, profile.CurrentVersion);
  }

  [Fact]
  public void Ctor_Valid_FirstVersionHasNoCommentAndNoRestoreSource()
  {
    var profile = Create();

    var version = profile.CurrentVersion;
    Assert.Null(version.Comment);
    Assert.Null(version.RestoredFromVersionId);
  }

  [Fact]
  public void Ctor_Valid_FirstVersionBelongsToProfileAndUser()
  {
    var userId = Guid.NewGuid();

    var profile = new MasterProfile(userId, "# Profile", Now);

    Assert.Equal(userId, profile.UserId);
    Assert.Equal(userId, profile.CurrentVersion.UserId);
    Assert.Equal(profile.Id, profile.CurrentVersion.MasterProfileId);
    Assert.Equal(Now, profile.CurrentVersion.CreatedAt);
    Assert.Equal(Now, profile.CreatedAt);
  }

  [Fact]
  public void Ctor_MarkdownWithSurroundingWhitespace_StoresVerbatim()
  {
    const string markdown = "\n\n  # Profile\n\ntext  \n";

    var profile = Create(markdown);

    Assert.Equal(markdown, profile.CurrentVersion.Markdown);
  }

  [Fact]
  public void Ctor_CrlfMarkdown_StoresLfLineEndings()
  {
    var profile = Create("# Profile\r\n\r\ntext\r\n");

    Assert.Equal("# Profile\n\ntext\n", profile.CurrentVersion.Markdown);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("\n\t ")]
  public void Ctor_BlankMarkdown_Throws(string markdown)
  {
    Assert.ThrowsAny<ArgumentException>(() => Create(markdown));
  }

  [Fact]
  public void Ctor_NonUtcCreatedAt_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new MasterProfile(Guid.NewGuid(), "# Profile", DateTime.SpecifyKind(Now, DateTimeKind.Local)));
  }

  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new MasterProfile(Guid.Empty, "# Profile", Now));
  }

  [Fact]
  public void AddVersion_Valid_AppendsAndBecomesCurrent()
  {
    var profile = Create("first");

    var added = profile.AddVersion("second", null, Now.AddHours(1));

    Assert.Equal(2, profile.Versions.Count);
    Assert.Same(added, profile.CurrentVersion);
    Assert.Equal("second", added.Markdown);
    Assert.Equal(profile.Id, added.MasterProfileId);
    Assert.Equal(profile.UserId, added.UserId);
    Assert.Equal(Now.AddHours(1), added.CreatedAt);
  }

  [Fact]
  public void AddVersion_MarkdownWithSurroundingWhitespace_StoresVerbatim()
  {
    var profile = Create();

    var added = profile.AddVersion("\n  text \n\n", null, Now.AddHours(1));

    Assert.Equal("\n  text \n\n", added.Markdown);
  }

  [Fact]
  public void AddVersion_CommentWithWhitespace_Trims()
  {
    var profile = Create();

    var added = profile.AddVersion("second", "  tweaked  ", Now.AddHours(1));

    Assert.Equal("tweaked", added.Comment);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void AddVersion_BlankComment_StoresNull(string? comment)
  {
    var profile = Create();

    var added = profile.AddVersion("second", comment, Now.AddHours(1));

    Assert.Null(added.Comment);
  }

  [Theory]
  [InlineData("")]
  [InlineData("  \n ")]
  public void AddVersion_BlankMarkdown_ThrowsAndAddsNothing(string markdown)
  {
    var profile = Create();

    Assert.ThrowsAny<ArgumentException>(() => profile.AddVersion(markdown, null, Now.AddHours(1)));

    Assert.Single(profile.Versions);
  }

  [Fact]
  public void AddVersion_NonUtcCreatedAt_Throws()
  {
    var profile = Create();

    Assert.ThrowsAny<ArgumentException>(
      () => profile.AddVersion("second", null, DateTime.SpecifyKind(Now.AddHours(1), DateTimeKind.Unspecified)));
  }

  [Fact]
  public void CurrentVersion_VersionsAddedOutOfOrder_IsLatestCreatedAt()
  {
    var profile = Create("first");
    var latest = profile.AddVersion("latest", null, Now.AddDays(2));
    profile.AddVersion("backdated", null, Now.AddDays(1));

    Assert.Same(latest, profile.CurrentVersion);
  }

  [Fact]
  public void CurrentVersion_SameCreatedAt_IsLaterCreatedVersion()
  {
    var profile = Create("first");
    var added = profile.AddVersion("second", null, Now);

    Assert.Same(added, profile.CurrentVersion);
  }

  [Fact]
  public void AddVersion_RestoredFromOwnVersion_StoresSourceId()
  {
    var profile = Create("first");
    var first = profile.CurrentVersion;
    profile.AddVersion("second", null, Now.AddHours(1));

    var restored = profile.AddVersion(first.Markdown, null, Now.AddHours(2), first.Id);

    Assert.Equal(first.Id, restored.RestoredFromVersionId);
    Assert.Same(restored, profile.CurrentVersion);
  }

  [Fact]
  public void AddVersion_RestoredFromForeignVersion_ThrowsKeyNotFoundAndAddsNothing()
  {
    var profile = Create("first");

    Assert.Throws<KeyNotFoundException>(() => profile.AddVersion("second", null, Now.AddHours(1), Guid.NewGuid()));

    Assert.Single(profile.Versions);
  }

  [Fact]
  public void AddVersion_RestoredFromVersionOfAnotherProfile_ThrowsKeyNotFound()
  {
    var other = Create("other");
    var profile = Create("first");

    Assert.Throws<KeyNotFoundException>(
      () => profile.AddVersion("second", null, Now.AddHours(1), other.CurrentVersion.Id));
  }
}
