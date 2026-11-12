using Microsoft.AspNetCore.Mvc.Testing;

namespace Karajob.Tests.Web;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  [Fact]
  public async Task GetHome_ReturnsSuccessAndHomeTitle()
  {
    var client = factory.CreateClient();

    var response = await client.GetAsync("/");

    response.EnsureSuccessStatusCode();
    var html = await response.Content.ReadAsStringAsync();
    Assert.Contains("<title>Home</title>", html);
  }
}
