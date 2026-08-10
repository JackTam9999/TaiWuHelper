using System.Collections.Immutable;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookAdjustmentDiagnostic
{
    internal TargetPlaybookAdjustmentDiagnostic(
        string code,
        string ruleCode,
        IEnumerable<string> evidenceIdentities)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        RuleCode = TargetProfileText.Code(ruleCode, nameof(ruleCode));
        EvidenceIdentities =
        [
            .. evidenceIdentities
                .Select(value => TargetProfileText.Code(
                    value,
                    nameof(evidenceIdentities)))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
    }

    public string Code { get; }

    public string RuleCode { get; }

    public ImmutableArray<string> EvidenceIdentities { get; }

    internal string StableKey => TargetProfileText.Stable(
        Code,
        RuleCode,
        TargetProfileText.StableCollection(EvidenceIdentities));
}
