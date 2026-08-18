using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed class VillageWorkforceSnapshot
{
    public VillageWorkforceSnapshot(
        SettlementIdentity settlement,
        DateTimeOffset capturedAt,
        WorkforceSourceVersions sourceVersions,
        IEnumerable<VillageWorkerProfile> workers,
        IEnumerable<ShopManagerTarget> targets,
        IEnumerable<CurrentShopManagerAssignment> currentAssignments,
        IEnumerable<WorkforceDiagnostic> diagnostics)
    {
        Settlement = settlement
            ?? throw new ArgumentNullException(nameof(settlement));
        CapturedAt = capturedAt.ToUniversalTime();
        SourceVersions = sourceVersions
            ?? throw new ArgumentNullException(nameof(sourceVersions));
        Workers = CopyUnique(
            workers,
            item => item.Identity.StableKey,
            "worker",
            nameof(workers));
        Targets = CopyUnique(
            targets,
            item => item.Identity.StableKey,
            "target",
            nameof(targets));
        CurrentAssignments = CopyUnique(
            currentAssignments,
            item => item.Target.StableKey,
            "current assignment target",
            nameof(currentAssignments));
        Diagnostics = CopyUnique(
            diagnostics,
            item => item.StableKey,
            "diagnostic",
            nameof(diagnostics));

        if (Workers.Any(item => item.SourceVersions != SourceVersions))
        {
            throw new ArgumentException(
                "Every worker profile must use the snapshot source versions.",
                nameof(workers));
        }

        foreach (var provenance in EnumerateProvenances())
        {
            if (provenance.SourceKind
                    == WorkforceEvidenceSourceKind.ConfiguredSave
                && !string.Equals(
                    provenance.RevisionIdentity,
                    SourceVersions.SaveSha256,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Configured-save evidence must match the snapshot revision.");
            }

            if (provenance.SourceKind
                    == WorkforceEvidenceSourceKind.InstalledGameData
                && !string.Equals(
                    provenance.SourceVersion,
                    SourceVersions.GameDataVersion,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Installed GameData evidence must match the snapshot version.");
            }
        }

        var workerIdentities = Workers
            .Select(item => item.Identity)
            .ToHashSet();
        var targetIdentities = Targets
            .Select(item => item.Identity)
            .ToHashSet();
        if (CurrentAssignments.Any(item =>
            !targetIdentities.Contains(item.Target)))
        {
            throw new ArgumentException(
                "A current assignment must reference a snapshot target.",
                nameof(currentAssignments));
        }

        if (CurrentAssignments.Any(item =>
            !workerIdentities.Contains(item.Worker)))
        {
            throw new ArgumentException(
                "A current assignment must reference a snapshot worker.",
                nameof(currentAssignments));
        }

        if (CurrentAssignments.Length != Targets.Length)
        {
            throw new ArgumentException(
                "Every version-1 occupied target requires exactly one current assignment.",
                nameof(currentAssignments));
        }

        Fingerprint = CreateFingerprint();
    }

    public SettlementIdentity Settlement { get; }

    public DateTimeOffset CapturedAt { get; }

    public WorkforceSourceVersions SourceVersions { get; }

    public ImmutableArray<VillageWorkerProfile> Workers { get; }

    public ImmutableArray<ShopManagerTarget> Targets { get; }

    public ImmutableArray<CurrentShopManagerAssignment> CurrentAssignments
    {
        get;
    }

    public ImmutableArray<WorkforceDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKFORCE_SNAPSHOT|")
            .Append(SourceVersions.FingerprintSchemaVersion)
            .Append('\n')
            .Append(Settlement.StableKey).Append('\n')
            .Append(CapturedAt.UtcTicks).Append('\n')
            .Append(SourceVersions.StableKey).Append('\n');
        foreach (var worker in Workers)
        {
            canonical.Append("WORKER|")
                .Append(worker.Fingerprint).Append('\n');
        }

        foreach (var target in Targets)
        {
            canonical.Append("TARGET|")
                .Append(target.Fingerprint).Append('\n');
        }

        foreach (var assignment in CurrentAssignments)
        {
            canonical.Append("CURRENT|")
                .Append(assignment.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical.Append("DIAGNOSTIC|")
                .Append(diagnostic.StableKey).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }

    private IEnumerable<WorkforceProvenance> EnumerateProvenances()
    {
        foreach (var worker in Workers)
        {
            foreach (var fact in worker.Facts)
            {
                if (fact.Provenance is not null)
                {
                    yield return fact.Provenance;
                }

                foreach (var conflict in fact.Conflicts)
                {
                    yield return conflict.Provenance;
                }

                foreach (var reference in fact.Evidence)
                {
                    yield return reference.Provenance;
                }
            }

            foreach (var diagnostic in worker.Diagnostics)
            {
                foreach (var reference in diagnostic.Evidence)
                {
                    yield return reference.Provenance;
                }
            }
        }

        foreach (var target in Targets)
        {
            foreach (var reference in target.Evidence)
            {
                yield return reference.Provenance;
            }
        }

        foreach (var assignment in CurrentAssignments)
        {
            yield return assignment.Provenance;
        }

        foreach (var diagnostic in Diagnostics)
        {
            foreach (var reference in diagnostic.Evidence)
            {
                yield return reference.Provenance;
            }
        }
    }

    private static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> source,
        Func<T, string> keySelector,
        string itemName,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        var copied = source.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                $"A snapshot cannot contain a null {itemName}.",
                parameterName);
        }

        var duplicate = copied
            .GroupBy(keySelector, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Snapshot {itemName} {duplicate.Key} is duplicated.",
                parameterName);
        }

        return [.. copied.OrderBy(keySelector, StringComparer.Ordinal)];
    }
}
