namespace KidsTraining.App.Domain.Learning;

internal sealed record CurriculumQuestion(
    int Stage,
    string Prompt,
    string Answer,
    IReadOnlyList<string> Distractors,
    string Explanation,
    string? ActivityPrompt = null);

internal sealed record CurriculumUnit(
    string Id,
    string SubjectId,
    string TopicId,
    string Label,
    int Grade,
    int Order,
    IReadOnlyList<string> Prerequisites,
    string GeneratorKey,
    string AssessmentMode,
    string SourceReference,
    IReadOnlyList<CurriculumQuestion> Questions);

internal static partial class CurriculumPolicy
{
    private const string JapaneseSource = "https://www.mext.go.jp/content/20220606-mxt_kyoiku02-100002607_002.pdf";
    private const string MathematicsSource = "https://www.mext.go.jp/content/20211102-mxt_kyoiku02-100002607_04.pdf";
    private const string SocialSource = "https://www.mext.go.jp/content/20230308-mxt_kyoiku02-100002607_003.pdf";
    private const string ScienceSource = "https://www.mext.go.jp/content/20211020-mxt_kyoiku02-100002607_05.pdf";
    private const string EnglishSource = "https://www.mext.go.jp/content/20220614-mxt_kyoiku02-100002607_11.pdf";
    private const string HomeEconomicsSource = "https://www.mext.go.jp/content/20240918-mxt_kyoiku01-100002607_02.pdf";
    private const string GeneralSource = "https://www.mext.go.jp/a_menu/shotou/new-cs/1384661.htm";
    private const string MiddleJapaneseSource = "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_002.pdf";
    private const string MiddleMathematicsSource = "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_004.pdf";
    private const string MiddleSocialSource = "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_003.pdf";
    private const string MiddleScienceSource = "https://www.mext.go.jp/content/20210830-mxt_kyoiku01-100002608_05.pdf";
    private const string MiddleEnglishSource = "https://www.mext.go.jp/content/20210531-mxt_kyoiku01-100002608_010.pdf";
    private const string MiddleTechnologyHomeSource = "https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_009.pdf";
    private const string MiddleGeneralSource = "https://www.mext.go.jp/a_menu/shotou/new-cs/1387016.htm";

    private static readonly string[] TopicKeys =
    [
        "add", "sub", "mul", "clock", "kokugo", "hissan", "moji", "measure", "kazu", "shape", "div",
        "frac", "chart", "story", "bun", "goi", "dokkai", "eigo", "money", "groups", "order", "keyboard",
        "soroban", "seikatsu", "shakai", "rika", "kateika", "gijutsu", "doutoku", "sougou", "jouhou", "tokubetsu"
    ];

    private static readonly IReadOnlyList<CurriculumUnit> Units = BuildUnits();
    private static readonly IReadOnlyDictionary<string, CurriculumUnit> UnitsById =
        Units.ToDictionary(static unit => unit.Id, StringComparer.Ordinal);
    private static readonly IReadOnlyList<IReadOnlyList<string>> SubjectLanes =
        Units.GroupBy(static unit => unit.SubjectId, StringComparer.Ordinal)
            .Select(static group => (IReadOnlyList<string>)group.OrderBy(static unit => unit.Order).Select(static unit => unit.Id).ToArray())
            .ToArray();

    public static int NormalizeGrade(int grade) => Math.Clamp(grade, 1, 9);

    public static IReadOnlyList<IReadOnlyList<string>> CurriculumLanes => SubjectLanes;

    public static IReadOnlyList<CurriculumUnit> AllUnits => Units;

    public static IReadOnlyList<string> AllTopics => TopicKeys;

    public static CurriculumUnit Unit(string unitId) =>
        UnitsById.TryGetValue(unitId, out var unit)
            ? unit
            : throw new KeyNotFoundException($"Unknown curriculum unit: {unitId}");

