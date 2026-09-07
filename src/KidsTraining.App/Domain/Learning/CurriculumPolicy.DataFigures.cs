namespace KidsTraining.App.Domain.Learning;

internal sealed record CurriculumFigure(
    string Kind, string Title, string XLabel, string YLabel,
    IReadOnlyList<string> Labels, IReadOnlyList<double> Values,
    IReadOnlyList<string>? RowLabels = null,
    IReadOnlyList<IReadOnlyList<double>>? Table = null,
    string? RowHeading = null);

internal static partial class CurriculumPolicy
{
    private static void AddDataFigureQuestions(List<CurriculumUnit> units)
    {
        void Add(string id, params CurriculumQuestion[] questions)
        {
            var index = units.FindIndex(unit => unit.Id == id);
            if (index < 0) throw new InvalidOperationException($"Missing figure unit {id}");
            units[index] = units[index] with { Questions = units[index].Questions.Concat(questions).ToArray() };
        }
        var temperature = new CurriculumFigure("line", "ある日の気温", "時刻（時）", "気温（℃）", ["8", "10", "12", "14", "16"], [16, 20, 24, 22, 18]);
        var table = new CurriculumFigure("table", "学年と好きな遊び", "遊び", "人数（人）", ["おにごっこ", "なわとび"], [], ["3年", "4年"], [[12, 8], [9, 11]], RowHeading: "学年");
        Add("math.g4.relations-data",
            Q(4, "折れ線グラフを見て、8時から12時までに気温は何℃上がった？", "8℃", ["4℃", "24℃", "40℃"], "8時は16℃、12時は24℃。24−16=8℃。") with { Figure = temperature },
            Q(3, "二次元表を見て、なわとびが好きな3年生と4年生は合わせて何人？", "19人", ["20人", "21人", "40人"], "なわとびの列を足す。8+11=19人。") with { Figure = table });
        var favorite = new CurriculumFigure("pie", "好きな運動（全体40人）", "運動", "割合（%）", ["水泳", "球技", "陸上", "その他"], [40, 30, 20, 10]);
        Add("math.g5.rate-statistics",
            Q(3, "円グラフを見て、水泳が好きな人は40人のうち何人？", "16人", ["10人", "24人", "40人"], "水泳は40%。40×0.4=16人。") with { Figure = favorite },
            Q(4, "帯グラフを見て、球技と陸上が好きな人の割合の合計は？", "50%", ["10%", "30%", "70%"], "球技30%と陸上20%を足して50%。") with { Figure = favorite with { Kind = "band" } });
        var frequency = new CurriculumFigure("bar", "読書時間の度数分布", "時間（分、以上〜未満）", "人数（人）", ["0〜10", "10〜20", "20〜30", "30〜40"], [2, 6, 8, 4]);
        Add("math.g6.ratio-data",
            Q(3, "度数分布のグラフを見て、読書時間が20分以上の人は合わせて何人？", "12人", ["8人", "10人", "20人"], "20〜30分の8人と30〜40分の4人を足して12人。") with { Figure = frequency },
            Q(4, "度数分布のグラフを見て、10分以上20分未満の人は全体の何%？", "30%", ["20%", "40%", "60%"], "全体は2+6+8+4=20人。6÷20×100=30%。") with { Figure = frequency });
        var proportion = new CurriculumFigure("line", "比例のグラフ", "x", "y", ["-2", "-1", "0", "1", "2"], [-6, -3, 0, 3, 6]);
        Add("math.g7.proportion-functions",
            Q(1, "比例のグラフを見て、x=2のときのyは？", "6", ["2", "3", "-6"], "横軸の2から上へたどると、グラフの点のyは6。") with { Figure = proportion },
            Q(5, "比例のグラフを見て、比例定数を求めよう。", "3", ["-3", "6", "1/3"], "点(2,6)からy÷x=6÷2=3。") with { Figure = proportion });
        var linear = new CurriculumFigure("line", "一次関数のグラフ", "x", "y", ["0", "1", "2", "3", "4"], [1, 3, 5, 7, 9]);
        Add("math.g8.linear-functions",
            Q(1, "一次関数のグラフを見て、y軸との交点のy座標は？", "1", ["0", "2", "3"], "x=0のときy=1なので、y軸との交点のy座標は1。") with { Figure = linear },
            Q(3, "一次関数のグラフを見て、xが1から3へ増えるときのyの増加量は？", "4", ["2", "7", "10"], "x=1でy=3、x=3でy=7。7−3=4。") with { Figure = linear });
        var box = new CurriculumFigure("box", "通学時間の箱ひげ図", "時間（分）", "", ["最小値", "第1四分位数", "中央値", "第3四分位数", "最大値"], [2, 4, 6, 9, 12]);
        Add("math.g8.probability-distribution",
            Q(3, "箱ひげ図を見て、四分位範囲は何分？", "5分", ["3分", "6分", "10分"], "箱の右端9分から左端4分を引く。9−4=5分。") with { Figure = box },
            Q(5, "箱ひげ図を見て、通学時間の範囲は何分？", "10分", ["5分", "6分", "12分"], "最大値12分から最小値2分を引く。12−2=10分。") with { Figure = box });
        var quadratic = new CurriculumFigure("quadratic", "関数y=ax²のグラフ", "x", "y", ["-3", "-2", "-1", "0", "1", "2", "3"], [9, 4, 1, 0, 1, 4, 9]);
        Add("math.g9.quadratic-functions",
            Q(1, "放物線のグラフを見て、x=-2のときのyは？", "4", ["-4", "2", "-2"], "x=-2の点のy座標は4。負のxでもyは正になる。") with { Figure = quadratic },
            Q(3, "放物線のグラフを見て、xが1から3まで増えるときの変化の割合は？", "4", ["2", "8", "9"], "yは1から9へ増える。(9−1)÷(3−1)=4。") with { Figure = quadratic });
    }
}
