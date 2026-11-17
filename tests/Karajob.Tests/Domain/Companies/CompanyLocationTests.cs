using Karajob.Domain.Companies;

namespace Karajob.Tests.Domain.Companies;

public class CompanyLocationTests
{
  [Fact]
  public void SetLocations_NewIds_AddsOneEntryPerIdOwnedByCompany()
  {
    var company = Create();
    var first = Guid.NewGuid();
    var second = Guid.NewGuid();

    company.SetLocations([first, second]);

    Assert.Equal([first, second], company.Locations.Select(l => l.LocationId));
    Assert.All(company.Locations, l =>
    {
      Assert.NotEqual(Guid.Empty, l.Id);
      Assert.Equal(company.Id, l.CompanyId);
    });
  }

  [Fact]
  public void SetLocations_DuplicateIds_KeepsEachOnce()
  {
    var company = Create();
    var id = Guid.NewGuid();

    company.SetLocations([id, id, id]);

    Assert.Equal(id, Assert.Single(company.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsMissingFromNewSet_AreRemoved()
  {
    var company = Create();
    var kept = Guid.NewGuid();
    company.SetLocations([kept, Guid.NewGuid()]);

    company.SetLocations([kept]);

    Assert.Equal(kept, Assert.Single(company.Locations).LocationId);
  }

  [Fact]
  public void SetLocations_IdsThatStay_KeepSameEntryInstance()
  {
    var company = Create();
    var kept = Guid.NewGuid();
    company.SetLocations([kept]);
    var original = Assert.Single(company.Locations);

    company.SetLocations([kept, Guid.NewGuid()]);

    var stayed = Assert.Single(company.Locations, l => l.LocationId == kept);
    Assert.Same(original, stayed);
    Assert.Equal(2, company.Locations.Count);
  }

  [Fact]
  public void SetLocations_Empty_ClearsLocations()
  {
    var company = Create();
    company.SetLocations([Guid.NewGuid(), Guid.NewGuid()]);

    company.SetLocations([]);

    Assert.Empty(company.Locations);
  }

  [Fact]
  public void SetLocations_EmptyGuid_Throws()
  {
    var company = Create();

    Assert.ThrowsAny<ArgumentException>(() => company.SetLocations([Guid.NewGuid(), Guid.Empty]));
  }

  private static Company Create()
  {
    return new Company("pixel-forge", "Pixel Forge");
  }
}
