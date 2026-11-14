using System.Net;
using Karajob.Application.Cvs;
using Karajob.Application.Files;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Tests.Web;

public class CvEndpointTests : IDisposable
{
  private readonly string root = Path.Combine(Path.GetTempPath(), "karajob-web-files-" + Guid.NewGuid().ToString("N"));
  private readonly WebApplicationFactory<Program> factory;

  [Fact]
  public async Task GetVersionFile_Existing_ReturnsBytesContentTypeAndFileName()
  {
    byte[] bytes = [0x25, 0x50, 0x44, 0x46, 9, 8, 7];
    var versionId = await CreateCvAsync("My CV.pdf", bytes);
    var client = factory.CreateClient();

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
    Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    var disposition = response.Content.Headers.ContentDisposition;
    Assert.NotNull(disposition);
    Assert.Equal("My CV.pdf", disposition.FileNameStar ?? disposition.FileName?.Trim('"'));
  }

  [Fact]
  public async Task GetVersionFile_UnknownId_ReturnsNotFound()
  {
    var client = factory.CreateClient();

    var response = await client.GetAsync($"/cvs/versions/{Guid.NewGuid()}/file");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public void Host_ResolvesCvOptionsWithFiveMegabyteLimit()
  {
    var options = factory.Services.GetRequiredService<CvOptions>();

    Assert.Equal(5, options.MaxFileSizeMb);
  }

  [Fact]
  public void Host_ResolvesFileStorage()
  {
    Assert.NotNull(factory.Services.GetService<IFileStorage>());
  }

  public void Dispose()
  {
    factory.Dispose();
    if (Directory.Exists(root))
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private async Task<Guid> CreateCvAsync(string fileName, byte[] bytes)
  {
    using var scope = factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<CvService>();
    var languageId = Karajob.Infrastructure.Persistence.SeedData.EnglishLanguageId;
    var details = await service.CreateAsync(
      new CvInput("Endpoint CV", languageId, []),
      new CvUpload(fileName, bytes.Length, () => new MemoryStream(bytes), null));
    return details.Versions[0].Id;
  }

  public CvEndpointTests()
  {
    factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("Files:RootDirectory", root));
  }
}
