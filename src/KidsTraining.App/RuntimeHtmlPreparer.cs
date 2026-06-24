using System.Text;
using System.Text.Json;

namespace KidsTraining.App;

internal static class RuntimeHtmlPreparer
{
    public const string EmergencyPin = "1234";
    private const string TemplateOpenTag = "<script type=\"__bundler/template\">";
    private const string TemplateCloseTag = "</script>";
    private const string BeginnerMasteryMarkup = "mastery:{add:.05,sub:.05,mul:.05,clock:.05,kokugo:.05,hissan:.05}";

    public static string PrimaryProfileName
    {
        get
        {
            var userName = Environment.UserName;
            return string.IsNullOrWhiteSpace(userName) ? "User" : userName.Trim();
        }
    }

    public static string Prepare()
    {
        if (!File.Exists(AppPaths.HtmlPath))
        {
            throw new FileNotFoundException("Learning HTML was not found.", AppPaths.HtmlPath);
        }

        var html = File.ReadAllText(AppPaths.HtmlPath, Encoding.UTF8);
        html = PatchBundledTemplate(html, PrimaryProfileName);

        File.WriteAllText(AppPaths.RuntimeHtmlPath, html, new UTF8Encoding(false));
        File.SetLastWriteTimeUtc(AppPaths.RuntimeHtmlPath, DateTime.UtcNow);
        return AppPaths.RuntimeHtmlPath;
    }

    public static string? ExtractBundledTemplate(string html)
    {
        if (!TryFindBundledTemplate(html, out var contentStart, out var contentEnd))
        {
            return null;
        }

        var encodedTemplate = html[contentStart..contentEnd].Trim();
        if (string.IsNullOrWhiteSpace(encodedTemplate))
        {
            return null;
        }

        return JsonSerializer.Deserialize<string>(encodedTemplate);
    }

    private static string PatchBundledTemplate(string html, string profileName)
    {
        if (!TryFindBundledTemplate(html, out var contentStart, out var contentEnd))
        {
            return PatchLearningMarkup(html, profileName);
        }

        var template = ExtractBundledTemplate(html)
            ?? throw new InvalidOperationException("Bundled learning template could not be decoded.");
        var patchedTemplate = PatchLearningMarkup(template, profileName);
        var encodedTemplate = JsonSerializer.Serialize(patchedTemplate);

        return html[..contentStart] + Environment.NewLine + encodedTemplate + Environment.NewLine + html[contentEnd..];
    }

    private static bool TryFindBundledTemplate(string html, out int contentStart, out int contentEnd)
    {
        contentStart = -1;
        contentEnd = -1;

        var openStart = html.IndexOf(TemplateOpenTag, StringComparison.Ordinal);
        if (openStart < 0)
        {
            return false;
        }

        contentStart = openStart + TemplateOpenTag.Length;
        contentEnd = html.IndexOf(TemplateCloseTag, contentStart, StringComparison.Ordinal);
        return contentEnd >= 0;
    }

    private static string PatchLearningMarkup(string markup, string profileName)
    {
        markup = markup.Replace("screen:'profile', profileIdx:0,", "screen:'start', profileIdx:0,", StringComparison.Ordinal);
        markup = markup.Replace(
            "unlockPC(){this.sfx('unlock');this.setState({screen:'profile',session:null,combo:0,pin:'',emergencyDone:false});}",
            "unlockPC(){this.sfx('unlock');this.setState({screen:'start',session:null,combo:0,pin:'',emergencyDone:false});}",
            StringComparison.Ordinal);
        markup = PatchBeginnerProgression(markup);

        return ReplaceBundledProfiles(markup, profileName);
    }

