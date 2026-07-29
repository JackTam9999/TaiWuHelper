using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatLoadoutSnapshot
{
    public CombatLoadoutSnapshot(
        IEnumerable<int> neigongSkillIds,
        IEnumerable<int> attackSkillIds,
        IEnumerable<int> agilitySkillIds,
        IEnumerable<int> defenseSkillIds,
        IEnumerable<int> assistanceSkillIds)
    {
        NeigongSkillIds = CopyIds(neigongSkillIds, nameof(neigongSkillIds));
        AttackSkillIds = CopyIds(attackSkillIds, nameof(attackSkillIds));
        AgilitySkillIds = CopyIds(agilitySkillIds, nameof(agilitySkillIds));
        DefenseSkillIds = CopyIds(defenseSkillIds, nameof(defenseSkillIds));
        AssistanceSkillIds = CopyIds(
            assistanceSkillIds,
            nameof(assistanceSkillIds));

        var duplicate = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => Get(category))
            .GroupBy(skillId => skillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Skill {duplicate.Key} is equipped more than once.");
        }
    }

    public ImmutableArray<int> NeigongSkillIds { get; }

    public ImmutableArray<int> AttackSkillIds { get; }

    public ImmutableArray<int> AgilitySkillIds { get; }

    public ImmutableArray<int> DefenseSkillIds { get; }

    public ImmutableArray<int> AssistanceSkillIds { get; }

    public ImmutableArray<int> Get(SkillCategory category) => category switch
    {
        SkillCategory.Neigong => NeigongSkillIds,
        SkillCategory.Attack => AttackSkillIds,
        SkillCategory.Agility => AgilitySkillIds,
        SkillCategory.Defense => DefenseSkillIds,
        SkillCategory.Assistance => AssistanceSkillIds,
        _ => throw new ArgumentOutOfRangeException(
            nameof(category),
            category,
            "Unknown skill category.")
    };

    private static ImmutableArray<int> CopyIds(
        IEnumerable<int> skillIds,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(skillIds, parameterName);

        var values = skillIds.ToImmutableArray();
        if (values.Any(skillId => skillId < 0))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Skill IDs cannot be negative.");
        }

        return values;
    }
}
