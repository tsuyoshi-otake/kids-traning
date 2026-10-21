namespace KidsTraining.App.Domain.Learning;

internal static partial class CurriculumPolicy
{
    private static partial void AddUpperEnglish(Action<string, string, int, string, string, string, CurriculumQuestion[]> add)
    {
        add("english", "eigo", 4, "listen-speak", "4年 外国語活動（聞く・話す）", EnglishSource,
        [
            Q(1, "朝のあいさつは？", "Good morning.", ["Good night.", "Thank you.", "See you."], "朝には Good morning. とあいさつする。", "家の人と朝・昼・夜のあいさつを交代で言ってみよう。"),
            Q(3, "好きなものをたずねる表現は？", "What do you like?", ["How old are you?", "Where is it?", "What time is it?"], "What do you like? で好みをたずねられる。", "相手の答えを最後まで聞き、好きなものを一つたずね合おう。"),
            Q(5, "『私はサッカーが好きです』に合う表現は？", "I like soccer.", ["I am soccer.", "I can soccer.", "I have soccer?"], "I like ... で好きなものを伝える。", "絵カードを見せながら I like ... を使って発表しよう。")
        ]);
        add("english", "eigo", 5, "five-domains-foundation", "5年 外国語（五領域の基礎）", EnglishSource,
        [
            Q(1, "アルファベット順で G の次は？", "H", ["F", "I", "J"], "G の次は H。"),
            Q(2, "can / swim / I を正しい順に並べると？", "I can swim.", ["Can I swim.", "Swim can I.", "I swim can."], "主語 I、助動詞 can、動詞 swim の順。"),
            Q(3, "誕生日をたずねる表現は？", "When is your birthday?", ["What is your name?", "Where do you live?", "How is the weather?"], "When で時をたずねる。"),
            Q(4, "『私は音楽を勉強します』に合う文は？", "I study music.", ["I music study?", "Study I music.", "I studies music."], "I の後は動詞の原形 study。"),
            Q(5, "短い自己紹介に入れるとよい組合せは？", "名前・好きなもの・できること", ["名前だけを繰り返す", "相手の個人情報", "意味のない文字列"], "目的に合わせて基本表現を組み合わせる。", "自分で公開してよい内容だけを選び、三文の自己紹介を練習しよう。")
        ]);
        add("english", "eigo", 6, "five-domains-integration", "6年 外国語（五領域の活用）", EnglishSource,
        [
            Q(1, "『私は昨日野球をしました』に合う文は？", "I played baseball yesterday.", ["I play baseball tomorrow.", "I playing baseball yesterday.", "Yesterday baseball I."], "過去のことは played、時を表す語は yesterday。"),
            Q(2, "Where do you want to go? への答えは？", "I want to go to Kyoto.", ["I am Kyoto.", "Kyoto wants me.", "I going where."], "I want to go to ... で行きたい場所を表す。"),
            Q(3, "紹介文を読むとき最初に探すとよい情報は？", "だれ・何についての文か", ["文字数だけ", "ピリオドの数だけ", "書体だけ"], "題材と要点をつかんでから詳しい情報を読む。"),
            Q(4, "相手の発表を聞いた後の自然な応答は？", "That sounds great.", ["I don't listen.", "No name.", "Goodbye yesterday."], "内容に応じた反応を返すとやり取りが続く。", "発音の点数ではなく、内容が伝わったかを相手と確かめよう。"),
            Q(5, "英語で作文した後に確かめるものは？", "語順・大文字・単語間の空白・終止符", ["文字の色だけ", "行数だけ", "紙の大きさだけ"], "読み手に伝わる英文の基本を確認する。")
        ]);
    }

    private static partial void AddUpperActivities(Action<string, string, int, string, string, string, CurriculumQuestion[]> add)
    {
        for (var grade = 4; grade <= 6; grade++)
        {
            var band = grade == 4 ? "3・4年" : "5・6年";
            add("integrated", "sougou", grade, "inquiry-cycle", $"{grade}年 総合（課題設定・探究）", GeneralSource,
            [
                Q(1, "探究の最初に行うことは？", "知りたいことを問いにする", ["結論を先に決める", "資料を写すだけ", "出典を消す"], "具体的な問いが情報収集と整理の方向を決める。"),
                Q(3, "集めた情報を比較するときそろえるものは？", "観点・単位・時点", ["文字色", "紙の種類", "発表順だけ"], "同じ観点で比べると共通点や違いが分かる。"),
                Q(5, "探究をまとめた後に行うことは？", "結論と方法を振り返り次の問いを考える", ["記録を全部消す", "出典を隠す", "一度も共有しない"], "探究の過程を評価すると学びを次へつなげられる。", "身近な課題について問い・資料・分かったこと・次の問いを一枚にまとめよう。")
            ]);
        }
    }
}