    private static string PatchBeginnerProgression(string markup)
    {
        markup = markup.Replace(
            "mastery:{add:.5,sub:.5,mul:.5,clock:.5,kokugo:.5,hissan:.5}",
            BeginnerMasteryMarkup,
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "genAdd(){",
            "\n  genSub(){",
            BuildGenAddScript());

        markup = ReplaceBlock(
            markup,
            "genSub(){",
            "\n  genHissan(){",
            BuildGenSubScript());

        markup = ReplaceBlock(
            markup,
            "pickMul(){",
            "\n  pick4(",
            BuildPickMulScript());

        markup = ReplaceBlock(
            markup,
            "clockExplain(h,m,ask,a){",
            "\n  pickKokugo(){",
            BuildPickClockScript());

        markup = ReplaceBlock(
            markup,
            "pickKokugo(){",
            "\n  genFor(k){",
            BuildPickKokugoScript());

        markup = markup.Replace(
            "genFor(k){return k==='add'?this.genAdd():k==='sub'?this.genSub():k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            "genFor(k,p){return k==='add'?this.genAdd(p):k==='sub'?this.genSub(p):k==='hissan'?this.genHissan():k==='mul'?this.pickMul(p):k==='clock'?this.pickClock(p):this.pickKokugo(p);}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "weightedPick(p){",
            "\n  total(){",
            BuildProgressionScript());

        markup = markup.Replace(
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p)));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p),p));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "lvl(p){",
            "\n\n  selectProfile",
            "skillLevel(p){const values=Object.values(p.mastery||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const avg=this.skillAverage(p),top=values.length?Math.max(...values):0.05,stars=Math.min(Number(p.stars)||0,180);const score=Math.min(1,avg*0.45+top*0.35+stars/320);return Math.max(1,Math.min(5,Math.floor(score*5)));}\n  lvl(p){return 'レベル '+this.skillLevel(p);}");

        markup = markup.Replace(
            "const weakKeys=Object.keys(T).filter(k=>p.mastery[k]<0.5);",
            "const weakKeys=this.allowedTopics(p).filter(k=>p.mastery[k]<0.5);",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "gradeLabel:pr.grade+'年生'",
            "gradeLabel:this.gradeLabel(pr)",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "profileGrade:p.grade+'年生'",
            "profileGrade:this.gradeLabel(p)",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "        <div style=\"display:flex; align-items:center; gap:14px;\">\n          <div style=\"{{ avatarStyle }}\">{{ profileInitial }}</div>\n          <div>",
            "        <div style=\"display:flex; align-items:center;\">\n          <div>",
            StringComparison.Ordinal);

        markup = PatchArithmeticVisuals(markup);

        return markup;
    }

    private static string BuildGenAddScript()
    {
        return """
genAdd(p){const g=this.effectiveGrade(p),stage=this.learningStage(p),m=p&&p.mastery?Number(p.mastery.add):0.05;let a,b;if(g<=1){if(stage<=1||m<0.30){a=this.rand(1,5);b=this.rand(1,5);if(a+b>10)b=Math.max(1,10-a);}else if(stage<=2||m<0.65){a=this.rand(2,9);b=this.rand(1,9);if(a+b>18)b=Math.max(1,18-a);}else{a=this.rand(10,29);b=this.rand(1,9);}}else if(g===2){if(stage<=2||m<0.45){a=this.rand(10,49);b=this.rand(1,40);if(a%10+b%10>=10)b=Math.max(1,b-(a%10+b%10-9));}else{a=this.rand(18,79);b=this.rand(12,79);if(a+b>99)b=99-a;}}else{if(stage<=3||m<0.55){a=this.rand(25,79);b=this.rand(11,79);if(a+b>99)b=99-a;}else{a=this.rand(35,89);b=this.rand(10,99-a);}}const ans=a+b;return{topic:'add',mode:'num',n1:a,n2:b,prompt:a+' + '+b,answer:''+ans,explanation:a+' + '+b+' = '+ans};}
""";
    }

    private static string BuildGenSubScript()
    {
        return """
genSub(p){const g=this.effectiveGrade(p),stage=this.learningStage(p),m=p&&p.mastery?Number(p.mastery.sub):0.05;let a,b;if(g<=1){if(stage<=1||m<0.30){a=this.rand(2,10);b=this.rand(1,a-1);}else if(stage<=2||m<0.65){a=this.rand(11,18);b=this.rand(1,Math.max(1,a%10));}else{a=this.rand(11,29);b=this.rand(1,9);}}else if(g===2){if(stage<=2||m<0.45){a=this.rand(20,89);b=this.rand(1,Math.min(40,a-1));if(a%10<b%10)b=Math.max(1,b-(b%10)+(a%10));}else{a=this.rand(30,99);b=this.rand(11,a-1);}}else{if(stage<=3||m<0.55){a=this.rand(35,99);b=this.rand(11,a-1);}else{a=this.rand(50,99);b=this.rand(20,a-1);}}const ans=a-b;return{topic:'sub',mode:'num',a:a,b:b,prompt:a+' - '+b,answer:''+ans,explanation:a+' - '+b+' = '+ans};}
""";
    }

    private static string BuildPickMulScript()
    {
        return """
pickMul(p){const g=this.effectiveGrade(p),stage=this.learningStage(p),m=p&&p.mastery?Number(p.mastery.mul):0.05;if(g>=3&&stage>=3&&Math.random()<0.45){const d=this.rand(2,9),q=this.rand(2,stage>=4?12:9),n=d*q,ans=''+q;return{topic:'mul',mode:'choices',op:'div',a:d,b:q,prompt:n+' ÷ '+d,answer:ans,choices:this.pick4(ans,[q+1,q-1,q+2,Math.max(1,q-2),d].map(String)),explanation:n+' ÷ '+d+' = '+q+'。'+d+' の まとまりが '+q+' こ。'};}const tables=g<=1?[2,3,4,5]:(stage<=2||m<0.35?[2,5,3,4]:[2,3,4,5,6,7,8,9]);const a=tables[this.rand(0,tables.length-1)],b=this.rand(2,g>=3&&stage>=4?12:9),ans=a*b;return{topic:'mul',mode:'choices',op:'mul',a:a,b:b,prompt:a+' x '+b,answer:''+ans,choices:this.pick4(''+ans,[ans+a,ans-a,ans+b,ans-b,a+b,Math.max(1,ans+a+b)].map(String)),explanation:a+' x '+b+' = '+ans+'。'+a+' こずつが '+b+' つ。'};}
""";
    }

    private static string BuildPickClockScript()
    {
        return """
clockExplain(h,m,ask,a){if(ask==='hour')return 'みじかい はり が '+h+' を さして いるね。こたえは '+a+'。';if(ask==='minute')return 'ながい はり が さす すうじ ×5 が ふん。'+(m/5)+'×5='+m+'ふん。こたえは '+a+'。';return 'みじかい はり＝じ、ながい はり＝ふん。こたえは '+a+'。';}
  pickMeasure(p){const g=this.effectiveGrade(p),stage=this.learningStage(p);const L=g>=3&&stage>=3?[
    ['1kg は 何g？','1000g',['100g','10g','10000g'],'1kg = 1000g。'],
    ['1分 は 何秒？','60秒',['30秒','100秒','10秒'],'1分 = 60秒。'],
    ['1km は 何m？','1000m',['100m','10m','10000m'],'1km = 1000m。'],
    ['3時10分 の 20分後は？','3時30分',['3時20分','4時10分','2時50分'],'10分に20分を足すと30分。']
  ]:[
    ['1m は 何cm？','100cm',['10cm','1000cm','1cm'],'1m = 100cm。'],
    ['1L は 何dL？','10dL',['100dL','1dL','5dL'],'1L = 10dL。'],
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['10cm と 30cm を あわせると？','40cm',['20cm','30cm','50cm'],'10cm + 30cm = 40cm。']
  ];const it=L[this.rand(0,L.length-1)];return{topic:'clock',mode:'choices',prompt:it[0],answer:it[1],choices:this.pick4(it[1],it[2]),explanation:it[3]};}
  pickClock(p){const g=this.effectiveGrade(p),stage=this.learningStage(p);if(g>=2&&stage>=3&&Math.random()<0.35)return this.pickMeasure(p);const hourStr=x=>((x-1+12)%12+1)+'じ';const kinds=stage<=2?['hour','hour','half']:(g>=2?['hour','half','minute','both']:['hour','hour','half','minute']);const k=kinds[this.rand(0,kinds.length-1)];let h=this.rand(1,12),m=0,ask='hour',prompt='なんじ？',a='',pool=[];
    if(k==='hour'){m=0;ask='hour';prompt='とけいを よもう ・ なんじ？';a=h+'じ';pool=[hourStr(h+1),hourStr(h-1),hourStr(h+2),hourStr(h+3)];}
    else if(k==='half'){m=30;ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ30ぷん';pool=[hourStr(h+1).replace('じ','じ30ぷん'),h+'じ',hourStr(h-1).replace('じ','じ30ぷん'),hourStr(h+2).replace('じ','じ30ぷん')];}
    else if(k==='minute'){const mins=[5,10,15,20,25,35,40,45,50,55];m=mins[this.rand(0,mins.length-1)];ask='minute';prompt='ながい はりを よもう ・ なんぷん？';a=m+'ふん';pool=[5,10,15,20,25,30,35,40,45,50,55].filter(x=>x!==m).map(x=>x+'ふん');}
    else{const mins=[10,15,20,40,45,50];m=mins[this.rand(0,mins.length-1)];ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ'+m+'ふん';pool=[hourStr(h+1).replace('じ','じ'+m+'ふん'),h+'じ'+(m===15?45:15)+'ふん',hourStr(h-1).replace('じ','じ'+m+'ふん'),h+'じ'];}
    return{topic:'clock',mode:'choices',isClock:true,h:h,m:m,ask:ask,prompt:prompt,answer:a,choices:this.pick4(a,pool),explanation:this.clockExplain(h,m,ask,a)};}
""";
    }

    private static string BuildPickKokugoScript()
    {
        return """
pickKokugo(p){const g=this.effectiveGrade(p),stage=this.learningStage(p);const L=[
  {g:1,k:'山',r:'やま',pre:'',post:' に のぼる',mean:'たかい ところ'},{g:1,k:'川',r:'かわ',pre:'',post:' で あそぶ',mean:'みずが ながれる ところ'},{g:1,k:'花',r:'はな',pre:'あかい ',post:' が さく',mean:'くさきに さく もの'},{g:1,k:'空',r:'そら',pre:'あおい ',post:' を みる',mean:'あたまの うえ'},{g:1,k:'学校',r:'がっこう',pre:'まいにち ',post:' へ いく',mean:'べんきょうする ところ'},{g:1,k:'先生',r:'せんせい',pre:'やさしい ',post:'',mean:'おしえて くれる ひと'},{g:1,k:'雨',r:'あめ',pre:'',post:' が ふる',mean:'そらから おちる みず'},{g:1,k:'水',r:'みず',pre:'',post:' を のむ',mean:'のむ もの'},{g:1,k:'木',r:'き',pre:'大きな ',post:'',mean:'えだや はが ある'},{g:1,k:'犬',r:'いぬ',pre:'',post:' と あるく',mean:'どうぶつ'},{g:1,k:'耳',r:'みみ',pre:'',post:' で きく',mean:'おとを きく ところ'},{g:1,k:'手',r:'て',pre:'',post:' を あげる',mean:'ものを もつ ところ'},{g:1,k:'足',r:'あし',pre:'',post:' で はしる',mean:'あるく ところ'},{g:1,k:'町',r:'まち',pre:'',post:' を あるく',mean:'いえや みせが ある'},{g:1,k:'森',r:'もり',pre:'',post:' に 木が ある',mean:'木が たくさん ある'},{g:1,k:'名まえ',r:'なまえ',pre:'',post:' を 書く',mean:'よびかた'},
  {g:2,k:'春',r:'はる',pre:'',post:' に 花が さく',mean:'あたたかい きせつ'},{g:2,k:'夏',r:'なつ',pre:'',post:' は あつい',mean:'あつい きせつ'},{g:2,k:'秋',r:'あき',pre:'',post:' に 木のはが かわる',mean:'すずしい きせつ'},{g:2,k:'冬',r:'ふゆ',pre:'',post:' は さむい',mean:'さむい きせつ'},{g:2,k:'朝',r:'あさ',pre:'',post:' に おきる',mean:'一日の はじまり'},{g:2,k:'昼',r:'ひる',pre:'',post:' に ごはんを 食べる',mean:'日中'},{g:2,k:'夜',r:'よる',pre:'',post:' に ねる',mean:'くらい じかん'},{g:2,k:'魚',r:'さかな',pre:'',post:' が およぐ',mean:'みずの 中の いきもの'},{g:2,k:'鳥',r:'とり',pre:'',post:' が とぶ',mean:'はねの ある いきもの'},{g:2,k:'馬',r:'うま',pre:'',post:' が はしる',mean:'どうぶつ'},{g:2,k:'歩く',r:'あるく',pre:'みちを ',post:'',mean:'足で すすむ'},{g:2,k:'走る',r:'はしる',pre:'校ていを ',post:'',mean:'はやく すすむ'},{g:2,k:'近い',r:'ちかい',pre:'家が ',post:'',mean:'きょりが みじかい'},{g:2,k:'遠い',r:'とおい',pre:'駅が ',post:'',mean:'きょりが ながい'},{g:2,k:'高い',r:'たかい',pre:'',post:' 山',mean:'上まで 大きい'},{g:2,k:'新しい',r:'あたらしい',pre:'',post:' 本',mean:'できた ばかり'},{g:2,k:'読む',r:'よむ',pre:'本を ',post:'',mean:'文字を こえに する'},{g:2,k:'書く',r:'かく',pre:'字を ',post:'',mean:'文字を しるす'},{g:2,k:'聞く',r:'きく',pre:'話を ',post:'',mean:'耳で うける'},{g:2,k:'考える',r:'かんがえる',pre:'こたえを ',post:'',mean:'あたまで くらべる'},
  {g:3,k:'漢字',r:'かんじ',pre:'',post:' を おぼえる',mean:'日本語の 文字'},{g:3,k:'病院',r:'びょういん',pre:'',post:' へ 行く',mean:'びょうきを みる ところ'},{g:3,k:'薬',r:'くすり',pre:'',post:' を のむ',mean:'からだを よくする もの'},{g:3,k:'医者',r:'いしゃ',pre:'',post:' に みてもらう',mean:'びょうきを みる ひと'},{g:3,k:'神社',r:'じんじゃ',pre:'',post:' へ 行く',mean:'おまいりする ところ'},{g:3,k:'研究',r:'けんきゅう',pre:'虫を ',post:' する',mean:'くわしく しらべる'},{g:3,k:'宿題',r:'しゅくだい',pre:'',post:' を する',mean:'家でする べんきょう'},{g:3,k:'運動',r:'うんどう',pre:'',post:' を する',mean:'からだを うごかす'},{g:3,k:'始める',r:'はじめる',pre:'会を ',post:'',mean:'スタートする'},{g:3,k:'終わる',r:'おわる',pre:'じゅぎょうが ',post:'',mean:'おしまいに なる'},{g:3,k:'急ぐ',r:'いそぐ',pre:'駅へ ',post:'',mean:'はやく する'},{g:3,k:'泳ぐ',r:'およぐ',pre:'プールで ',post:'',mean:'水の 中を すすむ'},{g:3,k:'橋',r:'はし',pre:'',post:' を わたる',mean:'川などを こえる もの'},{g:3,k:'湖',r:'みずうみ',pre:'',post:' を 見る',mean:'大きな 水たまり'},{g:3,k:'祭り',r:'まつり',pre:'',post:' に 行く',mean:'みんなで たのしむ 行事'},{g:3,k:'緑',r:'みどり',pre:'',post:' の 葉',mean:'草や 葉の 色'},{g:3,k:'短い',r:'みじかい',pre:'',post:' えんぴつ',mean:'ながくない'},{g:3,k:'深い',r:'ふかい',pre:'',post:' 池',mean:'そこまで とおい'},{g:3,k:'世界',r:'せかい',pre:'',post:' を 知る',mean:'たくさんの 国や 人'},{g:3,k:'写真',r:'しゃしん',pre:'',post:' を とる',mean:'カメラで うつした もの'}
];const pool=L.filter(x=>x.g<=g);const early=stage<=2?pool.filter(x=>x.g===1):pool;const use=early.length?early:pool;const it=use[this.rand(0,use.length-1)];const choose=stage>=2&&Math.random()<0.5;const others=this.shuffle(use.filter(x=>x.k!==it.k));if(choose){const choices=this.shuffle([it.k].concat(others.slice(0,3).map(x=>x.k)));return{topic:'kokugo',mode:'choices',isKokugo:true,subtype:'kanji-choice',pre:it.pre,word:it.r,post:it.post,mean:'あう かんじを えらぼう',prompt:it.pre+it.r+it.post,answer:it.k,choices:choices,explanation:'「'+it.r+'」は 「'+it.k+'」と 書くよ。いみ：'+it.mean};}const choices=this.shuffle([it.r].concat(others.filter(x=>x.r!==it.r).slice(0,3).map(x=>x.r)));return{topic:'kokugo',mode:'choices',isKokugo:true,subtype:'reading',pre:it.pre,word:it.k,post:it.post,mean:it.mean,prompt:it.pre+it.k+it.post,answer:it.r,choices:choices,explanation:'「'+it.k+'」は 「'+it.r+'」と よむよ。いみ：'+it.mean};}
""";
    }

    private static string BuildProgressionScript()
    {
        return """
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  effectiveGrade(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1));const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const top=values.length?Math.max(...values):0.05,stars=Number(p&&p.stars)||0;const byProgress=stars>=150&&top>=0.65?3:(stars>=55&&top>=0.45?2:1);return Math.max(base,byProgress);}
  gradeLabel(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1)),g=this.effectiveGrade(p);return g+'年生'+(g>base?' 範囲':'');}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;return 4;}
  allowedTopics(p){const all=Object.keys(this.topics);const cfg=this.state.settings;const en=(cfg&&cfg.topics)?all.filter(k=>cfg.topics[k]):all;const enabled=en.length?en:all;const stage=this.learningStage(p),grade=this.effectiveGrade(p);let staged;if(stage<=1)staged=['add'];else if(stage===2)staged=['add','sub'];else if(grade<=1)staged=['add','sub','clock','kokugo'];else if(grade===2)staged=stage===3?['add','sub','clock','kokugo','mul']:['add','sub','clock','kokugo','mul','hissan'];else staged=stage===3?['add','sub','clock','kokugo','mul']:all;const allowed=enabled.filter(k=>staged.includes(k));return allowed.length?allowed:enabled;}
  weightedPick(p){const ks=this.allowedTopics(p),stage=this.learningStage(p);const w=ks.map(k=>{let base=0.25+(1-(Number(p.mastery[k])||0.05))*1.7;if(k==='hissan'&&stage<4)base*=0.45;return base;});let s=w.reduce((a,b)=>a+b,0),r=Math.random()*s;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}
""";
    }

    private static string PatchArithmeticVisuals(string markup)
    {
        markup = markup.Replace(
            "let isAddViz=false,addFrames=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));",
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));\n      if(modeChoices&&q.topic==='mul'){isMulViz=true;const a=Number(q.a)||0,b=Number(q.b)||0;for(let g=0;g<b;g++){const cells=[];for(let i=0;i<a;i++)cells.push({style:'width:16px;height:16px;border-radius:50%;background:#1fa39a;border:2px solid #178a82;'});mulGroups.push({cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+Math.min(a,5)+',16px);gap:4px;padding:8px;border-radius:12px;border:3px solid #b8e8e2;background:#e6fbf7;'});}}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "if(q.topic==='add'){isAddViz=true;",
            "\n        if(S.numChoices)",
            "isAddViz=q.topic==='add'||q.topic==='sub';if(isAddViz){const isSub=q.topic==='sub',base=isSub?Number(q.a||0):Number(q.n1||0),delta=isSub?Number(q.b||0):Number(q.n2||0),total=isSub?base:base+delta,frames=Math.max(1,Math.ceil(Math.max(base,total)/10));for(let f=0;f<frames;f++){const cells=[];let fill=0;for(let i=0;i<10;i++){const idx=f*10+i;let st='background:#fff;border:2px dashed #d8c4a0;';if(idx<base){fill++;if(isSub&&idx>=base-delta)st='background:linear-gradient(135deg,#ffdad4 0 42%,#d2503f 44% 56%,#ffdad4 58% 100%);border:2px solid #d2503f;';else st='background:#ff8a3d;border:2px solid #e07d2a;';}else if(!isSub&&idx<total){fill++;st='background:#2aa39a;border:2px solid #178a82;';}cells.push({style:'width:26px;height:26px;border-radius:50%;'+st});}addFrames.push({full:fill===10,cells:cells,boxStyle:'display:inline-grid;grid-template-columns:repeat(5,26px);gap:6px;padding:10px;border-radius:14px;'+(fill===10?'border:3px solid #3aa655;background:#eafaef;':'border:3px solid #f0e2c8;background:#fff;')});}}\n        ");

        markup = markup.Replace(
            "<div style=\"font-size:54px; font-weight:900; text-align:center; margin-bottom:6px; white-space:nowrap;\">{{ prompt }}</div>",
            "<div style=\"font-size:54px; font-weight:900; text-align:center; margin-bottom:6px; white-space:nowrap;\">{{ prompt }}</div>\n            <sc-if value=\"{{ isMulViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; flex-wrap:wrap; gap:10px; justify-content:center; align-items:center; margin:8px 0 10px; max-width:720px;\">\n                <sc-for list=\"{{ mulGroups }}\" as=\"grp\" hint-placeholder-count=\"6\">\n                  <div style=\"{{ grp.style }}\">\n                    <sc-for list=\"{{ grp.cells }}\" as=\"cell\" hint-placeholder-count=\"8\"><div style=\"{{ cell.style }}\"></div></sc-for>\n                  </div>\n                </sc-for>\n              </div>\n            </sc-if>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "if(q.isKokugo){isKokugo=true;kokuPre=q.pre;kokuWord=q.word;kokuPost=q.post;kokuMean=q.mean;}",
            "if(q.isKokugo){isKokugo=true;kokuPre=q.pre;kokuWord=q.word;kokuPost=q.post;kokuMean=q.mean;kokuInstruction=q.subtype==='kanji-choice'?'ただしい かんじを えらぼう':'したせんの ことばは なんと よむ？';}",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "<div style=\"font-size:22px; color:#9a8662; font-weight:700;\">したせんの ことばは なんと よむ？</div>",
            "<div style=\"font-size:22px; color:#9a8662; font-weight:700;\">{{ kokuInstruction }}</div>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "isAddViz:isAddViz, addFrames:addFrames,\n      isKokugo:isKokugo",
            "isAddViz:isAddViz, addFrames:addFrames, isMulViz:isMulViz, mulGroups:mulGroups,\n      isKokugo:isKokugo",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "kokuPre:kokuPre, kokuWord:kokuWord, kokuPost:kokuPost, kokuMean:kokuMean,",
            "kokuPre:kokuPre, kokuWord:kokuWord, kokuPost:kokuPost, kokuMean:kokuMean, kokuInstruction:kokuInstruction,",
            StringComparison.Ordinal);

        return markup;
    }

    private static string ReplaceBlock(string source, string startToken, string endToken, string replacement)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            return source;
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            return source;
        }

        return source[..start] + replacement + source[end..];
    }

    private static string ReplaceBundledProfiles(string html, string profileName)
    {
        const string startToken = "profiles:[\n";
        const string endToken = "\n    session:null";

        var start = html.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            return html;
        }

        var end = html.IndexOf(endToken, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return html;
        }

        var escapedName = JsonSerializer.Serialize(profileName);
        var replacement =
            "profiles:[\n" +
            $"      {{name:{escapedName},grade:1,color:'#4ad991',streak:0,stars:0,{BeginnerMasteryMarkup}}},\n" +
            "    ],";

        return html[..start] + replacement + html[end..];
    }
}
