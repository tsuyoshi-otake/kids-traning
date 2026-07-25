namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildKanjiCurriculumScript()
    {
        return """
  kanjiCurriculumEntries(){
    // Inflected kun readings only make sense with their okurigana: the bare 休 is not
    // read やすむ, 休む is. Each token is one kanji followed by its okurigana tail, and
    // the tail must be the end of the stored reading so word=kanji+okuri stays readable.
    // Derived nouns and na-adjectives belong here too: 同じ, 幸せ and 平ら are no more
    // readable as a bare 同 / 幸 / 平 than 休 is readable as やすむ.
    const okuriText='休む|見る|出る|小さい|正しい|生きる|早い|大きい|入る|立つ|引く|遠い|会う|楽しい|帰る|強い|教える|近い|計る|言う|古い|広い|交わる|考える|行く|高い|合う|細い|作る|止まる|思う|弱い|書く|少ない|食べる|新しい|晴れる|切る|走る|多い|太い|知る|長い|直す|通る|当たる|答える|読む|売る|買う|分ける|聞く|歩く|明るい|鳴く|来る|話す|悪い|安い|暗い|育てる|飲む|運ぶ|泳ぐ|温かい|寒い|感じる|起きる|急ぐ|去る|苦しい|軽い|決める|向く|使う|始める|持つ|写す|守る|取る|受ける|拾う|終わる|習う|集める|住む|重い|暑い|助ける|消す|勝つ|乗る|植える|申す|深い|進む|整える|送る|速い|打つ|待つ|代わる|短い|着る|注ぐ|調べる|追う|定める|転がる|投げる|登る|等しい|動く|配る|悲しい|美しい|負ける|返す|放す|問う|有る|遊ぶ|落ちる|流れる|開く|同じ|幸せ|平ら';
    const okuri=new Map(okuriText.split('|').map(word=>[Array.from(word)[0],Array.from(word).slice(1).join('')]));
    const make=(g,chars,readingText)=>{const kanji=Array.from(chars),readings=readingText.split('|');if(kanji.length!==readings.length)throw new Error('Kanji curriculum reading count mismatch for grade '+g);if(new Set(kanji).size!==kanji.length)throw new Error('Duplicate kanji in curriculum grade '+g);return kanji.map((k,index)=>{const r=readings[index],tail=okuri.get(k)||'';if(tail&&!r.endsWith(tail))throw new Error('Okurigana does not match the reading for '+k);return{g:g,k:k,r:r,okuri:tail,stem:tail?r.slice(0,r.length-tail.length):r,word:k+tail,pre:'',post:'',mean:g+'年生で 習う 漢字'};});};
    const kanjiGrade1='一右雨円王音下火花貝学気九休玉金空月犬見五口校左三山子四糸字耳七車手十出女小上森人水正生青夕石赤千川先早草足村大男竹中虫町天田土二日入年白八百文木本名目立力林六';
    const readingGrade1='いち|みぎ|あめ|えん|おう|おと|した|ひ|はな|かい|がく|き|きゅう|やすむ|たま|かね|そら|つき|いぬ|みる|ご|くち|こう|ひだり|さん|やま|こ|よん|いと|じ|みみ|なな|くるま|て|じゅう|でる|おんな|ちいさい|うえ|もり|ひと|みず|ただしい|いきる|あお|ゆう|いし|あか|せん|かわ|さき|はやい|くさ|あし|むら|おおきい|おとこ|たけ|なか|むし|まち|てん|た|つち|に|ひ|はいる|とし|しろ|はち|ひゃく|ぶん|き|ほん|な|め|たつ|ちから|はやし|ろく';
    const kanjiGrade2='引羽雲園遠何科夏家歌画回会海絵外角楽活間丸岩顔汽記帰弓牛魚京強教近兄形計元言原戸古午後語工公広交光考行高黄合谷国黒今才細作算止市矢姉思紙寺自時室社弱首秋週春書少場色食心新親図数西声星晴切雪船線前組走多太体台地池知茶昼長鳥朝直通弟店点電刀冬当東答頭同道読内南肉馬売買麦半番父風分聞米歩母方北毎妹万明鳴毛門夜野友用曜来里理話';
    const readingGrade2='ひく|はね|くも|えん|とおい|なに|か|なつ|いえ|うた|が|かい|あう|うみ|え|そと|かど|たのしい|かつ|あいだ|まる|いわ|かお|き|き|かえる|ゆみ|うし|さかな|きょう|つよい|おしえる|ちかい|あに|かたち|はかる|もと|いう|はら|と|ふるい|ご|あと|ご|こう|こう|ひろい|まじわる|ひかり|かんがえる|いく|たかい|き|あう|たに|くに|くろ|いま|さい|ほそい|つくる|さん|とまる|し|や|あね|おもう|かみ|てら|じ|とき|しつ|しゃ|よわい|くび|あき|しゅう|はる|かく|すくない|ば|いろ|たべる|こころ|あたらしい|おや|ず|かず|にし|こえ|ほし|はれる|きる|ゆき|ふね|せん|まえ|くみ|はしる|おおい|ふとい|からだ|だい|ち|いけ|しる|ちゃ|ひる|ながい|とり|あさ|なおす|とおる|おとうと|みせ|てん|でん|かたな|ふゆ|あたる|ひがし|こたえる|あたま|おなじ|みち|よむ|うち|みなみ|にく|うま|うる|かう|むぎ|はん|ばん|ちち|かぜ|わける|きく|こめ|あるく|はは|かた|きた|まい|いもうと|まん|あかるい|なく|け|もん|よる|の|とも|よう|よう|くる|さと|り|はなす';
    const kanjiGrade3='悪安暗医委意育員院飲運泳駅央横屋温化荷界階寒感漢館岸起期客究急級宮球去橋業曲局銀区苦具君係軽血決研県庫湖向幸港号根祭皿仕死使始指歯詩次事持式実写者主守取酒受州拾終習集住重宿所暑助昭消商章勝乗植申身神真深進世整昔全相送想息速族他打対待代第題炭短談着注柱丁帳調追定庭笛鉄転都度投豆島湯登等動童農波配倍箱畑発反坂板皮悲美鼻筆氷表秒病品負部服福物平返勉放味命面問役薬由油有遊予羊洋葉陽様落流旅両緑礼列練路和開';
    const readingGrade3='わるい|やすい|くらい|い|い|い|そだてる|いん|いん|のむ|はこぶ|およぐ|えき|おう|よこ|や|あたたかい|か|に|かい|かい|さむい|かんじる|かん|かん|きし|おきる|き|きゃく|きゅう|いそぐ|きゅう|みや|きゅう|さる|はし|ぎょう|きょく|きょく|ぎん|く|くるしい|ぐ|きみ|かかり|かるい|ち|きめる|けん|けん|こ|みずうみ|むく|しあわせ|みなと|ごう|ね|まつり|さら|し|しぬ|つかう|はじめる|ゆび|は|し|つぎ|こと|もつ|しき|み|うつす|もの|ぬし|まもる|とる|さけ|うける|しゅう|ひろう|おわる|ならう|あつめる|すむ|おもい|やど|ところ|あつい|たすける|しょう|けす|しょう|しょう|かつ|のる|うえる|もうす|み|かみ|ま|ふかい|すすむ|よ|ととのえる|むかし|ぜん|あい|おくる|そう|いき|はやい|ぞく|ほか|うつ|たい|まつ|かわる|だい|だい|すみ|みじかい|だん|きる|そそぐ|はしら|ちょう|ちょう|しらべる|おう|さだめる|にわ|ふえ|てつ|ころがる|みやこ|ど|なげる|まめ|しま|ゆ|のぼる|ひとしい|うごく|どう|のう|なみ|くばる|ばい|はこ|はたけ|はつ|はん|さか|いた|かわ|かなしい|うつくしい|はな|ふで|こおり|おもて|びょう|やまい|しな|まける|ぶ|ふく|ふく|もの|たいら|かえす|べん|はなす|あじ|いのち|おもて|とう|やく|くすり|ゆう|あぶら|ある|あそぶ|よ|ひつじ|よう|は|よう|さま|おちる|ながれる|たび|りょう|みどり|れい|れつ|れん|ろ|わ|ひらく';
    const all=[...make(1,kanjiGrade1,readingGrade1),...make(2,kanjiGrade2,readingGrade2),...make(3,kanjiGrade3,readingGrade3)];if(all.length!==440||new Set(all.map(entry=>entry.k)).size!==440)throw new Error('Kanji curriculum allocation mismatch');const covered=new Set(all.map(entry=>entry.k));for(const k of okuri.keys())if(!covered.has(k))throw new Error('Okurigana entry is not in the curriculum: '+k);return all;
  }
""";
    }
}
