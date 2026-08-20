using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalSkillRole
{
    public TacticalSkillRole(
        TacticalRoleIdentity identity,
        short skillId,
        PracticeDirection direction,
        short effectId,
        TacticalTransitionTiming timing,
        IEnumerable<TacticalTransitionIdentity> transitions,
        IEnumerable<TacticalRequirementIdentity> requirements,
        string limitationIdentity,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillId));
        }

        if (effectId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(effectId));
        }

        if (direction is not PracticeDirection.Direct
            and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "A tactical role requires Direct or Reverse practice.");
        }

        SkillId = skillId;
        Direction = direction;
        EffectId = effectId;
        Timing = TacticalCombatText.Defined(timing, nameof(timing));
        Transitions = TacticalCombatText.CopyUnique(
            transitions,
            item => item.StableKey,
            "role transition",
            nameof(transitions));
        Requirements = TacticalCombatText.CopyUnique(
            requirements,
            item => item.StableKey,
            "role requirement",
            nameof(requirements));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "role evidence",
            nameof(evidence));
        if (Transitions.IsEmpty || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical skill role requires a transition and evidence.");
        }
    }

    public TacticalRoleIdentity Identity { get; }

    public short SkillId { get; }

    public PracticeDirection Direction { get; }

    public short EffectId { get; }

    public TacticalTransitionTiming Timing { get; }

    public ImmutableArray<TacticalTransitionIdentity> Transitions { get; }

    public ImmutableArray<TacticalRequirementIdentity> Requirements { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        SkillId.ToString(CultureInfo.InvariantCulture),
        Direction.ToString().ToUpperInvariant(),
        EffectId.ToString(CultureInfo.InvariantCulture),
        TacticalCombatText.EnumKey(Timing),
        LimitationIdentity,
        string.Join("||", Transitions.Select(item => item.StableKey)),
        string.Join("||", Requirements.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed class TacticalCandidateConsideration
{
    public TacticalCandidateConsideration(
        TacticalCandidateIdentity identity,
        TacticalCandidateDecision decision,
        IEnumerable<TacticalRoleIdentity> roles,
        IEnumerable<TacticalRequirementEvaluation> requirements,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        TacticalCandidateIdentity? dominatedBy = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Decision = TacticalCombatText.Defined(decision, nameof(decision));
        Roles = TacticalCombatText.CopyUnique(
            roles,
            item => item.StableKey,
            "candidate role",
            nameof(roles));
        Requirements = TacticalCombatText.CopyUnique(
            requirements,
            item => item.StableKey,
            "candidate requirement",
            nameof(requirements));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "candidate evidence",
            nameof(evidence));
        DominatedBy = dominatedBy;
        ValidateInvariant();
    }

    public TacticalCandidateIdentity Identity { get; }

    public TacticalCandidateDecision Decision { get; }

    public ImmutableArray<TacticalRoleIdentity> Roles { get; }

    public ImmutableArray<TacticalRequirementEvaluation> Requirements { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public TacticalCandidateIdentity? DominatedBy { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Decision),
        ReasonIdentity,
        DominatedBy?.StableKey ?? "NONE",
        string.Join("||", Roles.Select(item => item.StableKey)),
        string.Join("||", Requirements.Select(item => item.ContentKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    internal IEnumerable<TacticalEvidenceReference> AllEvidence =>
        Evidence.Concat(Requirements.SelectMany(item => item.Evidence));

    private void ValidateInvariant()
    {
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A candidate consideration requires evidence.",
                nameof(Evidence));
        }

        var allSatisfied = Requirements.All(item =>
            item.Outcome == TacticalRequirementOutcome.Satisfied);
        switch (Decision)
        {
            case TacticalCandidateDecision.Admitted:
            case TacticalCandidateDecision.Irrelevant:
                if (Roles.IsEmpty || !allSatisfied || DominatedBy is not null)
                {
                    throw new ArgumentException(
                        "An admitted or irrelevant candidate requires roles, satisfied requirements, and no dominator.");
                }

                break;
            case TacticalCandidateDecision.Rejected:
                if (Roles.IsEmpty
                    || !Requirements.Any(item =>
                    item.Outcome == TacticalRequirementOutcome.Unsatisfied)
                    || DominatedBy is not null)
                {
                    throw new ArgumentException(
                        "A rejected candidate requires a role, an unsatisfied hard requirement, and no dominator.");
                }

                break;
            case TacticalCandidateDecision.Unsupported:
                if (!Roles.IsEmpty
                    && !Requirements.Any(item => item.Outcome is
                        TacticalRequirementOutcome.Unknown
                        or TacticalRequirementOutcome.Unsupported
                        or TacticalRequirementOutcome.Conflicting))
                {
                    throw new ArgumentException(
                        "An unsupported candidate requires a missing role or an unavailable requirement.");
                }

                if (DominatedBy is not null)
                {
                    throw new ArgumentException(
                        "An unsupported candidate cannot have a dominator.");
                }

                break;
            case TacticalCandidateDecision.Dominated:
                if (Roles.IsEmpty
                    || !allSatisfied
                    || DominatedBy is null
                    || DominatedBy == Identity)
                {
                    throw new ArgumentException(
                        "A dominated candidate requires comparable roles, satisfied requirements, and a distinct dominator.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Decision));
        }
    }
}
