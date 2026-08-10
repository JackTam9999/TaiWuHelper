using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookCompositionConflict
{
    internal TargetPlaybookCompositionConflict(
        TargetPlaybookCompositionConflictKind kind,
        string conflictGroup,
        IEnumerable<string> goalCodes,
        IEnumerable<string> optionCodes)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        ConflictGroup = TargetProfileText.Code(
            conflictGroup,
            nameof(conflictGroup));
        GoalCodes = CopyCodes(goalCodes, nameof(goalCodes));
        if (GoalCodes.Length < 2)
        {
            throw new ArgumentException(
                "A composition conflict requires at least two goals.",
                nameof(goalCodes));
        }

        OptionCodes = CopyCodes(
            optionCodes,
            nameof(optionCodes),
            requireValue: false);
        StableKey = CreateStableKey();
    }

    public TargetPlaybookCompositionConflictKind Kind { get; }

    public string ConflictGroup { get; }

    public ImmutableArray<string> GoalCodes { get; }

    public ImmutableArray<string> OptionCodes { get; }

    public string StableKey { get; }

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_PLAYBOOK_CONFLICT_V1",
            ((int)Kind).ToString(CultureInfo.InvariantCulture),
            ConflictGroup,
            TargetProfileText.StableCollection(GoalCodes),
            TargetProfileText.StableCollection(OptionCodes));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static ImmutableArray<string> CopyCodes(
        IEnumerable<string> values,
        string parameterName,
        bool requireValue = true)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var codes = values
            .Select(value => TargetProfileText.Code(value, parameterName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (requireValue && codes.Length == 0)
        {
            throw new ArgumentException(
                "A composition conflict requires stable references.",
                parameterName);
        }

        return codes;
    }
}
