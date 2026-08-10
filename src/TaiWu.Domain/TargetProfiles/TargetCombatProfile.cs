using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace TaiWu.Domain.TargetProfiles;

public sealed class TargetCombatProfile
{
    public TargetCombatProfile(
        int targetCharacterId,
        TargetProfileVersion ruleVersion,
        IEnumerable<TargetProfileFacet> facets,
        IEnumerable<TargetProfileDiagnostic> diagnostics)
    {
        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCharacterId),
                targetCharacterId,
                "Target character ID must be greater than zero.");
        }

        RuleVersion = ruleVersion
            ?? throw new ArgumentNullException(nameof(ruleVersion));
        ArgumentNullException.ThrowIfNull(facets);
        ArgumentNullException.ThrowIfNull(diagnostics);
        var facetValues = facets.ToImmutableArray();
        if (facetValues.Any(facet => facet is null))
        {
            throw new ArgumentException(
                "A target combat profile cannot contain null facets.",
                nameof(facets));
        }

        var duplicateFacet = facetValues
            .GroupBy(facet => facet.Identity.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFacet is not null)
        {
            throw new ArgumentException(
                $"Target-profile facet {duplicateFacet.Key} is duplicated.",
                nameof(facets));
        }

        var diagnosticValues = diagnostics.ToImmutableArray();
        if (diagnosticValues.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "A target combat profile cannot contain null diagnostics.",
                nameof(diagnostics));
        }

        var duplicateDiagnostic = diagnosticValues
            .GroupBy(diagnostic => diagnostic.StableKey,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDiagnostic is not null)
        {
            throw new ArgumentException(
                "A target combat profile cannot contain duplicate diagnostics.",
                nameof(diagnostics));
        }

        TargetCharacterId = targetCharacterId;
        Facets = [.. facetValues
            .OrderBy(facet => facet.Identity.Dimension)
            .ThenBy(facet => facet.Identity.Code, StringComparer.Ordinal)];
        Diagnostics = [.. diagnosticValues.OrderBy(
            diagnostic => diagnostic.StableKey,
            StringComparer.Ordinal)];
        Fingerprint = CreateFingerprint();
    }

    public int TargetCharacterId { get; }

    public TargetProfileVersion RuleVersion { get; }

    public ImmutableArray<TargetProfileFacet> Facets { get; }

    public ImmutableArray<TargetProfileDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    public TargetProfileFacet? FindFacet(
        TargetProfileDimension dimension,
        string code)
    {
        if (!Enum.IsDefined(dimension))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimension),
                dimension,
                "Unknown target-profile dimension.");
        }

        var normalizedCode = TargetProfileText.Code(code, nameof(code));
        return Facets.SingleOrDefault(facet =>
            facet.Identity.Dimension == dimension
            && string.Equals(
                facet.Identity.Code,
                normalizedCode,
                StringComparison.Ordinal));
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TARGET_PROFILE_V1\n")
            .Append(TargetCharacterId)
            .Append('\n')
            .Append(RuleVersion.Value)
            .Append('\n');
        foreach (var facet in Facets)
        {
            canonical.Append("FACET|").Append(facet.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical
                .Append("DIAGNOSTIC|")
                .Append(diagnostic.StableKey)
                .Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
