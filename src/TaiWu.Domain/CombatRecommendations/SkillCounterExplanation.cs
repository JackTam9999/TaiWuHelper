using TaiWu.Domain.CombatCounters;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillCounterExplanation
{
    internal SkillCounterExplanation(
        bool isAvailable,
        CombatCounterStrength? strength,
        CombatCounterActivationTiming? activationTiming,
        string? evidenceReference,
        string? unavailableReason)
    {
        IsAvailable = isAvailable;
        Strength = strength;
        ActivationTiming = activationTiming;
        EvidenceReference = evidenceReference;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable { get; }

    public CombatCounterStrength? Strength { get; }

    public CombatCounterActivationTiming? ActivationTiming { get; }

    public string? EvidenceReference { get; }

    public string? UnavailableReason { get; }
}
