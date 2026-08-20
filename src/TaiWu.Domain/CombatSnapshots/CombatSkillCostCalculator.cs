namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSkillCostCalculator
{
    public const int MinimumEffectiveCost = 1;

    public static CombatSkillCostBreakdown Calculate(
        PlayerCombatSnapshot player,
        int skillId)
    {
        ArgumentNullException.ThrowIfNull(player);

        var skill = FindLearnedSkill(player, skillId);
        var assignments = player.LegendaryBookCostAssignments
            .Where(assignment => assignment.SkillId == skillId)
            .ToArray();

        return CalculateCore(skill, assignments);
    }

    public static CombatSkillCostBreakdown CalculateProposed(
        PlayerCombatSnapshot player,
        LegendaryBookCostAssignment proposedAssignment)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(proposedAssignment);

        if (proposedAssignment.Origin
            != LegendaryBookAssignmentOrigin.Proposed)
        {
            throw new ArgumentException(
                "A proposed calculation requires a proposed assignment.",
                nameof(proposedAssignment));
        }

        var knownSlot = player.LegendaryBookCostSlots.FirstOrDefault(
            slot => string.Equals(
                slot.SlotReference,
                proposedAssignment.Slot.SlotReference,
                StringComparison.Ordinal));
        if (knownSlot is null || knownSlot != proposedAssignment.Slot)
        {
            throw new ArgumentException(
                $"Proposed assignment references unavailable slot "
                + $"'{proposedAssignment.Slot.SlotReference}'.",
                nameof(proposedAssignment));
        }

        var skill = FindLearnedSkill(player, proposedAssignment.SkillId);
        if (skill.Category != proposedAssignment.Category)
        {
            throw new ArgumentException(
                $"Proposed assignment for skill {skill.SkillId} uses "
                + $"{proposedAssignment.Category}, not {skill.Category}.",
                nameof(proposedAssignment));
        }

        return CalculateCore(skill, [proposedAssignment]);
    }

    public static CombatSkillCostBreakdown CalculateWithoutLegendaryAssignment(
        PlayerCombatSnapshot player,
        int skillId)
    {
        ArgumentNullException.ThrowIfNull(player);
        return CalculateCore(FindLearnedSkill(player, skillId), []);
    }

    public static CombatSkillCostBreakdown CalculateProposed(
        PlayerCombatSnapshot player,
        int skillId,
        IEnumerable<LegendaryBookCostAssignment> proposedAssignments)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(proposedAssignments);
        var values = proposedAssignments.ToArray();
        if (values.Any(item => item is null)
            || values.Any(item =>
                item.Origin != LegendaryBookAssignmentOrigin.Proposed)
            || values.Select(item => item.SkillId).Distinct().Count()
                != values.Length
            || values.Select(item => item.Slot.SlotReference)
                .Distinct(StringComparer.Ordinal).Count()
                != values.Length)
        {
            throw new ArgumentException(
                "Projected legendary assignments must be proposed and unique by skill and slot.",
                nameof(proposedAssignments));
        }

        foreach (var assignment in values)
        {
            var knownSlot = player.LegendaryBookCostSlots.SingleOrDefault(
                slot => string.Equals(
                    slot.SlotReference,
                    assignment.Slot.SlotReference,
                    StringComparison.Ordinal));
            if (knownSlot is null || knownSlot != assignment.Slot)
            {
                throw new ArgumentException(
                    "Projected legendary assignments must use known slots.",
                    nameof(proposedAssignments));
            }

            var assignedSkill = FindLearnedSkill(player, assignment.SkillId);
            if (assignedSkill.Category != assignment.Category)
            {
                throw new ArgumentException(
                    $"Projected assignment for skill {assignedSkill.SkillId} "
                    + $"uses {assignment.Category}, not "
                    + $"{assignedSkill.Category}.",
                    nameof(proposedAssignments));
            }
        }

        return CalculateCore(
            FindLearnedSkill(player, skillId),
            values.Where(item => item.SkillId == skillId).ToArray());
    }

    private static CombatSkillSnapshot FindLearnedSkill(
        PlayerCombatSnapshot player,
        int skillId)
    {
        return player.LearnedSkills.FirstOrDefault(
            candidate => candidate.SkillId == skillId)
            ?? throw new KeyNotFoundException(
                $"Player has not learned combat skill {skillId}.");
    }

    private static CombatSkillCostBreakdown CalculateCore(
        CombatSkillSnapshot skill,
        LegendaryBookCostAssignment[] assignments)
    {
        if (assignments.Length > 1)
        {
            throw new ArgumentException(
                $"Skill {skill.SkillId} has more than one legendary-book "
                + "fixed-cost assignment.",
                nameof(assignments));
        }

        var assignment = assignments.SingleOrDefault();
        if (assignment is not null && assignment.Category != skill.Category)
        {
            throw new ArgumentException(
                $"Legendary-book assignment for skill {skill.SkillId} "
                + $"uses {assignment.Category}, not {skill.Category}.",
                nameof(assignments));
        }

        var masteryReduction = CalculateMasteryReduction(skill);
        var masteryAdjustedCost = CalculateMasteryAdjustedCost(
            skill,
            masteryReduction);

        SnapshotValue<int> legendaryBookReduction;
        SnapshotValue<int> effectiveCost;
        if (assignment is null)
        {
            legendaryBookReduction = SnapshotValue<int>.Available(0);
            effectiveCost = masteryAdjustedCost;
        }
        else
        {
            var fixedCost = assignment.Slot.Rule.FixedCost;
            effectiveCost = SnapshotValue<int>.Available(fixedCost);
            legendaryBookReduction = masteryAdjustedCost.IsAvailable
                ? SnapshotValue<int>.Available(
                    Math.Max(0, masteryAdjustedCost.Value - fixedCost))
                : SnapshotValue<int>.Unavailable(
                    "Legendary-book reduction is unavailable because the "
                    + "mastery-adjusted base cost is unavailable.");
        }

        return new CombatSkillCostBreakdown(
            skill,
            assignments,
            masteryReduction,
            legendaryBookReduction,
            effectiveCost);
    }

    private static SnapshotValue<int> CalculateMasteryReduction(
        CombatSkillSnapshot skill)
    {
        if (!skill.GridCost.IsAvailable)
        {
            return SnapshotValue<int>.Unavailable(
                "Mastery reduction is unavailable because GridCost is "
                + $"unavailable: {skill.GridCost.UnavailableReason}");
        }

        if (!skill.Mastered.IsAvailable)
        {
            return SnapshotValue<int>.Unavailable(
                "Mastery reduction is unavailable because mastery is "
                + $"unavailable: {skill.Mastered.UnavailableReason}");
        }

        var masteryAdjustedCost = Math.Max(
            MinimumEffectiveCost,
            skill.GridCost.Value - (skill.Mastered.Value ? 1 : 0));
        return SnapshotValue<int>.Available(
            skill.GridCost.Value - masteryAdjustedCost);
    }

    private static SnapshotValue<int> CalculateMasteryAdjustedCost(
        CombatSkillSnapshot skill,
        SnapshotValue<int> masteryReduction)
    {
        if (!skill.GridCost.IsAvailable)
        {
            return SnapshotValue<int>.Unavailable(
                "Effective cost is unavailable because GridCost is "
                + $"unavailable: {skill.GridCost.UnavailableReason}");
        }

        if (!masteryReduction.IsAvailable)
        {
            return SnapshotValue<int>.Unavailable(
                "Effective cost is unavailable because mastery reduction is "
                + $"unavailable: {masteryReduction.UnavailableReason}");
        }

        return SnapshotValue<int>.Available(
            skill.GridCost.Value - masteryReduction.Value);
    }
}
