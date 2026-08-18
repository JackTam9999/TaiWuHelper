using System.Collections.Immutable;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

public sealed record VillageWorkerDisplay
{
    public VillageWorkerDisplay(
        VillageWorkerIdentity identity,
        string? traditionalChineseName,
        string? englishName,
        string? traditionalChineseLocation,
        string? englishLocation,
        VillageWorkerCapabilityDisplay? capability = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (capability is not null && capability.Identity != identity)
        {
            throw new ArgumentException(
                "Worker capability display identity must match the worker display.",
                nameof(capability));
        }

        TraditionalChineseName = Optional(traditionalChineseName);
        EnglishName = Optional(englishName);
        TraditionalChineseLocation = Optional(traditionalChineseLocation);
        EnglishLocation = Optional(englishLocation);
        Capability = capability;
    }

    public VillageWorkerIdentity Identity { get; }

    public string? TraditionalChineseName { get; }

    public string? EnglishName { get; }

    public string? TraditionalChineseLocation { get; }

    public string? EnglishLocation { get; }

    public VillageWorkerCapabilityDisplay? Capability { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record VillageWorkerCapabilityDisplay
{
    public const int MainAttributeCount = 6;
    public const int MartialDisciplineCount = 14;
    public const int LifeSkillDisciplineCount = 16;

    public VillageWorkerCapabilityDisplay(
        VillageWorkerIdentity identity,
        IEnumerable<short> mainAttributes,
        IEnumerable<short> martialDisciplines,
        IEnumerable<short> lifeSkillDisciplines)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        MainAttributes = Copy(
            mainAttributes,
            MainAttributeCount,
            nameof(mainAttributes));
        MartialDisciplines = Copy(
            martialDisciplines,
            MartialDisciplineCount,
            nameof(martialDisciplines));
        LifeSkillDisciplines = Copy(
            lifeSkillDisciplines,
            LifeSkillDisciplineCount,
            nameof(lifeSkillDisciplines));
    }

    public VillageWorkerIdentity Identity { get; }

    public ImmutableArray<short> MainAttributes { get; }

    public ImmutableArray<short> MartialDisciplines { get; }

    public ImmutableArray<short> LifeSkillDisciplines { get; }

    private static ImmutableArray<short> Copy(
        IEnumerable<short> values,
        int expectedCount,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copied = values.ToImmutableArray();
        if (copied.Length != expectedCount)
        {
            throw new ArgumentException(
                $"Capability display requires exactly {expectedCount} values.",
                parameterName);
        }

        return copied;
    }
}

public sealed record VillageWorkforceTargetDisplay
{
    public VillageWorkforceTargetDisplay(
        ShopManagerTargetIdentity identity,
        string? traditionalChineseBuildingName,
        string? englishBuildingName,
        string? traditionalChineseLocation,
        string? englishLocation,
        string? traditionalChineseDisciplineName,
        string? englishDisciplineName)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        TraditionalChineseBuildingName = Optional(traditionalChineseBuildingName);
        EnglishBuildingName = Optional(englishBuildingName);
        TraditionalChineseLocation = Optional(traditionalChineseLocation);
        EnglishLocation = Optional(englishLocation);
        TraditionalChineseDisciplineName = Optional(traditionalChineseDisciplineName);
        EnglishDisciplineName = Optional(englishDisciplineName);
    }

    public ShopManagerTargetIdentity Identity { get; }

    public string? TraditionalChineseBuildingName { get; }

    public string? EnglishBuildingName { get; }

    public string? TraditionalChineseLocation { get; }

    public string? EnglishLocation { get; }

    public string? TraditionalChineseDisciplineName { get; }

    public string? EnglishDisciplineName { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
