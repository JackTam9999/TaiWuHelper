namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSkillCandidateValidator
{
    public static CombatSkillCandidateValidationResult Validate(
        PlayerCombatSnapshot player,
        CombatSkillCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(candidate);

        var skill = player.LearnedSkills.FirstOrDefault(
            value => value.SkillId == candidate.SkillId);
        if (skill is null)
        {
            return new CombatSkillCandidateValidationResult(
                candidate,
                skill: null,
                [
                    Reject(
                        CombatSkillCandidateRejectionCode.SkillNotLearned,
                        $"Skill {candidate.SkillId} is not present in the "
                        + "player's learned-skill snapshot.")
                ]);
        }

        List<CombatSkillCandidateRejection> rejections = [];
        ValidateMastery(candidate, skill, rejections);
        ValidateDirection(candidate, skill, rejections);

        return new CombatSkillCandidateValidationResult(
            candidate,
            skill,
            rejections);
    }

    private static void ValidateMastery(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot skill,
        List<CombatSkillCandidateRejection> rejections)
    {
        if (!candidate.RequiresMastery)
        {
            return;
        }

        if (!skill.Mastered.IsAvailable)
        {
            rejections.Add(
                Reject(
                    CombatSkillCandidateRejectionCode
                        .MasteryStatusUnavailable,
                    $"Skill {skill.SkillId} requires mastery, but mastery "
                    + $"status is unavailable: "
                    + skill.Mastered.UnavailableReason));
            return;
        }

        if (!skill.Mastered.Value)
        {
            rejections.Add(
                Reject(
                    CombatSkillCandidateRejectionCode.MasteryRequired,
                    $"Skill {skill.SkillId} requires mastery, but it is not "
                    + "mastered."));
        }
    }

    private static void ValidateDirection(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot skill,
        List<CombatSkillCandidateRejection> rejections)
    {
        if (!candidate.RequiredDirection.HasValue)
        {
            return;
        }

        var requiredDirection = candidate.RequiredDirection.Value;
        if (requiredDirection == PracticeDirection.Neutral)
        {
            rejections.Add(
                Reject(
                    CombatSkillCandidateRejectionCode
                        .NeutralDirectionCannotActivateEffect,
                    $"Skill {skill.SkillId} cannot use Neutral as a "
                    + "direction-specific effect."));
            return;
        }

        if (!skill.Direction.IsAvailable)
        {
            if (!CanCompleteBreakthroughAs(
                    candidate,
                    skill,
                    requiredDirection))
            {
                rejections.Add(
                    Reject(
                        CombatSkillCandidateRejectionCode
                            .DirectionStatusUnavailable,
                        BreakthroughRejectionReason(
                            skill,
                            requiredDirection)));
            }
        }
        else if (skill.Direction.Value == PracticeDirection.Neutral)
        {
            if (!candidate.AllowDirectionChange)
            {
                rejections.Add(
                    Reject(
                        CombatSkillCandidateRejectionCode
                            .NeutralDirectionCannotActivateEffect,
                        $"Skill {skill.SkillId} is Neutral and cannot "
                        + $"activate its {requiredDirection} effect."));
            }
        }
        else if (skill.Direction.Value != requiredDirection)
        {
            if (!candidate.AllowDirectionChange)
            {
                rejections.Add(
                    Reject(
                        CombatSkillCandidateRejectionCode.DirectionMismatch,
                        $"Skill {skill.SkillId} is "
                        + $"{skill.Direction.Value}, not "
                        + $"{requiredDirection}."));
            }
        }

        ValidateDirectionEffect(
            skill,
            requiredDirection,
            rejections);
    }

    private static bool CanCompleteBreakthroughAs(
        CombatSkillCandidate candidate,
        CombatSkillSnapshot skill,
        PracticeDirection requiredDirection)
    {
        return candidate.AllowBreakthrough
            && skill.BreakthroughDirections.IsAvailable
            && skill.BreakthroughDirections.Value.Includes(
                requiredDirection);
    }

    private static string BreakthroughRejectionReason(
        CombatSkillSnapshot skill,
        PracticeDirection requiredDirection)
    {
        if (!skill.BreakthroughDirections.IsAvailable)
        {
            return $"Skill {skill.SkillId} requires {requiredDirection}, "
                + "but its practice direction is unavailable: "
                + skill.Direction.UnavailableReason;
        }

        var availability = skill.BreakthroughDirections.Value;
        if (!availability.IsBrokenOut
            && availability.CanBreakthroughNow)
        {
            return $"Skill {skill.SkillId} requires {requiredDirection}, "
                + "but its immediately available breakthrough cannot "
                + $"produce {requiredDirection}.";
        }

        if (!availability.IsBrokenOut)
        {
            return $"Skill {skill.SkillId} requires {requiredDirection}, "
                + "but it has not completed breakthrough and cannot "
                + "break through now.";
        }

        return $"Skill {skill.SkillId} requires {requiredDirection}, but "
            + "its active practice direction is unavailable.";
    }

    private static void ValidateDirectionEffect(
        CombatSkillSnapshot skill,
        PracticeDirection requiredDirection,
        List<CombatSkillCandidateRejection> rejections)
    {
        var effectId = requiredDirection == PracticeDirection.Direct
            ? skill.DirectEffectId
            : skill.ReverseEffectId;
        if (effectId.IsAvailable)
        {
            return;
        }

        var code = requiredDirection == PracticeDirection.Direct
            ? CombatSkillCandidateRejectionCode.DirectEffectUnavailable
            : CombatSkillCandidateRejectionCode.ReverseEffectUnavailable;
        rejections.Add(
            Reject(
                code,
                $"Skill {skill.SkillId} has no available "
                + $"{requiredDirection} effect: "
                + effectId.UnavailableReason));
    }

    private static CombatSkillCandidateRejection Reject(
        CombatSkillCandidateRejectionCode code,
        string reason)
    {
        return new CombatSkillCandidateRejection(code, reason);
    }
}
