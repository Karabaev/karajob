using Karajob.Domain.Companies;

namespace Karajob.Application.Companies;

public record CompanyInput(
  string Slug,
  string Name,
  string? Website,
  string? Description,
  IReadOnlyList<string> Domains,
  IReadOnlyList<string> Aliases,
  IReadOnlyList<Guid> LocationIds);

public record CompanyListItem(
  Guid Id,
  string Slug,
  string Name,
  string? IconUrl,
  IReadOnlyList<string> Domains,
  bool IsBlacklisted,
  IReadOnlyList<string> LocationNames);

public record CompanyDetails(
  Guid Id,
  string Slug,
  string Name,
  string? Website,
  string? Description,
  string? IconUrl,
  IReadOnlyList<string> Domains,
  IReadOnlyList<string> Aliases,
  IReadOnlyList<Guid> LocationIds,
  IReadOnlyList<ProjectItem> Projects,
  string? Note,
  bool IsBlacklisted);

public record ProjectInput(string Name, string? Genre, Guid? EngineId, ProjectStatus Status, string? Url);

public record ProjectItem(Guid Id, string Name, string? Genre, Guid? EngineId, string? EngineName, ProjectStatus Status, string? Url);
