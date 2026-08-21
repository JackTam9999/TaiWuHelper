using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TacticalCombat;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
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

    private const string ExpectedBehaviorIdentities = """
        GameData.Domains.SpecialEffect.CombatSkill.Baihuagu.Agile.WanHuaTingYuShi|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.BuffHitOrDebuffAvoid|D15D599201B0379EB3D07B7BE1A436692B1950A2D300C31B90986C1ABD3A441A|methods=63
        GameData.Domains.SpecialEffect.CombatSkill.Fulongtan.Blade.FuLongDaoFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Attack.ChangePowerByEquipType|B3ADC8D5580C027BF0B23A37ABA33227021ECC2106C8E3DEBF2D75DAA3ACE87B|methods=55
        GameData.Domains.SpecialEffect.CombatSkill.Jingangzong.Blade.LuoChaDaoFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Attack.AttackBodyPart|F2184B0E9AA5895B4EF4611C7CBEFACD678D9710B30C3972E2FFEAD2E5061F3D|methods=56
        GameData.Domains.SpecialEffect.CombatSkill.Jingangzong.DefenseAndAssist.JiShenChengFo|GameData.Domains.SpecialEffect.CombatSkill.Common.Defense.DefenseSkillBase|6C8CEB88479CDCAE1ACA8B763191810FA644CE4F021B1F0439BDF1D0FA764293|methods=57
        GameData.Domains.SpecialEffect.CombatSkill.Jingangzong.DefenseAndAssist.NaMaiGong|GameData.Domains.SpecialEffect.CombatSkill.Common.Defense.DefenseSkillBase|AD9D0C97915378C44F160639073B5A710FDF1EE6FB3DF1D2950B7C75A4BE599E|methods=55
        GameData.Domains.SpecialEffect.CombatSkill.Kongsangpai.DefenseAndAssist.SanBuJiuHouFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Assist.AssistSkillBase|3007115D9054EA4FF4B68DE05977E7ED8A611C5EAC5F4533C5FD155EDDE39600|methods=62
        GameData.Domains.SpecialEffect.CombatSkill.NoSect.DefenseAndAssist.ShuiHuoYingQiGong|GameData.Domains.SpecialEffect.CombatSkill.Common.Defense.DefenseSkillBase|67EC714E340B0E0352AE5C8919D8586231A50F4E062D1A16DC785F39E5BA9573|methods=56
        GameData.Domains.SpecialEffect.CombatSkill.Ranshanpai.Agile.WuGuiBu|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.ChangeAttackHitType|5B4C84FA07177ECD250A74E7D3E9B68FCD2A65E013D274BA0A89E15EB04852D9|methods=58
        GameData.Domains.SpecialEffect.CombatSkill.Ranshanpai.Agile.YuFengFu|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|A7FD8E6523AD80D6D07B4BC667C8812867F094814809D816B851ADDE4075F7A7|methods=55
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.Agile.HengJiangSuo|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|ACBCD0C6937CAB2C0C6BA28ED6848E009F4A506DAD2921DC9F2D79527810A80C|methods=55
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.Agile.TieQiaoGong|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|F815CFA476891675ACBF5F2733EE54AC037B2FB2F1A78F69B7AAFD3F4D599B1B|methods=54
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.Blade.JinNiZhenMoDao|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|EC6B2CB425F7B8CCE7649F521544F33E3AD862D9DC11B649C798D81A2BB86A08|methods=55
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.Blade.KaiShanKuaiDao|GameData.Domains.SpecialEffect.CombatSkill.Common.Attack.GetTrick|E05595281A020499674C34872B781C6B48EC24431F5DA6EBE99DFEC70FA69E2B|methods=53
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.Blade.ZhanAoDaoFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Attack.AttackBodyPart|F4760E850C3530743123C3B04B875C5EA5F45B917E12487E3EF6983FF838DD19|methods=56
        GameData.Domains.SpecialEffect.CombatSkill.Shixiangmen.DefenseAndAssist.BingWenZhuoSu|GameData.Domains.SpecialEffect.CombatSkill.Common.Assist.AssistSkillBase|DFB244D0C82487A2199094FE5CF29EBF3EC05C18092E9C0298C950E32E5A3A4B|methods=62
        GameData.Domains.SpecialEffect.CombatSkill.Wudangpai.Whip.LaoJunFuChenGong|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|B7D887AD9D2AE4F72F5A03BEE48A29C51F2987D80A17F4106AA3EC8A7F7BBDBE|methods=50
        GameData.Domains.SpecialEffect.CombatSkill.Wuxianjiao.DefenseAndAssist.GuiJiangDaFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Defense.DefenseSkillBase|29105157E0BF97DD69FA49B4B34DF77F6845925E6A60275444584A47B8FD7F6D|methods=58
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.DefenseAndAssist.BingQingYuJie|GameData.Domains.SpecialEffect.CombatSkill.Common.Assist.AssistSkillBase|7773B0097C8AA3735E5EC60C28FC9D0AE3853D0F28FE43C35E6B4873AFF507C1|methods=60
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.DefenseAndAssist.MoYuGong|GameData.Domains.SpecialEffect.CombatSkill.Common.Assist.AssistSkillBase|56FBD75E80B3FD8892421EC0046119C2C9B981EE9A3F0DA364A0B9AD5E8810B3|methods=58
        """;

    private const string ExpectedLaterPhaseBehaviorIdentities = """
        GameData.Domains.Combat.CombatCharacter|GameData.Common.BaseGameDataObject|A9F9F5934905366E7400E6BAD2C0D6FE496B6E3B6943D9A1C50D3DB62D2F9440|methods=686
        GameData.Domains.Combat.CombatCharacterStateBase|System.Object|F2BC3D5E3ADCA63035C9204155F2BC6A89442975B5D788989763B8BB3392AB33|methods=22
        GameData.Domains.Combat.CombatDomain|GameData.Common.BaseGameDataDomain|5EA3200605A5A9C661127F378C19AE54287986D3E7C2A3839D045335618648CA|methods=820
        GameData.Domains.SpecialEffect.CombatSkill.Kongsangpai.DefenseAndAssist.JiuSeYuChanFa|GameData.Domains.SpecialEffect.CombatSkill.Common.Assist.AssistSkillBase|1E65853CB8F234085FA2DC153CCA20BAEBA83BA542CB111EBB6BCFD5E4E61961|methods=19
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.BieLiBu|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|BF0DA8BAE93A7CAE27983584BD214DF7755EFEA2087D712EFF4F5D6D1E378A04|methods=8
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.ChangEBenYue|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|0113B6BE4389DD79A449420B8654B351F87BF4FD4CE859C55548FF14D1BFCCFF|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.GuSheTaXue|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.CheckHitEffect|74A780FCF95A17CCEA56753AC650CEDFA148B687FA93B080EABB44EA1B83C60E|methods=3
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.LuoShenLingBo|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|64998D1BA1F658F91769F6D09043987CC44C296B23E60AE9BC7AE36167A86764|methods=5
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.MeiDianTou|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AttackChangeMobility|52619A0D9DBCEE2D1433703519BB7C3B1C4EAB83E60D4B17DD39CC7921691BC0|methods=2
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.QingNvLvBing|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.BuffHitOrDebuffAvoid|3C82CBFF9B459C84F82012E29BCA6BE5C9AB3746B47A61AF6CFBB23F56D48784|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.ShangYuGe|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|15D4974D282B3472061E1EBA3491638BE5E43054268011F7B5989A008BDDF74F|methods=7
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.WangXiaBaBu|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|CF12E636004B18864B5AB3E02DF9A68E4C2882F2A625ABFA929738DE5DB4B9A1|methods=5
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Agile.YuYiGong|GameData.Domains.SpecialEffect.CombatSkill.Common.Agile.AgileSkillBase|864E7D7246D2833CE936ADD8C84A6A630B388480B880F3D86E14D3D5D9A8B3B1|methods=8
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.DuanHunYouYinQu|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|6535085507B24F2F082E433E1C79480D9F7BCE1DABC25DB5C660A914C76EE606|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.HouRenXiYi|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|5FA1ACC605DC58367B704918D66EA0F5F2F725B258DE834A02F65397D75145E5|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.HuangZhuGe|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|3938384610F06AB559D7E5178321C4333F990DEB3F5BB810673074653E9F453D|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.QingPingDiao|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|A0430363D2B71489D6DB864BE68B5B7E86D3B44541B093D4E928D12669DEDE0C|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.SuNvTianYin|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|C43B106734E3FE05BE6ED6D349139778A1D452E58152305139F75B433CBCE68C|methods=6
        GameData.Domains.SpecialEffect.CombatSkill.Xuannvpai.Music.XiangNvQiCangWu|GameData.Domains.SpecialEffect.CombatSkill.CombatSkillEffectBase|0892DFB7C7D87421CFFE6CCD0BF272D1293FEC039991DD43EDEF97F441FCE8E6|methods=7
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
    public async Task Current_candidate_behavior_contracts_are_version_bound()
    {
        RequireEvidenceOptIn();
        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F01 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var runtimeAssembly = GameDataRuntimePath(located.Paths!);
        var before = await CaptureAsync([runtimeAssembly]);

        try
        {
            Assert.Equal(
                ExpectedGameDataVersion,
                FileVersionInfo.GetVersionInfo(runtimeAssembly)
                    .ProductVersion);
            var bytes = await File.ReadAllBytesAsync(
                runtimeAssembly,
                TestContext.Current.CancellationToken);
            var assembly = Assembly.Load(bytes);
            var expected = BehaviorLines();
            var expectedTypeNames = expected
                .Select(line => line.Split('|', 2)[0])
                .ToHashSet(StringComparer.Ordinal);
            var actual = assembly.GetTypes()
                .Where(type => type.FullName is not null
                    && expectedTypeNames.Contains(type.FullName))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(BehaviorIdentity)
                .ToArray();

            Assert.Equal(expected, actual);
            output.WriteLine(
                "E8-F01 current tactical behavior contracts: "
                + "gameData={0}; candidates={1}/{2}; guardedFiles=1.",
                ExpectedGameDataVersion,
                actual.Length,
                expected.Length);
        }
        finally
        {
            var after = await CaptureAsync([runtimeAssembly]);
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task Current_later_magic_sound_phase_is_exact_and_read_only()
    {
        RequireEvidenceOptIn("E8-F02");
        var savePath = RequireSavePath();
        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F02 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var guardedPaths = GuardedPaths(located.Paths!)
            .Append(savePath)
            .ToArray();
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var lookup = await provider
                .GetRequiredService<ITargetLookupReader>()
                .ReadAsync(
                    new TargetLookupReadRequest(
                        savePath,
                        TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            var target = lookup.Entries.Single(item =>
                item.Kind == TargetLookupKind.StoryCharacter
                && item.TemplateId
                    == VerifiedExactTargetEncounterRuleSets
                        .LaterMagicSoundTargetTemplateId);
            var snapshot = await provider
                .GetRequiredService<ICombatSnapshotReader>()
                .ReadAsync(
                    new CombatSnapshotReadRequest(
                        savePath,
                        target.CharacterId,
                        language: TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);

            Assert.Equal(ExpectedGameDataVersion, lookup.GameDataVersion);
            Assert.True(snapshot.Target.EquippedSkills.IsAvailable);
            var equippedIds = Enum.GetValues<SkillCategory>()
                .SelectMany(category =>
                    snapshot.Target.EquippedSkills.Value.Get(category))
                .Order()
                .ToArray();
            var learnedById = snapshot.Target.LearnedSkills.ToDictionary(
                item => item.SkillId);
            var signatures = equippedIds.Select(skillId =>
            {
                var skill = learnedById[skillId];
                Assert.True(skill.Direction.IsAvailable);
                Assert.True(skill.DirectEffectId.IsAvailable);
                Assert.True(skill.ReverseEffectId.IsAvailable);
                var effectId = skill.Direction.Value switch
                {
                    PracticeDirection.Direct => skill.DirectEffectId.Value,
                    PracticeDirection.Reverse => skill.ReverseEffectId.Value,
                    _ => throw new InvalidOperationException(
                        "An exact target signature requires a practice "
                        + "direction.")
                };
                return new TargetThreatSkillSignature(
                    skillId,
                    skill.Direction.Value,
                    effectId);
            }).ToArray();
            var phaseEvidence = new TargetEncounterEvidence(
                TargetEncounterEvidenceSource.SavedStoryTemplate,
                "E8-F02-CURRENT-SAVE-STORY-TEMPLATE",
                ExpectedGameDataVersion);
            var loadoutEvidence = new TargetEncounterEvidence(
                TargetEncounterEvidenceSource.SavedEquippedLoadout,
                "E8-F02-CURRENT-SAVE-EQUIPPED-LOADOUT",
                ExpectedGameDataVersion);
            var observation = new TargetEncounterPhaseObservation(
                lookup.GameDataVersion!,
                [new(target.TemplateId!.Value, phaseEvidence)],
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                signatures,
                loadoutEvidence);
            var rule = VerifiedExactTargetEncounterRuleSets
                .CurrentLaterMagicSound;
            var resolution = ExactTargetEncounterPhaseResolver.Resolve(
                rule,
                observation);

            Assert.Equal(
                TargetEncounterBindingStatus.Complete,
                resolution.Status);
            Assert.Equal(34, signatures.Length);
            Assert.All(
                signatures,
                item => Assert.Equal(
                    PracticeDirection.Direct,
                    item.Direction));
            Assert.DoesNotContain(287, equippedIds);
            Assert.DoesNotContain(
                snapshot.Target.LearnedSkills,
                item => item.SkillId == 287);
            Assert.False(snapshot.Target.BaseChannelResistance.IsAvailable);
            Assert.Equal(
                new[] { 20, 30, 40, 50, 120, 160 },
                TaiwuTacticalCombatEvidenceProbe.ReadMindDamageSteps(
                    rule.DirectMagicSoundSkillIds));
            Assert.All(
                rule.Facts.Where(item => item.Code.StartsWith(
                    "LIVE_",
                    StringComparison.Ordinal)
                    || item.Kind is TargetEncounterFactKind.ActiveAgility
                        or TargetEncounterFactKind.ActiveInnerPowerState),
                item => Assert.Equal(
                    TargetEncounterFactState.ManualObservationRequired,
                    item.State));

            output.WriteLine(
                "E8-F02 exact later-phase evidence: templateBound=true; "
                + "equipped=34; direct=34; magicSound=6; reset287=false; "
                + "baseResistance=unavailable; guardedFiles={0}.",
                guardedPaths.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task Current_later_magic_sound_behaviors_are_version_bound()
    {
        RequireEvidenceOptIn("E8-F02");
        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F02 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var runtimeAssembly = GameDataRuntimePath(located.Paths!);
        var before = await CaptureAsync([runtimeAssembly]);

        try
        {
            Assert.Equal(
                ExpectedGameDataVersion,
                FileVersionInfo.GetVersionInfo(runtimeAssembly)
                    .ProductVersion);
            var bytes = await File.ReadAllBytesAsync(
                runtimeAssembly,
                TestContext.Current.CancellationToken);
            var assembly = Assembly.Load(bytes);
            var expected = LaterPhaseBehaviorLines();
            var names = expected
                .Select(line => line.Split('|', 2)[0])
                .ToHashSet(StringComparer.Ordinal);
            var actual = assembly.GetTypes()
                .Where(type => type.FullName is not null
                    && names.Contains(type.FullName))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(DeclaredTypeIdentity)
                .ToArray();

            Assert.Equal(expected, actual);
            output.WriteLine(
                "E8-F02 exact later-phase behavior identities: "
                + "gameData={0}; identities={1}/{2}; guardedFiles=1.",
                ExpectedGameDataVersion,
                actual.Length,
                expected.Length);
        }
        finally
        {
            var after = await CaptureAsync([runtimeAssembly]);
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task Current_tactical_role_atlas_is_exact_repeatable_and_read_only()
    {
        RequireEvidenceOptIn("E8-F03");
        var savePath = RequireSavePath("E8-F03");
        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F03 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var guardedPaths = GuardedPaths(located.Paths!)
            .Append(savePath)
            .ToArray();
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var lookup = await provider
                .GetRequiredService<ITargetLookupReader>()
                .ReadAsync(
                    new TargetLookupReadRequest(
                        savePath,
                        TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            var target = lookup.Entries.Single(item =>
                item.Kind == TargetLookupKind.StoryCharacter
                && item.TemplateId
                    == VerifiedExactTargetEncounterRuleSets
                        .LaterMagicSoundTargetTemplateId);
            var reader = provider.GetRequiredService<ICombatSnapshotReader>();
            var diskSnapshot = await reader.ReadAsync(
                new CombatSnapshotReadRequest(
                    savePath,
                    target.CharacterId,
                    language: TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                TargetEncounterBindingStatus.Complete,
                ResolveCurrentLaterPhase(lookup.GameDataVersion!, target.TemplateId,
                    diskSnapshot).Status);

            var screenCapacities = new[] { 6, 10, 7, 9, 4 };
            var screenBudgets = new SlotBudgetSet(
                Enum.GetValues<SkillCategory>().Select(category =>
                    new SlotBudget(
                        category,
                        SnapshotValue<int>.Unavailable(
                            "E8-F03 screen evidence did not capture used slots."),
                        screenCapacities[(int)category])));
            var screen = new PlayerLoadoutObservation(
                DateTimeOffset.UtcNow,
                "E8-F03-CURRENT-SCREEN-LOADOUT",
                diskSnapshot.Player.EquippedSkills,
                diskSnapshot.Player.GenericSlotAllocation,
                screenBudgets);
            var snapshot = await reader.ReadAsync(
                new CombatSnapshotReadRequest(
                    savePath,
                    target.CharacterId,
                    screen,
                    TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);
            Assert.Equal(
                screenCapacities,
                snapshot.Player.SlotBudgets.Values.Select(item =>
                    item.Capacity));

            var rules = VerifiedTacticalCombatRuleSets
                .CurrentLaterMagicSound;
            var evidence = rules.Transitions
                .SelectMany(item => item.EvidenceRequirements)
                .Concat(rules.Roles.SelectMany(item =>
                    item.EvidenceRequirements))
                .DistinctBy(item => new
                {
                    item.Identity.Code,
                    item.Scope,
                    item.Source
                })
                .Select(item => new TacticalRuleEvidenceObservation(
                    item.Identity,
                    item.Scope,
                    item.Source,
                    TacticalRuleEvidenceDisposition.Confirmed,
                    new TacticalEvidenceReference(
                        item.Source,
                        $"E8-F03-{item.Identity.Code}",
                        ExpectedGameDataVersion,
                        VerifiedTacticalCombatRuleSets.RuleVersion,
                        "CURRENT_LATER_PHASE_COMPLETE")))
                .ToArray();
            var resolution = rules.Resolve(
                ExpectedGameDataVersion,
                rules.SupportedTargetGoalCodes,
                evidence);
            var context = TacticalExecutionContextProjector
                .ProjectCurrentLoadout(
                    snapshot,
                    resolution,
                    TestContext.Current.CancellationToken);
            var first = TacticalCandidateDiscovery.Discover(
                snapshot.Player,
                context,
                resolution,
                cancellationToken: TestContext.Current.CancellationToken);
            var second = TacticalCandidateDiscovery.Discover(
                snapshot.Player,
                context,
                resolution,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(first.SemanticFingerprint, second.SemanticFingerprint);
            Assert.Equal(19, first.Entries.Count(item => item.Role is not null));
            Assert.Equal(
                TacticalCandidateAdmissionState.Admitted,
                AtlasEntry(first, 267, PracticeDirection.Direct)
                    .AdmissionState);
            Assert.Equal(
                TacticalCandidateAdmissionState.Infeasible,
                AtlasEntry(first, 599, PracticeDirection.Reverse)
                    .AdmissionState);
            Assert.Equal(
                TacticalCandidateSupportState.UnsupportedEffect,
                AtlasEntry(first, 604, PracticeDirection.Direct)
                    .SupportState);
            Assert.Equal(
                TacticalCandidateAdmissionState.UnknownContext,
                AtlasEntry(first, 604, PracticeDirection.Reverse)
                    .AdmissionState);
            Assert.Contains(
                AtlasEntry(first, 604, PracticeDirection.Reverse).Gates,
                item => item.Kind
                        == TacticalCandidateGateKind.ExecutionRequirements
                    && item.State == TacticalCandidateGateState.Unknown);
            Assert.DoesNotContain(
                first.Entries.Where(item => item.Role?.Identity.Kind
                    == TacticalRoleKind.Recovery),
                item => item.AdmissionState
                    == TacticalCandidateAdmissionState.Admitted);
            Assert.All(
                new[]
                {
                    TacticalCandidateDecision.Admitted,
                    TacticalCandidateDecision.Rejected,
                    TacticalCandidateDecision.Unsupported,
                    TacticalCandidateDecision.Irrelevant
                },
                decision => Assert.Contains(
                    first.Entries,
                    item => item.Consideration.Decision == decision));

            output.WriteLine(
                "E8-F03 current tactical role atlas: roles=19; "
                + "screenBudgets=6/10/7/9/4; decisions={0}; "
                + "recoveryAdmitted=0; guardedFiles={1}.",
                string.Join(',', first.Entries
                    .GroupBy(item => item.Consideration.Decision)
                    .OrderBy(item => item.Key)
                    .Select(item => $"{item.Key}:{item.Count()}")),
                guardedPaths.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            Assert.Equal(before, after);
        }
    }

    [Fact]
    public async Task Current_and_proposed_execution_context_is_coherent_and_read_only()
    {
        RequireEvidenceOptIn("E8-F04");
        var savePath = RequireSavePath("E8-F04");
        var located = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            located.IsAvailable,
            "E8-F04 skipped: installed GameData catalogue sources are "
            + "unavailable.");
        var guardedPaths = GuardedPaths(located.Paths!)
            .Append(savePath)
            .ToArray();
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var lookup = await provider
                .GetRequiredService<ITargetLookupReader>()
                .ReadAsync(
                    new TargetLookupReadRequest(
                        savePath,
                        TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            var target = lookup.Entries.Single(item =>
                item.Kind == TargetLookupKind.StoryCharacter
                && item.TemplateId
                    == VerifiedExactTargetEncounterRuleSets
                        .LaterMagicSoundTargetTemplateId);
            var diskSnapshot = await provider
                .GetRequiredService<ICombatSnapshotReader>()
                .ReadAsync(
                    new CombatSnapshotReadRequest(
                        savePath,
                        target.CharacterId,
                        language: TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            Assert.Equal(
                TargetEncounterBindingStatus.Complete,
                ResolveCurrentLaterPhase(
                    lookup.GameDataVersion!,
                    target.TemplateId,
                    diskSnapshot).Status);

            var screenCapacities = new[] { 6, 10, 7, 9, 4 };
            var screenBudgets = new SlotBudgetSet(
                Enum.GetValues<SkillCategory>().Select(category =>
                    new SlotBudget(
                        category,
                        SnapshotValue<int>.Unavailable(
                            "E8-F04 screen evidence did not capture used slots."),
                        screenCapacities[(int)category])));
            var loadoutObservation = new PlayerLoadoutObservation(
                DateTimeOffset.UtcNow,
                "E8-F04-CURRENT-SCREEN-LOADOUT",
                diskSnapshot.Player.EquippedSkills,
                diskSnapshot.Player.GenericSlotAllocation,
                screenBudgets);
            var equippedIds = Enum.GetValues<SkillCategory>()
                .SelectMany(category =>
                    diskSnapshot.Player.EquippedSkills.Get(category))
                .Distinct()
                .Order()
                .ToArray();
            Assert.Contains(2, equippedIds);
            Assert.Contains(134, equippedIds);
            var proposal = new TacticalExecutionProposal(
                new CombatRequirementContext(
                    equippedWeaponTypeIds: [9],
                    trickCounts: [],
                    SnapshotValue<int>.Available(5),
                    resources:
                    [
                        Resource(CombatResourceKind.Stance, 100),
                        Resource(CombatResourceKind.Breath, 100),
                        Resource(CombatResourceKind.DefenseTrueQi, 3)
                    ],
                    unlockedWeaponTypeIds: [6, 9],
                    equippedSkillIds: equippedIds,
                    activeDefenseSkillId: 2,
                    activeAgilitySkillId: 134),
                screenBudgets,
                diskSnapshot.Player.GenericSlotAllocation,
                legendaryCostAssignments: [],
                usableCombatStyleIds: []);
            var rules = VerifiedTacticalCombatRuleSets
                .CurrentLaterMagicSound;
            var request = new TacticalExecutionContextReadRequest(
                new CombatSnapshotReadRequest(
                    savePath,
                    target.CharacterId,
                    loadoutObservation,
                    TaiwuLanguage.Chinese),
                rules.SupportedTargetGoalCodes,
                CurrentRuleEvidence("E8-F04"),
                proposal);
            var first = await provider
                .GetRequiredService<IReadTacticalExecutionContext>()
                .ExecuteAsync(
                    request,
                    TestContext.Current.CancellationToken);
            var second = await provider
                .GetRequiredService<IReadTacticalExecutionContext>()
                .ExecuteAsync(
                    request,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                first.Context.SemanticFingerprint,
                second.Context.SemanticFingerprint);
            Assert.Equal(rules.Fingerprint, first.Context.RuleSetFingerprint);
            Assert.Equal(
                TacticalContextOrigin.CurrentScreenObservation,
                first.Context.Current.SlotBudgets.Origin);
            Assert.Equal(
                screenCapacities,
                first.Context.Current.SlotBudgets.Value.Values.Select(item =>
                    item.Capacity));
            Assert.All(
                first.Context.Current.SlotBudgets.Value.Values,
                item => Assert.False(item.Used.IsAvailable));
            Assert.False(first.Context.Current.TrickCounts.IsAvailable);
            Assert.False(first.Context.Current.Distance.IsAvailable);
            Assert.False(first.Context.Current.Resources.IsAvailable);
            Assert.False(first.Context.Current.ActiveDefenseSkillId.IsAvailable);
            Assert.False(first.Context.Current.ActiveAgilitySkillId.IsAvailable);

            Assert.Equal([9], first.Context.Proposed.EquippedWeaponTypeIds.Value);
            Assert.Equal([6, 9], first.Context.Proposed.UnlockedWeaponTypeIds.Value);
            Assert.Empty(first.Context.Proposed.TrickCounts.Value);
            Assert.Empty(first.Context.Proposed.UsableCombatStyleIds.Value);
            Assert.Equal(5, first.Context.Proposed.Distance.Value);
            Assert.Equal(100, first.Context.Proposed.Stance.Value);
            Assert.Equal(100, first.Context.Proposed.Breath.Value);
            Assert.Equal(2, first.Context.Proposed.ActiveDefenseSkillId.Value);
            Assert.Equal(134, first.Context.Proposed.ActiveAgilitySkillId.Value);
            Assert.Equal(
                TacticalContextOrigin.ProposedPlan,
                first.Context.Proposed.Distance.Origin);

            output.WriteLine(
                "E8-F04 coherent execution context: currentScreen="
                + "6/10/7/9/4; currentLiveFacts=unknown; proposedWeapon=9; "
                + "proposedDistance=5; proposedStance=100; "
                + "proposedBreath=100; activeDefense=2; activeAgility=134; "
                + "guardedFiles={0}.",
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

    private static string[] BehaviorLines() => ExpectedBehaviorIdentities
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(item => item.Trim())
        .ToArray();

    private static string[] LaterPhaseBehaviorLines() =>
        ExpectedLaterPhaseBehaviorIdentities
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .ToArray();

    private static string BehaviorIdentity(Type type)
    {
        var behaviorTypes = BehaviorTypeChain(type).ToArray();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var behaviorType in behaviorTypes)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(
                (behaviorType.FullName ?? behaviorType.Name) + "\n"));
            foreach (var method in DeclaredMethodsAndConstructors(behaviorType))
            {
                hash.AppendData(Encoding.UTF8.GetBytes(
                    (method.ToString() ?? method.Name) + "\n"));
                hash.AppendData(method.GetMethodBody()?.GetILAsByteArray() ?? []);
            }
        }

        return string.Join('|',
            type.FullName ?? type.Name,
            type.BaseType?.FullName ?? "<none>",
            Convert.ToHexString(hash.GetHashAndReset()),
            "methods=" + behaviorTypes.Sum(item =>
                DeclaredMethodsAndConstructors(item).Length));
    }

    private static string DeclaredTypeIdentity(Type type)
    {
        var methods = DeclaredMethodsAndConstructors(type);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var method in methods)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(
                (method.ToString() ?? method.Name) + "\n"));
            hash.AppendData(method.GetMethodBody()?.GetILAsByteArray() ?? []);
        }

        return string.Join('|',
            type.FullName ?? type.Name,
            type.BaseType?.FullName ?? "<none>",
            Convert.ToHexString(hash.GetHashAndReset()),
            "methods=" + methods.Length);
    }

    private static IEnumerable<Type> BehaviorTypeChain(Type type)
    {
        for (var current = type;
             current is not null
             && current.Namespace?.StartsWith(
                 "GameData.Domains.SpecialEffect.CombatSkill",
                 StringComparison.Ordinal) == true;
             current = current.BaseType)
        {
            yield return current;
        }
    }

    private static MethodBase[] DeclaredMethodsAndConstructors(Type type) =>
        type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly)
            .Cast<MethodBase>()
            .Concat(type.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static))
            .OrderBy(method => method.ToString(), StringComparer.Ordinal)
            .ToArray();

    private static void RequireEvidenceOptIn(string item = "E8-F01") =>
        Assert.SkipUnless(
        string.Equals(
            Environment.GetEnvironmentVariable(EvidenceVariable),
            "1",
            StringComparison.Ordinal),
        $"{item} skipped: set {EvidenceVariable}=1 to verify the installed "
        + "current-version tactical evidence.");

    private static string RequireSavePath(string item = "E8-F02")
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"{item} skipped: set {SavePathVariable} to a local Taiwu save.");
        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"{item} skipped: {SavePathVariable} does not identify a file.");
        return path;
    }

    private static TargetEncounterPhaseResolution ResolveCurrentLaterPhase(
        string gameDataVersion,
        int? templateId,
        CombatSnapshot snapshot)
    {
        Assert.NotNull(templateId);
        Assert.True(snapshot.Target.EquippedSkills.IsAvailable);
        var equippedIds = Enum.GetValues<SkillCategory>()
            .SelectMany(category =>
                snapshot.Target.EquippedSkills.Value.Get(category))
            .Order()
            .ToArray();
        var learnedById = snapshot.Target.LearnedSkills.ToDictionary(
            item => item.SkillId);
        var signatures = equippedIds.Select(skillId =>
        {
            var skill = learnedById[skillId];
            Assert.True(skill.Direction.IsAvailable);
            var effect = skill.Direction.Value == PracticeDirection.Direct
                ? skill.DirectEffectId
                : skill.ReverseEffectId;
            Assert.True(effect.IsAvailable);
            return new TargetThreatSkillSignature(
                skillId,
                skill.Direction.Value,
                effect.Value);
        });
        return ExactTargetEncounterPhaseResolver.Resolve(
            VerifiedExactTargetEncounterRuleSets.CurrentLaterMagicSound,
            new TargetEncounterPhaseObservation(
                gameDataVersion,
                [new(templateId.Value, new TargetEncounterEvidence(
                    TargetEncounterEvidenceSource.SavedStoryTemplate,
                    "E8-F03-CURRENT-SAVE-STORY-TEMPLATE",
                    ExpectedGameDataVersion))],
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                signatures,
                new TargetEncounterEvidence(
                    TargetEncounterEvidenceSource.SavedEquippedLoadout,
                    "E8-F03-CURRENT-SAVE-EQUIPPED-LOADOUT",
                    ExpectedGameDataVersion)));
    }

    private static TacticalCandidateDiscoveryEntry AtlasEntry(
        TacticalCandidateDiscoveryResult atlas,
        int skillId,
        PracticeDirection direction) => atlas.Entries.Single(item =>
            item.SkillId == skillId && item.Direction == direction);

    private static TacticalRuleEvidenceObservation[] CurrentRuleEvidence(
        string item) => VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound
        .Transitions
        .SelectMany(rule => rule.EvidenceRequirements)
        .Concat(VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound.Roles
            .SelectMany(rule => rule.EvidenceRequirements))
        .DistinctBy(requirement => new
        {
            requirement.Identity.Code,
            requirement.Scope,
            requirement.Source
        })
        .Select(requirement => new TacticalRuleEvidenceObservation(
            requirement.Identity,
            requirement.Scope,
            requirement.Source,
            TacticalRuleEvidenceDisposition.Confirmed,
            new TacticalEvidenceReference(
                requirement.Source,
                $"{item}-{requirement.Identity.Code}",
                ExpectedGameDataVersion,
                VerifiedTacticalCombatRuleSets.RuleVersion,
                "CURRENT_LATER_PHASE_COMPLETE")))
        .ToArray();

    private static CombatResourceAmount Resource(
        CombatResourceKind kind,
        int value) => new(kind, SnapshotValue<int>.Available(value));

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
