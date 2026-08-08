using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Application.CombatSkills;

public enum CombatSkillPageSourceReadStatus
{
    NotRead = 0,
    Available = 1,
    SaveMissing = 2,
    SaveReadFailed = 3,
    UnsupportedVersion = 4
}

public enum CombatSkillPageSourceKind
{
    CharacterKnowledge = 0,
    InventoryBook = 1
}

public enum CombatSkillPageSourceAvailability
{
    Locatable = 0,
    TaiwuInventory = 1,
    Unlocated = 2
}

public sealed record CombatSkillPageSourceReadRequest
{
    private static readonly HashSet<string> SupportedDetailIds =
        Enumerable.Range(0, 5)
            .SelectMany(index => new[]
            {
                $"outline-{index}",
                $"direct-{index}",
                $"reverse-{index}"
            })
            .ToHashSet(StringComparer.Ordinal);

    public CombatSkillPageSourceReadRequest(
        int skillId,
        IEnumerable<string> detailIds,
        CatalogueLanguage preferredLanguage =
            CatalogueLanguage.TraditionalChinese)
    {
        if (skillId is < 0 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A source-search skill ID must fit the installed GameData ID.");
        }

        ArgumentNullException.ThrowIfNull(detailIds);
        var details = detailIds
            .Select(value => value?.Trim())
            .ToImmutableArray();
        if (details.Length is < 1 or > 15
            || details.Any(value => string.IsNullOrWhiteSpace(value)
                || !SupportedDetailIds.Contains(value!))
            || details.Distinct(StringComparer.Ordinal).Count()
                != details.Length)
        {
            throw new ArgumentException(
                "Source search requires one to fifteen distinct supported "
                + "study-detail IDs.",
                nameof(detailIds));
        }

        if (!Enum.IsDefined(preferredLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredLanguage),
                preferredLanguage,
                "Unknown catalogue language.");
        }

        SkillId = skillId;
        DetailIds = details.Select(value => value!).ToImmutableArray();
        PreferredLanguage = preferredLanguage;
    }

    public int SkillId { get; }

    public ImmutableArray<string> DetailIds { get; }

    public CatalogueLanguage PreferredLanguage { get; }
}

public sealed record CombatSkillPageSourceWarning(string Code, string Reason);

public sealed record CombatSkillPageSourceCandidate(
    CombatSkillPageSourceKind Kind,
    CombatSkillPageSourceAvailability Availability,
    int CharacterId,
    string? CharacterName,
    int? Age,
    int? AreaId,
    int? BlockId,
    string? LocationName,
    int? BookItemId,
    int? BookTemplateId,
    int Quantity,
    ImmutableArray<string> DetailIds)
{
    public bool IsActionable => Availability is
        CombatSkillPageSourceAvailability.Locatable
        or CombatSkillPageSourceAvailability.TaiwuInventory;
}

public sealed record CombatSkillPageSourceMetadata(
    SaveSnapshotIdentity SaveSnapshot,
    string GameDataVersion,
    ImmutableArray<CombatSkillPageSourceWarning> Warnings);

public sealed record CombatSkillPageSourceReadResult(
    CombatSkillPageSourceReadStatus Status,
    int SkillId,
    ImmutableArray<string> RequestedDetailIds,
    CombatSkillPageSourceMetadata? Metadata,
    ImmutableArray<CombatSkillPageSourceCandidate> Candidates,
    string? Reason)
{
    public static CombatSkillPageSourceReadResult Unavailable(
        CombatSkillPageSourceReadStatus status,
        CombatSkillPageSourceReadRequest request,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (status is CombatSkillPageSourceReadStatus.Available
            or CombatSkillPageSourceReadStatus.NotRead)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "An unavailable result requires a failure status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new CombatSkillPageSourceReadResult(
            status,
            request.SkillId,
            request.DetailIds,
            Metadata: null,
            Candidates: [],
            reason.Trim());
    }
}

public interface ICombatSkillPageSourceReader
{
    Task<CombatSkillPageSourceReadResult> ReadAsync(
        CombatSkillPageSourceReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FindCombatSkillPageSources(
    ICombatSkillPageSourceReader reader)
{
    public Task<CombatSkillPageSourceReadResult> ExecuteAsync(
        CombatSkillPageSourceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return reader.ReadAsync(request, cancellationToken);
    }
}
