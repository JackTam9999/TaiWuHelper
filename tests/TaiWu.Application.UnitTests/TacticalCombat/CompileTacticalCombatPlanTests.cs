using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class CompileTacticalCombatPlanTests
{
    [Fact]
    public void Execute_rejects_a_missing_coherent_compilation_request()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompileTacticalCombatPlan().Execute(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Execute_honors_precancellation_before_compilation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new CompileTacticalCombatPlan().Execute(
                null!,
                source.Token));
    }

    [Fact]
    public void Application_contract_exposes_the_domain_compilation_boundary()
    {
        var method = typeof(ICompileTacticalCombatPlan).GetMethod("Execute");

        Assert.NotNull(method);
        Assert.Equal(typeof(TacticalCompiledCombatPlan), method.ReturnType);
        Assert.Equal(
            typeof(TacticalPlanCompilationRequest),
            method.GetParameters()[0].ParameterType);
    }
}
