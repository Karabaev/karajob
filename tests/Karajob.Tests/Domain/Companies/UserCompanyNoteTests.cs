using Karajob.Domain.Companies;

namespace Karajob.Tests.Domain.Companies;

public class UserCompanyNoteTests
{
  [Fact]
  public void Ctor_EmptyUserId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new UserCompanyNote(Guid.Empty, Guid.NewGuid()));
  }

  [Fact]
  public void Ctor_EmptyCompanyId_Throws()
  {
    Assert.ThrowsAny<ArgumentException>(() => new UserCompanyNote(Guid.NewGuid(), Guid.Empty));
  }

  [Fact]
  public void Ctor_Valid_SetsIdsAndDefaults()
  {
    var userId = Guid.NewGuid();
    var companyId = Guid.NewGuid();

    var note = new UserCompanyNote(userId, companyId);

    Assert.Equal(userId, note.UserId);
    Assert.Equal(companyId, note.CompanyId);
    Assert.NotEqual(Guid.Empty, note.Id);
    Assert.False(note.IsBlacklisted);
  }
}
