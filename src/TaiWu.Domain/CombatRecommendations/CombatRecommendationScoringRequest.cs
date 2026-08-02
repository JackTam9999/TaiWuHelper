using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatRecommendationScoringRequest
{
    public CombatRecommendationScoringRequest(
        PlayerCombatSnapshot player,
        IEnumerable<TargetThreat> targetThreats,
        IEnumerable<GeneratedCombatLoadout> candidates,
        RecommendationPolicy policy,
        IEnumerable<CandidateDamageEvidence>? damageEvidence = null)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        ArgumentNullException.ThrowIfNull(targetThreats);
        ArgumentNullException.ThrowIfNull(candidates);
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unknown recommendation policy.");
        }

        TargetThreats = [.. targetThreats];
        Candidates = [.. candidates];
        DamageEvidence = damageEvidence?.ToImmutableArray() ?? [];
        if (TargetThreats.Any(threat => threat is null)
            || Candidates.Any(candidate => candidate is null)
            || DamageEvidence.Any(evidence => evidence is null))
        {
            throw new ArgumentException(
                "Scoring collections cannot contain null entries.");
        }

        var duplicateCandidate = Candidates
            .GroupBy(candidate => candidate.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCandidate is not null)
        {
            throw new ArgumentException(
                $"Duplicate candidate '{duplicateCandidate.Key}'.",
                nameof(candidates));
        }

        var duplicateEvidence = DamageEvidence
            .GroupBy(
                evidence => evidence.CandidateStableKey,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEvidence is not null)
        {
            throw new ArgumentException(
                $"Duplicate damage evidence for "
                + $"'{duplicateEvidence.Key}'.",
                nameof(damageEvidence));
        }

        var candidateKeys = Candidates
            .Select(candidate => candidate.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        if (DamageEvidence.Any(evidence =>
                !candidateKeys.Contains(evidence.CandidateStableKey)))
        {
            throw new ArgumentException(
                "Damage evidence references an unknown candidate.",
                nameof(damageEvidence));
        }

        Policy = policy;
    }

    public PlayerCombatSnapshot Player { get; }

    public ImmutableArray<TargetThreat> TargetThreats { get; }

    public ImmutableArray<GeneratedCombatLoadout> Candidates { get; }

    public RecommendationPolicy Policy { get; }

    public ImmutableArray<CandidateDamageEvidence> DamageEvidence { get; }
}
