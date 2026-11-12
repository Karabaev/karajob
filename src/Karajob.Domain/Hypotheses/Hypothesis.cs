using Karajob.Domain.Common;

namespace Karajob.Domain.Hypotheses;

public class Hypothesis : IUserOwned
{
  private DateTime? startedAt;

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public string Name { get; set; }
  public string Role { get; set; }
  public List<string> Locations { get; private set; } = [];
  public List<string> Industries { get; private set; } = [];
  public WorkFormat WorkFormat { get; set; }
  public string Language { get; set; }
  public string? Positioning { get; set; }
  public Guid? PrimaryCvDocumentId { get; set; }
  public HypothesisStatus Status { get; set; }
  public DateTime CreatedAt { get; private set; }

  public DateTime? StartedAt
  {
    get => startedAt;
    set => startedAt = UtcGuard.Ensure(value);
  }

  public Hypothesis(Guid userId, string name, string role, string language, DateTime createdAt)
  {
    Id = Guid.CreateVersion7();
    UserId = Require.NotEmpty(userId);
    Name = Require.NotBlank(name);
    Role = Require.NotBlank(role);
    Language = Require.NotBlank(language);
    Status = HypothesisStatus.Draft;
    CreatedAt = UtcGuard.Ensure(createdAt);
  }
}
