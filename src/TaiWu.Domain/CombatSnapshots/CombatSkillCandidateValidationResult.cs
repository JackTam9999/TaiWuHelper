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
        RequiredDirectionChange = GetRequiredDirectionChange(
            candidate,
            skill,
            rejectionValues);
        RequiredBreakthroughDirection = GetRequiredBreakthroughDirection(
            candidate,
            skill,
            rejectionValues);
    }

    public CombatSkillCandidate Candidate { get; }

    public CombatSkillSnapshot? Skill { get; }

    public ImmutableArray<CombatSkillCandidateRejection> Rejections { get; }

    public PracticeDirection? RequiredDirectionChange { get; }

    public PracticeDirection? RequiredBreakthroughDirection { get; }

    public bool IsAccepted => Rejections.IsEmpty;

    private static PracticeDirection? GetRequiredDirectionChange(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot? skill,
        ImmutableArray<CombatSkillCandidateRejection> rejections)
    {
        if (!rejections.IsEmpty
            || !candidate.AllowDirectionChange
            || !candidate.RequiredDirection.HasValue
            || skill is null
            || !skill.Direction.IsAvailable
            || skill.Direction.Value == candidate.RequiredDirection.Value)
        {
            return null;
        }

        return candidate.RequiredDirection;
    }

    private static PracticeDirection? GetRequiredBreakthroughDirection(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot? skill,
        ImmutableArray<CombatSkillCandidateRejection> rejections)
    {
        if (!rejections.IsEmpty
            || !candidate.AllowBreakthrough
            || !candidate.RequiredDirection.HasValue
            || skill is null
            || skill.Direction.IsAvailable
            || !skill.BreakthroughDirections.IsAvailable
            || !skill.BreakthroughDirections.Value.Includes(
                candidate.RequiredDirection.Value))
        {
            return null;
        }

        return candidate.RequiredDirection;
    }
}
