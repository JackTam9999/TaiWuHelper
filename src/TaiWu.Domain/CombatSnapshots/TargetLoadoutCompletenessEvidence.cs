namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetLoadoutCompletenessEvidence
{
    public const string E3000RuleId =
        "TAIWU-CNH-TARGET-LOADOUT-1.0.0-68032f25";

    public const string E3000GameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

    public const string E3000LanguageCode = "CNH";

    public const string E3000EvidenceReference = "E3-000-CAP-002";

    private TargetLoadoutCompletenessEvidence(
        string detectedGameDataVersion)
    {
        RuleId = E3000RuleId;
        SupportedGameDataVersion = E3000GameDataVersion;
        DetectedGameDataVersion = detectedGameDataVersion;
        LanguageCode = E3000LanguageCode;
        EvidenceReference = E3000EvidenceReference;
        ObservationContext = TargetObservationContext.Sparring;
    }

    public string RuleId { get; }

    public string SupportedGameDataVersion { get; }

    public string DetectedGameDataVersion { get; }

    public string LanguageCode { get; }

    public string EvidenceReference { get; }

    public TargetObservationContext ObservationContext { get; }

    public static TargetLoadoutCompletenessEvidence FromE3000(
        string detectedGameDataVersion)
    {
        if (string.IsNullOrWhiteSpace(detectedGameDataVersion))
        {
            throw new ArgumentException(
                "Completeness evidence requires a detected GameData version.",
                nameof(detectedGameDataVersion));
        }

        var normalizedVersion = detectedGameDataVersion.Trim();
        if (!string.Equals(
                normalizedVersion,
                E3000GameDataVersion,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "E3-000 completeness evidence does not support the detected "
                + $"GameData version '{normalizedVersion}'.",
                nameof(detectedGameDataVersion));
        }

        return new TargetLoadoutCompletenessEvidence(normalizedVersion);
    }
}
