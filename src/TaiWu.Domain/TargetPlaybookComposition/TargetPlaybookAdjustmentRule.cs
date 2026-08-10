using System.Collections.Immutable;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookAdjustmentRule
{
    public TargetPlaybookAdjustmentRule(
        string code,
        TargetPlaybookAdjustmentAction action,
        TargetPlaybookResponseReference? originalResponse,
        TargetPlaybookResponseReference? resultResponse,
        string reasonCode,
        IEnumerable<string> requiredEvidenceIdentities)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        ReasonCode = TargetProfileText.Code(
            reasonCode,
            nameof(reasonCode));
        EnsureReferences(action, originalResponse, resultResponse);
        ArgumentNullException.ThrowIfNull(requiredEvidenceIdentities);
        var identities = requiredEvidenceIdentities
            .Select(value => TargetProfileText.Code(
                value,
                nameof(requiredEvidenceIdentities)))
            .ToImmutableArray();
        if (identities.Length == 0)
        {
            throw new ArgumentException(
                "An adjustment rule requires exact evidence identities.",
                nameof(requiredEvidenceIdentities));
        }

        if (identities.Distinct(StringComparer.Ordinal).Count()
            != identities.Length)
        {
            throw new ArgumentException(
                "Adjustment-rule evidence identities must be unique.",
                nameof(requiredEvidenceIdentities));
        }

        Action = action;
        OriginalResponse = originalResponse;
        ResultResponse = resultResponse;
        RequiredEvidenceIdentities =
            [.. identities.Order(StringComparer.Ordinal)];
    }

    public string Code { get; }

    public TargetPlaybookAdjustmentAction Action { get; }

    public TargetPlaybookResponseReference? OriginalResponse { get; }

    public TargetPlaybookResponseReference? ResultResponse { get; }

    public string ReasonCode { get; }

    public ImmutableArray<string> RequiredEvidenceIdentities { get; }

    private static void EnsureReferences(
        TargetPlaybookAdjustmentAction action,
        TargetPlaybookResponseReference? original,
        TargetPlaybookResponseReference? result)
    {
        switch (action)
        {
            case TargetPlaybookAdjustmentAction.Retained:
            case TargetPlaybookAdjustmentAction.Elevated:
            case TargetPlaybookAdjustmentAction.Reduced:
            case TargetPlaybookAdjustmentAction.Unresolved:
                if (original is null || result is not null)
                {
                    throw new ArgumentException(
                        $"{action} requires one original response and no "
                        + "replacement.");
                }

                break;
            case TargetPlaybookAdjustmentAction.Added:
                if (original is not null || result is null)
                {
                    throw new ArgumentException(
                        "Added requires one new response and no original.");
                }

                break;
            case TargetPlaybookAdjustmentAction.Replaced:
                if (original is null
                    || result is null
                    || string.Equals(
                        original.StableKey,
                        result.StableKey,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Replaced requires distinct original and replacement "
                        + "responses.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}
