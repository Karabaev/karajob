using System.Reflection;
using Karajob.Domain.Common;

namespace Karajob.Tests.Architecture;

public class DomainConventionsTests
{
  private static readonly Assembly DomainAssembly = typeof(IUserOwned).Assembly;

  private static readonly string[] SharedReferenceTypes =
    ["User", "Company", "CompanyDomain", "CompanyAlias", "Project", "Role", "Location", "Language"];

  [Fact]
  public void EntitiesWithGuidId_ExceptSharedReferenceData_ImplementIUserOwned()
  {
    var offenders = DomainAssembly.GetExportedTypes()
      .Where(t => t is { IsClass: true, IsAbstract: false })
      .Where(t => t.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(Guid))
      .Where(t => !SharedReferenceTypes.Contains(t.Name))
      .Where(t => !typeof(IUserOwned).IsAssignableFrom(t))
      .Select(t => t.FullName)
      .ToList();

    Assert.True(offenders.Count == 0, "Types missing IUserOwned: " + string.Join(", ", offenders));
  }

  [Fact]
  public void PublicProperties_DoNotUseDateTimeOffsetOrDecimal()
  {
    var forbidden = new[] { typeof(DateTimeOffset), typeof(DateTimeOffset?), typeof(decimal), typeof(decimal?) };

    var offenders = DomainAssembly.GetExportedTypes()
      .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
      .Where(p => forbidden.Contains(p.PropertyType))
      .Select(p => p.DeclaringType!.FullName + "." + p.Name)
      .ToList();

    Assert.True(offenders.Count == 0, "Forbidden property types: " + string.Join(", ", offenders));
  }
}
