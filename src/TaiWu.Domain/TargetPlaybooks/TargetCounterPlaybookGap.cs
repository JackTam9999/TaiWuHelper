using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed class TargetCounterPlaybookGap
{
    public TargetCounterPlaybookGap(
        string code,
        TargetCounterPlaybookGapKind kind,
        string localizedMessageKey,
        IEnumerable<string> evidenceReferences,
        string? relatedCounterCode = null)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown playbook-gap kind.");
        }

        LocalizedMessageKey = TargetProfileText.ResourceKey(
            localizedMessageKey,
            nameof(localizedMessageKey));
        RelatedCounterCode = relatedCounterCode is null
            ? null
            : TargetProfileText.Code(
                relatedCounterCode,
                nameof(relatedCounterCode));
        if (kind == TargetCounterPlaybookGapKind.InaccessibleVerifiedOption
            && RelatedCounterCode is null)
        {
            throw new ArgumentException(
                "An inaccessible-option gap requires a counter code.",
                nameof(relatedCounterCode));
        }

        ArgumentNullException.ThrowIfNull(evidenceReferences);
        var references = evidenceReferences
            .Select(value => TargetProfileText.Code(
                value,
                nameof(evidenceReferences)))
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "A playbook gap requires evidence.",
                nameof(evidenceReferences));
        }

        if (references.Distinct(StringComparer.Ordinal).Count()
            != references.Length)
        {
            throw new ArgumentException(
                "Playbook-gap evidence references must be unique.",
                nameof(evidenceReferences));
        }

        Kind = kind;
        EvidenceReferences = [.. references.Order(StringComparer.Ordinal)];
    }

    public string Code { get; }

    public TargetCounterPlaybookGapKind Kind { get; }

    public string LocalizedMessageKey { get; }

    public string? RelatedCounterCode { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public string StableKey => Code;

    internal string ContentKey => TargetProfileText.Stable(
        Code,
        ((int)Kind).ToString(CultureInfo.InvariantCulture),
        LocalizedMessageKey,
        RelatedCounterCode ?? string.Empty,
        TargetProfileText.StableCollection(EvidenceReferences));
}
