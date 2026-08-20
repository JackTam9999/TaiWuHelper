using System.Globalization;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalFactIdentity
{
    public TacticalFactIdentity(TacticalFactKind kind, string code)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        Code = TacticalCombatText.Code(code, nameof(code));
    }

    public TacticalFactKind Kind { get; }

    public string Code { get; }

    internal string StableKey =>
        $"{TacticalCombatText.EnumKey(Kind)}:{Code}";
}

public sealed record TacticalRequirementIdentity
{
    public TacticalRequirementIdentity(string code) =>
        Code = TacticalCombatText.Code(code, nameof(code));

    public string Code { get; }

    internal string StableKey => Code;
}

public sealed record TacticalTransitionIdentity
{
    public TacticalTransitionIdentity(string code) =>
        Code = TacticalCombatText.Code(code, nameof(code));

    public string Code { get; }

    internal string StableKey => Code;
}

public sealed record TacticalRoleIdentity
{
    public TacticalRoleIdentity(TacticalRoleKind kind, string code)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        Code = TacticalCombatText.Code(code, nameof(code));
    }

    public TacticalRoleKind Kind { get; }

    public string Code { get; }

    internal string StableKey =>
        $"{TacticalCombatText.EnumKey(Kind)}:{Code}";
}

public sealed record TacticalCandidateIdentity
{
    public TacticalCandidateIdentity(
        short skillId,
        PracticeDirection direction)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillId));
        }

        if (direction is not PracticeDirection.Direct
            and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "A tactical candidate requires Direct or Reverse practice.");
        }

        SkillId = skillId;
        Direction = direction;
    }

    public short SkillId { get; }

    public PracticeDirection Direction { get; }

    internal string StableKey => string.Join(':',
        SkillId.ToString(CultureInfo.InvariantCulture),
        Direction.ToString().ToUpperInvariant());
}

public sealed record TacticalPlanStepIdentity
{
    public TacticalPlanStepIdentity(string code) =>
        Code = TacticalCombatText.Code(code, nameof(code));

    public string Code { get; }

    internal string StableKey => Code;
}
