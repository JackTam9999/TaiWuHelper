using TaiWu.Application.Localization;

namespace TaiWuAPI.Presentation;

public sealed record XiangshuStoryDisplayDefinition(
    int StoryIndex,
    string SwordName,
    string IncarnationName,
    string StorySubject,
    string Question,
    int ResolutionNoteId,
    string ResolutionChoice,
    string ResolutionConsequence,
    int CalamityNoteId,
    string CalamityChoice,
    string CalamityConsequence);

public static class XiangshuStoryCatalogue
{
    public static IReadOnlyList<XiangshuStoryDisplayDefinition> For(
        TaiwuLanguage language) => Entries
        .Select(entry => new XiangshuStoryDisplayDefinition(
            entry.StoryIndex,
            entry.SwordName.Get(language),
            entry.IncarnationName.Get(language),
            entry.StorySubject.Get(language),
            entry.Question.Get(language),
            entry.ResolutionNoteId,
            entry.ResolutionChoice.Get(language),
            entry.ResolutionConsequence.Get(language),
            entry.CalamityNoteId,
            entry.CalamityChoice.Get(language),
            entry.CalamityConsequence.Get(language)))
        .ToArray();

    private static IReadOnlyList<Entry> Entries { get; } =
    [
        new(
            1,
            new("Monu Weave", "莫女衣"),
            new("Monu", "莫女"),
            new("Monu", "莫女"),
            new(
                "\"I brought back the sword with my life... Did you have it?\"",
                "「那劍我已捨身取回……你可拿到了嗎？」"),
            4,
            new(
                "I had the sword and banished the fiend...",
                "我已取得了劍，將妖魔驅趕了……"),
            new(
                "Protected by Monu's lingering spiritual Qi, the swordsmith "
                + "defeats the fiend and names the blade Monu Weave in her "
                + "memory.",
                "得莫女靈氣所護，鑄劍人殺退毒池妖魔，並以「莫女衣」為劍名，"
                + "紀念莫女還劍之義。"),
            5,
            new(
                "The birds lacked the strength to deliver the sword...",
                "鳥兒力氣太小，未能將劍送來……"),
            new(
                "The unarmed swordsmith is also slain by the fiend and "
                + "drowns in the poisoned pool.",
                "手無寸鐵的鑄劍人也被妖魔所害，一併溺於毒池。")),
        new(
            2,
            new("Spiritsealer", "伏邪鐵"),
            new("Dayue Yaochang", "大岳瑤常"),
            new("Dayue Yaochang", "大岳瑤常"),
            new(
                "\"After my death... Were there still fiends plaguing the world?\"",
                "「我死之後……世上當真還有妖魔未除？」"),
            10,
            new(
                "At the place of your passing away, no fiend dared draw near...",
                "你身死處，再無妖魔膽敢靠近……"),
            new(
                "His bones merge with Kunlun stone to form the Spirit Seal "
                + "Stone; his seven centuries of struggle are given meaning.",
                "遺骨與崑崙山石融合，凝成「鎮獄伏邪石」；七百年斬妖雖未竟全功，"
                + "仍因護得半寸淨土而得償所願。"),
            11,
            new(
                "Yin and yang. Righteous and evil. A delicate equilibrium "
                + "exists between them, and neither can overcome the other...",
                "陰陽正邪，絕無獨消獨長之理……"),
            new(
                "His bones still form the Spirit Seal Stone, but his seven "
                + "centuries of fighting are judged a futile struggle.",
                "遺骨同樣凝成「鎮獄伏邪石」，但七百年征戰被視為無法除盡妖魔的"
                + "徒勞之舉。")),
        new(
            3,
            new("Dark Frost", "大玄凝"),
            new("Jiu Han", "九寒"),
            new("Jiu Han", "九寒"),
            new(
                "\"I was long buried under the iceberg, unable to know "
                + "everyone's well-being...\"",
                "「我已在冰山之下，不能知道大家的安危……」"),
            16,
            new(
                "Everyone was safe, and they came to visit you at the "
                + "iceberg every year...",
                "大家安然無恙，此後年年來冰山下看你……"),
            new(
                "The Jiao people survive, name the glacier Mt. Jiu Han and "
                + "mourn his sacrifice.",
                "角民村落得以保全；眾人將冰山喚作九寒山，悲傷懷念九寒。"),
            17,
            new(
                "The raging flood could not be stopped, and everyone passed "
                + "away as the flood swept them away...",
                "洪水勢不可擋，大家全都隨著洪水去了……"),
            new(
                "The glacier shatters and the displaced Jiao people lose "
                + "their homeland.",
                "冰山支離破碎，角民流離失所，自此無家可歸。")),
        new(
            4,
            new("Fenghuang Cocoon", "鳳凰繭"),
            new("Jin Huang'er", "金凰兒"),
            new("Jin Huang'er", "金凰兒"),
            new(
                "\"Say, do you think the baby... was the baby saved by the sage?\"",
                "「你道那嬰孩……最後被聖人救走了嗎？」"),
            22,
            new(
                "Though there may be no sage, the child may still see salvation...",
                "雖無聖人，嬰孩亦可得救……"),
            new(
                "He raises her as his foster daughter. Although he is no "
                + "sage, she smiles brightly all the same.",
                "嬰孩被鑄劍人收為義女；她雖知義父絕非聖賢，卻仍開懷而笑。"),
            23,
            new(
                "None may defy the heaven, for there was never a sage...",
                "天威難犯，世上更無聖人……"),
            new(
                "Only bones and torn five-colored feathers remain; no saving "
                + "sage ever came.",
                "繭中只剩屍骨與殘破的五彩鳥羽；世間終究沒有救世聖賢。")),
        new(
            5,
            new("Ignideus Blaze", "焚神煉"),
            new("Yi Yihou", "衣以侯"),
            new("Yi Yihou", "衣以侯"),
            new(
                "\"Plain Fox... Surely he is waiting for me in the flame... Right?\"",
                "「醜狐……定在那片火海裡面等我……對嗎？」"),
            28,
            new(
                "(Nod...)",
                "（點頭……）"),
            new(
                "The human and fox reunite, and the flames moved by their "
                + "bond become breathing fire.",
                "一人一狐終得團圓；身邊火苗因而生情，化作「活火種」。"),
            29,
            new(
                "(Shake your head...)",
                "（搖頭……）"),
            new(
                "She collapses beneath a peach tree in grief; the flames "
                + "moved by her sorrow become breathing fire.",
                "她傷心倒在桃花樹下痛哭；身邊火苗因而生情，同樣化作「活火種」。")),
        new(
            6,
            new("Candelor's Tail", "解龍魄"),
            new("Wei Qi", "衛起"),
            new("Wei Qi", "衛起"),
            new(
                "\"What is righteousness... Where does the Tao lie...?\"",
                "「義為何物……道在何方……？」"),
            34,
            new(
                "Simplicity resides within the Tao that unifies all beneath "
                + "heaven, achieved only by non-intention...",
                "大道至簡，和光同塵，無心而成……"),
            new(
                "Wei Qi lets go of his fixation, returns to his original "
                + "aspiration and comprehends the truth of the Grand Tao.",
                "衛起放下執著，回歸雲山白鶴所象徵的初心，終於領悟大道真諦。"),
            35,
            new(
                "Obsessions lurk within the heart that long only for "
                + "righteousness, only to be rejected by the Tao...",
                "心有所執，只求高義，道所不容……"),
            new(
                "Wei Qi fails to attain enlightenment and falls into endless "
                + "bewilderment.",
                "衛起求道不成、無法參悟玄機，從此陷入無盡迷惘。")),
        new(
            7,
            new("Vitalos", "溶塵隱"),
            new("Yi Xiang", "以向"),
            new("Yi Xiang and the impurity", "以向與汙痕"),
            new(
                "\"The white one is Yi Xiang, and the black one is also Yi "
                + "Xiang. So, am I Yi Xiang...\"",
                "「白的是以向，黑的也是以向，那我究竟是不是以向呢……」"),
            40,
            new(
                "You forgave Yi Xiang, and Yi Xiang was beyond shamed, "
                + "joining with you as one...",
                "你饒恕了以向，以向自愧難當，與你重合為一……"),
            new(
                "The divine Yi Xiang repents and reunites with the impurity "
                + "as a mortal; the immortals sever Jianmu and divide heaven "
                + "from earth.",
                "神人以向幡然悔悟，與汙痕重合為凡人；眾仙斬斷建木，絕地天通。"),
            41,
            new(
                "You killed Yi Xiang, and there was only one Yi Xiang in "
                + "the world from that moment onward...",
                "你殺死了以向，從此這世上，只有一個以向了……"),
            new(
                "Only the depraved Yi Xiang remains; the immortals still "
                + "sever Jianmu and divide heaven from earth.",
                "天地間只剩下醜惡的以向；眾仙同樣斬斷建木，絕地天通。")),
        new(
            8,
            new("Demonbind", "囚魔木"),
            new("Blood Maple", "血楓"),
            new("Chi You", "蚩尤"),
            new(
                "\"Grandfather Arbogod said I am destined for great duties, "
                + "so fate won't let me die...\"",
                "「楓神爺爺說老子有天命在身，不會讓老子死的……」"),
            46,
            new(
                "Yet your fated destiny was to be defeated by Xuanyuan, "
                + "thereby facilitating the unification that was to come...",
                "敗於軒轅，促成一統，便是天命……"),
            new(
                "Chi You realizes his fate was to enable unification; seeing "
                + "the Jiuli people prosper, he finds solace.",
                "蚩尤明白自己的天命是成就一統；見九黎之民富足安定，終於默默釋然。"),
            47,
            new(
                "How could you be so foolish as to believe and spread such "
                + "fanciful and false tales...",
                "怪力亂神，妖言惑眾，豈能有信……"),
            new(
                "The people suffer, and Chi You cannot rest in death after "
                + "leading the Jiuli into ruin by trusting the Arbogod.",
                "黎民受苦；蚩尤因誤信楓神，使九黎飽受戰亂，死後仍難瞑目。")),
        new(
            9,
            new("Rainbow Ultima", "鬼神霞"),
            new("Spellwright", "術方"),
            new("Spellwright", "術方"),
            new(
                "\"After my death... Big brother... How was he...\"",
                "「我死之後……兄長……如何了……」"),
            52,
            new(
                "Despite your passing away, your spirit remained with him, "
                + "and you two fought alongside each other...",
                "你身雖滅，神魂仍在，你二人仍然並肩而戰……"),
            new(
                "Although divine punishment seals her soul in the token, her "
                + "life-saving duty and loyalty to her friend are both "
                + "fulfilled.",
                "術方雖受天罰、神魂封入令牌，最終仍使救命之恩、朋友之義兩全。"),
            53,
            new(
                "Without your help, he was powerless in the face of the "
                + "fiends and died a horrible death...",
                "無你相助，力有未逮，定然慘死於妖魔之手……"),
            new(
                "She falls into deep remorse, believing she has lost both "
                + "duty and friendship.",
                "術方陷入深深自責，認定救命之恩與朋友之義兩者皆失。"))
    ];

    private sealed record Entry(
        int StoryIndex,
        LocalizedText SwordName,
        LocalizedText IncarnationName,
        LocalizedText StorySubject,
        LocalizedText Question,
        int ResolutionNoteId,
        LocalizedText ResolutionChoice,
        LocalizedText ResolutionConsequence,
        int CalamityNoteId,
        LocalizedText CalamityChoice,
        LocalizedText CalamityConsequence);

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
