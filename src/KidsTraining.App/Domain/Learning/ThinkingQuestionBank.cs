namespace KidsTraining.App.Domain.Learning;

// Bounded, deterministic authoring: six reasoning families at every stage.
// The runtime retains bank shuffling, duplicate avoidance and evidence handling.
internal static class ThinkingQuestionBank
{
    public static CurriculumQuestion[] Build(int grade)
    {
        if (grade is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(grade));
        var questions = new List<CurriculumQuestion>();
        for (var stage = 1; stage <= 5; stage++)
        for (var variant = 0; variant < 4; variant++)
        {
            questions.Add(Order(grade, stage, variant));
            questions.Add(Assignment(grade, stage, variant));
            questions.Add(Reverse(grade, stage, variant));
            questions.Add(Route(grade, stage, variant));
            questions.Add(Combination(grade, stage, variant));
            questions.Add(Strategy(grade, stage, variant));
        }
        return questions.ToArray();
    }

    private static CurriculumQuestion Choice(int stage, string family, string prompt,
        string answer, IEnumerable<string> alternatives, string explanation)
    {
        var distractors = alternatives.Where(value => value != answer).Distinct(StringComparer.Ordinal).Take(3).ToArray();
        if (distractors.Length != 3) throw new InvalidOperationException($"Insufficient choices: {prompt}");
        return new(stage, $"【{family}】{prompt}", answer, distractors, explanation);
    }

    private static CurriculumQuestion Number(int stage, string family, string prompt, int answer, string explanation) =>
        Choice(stage, family, prompt, answer.ToString(),
            new[] { answer + 1, Math.Max(0, answer - 1), answer + 2, answer + 3 }.Select(value => value.ToString()), explanation);

    private static CurriculumQuestion Order(int grade, int stage, int variant)
    {
        string[] cards = grade == 1 && stage <= 3 ? ["あか", "あお", "きいろ"] : ["あか", "あお", "きいろ", "みどり"];
        var ordered = cards.Skip(variant % cards.Length).Concat(cards.Take(variant % cards.Length)).ToArray();
        if (variant == 3) Array.Reverse(ordered);
        var incomplete = stage >= 4 && variant == 3;
        var clues = Enumerable.Range(0, ordered.Length - 1)
            .Where(index => !incomplete || index != ordered.Length - 2)
            .Select(index => $"{ordered[index]}は {ordered[index + 1]}より 左。").Reverse().ToArray();
        var position = incomplete ? ordered.Length : 1 + (stage + variant - 1) % ordered.Length;
        var answer = incomplete ? "きめられない" : ordered[position - 1];
        var alternate = ordered.Take(ordered.Length - 2).Concat(new[] { ordered[^1], ordered[^2] });
        var explanation = incomplete
            ? $"{ordered[^1]}の ばしょが まだ わかりません。左から {string.Join("・", ordered)}でも、{string.Join("・", alternate)}でも、すべての てがかりに 合います。{position}ばんめが かわるので、きめられません。"
            : $"てがかりを つなぐと、左から {string.Join(" → ", ordered)}。左から {position}ばんめは {answer}です。てがかりに 書かれた じゅんではなく、ならびを 作って たしかめよう。";
        return Choice(stage, "ならびの推理",
            $"{string.Join("・", cards)}の カードを 1まいずつ、横1列に ならべます。{string.Join("", clues)}左から {position}ばんめは？ てがかりだけでは きまらないときは「きめられない」を えらぼう。",
            answer, cards.Append("きめられない"), explanation);
    }

    private static CurriculumQuestion Assignment(int grade, int stage, int variant)
    {
        var count = grade == 1 && stage <= 3 ? 3 : 4;
        string[] allNames = ["うさぎ", "ねこ", "いぬ", "くま"];
        string[] allItems = ["りんご", "みかん", "ぶどう", "もも"];
        var names = allNames.Take(count).ToArray();
        var items = allItems.Take(count).ToArray();
        var assigned = items.Skip(variant % count).Concat(items.Take(variant % count)).ToArray();
        if (variant == 3) Array.Reverse(assigned);
        var clues = Enumerable.Range(1, count - 1)
            .Select(index => $"{names[index]}は {string.Join("・", assigned.Take(index))}を えらびません。").ToArray();
        var target = stage <= 2 ? count - 1 - variant % 2 : variant % (count - 1);
        var steps = Enumerable.Range(0, count).Reverse().Select(index => $"{names[index]}は {assigned[index]}");
        return Choice(stage, "わりあての推理",
            $"{string.Join("・", names)}が、{string.Join("・", items)}を 1こずつ えらびます。同じ くだものは えらべません。{string.Join("", clues)}{names[target]}が えらぶのは？",
            assigned[target], items.Append("きめられない"),
            $"えらべない ものが 多い人から しらべよう。{string.Join(" → ", steps)}の じゅんに きまります。すでに えらばれた ものを けすと、{names[target]}は {assigned[target]}です。");
    }

