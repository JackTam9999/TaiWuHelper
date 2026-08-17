using System.Collections.Immutable;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Application.CompanionCandidates;

public sealed record CompanionCandidateDisplay
{
    public CompanionCandidateDisplay(
        CandidateIdentity identity,
        string? traditionalChineseName,
        string? englishName,
        string? traditionalChineseLocation,
        string? englishLocation)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        TraditionalChineseName = Optional(traditionalChineseName);
        EnglishName = Optional(englishName);
        TraditionalChineseLocation = Optional(traditionalChineseLocation);
        EnglishLocation = Optional(englishLocation);
    }

    public CandidateIdentity Identity { get; }

    public string? TraditionalChineseName { get; }

    public string? EnglishName { get; }

    public string? TraditionalChineseLocation { get; }

    public string? EnglishLocation { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record CompanionDisciplineDisplayName
{
    public CompanionDisciplineDisplayName(
        CandidateDisciplineIdentity discipline,
        string? traditionalChineseName,
        string? englishName)
    {
        Discipline = discipline ?? throw new ArgumentNullException(nameof(discipline));
        TraditionalChineseName = Optional(traditionalChineseName);
        EnglishName = Optional(englishName);
    }

    public CandidateDisciplineIdentity Discipline { get; }

    public string? TraditionalChineseName { get; }

    public string? EnglishName { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum CompanionDisciplineDisplayStatus
{
    Complete = 0,
    Partial = 1,
    Unavailable = 2
}

public sealed class CompanionDisciplineDisplayResult
{
    public CompanionDisciplineDisplayResult(
        CompanionDisciplineDisplayStatus status,
        IEnumerable<CompanionDisciplineDisplayName> disciplines,
        string? failureIdentity = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown discipline-display status.");
        }

        ArgumentNullException.ThrowIfNull(disciplines);
        var values = disciplines.ToImmutableArray();
        if (values.Any(item => item is null)
            || values.GroupBy(item => item.Discipline).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Discipline display entries must be unique and non-null.",
                nameof(disciplines));
        }

        if (status == CompanionDisciplineDisplayStatus.Complete
            && values.Any(item => item.TraditionalChineseName is null
                || item.EnglishName is null))
        {
            throw new ArgumentException(
                "A complete discipline display requires both localized names.",
                nameof(disciplines));
        }

        if ((status == CompanionDisciplineDisplayStatus.Unavailable)
            != (failureIdentity is not null))
        {
            throw new ArgumentException(
                "Unavailable discipline display state requires one typed failure identity.",
                nameof(failureIdentity));
        }

        Status = status;
        Disciplines = [.. values
            .OrderBy(item => item.Discipline.Domain)
            .ThenBy(item => item.Discipline.Type)];
        FailureIdentity = failureIdentity;
    }

    public CompanionDisciplineDisplayStatus Status { get; }

    public ImmutableArray<CompanionDisciplineDisplayName> Disciplines { get; }

    public string? FailureIdentity { get; }
}

public interface ICompanionDisciplineDisplaySource
{
    Task<CompanionDisciplineDisplayResult> ReadAsync(
        CancellationToken cancellationToken = default);
}
