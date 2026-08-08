using TaiWu.Application.Localization;

namespace TaiWuAPI.Presentation;

public sealed record RegionStoryDisplayDefinition(
    int OrganizationId,
    string Faction,
    string Story,
    string MainStoryBonus,
    string PostStoryBonus);

public static class RegionStoryCatalogue
{
    public static IReadOnlyList<RegionStoryDisplayDefinition> For(
        TaiwuLanguage language) => Entries
        .Select(entry => new RegionStoryDisplayDefinition(
            entry.OrganizationId,
            entry.Faction.Get(language),
            entry.Story.Get(language),
            entry.MainStoryBonus.Get(language),
            entry.PostStoryBonus.Get(language)))
        .ToArray();

    private static IReadOnlyList<Entry> Entries { get; } =
    [
        new(
            1,
            new("Shaolin Sect", "少林派"),
            new("The Way of Chan and Martial Arts", "禪武之道"),
            new(
                "Demon Trials: challenge nine stages of 18-Consummate "
                + "demon remnants for experience, Blood Dew, temporary "
                + "aptitude traits and Arhat statues.",
                "「誅魔試煉」：挑戰九關、十八精純魔頭殘影，取得歷練、血露、"
                + "暫時資質特性及羅漢像。"),
            new(
                "Arhat Enlightenment improves skill insights; monthly "
                + "enlightenment can raise the next breakthrough's talent "
                + "cap by 5 or connection rate by 5%.",
                "「羅漢開悟」強化功法玄機；每月可請求開悟，使下次突破的天資"
                + "上限 +5 或連接率 +5%。")),
        new(
            2,
            new("Emei Sect", "峨眉派"),
            new("The Hidden White Ape", "隱世白猿"),
            new(
                "Original Heart Method adds one researched custom effect "
                + "to each martial art.",
                "「獨創心法」：鑽研特殊效果並附加到功法；每門功法可裝一個。"),
            new(
                "Each martial art can hold two Original Heart Method effects.",
                "每門功法可裝的獨創心法增至兩個。")),
        new(
            3,
            new("Baihua Valley", "百花谷"),
            new("The Black Owl and White Deer", "玄鴞白鹿"),
            new(
                "Life and Death Gates let a character in a death slot "
                + "restore health to one in a life slot; both gain elemental "
                + "traits.",
                "「生關死節」：死位人物替生位人物補充健康，雙方取得五行特性。"),
            new(
                "Life and death slots each increase from four to eight.",
                "生位、死位由各四格增至各八格。")),
        new(
            4,
            new("Wudang Sect", "武當派"),
            new("The Coiling Tortoise and Snake", "龜蛇蟠扶"),
            new(
                "Correct Direct and Reverse Practice spends experience to "
                + "change a martial-art book's outline and chapter directions.",
                "「改正修逆」：消耗歷練修改功法書總綱及任意篇章的正逆類型。"),
            new(
                "Moving Palaces and Changing Acupoints swaps two grids "
                + "during breakthrough.",
                "「移宮易穴」：突破時交換兩個突破格。")),
        new(
            5,
            new("Yuanshan Sect", "元山派"),
            new("The Three Demons of the Stone Prison", "石牢三魔"),
            new(
                "Three-Talent Guard and Three-Demon Chaos transfer demonic "
                + "corruption and provide special battle assistance.",
                "「三才護陣／三魔亂陣」：轉移入魔程度，並讓三才或三魔特殊助戰。"),
            new(
                "The simultaneous special-assistant limit rises from one "
                + "to three.",
                "同時特殊助戰的上限由一人增至三人。")),
        new(
            6,
            new("Shixiang Sect", "獅相門"),
            new("Master of Letters and Arms", "文武雙全"),
            new(
                "Strategic Coordination grants and improves companion "
                + "assist commands by sounding the drum.",
                "「統籌方略」：擂鼓賦予並強化同道的助戰指令。"),
            new(
                "Each character can hold up to two advanced assist commands.",
                "每名人物可持有的高級助戰指令增至兩個。")),
        new(
            7,
            new("Ranshan Sect", "然山派"),
            new("The Azure Pavilion of Immortals", "青琅仙閣"),
            new(
                "Entrust Legendary Books lets each of the Three Corpses "
                + "protect two books while their effects remain active; "
                + "Sever Obsession can make others abandon books.",
                "「寄託奇書」：每名三屍保管兩本奇書，效果照常生效且不被爭奪；"
                + "「奇書斷執」可使他人放棄奇書。"),
            new(
                "Each of the Three Corpses can protect four books. Full use "
                + "also depends on enlightening Huaju, Xuanzhi and Yingjiao "
                + "before the main story ends.",
                "每名三屍可保管四本奇書。完整功能另要求在主線結束前完成華居、"
                + "玄質、迎嬌三人的開悟。")),
        new(
            8,
            new("Xuannü Sect", "璇女派"),
            new("Mirror Waters Reversed", "鏡水倒顛"),
            new(
                "Collecting scores for Song of the Solitary Phoenix "
                + "permanently raises Attraction and Composure across "
                + "legacies; completion unlocks Creating Life.",
                "蒐集《孤鸞鏡水謠》曲譜，永久增加歷代太吾的動心、守心；完成後"
                + "解鎖「造化生人」。"),
            new(
                "Otherworldly Travel returns a child next month at age 16 "
                + "with greatly improved traits.",
                "「天外遊歷」：孩童下月直接成長至十六歲並大幅改善特性。")),
        new(
            9,
            new("Zhu Jian Manor", "鑄劍山莊"),
            new("Tongsheng's Sword Trial", "銅生試劍"),
            new(
                "Tianshu Forging grants the automaton companion Tongsheng, "
                + "with unique growth and assist commands.",
                "「天樞玄鑄」：取得機關人同道銅生，具特殊成長及助戰指令。"),
            new(
                "Gain another automaton with different assist commands.",
                "再取得另一名具有不同助戰指令的機關人。")),
        new(
            10,
            new("Kongsang Sect", "空桑派"),
            new("The Lost Poison Formula", "奇毒絕方"),
            new(
                "Command the Ancient Cauldron by spending all Taiwu health "
                + "to heal a region. Liao Wuming must reach Unbreakable favor.",
                "「驅使古鼎」：耗盡太吾健康，持續治療指定地區所有人物；要求"
                + "廖無命好感達不渝。"),
            new(
                "Cauldron-Dragon Tempering consumes a King Gu to remove "
                + "Xuan Ash and grants ten years of immunity.",
                "「鼎蛟淬身」：消耗王蠱移除玄灰，並提供十年玄灰免疫。")),
        new(
            11,
            new("Vajra Sect", "金剛宗"),
            new("The Wordless True Scripture", "真經無字"),
            new(
                "The Soul Transformation Ritual fuses a reincarnation-platform "
                + "soul into a body so the soul may live again.",
                "「化魂儀式」：把輪迴台魂魄融入軀體，使魂魄重生。"),
            new(
                "One body can hold up to three fused souls.",
                "一具軀體可同時融合最多三個魂魄。")),
        new(
            12,
            new("Wuxian Cult", "五仙教"),
            new("The Five Sacred Heart Poisons", "五聖心毒"),
            new(
                "Refine King Gu to craft, consume or throw them; using one "
                + "also raises Wuxian finger-art power by 40%.",
                "「煉製王蠱」：製造、服食或投擲王蠱；使用後五仙指法威力提高"
                + "40%。"),
            new(
                "Command King Gu to trigger an immediate beneficial or "
                + "harmful effect in its host.",
                "「驅動王蠱」：觸發宿主體內王蠱的一次性正面或負面效果。")),
        new(
            13,
            new("Jieqing Sect", "界青門"),
            new("Neither Birth nor Death", "善惡無生"),
            new(
                "Strange-Pattern Stars builds star platforms, steals star "
                + "fortune and buys legacy benefits before succession.",
                "「奇紋星斗」：建立星台、奪取星運，並在傳承前購買生平遺惠。"),
            new(
                "The star-platform construction limit rises from five to ten.",
                "奇紋星台的建造上限由五座增至十座。")),
        new(
            14,
            new("Fulong Altar", "伏龍壇"),
            new("Fulong's Feathered Ascension", "伏龍化羽"),
            new(
                "Dispatch Yuan Chickens to improve villager roles; owning "
                + "three unlocks an extra role function.",
                "「調遣元雞」：為村民身分提供七元加成；擁有三隻元雞時解鎖"
                + "額外職能。"),
            new(
                "Yuan Chicken Feathers periodically provide feathers for "
                + "equipment refinement or character traits.",
                "「元雞靈羽」：定期取得可精製裝備或提供特性的元雞羽。")),
        new(
            15,
            new("Xuehou Cult", "血犼教"),
            new("The Lady of the Blood Tomb", "血塚遺姝"),
            new(
                "Ji Xi appears and may follow at Unbreakable favor, clearing "
                + "outlaws and heroes and healing low health by reducing age.",
                "姬穸出現；好感不渝後可隨行，自動清除外道、任俠，並在健康"
                + "過低時以減齡治療太吾。"),
            new(
                "Draw Qi with the Seal replaces killing with Qi absorption, "
                + "transfers Qi and inner-power elements, and grants favor "
                + "when clearing outlaws.",
                "「持印汲氣」：由殺人飲血改為吸取真氣，可轉移真氣及內力五行；"
                + "清除外道時還會取得恩義。"))
    ];

    private sealed record Entry(
        int OrganizationId,
        LocalizedText Faction,
        LocalizedText Story,
        LocalizedText MainStoryBonus,
        LocalizedText PostStoryBonus);

    private sealed record LocalizedText(string English, string Chinese)
    {
        public string Get(TaiwuLanguage language) => language switch
        {
            TaiwuLanguage.English => English,
            TaiwuLanguage.Chinese => Chinese,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.")
        };
    }
}
