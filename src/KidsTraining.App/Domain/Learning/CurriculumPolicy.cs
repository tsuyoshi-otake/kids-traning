namespace KidsTraining.App.Domain.Learning;

internal sealed record CurriculumQuestionDisplay(
    string? Prompt = null,
    string? Answer = null,
    IReadOnlyList<string>? Choices = null,
    string? Explanation = null,
    string? ActivityPrompt = null);

internal sealed record CurriculumQuestion(
    int Stage,
    string Prompt,
    string Answer,
    IReadOnlyList<string> Distractors,
    string Explanation,
    string? ActivityPrompt = null,
    CurriculumQuestionDisplay? Display = null);

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
        "soroban", "seikatsu", "shakai", "rika", "kateika", "gijutsu", "doutoku", "sougou", "jouhou", "tokubetsu", "thinking"
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

        AddThinkingUnits(AddBank);

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
            Q(1, "1000万は 100万の いくつ分？", "10", ["100", "5", "1000"], "1000万÷100万=10。"),
            Q(1, "9000は 1000の いくつ分？", "9", ["90", "3", "900"], "9000÷1000=9。"),
            Q(2, "864÷24は？", "36", ["34", "38", "42"], "24×36=864。"),
            Q(2, "775÷25は？", "31", ["29", "33", "35"], "25×31=775。"),
            Q(2, "912÷24は？", "38", ["36", "39", "42"], "24×38=912。"),
            Q(3, "3.6×4は？", "14.4", ["1.44", "7.2", "144"], "36×4=144として小数点を1けた戻す。"),
            Q(3, "2.7×3は？", "8.1", ["8.7", "0.81", "81"], "27×3=81として小数点を1けた戻す。"),
            Q(3, "4.5×6は？", "27", ["2.7", "270", "24"], "45×6=270として小数点を1けた戻す。"),
            Q(4, "3/8と同じ大きさは？", "6/16", ["3/16", "6/8", "9/16"], "分子と分母を同じ数で2倍しても大きさは同じ。"),
            Q(4, "2/5と同じ大きさは？", "4/10", ["2/10", "4/5", "6/10"], "分子と分母を同じ数で2倍しても大きさは同じ。"),
            Q(4, "1/4と同じ大きさは？", "2/8", ["1/8", "2/4", "3/8"], "分子と分母を同じ数で2倍しても大きさは同じ。"),
            Q(5, "3987を千の位までの概数にすると？", "4000", ["3000", "3900", "3980"], "百の位が9なので切り上げる。"),
            Q(5, "6521を百の位までの概数にすると？", "6500", ["6600", "6000", "6520"], "十の位が2なので切り捨てる。"),
            Q(5, "4382を十の位までの概数にすると？", "4380", ["4390", "4400", "4370"], "一の位が2なので切り捨てる。")
        ]);
        add("math", "shape", 4, "geometry-measurement", "4年 面積・角・四角形・直方体", MathematicsSource,
        [
            Q(1, "たて8cm、横6cmの長方形の面積は？", "48cm²", ["14cm²", "28cm²", "96cm²"], "長方形の面積は たて×横。"),
            Q(1, "たて5cm、横9cmの長方形の面積は？", "45cm²", ["14cm²", "28cm²", "90cm²"], "長方形の面積は たて×横。"),
            Q(1, "1辺が7cmの正方形の面積は？", "49cm²", ["14cm²", "28cm²", "98cm²"], "正方形の面積は 1辺×1辺。"),
            Q(2, "1m²は何cm²？", "10000cm²", ["100cm²", "1000cm²", "100000cm²"], "1m=100cmなので100×100。"),
            Q(2, "1km²は何m²？", "1000000m²", ["1000m²", "10000m²", "100000m²"], "1km=1000mなので1000×1000。"),
            Q(2, "50000cm²は何m²？", "5m²", ["50m²", "500m²", "0.5m²"], "1m²=10000cm²なので50000÷10000=5。"),
            Q(3, "直角は何度？", "90度", ["45度", "180度", "360度"], "直角は90度。"),
            Q(3, "平角(一直線)は何度？", "180度", ["90度", "270度", "360度"], "一直線は180度。"),
            Q(3, "1回転した角の大きさは何度？", "360度", ["90度", "180度", "270度"], "一回転は直角4つ分なので90×4=360度。"),
            Q(4, "向かい合う2組の辺が平行な四角形は？", "平行四辺形", ["台形", "三角形", "円"], "平行四辺形は向かい合う辺がそれぞれ平行。"),
            Q(4, "4つの辺の長さがすべて等しい四角形は？", "ひし形", ["台形", "長方形", "三角形"], "ひし形は4辺の長さがすべて等しい。"),
            Q(4, "1組の辺だけが平行な四角形は？", "台形", ["平行四辺形", "ひし形", "正方形"], "台形は1組の辺だけが平行である。"),
            Q(5, "直方体の面の数は？", "6", ["4", "8", "12"], "上下面・前後・左右で6面。"),
            Q(5, "直方体の辺の数は？", "12", ["6", "8", "10"], "直方体には辺が12本ある。"),
            Q(5, "直方体の頂点の数は？", "8", ["4", "6", "12"], "直方体には頂点が8つある。")
        ]);
        add("math", "chart", 4, "relations-data", "4年 変化・表・折れ線グラフ・二次元表", MathematicsSource,
        [
            Q(1, "時刻と気温の変化を表すのに向くグラフは？", "折れ線グラフ", ["円グラフ", "絵グラフ", "帯グラフ"], "時間による変化は折れ線グラフで捉えやすい。"),
            Q(1, "折れ線グラフのたてじくに書くものは？", "調べる量と単位", ["調べた人の名前", "グラフの色", "紙の大きさ"], "たてじくには調べる量と単位、よこじくには時こくなどを書く。"),
            Q(1, "月ごとの気温を折れ線グラフにする目的は？", "変化の様子を捉えるため", ["数を覚えるため", "色を決めるため", "名前を書くため"], "折れ線グラフは時間による変化を捉えやすい。"),
            Q(2, "1個120円の品をx個買う代金を表す式は？", "120×x", ["120+x", "x÷120", "120-x"], "1個分×個数で全体の代金になる。"),
            Q(2, "1本80円のえんぴつをx本買う代金を表す式は？", "80×x", ["80+x", "x÷80", "80-x"], "1本分×本数で全体の代金になる。"),
            Q(2, "1人にy個ずつ配るとき、5人分の個数を表す式は？", "y×5", ["y+5", "5÷y", "y-5"], "1人分×人数で全体の個数になる。"),
            Q(3, "2つの条件で人数を分類する表は？", "二次元表", ["数直線", "式", "地図"], "行と列に別々の条件を置く。"),
            Q(3, "好きな教科と学年を同時に分類する表は？", "二次元表", ["折れ線グラフ", "数直線", "えグラフ"], "行と列に2つの条件を置いて整理する。"),
            Q(3, "二次元表の交わるますに書く数は？", "両方の条件に当てはまる人数", ["どちらか一方の合計だけ", "全員の人数だけ", "0だけ"], "行と列の条件を両方満たす人数を書く。"),
            Q(4, "折れ線が右上がりの区間で分かることは？", "値が増えている", ["値が減っている", "値が同じ", "値が0"], "右へ進むほど点が高いので値は増える。"),
            Q(4, "折れ線が右下がりの区間で分かることは？", "値が減っている", ["値が増えている", "値が同じ", "値が0"], "右へ進むほど点が低いので値は減る。"),
            Q(4, "折れ線が水平な区間で分かることは？", "値が変わらない", ["値が増えている", "値が減っている", "値が消える"], "高さが同じなら値は変わらない。"),
            Q(5, "表の縦と横の合計を確かめる理由は？", "分類漏れを見つけるため", ["色を決めるため", "単位を消すため", "順番を逆にするため"], "合計を照合すると重複や漏れに気付きやすい。"),
            Q(5, "アンケート結果を表にまとめる利点は？", "全体の傾向を比べやすい", ["回答者の名前が分かる", "色が分かる", "天気が分かる"], "表にすると数の多さや傾向を比較しやすい。"),
            Q(5, "二次元表で行と列の合計が一致しないとき考えられることは？", "数え間違いがある", ["天気が悪い", "グラフの色が違う", "人気がない"], "合計が合わないときは集計ミスを疑う。")
        ]);

        add("math", "kazu", 5, "number-calculation", "5年 整数の性質・小数の乗除・分数", MathematicsSource,
        [
            Q(1, "24の約数はどれ？", "6", ["5", "7", "10"], "24÷6=4で割り切れる。"),
            Q(1, "18の約数はどれ？", "9", ["8", "7", "5"], "18÷9=2で割り切れる。"),
            Q(1, "30の約数ではないものは？", "7", ["5", "6", "10"], "30の約数は1,2,3,5,6,10,15,30。7は入っていない。"),
            Q(2, "4と6の最小公倍数は？", "12", ["10", "18", "24"], "4と6の最初の共通する倍数は12。"),
            Q(2, "3と5の最小公倍数は？", "15", ["8", "10", "30"], "3と5の最初の共通する倍数は15。"),
            Q(2, "6と8の最小公倍数は？", "24", ["14", "48", "16"], "6と8の最初の共通する倍数は24。"),
            Q(3, "2.4×0.5は？", "1.2", ["0.12", "2.9", "12"], "0.5倍は半分なので1.2。"),
            Q(3, "1.5×0.4は？", "0.6", ["0.06", "6", "0.9"], "15×4=60として小数点を2けた戻す。"),
            Q(3, "3.2×0.25は？", "0.8", ["0.08", "8", "1.28"], "32×25=800として小数点を3けた戻す。"),
            Q(4, "3.6÷0.9は？", "4", ["0.4", "3", "40"], "両方を10倍して36÷9。"),
            Q(4, "4.8÷0.6は？", "8", ["0.8", "80", "48"], "両方を10倍して48÷6。"),
            Q(4, "2.7÷0.3は？", "9", ["0.9", "90", "27"], "両方を10倍して27÷3。"),
            Q(5, "2/3+1/4は？", "11/12", ["3/7", "3/12", "8/12"], "通分すると8/12+3/12=11/12。"),
            Q(5, "1/2+1/3は？", "5/6", ["2/5", "2/6", "3/6"], "通分すると3/6+2/6=5/6。"),
            Q(5, "3/4+1/6は？", "11/12", ["4/10", "4/12", "2/3"], "通分すると9/12+2/12=11/12。")
        ]);
        add("math", "shape", 5, "geometry-measurement", "5年 体積・合同・多角形・角柱", MathematicsSource,
        [
            Q(1, "たて4cm、横3cm、高さ5cmの直方体の体積は？", "60cm³", ["12cm³", "20cm³", "120cm³"], "4×3×5=60。"),
            Q(1, "たて7cm、横4cm、高さ3cmの直方体の体積は？", "84cm³", ["14cm³", "28cm³", "168cm³"], "7×4×3=84。"),
            Q(1, "1辺が4cmの立方体の体積は？", "64cm³", ["12cm³", "16cm³", "48cm³"], "立方体の体積は1辺×1辺×1辺で4×4×4=64。"),
            Q(2, "1Lは何cm³？", "1000cm³", ["100cm³", "10000cm³", "10cm³"], "1Lは1000cm³。"),
            Q(2, "1mLは何cm³？", "1cm³", ["10cm³", "100cm³", "1000cm³"], "1mLは1cm³に等しい。"),
            Q(2, "2.5Lは何cm³？", "2500cm³", ["250cm³", "25000cm³", "2050cm³"], "1L=1000cm³なので2.5×1000=2500。"),
            Q(3, "形も大きさも同じ図形を何という？", "合同", ["相似", "平行", "対称"], "重ね合わせられる図形は合同。"),
            Q(3, "合同な図形で対応する辺の長さは？", "等しい", ["2倍になる", "半分になる", "変わることがある"], "合同な図形は対応する辺や角がそれぞれ等しい。"),
            Q(3, "合同な三角形をかくために必要な条件の一つは？", "3辺の長さ", ["1辺の長さだけ", "色だけ", "面積だけ"], "3辺の長さが分かれば合同な三角形がかける。"),
            Q(4, "正六角形の辺の数は？", "6", ["5", "8", "12"], "六角形には6本の辺がある。"),
            Q(4, "正五角形の辺の数は？", "5", ["4", "6", "10"], "五角形には5本の辺がある。"),
            Q(4, "正八角形の頂点の数は？", "8", ["6", "7", "10"], "八角形には頂点が8つある。"),
            Q(5, "三角柱の底面は何形？", "三角形", ["四角形", "円", "五角形"], "三角柱は合同な三角形を2つ底面にもつ。"),
            Q(5, "四角柱の底面は何形？", "四角形", ["三角形", "円", "五角形"], "四角柱は合同な四角形を2つ底面にもつ。"),
            Q(5, "円柱の底面は何形？", "円", ["四角形", "三角形", "五角形"], "円柱は合同な円を2つ底面にもつ。")
        ]);
        add("math", "chart", 5, "rate-statistics", "5年 平均・単位量・割合・円グラフ", MathematicsSource,
        [
            Q(1, "6, 8, 10の平均は？", "8", ["6", "9", "24"], "合計24を3個で割る。"),
            Q(1, "5, 7, 9, 11の平均は？", "8", ["7", "9", "32"], "合計32を4個で割る。"),
            Q(1, "3, 5, 4, 8の平均は？", "5", ["4", "20", "6"], "合計20を4個で割る。"),
            Q(2, "240kmを3時間で進む平均の速さは？", "80km/h", ["60km/h", "120km/h", "720km/h"], "道のり÷時間=80。"),
            Q(2, "180kmを2時間で進む平均の速さは？", "90km/h", ["60km/h", "120km/h", "360km/h"], "道のり÷時間=90。"),
            Q(2, "2時間で320km進む自動車の時速は？", "160km/h", ["80km/h", "640km/h", "120km/h"], "道のり÷時間=160。"),
            Q(3, "50人の20%は何人？", "10人", ["5人", "20人", "25人"], "50×0.2=10。"),
            Q(3, "80人の25%は何人？", "20人", ["16人", "25人", "40人"], "80×0.25=20。"),
            Q(3, "200円の30%は何円？", "60円", ["30円", "70円", "170円"], "200×0.3=60。"),
            Q(4, "全体に対する部分の割合を表すのに向くグラフは？", "円グラフ", ["折れ線グラフ", "数直線", "散布図"], "円全体を100%として割合を示す。"),
            Q(4, "割合を長方形の帯で表すグラフは？", "帯グラフ", ["折れ線グラフ", "円グラフ", "二次元表"], "帯グラフは全体を100%とした帯で割合を表す。"),
            Q(4, "円グラフで角度が100%を360度で表すとき、50%は何度？", "180度", ["90度", "120度", "270度"], "360×0.5=180。"),
            Q(5, "比べる量を求める式は？", "もとにする量×割合", ["もとにする量÷割合", "割合-もとにする量", "割合÷もとにする量"], "比べる量=もとにする量×割合。"),
            Q(5, "割合を求める式は？", "比べる量÷もとにする量", ["もとにする量÷比べる量", "比べる量×もとにする量", "比べる量-もとにする量"], "割合=比べる量÷もとにする量。"),
            Q(5, "もとにする量を求める式は？", "比べる量÷割合", ["比べる量×割合", "割合÷比べる量", "比べる量+割合"], "もとにする量=比べる量÷割合。")
        ]);

        add("math", "frac", 6, "fraction-expression", "6年 分数の乗除・文字式", MathematicsSource,
        [
            Q(1, "2/3×3/5は？", "2/5", ["5/8", "6/8", "1/5"], "分子どうし・分母どうしを掛けて約分する。", display: D(
                prompt: @"\(\frac{2}{3}\times\frac{3}{5}\) は？",
                answer: @"\(\frac{2}{5}\)",
                choices: [@"\(\frac{5}{8}\)", @"\(\frac{6}{8}\)", @"\(\frac{1}{5}\)"],
                explanation: @"分子どうし・分母どうしを掛けて約分します。**答えは \(\frac{2}{5}\) です。**")),
            Q(1, "1/2×2/3は？", "1/3", ["2/3", "3/4", "7/6"], "分子どうし・分母どうしを掛けて約分すると1/3になる。"),
            Q(1, "2/3×3/4は？", "1/2", ["5/7", "1/4", "3/2"], "分子どうし・分母どうしを掛けて約分すると1/2になる。"),
            Q(2, "3/4÷2/5は？", "15/8", ["6/20", "5/6", "8/15"], "割る数の逆数5/2を掛ける。", display: D(
                prompt: @"\(\frac{3}{4}\div\frac{2}{5}\) は？",
                answer: @"\(\frac{15}{8}\)",
                choices: [@"\(\frac{6}{20}\)", @"\(\frac{5}{6}\)", @"\(\frac{8}{15}\)"],
                explanation: @"割る数の逆数 \(\frac{5}{2}\) を掛けます。")),
            Q(2, "2/3÷1/4は？", "8/3", ["3/8", "1/6", "6/4"], "割る数1/4の逆数4を掛けると8/3になる。"),
            Q(2, "5/6÷1/3は？", "5/2", ["5/18", "2/5", "5/9"], "割る数1/3の逆数3を掛けると5/2になる。"),
            Q(3, "1個x円の品を6個買う代金は？", "6x円", ["x+6円", "x÷6円", "6-x円"], "1個の値段×個数。"),
            Q(3, "1本y円のジュースを4本買う代金は？", "4y円", ["y+4円", "y÷4円", "4-y円"], "1本の値段×本数。"),
            Q(3, "1冊a円のノートを3冊買う代金は？", "3a円", ["a+3円", "a÷3円", "3-a円"], "1冊の値段×冊数。"),
            Q(4, "分数で割るときに行うことは？", "逆数を掛ける", ["分母だけ足す", "分子だけ引く", "小数点を消す"], "a÷b/c は a×c/b。"),
            Q(4, "2/3÷4/5を計算するときにかける数は？", "5/4", ["4/5", "3/2", "2/1"], "割る数4/5の逆数5/4を掛ける。"),
            Q(4, "整数を分数で割るとき最初にすることは？", "整数を分数になおす", ["整数をそのまま使う", "分母をたす", "分子を消す"], "整数は分母が1の分数とみて計算すると割り算がしやすい。"),
            Q(5, "速さを求める式は？", "道のり÷時間", ["道のり×時間", "時間÷道のり", "道のり+時間"], "単位時間あたりの道のりを求める。"),
            Q(5, "道のりを求める式は？", "速さ×時間", ["速さ÷時間", "時間÷速さ", "速さ-時間"], "道のり=速さ×時間。"),
            Q(5, "時間を求める式は？", "道のり÷速さ", ["道のり×速さ", "速さ÷道のり", "道のり-速さ"], "時間=道のり÷速さ。")
        ]);
        add("math", "shape", 6, "geometry-measurement", "6年 対称・拡大縮小・面積・体積", MathematicsSource,
        [
            Q(1, "1本の直線で折ると重なる図形は？", "線対称", ["点対称", "合同でない", "平行"], "折り目になる直線が対称の軸。"),
            Q(1, "線対称な図形で対称の軸に関して対応する点を結ぶと軸と？", "垂直に交わる", ["平行になる", "重ならない", "一致する"], "対応する点を結ぶ線分は対称の軸と垂直に交わる。"),
            Q(1, "正方形の対称の軸の数は？", "4", ["1", "2", "8"], "正方形には対称の軸が4本ある。"),
            Q(2, "1つの点を中心に180度回すと重なる図形は？", "点対称", ["線対称だけ", "相似でない", "垂直"], "中心を対称の中心という。"),
            Q(2, "点対称な図形で対応する点と対称の中心を結んだ長さの関係は？", "対称の中心から等しい距離にある", ["対称の中心に近い方が長い", "関係がない", "必ず2倍になる"], "対応する点は対称の中心から等距離にある。"),
            Q(2, "平行四辺形は点対称な図形と言えるか？", "言える", ["言えない", "線対称のときだけ言える", "正方形のときだけ言える"], "平行四辺形は対角線の交点を中心に点対称である。"),
            Q(3, "半径4cmの円の面積を表す式は？", "4×4×3.14", ["4×2×3.14", "4×3.14", "8×8"], "円の面積=半径×半径×円周率。"),
            Q(3, "半径5cmの円の面積を表す式は？", "5×5×3.14", ["5×2×3.14", "5×3.14", "10×10"], "円の面積=半径×半径×円周率。"),
            Q(3, "直径8cmの円の面積を表す式は？", "4×4×3.14", ["8×8×3.14", "4×3.14", "8×3.14"], "直径8cmの半径は4cmなので4×4×3.14。"),
            Q(4, "底面積12cm²、高さ5cmの角柱の体積は？", "60cm³", ["17cm³", "34cm³", "120cm³"], "角柱の体積=底面積×高さ。"),
            Q(4, "底面積20cm²、高さ6cmの角柱の体積は？", "120cm³", ["26cm³", "52cm³", "240cm³"], "角柱の体積=底面積×高さ。"),
            Q(4, "底面積15cm²、高さ4cmの角柱の体積は？", "60cm³", ["19cm³", "38cm³", "240cm³"], "角柱の体積=底面積×高さ。"),
            Q(5, "2倍の拡大図で長さ3cmの辺は？", "6cm", ["1.5cm", "3cm", "9cm"], "対応する長さを2倍する。"),
            Q(5, "3倍の拡大図で長さ2cmの辺は？", "6cm", ["1.5cm", "2cm", "5cm"], "対応する長さを3倍する。"),
            Q(5, "1/2の縮図で長さ8cmの辺は？", "4cm", ["16cm", "2cm", "6cm"], "対応する長さを1/2にする。")
        ]);
        add("math", "chart", 6, "ratio-data", "6年 比・比例・データ分析", MathematicsSource,
        [
            Q(1, "12:18を最も簡単な比にすると？", "2:3", ["3:2", "6:9", "12:6"], "両方を6で割る。"),
            Q(1, "15:25を最も簡単な比にすると？", "3:5", ["5:3", "5:15", "15:5"], "両方を5で割る。"),
            Q(1, "8:20を最も簡単な比にすると？", "2:5", ["5:2", "4:10", "2:20"], "両方を4で割る。"),
            Q(2, "yがxに比例し、x=3でy=12。x=5では？", "20", ["14", "15", "60"], "y=4xなので20。"),
            Q(2, "yがxに比例し、x=2でy=8。x=6では？", "24", ["12", "16", "48"], "y=4xなので24。"),
            Q(2, "yがxに比例し、x=4でy=20。x=7では？", "35", ["23", "28", "140"], "y=5xなので35。"),
            Q(3, "2, 3, 3, 7, 10の最頻値は？", "3", ["2", "5", "10"], "最も多く現れる値は3。"),
            Q(3, "4, 5, 5, 5, 8, 9の最頻値は？", "5", ["4", "8", "9"], "最も多く現れる値は5。"),
            Q(3, "1, 2, 2, 6, 6, 6, 9の最頻値は？", "6", ["2", "9", "1"], "最も多く現れる値は6。"),
            Q(4, "データを小さい順に並べた中央の値は？", "中央値", ["平均値", "最頻値", "最大値"], "中央に位置する値を中央値という。"),
            Q(4, "資料の合計をデータの個数で割った値は？", "平均値", ["中央値", "最頻値", "最大値"], "合計÷個数で求めるのは平均値。"),
            Q(4, "最も多く現れる値を何という？", "最頻値", ["中央値", "平均値", "範囲"], "最も多く現れる値を最頻値という。"),
            Q(5, "表が比例か確かめる見方は？", "y÷xが一定", ["x+yが一定", "y-xが必ず0", "xだけを見る"], "比例ではy/xが一定になる。"),
            Q(5, "xが2倍、3倍になるとyも2倍、3倍になるとき、xとyは？", "比例している", ["反比例している", "無関係である", "比例していない"], "xの倍数分だけyも増えるのは比例の性質。"),
            Q(5, "反比例の表で確かめる見方は？", "x×yが一定", ["x+yが一定", "y÷xが一定", "xだけが一定"], "反比例ではx×yが一定になる。")
        ]);
    }

    private static CurriculumQuestion Q(
        int stage,
        string prompt,
        string answer,
        IReadOnlyList<string> distractors,
        string explanation,
        string? activity = null,
        CurriculumQuestionDisplay? display = null) =>
        new(stage, prompt, answer, distractors, explanation, activity, display);

    private static CurriculumQuestionDisplay D(
        string? prompt = null,
        string? answer = null,
        IReadOnlyList<string>? choices = null,
        string? explanation = null,
        string? activityPrompt = null) =>
        new(prompt, answer, choices, explanation, activityPrompt);

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
        "thinking" => "思考トレーニング",
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
