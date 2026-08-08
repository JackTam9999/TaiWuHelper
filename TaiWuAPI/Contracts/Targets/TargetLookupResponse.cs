using TaiWu.Application.Targets;

namespace TaiWuAPI.Contracts.Targets;

public sealed record TargetLookupResponse(
    string Query,
    TargetLookupStatus Status,
    int TotalMatches,
    DateTimeOffset CapturedAtUtc,
    string? GameDataVersion,
    IReadOnlyList<TargetLookupMatchResponse> Matches,
    IReadOnlyList<TargetLookupWarningResponse> Warnings);

public sealed record TargetLookupMatchResponse(
    string Reference,
    int CharacterId,
    string DisplayName,
    int Age,
    TargetLookupKind Kind,
    int? TemplateId,
    TargetLocationResponse Location);

public sealed record TargetLocationResponse(
    string Reference,
    int AreaId,
    int BlockId,
    string? DisplayName);

public sealed record TargetLookupWarningResponse(
    string Reference,
    string Code,
    string Message);
