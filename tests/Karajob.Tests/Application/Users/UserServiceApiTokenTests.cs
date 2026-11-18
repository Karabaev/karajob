using System.Security.Cryptography;
using System.Text;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Users;

public class UserServiceApiTokenTests : UserServiceFixture
{
  [Fact]
  public async Task IssueApiTokenAsync_Valid_ReturnsPrefixedLowercaseHexToken()
  {
    var token = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    Assert.Matches("^kj_[0-9a-f]{64}$", token);
  }

  [Fact]
  public async Task IssueApiTokenAsync_Valid_StoresOnlySha256HashNotPlaintext()
  {
    var token = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    await using var context = Database.CreateContext(SeedData.OwnerId);
    var stored = await context.Users.SingleAsync(u => u.Id == SeedData.OwnerId);
    Assert.NotEqual(token, stored.ApiTokenHash);
    Assert.Equal(Sha256Hex(token), stored.ApiTokenHash);
    Assert.Equal(Now, stored.ApiTokenCreatedAt);
  }

  [Fact]
  public async Task IssueApiTokenAsync_TwiceForSameProfile_ReturnsDifferentTokens()
  {
    var first = await Service.IssueApiTokenAsync(SeedData.OwnerId);
    var second = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    Assert.NotEqual(first, second);
  }

  [Fact]
  public async Task IssueApiTokenAsync_UnknownProfile_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.IssueApiTokenAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task FindByApiTokenAsync_IssuedToken_ReturnsOwningProfile()
  {
    var token = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    var found = await Service.FindByApiTokenAsync(token);

    Assert.NotNull(found);
    Assert.Equal(SeedData.OwnerId, found.Id);
    Assert.Equal("Owner", found.Name);
  }

  [Fact]
  public async Task FindByApiTokenAsync_TwoProfiles_ReturnEachOwnProfile()
  {
    var anna = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));
    var ownerToken = await Service.IssueApiTokenAsync(SeedData.OwnerId);
    var annaToken = await Service.IssueApiTokenAsync(anna.Id);

    Assert.NotEqual(ownerToken, annaToken);
    Assert.Equal(SeedData.OwnerId, (await Service.FindByApiTokenAsync(ownerToken))!.Id);
    Assert.Equal(anna.Id, (await Service.FindByApiTokenAsync(annaToken))!.Id);
  }

  [Fact]
  public async Task FindByApiTokenAsync_AfterRegenerate_OldTokenInvalidAndNewTokenWorks()
  {
    var oldToken = await Service.IssueApiTokenAsync(SeedData.OwnerId);
    var newToken = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    Assert.Null(await Service.FindByApiTokenAsync(oldToken));
    Assert.Equal(SeedData.OwnerId, (await Service.FindByApiTokenAsync(newToken))!.Id);
  }

  [Fact]
  public async Task FindByApiTokenAsync_AfterRevoke_ReturnsNull()
  {
    var token = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    await Service.RevokeApiTokenAsync(SeedData.OwnerId);

    Assert.Null(await Service.FindByApiTokenAsync(token));
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("kj_")]
  [InlineData("kj_unknown")]
  public async Task FindByApiTokenAsync_UnknownOrEmptyToken_ReturnsNull(string token)
  {
    await Service.IssueApiTokenAsync(SeedData.OwnerId);

    Assert.Null(await Service.FindByApiTokenAsync(token));
  }

  [Fact]
  public async Task FindByApiTokenAsync_StoredHashPassedAsToken_ReturnsNull()
  {
    var token = await Service.IssueApiTokenAsync(SeedData.OwnerId);

    Assert.Null(await Service.FindByApiTokenAsync(Sha256Hex(token)));
  }

  [Fact]
  public async Task FindByApiTokenAsync_NoTokenEverIssued_ReturnsNull()
  {
    Assert.Null(await Service.FindByApiTokenAsync("kj_" + new string('a', 64)));
  }

  [Fact]
  public async Task GetApiTokenStatusAsync_NothingIssued_ReportsNotExisting()
  {
    var status = await Service.GetApiTokenStatusAsync(SeedData.OwnerId);

    Assert.False(status.Exists);
    Assert.Null(status.CreatedAt);
  }

  [Fact]
  public async Task GetApiTokenStatusAsync_AfterIssue_ReportsExistsWithCreatedAt()
  {
    await Service.IssueApiTokenAsync(SeedData.OwnerId);

    var status = await Service.GetApiTokenStatusAsync(SeedData.OwnerId);

    Assert.True(status.Exists);
    Assert.Equal(Now, status.CreatedAt);
  }

  [Fact]
  public async Task GetApiTokenStatusAsync_AfterRegenerate_ShowsNewCreatedAt()
  {
    await Service.IssueApiTokenAsync(SeedData.OwnerId);
    Clock.UtcNow = Now.AddDays(3);
    await Service.IssueApiTokenAsync(SeedData.OwnerId);

    var status = await Service.GetApiTokenStatusAsync(SeedData.OwnerId);

    Assert.Equal(Now.AddDays(3), status.CreatedAt);
  }

  [Fact]
  public async Task GetApiTokenStatusAsync_AfterRevoke_ReportsNotExisting()
  {
    await Service.IssueApiTokenAsync(SeedData.OwnerId);

    await Service.RevokeApiTokenAsync(SeedData.OwnerId);

    var status = await Service.GetApiTokenStatusAsync(SeedData.OwnerId);
    Assert.False(status.Exists);
    Assert.Null(status.CreatedAt);
  }

  [Fact]
  public async Task GetApiTokenStatusAsync_OtherProfileIssued_DoesNotAffectThisProfile()
  {
    var anna = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));
    await Service.IssueApiTokenAsync(anna.Id);

    var status = await Service.GetApiTokenStatusAsync(SeedData.OwnerId);

    Assert.False(status.Exists);
  }

  [Fact]
  public async Task RevokeApiTokenAsync_OneProfile_KeepsOtherProfileTokenValid()
  {
    var anna = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));
    var ownerToken = await Service.IssueApiTokenAsync(SeedData.OwnerId);
    var annaToken = await Service.IssueApiTokenAsync(anna.Id);

    await Service.RevokeApiTokenAsync(SeedData.OwnerId);

    Assert.Null(await Service.FindByApiTokenAsync(ownerToken));
    Assert.Equal(anna.Id, (await Service.FindByApiTokenAsync(annaToken))!.Id);
  }

  [Fact]
  public async Task RevokeApiTokenAsync_NothingIssued_DoesNotThrow()
  {
    await Service.RevokeApiTokenAsync(SeedData.OwnerId);

    Assert.False((await Service.GetApiTokenStatusAsync(SeedData.OwnerId)).Exists);
  }

  [Fact]
  public async Task RevokeApiTokenAsync_UnknownProfile_ThrowsKeyNotFound()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.RevokeApiTokenAsync(Guid.NewGuid()));
  }

  private static string Sha256Hex(string value)
  {
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
  }
}
