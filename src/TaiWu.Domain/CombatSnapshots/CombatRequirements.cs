namespace TaiWu.Domain.CombatSnapshots;

public abstract class CombatRequirement
{
    protected CombatRequirement(
        CombatRequirementCriticality criticality,
        string evidenceReference)
    {
        if (!Enum.IsDefined(criticality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(criticality),
                criticality,
                "Unknown combat-requirement criticality.");
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A combat requirement requires evidence.",
                nameof(evidenceReference));
        }

        Criticality = criticality;
        EvidenceReference = evidenceReference.Trim();
    }

    public CombatRequirementCriticality Criticality { get; }

    public string EvidenceReference { get; }
}

public sealed class WeaponRequirement : CombatRequirement
{
    public WeaponRequirement(
        int weaponTypeId,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        WeaponTypeId = ValidateId(weaponTypeId, nameof(weaponTypeId));
    }

    public int WeaponTypeId { get; }

    private static int ValidateId(int value, string parameterName)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Weapon type ID cannot be negative.");
    }
}

public sealed class TrickRequirement : CombatRequirement
{
    public TrickRequirement(
        int trickTypeId,
        int minimumCount,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        if (trickTypeId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(trickTypeId),
                trickTypeId,
                "Trick type ID cannot be negative.");
        }

        if (minimumCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumCount),
                minimumCount,
                "Minimum trick count must be greater than zero.");
        }

        TrickTypeId = trickTypeId;
        MinimumCount = minimumCount;
    }

    public int TrickTypeId { get; }

    public int MinimumCount { get; }
}

public sealed class RangeRequirement : CombatRequirement
{
    public RangeRequirement(
        int? minimumInclusive,
        int? maximumInclusive,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        if (!minimumInclusive.HasValue && !maximumInclusive.HasValue)
        {
            throw new ArgumentException(
                "A range requirement needs at least one bound.");
        }

        if (minimumInclusive < 0 || maximumInclusive < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumInclusive),
                "Range bounds cannot be negative.");
        }

        if (minimumInclusive > maximumInclusive)
        {
            throw new ArgumentException(
                "Minimum range cannot exceed maximum range.");
        }

        MinimumInclusive = minimumInclusive;
        MaximumInclusive = maximumInclusive;
    }

    public int? MinimumInclusive { get; }

    public int? MaximumInclusive { get; }
}

public sealed class ResourceRequirement : CombatRequirement
{
    public ResourceRequirement(
        CombatResourceKind resource,
        int minimumAmount,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        if (!Enum.IsDefined(resource))
        {
            throw new ArgumentOutOfRangeException(
                nameof(resource),
                resource,
                "Unknown combat resource.");
        }

        if (minimumAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAmount),
                minimumAmount,
                "Minimum resource amount cannot be negative.");
        }

        Resource = resource;
        MinimumAmount = minimumAmount;
    }

    public CombatResourceKind Resource { get; }

    public int MinimumAmount { get; }
}

public sealed class WeaponUnlockRequirement : CombatRequirement
{
    public WeaponUnlockRequirement(
        int weaponTypeId,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        if (weaponTypeId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weaponTypeId),
                weaponTypeId,
                "Weapon type ID cannot be negative.");
        }

        WeaponTypeId = weaponTypeId;
    }

    public int WeaponTypeId { get; }
}

public sealed class SkillActivationRequirement : CombatRequirement
{
    public SkillActivationRequirement(
        int skillId,
        SkillActivationState requiredState,
        CombatRequirementCriticality criticality,
        string evidenceReference)
        : base(criticality, evidenceReference)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!Enum.IsDefined(requiredState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredState),
                requiredState,
                "Unknown skill activation state.");
        }

        SkillId = skillId;
        RequiredState = requiredState;
    }

    public int SkillId { get; }

    public SkillActivationState RequiredState { get; }
}
