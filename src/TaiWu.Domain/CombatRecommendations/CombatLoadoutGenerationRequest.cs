using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatLoadoutGenerationRequest
{
    public const int MaximumOptions = 40;

    public const int MaximumExploredCombinations = 65_536;

    public const int MaximumResults = 256;

    public CombatLoadoutGenerationRequest(
        PlayerCombatSnapshot player,
        IEnumerable<CombatLoadoutOption> options,
        CombatRequirementContext baseRequirementContext,
        GenericSlotAllocation genericSlotAllocation,
        int maxExploredCombinations = 4096,
        int maxResults = 32)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        BaseRequirementContext = baseRequirementContext
            ?? throw new ArgumentNullException(
                nameof(baseRequirementContext));
        GenericSlotAllocation = genericSlotAllocation
            ?? throw new ArgumentNullException(
                nameof(genericSlotAllocation));
        ArgumentNullException.ThrowIfNull(options);

        Options = [.. options];
        if (Options.Any(option => option is null))
        {
            throw new ArgumentException(
                "Loadout options cannot contain null entries.",
                nameof(options));
        }

        if (Options.Length > MaximumOptions)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"At most {MaximumOptions} options may be explored.");
        }

        var duplicate = Options
            .GroupBy(option => option.Candidate.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate loadout option for skill "
                + $"{duplicate.Key}.",
                nameof(options));
        }

        if (maxExploredCombinations is < 1
            or > MaximumExploredCombinations)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExploredCombinations),
                maxExploredCombinations,
                $"Exploration limit must be between 1 and "
                + $"{MaximumExploredCombinations}.");
        }

        if (maxResults is < 1 or > MaximumResults)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                maxResults,
                $"Result limit must be between 1 and {MaximumResults}.");
        }

        MaxExploredCombinations = maxExploredCombinations;
        MaxResults = maxResults;
    }

    public PlayerCombatSnapshot Player { get; }

    public ImmutableArray<CombatLoadoutOption> Options { get; }

    public CombatRequirementContext BaseRequirementContext { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public int MaxExploredCombinations { get; }

    public int MaxResults { get; }
}
