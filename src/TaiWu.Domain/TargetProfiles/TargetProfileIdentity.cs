namespace TaiWu.Domain.TargetProfiles;

public sealed record TargetProfileVersion
{
    public TargetProfileVersion(string value)
    {
        Value = TargetProfileText.Version(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record TargetProfileFacetIdentity
{
    public TargetProfileFacetIdentity(
        TargetProfileDimension dimension,
        string code)
    {
        if (!Enum.IsDefined(dimension))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimension),
                dimension,
                "Unknown target-profile dimension.");
        }

        Dimension = dimension;
        Code = TargetProfileText.Code(code, nameof(code));
    }

    public TargetProfileDimension Dimension { get; }

    public string Code { get; }

    internal string StableKey => TargetProfileText.Stable(
        ((int)Dimension).ToString(System.Globalization.CultureInfo.InvariantCulture),
        Code);
}
