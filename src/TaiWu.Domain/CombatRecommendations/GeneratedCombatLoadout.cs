using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record GeneratedCombatLoadout
{
    internal GeneratedCombatLoadout(
        FeasibleCombatLoadout feasibleLoadout,
        IEnumerable<CombatLoadoutOption> selectedOptions,
        string stableKey)
    {
        FeasibleLoadout = feasibleLoadout;
        SelectedOptions = [.. selectedOptions];
        StableKey = stableKey;
        ThreatCodes = SelectedOptions
            .SelectMany(option => option.ThreatCodes)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        CombatStartCounterCount = SelectedOptions.Count(
            option => option.IsCombatStartCounter);
        HardCounterCount = SelectedOptions.Count(
            option => option.IsHardCounter);
        RetainedCurrentSkillCount = SelectedOptions.Count(
            option => option.IsCurrentlyEquipped);
    }

    public FeasibleCombatLoadout FeasibleLoadout { get; }

    public ImmutableArray<CombatLoadoutOption> SelectedOptions { get; }

    public ImmutableArray<string> ThreatCodes { get; }

    public int CombatStartCounterCount { get; }

    public int HardCounterCount { get; }

    public int RetainedCurrentSkillCount { get; }

    public string StableKey { get; }
}
