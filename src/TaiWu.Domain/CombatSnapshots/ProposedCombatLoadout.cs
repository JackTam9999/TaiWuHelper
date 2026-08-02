using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record ProposedCombatLoadout
{
    public ProposedCombatLoadout(
        CombatLoadoutSnapshot skills,
        GenericSlotAllocation genericSlotAllocation,
        IEnumerable<CombatSkillCandidate> skillCandidates,
        IEnumerable<CombatRequirement> requirements,
        CombatRequirementContext requirementContext)
    {
        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        GenericSlotAllocation = genericSlotAllocation
            ?? throw new ArgumentNullException(
                nameof(genericSlotAllocation));
        RequirementContext = requirementContext
            ?? throw new ArgumentNullException(nameof(requirementContext));
        ArgumentNullException.ThrowIfNull(skillCandidates);
        ArgumentNullException.ThrowIfNull(requirements);

        SkillCandidates = [.. skillCandidates];
        Requirements = [.. requirements];
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
}
