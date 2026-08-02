using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Application.CombatSkills;

public sealed record CombatSkillCatalogueSourceIdentity
{
    public CombatSkillCatalogueSourceIdentity(
        string gameDataVersion,
        int importerVersion,
        string gameDataFingerprint,
        string traditionalChineseFingerprint,
        string englishFingerprint)
    {
        if (string.IsNullOrWhiteSpace(gameDataVersion))
        {
            throw new ArgumentException(
                "A GameData version is required.",
                nameof(gameDataVersion));
        }

        GameDataVersion = gameDataVersion.Trim();
        if (importerVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(importerVersion),
                importerVersion,
                "An importer version must be positive.");
        }

        ImporterVersion = importerVersion;
        GameDataFingerprint = ValidateFingerprint(
            gameDataFingerprint,
            nameof(gameDataFingerprint));
        TraditionalChineseFingerprint = ValidateFingerprint(
            traditionalChineseFingerprint,
            nameof(traditionalChineseFingerprint));
        EnglishFingerprint = ValidateFingerprint(
            englishFingerprint,
            nameof(englishFingerprint));
    }

    public string GameDataVersion { get; }

    public int ImporterVersion { get; }

    public string GameDataFingerprint { get; }

    public string TraditionalChineseFingerprint { get; }

    public string EnglishFingerprint { get; }

    private static string ValidateFingerprint(
        string value,
        string parameterName)
    {
        if (value is null
            || value.Length != 64
            || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A source fingerprint must be a 64-character SHA-256 value.",
                parameterName);
        }

        return value.ToUpperInvariant();
    }
}

public enum CombatSkillImportDiagnosticSeverity
{
    Warning = 0,
    Error = 1
}

public sealed record CombatSkillImportDiagnostic
{
    public CombatSkillImportDiagnostic(
        CombatSkillImportDiagnosticSeverity severity,
        string code,
        string sourceRecordIdentity,
        string reason)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unknown import diagnostic severity.");
        }

        Severity = severity;
        Code = ValidateOpaque(code, nameof(code));
        SourceRecordIdentity = ValidateOpaque(
            sourceRecordIdentity,
            nameof(sourceRecordIdentity));
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException(
                "An import diagnostic requires a reason.",
                nameof(reason))
            : reason.Trim();
    }

    public CombatSkillImportDiagnosticSeverity Severity { get; }

    public string Code { get; }

    public string SourceRecordIdentity { get; }

    public string Reason { get; }

    private static string ValidateOpaque(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "An opaque diagnostic identity is required.",
                parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Diagnostic identities cannot contain filesystem paths.",
                parameterName);
        }

        return normalized;
    }
}

public enum DefinitionSourceReadStatus
{
    Available = 0,
    MissingSources = 1,
    UnsupportedVersion = 2,
    Failed = 3
}

public sealed record CombatSkillDefinitionSourceResult
{
    private CombatSkillDefinitionSourceResult(
        DefinitionSourceReadStatus status,
        CombatSkillCatalogueSourceIdentity? sourceIdentity,
        ImmutableArray<CombatSkillDefinition> definitions,
        ImmutableArray<CombatSkillImportDiagnostic> diagnostics,
        string? reason)
    {
        Status = status;
        SourceIdentity = sourceIdentity;
        Definitions = definitions;
        Diagnostics = diagnostics;
        Reason = reason;
    }

    public DefinitionSourceReadStatus Status { get; }

    public CombatSkillCatalogueSourceIdentity? SourceIdentity { get; }

    public ImmutableArray<CombatSkillDefinition> Definitions { get; }

    public ImmutableArray<CombatSkillImportDiagnostic> Diagnostics { get; }

    public string? Reason { get; }

