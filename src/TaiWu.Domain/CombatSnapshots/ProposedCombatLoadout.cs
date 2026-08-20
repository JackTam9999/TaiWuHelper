using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record ProposedCombatLoadout
{
    public ProposedCombatLoadout(
        CombatLoadoutSnapshot skills,
        GenericSlotAllocation genericSlotAllocation,
        IEnumerable<CombatSkillCandidate> skillCandidates,
        IEnumerable<CombatRequirement> requirements,
        CombatRequirementContext requirementContext,
        IEnumerable<LegendaryBookCostAssignment>?
            legendaryCostAssignments = null)
    {
        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        GenericSlotAllocation = genericSlotAllocation
            ?? throw new ArgumentNullException(
                nameof(genericSlotAllocation));
        RequirementContext = requirementContext
            ?? throw new ArgumentNullException(nameof(requirementContext));
        HasLegendaryCostAssignments = legendaryCostAssignments is not null;
        ArgumentNullException.ThrowIfNull(skillCandidates);
        ArgumentNullException.ThrowIfNull(requirements);

        SkillCandidates = [.. skillCandidates];
        Requirements = [.. requirements];
        var legendaryValues = (legendaryCostAssignments ?? []).ToArray();
        if (SkillCandidates.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Skill candidates cannot contain null entries.",
                nameof(skillCandidates));
        }

        if (Requirements.Any(requirement => requirement is null))
        {
            throw new ArgumentException(
                "Requirements cannot contain null entries.",
                nameof(requirements));
        }
        if (legendaryValues.Any(item => item is null)
            || legendaryValues.Any(item =>
                item.Origin != LegendaryBookAssignmentOrigin.Proposed)
            || legendaryValues.Select(item => item.SkillId)
                .Distinct().Count() != legendaryValues.Length
            || legendaryValues.Select(item => item.Slot.SlotReference)
                .Distinct(StringComparer.Ordinal).Count()
                != legendaryValues.Length)
        {
            throw new ArgumentException(
                "Proposed legendary assignments require unique proposed skills and slots.",
                nameof(legendaryCostAssignments));
        }

        LegendaryCostAssignments =
        [
            .. legendaryValues.OrderBy(
                item => item.Slot.SlotReference,
                StringComparer.Ordinal)
        ];

        var duplicateCandidate = SkillCandidates
            .GroupBy(candidate => candidate.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCandidate is not null)
        {
            throw new ArgumentException(
                $"Duplicate candidate for skill "
                + $"{duplicateCandidate.Key}.",
                nameof(skillCandidates));
        }
    }

    public CombatLoadoutSnapshot Skills { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public ImmutableArray<CombatSkillCandidate> SkillCandidates { get; }

    public ImmutableArray<CombatRequirement> Requirements { get; }

    public CombatRequirementContext RequirementContext { get; }

    public bool HasLegendaryCostAssignments { get; }

    public ImmutableArray<LegendaryBookCostAssignment>
        LegendaryCostAssignments
    { get; }
}
