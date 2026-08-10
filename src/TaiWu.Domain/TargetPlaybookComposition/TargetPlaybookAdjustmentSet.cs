using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookAdjustmentSet
{
    internal TargetPlaybookAdjustmentSet(
        string profileFingerprint,
        string compositionKey,
        IEnumerable<TargetPlaybookAdjustmentEvidence> exactEvidence,
        IEnumerable<TargetPlaybookAdjustment> adjustments,
        IEnumerable<TargetPlaybookAdjustmentDiagnostic> diagnostics)
    {
        ProfileFingerprint = TargetProfileText.Fingerprint(
            profileFingerprint,
            nameof(profileFingerprint));
        CompositionKey = TargetProfileText.Fingerprint(
            compositionKey,
            nameof(compositionKey));
        ExactEvidence =
        [
            .. exactEvidence
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
        ];
        Adjustments =
        [
            .. adjustments
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.TargetKey, StringComparer.Ordinal)
                .ThenBy(value => value.StableKey, StringComparer.Ordinal)
        ];
        Diagnostics =
        [
            .. diagnostics
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
        ];
        StableKey = CreateStableKey();
    }

    public string ProfileFingerprint { get; }

    public string CompositionKey { get; }

    public ImmutableArray<TargetPlaybookAdjustmentEvidence> ExactEvidence
    { get; }

    public ImmutableArray<TargetPlaybookAdjustment> Adjustments { get; }

    public ImmutableArray<TargetPlaybookAdjustmentDiagnostic> Diagnostics
    { get; }

    public string StableKey { get; }

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_PLAYBOOK_ADJUSTMENT_SET_V1",
            ProfileFingerprint,
            CompositionKey,
            TargetProfileText.StableCollection(
                ExactEvidence.Select(value => value.StableKey)),
            TargetProfileText.StableCollection(
                Adjustments.Select(value => value.StableKey)),
            TargetProfileText.StableCollection(
                Diagnostics.Select(value => value.StableKey)));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
