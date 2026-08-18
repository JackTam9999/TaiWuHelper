using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

public sealed record VillageWorkerDisplay
{
    public VillageWorkerDisplay(
        VillageWorkerIdentity identity,
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

    public VillageWorkerIdentity Identity { get; }

    public string? TraditionalChineseName { get; }

    public string? EnglishName { get; }

    public string? TraditionalChineseLocation { get; }

    public string? EnglishLocation { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
