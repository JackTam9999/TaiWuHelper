using System.Collections.Immutable;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed record TacticalLoadoutSearchReadRequest
{
    public TacticalLoadoutSearchReadRequest(
        TacticalExecutionContextReadRequest contextRequest,
        TacticalSearchBounds bounds,
        TacticalCandidateDiscoveryLimits? discoveryLimits = null,
        IEnumerable<TacticalIrrelevanceProof>? irrelevanceProofs = null,
        IEnumerable<TacticalDominanceProof>? dominanceProofs = null)
    {
        ContextRequest = contextRequest
            ?? throw new ArgumentNullException(nameof(contextRequest));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        DiscoveryLimits = discoveryLimits
            ?? TacticalCandidateDiscoveryLimits.Default;
        IrrelevanceProofs = CopyUnique(
            irrelevanceProofs ?? [],
            item => IdentityKey(item.Candidate),
            nameof(irrelevanceProofs));
        DominanceProofs = CopyUnique(
            dominanceProofs ?? [],
            item => IdentityKey(item.Dominated),
            nameof(dominanceProofs));
    }

    public TacticalExecutionContextReadRequest ContextRequest { get; }

    public TacticalSearchBounds Bounds { get; }

    public TacticalCandidateDiscoveryLimits DiscoveryLimits { get; }

    public ImmutableArray<TacticalIrrelevanceProof> IrrelevanceProofs { get; }

    public ImmutableArray<TacticalDominanceProof> DominanceProofs { get; }

    private static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> source,
        Func<T, string> key,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);
        var values = source.ToImmutableArray();
        if (values.Any(item => item is null)
            || values.Select(key).Distinct(StringComparer.Ordinal).Count()
                != values.Length)
        {
            throw new ArgumentException(
                "Search proofs must be non-null and unique by candidate.",
                parameterName);
        }

        return [.. values.OrderBy(key, StringComparer.Ordinal)];
    }

    private static string IdentityKey(TacticalCandidateIdentity value) =>
        $"{value.SkillId}:{value.Direction}";
}
