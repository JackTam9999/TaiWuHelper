using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatLoadoutGenerationDiagnostic
{
    internal CombatLoadoutGenerationDiagnostic(
        CombatLoadoutGenerationDiagnosticCode code,
        string reason,
        int? skillId = null,
        IEnumerable<CombatLoadoutFeasibilityFailure>? feasibilityFailures =
            null)
    {
        Code = code;
        Reason = reason;
        SkillId = skillId;
        FeasibilityFailures = feasibilityFailures?.ToImmutableArray()
            ?? [];
    }

    public CombatLoadoutGenerationDiagnosticCode Code { get; }

    public string Reason { get; }

    public int? SkillId { get; }

    public ImmutableArray<CombatLoadoutFeasibilityFailure>
        FeasibilityFailures
    {
        get;
    }
}
