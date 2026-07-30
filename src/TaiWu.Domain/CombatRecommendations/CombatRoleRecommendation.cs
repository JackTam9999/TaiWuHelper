using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatRoleRecommendation
{
    internal CombatRoleRecommendation(
        SkillCategory category,
        CombatRoleChoice? primary,
        IEnumerable<CombatRoleChoice> alternatives)
    {
        Category = category;
        Primary = primary;
        Alternatives = [.. alternatives];
    }

    public SkillCategory Category { get; }

    public CombatRoleChoice? Primary { get; }

    public ImmutableArray<CombatRoleChoice> Alternatives { get; }
}
