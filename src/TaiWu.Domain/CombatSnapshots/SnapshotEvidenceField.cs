using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotEvidenceField<T>
{
    private readonly T? _value;

    private SnapshotEvidenceField(
        SnapshotEvidenceStatus status,
        T? value,
        string? reasonCode,
        SnapshotFieldSource? source,
        ImmutableArray<SnapshotFieldObservation<T>> observations)
    {
        Status = status;
        _value = value;
        ReasonCode = reasonCode;
        Source = source;
        Observations = observations;
    }

    public SnapshotEvidenceStatus Status { get; }

    public bool IsAvailable => Status == SnapshotEvidenceStatus.Available;

    public string? ReasonCode { get; }

    public SnapshotFieldSource? Source { get; }

    public ImmutableArray<SnapshotFieldObservation<T>> Observations { get; }

    public T Value => IsAvailable
        ? _value!
        : throw new InvalidOperationException(
            $"Snapshot evidence is {Status}: {ReasonCode}");

    public static SnapshotEvidenceField<T> Available(
        T value,
        SnapshotFieldSource source)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(source);

        return new SnapshotEvidenceField<T>(
            SnapshotEvidenceStatus.Available,
            value,
            reasonCode: null,
            source,
            [new SnapshotFieldObservation<T>(value, source)]);
    }

    public static SnapshotEvidenceField<T> Unavailable(string reasonCode)
    {
        return new SnapshotEvidenceField<T>(
            SnapshotEvidenceStatus.Unavailable,
            value: default,
            ValidateReasonCode(reasonCode, nameof(reasonCode)),
            source: null,
            []);
    }

    public static SnapshotEvidenceField<T> Stale(
        string reasonCode,
        IEnumerable<SnapshotFieldObservation<T>> observations)
    {
        var values = ValidateAndOrderObservations(
            observations,
            minimumCount: 1);

        return new SnapshotEvidenceField<T>(
            SnapshotEvidenceStatus.Stale,
            value: default,
            ValidateReasonCode(reasonCode, nameof(reasonCode)),
            source: null,
            values);
    }

    public static SnapshotEvidenceField<T> Conflicting(
        string reasonCode,
        IEnumerable<SnapshotFieldObservation<T>> observations)
    {
        var values = ValidateAndOrderObservations(
            observations,
            minimumCount: 2);
        if (values
            .Select(observation => observation.Value)
            .Distinct(EqualityComparer<T>.Default)
            .Count() < 2)
        {
            throw new ArgumentException(
                "A conflict requires at least two distinct values.",
                nameof(observations));
        }

        return new SnapshotEvidenceField<T>(
            SnapshotEvidenceStatus.Conflicting,
            value: default,
            ValidateReasonCode(reasonCode, nameof(reasonCode)),
            source: null,
            values);
    }

    private static string ValidateReasonCode(
        string reasonCode,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException(
                "An unavailable, stale, or conflicting evidence field "
                + "requires a reason code.",
                parameterName);
        }

        var normalized = reasonCode.Trim();
        if (normalized[0] is not (>= 'A' and <= 'Z')
            || normalized.Any(character => character is not (
                >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '_')))
        {
            throw new ArgumentException(
                "An evidence reason code may contain only uppercase ASCII "
                + "letters, digits, and underscores, and must start with a "
                + "letter.",
                parameterName);
        }

        return normalized;
    }

    private static ImmutableArray<SnapshotFieldObservation<T>>
        ValidateAndOrderObservations(
            IEnumerable<SnapshotFieldObservation<T>> observations,
            int minimumCount)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var values = observations.ToImmutableArray();
        if (values.Length < minimumCount)
        {
            throw new ArgumentException(
                $"Evidence status requires at least {minimumCount} "
                + "observation(s).",
                nameof(observations));
        }

        if (values.Any(observation => observation is null))
        {
            throw new ArgumentException(
                "Evidence observations cannot contain null entries.",
                nameof(observations));
        }

        var fieldPaths = values
            .Select(observation => observation.Source.FieldPath)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (fieldPaths.Length != 1)
        {
            throw new ArgumentException(
                "Evidence observations must describe the same field path.",
                nameof(observations));
        }

        var duplicateSource = values
            .GroupBy(
                observation => new
                {
                    observation.Source.Source,
                    observation.Source.CapturedAtUtc,
                    observation.Source.EvidenceReference,
                    observation.Source.FieldPath
                })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSource is not null)
        {
            throw new ArgumentException(
                "Evidence observations cannot duplicate one source identity.",
                nameof(observations));
        }

        return
        [
            .. values
                .OrderBy(
                    observation => observation.Source.CapturedAtUtc)
                .ThenBy(observation => observation.Source.Source)
                .ThenBy(
                    observation => observation.Source.EvidenceReference,
                    StringComparer.Ordinal)
                .ThenBy(
                    observation => observation.Source.FieldPath,
                    StringComparer.Ordinal)
        ];
    }
}
