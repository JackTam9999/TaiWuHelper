using System.ComponentModel.DataAnnotations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed record TacticalPlanningApiRequest
{
    [Required]
    public IReadOnlyList<TacticalRuleObservationApiRequest> Observations
    {
        get;
        init;
    } = [];

    [Required]
    public TacticalSearchBoundsApiRequest Bounds { get; init; } = new();

    internal TacticalCombatRecommendationRequest ToApplication(
        string saveFilePath,
        int targetCharacterId,
        RecommendationPolicy policy,
        PlayerLoadoutObservation? observation,
        TaiwuLanguage language)
    {
        if (Observations is null || Bounds is null)
        {
            throw new ArgumentException(
                "Tactical observations and bounds are required.");
        }

        var knownEvidence = VerifiedTacticalCombatRuleSets
            .HistoricalMagicSound.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(VerifiedTacticalCombatRuleSets.HistoricalMagicSound.Roles
                .SelectMany(item => item.EvidenceRequirements))
            .Select(item => (item.Identity.Code, item.Scope, item.Source))
            .ToHashSet();
        if (Observations.Any(item => item is null)
            || Observations.Any(item => !knownEvidence.Contains((
                item.Identity,
                item.Scope,
                item.Source))))
        {
            throw new ArgumentException(
                "Every tactical observation identity, scope, and source must match the published rule contract.");
        }

        var snapshotRequest = new CombatSnapshotReadRequest(
            saveFilePath,
            targetCharacterId,
            observation,
            language);
        var contextRequest = new TacticalExecutionContextReadRequest(
            snapshotRequest,
            VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .SupportedTargetGoalCodes,
            Observations.Select(item => item.ToDomain()),
            proposal: null);
        return new TacticalCombatRecommendationRequest(
            playerCharacterId: null,
            policy,
            new TacticalLoadoutSearchReadRequest(
                contextRequest,
                Bounds.ToDomain()));
    }
}

public sealed record TacticalRuleObservationApiRequest
{
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string Identity { get; init; } = string.Empty;

    public TacticalRuleEvidenceScope Scope { get; init; }

    public TacticalEvidenceSourceKind Source { get; init; }

    public TacticalRuleEvidenceDisposition Disposition { get; init; }

    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string EvidenceIdentity { get; init; } = string.Empty;

    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string ScopeIdentity { get; init; } = string.Empty;

    internal TacticalRuleEvidenceObservation ToDomain()
    {
        if (!Enum.IsDefined(Scope)
            || !Enum.IsDefined(Source)
            || !Enum.IsDefined(Disposition))
        {
            throw new ArgumentException(
                "Tactical observation enum tokens are invalid.");
        }

        return new TacticalRuleEvidenceObservation(
            new TacticalRuleEvidenceIdentity(Identity),
            Scope,
            Source,
            Disposition,
            new TacticalEvidenceReference(
                Source,
                EvidenceIdentity,
                VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                VerifiedTacticalCombatRuleSets.RuleVersion,
                ScopeIdentity));
    }
}

public sealed record TacticalSearchBoundsApiRequest
{
    public int MaximumOptions { get; init; } = 16;

    public int MaximumExploredCombinations { get; init; } = 65_536;

    public int MaximumElapsedMilliseconds { get; init; } = 2_000;

    public int MaximumResults { get; init; } = 256;

    internal TacticalSearchBounds ToDomain() => new(
        MaximumOptions,
        MaximumExploredCombinations,
        TimeSpan.FromMilliseconds(MaximumElapsedMilliseconds),
        MaximumResults);
}
