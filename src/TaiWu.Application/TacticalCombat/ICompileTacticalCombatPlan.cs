using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public interface ICompileTacticalCombatPlan
{
    TacticalCompiledCombatPlan Execute(
        TacticalPlanCompilationRequest request,
        CancellationToken cancellationToken = default);
}
