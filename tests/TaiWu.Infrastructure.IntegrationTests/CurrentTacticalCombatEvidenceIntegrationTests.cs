using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class CurrentTacticalCombatEvidenceIntegrationTests(
    ITestOutputHelper output)
{
    private const string EvidenceVariable =
        "TAIWU_INTEGRATION_CURRENT_TACTICAL_EVIDENCE";
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";
    private const string ExpectedGameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";
    private const string ExpectedConfigurationVersion = "1.0.0";

    private const string ExpectedDefinitionIdentities = """
        2|水火硬氣功|SpecialTechnique|0|Mixed|Defense|1|4500|35|0|1739|1740|character-property:3:slot:0=50,character-property:4:slot:1=50,character-property:82:slot:2=100
        134|萬花聽雨式|Agility|7|Water|Agility|3|4000|0|0|247|973|character-property:1:slot:0=85,character-property:2:slot:1=60,character-property:58:slot:4=450,character-property:5:slot:2=60,character-property:81:slot:3=450
        147|鐵橋功|Agility|1|Metal|Agility|1|4000|0|0|260|986|character-property:0:slot:0=115,character-property:2:slot:1=30,character-property:81:slot:2=150
        148|橫江鎖|Agility|2|Metal|Agility|1|8000|0|0|261|987|character-property:0:slot:0=110,character-property:3:slot:1=50,character-property:81:slot:2=200
        150|五鬼步|Agility|0|Wood|Agility|1|6000|0|0|263|989|character-property:1:slot:0=30,character-property:54:slot:3=80,character-property:5:slot:1=70,character-property:65:slot:4=80,character-property:81:slot:2=100
        151|御風符|Agility|1|Wood|Agility|1|4000|0|0|264|990|character-property:1:slot:0=35,character-property:54:slot:3=120,character-property:5:slot:1=80,character-property:65:slot:4=120,character-property:81:slot:2=150
        252|兵聞拙速|SpecialTechnique|1|Metal|Assistance|1|0|0|0|150|876|character-property:1:slot:0=65,character-property:3:slot:1=80,character-property:82:slot:2=150
        265|冰清玉潔|SpecialTechnique|0|Water|Assistance|1|0|0|0|163|889|character-property:2:slot:0=30,character-property:4:slot:1=70,character-property:53:slot:3=80,character-property:62:slot:4=80,character-property:82:slot:2=100
        267|墨玉功|SpecialTechnique|2|Water|Assistance|1|0|0|0|165|891|character-property:4:slot:0=80,character-property:53:slot:3=160,character-property:5:slot:1=50,character-property:62:slot:4=160,character-property:82:slot:2=200
        280|三部九候法|SpecialTechnique|1|Mixed|Assistance|1|0|0|0|178|904|character-property:1:slot:0=35,character-property:2:slot:1=35,character-property:58:slot:4=120,character-property:59:slot:5=120,character-property:5:slot:2=45,character-property:82:slot:3=150
        289|拿脈功|SpecialTechnique|1|Metal|Defense|1|3000|40|0|187|913|character-property:0:slot:0=40,character-property:2:slot:1=35,character-property:4:slot:2=40,character-property:63:slot:4=150,character-property:82:slot:3=150
        295|即身成佛|SpecialTechnique|7|Metal|Defense|3|4500|45|0|193|919|character-property:2:slot:0=75,character-property:3:slot:1=130,character-property:63:slot:3=450,character-property:82:slot:2=450
        303|鬼降大法|SpecialTechnique|7|Wood|Defense|3|4500|45|0|201|927|character-property:1:slot:0=55,character-property:3:slot:1=50,character-property:59:slot:4=450,character-property:5:slot:2=100,character-property:82:slot:3=450
        599|開山快刀|Blade|1|Metal|Attack|1|21000|60|0|333|1059|character-property:0:slot:0=100,character-property:1:slot:1=45,character-property:88:slot:2=150
        602|斬鰲刀法|Blade|4|Metal|Attack|2|30000|80|0|336|1062|character-property:0:slot:0=120,character-property:3:slot:1=70,character-property:88:slot:2=300
        604|金猊鎮魔刀|Blade|6|Metal|Attack|3|33000|100|0|338|1064|character-property:0:slot:0=150,character-property:3:slot:1=70,character-property:88:slot:2=400
        616|羅剎刀法|Blade|1|Metal|Attack|1|24000|60|0|525|1251|character-property:0:slot:0=85,character-property:1:slot:1=30,character-property:63:slot:3=150,character-property:88:slot:2=150
        624|伏龍刀法|Blade|0|Fire|Attack|1|24000|60|0|508|1234|character-property:0:slot:0=55,character-property:4:slot:1=45,character-property:55:slot:3=80,character-property:64:slot:4=80,character-property:88:slot:2=100
        686|老君拂塵功|FlexibleWeapon|3|Fire|Attack|2|24000|80|0|696|1422|character-property:1:slot:0=55,character-property:2:slot:1=45,character-property:4:slot:2=45,character-property:52:slot:4=200,character-property:62:slot:5=200,character-property:91:slot:3=250
        """;

    private const string ExpectedPlayerCandidateStates = """
        2|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        134|learned=true|direction=Reverse|grid=3|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        147|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=False
        148|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=False
        150|learned=true|direction=Reverse|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        151|learned=true|direction=Unavailable|grid=1|mastered=False|brokenOut=False|canBreakthrough=True|available=Reverse|completed=|equipped=False
        252|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=False
        265|learned=true|direction=Unavailable|grid=1|mastered=False|brokenOut=False|canBreakthrough=True|available=Reverse|completed=|equipped=False
        267|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        280|learned=true|direction=Reverse|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=False
        289|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=False
        295|learned=true|direction=Unavailable|grid=3|mastered=False|brokenOut=False|canBreakthrough=True|available=Direct,Reverse|completed=|equipped=False
        303|learned=true|direction=Reverse|grid=3|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        599|learned=true|direction=Direct|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        602|learned=true|direction=Direct|grid=2|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        604|learned=true|direction=Reverse|grid=3|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        616|learned=true|direction=Reverse|grid=1|mastered=False|brokenOut=True|canBreakthrough=False|available=|completed=|equipped=True
        624|learned=true|direction=Unavailable|grid=1|mastered=False|brokenOut=False|canBreakthrough=True|available=Direct,Reverse|completed=|equipped=False
        686|learned=true|direction=Unavailable|grid=2|mastered=False|brokenOut=False|canBreakthrough=True|available=Direct,Reverse|completed=|equipped=False
        """;

    private static readonly int[] CandidateSkillIds =
    [
        2, 134, 147, 148, 150, 151, 252, 265, 267, 280, 289, 295, 303,
        599, 602, 604, 616, 624, 686
    ];

    [Fact]
    public async Task Current_candidate_definitions_are_available()
    {
        Assert.SkipUnless(
            string.Equals(
                Environment.GetEnvironmentVariable(EvidenceVariable),
                "1",
                StringComparison.Ordinal),
            $"E8-F01 skipped: set {EvidenceVariable}=1 to verify the "
            + "installed current-version tactical evidence.");

        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F01 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var guardedPaths = GuardedPaths(located.Paths!);
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var source = provider.GetRequiredService<
                ICombatSkillDefinitionSource>();
            var first = await source.ReadAsync(
                TestContext.Current.CancellationToken);
            var second = await source.ReadAsync(
                TestContext.Current.CancellationToken);

            Assert.Equal(DefinitionSourceReadStatus.Available, first.Status);
            Assert.Equal(DefinitionSourceReadStatus.Available, second.Status);
            Assert.NotNull(first.SourceIdentity);
            Assert.Equal(first.SourceIdentity, second.SourceIdentity);
            Assert.Equal(
                ExpectedConfigurationVersion,
                first.SourceIdentity!.GameDataVersion);
            var runtimeAssembly = GameDataRuntimePath(located.Paths!);
            Assert.Equal(
                ExpectedGameDataVersion,
                FileVersionInfo.GetVersionInfo(runtimeAssembly)
                    .ProductVersion);
            Assert.DoesNotContain(
                first.Diagnostics,
                item => item.Severity
                    == CombatSkillImportDiagnosticSeverity.Error);
            Assert.Equal(
                CandidateSkillIds,
                first.Definitions
                    .Where(item => CandidateSkillIds.Contains(item.SkillId))
                    .Select(item => item.SkillId));

            var actual = first.Definitions
                .Where(item => CandidateSkillIds.Contains(item.SkillId))
                .Select(DefinitionIdentity)
                .ToArray();
            Assert.Equal(ExpectedLines(), actual);
            Assert.All(
                first.Definitions.Where(item =>
                    CandidateSkillIds.Contains(item.SkillId)),
                definition => Assert.All(
                    new[]
                    {
                        (RawCombatSkillDescriptionKind.DirectEffect,
                            CatalogueLanguage.TraditionalChinese),
                        (RawCombatSkillDescriptionKind.DirectEffect,
                            CatalogueLanguage.English),
                        (RawCombatSkillDescriptionKind.ReverseEffect,
                            CatalogueLanguage.TraditionalChinese),
                        (RawCombatSkillDescriptionKind.ReverseEffect,
                            CatalogueLanguage.English)
                    },
                    expected => Assert.Contains(
                        definition.RawDescriptions,
                        item => item.Kind == expected.Item1
                            && item.Language == expected.Item2
                            && !string.IsNullOrWhiteSpace(item.Text))));
            Assert.Equal(
                actual,
                second.Definitions
                    .Where(item => CandidateSkillIds.Contains(item.SkillId))
                    .Select(DefinitionIdentity));

            output.WriteLine(
                "E8-F01 current tactical definitions: gameData={0}; "
                + "configurationVersion={1}; candidates={2}/{3}; errors=0; "
                + "guardedFiles={4}.",
                ExpectedGameDataVersion,
                first.SourceIdentity.GameDataVersion,
                actual.Length,
                CandidateSkillIds.Length,
                guardedPaths.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task Current_player_candidate_state_is_repeatable()
    {
        RequireEvidenceOptIn();
        var savePath = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(savePath),
            $"E8-F01 skipped: set {SavePathVariable} to a local Taiwu save.");
        Assert.SkipUnless(
            File.Exists(savePath),
            $"E8-F01 skipped: {SavePathVariable} does not identify a file.");

        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F01 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var guardedPaths = GuardedPaths(located.Paths!)
            .Append(Path.GetFullPath(savePath!))
            .ToArray();
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var targetLookup = await provider
                .GetRequiredService<ITargetLookupReader>()
                .ReadAsync(
                    new TargetLookupReadRequest(
                        savePath!,
                        TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            var target = targetLookup.Entries
                .OrderBy(item => item.CharacterId)
                .FirstOrDefault();
            Assert.SkipUnless(
                target is not null,
                "E8-F01 skipped: the configured save has no target entry.");

            var reader = provider.GetRequiredService<ICombatSnapshotReader>();
            var request = new CombatSnapshotReadRequest(
                savePath!,
                target!.CharacterId,
                language: TaiwuLanguage.Chinese);
            var first = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);
            var second = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                ExpectedGameDataVersion,
                first.Metadata.GameDataVersion.Value);
            Assert.Equal(first.Metadata.SaveSha256, second.Metadata.SaveSha256);
            Assert.Equal(
                first.Metadata.GameDataVersion,
                second.Metadata.GameDataVersion);
            var equipped = Enum.GetValues<SkillCategory>()
                .SelectMany(category =>
                    first.Player.EquippedSkills.Get(category))
                .ToHashSet();
            var lines = CandidateSkillIds.Select(skillId =>
            {
                var skill = first.Player.LearnedSkills.SingleOrDefault(item =>
                    item.SkillId == skillId);
                if (skill is null)
                {
                    return $"{skillId}|learned=false";
                }

                var breakthrough = skill.BreakthroughDirections.IsAvailable
                    ? skill.BreakthroughDirections.Value
                    : null;
                return string.Join('|',
                    skillId,
                    "learned=true",
                    $"direction={Snapshot(skill.Direction)}",
                    $"grid={Snapshot(skill.GridCost)}",
                    $"mastered={Snapshot(skill.Mastered)}",
                    $"brokenOut={breakthrough?.IsBrokenOut}",
                    $"canBreakthrough={breakthrough?.CanBreakthroughNow}",
                    "available=" + string.Join(',',
                        breakthrough?.AvailableDirections ?? []),
                    "completed=" + string.Join(',',
                        breakthrough?.CompletedDirections ?? []),
                    $"equipped={equipped.Contains(skillId)}");
            }).ToArray();
            Assert.Equal(PlayerStateLines(), lines);
            Assert.All(
                first.Player.SlotBudgets.Values,
                item => Assert.False(item.Used.IsAvailable));
            Assert.Equal(
                new[] { 6, 9, 6, 10, 5 },
                first.Player.SlotBudgets.Values
                    .Select(item => item.Capacity));
            Assert.Equal(8, first.Player.GenericSlotAllocation.TotalSlots);
            Assert.Equal(1, first.Player.GenericSlotAllocation.Attack);
            Assert.Equal(3, first.Player.GenericSlotAllocation.Agility);
            Assert.Equal(1, first.Player.GenericSlotAllocation.Defense);
            Assert.Equal(3, first.Player.GenericSlotAllocation.Assistance);

            output.WriteLine(
                "E8-F01 current player evidence: candidates={0}/{1}; "
                + "equipped={2}; diskBudgets=6/9/6/10/5; "
                + "usedSlots=unavailable; guardedFiles={3}.",
                lines.Count(item => item.Contains("learned=true",
                    StringComparison.Ordinal)),
                CandidateSkillIds.Length,
                lines.Count(item => item.EndsWith("equipped=True",
                    StringComparison.Ordinal)),
                guardedPaths.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            Assert.Equal(before, after);
        }
    }

    private static string Snapshot<T>(SnapshotValue<T> value) =>
        value.IsAvailable ? value.Value?.ToString() ?? "<null>" : "Unavailable";

    private static string[] PlayerStateLines() =>
        ExpectedPlayerCandidateStates
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .ToArray();

    private static void RequireEvidenceOptIn() => Assert.SkipUnless(
        string.Equals(
            Environment.GetEnvironmentVariable(EvidenceVariable),
            "1",
            StringComparison.Ordinal),
        $"E8-F01 skipped: set {EvidenceVariable}=1 to verify the installed "
        + "current-version tactical evidence.");

    private static string[] ExpectedLines() => ExpectedDefinitionIdentities
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(item => item.Trim())
        .ToArray();

    private static string[] GuardedPaths(TaiwuCatalogueSourcePaths paths) =>
    [
        GameDataRuntimePath(paths),
        paths.GameDataConfigurationAssembly,
        paths.TraditionalChineseCombatSkillLanguage,
        paths.EnglishCombatSkillLanguage,
        paths.TraditionalChineseSpecialEffectLanguage,
        paths.EnglishSpecialEffectLanguage,
        paths.TraditionalChineseLegendaryBookSlotLanguage,
        paths.EnglishLegendaryBookSlotLanguage
    ];

    private static string GameDataRuntimePath(
        TaiwuCatalogueSourcePaths paths) => Path.Combine(
        Path.GetDirectoryName(paths.GameDataConfigurationAssembly)!,
        "GameData.dll");

    private static async Task<IReadOnlyList<GuardedFileState>> CaptureAsync(
        IEnumerable<string> paths)
    {
        List<GuardedFileState> values = [];
        foreach (var path in paths.Order(StringComparer.OrdinalIgnoreCase))
        {
            Assert.True(File.Exists(path));
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(
                stream,
                TestContext.Current.CancellationToken);
            values.Add(new GuardedFileState(
                Path.GetFileName(path),
                stream.Length,
                File.GetLastWriteTimeUtc(path),
                Convert.ToHexString(hash)));
        }

        return values;
    }

    private static string DefinitionIdentity(CombatSkillDefinition value)
    {
        var requirements = value.Requirements
            .OrderBy(item => item.RequirementId.Value, StringComparer.Ordinal)
            .Select(item => $"{item.RequirementId.Value}="
                + Field(item.RequiredValue));
        return string.Join('|',
            value.SkillId,
            value.Names.Get(CatalogueLanguage.TraditionalChinese).Value.Text,
            Field(value.Category),
            Field(value.Grade, item => item.Value.ToString()),
            Field(value.Element),
            Field(value.EquipmentType),
            Field(value.BaseGridCost, item => item.Value.ToString()),
            Field(value.Timing.PreparationProgress),
            Field(value.Timing.BreathStanceCost),
            Field(value.Timing.CastSpeed),
            Field(value.Effects.Direct, item => item.Value.ToString()),
            Field(value.Effects.Reverse, item => item.Value.ToString()),
            string.Join(',', requirements));
    }

    private static string Field<T>(CatalogueField<T> value) =>
        value.IsAvailable ? value.Value?.ToString() ?? "<null>" : value.Status.ToString();

    private static string Field<T>(
        CatalogueField<T> value,
        Func<T, string> format) => value.IsAvailable
        ? format(value.Value)
        : value.Status.ToString();

    private sealed record GuardedFileState(
        string Name,
        long Length,
        DateTime LastWriteUtc,
        string Sha256);
}