    public static CombatSkillDefinitionSourceResult Available(
        CombatSkillCatalogueSourceIdentity sourceIdentity,
        IEnumerable<CombatSkillDefinition> definitions,
        IEnumerable<CombatSkillImportDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentity);
        ArgumentNullException.ThrowIfNull(definitions);
        var values = definitions.ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Definitions cannot contain null.",
                nameof(definitions));
        }

        if (values.GroupBy(value => value.SkillId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Definition-source results cannot contain duplicate skill IDs.",
                nameof(definitions));
        }

        values = values.OrderBy(value => value.SkillId).ToImmutableArray();
        var diagnosticValues = (diagnostics ?? []).ToImmutableArray();
        if (diagnosticValues.Any(value => value is null))
        {
            throw new ArgumentException(
                "Diagnostics cannot contain null.",
                nameof(diagnostics));
        }
        diagnosticValues = diagnosticValues
            .OrderBy(value => value.SourceRecordIdentity, StringComparer.Ordinal)
            .ThenBy(value => value.Code, StringComparer.Ordinal)
            .ToImmutableArray();

        return new CombatSkillDefinitionSourceResult(
            DefinitionSourceReadStatus.Available,
            sourceIdentity,
            values,
            diagnosticValues,
            reason: null);
    }

    public static CombatSkillDefinitionSourceResult MissingSources(string reason) =>
        Failure(DefinitionSourceReadStatus.MissingSources, reason);

    public static CombatSkillDefinitionSourceResult UnsupportedVersion(
        string reason) => Failure(
            DefinitionSourceReadStatus.UnsupportedVersion,
            reason);

    public static CombatSkillDefinitionSourceResult Failed(string reason) =>
        Failure(DefinitionSourceReadStatus.Failed, reason);

    private static CombatSkillDefinitionSourceResult Failure(
        DefinitionSourceReadStatus status,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A source-read failure requires a reason.",
                nameof(reason));
        }

        return new CombatSkillDefinitionSourceResult(
            status,
            sourceIdentity: null,
            definitions: [],
            diagnostics: [],
            reason.Trim());
    }
}

public enum CatalogueRepositoryState
{
    Missing = 0,
    Ready = 1,
    Corrupt = 2,
    Failed = 3
}

public sealed record CombatSkillCatalogueRepositorySnapshot
{
    public CombatSkillCatalogueRepositorySnapshot(
        CatalogueRepositoryState state,
        CombatSkillCatalogueSourceIdentity? sourceIdentity,
        int definitionCount,
        DateTimeOffset? builtAtUtc,
        string? reason = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown catalogue repository state.");
        }

        if (definitionCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(definitionCount),
                definitionCount,
                "Definition count cannot be negative.");
        }

        if (state == CatalogueRepositoryState.Ready
            && (sourceIdentity is null || builtAtUtc is null))
        {
            throw new ArgumentException(
                "A ready catalogue requires source identity and build time.",
                nameof(sourceIdentity));
        }

        if (state is CatalogueRepositoryState.Corrupt
                or CatalogueRepositoryState.Failed
            && string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A corrupt or failed catalogue requires a reason.",
                nameof(reason));
        }

        State = state;
        SourceIdentity = sourceIdentity;
        DefinitionCount = definitionCount;
        BuiltAtUtc = builtAtUtc?.ToUniversalTime();
        Reason = reason?.Trim();
    }

    public CatalogueRepositoryState State { get; }

    public CombatSkillCatalogueSourceIdentity? SourceIdentity { get; }

    public int DefinitionCount { get; }

    public DateTimeOffset? BuiltAtUtc { get; }

    public string? Reason { get; }
}

public sealed record CatalogueReplaceResult
{
    private CatalogueReplaceResult(bool succeeded, string? reason)
    {
        Succeeded = succeeded;
        Reason = reason;
    }

    public bool Succeeded { get; }

    public string? Reason { get; }

    public static CatalogueReplaceResult Success() => new(true, null);

    public static CatalogueReplaceResult Failure(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A replacement failure requires a reason.",
                nameof(reason));
        }

        return new CatalogueReplaceResult(false, reason.Trim());
    }
}

public enum CombatSkillCatalogueStatus
{
    Current = 0,
    Missing = 1,
    Stale = 2,
    MissingSources = 3,
    UnsupportedVersion = 4,
    SourceReadFailed = 5,
    RepositoryFailed = 6,
    Corrupt = 7,
    Rebuilding = 8
}

public sealed record CombatSkillCatalogueStatusResult(
    CombatSkillCatalogueStatus Status,
    int DefinitionCount,
    CombatSkillCatalogueSourceIdentity? InstalledSource,
    CombatSkillCatalogueSourceIdentity? StoredSource,
    DateTimeOffset? BuiltAtUtc,
    string? Reason);

public enum EnsureCombatSkillCatalogueStatus
{
    Current = 0,
    Rebuilt = 1,
    MissingSources = 2,
    UnsupportedVersion = 3,
    SourceReadFailed = 4,
    RebuildFailed = 5
}

public enum CatalogueRecoveryStatus
{
    None = 0,
    StaleCataloguePreserved = 1,
    CorruptCatalogueRemains = 2,
    RepositoryUnavailable = 3
}