    private static CurriculumQuestion Reverse(int grade, int stage, int variant)
    {
        var start = 2 + variant + stage + (grade <= 2 ? 2 * (grade - 1) : grade);
        var count = grade <= 2 ? grade + (stage - 1) / 2 : Math.Min(5, 1 + (grade - 3 + stage) / 2);
        var value = start;
        var actions = new List<string>();
        var undo = new List<string>();
        for (var index = 0; index < count; index++)
        {
            var amount = 2 + (variant + index) % 3;
            var before = value;
            if (grade >= 3 && index == variant % count)
            {
                value *= amount;
                actions.Add($"{amount}倍する");
                undo.Insert(0, $"{value}を {amount}で わると {before}");
            }
            else if (index % 2 == 1)
            {
                value -= 2;
                actions.Add("2ひく");
                undo.Insert(0, $"{value}に 2を たすと {before}");
            }
            else
            {
                value += amount;
                actions.Add($"{amount}たす");
                undo.Insert(0, $"{value}から {amount}を ひくと {before}");
            }
        }
        return Number(stage, "うしろから考える",
            $"はじめの 数に「{string.Join(" → ", actions)}」を 左から じゅんに すると、{value}になりました。はじめの 数は？",
            start, $"さいごから もどそう。{string.Join("。つぎに、", undo)}。はじめは {start}です。もとの 手じゅんを {start}から やって、{value}になるか たしかめよう。");
    }

    private static CurriculumQuestion Route(int grade, int stage, int variant)
    {
        if (grade >= 4 && stage >= 3) return ProjectPlan(grade, stage, variant);
        if (grade <= 2)
        {
            var start = 2 + variant + stage + (grade - 1) * 3;
            var right = grade + stage % 2;
            var left = 1 + variant % 2;
            var destination = start + right - left;
            return Number(stage, "道すじをたどる",
                $"1から 20までの ますが 左から じゅんに ならんでいます。{start}の ますから、右へ {right}ます、左へ {left}ます うごきます。さいごは 何ばんの ます？ はじめの ますは 歩いた数に 入れません。",
                destination, $"{start}から 右へ {right}ますで {start + right}。そこから 左へ {left}ますで {destination}です。むきを かえるときも、今いる ますから かぞえよう。");
        }
        var a = 2 + variant;
        var b = grade + stage;
        var c = 1 + (variant + stage) % 4;
        var direct = a + b + (variant % 2 == 0 ? 2 : -1);
        var viaB = a + b;
        var last = Math.Max(1, b - stage);
        var viaC = a + c + last;
        var best = Math.Min(direct, Math.Min(viaB, viaC));
        return Number(stage, "道すじをたどる",
            $"家から 学校へ 行く道は 次の3つだけです。直通：{direct}分。公園を通る道：{a}分と {b}分。図書館を通る道：{a}分と {c}分と {last}分。いちばん 短い道は 合計何分？",
            best, $"直通は {direct}分。公園経由は {a}＋{b}＝{viaB}分。図書館経由は {a}＋{c}＋{last}＝{viaC}分。合計を くらべると {best}分が 最短です。区間の数だけで 決めず、道全体を くらべよう。");
    }

    private static CurriculumQuestion Combination(int grade, int stage, int variant)
    {
        if (grade >= 4) return Budget(grade, stage, variant);
        // Powers-of-two offsets make all six pair sums different.
        var offset = grade <= 2 ? variant + (grade - 1) * 2 : grade + stage + variant;
        var cards = new[] { 1, 2, 4, 8 }.Select(value => value + offset).ToArray();
        var pairs = (from left in Enumerable.Range(0, 4)
                     from right in Enumerable.Range(left + 1, 3 - left)
                     select (Left: cards[left], Right: cards[right])).ToArray();
        var chosen = pairs[(stage + variant) % pairs.Length];
        var target = chosen.Left + chosen.Right;
        string Label((int Left, int Right) pair) => $"{pair.Left}と{pair.Right}";
        return Choice(stage, "組合せをしぼる",
            $"{string.Join("・", cards)}の 数カードが 1まいずつ あります。ちがう 2まいを えらび、合計を {target}にします。どの 2まい？",
            Label(chosen), pairs.Select(Label),
            $"1まい目を きめて、{target}から その数を ひこう。{target}−{chosen.Left}＝{chosen.Right}で、どちらの カードも あります。{chosen.Left}＋{chosen.Right}＝{target}と たしかめられます。同じ カードを 2回つかわないことも たしかめよう。");
    }