    public static bool IsAvailable(int grade, string topic) =>
        Units.Any(unit => unit.Grade == NormalizeGrade(grade) && string.Equals(unit.TopicId, topic, StringComparison.Ordinal));

    public static IReadOnlyList<string> TopicsForGrade(int grade) =>
        Units.Where(unit => unit.Grade == NormalizeGrade(grade))
            .Select(static unit => unit.TopicId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CurriculumUnit> UnitsForGrade(int grade) =>
        Units.Where(unit => unit.Grade == NormalizeGrade(grade)).ToArray();

    private static IReadOnlyList<CurriculumUnit> BuildUnits()
    {
        var units = new List<CurriculumUnit>();
        var previousBySubject = new Dictionary<string, string>(StringComparer.Ordinal);
        var orderBySubject = new Dictionary<string, int>(StringComparer.Ordinal);

        void Add(
            string subject,
            string topic,
            int grade,
            string slug,
            string label,
            string source,
            string generator,
            string assessment,
            IReadOnlyList<CurriculumQuestion>? questions = null)
        {
            var id = $"{subject}.g{grade}.{slug}";
            var prerequisites = previousBySubject.TryGetValue(subject, out var previous) ? new[] { previous } : [];
            var order = orderBySubject.TryGetValue(subject, out var currentOrder) ? currentOrder + 1 : 1;
            units.Add(new CurriculumUnit(
                id,
                subject,
                topic,
                label,
                grade,
                order,
                prerequisites,
                generator,
                assessment,
                source,
                questions ?? []));
            previousBySubject[subject] = id;
            orderBySubject[subject] = order;
        }

        void AddLegacyLane(string subject, int grade, string source, params string[] topics)
        {
            foreach (var topic in topics)
            {
                Add(subject, topic, grade, topic, $"{grade}年 {TopicLabel(topic)}", source, topic, "auto");
            }
        }

        void AddBank(
            string subject,
            string topic,
            int grade,
            string slug,
            string label,
            string source,
            params CurriculumQuestion[] questions) =>
            Add(
                subject,
                topic,
                grade,
                slug,
                label,
                source,
                "curriculum-bank",
                questions.Any(static question => question.ActivityPrompt is not null) ? "auto+activity" : "auto",
                questions);

        AddLegacyLane("math", 1, MathematicsSource, "kazu", "shape", "add", "sub", "clock", "measure", "story", "money", "groups", "chart");
        AddLegacyLane("japanese", 1, JapaneseSource, "moji", "bun", "kokugo", "goi", "dokkai");
        AddLegacyLane("life", 1, GeneralSource, "seikatsu");
        AddLegacyLane("moral", 1, GeneralSource, "doutoku");
        AddLegacyLane("information", 1, GeneralSource, "jouhou");
        AddLegacyLane("special-activities", 1, GeneralSource, "tokubetsu");
        AddLegacyLane("keyboard", 1, GeneralSource, "keyboard");

        AddLegacyLane("math", 2, MathematicsSource, "chart", "clock", "add", "sub", "measure", "hissan", "story", "kazu", "money", "order", "groups", "mul", "shape", "frac");
        AddLegacyLane("japanese", 2, JapaneseSource, "kokugo", "bun", "goi", "dokkai", "moji");
        AddLegacyLane("life", 2, GeneralSource, "seikatsu");
        AddLegacyLane("moral", 2, GeneralSource, "doutoku");
        AddLegacyLane("information", 2, GeneralSource, "jouhou");
        AddLegacyLane("special-activities", 2, GeneralSource, "tokubetsu");
        AddLegacyLane("keyboard", 2, GeneralSource, "keyboard");

        AddLegacyLane("math", 3, MathematicsSource, "mul", "div", "shape", "hissan", "kazu", "soroban", "add", "sub", "clock", "measure", "story", "order", "chart", "frac", "money", "groups");
        AddLegacyLane("japanese", 3, JapaneseSource, "kokugo", "bun", "goi", "dokkai", "moji");
        AddLegacyLane("social", 3, SocialSource, "shakai");
        AddLegacyLane("science", 3, ScienceSource, "rika");
        AddLegacyLane("english", 3, EnglishSource, "eigo");
        AddLegacyLane("moral", 3, GeneralSource, "doutoku");
        AddLegacyLane("information", 3, GeneralSource, "jouhou");
        AddLegacyLane("integrated", 3, GeneralSource, "sougou");
        AddLegacyLane("special-activities", 3, GeneralSource, "tokubetsu");
        AddLegacyLane("keyboard", 3, GeneralSource, "keyboard");

        AddUpperGradeUnits(AddBank, Add);
        AddMiddleCore(AddBank);
        AddMiddleActivities(AddBank);
        return units;
    }

    private static void AddUpperGradeUnits(
        Action<string, string, int, string, string, string, CurriculumQuestion[]> addBank,
        Action<string, string, int, string, string, string, string, string, IReadOnlyList<CurriculumQuestion>?> add)
    {
        AddUpperMathematics(addBank);
        AddUpperJapanese(addBank, add);
        AddUpperScience(addBank);
        AddUpperSocial(addBank);
        AddUpperEnglish(addBank);
        AddHomeEconomics(addBank);
        AddUpperActivities(addBank);
    }

    private static void AddUpperMathematics(Action<string, string, int, string, string, string, CurriculumQuestion[]> add)
    {
        add("math", "kazu", 4, "number-calculation", "4年 大きな数・除法・小数・分数・概数", MathematicsSource,
        [
            Q(1, "1億は 1万の いくつ分？", "10000", ["100", "1000", "100000"], "1億÷1万=10000。"),
            Q(2, "864÷24は？", "36", ["34", "38", "42"], "24×36=864。"),
            Q(3, "3.6×4は？", "14.4", ["1.44", "7.2", "144"], "36×4=144として小数点を1けた戻す。"),
            Q(4, "3/8と同じ大きさは？", "6/16", ["3/16", "6/8", "9/16"], "分子と分母を同じ数で2倍しても大きさは同じ。"),
            Q(5, "3987を千の位までの概数にすると？", "4000", ["3000", "3900", "3980"], "百の位が9なので切り上げる。")
        ]);
        add("math", "shape", 4, "geometry-measurement", "4年 面積・角・四角形・直方体", MathematicsSource,
        [
            Q(1, "たて8cm、横6cmの長方形の面積は？", "48cm²", ["14cm²", "28cm²", "96cm²"], "長方形の面積は たて×横。"),
            Q(2, "1m²は何cm²？", "10000cm²", ["100cm²", "1000cm²", "100000cm²"], "1m=100cmなので100×100。"),
            Q(3, "直角は何度？", "90度", ["45度", "180度", "360度"], "直角は90度。"),
            Q(4, "向かい合う2組の辺が平行な四角形は？", "平行四辺形", ["台形", "三角形", "円"], "平行四辺形は向かい合う辺がそれぞれ平行。"),
            Q(5, "直方体の面の数は？", "6", ["4", "8", "12"], "上下面・前後・左右で6面。")
        ]);
        add("math", "chart", 4, "relations-data", "4年 変化・表・折れ線グラフ・二次元表", MathematicsSource,
        [
            Q(1, "時刻と気温の変化を表すのに向くグラフは？", "折れ線グラフ", ["円グラフ", "絵グラフ", "帯グラフ"], "時間による変化は折れ線グラフで捉えやすい。"),
            Q(2, "1個120円の品をx個買う代金を表す式は？", "120×x", ["120+x", "x÷120", "120-x"], "1個分×個数で全体の代金になる。"),
            Q(3, "2つの条件で人数を分類する表は？", "二次元表", ["数直線", "式", "地図"], "行と列に別々の条件を置く。"),
            Q(4, "折れ線が右上がりの区間で分かることは？", "値が増えている", ["値が減っている", "値が同じ", "値が0"], "右へ進むほど点が高いので値は増える。"),
            Q(5, "表の縦と横の合計を確かめる理由は？", "分類漏れを見つけるため", ["色を決めるため", "単位を消すため", "順番を逆にするため"], "合計を照合すると重複や漏れに気付きやすい。")
        ]);

        add("math", "kazu", 5, "number-calculation", "5年 整数の性質・小数の乗除・分数", MathematicsSource,
        [
            Q(1, "24の約数はどれ？", "6", ["5", "7", "10"], "24÷6=4で割り切れる。"),
            Q(2, "4と6の最小公倍数は？", "12", ["10", "18", "24"], "4と6の最初の共通する倍数は12。"),
            Q(3, "2.4×0.5は？", "1.2", ["0.12", "2.9", "12"], "0.5倍は半分なので1.2。"),
            Q(4, "3.6÷0.9は？", "4", ["0.4", "3", "40"], "両方を10倍して36÷9。"),
            Q(5, "2/3+1/4は？", "11/12", ["3/7", "3/12", "8/12"], "通分すると8/12+3/12=11/12。")
        ]);
        add("math", "shape", 5, "geometry-measurement", "5年 体積・合同・多角形・角柱", MathematicsSource,
        [
            Q(1, "たて4cm、横3cm、高さ5cmの直方体の体積は？", "60cm³", ["12cm³", "20cm³", "120cm³"], "4×3×5=60。"),
            Q(2, "1Lは何cm³？", "1000cm³", ["100cm³", "10000cm³", "10cm³"], "1Lは1000cm³。"),
            Q(3, "形も大きさも同じ図形を何という？", "合同", ["相似", "平行", "対称"], "重ね合わせられる図形は合同。"),
            Q(4, "正六角形の辺の数は？", "6", ["5", "8", "12"], "六角形には6本の辺がある。"),
            Q(5, "三角柱の底面は何形？", "三角形", ["四角形", "円", "五角形"], "三角柱は合同な三角形を2つ底面にもつ。")
        ]);
        add("math", "chart", 5, "rate-statistics", "5年 平均・単位量・割合・円グラフ", MathematicsSource,
        [
            Q(1, "6, 8, 10の平均は？", "8", ["6", "9", "24"], "合計24を3個で割る。"),
            Q(2, "240kmを3時間で進む平均の速さは？", "80km/h", ["60km/h", "120km/h", "720km/h"], "道のり÷時間=80。"),
            Q(3, "50人の20%は何人？", "10人", ["5人", "20人", "25人"], "50×0.2=10。"),
            Q(4, "全体に対する部分の割合を表すのに向くグラフは？", "円グラフ", ["折れ線グラフ", "数直線", "散布図"], "円全体を100%として割合を示す。"),
            Q(5, "比べる量を求める式は？", "もとにする量×割合", ["もとにする量÷割合", "割合-もとにする量", "割合÷100だけ"], "比べる量=もとにする量×割合。")
        ]);

        add("math", "frac", 6, "fraction-expression", "6年 分数の乗除・文字式", MathematicsSource,
        [
            Q(1, "2/3×3/5は？", "2/5", ["5/8", "6/8", "1/5"], "分子どうし・分母どうしを掛けて約分する。"),
            Q(2, "3/4÷2/5は？", "15/8", ["6/20", "5/6", "8/15"], "割る数の逆数5/2を掛ける。"),
            Q(3, "1個x円の品を6個買う代金は？", "6x円", ["x+6円", "x÷6円", "6-x円"], "1個の値段×個数。"),
            Q(4, "分数で割るときに行うことは？", "逆数を掛ける", ["分母だけ足す", "分子だけ引く", "小数点を消す"], "a÷b/c は a×c/b。"),
            Q(5, "速さを求める式は？", "道のり÷時間", ["道のり×時間", "時間÷道のり", "道のり+時間"], "単位時間あたりの道のりを求める。")
        ]);
        add("math", "shape", 6, "geometry-measurement", "6年 対称・拡大縮小・面積・体積", MathematicsSource,
        [
            Q(1, "1本の直線で折ると重なる図形は？", "線対称", ["点対称", "合同でない", "平行"], "折り目になる直線が対称の軸。"),
            Q(2, "1つの点を中心に180度回すと重なる図形は？", "点対称", ["線対称だけ", "相似でない", "垂直"], "中心を対称の中心という。"),
            Q(3, "半径4cmの円の面積を表す式は？", "4×4×3.14", ["4×2×3.14", "4×3.14", "8×8"], "円の面積=半径×半径×円周率。"),
            Q(4, "底面積12cm²、高さ5cmの角柱の体積は？", "60cm³", ["17cm³", "34cm³", "120cm³"], "角柱の体積=底面積×高さ。"),
            Q(5, "2倍の拡大図で長さ3cmの辺は？", "6cm", ["1.5cm", "3cm", "9cm"], "対応する長さを2倍する。")
        ]);
        add("math", "chart", 6, "ratio-data", "6年 比・比例・データ分析", MathematicsSource,
        [
            Q(1, "12:18を最も簡単な比にすると？", "2:3", ["3:2", "6:9", "12:6"], "両方を6で割る。"),
            Q(2, "yがxに比例し、x=3でy=12。x=5では？", "20", ["14", "15", "60"], "y=4xなので20。"),
            Q(3, "2, 3, 3, 7, 10の最頻値は？", "3", ["2", "5", "10"], "最も多く現れる値は3。"),
            Q(4, "データを小さい順に並べた中央の値は？", "中央値", ["平均値", "最頻値", "最大値"], "中央に位置する値を中央値という。"),
            Q(5, "表が比例か確かめる見方は？", "y÷xが一定", ["x+yが一定", "y-xが必ず0", "xだけを見る"], "比例ではy/xが一定になる。")
        ]);
    }

    private static CurriculumQuestion Q(
        int stage,
        string prompt,
        string answer,
        IReadOnlyList<string> distractors,
        string explanation,
        string? activity = null) =>
        new(stage, prompt, answer, distractors, explanation, activity);

    private static string TopicLabel(string topic) => topic switch
    {
        "add" => "たし算",
        "sub" => "ひき算",
        "mul" => "かけ算",
        "clock" => "時刻と時間",
        "kokugo" => "漢字",
        "hissan" => "筆算",
        "moji" => "文字",
        "measure" => "単位",
        "kazu" => "数",
        "shape" => "図形",
        "div" => "わり算",
        "frac" => "分数",
        "chart" => "表とグラフ",
        "story" => "文章題",
        "bun" => "文",
        "goi" => "言葉",
        "dokkai" => "読解",
        "eigo" => "外国語",
        "money" => "お金",
        "groups" => "同じ数のまとまり",
        "order" => "式の順序",
        "keyboard" => "キーボード",
        "soroban" => "そろばん",
        "seikatsu" => "生活",
        "shakai" => "社会",
        "rika" => "理科",
        "kateika" => "家庭科",
        "gijutsu" => "技術",
        "doutoku" => "道徳",
        "sougou" => "総合",
        "jouhou" => "情報",
        "tokubetsu" => "特別活動",
        _ => topic
    };

    private static partial void AddUpperJapanese(
        Action<string, string, int, string, string, string, CurriculumQuestion[]> addBank,
        Action<string, string, int, string, string, string, string, string, IReadOnlyList<CurriculumQuestion>?> add);

    private static partial void AddUpperScience(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddUpperSocial(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddUpperEnglish(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddHomeEconomics(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddUpperActivities(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddMiddleCore(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);

    private static partial void AddMiddleActivities(Action<string, string, int, string, string, string, CurriculumQuestion[]> add);
}