public sealed record EnsureCombatSkillCatalogueResult(
    EnsureCombatSkillCatalogueStatus Status,
    int DefinitionCount,
    CombatSkillCatalogueSourceIdentity? SourceIdentity,
    string? Reason,
    CatalogueRecoveryStatus RecoveryStatus = CatalogueRecoveryStatus.None,
    CombatSkillCatalogueSourceIdentity? RetainedSourceIdentity = null,
    int RetainedDefinitionCount = 0,
    DateTimeOffset? RetainedBuiltAtUtc = null);

public enum CharacterProgressReadStatus
{
    NotRead = 0,
    Available = 1,
    SaveMissing = 2,
    SaveReadFailed = 3,
    UnsupportedVersion = 4
}

public sealed record CharacterCombatSkillProgressWarning
{
    public CharacterCombatSkillProgressWarning(string code, string reason)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "A progress warning requires a code.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A progress warning requires a reason.",
                nameof(reason));
        }

        Code = code.Trim();
        Reason = reason.Trim();
    }

    public string Code { get; }

    public string Reason { get; }
}

public sealed record CharacterCombatSkillProgressMetadata
{
    public CharacterCombatSkillProgressMetadata(
        SaveSnapshotIdentity saveSnapshot,
        string gameDataVersion,
        IEnumerable<CharacterCombatSkillProgressWarning>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(saveSnapshot);
        if (string.IsNullOrWhiteSpace(gameDataVersion))
        {
            throw new ArgumentException(
                "A progress snapshot requires a GameData version.",
                nameof(gameDataVersion));
        }

        var warningValues = (warnings ?? []).ToImmutableArray();
        if (warningValues.Any(value => value is null))
        {
            throw new ArgumentException(
                "Progress warnings cannot contain null.",
                nameof(warnings));
        }

        SaveSnapshot = saveSnapshot;
        GameDataVersion = gameDataVersion.Trim();
        Warnings = warningValues;
    }

    public SaveSnapshotIdentity SaveSnapshot { get; }

    public string GameDataVersion { get; }

    public ImmutableArray<CharacterCombatSkillProgressWarning> Warnings { get; }
}

public sealed record CharacterCombatSkillProgressReadResult
{
    private CharacterCombatSkillProgressReadResult(
        CharacterProgressReadStatus status,
        CharacterCombatSkillProgressMetadata? metadata,
        ImmutableArray<CharacterCombatSkillProgress> progress,
        string? reason)
    {
        Status = status;
        Metadata = metadata;
        Progress = progress;
        Reason = reason;
    }

    public CharacterProgressReadStatus Status { get; }

    public CharacterCombatSkillProgressMetadata? Metadata { get; }

    public ImmutableArray<CharacterCombatSkillProgress> Progress { get; }

    public string? Reason { get; }

    public static CharacterCombatSkillProgressReadResult Available(
        CharacterCombatSkillProgressMetadata metadata,
        IEnumerable<CharacterCombatSkillProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(progress);
        var values = progress.ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Character progress cannot contain null.",
                nameof(progress));
        }

        if (values.GroupBy(value => value.SkillId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Character progress cannot contain duplicate skill IDs.",
                nameof(progress));
        }

        values = values.OrderBy(value => value.SkillId).ToImmutableArray();
        if (values.Any(value => value.SaveSnapshot != metadata.SaveSnapshot))
        {
            throw new ArgumentException(
                "Character progress must use the result snapshot identity.",
                nameof(progress));
        }

        if (values.Select(value => value.CharacterId).Distinct().Skip(1).Any())
        {
            throw new ArgumentException(
                "Character progress must describe one character.",
                nameof(progress));
        }

        return new CharacterCombatSkillProgressReadResult(
            CharacterProgressReadStatus.Available,
            metadata,
            values,
            reason: null);
    }

    public static CharacterCombatSkillProgressReadResult SaveMissing(
        string reason) => Failure(CharacterProgressReadStatus.SaveMissing, reason);

    public static CharacterCombatSkillProgressReadResult SaveReadFailed(
        string reason) => Failure(
            CharacterProgressReadStatus.SaveReadFailed,
            reason);

    public static CharacterCombatSkillProgressReadResult UnsupportedVersion(
        string reason) => Failure(
            CharacterProgressReadStatus.UnsupportedVersion,
            reason);

    private static CharacterCombatSkillProgressReadResult Failure(
        CharacterProgressReadStatus status,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A progress-read failure requires a reason.",
                nameof(reason));
        }

        return new CharacterCombatSkillProgressReadResult(
            status,
            metadata: null,
            progress: [],
            reason.Trim());
    }
}
