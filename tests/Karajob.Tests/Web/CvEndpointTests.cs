using System.Net;
using System.Security.Claims;
using Karajob.Application.Cvs;
using Karajob.Application.Files;
using Karajob.Application.Users;
using Karajob.Domain.Cvs;
using Karajob.Domain.Users;
using Karajob.Infrastructure.Persistence;
using Karajob.Web.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

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
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{Guid.NewGuid()}/file");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetVersionFile_OtherUsersVersion_ReturnsNotFound()
  {
    using var scope = factory.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var other = new User("Other", "#26a69a", DateTime.UtcNow);
    var document = new CvDocument(other.Id, "Other CV", SeedData.EnglishLanguageId, DateTime.UtcNow);
    var version = document.AddVersion("other.pdf", "cv/other/other.pdf", "application/pdf", 3, null, DateTime.UtcNow);
    context.Users.Add(other);
    context.CvDocuments.Add(document);
    await context.SaveChangesAsync();
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{version.Id}/file");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetVersionFile_StoredFileMissing_ReturnsNotFound()
  {
    var versionId = await CreateCvAsync("Lost.pdf", [1, 2, 3]);
    Directory.Delete(root, recursive: true);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetVersionFile_PdfInline_ReturnsInlineDispositionWithBytes()
  {
    byte[] bytes = [0x25, 0x50, 0x44, 0x46, 1, 2, 3];
    var versionId = await CreateCvAsync("Inline.pdf", bytes);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    Assert.Equal("inline", response.Content.Headers.ContentDisposition?.DispositionType);
    Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
  }

  [Fact]
  public async Task GetVersionFile_PdfInlineWithLongCyrillicName_ReturnsInlineWithEncodedFileName()
  {
    const string fileName = "Резюме Иван Иванович Иванов Senior Unity Developer 2026 final version.pdf";
    var versionId = await CreateCvAsync(fileName, [1, 2, 3]);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var disposition = response.Content.Headers.ContentDisposition;
    Assert.NotNull(disposition);
    Assert.Equal("inline", disposition.DispositionType);
    Assert.Equal(fileName, disposition.FileNameStar);
  }

  [Fact]
  public async Task GetVersionFile_PdfInline_DoesNotBlockSameOriginFraming()
  {
    var versionId = await CreateCvAsync("Frame.pdf", [1, 2, 3]);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.False(response.Headers.Contains("X-Frame-Options"));
    if (response.Headers.TryGetValues("Content-Security-Policy", out var policies))
    {
      Assert.DoesNotContain(policies, p => p.Contains("frame-ancestors", StringComparison.OrdinalIgnoreCase));
    }
  }

  [Fact]
  public async Task GetVersionFile_PdfInline_SendsNosniffHeader()
  {
    var versionId = await CreateCvAsync("Sniff.pdf", [1, 2, 3]);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

    Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
  }

  [Theory]
  [InlineData("")]
  [InlineData("?inline=false")]
  public async Task GetVersionFile_PdfWithoutInlineFlag_ReturnsAttachmentWithFileName(string query)
  {
    var versionId = await CreateCvAsync("Plain.pdf", [1, 2, 3]);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file{query}");

    var disposition = response.Content.Headers.ContentDisposition;
    Assert.NotNull(disposition);
    Assert.Equal("attachment", disposition.DispositionType);
    Assert.Equal("Plain.pdf", disposition.FileNameStar ?? disposition.FileName?.Trim('"'));
  }

  [Fact]
  public async Task GetVersionFile_DocxInline_StillReturnsAttachment()
  {
    var versionId = await CreateCvAsync("Resume.docx", [1, 2, 3]);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
  }

  [Fact]
  public async Task GetVersionFile_InlineUnknownId_ReturnsNotFound()
  {
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{Guid.NewGuid()}/file?inline=true");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetVersionFile_InlineOtherUsersVersion_ReturnsNotFound()
  {
    using var scope = factory.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KarajobDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var other = new User("Other", "#26a69a", DateTime.UtcNow);
    var document = new CvDocument(other.Id, "Other CV", SeedData.EnglishLanguageId, DateTime.UtcNow);
    var version = document.AddVersion("other.pdf", "cv/other/other.pdf", "application/pdf", 3, null, DateTime.UtcNow);
    context.Users.Add(other);
    context.CvDocuments.Add(document);
    await context.SaveChangesAsync();
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{version.Id}/file?inline=true");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetVersionFile_InlineStoredFileMissing_ReturnsNotFound()
  {
    var versionId = await CreateCvAsync("Gone.pdf", [1, 2, 3]);
    Directory.Delete(root, recursive: true);
    var client = await ProfileClient.AuthenticatedClientAsync(factory, SeedData.OwnerId);

    var response = await client.GetAsync($"/cvs/versions/{versionId}/file?inline=true");

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
    var currentUser = Assert.IsType<ProfileCurrentUser>(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    currentUser.SetUser(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, SeedData.OwnerId.ToString())], "test")));
    var service = scope.ServiceProvider.GetRequiredService<CvService>();
    var languageId = SeedData.EnglishLanguageId;
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
