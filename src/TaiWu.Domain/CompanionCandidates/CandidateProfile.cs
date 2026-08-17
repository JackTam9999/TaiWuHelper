using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace TaiWu.Domain.CompanionCandidates;

public sealed class CandidateProfile
{
    public CandidateProfile(
        CandidateIdentity identity,
        CandidateUniverseState universeState,
        CandidateProfileSourceVersions sourceVersions,
        IEnumerable<CandidateProfileFact> facts,
        IEnumerable<CandidateProfileDiagnostic> diagnostics)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (!Enum.IsDefined(universeState))
        {
            throw new ArgumentOutOfRangeException(nameof(universeState), universeState, "Unknown candidate-universe state.");
        }

        SourceVersions = sourceVersions ?? throw new ArgumentNullException(nameof(sourceVersions));
        ArgumentNullException.ThrowIfNull(facts);
        var copiedFacts = facts.ToImmutableArray();
        if (copiedFacts.Any(item => item is null))
        {
            throw new ArgumentException("A candidate profile cannot contain null facts.", nameof(facts));
        }

        var duplicateFact = copiedFacts
            .GroupBy(item => item.Identity.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFact is not null)
        {
            throw new ArgumentException(
                $"Candidate-profile field {duplicateFact.Key} is duplicated.",
                nameof(facts));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var copiedDiagnostics = diagnostics.ToImmutableArray();
        if (copiedDiagnostics.Any(item => item is null))
        {
            throw new ArgumentException("A candidate profile cannot contain null diagnostics.", nameof(diagnostics));
        }

        var duplicateDiagnostic = copiedDiagnostics
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDiagnostic is not null)
        {
            throw new ArgumentException("A candidate profile cannot contain duplicate diagnostics.", nameof(diagnostics));
        }

        UniverseState = universeState;
        Facts = [.. copiedFacts.OrderBy(item => item.Identity.StableKey, StringComparer.Ordinal)];
        Diagnostics = [.. copiedDiagnostics.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
        Fingerprint = CreateFingerprint();
    }

    public CandidateIdentity Identity { get; }

    public CandidateUniverseState UniverseState { get; }

    public CandidateProfileSourceVersions SourceVersions { get; }

    public ImmutableArray<CandidateProfileFact> Facts { get; }

    public ImmutableArray<CandidateProfileDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    public CandidateProfileFact? FindFact(CandidateProfileFieldIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return Facts.SingleOrDefault(item => item.Identity == identity);
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_CANDIDATE_PROFILE|")
            .Append(SourceVersions.FingerprintSchemaVersion)
            .Append('\n')
            .Append(Identity.StableKey)
            .Append('\n')
            .Append(CandidateProfileText.EnumKey(UniverseState))
            .Append('\n')
            .Append(SourceVersions.StableKey)
            .Append('\n');

        foreach (var fact in Facts)
        {
            canonical.Append("FACT|").Append(fact.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical.Append("DIAGNOSTIC|").Append(diagnostic.StableKey).Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
