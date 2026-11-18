using Karajob.Domain.Users;

namespace Karajob.Tests.Domain.Users;

public class UserApiTokenTests
{
  private static readonly DateTime Now = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);
  private const string Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

  [Fact]
  public void NewUser_HasNoApiToken()
  {
    var user = new User("Anna", "#26a69a", Now);

    Assert.Null(user.ApiTokenHash);
    Assert.Null(user.ApiTokenCreatedAt);
  }

  [Fact]
  public void IssueApiToken_Valid_SetsHashAndCreatedAt()
  {
    var user = new User("Anna", "#26a69a", Now);

    user.IssueApiToken(Hash, Now.AddHours(1));

    Assert.Equal(Hash, user.ApiTokenHash);
    Assert.Equal(Now.AddHours(1), user.ApiTokenCreatedAt);
  }

  [Fact]
  public void IssueApiToken_WhenAlreadyIssued_ReplacesHashAndCreatedAt()
  {
    var user = new User("Anna", "#26a69a", Now);
    user.IssueApiToken(Hash, Now);

    user.IssueApiToken("abc123", Now.AddDays(1));

    Assert.Equal("abc123", user.ApiTokenHash);
    Assert.Equal(Now.AddDays(1), user.ApiTokenCreatedAt);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void IssueApiToken_BlankHash_ThrowsAndKeepsState(string hash)
  {
    var user = new User("Anna", "#26a69a", Now);

    Assert.ThrowsAny<ArgumentException>(() => user.IssueApiToken(hash, Now));

    Assert.Null(user.ApiTokenHash);
    Assert.Null(user.ApiTokenCreatedAt);
  }

  [Theory]
  [InlineData(DateTimeKind.Local)]
  [InlineData(DateTimeKind.Unspecified)]
  public void IssueApiToken_NonUtcCreatedAt_Throws(DateTimeKind kind)
  {
    var user = new User("Anna", "#26a69a", Now);

    Assert.ThrowsAny<ArgumentException>(() => user.IssueApiToken(Hash, new DateTime(2026, 11, 12, 10, 0, 0, kind)));
  }

  [Fact]
  public void RevokeApiToken_Issued_ClearsHashAndCreatedAt()
  {
    var user = new User("Anna", "#26a69a", Now);
    user.IssueApiToken(Hash, Now);

    user.RevokeApiToken();

    Assert.Null(user.ApiTokenHash);
    Assert.Null(user.ApiTokenCreatedAt);
  }

  [Fact]
  public void RevokeApiToken_NothingIssued_DoesNotThrow()
  {
    var user = new User("Anna", "#26a69a", Now);

    user.RevokeApiToken();

    Assert.Null(user.ApiTokenHash);
  }
}
