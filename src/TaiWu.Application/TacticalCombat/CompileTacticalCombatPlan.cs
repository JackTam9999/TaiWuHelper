using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class CompileTacticalCombatPlan : ICompileTacticalCombatPlan
{
    public TacticalCompiledCombatPlan Execute(
        TacticalPlanCompilationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        return TacticalCombatPlanCompiler.Compile(request, cancellationToken);
    }
}
