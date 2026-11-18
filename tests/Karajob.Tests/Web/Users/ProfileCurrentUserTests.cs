using Karajob.Application.Users;
using Karajob.Web.Users;

namespace Karajob.Tests.Web.Users;

public class ProfileCurrentUserTests
{
  [Fact]
  public void NotifyProfileUpdated_RaisesProfileUpdatedOnce()
  {
    var currentUser = new ProfileCurrentUser();
    var raised = 0;
    currentUser.ProfileUpdated += () => raised++;

    currentUser.NotifyProfileUpdated(new UserDto(Guid.NewGuid(), "Anna", "#26a69a", "Asia/Tokyo"));

    Assert.Equal(1, raised);
  }

  [Fact]
  public void NotifyProfileUpdated_UpdatesTimeZoneToProfileZone()
  {
    var currentUser = new ProfileCurrentUser();

    currentUser.NotifyProfileUpdated(new UserDto(Guid.NewGuid(), "Anna", "#26a69a", "Asia/Tokyo"));

    Assert.Equal(TimeSpan.FromHours(9), currentUser.TimeZone.BaseUtcOffset);
  }

  [Fact]
  public void NotifyProfileUpdated_UpdatesTimeZoneBeforeRaisingEvent()
  {
    var currentUser = new ProfileCurrentUser();
    TimeSpan? seen = null;
    currentUser.ProfileUpdated += () => seen = currentUser.TimeZone.BaseUtcOffset;

    currentUser.NotifyProfileUpdated(new UserDto(Guid.NewGuid(), "Anna", "#26a69a", "Asia/Tokyo"));

    Assert.Equal(TimeSpan.FromHours(9), seen);
  }

  [Fact]
  public void NotifyProfileUpdated_WithoutSubscribers_DoesNotThrow()
  {
    var currentUser = new ProfileCurrentUser();

    var exception = Record.Exception(() =>
      currentUser.NotifyProfileUpdated(new UserDto(Guid.NewGuid(), "Anna", "#26a69a", "Asia/Tokyo")));

    Assert.Null(exception);
  }
}
