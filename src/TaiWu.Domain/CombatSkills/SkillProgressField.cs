using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSkills;

public enum SkillProgressSourceKind
{
    SaveSnapshot = 0,
    CurrentScreenObservation = 1,
    VerifiedRule = 2
}

public sealed record SkillProgressSource
{
    public SkillProgressSource(
        SkillProgressSourceKind kind,
        string sourceIdentity,
        string fieldIdentity)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown skill-progress source kind.");
        }

        Kind = kind;
        SourceIdentity = ValidateIdentity(
            sourceIdentity,
            nameof(sourceIdentity));
        FieldIdentity = ValidateIdentity(
            fieldIdentity,
            nameof(fieldIdentity));
    }

    public SkillProgressSourceKind Kind { get; }

    public string SourceIdentity { get; }

    public string FieldIdentity { get; }

    private static string ValidateIdentity(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A skill-progress source identity cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Skill-progress source identities must be opaque, not paths.",
                parameterName);
        }

        return normalized;
    }
}

public sealed record SkillProgressObservation<T>
{
    public SkillProgressObservation(T value, SkillProgressSource source)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(source);
        Value = value;
        Source = source;
    }

    public T Value { get; }

    public SkillProgressSource Source { get; }
}

public enum SkillProgressFieldStatus
{
    Available = 0,
    Unavailable = 1,
    Conflicting = 2
}

public sealed record SkillProgressField<T>
{
    private readonly T? _value;

    private SkillProgressField(
        SkillProgressFieldStatus status,
        T? value,
        string? reason,
        SkillProgressSource? source,
        ImmutableArray<SkillProgressObservation<T>> observations)
    {
        Status = status;
        _value = value;
        Reason = reason;
        Source = source;
        Observations = observations;
    }

    public SkillProgressFieldStatus Status { get; }

    public bool IsAvailable => Status == SkillProgressFieldStatus.Available;

    public string? Reason { get; }

    public SkillProgressSource? Source { get; }

    public ImmutableArray<SkillProgressObservation<T>> Observations { get; }

    public T Value => IsAvailable
        ? _value!
        : throw new InvalidOperationException(
            $"Skill-progress field is {Status}: {Reason}");

    public static SkillProgressField<T> Available(
        T value,
        SkillProgressSource source)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(source);
        return new SkillProgressField<T>(
            SkillProgressFieldStatus.Available,
            value,
            reason: null,
            source,
            [new SkillProgressObservation<T>(value, source)]);
    }

    public static SkillProgressField<T> Unavailable(
        string reason,
        SkillProgressSource? source = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "An unavailable skill-progress field requires a reason.",
                nameof(reason));
        }

        return new SkillProgressField<T>(
            SkillProgressFieldStatus.Unavailable,
            value: default,
            reason.Trim(),
            source,
            []);
    }

    public static SkillProgressField<T> Conflicting(
        string reason,
        IEnumerable<SkillProgressObservation<T>> observations)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A conflicting skill-progress field requires a reason.",
                nameof(reason));
        }

        ArgumentNullException.ThrowIfNull(observations);
        var values = observations.ToImmutableArray();
        if (values.Length < 2)
        {
            throw new ArgumentException(
                "A conflict requires at least two observations.",
                nameof(observations));
        }

        if (values.Any(observation => observation is null))
        {
            throw new ArgumentException(
                "Conflict observations cannot contain null.",
                nameof(observations));
        }

        return new SkillProgressField<T>(
            SkillProgressFieldStatus.Conflicting,
            value: default,
            reason.Trim(),
            source: null,
            values);
    }
}
