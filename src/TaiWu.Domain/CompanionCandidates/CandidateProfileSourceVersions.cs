namespace TaiWu.Domain.CompanionCandidates;

public sealed record CandidateProfileSourceVersions
{
    public CandidateProfileSourceVersions(
        string saveSha256,
        string gameDataVersion,
        string profileMappingVersion,
        string disciplineCatalogVersion,
        string fingerprintSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(saveSha256)
            || saveSha256.Length != 64
            || !saveSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Save SHA-256 must contain exactly 64 hexadecimal characters.",
                nameof(saveSha256));
        }

        SaveSha256 = saveSha256.ToUpperInvariant();
        GameDataVersion = CandidateProfileText.Stable(gameDataVersion, nameof(gameDataVersion));
        ProfileMappingVersion = CandidateProfileText.Stable(profileMappingVersion, nameof(profileMappingVersion));
        DisciplineCatalogVersion = CandidateProfileText.Stable(disciplineCatalogVersion, nameof(disciplineCatalogVersion));
        FingerprintSchemaVersion = CandidateProfileText.Stable(fingerprintSchemaVersion, nameof(fingerprintSchemaVersion));
    }

    public string SaveSha256 { get; }

    public string GameDataVersion { get; }

    public string ProfileMappingVersion { get; }

    public string DisciplineCatalogVersion { get; }

    public string FingerprintSchemaVersion { get; }

    internal string StableKey => string.Join('|',
        SaveSha256,
        GameDataVersion,
        ProfileMappingVersion,
        DisciplineCatalogVersion,
        FingerprintSchemaVersion);
}
