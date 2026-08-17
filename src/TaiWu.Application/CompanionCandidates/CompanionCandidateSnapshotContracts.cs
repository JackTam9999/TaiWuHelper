using System.Collections.Immutable;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Application.CompanionCandidates;

public enum CompanionCandidateSnapshotReadStatus
{
    Complete = 0,
    Partial = 1,
    SaveUnavailable = 2,
    UnsupportedVersion = 3,
    ChangedRevision = 4,
    ReadFailed = 5
}

public enum CompanionCandidateSnapshotWarningKind
{
    StandaloneEventRuntimeUnavailable = 0,
    StandaloneLiveRuntimeUnavailable = 1,
    ArchiveLoadWarning = 2
}

public enum CompanionCandidateSnapshotDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public sealed record CompanionCandidateSnapshotWarning
{
    public CompanionCandidateSnapshotWarning(
        CompanionCandidateSnapshotWarningKind kind,
        string message)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown candidate snapshot warning.");
        }

        Kind = kind;
        Message = Required(message, nameof(message));
    }

    public CompanionCandidateSnapshotWarningKind Kind { get; }

    public string Message { get; }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Snapshot warning text cannot be blank.", parameterName)
            : value.Trim();
}

public sealed record CompanionCandidateOmission
{
    public CompanionCandidateOmission(
        int? characterId,
        string reasonIdentity,
        string message)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "An omitted candidate ID must be positive when present.");
        }

        CharacterId = characterId;
        ReasonIdentity = Stable(reasonIdentity, nameof(reasonIdentity));
        Message = Required(message, nameof(message));
    }

    public int? CharacterId { get; }

    public string ReasonIdentity { get; }

    public string Message { get; }

    internal string StableKey => $"{CharacterId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NONE"}|{ReasonIdentity}";

    private static string Stable(string value, string parameterName)
    {
        var normalized = Required(value, parameterName);
        return normalized.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0
            ? throw new ArgumentException("A stable omission identity cannot contain delimiters or path separators.", parameterName)
            : normalized;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Omission text cannot be blank.", parameterName)
            : value.Trim();
}

public sealed record CompanionCandidateSnapshotDiagnostic
{
    public CompanionCandidateSnapshotDiagnostic(
        string identity,
        CompanionCandidateSnapshotDiagnosticSeverity severity,
        string message,
        CandidateIdentity? candidate = null)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown snapshot diagnostic severity.");
        }

        Identity = string.IsNullOrWhiteSpace(identity)
            || identity.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0
                ? throw new ArgumentException("A diagnostic requires a stable path-free identity.", nameof(identity))
                : identity.Trim();
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("A diagnostic requires safe message text.", nameof(message))
            : message.Trim();
        Severity = severity;
        Candidate = candidate;
    }

    public string Identity { get; }

    public CompanionCandidateSnapshotDiagnosticSeverity Severity { get; }

    public string Message { get; }

    public CandidateIdentity? Candidate { get; }

    internal string StableKey => $"{Identity}|{(int)Severity}|{Candidate?.CharacterId.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NONE"}";
}

public sealed class CompanionCandidateSnapshot
{
    public CompanionCandidateSnapshot(
        DateTimeOffset capturedAt,
        CandidateProfileSourceVersions sourceVersions,
        IEnumerable<CandidateProfile> profiles,
        IEnumerable<CompanionCandidateOmission> omissions,
        IEnumerable<CompanionCandidateSnapshotWarning> warnings,
        IEnumerable<CompanionCandidateSnapshotDiagnostic> diagnostics)
    {
        SourceVersions = sourceVersions ?? throw new ArgumentNullException(nameof(sourceVersions));
        CapturedAtUtc = capturedAt.ToUniversalTime();
        Profiles = CopyProfiles(profiles);
        Omissions = CopyUnique(
            omissions,
            item => item.StableKey,
            "Snapshot omissions cannot contain null or duplicate entries.",
            nameof(omissions));
        Warnings = CopyUnique(
            warnings,
            item => $"{(int)item.Kind}|{item.Message}",
            "Snapshot warnings cannot contain null or duplicate entries.",
            nameof(warnings));
        Diagnostics = CopyUnique(
            diagnostics,
            item => item.StableKey,
            "Snapshot diagnostics cannot contain null or duplicate entries.",
            nameof(diagnostics));

        if (Profiles.Any(profile => profile.SourceVersions != SourceVersions))
        {
            throw new ArgumentException(
                "Every candidate profile must share the snapshot source versions.",
                nameof(profiles));
        }
    }

