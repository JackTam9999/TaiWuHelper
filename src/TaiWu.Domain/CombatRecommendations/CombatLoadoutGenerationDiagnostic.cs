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
            null,
        int occurrences = 1)
    {
        if (occurrences < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurrences),
                occurrences,
                "Diagnostic occurrences must be at least one.");
        }

        Code = code;
        Reason = reason;
        SkillId = skillId;
        FeasibilityFailures = feasibilityFailures?.ToImmutableArray()
            ?? [];
        Occurrences = occurrences;
    }

    public CombatLoadoutGenerationDiagnosticCode Code { get; }

    public string Reason { get; }

    public int? SkillId { get; }

    public int Occurrences { get; }

    public ImmutableArray<CombatLoadoutFeasibilityFailure>
        FeasibilityFailures
    {
        get;
    }

    internal CombatLoadoutGenerationDiagnostic WithOccurrences(
        int occurrences) =>
        new(
            Code,
            Reason,
            SkillId,
            FeasibilityFailures,
            occurrences);
}
