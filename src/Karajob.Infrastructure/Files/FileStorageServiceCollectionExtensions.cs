using Karajob.Application.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Karajob.Infrastructure.Files;

public static class FileStorageServiceCollectionExtensions
{
  public static IServiceCollection AddKarajobFileStorage(this IServiceCollection services, FileStorageOptions options)
  {
    services.AddSingleton(options);
    services.AddSingleton<IFileStorage, LocalFileStorage>();
    return services;
  }
}
