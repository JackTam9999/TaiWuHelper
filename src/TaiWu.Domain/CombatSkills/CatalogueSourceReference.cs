namespace TaiWu.Domain.CombatSkills;

public enum CatalogueSourceKind
{
    GameData = 0,
    TraditionalChineseLanguageResource = 1,
    EnglishLanguageResource = 2,
    VerifiedRule = 3
}

public sealed record CatalogueSourceReference
{
    public CatalogueSourceReference(
        CatalogueSourceKind kind,
        string sourceIdentity,
        string recordIdentity)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown catalogue source kind.");
        }

        Kind = kind;
        SourceIdentity = ValidateOpaqueIdentity(
            sourceIdentity,
            nameof(sourceIdentity));
        RecordIdentity = ValidateOpaqueIdentity(
            recordIdentity,
            nameof(recordIdentity));
    }

    public CatalogueSourceKind Kind { get; }

    public string SourceIdentity { get; }

    public string RecordIdentity { get; }

    private static string ValidateOpaqueIdentity(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A catalogue source identity cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Catalogue source identities must be opaque identifiers, "
                + "not filesystem paths.",
                parameterName);
        }

        return normalized;
    }
}
