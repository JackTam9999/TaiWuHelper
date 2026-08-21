using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public enum TacticalPackageResolutionState
{
    NotApplicable,
    Complete,
    Unresolved
}

public enum TacticalActivationRotationKind
{
    ActiveDefense,
    ActiveAgility
}

public sealed record TacticalRecoveryCastStep
{
    internal TacticalRecoveryCastStep(
        int sequence,
        TacticalCandidateIdentity candidate,
        int effectiveSlotCost,
        IEnumerable<CombatRequirement> requirements)
    {
        if (sequence is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        if (effectiveSlotCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveSlotCost));
        }

        Sequence = sequence;
        Candidate = candidate
            ?? throw new ArgumentNullException(nameof(candidate));
        EffectiveSlotCost = effectiveSlotCost;
        Requirements = TacticalCombatText.CopyUnique(
            requirements,
            TacticalLoadoutPackageKeys.Requirement,
            "recovery cast requirement",
            nameof(requirements));
    }

    public int Sequence { get; }

    public TacticalCandidateIdentity Candidate { get; }

    public int EffectiveSlotCost { get; }

    public ImmutableArray<CombatRequirement> Requirements { get; }

    internal string SemanticKey => string.Join('|',
        Sequence.ToString(CultureInfo.InvariantCulture),
        Candidate.StableKey,
        EffectiveSlotCost.ToString(CultureInfo.InvariantCulture),
        string.Join("||", Requirements.Select(
            TacticalLoadoutPackageKeys.Requirement)));
}

public sealed record TacticalRecoveryPackage
{
    internal TacticalRecoveryPackage(
        TacticalPackageResolutionState state,
        TacticalCandidateIdentity? suppressionCandidate,
        IEnumerable<TacticalRecoveryCastStep> castSteps,
        string reasonIdentity)
    {
        State = TacticalCombatText.Defined(state, nameof(state));
        SuppressionCandidate = suppressionCandidate;
        CastSteps = TacticalCombatText.CopyUnique(
            castSteps,
            item => item.Sequence.ToString(CultureInfo.InvariantCulture),
            "recovery cast step",
            nameof(castSteps));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        if ((State == TacticalPackageResolutionState.NotApplicable)
                != (SuppressionCandidate is null)
            || (State == TacticalPackageResolutionState.Complete)
                != (CastSteps.Length == 3)
            || State == TacticalPackageResolutionState.Unresolved
                && !CastSteps.IsEmpty)
        {
            throw new ArgumentException(
                "Recovery package state must match suppression and three-cast resolution.");
        }
    }

    public TacticalPackageResolutionState State { get; }

    public TacticalCandidateIdentity? SuppressionCandidate { get; }

    public ImmutableArray<TacticalRecoveryCastStep> CastSteps { get; }

    public string ReasonIdentity { get; }

    internal string SemanticKey => string.Join('|',
        TacticalCombatText.EnumKey(State),
        SuppressionCandidate?.StableKey ?? "NO_SUPPRESSION",
        ReasonIdentity,
        string.Join("||", CastSteps.Select(item => item.SemanticKey)));
}

public sealed record TacticalActivationRotation
{
    internal TacticalActivationRotation(
        TacticalActivationRotationKind kind,
        TacticalPackageResolutionState state,
        TacticalCandidateIdentity? primaryCandidate,
        IEnumerable<TacticalCandidateIdentity> backupCandidates,
        string reasonIdentity)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        State = TacticalCombatText.Defined(state, nameof(state));
        PrimaryCandidate = primaryCandidate;
        BackupCandidates = TacticalCombatText.CopyUnique(
            backupCandidates,
            item => item.StableKey,
            "activation rotation backup",
            nameof(backupCandidates));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        if (PrimaryCandidate is not null
            && BackupCandidates.Contains(PrimaryCandidate))
        {
            throw new ArgumentException(
                "A rotation primary cannot also be a backup.",
                nameof(backupCandidates));
        }

        var candidateCount = BackupCandidates.Length
            + (PrimaryCandidate is null ? 0 : 1);
        if ((State == TacticalPackageResolutionState.NotApplicable)
                != (candidateCount == 0)
            || (State == TacticalPackageResolutionState.Complete)
                != (PrimaryCandidate is not null))
        {
            throw new ArgumentException(
                "Activation rotation state must match its primary and backups.");
        }
    }

    public TacticalActivationRotationKind Kind { get; }

    public TacticalPackageResolutionState State { get; }

    public TacticalCandidateIdentity? PrimaryCandidate { get; }

    public ImmutableArray<TacticalCandidateIdentity> BackupCandidates
    { get; }

    public string ReasonIdentity { get; }

    internal string SemanticKey => string.Join('|',
        TacticalCombatText.EnumKey(Kind),
        TacticalCombatText.EnumKey(State),
        PrimaryCandidate?.StableKey ?? "NO_PRIMARY",
        string.Join("||", BackupCandidates.Select(item => item.StableKey)),
        ReasonIdentity);
}

public sealed record TacticalLoadoutPackage
{
    internal TacticalLoadoutPackage(
        TacticalRecoveryPackage recovery,
        TacticalActivationRotation activeDefenseRotation,
        TacticalActivationRotation activeAgilityRotation,
        IEnumerable<TacticalCandidateIdentity> scoringEligibleCandidates)
    {
        Recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        ActiveDefenseRotation = activeDefenseRotation
            ?? throw new ArgumentNullException(nameof(activeDefenseRotation));
        ActiveAgilityRotation = activeAgilityRotation
            ?? throw new ArgumentNullException(nameof(activeAgilityRotation));
        ScoringEligibleCandidates = TacticalCombatText.CopyUnique(
            scoringEligibleCandidates,
            item => item.StableKey,
            "package scoring candidate",
            nameof(scoringEligibleCandidates));
    }

