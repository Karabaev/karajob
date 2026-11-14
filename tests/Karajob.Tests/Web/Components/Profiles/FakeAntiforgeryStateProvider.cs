using Microsoft.AspNetCore.Components.Forms;

namespace Karajob.Tests.Web.Components.Profiles;

internal class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
{
  public override AntiforgeryRequestToken? GetAntiforgeryToken()
  {
    return new AntiforgeryRequestToken("test-token", "__RequestVerificationToken");
  }
}
