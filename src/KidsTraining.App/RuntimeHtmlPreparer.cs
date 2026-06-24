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
            "genAdd(p){const m=p&&p.mastery?Number(p.mastery.add):0.05;let a,b;if(m<0.35){a=this.rand(1,5);b=this.rand(1,5);if(a+b>10)b=Math.max(1,10-a);}else if(m<0.65){a=this.rand(2,9);b=this.rand(1,9);if(a+b>18)b=Math.max(1,18-a);}else{a=this.rand(6,18);b=this.rand(4,18);}const ans=a+b;return{topic:'add',mode:'num',n1:a,n2:b,prompt:a+' + '+b,answer:''+ans,explanation:a+' + '+b+' = '+ans};}");

        markup = ReplaceBlock(
            markup,
            "genSub(){",
            "\n  genHissan(){",
            "genSub(p){const m=p&&p.mastery?Number(p.mastery.sub):0.05;let a,b;if(m<0.35){a=this.rand(2,10);b=this.rand(1,a-1);}else if(m<0.65){a=this.rand(11,18);b=this.rand(1,Math.max(1,a%10));}else{a=this.rand(11,20);const o=a%10;b=this.rand(o+1,9);}const ans=a-b;return{topic:'sub',mode:'num',a:a,b:b,prompt:a+' - '+b,answer:''+ans,explanation:a+' - '+b+' = '+ans};}");

        markup = ReplaceBlock(
            markup,
            "pickMul(){",
            "\n  pick4(",
            "pickMul(){const L=[[7,6,42,[42,48,36,56]],[8,7,56,[56,54,48,63]],[6,8,48,[48,42,54,56]],[9,4,36,[36,32,45,40]],[7,8,56,[56,49,54,63]],[6,7,42,[42,36,48,40]],[8,4,32,[32,28,36,24]],[9,7,63,[63,56,72,54]]];const it=L[this.rand(0,L.length-1)];return{topic:'mul',mode:'choices',a:it[0],b:it[1],prompt:it[0]+' x '+it[1],answer:''+it[2],choices:this.shuffle(it[3].map(String)),explanation:it[0]+' x '+it[1]+' = '+it[2]};}");

        markup = markup.Replace(
            "genFor(k){return k==='add'?this.genAdd():k==='sub'?this.genSub():k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            "genFor(k,p){return k==='add'?this.genAdd(p):k==='sub'?this.genSub(p):k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "weightedPick(p){",
            "\n  total(){",
            "learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(p.grade<2&&(stars<85||level<=3))return 3;return 4;}\n  allowedTopics(p){const all=Object.keys(this.topics);const cfg=this.state.settings;const en=(cfg&&cfg.topics)?all.filter(k=>cfg.topics[k]):all;const enabled=en.length?en:all;const stage=this.learningStage(p);const staged=stage<=1?['add']:stage===2?['add','sub']:stage===3?['add','sub','clock','kokugo']:all;const allowed=enabled.filter(k=>staged.includes(k));return allowed.length?allowed:enabled;}\n  weightedPick(p){const ks=this.allowedTopics(p);const w=ks.map(k=>0.25+(1-(Number(p.mastery[k])||0.05))*1.7);let s=w.reduce((a,b)=>a+b,0),r=Math.random()*s;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}");

        markup = markup.Replace(
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p)));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p),p));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "lvl(p){",
            "\n\n  selectProfile",
            "skillLevel(p){const values=Object.values(p.mastery||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const avg=values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;const top=values.length?Math.max(...values):0.05;const stars=Math.min(Number(p.stars)||0,80);const score=Math.min(1,avg*0.55+top*0.35+stars/250);return Math.max(1,Math.min(5,Math.floor(score*5)));}\n  lvl(p){return 'レベル '+this.skillLevel(p);}");

        markup = markup.Replace(
            "const weakKeys=Object.keys(T).filter(k=>p.mastery[k]<0.5);",
            "const weakKeys=this.allowedTopics(p).filter(k=>p.mastery[k]<0.5);",
            StringComparison.Ordinal);

        markup = PatchArithmeticVisuals(markup);

        return markup;
    }

    private static string PatchArithmeticVisuals(string markup)
    {
        markup = markup.Replace(
            "let isAddViz=false,addFrames=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
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
            "isAddViz:isAddViz, addFrames:addFrames,\n      isKokugo:isKokugo",
            "isAddViz:isAddViz, addFrames:addFrames, isMulViz:isMulViz, mulGroups:mulGroups,\n      isKokugo:isKokugo",
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
