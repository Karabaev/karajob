using Karajob.Domain.Applications;
using Karajob.Domain.Companies;
using Karajob.Domain.Cvs;
using Karajob.Domain.Tracks;
using Karajob.Domain.Users;
using Karajob.Domain.Vacancies;
using Karajob.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karajob.Tests.Infrastructure.Persistence;

public class DeleteBehaviorTests
{
  public static TheoryData<Type, Type, DeleteBehavior> Relationships => new()
  {
    { typeof(Vacancy), typeof(VacancyMatch), DeleteBehavior.Cascade },
    { typeof(Vacancy), typeof(ApplicationEvent), DeleteBehavior.Cascade },
    { typeof(Vacancy), typeof(JobApplication), DeleteBehavior.Cascade },
    { typeof(Company), typeof(CompanyDomain), DeleteBehavior.Cascade },
    { typeof(Company), typeof(CompanyAlias), DeleteBehavior.Cascade },
    { typeof(CvDocument), typeof(CvVersion), DeleteBehavior.Cascade },
    { typeof(CvDocument), typeof(CvDocumentTag), DeleteBehavior.Cascade },
    { typeof(Tag), typeof(CvDocumentTag), DeleteBehavior.Cascade },
    { typeof(User), typeof(Tag), DeleteBehavior.Restrict },
    { typeof(Company), typeof(Vacancy), DeleteBehavior.Restrict },
    { typeof(Company), typeof(Project), DeleteBehavior.Restrict },
    { typeof(Company), typeof(UserCompanyNote), DeleteBehavior.Restrict },
    { typeof(Track), typeof(VacancyMatch), DeleteBehavior.Restrict },
    { typeof(Track), typeof(JobApplication), DeleteBehavior.Restrict },
    { typeof(CvVersion), typeof(JobApplication), DeleteBehavior.Restrict },
    { typeof(CvDocument), typeof(Track), DeleteBehavior.Restrict },
    { typeof(User), typeof(Vacancy), DeleteBehavior.Restrict },
    { typeof(User), typeof(Track), DeleteBehavior.Restrict },
    { typeof(User), typeof(CvDocument), DeleteBehavior.Restrict },
    { typeof(User), typeof(ApplicationEvent), DeleteBehavior.Restrict },
  };

  [Theory]
  [MemberData(nameof(Relationships))]
  public async Task Model_Relationship_HasExpectedDeleteBehavior(Type principal, Type dependent, DeleteBehavior expected)
  {
    await using var database = await SqliteTestDatabase.CreateAsync();
    await using var context = database.CreateContext(SeedData.OwnerId);

    var foreignKey = Assert.Single(
      context.Model.FindEntityType(dependent)!.GetForeignKeys(),
      fk => fk.PrincipalEntityType.ClrType == principal);

    Assert.Equal(expected, foreignKey.DeleteBehavior);
  }
}
