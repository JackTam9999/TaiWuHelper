using System.Collections.Immutable;

namespace TaiWu.Domain.TargetProfiles;

public sealed record TargetProfileMeasurement
{
    public TargetProfileMeasurement(
        string code,
        int value,
        string unitCode)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A confirmed target-profile measurement must be positive.");
        }

        Code = TargetProfileText.Code(code, nameof(code));
        Value = value;
        UnitCode = TargetProfileText.Code(unitCode, nameof(unitCode));
    }

    public string Code { get; }

    public int Value { get; }

    public string UnitCode { get; }

    internal string StableKey => TargetProfileText.Stable(
        Code,
        Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        UnitCode);
}

public sealed class TargetProfileFacetValue :
    IEquatable<TargetProfileFacetValue>
{
    private TargetProfileFacetValue(
        TargetProfileDimension dimension,
        string code,
        TargetProfileFacetValueKind kind,
        IEnumerable<TargetProfileMeasurement> measurements)
    {
        if (!Enum.IsDefined(dimension))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimension),
                dimension,
                "Unknown target-profile dimension.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown target-profile value kind.");
        }

        ArgumentNullException.ThrowIfNull(measurements);
        var values = measurements.ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Target-profile measurements cannot contain null entries.",
                nameof(measurements));
        }

        var duplicate = values
            .GroupBy(value => value.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Target-profile measurement {duplicate.Key} is duplicated.",
                nameof(measurements));
        }

        if (kind == TargetProfileFacetValueKind.Presence && values.Length != 0
            || kind == TargetProfileFacetValueKind.Measurements
            && values.Length == 0)
        {
            throw new ArgumentException(
                "Presence values cannot contain measurements and measurement "
                + "values require at least one measurement.",
                nameof(measurements));
        }

        Dimension = dimension;
        Code = TargetProfileText.Code(code, nameof(code));
        Kind = kind;
        Measurements = [.. values.OrderBy(value => value.Code,
            StringComparer.Ordinal)];
    }

    public TargetProfileDimension Dimension { get; }

    public string Code { get; }

    public TargetProfileFacetValueKind Kind { get; }

    public ImmutableArray<TargetProfileMeasurement> Measurements { get; }

    public static TargetProfileFacetValue Presence(
        TargetProfileDimension dimension,
        string code) => new(
            dimension,
            code,
            TargetProfileFacetValueKind.Presence,
            []);

    public static TargetProfileFacetValue Measured(
        TargetProfileDimension dimension,
        string code,
        IEnumerable<TargetProfileMeasurement> measurements) => new(
            dimension,
            code,
            TargetProfileFacetValueKind.Measurements,
            measurements);

    internal string StableKey => TargetProfileText.Stable(
        ((int)Dimension).ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        Code,
        ((int)Kind).ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        TargetProfileText.StableCollection(
            Measurements.Select(value => value.StableKey)));

    public bool Equals(TargetProfileFacetValue? other) =>
        ReferenceEquals(this, other)
        || other is not null
        && Dimension == other.Dimension
        && string.Equals(Code, other.Code, StringComparison.Ordinal)
        && Kind == other.Kind
        && Measurements.SequenceEqual(other.Measurements);

    public override bool Equals(object? obj) =>
        obj is TargetProfileFacetValue other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Dimension);
        hash.Add(Code, StringComparer.Ordinal);
        hash.Add(Kind);
        foreach (var measurement in Measurements)
        {
            hash.Add(measurement);
        }

        return hash.ToHashCode();
    }
}
