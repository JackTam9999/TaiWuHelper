using System.Collections.Immutable;
using System.Globalization;

namespace TaiWu.Domain.CompanionCandidates;

public sealed class CandidateFactValue
{
    private readonly int _scalar;

    private CandidateFactValue(
        CandidateFactValueKind kind,
        int scalar,
        ImmutableArray<int> identities)
    {
        Kind = kind;
        _scalar = scalar;
        Identities = identities;
    }

    public CandidateFactValueKind Kind { get; }

    public ImmutableArray<int> Identities { get; }

    public bool BooleanValue => Kind == CandidateFactValueKind.Boolean
        ? _scalar != 0
        : throw WrongKind(CandidateFactValueKind.Boolean);

    public short Int16Value => Kind == CandidateFactValueKind.Int16
        ? checked((short)_scalar)
        : throw WrongKind(CandidateFactValueKind.Int16);

    public int Int32Value => Kind == CandidateFactValueKind.Int32
        ? _scalar
        : throw WrongKind(CandidateFactValueKind.Int32);

    public static CandidateFactValue Boolean(bool value) =>
        new(CandidateFactValueKind.Boolean, value ? 1 : 0, []);

    public static CandidateFactValue Int16(short value) =>
        new(CandidateFactValueKind.Int16, value, []);

    public static CandidateFactValue Int32(int value) =>
        new(CandidateFactValueKind.Int32, value, []);

    public static CandidateFactValue Int32Set(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.Any(value => value < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                "A saved identity cannot be negative.");
        }

        if (copied.Distinct().Count() != copied.Length)
        {
            throw new ArgumentException(
                "A saved identity set cannot contain duplicates.",
                nameof(values));
        }

        return new CandidateFactValue(
            CandidateFactValueKind.Int32Set,
            0,
            [.. copied.Order()]);
    }

    internal string StableKey => Kind == CandidateFactValueKind.Int32Set
        ? $"{CandidateProfileText.EnumKey(Kind)}:{string.Join(',', Identities.Select(value => value.ToString(CultureInfo.InvariantCulture)))}"
        : $"{CandidateProfileText.EnumKey(Kind)}:{_scalar.ToString(CultureInfo.InvariantCulture)}";

    private InvalidOperationException WrongKind(CandidateFactValueKind expected) =>
        new($"Candidate fact is {Kind}, not {expected}.");
}