    public DateTimeOffset CapturedAtUtc { get; }

    public CandidateProfileSourceVersions SourceVersions { get; }

    public ImmutableArray<CandidateProfile> Profiles { get; }

    public ImmutableArray<CompanionCandidateOmission> Omissions { get; }

    public ImmutableArray<CompanionCandidateSnapshotWarning> Warnings { get; }

    public ImmutableArray<CompanionCandidateSnapshotDiagnostic> Diagnostics { get; }

    private static ImmutableArray<CandidateProfile> CopyProfiles(
        IEnumerable<CandidateProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var copied = profiles.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(item => item.Identity.CharacterId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Snapshot profiles cannot contain null or duplicate candidate identities.",
                nameof(profiles));
        }

        return [.. copied.OrderBy(item => item.Identity.CharacterId)];
    }

    private static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> values,
        Func<T, string> stableKey,
        string error,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(stableKey, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(error, parameterName);
        }

        return [.. copied.OrderBy(stableKey, StringComparer.Ordinal)];
    }
}

public sealed class CompanionCandidateSnapshotReadResult
{
    private CompanionCandidateSnapshotReadResult(
        CompanionCandidateSnapshotReadStatus status,
        CompanionCandidateSnapshot? snapshot,
        string? failureIdentity,
        string? failureMessage)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown snapshot read status.");
        }

        var isSuccess = status is CompanionCandidateSnapshotReadStatus.Complete
            or CompanionCandidateSnapshotReadStatus.Partial;
        if (isSuccess != (snapshot is not null)
            || isSuccess == (failureIdentity is not null || failureMessage is not null))
        {
            throw new ArgumentException("Snapshot read state and payload are incompatible.");
        }

        Status = status;
        Snapshot = snapshot;
        FailureIdentity = failureIdentity;
        FailureMessage = failureMessage;
    }

    public CompanionCandidateSnapshotReadStatus Status { get; }

    public CompanionCandidateSnapshot? Snapshot { get; }

    public string? FailureIdentity { get; }

    public string? FailureMessage { get; }

    public static CompanionCandidateSnapshotReadResult Complete(
        CompanionCandidateSnapshot snapshot) =>
        new(CompanionCandidateSnapshotReadStatus.Complete, snapshot, null, null);

    public static CompanionCandidateSnapshotReadResult Partial(
        CompanionCandidateSnapshot snapshot) =>
        new(CompanionCandidateSnapshotReadStatus.Partial, snapshot, null, null);

    public static CompanionCandidateSnapshotReadResult Failed(
        CompanionCandidateSnapshotReadStatus status,
        string failureIdentity,
        string failureMessage)
    {
        if (status is CompanionCandidateSnapshotReadStatus.Complete
            or CompanionCandidateSnapshotReadStatus.Partial)
        {
            throw new ArgumentException("A successful status cannot create a failed read result.", nameof(status));
        }

        if (string.IsNullOrWhiteSpace(failureIdentity)
            || string.IsNullOrWhiteSpace(failureMessage))
        {
            throw new ArgumentException("A failed snapshot read requires safe identity and message text.");
        }

        return new CompanionCandidateSnapshotReadResult(
            status,
            null,
            failureIdentity.Trim(),
            failureMessage.Trim());
    }
}
