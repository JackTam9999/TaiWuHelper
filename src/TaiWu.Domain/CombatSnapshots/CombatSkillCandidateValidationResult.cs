using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillCandidateValidationResult
{
    internal CombatSkillCandidateValidationResult(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot? skill,
        IEnumerable<CombatSkillCandidateRejection> rejections)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(rejections);

        var rejectionValues = rejections.ToImmutableArray();
        if (rejectionValues.Any(rejection => rejection is null))
        {
            throw new ArgumentException(
                "Candidate rejections cannot contain null entries.",
                nameof(rejections));
        }

        Candidate = candidate;
        Skill = skill;
        Rejections = rejectionValues;
    }

    public CombatSkillCandidate Candidate { get; }

    public CombatSkillSnapshot? Skill { get; }

    public ImmutableArray<CombatSkillCandidateRejection> Rejections { get; }

    public bool IsAccepted => Rejections.IsEmpty;
}
