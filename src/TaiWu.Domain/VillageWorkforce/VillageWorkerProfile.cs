using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record WorkforceDiagnostic
{
    public WorkforceDiagnostic(
        string code,
        WorkforceDiagnosticSeverity severity,
        IEnumerable<WorkforceEvidenceReference> evidence)
    {
        Code = WorkforceText.Stable(code, nameof(code));
        WorkforceText.Defined(severity, nameof(severity));
        Severity = severity;
        ArgumentNullException.ThrowIfNull(evidence);
        var copied = evidence.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Diagnostic evidence cannot contain null entries.",
                nameof(evidence));
        }

        if (copied.GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Diagnostic evidence cannot contain duplicates.",
                nameof(evidence));
        }

        Evidence = [.. copied.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
    }

    public string Code { get; }

    public WorkforceDiagnosticSeverity Severity { get; }

    public ImmutableArray<WorkforceEvidenceReference> Evidence { get; }

    internal string StableKey => string.Join('|',
        Code,
        WorkforceText.EnumKey(Severity),
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed class VillageWorkerProfile
{
    public VillageWorkerProfile(
        VillageWorkerIdentity identity,
        WorkforceWorkerState state,
        WorkforceSourceVersions sourceVersions,
        IEnumerable<WorkforceFact> facts,
        IEnumerable<WorkforceDiagnostic> diagnostics)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        WorkforceText.Defined(state, nameof(state));
        SourceVersions = sourceVersions
            ?? throw new ArgumentNullException(nameof(sourceVersions));
        ArgumentNullException.ThrowIfNull(facts);
        var copiedFacts = facts.ToImmutableArray();
        if (copiedFacts.Any(item => item is null))
        {
            throw new ArgumentException(
                "A worker profile cannot contain null facts.",
                nameof(facts));
        }

        var duplicateFact = copiedFacts
            .GroupBy(item => item.Identity.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFact is not null)
        {
            throw new ArgumentException(
                $"Worker fact {duplicateFact.Key} is duplicated.",
                nameof(facts));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var copiedDiagnostics = diagnostics.ToImmutableArray();
        if (copiedDiagnostics.Any(item => item is null))
        {
            throw new ArgumentException(
                "A worker profile cannot contain null diagnostics.",
                nameof(diagnostics));
        }

        if (copiedDiagnostics
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A worker profile cannot contain duplicate diagnostics.",
                nameof(diagnostics));
        }

        State = state;
        Facts = [.. copiedFacts.OrderBy(
            item => item.Identity.StableKey,
            StringComparer.Ordinal)];
        Diagnostics = [.. copiedDiagnostics.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
        Fingerprint = CreateFingerprint();
    }

    public VillageWorkerIdentity Identity { get; }

    public WorkforceWorkerState State { get; }

    public WorkforceSourceVersions SourceVersions { get; }

    public ImmutableArray<WorkforceFact> Facts { get; }

    public ImmutableArray<WorkforceDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    public WorkforceFact? FindFact(WorkforceFactIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return Facts.SingleOrDefault(item => item.Identity == identity);
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKER_PROFILE|")
            .Append(SourceVersions.FingerprintSchemaVersion)
            .Append('\n')
            .Append(Identity.StableKey).Append('\n')
            .Append(WorkforceText.EnumKey(State)).Append('\n')
            .Append(SourceVersions.StableKey).Append('\n');
        foreach (var fact in Facts)
        {
            canonical.Append("FACT|").Append(fact.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical.Append("DIAGNOSTIC|")
                .Append(diagnostic.StableKey).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }
}
