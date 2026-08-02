namespace TaiWu.Domain.CombatRecommendations;

public sealed record ManualCombatPlanResult
{
    internal ManualCombatPlanResult(
        ManualCombatPlan? plan,
        string? diagnostic)
    {
        Plan = plan;
        Diagnostic = diagnostic;
    }

    public ManualCombatPlan? Plan { get; }

    public bool HasPlan => Plan is not null;

    public string? Diagnostic { get; }
}