    public TacticalRecoveryPackage Recovery { get; }

    public TacticalActivationRotation ActiveDefenseRotation { get; }

    public TacticalActivationRotation ActiveAgilityRotation { get; }

    public ImmutableArray<TacticalCandidateIdentity> ScoringEligibleCandidates
    { get; }

    internal string SemanticKey => string.Join('\n',
        Recovery.SemanticKey,
        ActiveDefenseRotation.SemanticKey,
        ActiveAgilityRotation.SemanticKey,
        string.Join("||", ScoringEligibleCandidates.Select(
            item => item.StableKey)));
}

internal static class TacticalLoadoutPackageBuilder
{
    internal static TacticalLoadoutPackage Build(
        IEnumerable<TacticalCandidateDiscoveryEntry> selectedEntries,
        ProposedTacticalExecutionFacts facts,
        TacticalCombatRuleResolution resolution)
    {
        var selected = selectedEntries
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var defense = Rotation(
            selected,
            facts.ActiveDefenseSkillId,
            TacticalRoleUseKind.ActiveDefense,
            TacticalActivationRotationKind.ActiveDefense);
        var agility = Rotation(
            selected,
            facts.ActiveAgilitySkillId,
            TacticalRoleUseKind.ActiveAgility,
            TacticalActivationRotationKind.ActiveAgility);
        var activeCandidates = selected
            .Where(item => item.Role!.UseKinds.Any(kind => kind is
                TacticalRoleUseKind.ActiveDefense
                or TacticalRoleUseKind.ActiveAgility))
            .Select(item => item.Consideration.Identity)
            .ToHashSet();
        var scoring = selected
            .Select(item => item.Consideration.Identity)
            .Where(item => !activeCandidates.Contains(item))
            .Concat(defense.PrimaryCandidate is null
                ? []
                : [defense.PrimaryCandidate])
            .Concat(agility.PrimaryCandidate is null
                ? []
                : [agility.PrimaryCandidate]);

        return new TacticalLoadoutPackage(
            Recovery(selected, resolution),
            defense,
            agility,
            scoring);
    }

    private static TacticalRecoveryPackage Recovery(
        TacticalCandidateDiscoveryEntry[] selected,
        TacticalCombatRuleResolution resolution)
    {
        var suppression = selected.SingleOrDefault(item =>
            item.SkillId == 604
            && item.Direction == PracticeDirection.Reverse
            && item.Role!.Identity.Kind == TacticalRoleKind.Suppression
            && string.Equals(
                item.Role.Identity.Code,
                "CURRENT_REVERSE_604_DIRECT_SUPPRESSION",
                StringComparison.Ordinal));
        if (suppression is null)
        {
            return new TacticalRecoveryPackage(
                TacticalPackageResolutionState.NotApplicable,
                suppressionCandidate: null,
                castSteps: [],
                "REVERSE_604_SUPPRESSION_NOT_SELECTED");
        }

        var recoveries = selected.Where(item =>
                item.Direction == PracticeDirection.Reverse
                && item.Role!.Identity.Kind == TacticalRoleKind.Recovery)
            .OrderBy(item => item.EffectiveCost.Value)
            .ThenBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        if (recoveries.Length == 0)
        {
            return new TacticalRecoveryPackage(
                TacticalPackageResolutionState.Unresolved,
                suppression.Consideration.Identity,
                castSteps: [],
                "REVERSE_604_RECOVERY_CASTS_UNRESOLVED");
        }

        var steps = Enumerable.Range(0, 3).Select(index =>
        {
            var entry = recoveries[index % recoveries.Length];
            var role = resolution.Roles.Single(item =>
                item.Rule.Identity == entry.Role!.Identity).Rule;
            return new TacticalRecoveryCastStep(
                index + 1,
                entry.Consideration.Identity,
                entry.EffectiveCost.Value,
                role.SharedCounter?.Requirements ?? []);
        });
        return new TacticalRecoveryPackage(
            TacticalPackageResolutionState.Complete,
            suppression.Consideration.Identity,
            steps,
            "REVERSE_604_THREE_CAST_RECOVERY_RESOLVED");
    }

    private static TacticalActivationRotation Rotation(
        TacticalCandidateDiscoveryEntry[] selected,
        TacticalContextFact<int> activeFact,
        TacticalRoleUseKind useKind,
        TacticalActivationRotationKind kind)
    {
        var entries = selected.Where(item =>
                item.Role!.UseKinds.Contains(useKind))
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        if (entries.Length == 0)
        {
            return new TacticalActivationRotation(
                kind,
                TacticalPackageResolutionState.NotApplicable,
                primaryCandidate: null,
                backupCandidates: [],
                $"{TacticalCombatText.EnumKey(kind)}_ROTATION_NOT_SELECTED");
        }

        var primary = activeFact.IsAvailable
            ? entries.SingleOrDefault(item => item.SkillId == activeFact.Value)
            : null;
        var backups = entries
            .Where(item => item != primary)
            .Select(item => item.Consideration.Identity);
        return new TacticalActivationRotation(
            kind,
            primary is null
                ? TacticalPackageResolutionState.Unresolved
                : TacticalPackageResolutionState.Complete,
            primary?.Consideration.Identity,
            backups,
            primary is null
                ? $"{TacticalCombatText.EnumKey(kind)}_PRIMARY_UNRESOLVED"
                : $"{TacticalCombatText.EnumKey(kind)}_ROTATION_RESOLVED");
    }
}

internal static class TacticalLoadoutPackageKeys
{
    internal static string Requirement(CombatRequirement requirement) =>
        string.Join('|',
            requirement.GetType().Name,
            TacticalCombatText.EnumKey(requirement.Criticality),
            requirement.EvidenceReference);
}
