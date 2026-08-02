namespace TaiWu.Domain.CombatSkills;

public enum CatalogueFieldStatus
{
    Available = 0,
    Unavailable = 1,
    Unsupported = 2
}

public sealed record CatalogueField<T>
{
    private readonly T? _value;

    private CatalogueField(
        CatalogueFieldStatus status,
        T? value,
        string? reason,
        CatalogueSourceReference? source)
    {
        Status = status;
        _value = value;
        Reason = reason;
        Source = source;
    }

    public CatalogueFieldStatus Status { get; }

    public bool IsAvailable => Status == CatalogueFieldStatus.Available;

    public string? Reason { get; }

    public CatalogueSourceReference? Source { get; }

    public T Value => IsAvailable
        ? _value!
        : throw new InvalidOperationException(
            $"Catalogue field is {Status}: {Reason}");

    public static CatalogueField<T> Available(
        T value,
        CatalogueSourceReference source)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(source);
        return new CatalogueField<T>(
            CatalogueFieldStatus.Available,
            value,
            reason: null,
            source);
    }

    public static CatalogueField<T> Unavailable(
        string reason,
        CatalogueSourceReference? source = null)
    {
        return NonAvailable(
            CatalogueFieldStatus.Unavailable,
            reason,
            source);
    }

    public static CatalogueField<T> Unsupported(
        string reason,
        CatalogueSourceReference source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return NonAvailable(
            CatalogueFieldStatus.Unsupported,
            reason,
            source);
    }

    private static CatalogueField<T> NonAvailable(
        CatalogueFieldStatus status,
        string reason,
        CatalogueSourceReference? source)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A non-available catalogue field requires a reason.",
                nameof(reason));
        }

        return new CatalogueField<T>(
            status,
            value: default,
            reason.Trim(),
            source);
    }
}
