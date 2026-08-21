using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalSkillRoleRule
{
    public TacticalSkillRoleRule(
        TacticalRoleIdentity identity,
        TacticalSemanticVersion semanticVersion,
        IEnumerable<string> supportedGameDataVersions,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        CombatEffectCatalogEntry effect,
        IEnumerable<CombatEffectMechanic> requiredMechanics,
        IEnumerable<string> targetGoalCodes,
        IEnumerable<TacticalTransitionIdentity> transitions,
        IEnumerable<TacticalRuleEvidenceRequirement> evidenceRequirements,
        string limitationIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        CombatCounterRule? sharedCounter = null,
        IEnumerable<TacticalRoleUseKind>? useKinds = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        SemanticVersion = semanticVersion
            ?? throw new ArgumentNullException(nameof(semanticVersion));
        SupportedGameDataVersions = TacticalRuleCollections.Versions(
            supportedGameDataVersions,
            nameof(supportedGameDataVersions));
        Purpose = TacticalCombatText.Defined(purpose, nameof(purpose));
        Timing = TacticalCombatText.Defined(timing, nameof(timing));
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        ArgumentNullException.ThrowIfNull(requiredMechanics);
        var mechanics = requiredMechanics.ToImmutableArray();
        if (mechanics.IsEmpty
            || mechanics.Any(item => !Enum.IsDefined(item))
            || mechanics.Distinct().Count() != mechanics.Length
            || mechanics.Length != Effect.Mechanics.Length
            || mechanics.Any(item => !Effect.Mechanics.Contains(item)))
        {
            throw new ArgumentException(
                "A tactical role requires the complete unique typed-mechanic set from its exact effect.",
                nameof(requiredMechanics));
        }

        RequiredMechanics = [.. mechanics.Order()];
        TargetGoalCodes = TacticalRuleCollections.Goals(
            targetGoalCodes,
            nameof(targetGoalCodes));
        Transitions = TacticalCombatText.CopyUnique(
            transitions,
            item => item.StableKey,
            "role-rule transition",
            nameof(transitions));
        EvidenceRequirements = TacticalCombatText.CopyUnique(
            evidenceRequirements,
            item => item.StableKey,
            "role-rule evidence requirement",
            nameof(evidenceRequirements));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "role-rule evidence",
            nameof(evidence));
        SharedCounter = sharedCounter;
        var uses = (useKinds ?? InferUseKinds(timing, sharedCounter))
            .ToImmutableArray();
        if (uses.IsEmpty
            || uses.Any(item => !Enum.IsDefined(item))
            || uses.Distinct().Count() != uses.Length)
        {
            throw new ArgumentException(
                "A tactical role requires unique defined use kinds.",
                nameof(useKinds));
        }

        UseKinds = [.. uses.Order()];
        if (Transitions.IsEmpty
            || EvidenceRequirements.IsEmpty
            || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical role rule requires transitions, evidence requirements, and evidence.");
        }

        ValidateIdentityAndPurpose();
        ValidateSharedCounter();
        ValidateEvidenceVersions();
    }

    public TacticalRoleIdentity Identity { get; }

    public TacticalSemanticVersion SemanticVersion { get; }

    public ImmutableArray<string> SupportedGameDataVersions { get; }

    public TacticalRulePurpose Purpose { get; }

    public TacticalTransitionTiming Timing { get; }

    public CombatEffectCatalogEntry Effect { get; }

    public ImmutableArray<CombatEffectMechanic> RequiredMechanics { get; }

    public ImmutableArray<string> TargetGoalCodes { get; }

    public ImmutableArray<TacticalTransitionIdentity> Transitions { get; }

    public ImmutableArray<TacticalRuleEvidenceRequirement> EvidenceRequirements
    { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public CombatCounterRule? SharedCounter { get; }

    public ImmutableArray<TacticalRoleUseKind> UseKinds { get; }

    public int SkillId => Effect.SkillId;

    public CombatSnapshots.PracticeDirection Direction => Effect.Direction;

    public int RawEffectId => Effect.RawEffectId;

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        SemanticVersion.StableKey,
        TacticalCombatText.EnumKey(Purpose),
        TacticalCombatText.EnumKey(Timing),
        SkillId,
        Direction.ToString().ToUpperInvariant(),
        RawEffectId,
        LimitationIdentity,
        SharedCounter?.Code ?? "NONE",
        string.Join("||", UseKinds.Select(TacticalCombatText.EnumKey)),
        string.Join("||", SupportedGameDataVersions),
        string.Join("||", RequiredMechanics.Select(
            TacticalCombatText.EnumKey)),
        string.Join("||", TargetGoalCodes),
        string.Join("||", Transitions.Select(item => item.StableKey)),
        string.Join("||", EvidenceRequirements.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    private static IEnumerable<TacticalRoleUseKind> InferUseKinds(
        TacticalTransitionTiming timing,
        CombatCounterRule? sharedCounter)
    {
        if (sharedCounter is not null)
        {
            return sharedCounter.ActivationTiming switch
            {
                CombatCounterActivationTiming.CombatStartPassive =>
                    [TacticalRoleUseKind.OpeningUse,
                        TacticalRoleUseKind.PersistentState],
                CombatCounterActivationTiming.EquippedPassive =>
                    [TacticalRoleUseKind.EquippedPassive],
                CombatCounterActivationTiming.ActiveAttack =>
                    [TacticalRoleUseKind.ActiveAttack],
                CombatCounterActivationTiming.ActiveDefense =>
                    [TacticalRoleUseKind.ActiveDefense],
                CombatCounterActivationTiming.ActiveAgility =>
                    [TacticalRoleUseKind.ActiveAgility],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(sharedCounter))
            };
        }

        return timing switch
        {
            TacticalTransitionTiming.BeforeCombat =>
                [TacticalRoleUseKind.EquippedPassive],
            TacticalTransitionTiming.CombatStart
                or TacticalTransitionTiming.BeforeFirstUse =>
                    [TacticalRoleUseKind.OpeningUse],
            TacticalTransitionTiming.DuringCast
                or TacticalTransitionTiming.AfterCast
                or TacticalTransitionTiming.AfterManualAction =>
                    [TacticalRoleUseKind.ActiveAttack],
            TacticalTransitionTiming.OnObservedState =>
                [TacticalRoleUseKind.PersistentState],
            _ => throw new ArgumentOutOfRangeException(nameof(timing))
        };
    }

    private void ValidateIdentityAndPurpose()
    {
        var expectedKind = Purpose switch
        {
            TacticalRulePurpose.CastSuppression =>
                TacticalRoleKind.Suppression,
            TacticalRulePurpose.DirectPracticeLockRecovery =>
                TacticalRoleKind.Recovery,
            TacticalRulePurpose.DamageChannelChoice =>
                TacticalRoleKind.DamageChannel,
            TacticalRulePurpose.FinishWindowSupport =>
                TacticalRoleKind.Finish,
            TacticalRulePurpose.MarkDurationReduction
                or TacticalRulePurpose.ResonanceDurationReduction
                or TacticalRulePurpose.HindranceMarkRemoval
                or TacticalRulePurpose.EnemyAttackPowerReduction
                or TacticalRulePurpose.ResetResourcePressure
                or TacticalRulePurpose.ConditionalMarkTransfer
                or TacticalRulePurpose.WeaponAttackParry
                or TacticalRulePurpose.HitChanceControl
                or TacticalRulePurpose.CriticalInjuryProtection
                or TacticalRulePurpose.MindMarkConversion
                or TacticalRulePurpose.DirectDamageReduction
                or TacticalRulePurpose.MindDefenseIncrease
                or TacticalRulePurpose.CloseRangeAvoidance
                or TacticalRulePurpose.MobilitySustain =>
                    TacticalRoleKind.Mitigation,
            TacticalRulePurpose.CastSpeedControl
                or TacticalRulePurpose.MovementCounterattack
                or TacticalRulePurpose.CounterStancePressure =>
                    TacticalRoleKind.Interrupt,
            _ => throw new ArgumentException(
                "This tactical purpose is not an approved skill-role purpose.",
                nameof(Purpose))
        };
        if (Identity.Kind != expectedKind)
        {
            throw new ArgumentException(
                "Tactical role identity kind must match its approved purpose.",
                nameof(Identity));
        }
    }

    private void ValidateSharedCounter()
    {
        if (SharedCounter is null)
        {
            return;
        }

        if (SharedCounter.Effect.SkillId != SkillId
            || SharedCounter.RequiredDirection != Direction
            || SharedCounter.Effect.RawEffectId != RawEffectId
            || !SharedCounter.Effect.Mechanics.SequenceEqual(Effect.Mechanics)
            || !TargetGoalCodes.All(code =>
                SharedCounter.ThreatCodes.Contains(code, StringComparer.Ordinal)))
        {
            throw new ArgumentException(
                "A shared counter must have the same exact effect and expose only applicable target goals.",
                nameof(SharedCounter));
        }

        var timingMatches = SharedCounter.ActivationTiming switch
        {
            CombatCounterActivationTiming.CombatStartPassive =>
                Timing == TacticalTransitionTiming.CombatStart,
            CombatCounterActivationTiming.EquippedPassive =>
                Timing is TacticalTransitionTiming.BeforeCombat
                    or TacticalTransitionTiming.OnObservedState,
            CombatCounterActivationTiming.ActiveAttack => Timing is
                TacticalTransitionTiming.DuringCast
                or TacticalTransitionTiming.AfterCast
                or TacticalTransitionTiming.AfterManualAction,
            CombatCounterActivationTiming.ActiveDefense =>
                Timing == TacticalTransitionTiming.OnObservedState,
            CombatCounterActivationTiming.ActiveAgility =>
                Timing == TacticalTransitionTiming.OnObservedState,
            _ => false
        };
        if (!timingMatches)
        {
            throw new ArgumentException(
                "A shared counter and tactical role must have compatible activation timing.",
                nameof(SharedCounter));
        }
    }

    private void ValidateEvidenceVersions()
    {
        var supported = SupportedGameDataVersions.ToHashSet(
            StringComparer.Ordinal);
        if (Evidence.Any(item => !supported.Contains(item.GameDataVersion)))
        {
            throw new ArgumentException(
                "Role-rule evidence must use a supported GameData version.",
                nameof(Evidence));
        }

        var expectedRuleVersion =
            $"TACTICAL_COMBAT_RULES@{SemanticVersion}";
        if (Evidence.Any(item => !string.Equals(
                item.RuleVersion,
                expectedRuleVersion,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Role-rule evidence must match its semantic rule version.",
                nameof(Evidence));
        }
    }
}
