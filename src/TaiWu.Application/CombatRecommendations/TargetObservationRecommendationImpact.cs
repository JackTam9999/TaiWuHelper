using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Application.CombatRecommendations;

public enum TargetThreatImpactKind
{
    Added,
    Confirmed,
    Demoted,
    Removed,
    Unchanged
}

public enum TargetRecommendationImpactKind
{
    Added,
    Removed
}

public enum TargetRecommendationChangeCause
{
    Feasibility,
    Scoring
}

public sealed record TargetObservationRecommendationImpact(
    ImmutableArray<TargetThreatImpact> Threats,
    ImmutableArray<TargetRecommendationImpact> RecommendationChanges,
    ImmutableArray<TargetUnresolvedEvidenceImpact> UnsupportedEvidence,
    bool PartialCoverageLeavesUnknown,
    ImmutableArray<TargetObservationConflictImpact> Conflicts)
{
    public ImmutableArray<TargetRecommendationImpact> FeasibilityChanges =>
        [.. RecommendationChanges.Where(value =>
            value.Cause == TargetRecommendationChangeCause.Feasibility)];

    public ImmutableArray<TargetRecommendationImpact> ScoringChanges =>
        [.. RecommendationChanges.Where(value =>
            value.Cause == TargetRecommendationChangeCause.Scoring)];
}

public sealed record TargetThreatImpact(
    string ThreatCode,
    string Title,
    TargetThreatImpactKind Kind,
    TargetThreatSeverity Severity,
    ImmutableArray<TargetThreatSourceKind> SourceKinds,
    ImmutableArray<string> EvidenceReferences);

public sealed record TargetRecommendationImpact(
    RecommendationPolicy Policy,
    TargetRecommendationImpactKind Kind,
    TargetRecommendationChangeCause Cause,
    int SkillId,
    SkillCategory Category,
    PracticeDirection? RequiredDirection,
    ImmutableArray<string> ThreatCodes,
    ImmutableArray<string> ThreatTitles,
    ImmutableArray<string> EvidenceReferences);

public sealed record TargetUnresolvedEvidenceImpact(
    string Code,
    bool WasPresentBefore,
    string EvidenceReference,
    int? SkillId,
    int? RawEffectId);

public sealed record TargetObservationConflictImpact(
    string Field,
    string ReasonCode,
    string PrecedenceRule,
    ImmutableArray<TargetObservationConflictSource> Sources);

public sealed record TargetObservationConflictSource(
    SnapshotDataSource Source,
    DateTimeOffset CapturedAtUtc,
    string EvidenceReference);
