namespace TaiWu.Domain.CombatSkills;

public enum CombatSkillPowerContext
{
    OutOfCombat = 0
}

public sealed record CombatSkillPowerProgress
{
    public CombatSkillPowerProgress(
        SkillProgressField<int> current,
        SkillProgressField<int> maximum,
        CombatSkillPowerContext context)
    {
        Current = ValidateCurrent(current, nameof(current));
        Maximum = ValidateMaximum(maximum, nameof(maximum));
        if (!Enum.IsDefined(context))
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context,
                "Unknown combat-skill power context.");
        }

        Context = context;
    }

    public SkillProgressField<int> Current { get; }

    public SkillProgressField<int> Maximum { get; }

    public CombatSkillPowerContext Context { get; }

    private static SkillProgressField<int> ValidateCurrent(
        SkillProgressField<int> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable && field.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                "Current combat-skill power cannot be negative.");
        }

        return field;
    }

    private static SkillProgressField<int> ValidateMaximum(
        SkillProgressField<int> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable && field.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                "Maximum combat-skill power must be positive.");
        }

        return field;
    }
}