    private static CurriculumQuestion Strategy(int grade, int stage, int variant)
    {
        if (grade <= 3 || stage <= 2)
        {
            var red = 2 + variant;
            var blue = grade + stage;
            var seekRed = variant % 2 == 0;
            var other = seekRed ? blue : red;
            var color = seekRed ? "あか" : "あお";
            return Number(stage, "かならずを考える",
                $"ふくろに あかい玉が {red}こ、あおい玉が {blue}こ あります。中を 見ずに 1こずつ 取り、もどしません。どんな じゅんでも {color}を 1こ以上 手に入れるには、少なくとも 何こ 取ればよい？",
                other + 1, $"ほしい色が さいごまで 出ない ばあいを 考えよう。ほかの色が {other}こ つづいても、その次は かならず {color}です。だから {other + 1}こ。{other}こだけだと、ぜんぶ ほかの色かもしれません。");
        }
        var maximum = 2 + (grade + stage + variant) % 3;
        var take = 1 + variant % maximum;
        var stones = (maximum + 1) * (1 + (stage - 3) + variant) + take;
        return Number(stage, "かならずを考える",
            $"石が {stones}こ あります。2人で 交代に、1回に 1〜{maximum}こ 取ります。さいごの 石を 取った人が 勝ち。あなたが 先です。相手が どう取っても 勝てるように、はじめに 何こ 取る？",
            take, $"はじめに {take}こ取り、{stones - take}こを 相手に のこします。その後は 相手と 自分の 取る数の 合計を {maximum + 1}こに そろえます。相手が xこなら 自分は {maximum + 1}−xこ。毎回1〜{maximum}この 範囲で 取れて、さいごを 自分が 取れます。");
    }

    private static CurriculumQuestion Budget(int grade, int stage, int variant)
    {
        var cheap = 30 + 10 * variant;
        var expensive = cheap + 20 + 10 * (grade - 4);
        var count = 4 + stage;
        var premium = 1 + (stage + variant) % (count - 1);
        var budget = cheap * (count - premium) + expensive * premium;
        var exact = stage <= 2;
        var constraint = exact ? $"代金が ちょうど {budget}円に なりました。" : $"予算は {budget + 5}円で、おつりが あっても かまいません。";
        var question = exact ? "ペンは 何本 買いましたか？" : "ペンは 多くても 何本 買えますか？";
        return Number(stage, "組合せをしぼる",
            $"えんぴつは 1本{cheap}円、ペンは 1本{expensive}円です。この2種類を 合わせて {count}本 買います。{constraint}{question}", premium,
            $"全部えんぴつなら {cheap}×{count}＝{cheap * count}円。1本を ペンに かえるたび {expensive - cheap}円ふえます。ペン{premium}本なら {budget}円、{premium + 1}本なら {budget + expensive - cheap}円です。{(exact ? "ちょうどの 金額になる" : "予算を こえずに 最も多く買える")}のは {premium}本です。");
    }

    private static CurriculumQuestion ProjectPlan(int grade, int stage, int variant)
    {
        var a = 3 + variant;
        var b = 2 + grade;
        var c = 1 + stage + variant;
        var d = 2 + variant % 2;
        var afterA = variant % 2 == 0;
        var answer = afterA ? Math.Max(a + c, b) + d : Math.Max(a, b) + c + d;
        var dependency = afterA ? "Cは Aが 終わったら 始められます。Dは BとCが 両方 終わったら 始められます。"
            : "Cは AとBが 両方 終わったら 始められます。Dは Cが 終わったら 始められます。";
        var explanation = afterA
            ? $"AからCまでは {a}＋{c}＝{a + c}分。Bは {b}分。Dは 遅いほうの {Math.Max(a + c, b)}分後に 始まり、さらに {d}分で 完了です。"
            : $"AとBは 同時に 始めます。両方終わるのは {Math.Max(a, b)}分後。そこから Cの{c}分と Dの{d}分が 必要です。";
        return Number(stage, "道すじをたどる",
            $"4人で 作業A・B・C・Dを 1人1つ 担当します。Aは {a}分、Bは {b}分、Cは {c}分、Dは {d}分。AとBは 同時に 始められます。{dependency}すべて 終わるまで 最短で何分？",
            answer, $"{explanation}答えは {answer}分。別々の人が 同時にできる 作業の時間は 足さず、待つ必要がある 作業を つないで 考えます。");
    }
}
