using Karajob.Application.Common;
using Karajob.Application.Users;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Application.Users;

public class UserServiceTests : UserServiceFixture
{
  [Fact]
  public async Task ListAsync_FreshDatabase_ReturnsOnlySeededOwner()
  {
    var profiles = await Service.ListAsync();

    var owner = Assert.Single(profiles);
    Assert.Equal(SeedData.OwnerId, owner.Id);
    Assert.Equal("Owner", owner.Name);
    Assert.Equal(SeedData.OwnerColor, owner.Color);
  }

  [Fact]
  public async Task ListAsync_WithCreatedProfiles_OrdersByCreatedAtAscending()
  {
    Clock.UtcNow = Now.AddHours(2);
    await Service.CreateAsync(new UserInput("Late", "#26a69a"));
    Clock.UtcNow = Now.AddHours(1);
    await Service.CreateAsync(new UserInput("Early", "#ef5350"));

    var profiles = await Service.ListAsync();

    Assert.Equal(["Owner", "Early", "Late"], profiles.Select(p => p.Name));
  }

  [Fact]
  public async Task CreateAsync_Valid_PersistsTrimmedNameNormalizedColorAndCreatedAt()
  {
    var dto = await Service.CreateAsync(new UserInput("  Anna ", "#26A69A"));

    Assert.Equal("Anna", dto.Name);
    Assert.Equal("#26a69a", dto.Color);
    await using var context = Database.CreateContext(SeedData.OwnerId);
    var stored = await context.Users.SingleAsync(u => u.Id == dto.Id);
    Assert.Equal("Anna", stored.Name);
    Assert.Equal("#26a69a", stored.Color);
    Assert.Equal(Now, stored.CreatedAt);
  }

  [Fact]
  public async Task CreateAsync_Valid_ReturnsDtoMatchingListedProfile()
  {
    var dto = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var listed = (await Service.ListAsync()).Single(p => p.Id == dto.Id);
    Assert.Equal(dto, listed);
  }

  [Fact]
  public async Task CreateAsync_DuplicateName_IsAllowed()
  {
    await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var second = await Service.CreateAsync(new UserInput("Anna", "#ef5350"));

    Assert.Equal(3, (await Service.ListAsync()).Count);
    Assert.Equal("Anna", second.Name);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public async Task CreateAsync_BlankName_ThrowsValidationFailedOnNameAndSavesNothing(string name)
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(new UserInput(name, "#26a69a")));

    Assert.Equal([nameof(UserInput.Name)], ex.Errors.Keys);
    Assert.Single(await Service.ListAsync());
  }

  [Theory]
  [InlineData("")]
  [InlineData("red")]
  [InlineData("#abc")]
  [InlineData("26a69a")]
  public async Task CreateAsync_InvalidColor_ThrowsValidationFailedOnColorAndSavesNothing(string color)
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(new UserInput("Anna", color)));

    Assert.Equal([nameof(UserInput.Color)], ex.Errors.Keys);
    Assert.Single(await Service.ListAsync());
  }

  [Fact]
  public async Task CreateAsync_BlankNameAndInvalidColor_ReportsBothErrors()
  {
    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.CreateAsync(new UserInput(" ", "red")));

    Assert.Contains(nameof(UserInput.Name), ex.Errors.Keys);
    Assert.Contains(nameof(UserInput.Color), ex.Errors.Keys);
  }

  [Fact]
  public async Task UpdateAsync_Valid_ChangesNameAndColor()
  {
    var created = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var updated = await Service.UpdateAsync(created.Id, new UserInput(" Maria ", "#EF5350"));

    Assert.Equal(created.Id, updated.Id);
    Assert.Equal("Maria", updated.Name);
    Assert.Equal("#ef5350", updated.Color);
    Assert.Equal(updated, await Service.GetAsync(created.Id));
  }

  [Fact]
  public async Task UpdateAsync_Valid_DoesNotChangeCreatedAt()
  {
    var created = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));
    Clock.UtcNow = Now.AddDays(3);

    await Service.UpdateAsync(created.Id, new UserInput("Maria", "#ef5350"));

    await using var context = Database.CreateContext(SeedData.OwnerId);
    Assert.Equal(Now, (await context.Users.SingleAsync(u => u.Id == created.Id)).CreatedAt);
  }

  [Fact]
  public async Task UpdateAsync_UnknownId_ThrowsKeyNotFoundException()
  {
    await Assert.ThrowsAsync<KeyNotFoundException>(() => Service.UpdateAsync(Guid.NewGuid(), new UserInput("Anna", "#26a69a")));
  }

  [Fact]
  public async Task UpdateAsync_BlankNameAndInvalidColor_ReportsBothErrorsAndKeepsStoredValues()
  {
    var created = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, new UserInput("", "red")));

    Assert.Contains(nameof(UserInput.Name), ex.Errors.Keys);
    Assert.Contains(nameof(UserInput.Color), ex.Errors.Keys);
    Assert.Equal(created, await Service.GetAsync(created.Id));
  }

  [Fact]
  public async Task UpdateAsync_BlankName_ThrowsValidationFailedOnNameOnly()
  {
    var created = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, new UserInput("  ", "#ef5350")));

    Assert.Equal([nameof(UserInput.Name)], ex.Errors.Keys);
    Assert.Equal(created, await Service.GetAsync(created.Id));
  }

  [Fact]
  public async Task UpdateAsync_InvalidColor_ThrowsValidationFailedOnColorOnly()
  {
    var created = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));

    var ex = await Assert.ThrowsAsync<ValidationFailedException>(() => Service.UpdateAsync(created.Id, new UserInput("Maria", "#12")));

    Assert.Equal([nameof(UserInput.Color)], ex.Errors.Keys);
    Assert.Equal(created, await Service.GetAsync(created.Id));
  }

  [Fact]
  public async Task GetAsync_Existing_ReturnsDto()
  {
    var dto = await Service.GetAsync(SeedData.OwnerId);

    Assert.Equal(new UserDto(SeedData.OwnerId, "Owner", SeedData.OwnerColor), dto);
  }

  [Fact]
  public async Task GetAsync_UnknownId_ReturnsNull()
  {
    Assert.Null(await Service.GetAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task ExistsAsync_Owner_ReturnsTrue()
  {
    Assert.True(await Service.ExistsAsync(SeedData.OwnerId));
  }

  [Fact]
  public async Task ExistsAsync_UnknownId_ReturnsFalse()
  {
    Assert.False(await Service.ExistsAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task ListAsync_ServiceBuiltForOtherUser_StillListsEveryProfile()
  {
    var anna = await Service.CreateAsync(new UserInput("Anna", "#26a69a"));
    var annaService = TestDbContextFactory.CreateUserService(Database, anna.Id, Clock);

    var profiles = await annaService.ListAsync();

    Assert.Equal(2, profiles.Count);
    Assert.Contains(profiles, p => p.Id == SeedData.OwnerId);
    Assert.Contains(profiles, p => p.Id == anna.Id);
  }
}
