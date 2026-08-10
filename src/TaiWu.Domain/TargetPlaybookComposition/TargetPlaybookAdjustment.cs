using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookAdjustment
{
    internal TargetPlaybookAdjustment(
        string ruleCode,
        TargetPlaybookAdjustmentAction action,
        TargetPlaybookResponseReference? originalResponse,
        TargetPlaybookResponseReference? resultResponse,
        string reasonCode,
        IEnumerable<TargetPlaybookAdjustmentEvidence> evidence)
    {
        RuleCode = TargetProfileText.Code(ruleCode, nameof(ruleCode));
        ReasonCode = TargetProfileText.Code(reasonCode, nameof(reasonCode));
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        ArgumentNullException.ThrowIfNull(evidence);
        var evidenceValues = evidence
            .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
            .OrderBy(value => value.StableKey, StringComparer.Ordinal)
            .ToImmutableArray();
        if (evidenceValues.Length == 0)
        {
            throw new ArgumentException(
                "A target-specific adjustment requires exact evidence.",
                nameof(evidence));
        }

        EnsureEvidenceState(action, evidenceValues);
        Action = action;
        OriginalResponse = originalResponse;
        ResultResponse = resultResponse;
        Evidence = evidenceValues;
        StableKey = CreateStableKey();
    }

    public string RuleCode { get; }

    public TargetPlaybookAdjustmentAction Action { get; }

    public TargetPlaybookResponseReference? OriginalResponse { get; }

    public TargetPlaybookResponseReference? ResultResponse { get; }

    public string ReasonCode { get; }

    public ImmutableArray<TargetPlaybookAdjustmentEvidence> Evidence { get; }

    public string StableKey { get; }

    internal string TargetKey =>
        OriginalResponse?.StableKey ?? ResultResponse!.StableKey;

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_PLAYBOOK_ADJUSTMENT_V1",
            RuleCode,
            ((int)Action).ToString(CultureInfo.InvariantCulture),
            OriginalResponse?.StableKey ?? string.Empty,
            ResultResponse?.StableKey ?? string.Empty,
            ReasonCode,
            TargetProfileText.StableCollection(
                Evidence.Select(value => value.StableKey)));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void EnsureEvidenceState(
        TargetPlaybookAdjustmentAction action,
        ImmutableArray<TargetPlaybookAdjustmentEvidence> evidence)
    {
        var required = action switch
        {
            TargetPlaybookAdjustmentAction.Retained
                or TargetPlaybookAdjustmentAction.Elevated
                or TargetPlaybookAdjustmentAction.Added
                or TargetPlaybookAdjustmentAction.Replaced =>
                TargetPlaybookAdjustmentEvidenceState.Confirmed,
            TargetPlaybookAdjustmentAction.Reduced =>
                TargetPlaybookAdjustmentEvidenceState.Contrary,
            TargetPlaybookAdjustmentAction.Unresolved =>
                TargetPlaybookAdjustmentEvidenceState.Incomplete,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        if (!evidence.Any(value => value.State == required))
        {
            throw new ArgumentException(
                $"{action} requires at least one {required} exact-target "
                + "evidence item.",
                nameof(evidence));
        }
    }
}
