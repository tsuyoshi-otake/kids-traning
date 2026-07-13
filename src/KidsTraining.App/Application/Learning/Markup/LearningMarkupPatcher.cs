using System.Text.Json;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private const string BeginnerMasteryMarkup = LearningDefaults.BeginnerMasteryMarkup;
    private const string DefaultEmergencyPin = LearningDefaults.DefaultEmergencyPin;

    public static string Apply(string markup, string profileName, string parentPassword)
    {
        markup = ReplaceRequired(markup, "screen:'profile', profileIdx:0,", "screen:'start', profileIdx:0,", StringComparison.Ordinal);
        markup = ReplaceRequired(markup,
            "unlockPC(){this.sfx('unlock');this.setState({screen:'profile',session:null,combo:0,pin:'',emergencyDone:false});}",
            "unlockPC(){this.sfx('unlock');this.setState({screen:'start',session:null,combo:0,pin:'',emergencyDone:false});}",
            StringComparison.Ordinal);
        markup = PatchBeginnerProgression(markup);
        markup = PatchParentPassword(markup, parentPassword);

        return ReplaceBundledProfiles(markup, profileName);
    }

    private static string PatchParentPassword(string markup, string parentPassword)
    {
        var password = parentPassword.Length == 4 && parentPassword.All(static character => character is >= '0' and <= '9')
            ? parentPassword
            : DefaultEmergencyPin;
        markup = ReplaceRequired(markup,
            "pinPress(d){if(this.state.emergencyDone||this.state.pin.length>=4)return;",
            $"parentPin(){{try{{return localStorage.getItem('kt_parent_pin_v1')||'{password}';}}catch{{return '{password}';}}}}\n  pinPress(d){{if(this.state.emergencyDone||this.state.pin.length>=4)return;",
            StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "const ok=np==='1234';", "const ok=np===this.parentPin();", StringComparison.Ordinal);
        return markup;
    }

    private static string PatchBeginnerProgression(string markup)
    {
        markup = ReplaceRequired(markup,
            "mastery:{add:.5,sub:.5,mul:.5,clock:.5,kokugo:.5,hissan:.5}",
            BeginnerMasteryMarkup,
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "defaultSettings(){return {topics:{add:true,sub:true,hissan:true,mul:true,clock:true,kokugo:true},count:this.props.questionCount??10,pass:this.props.passLine??8};}",
            "defaultSettings(){return {topics:{add:true,sub:true,hissan:true,mul:true,clock:true,kokugo:true,moji:true,measure:true,kazu:true,shape:true,div:true,frac:true,chart:true,story:true,bun:true,goi:true,dokkai:true,eigo:true},count:this.props.questionCount??20,pass:this.props.passLine??15};}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "countDelta(d){this.sfx('tap');const s=this.state.settings;const count=this.clamp(s.count+d,4,15);this.setSettings({count:count,pass:Math.min(s.pass,count)});}",
            "countDelta(d){this.sfx('tap');const s=this.state.settings;const count=this.clamp((Number(s.count)||20)+d,20,40);this.setSettings({count:count,pass:Math.min(Math.max(15,Number(s.pass)||15),count)});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "    kokugo:{label:'こくご',color:'#d2691e'},\n  };",
            "    kokugo:{label:'こくご',color:'#d2691e'},\n    moji:{label:'もじ',color:'#4f7edb'},\n    measure:{label:'たんい',color:'#3aa655'},\n    kazu:{label:'かず',color:'#c2891f'},\n    shape:{label:'かたち',color:'#9a4fd6'},\n    div:{label:'わりざん',color:'#0f8fbf'},\n    frac:{label:'ぶんすう',color:'#d64f8e'},\n    chart:{label:'グラフ',color:'#5a8f29'},\n    story:{label:'ぶんしょうだい',color:'#8a6d3b'},\n    bun:{label:'ぶん',color:'#7a5cd6'},\n    goi:{label:'ことば',color:'#2f9e8f'},\n    dokkai:{label:'よみとり',color:'#c2503f'},\n    eigo:{label:'えいご',color:'#2563eb'},\n  };",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "buildCalib(){const order=['add','sub','hissan','mul','kokugo'];return order.map(t=>{const q=this.genFor(t);return{q:q,choices:this.calibChoicesFor(q)};});}",
            "buildCalib(){const order=['add','sub','hissan','mul','kokugo','moji'];return order.map(t=>{const q=this.genFor(t);return{q:q,choices:this.calibChoicesFor(q)};});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "['add','sub','hissan','mul','clock','kokugo'].forEach(t=>{mastery[t]=results[t]===undefined?0.5:(results[t]?0.72:0.32);});",
            "['add','sub','hissan','mul','clock','kokugo','moji','measure','kazu','shape','div','frac','chart','story','bun','goi','dokkai','eigo'].forEach(t=>{mastery[t]=results[t]===undefined?0.5:(results[t]?0.72:0.32);});",
            StringComparison.Ordinal);

        // pick4 originally padded missing distractors with the answer plus invisible
        // ideographic spaces, producing a choice that looks identical to the correct
        // one but counts as wrong. Return fewer choices instead of fake duplicates.
        markup = ReplaceRequired(markup,
            "pick4(ans,pool){const s=new Set([ans]);const out=[ans];for(const x of pool){if(out.length>=4)break;if(!s.has(x)){s.add(x);out.push(x);}}let k=1;while(out.length<4){const v=ans+'　'.repeat(k);if(!s.has(v)){s.add(v);out.push(v);}k++;}return this.shuffle(out);}",
            "pick4(ans,pool){const s=new Set([String(ans)]);const out=[String(ans)];for(const x of pool){if(out.length>=4)break;const v=String(x);if(!s.has(v)){s.add(v);out.push(v);}}return this.shuffle(out);}",
            StringComparison.Ordinal);

        markup = PatchRewardSystem(markup);

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
            "genHissan(){",
            "\n  hissanAdd(",
            BuildGenHissanScript());

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

        markup = ReplaceRequired(markup,
            "genFor(k){return k==='add'?this.genAdd():k==='sub'?this.genSub():k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            "genFor(k,p){const stage=this.reviewStage(p,k),sp=this.profileAtStage(p,k,stage),q=k==='add'?this.genAdd(sp):k==='sub'?this.genSub(sp):k==='hissan'?this.genHissan(sp):k==='mul'?this.pickMul(sp):k==='clock'?this.pickClock(sp):k==='measure'?this.pickMeasure(sp):k==='kazu'?this.pickKazu(sp):k==='shape'?this.pickShape(sp):k==='div'?this.pickDiv(sp):k==='frac'?this.pickFrac(sp):k==='chart'?this.pickChart(sp):k==='story'?this.pickStory(sp):k==='kokugo'?this.pickKokugo(sp):k==='bun'?this.pickBun(sp):k==='goi'?this.pickGoi(sp):k==='dokkai'?this.pickDokkai(sp):k==='eigo'?this.pickEigo(sp):this.pickMoji(sp);q.difficulty=stage;return q;}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "weightedPick(p){",
            "\n  total(){",
            BuildProgressionScript());

        markup = ReplaceRequired(markup,
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p)));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p),p));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars,startXp:Number(p.xp)||0};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "lvl(p){",
            "\n\n  selectProfile",
            "skillLevel(p){const values=Object.values(p.mastery||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const avg=this.skillAverage(p),top=values.length?Math.max(...values):0.05,stars=Math.min(Number(p.stars)||0,180);const score=Math.min(1,avg*0.45+top*0.35+stars/320);return Math.max(1,Math.min(5,Math.floor(score*5)));}\n  xpLevel(p){return Math.max(1,Math.floor((Number(p&&p.xp)||0)/100)+1);}\n  lvl(p){return 'レベル '+this.xpLevel(p);}");

        markup = ReplaceRequired(markup,
            "const weakKeys=Object.keys(T).filter(k=>p.mastery[k]<0.5);",
            "const weakKeys=this.allowedTopics(p).filter(k=>(Number(p.mastery[k])||0.05)<0.5);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "const m=p.mastery[k];const pct=Math.round(m*100);const weak=m<0.5;const status=m>=0.75?'とくい':m>=0.5?'ふつう':'にがて';const bc=m>=0.75?'#3aa655':m>=0.5?'#9fd17a':'#ff8a8a';const sc2=m<0.5?'#d2503f':'#6b5e45';",
            "const m=Number(p.mastery[k])||0.05;const pct=Math.round(m*100);const masteryLevel=this.topicStage(p,k);const weak=masteryLevel<=2;const unlocked=this.allowedTopics(p).includes(k);const cleared=this.topicComplete(p,k);const status=(!unlocked?'🔒 ':'')+'Lv.'+masteryLevel+'/5'+(cleared?' ★':'');const levelColors=['#ff8a8a','#f2a03d','#e0c13d','#79b85a','#3aa655'];const bc=levelColors[masteryLevel-1];const sc2=!unlocked?'#b7a98a':masteryLevel===5?'#2f7d44':'#6b5e45';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "gradeLabel:pr.grade+'年生'",
            "gradeLabel:this.gradeLabel(pr)",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "profileGrade:p.grade+'年生'",
            "profileGrade:this.gradeLabel(p)",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "        <div style=\"display:flex; align-items:center; gap:14px;\">\n          <div style=\"{{ avatarStyle }}\">{{ profileInitial }}</div>\n          <div>",
            "        <div style=\"display:flex; align-items:center;\">\n          <div>",
            StringComparison.Ordinal);

        markup = PatchArithmeticVisuals(markup);
        markup = PatchEnglishSpeech(markup);

        return markup;
    }


    private static string ReplaceRequired(
        string source,
        string oldValue,
        string newValue,
        StringComparison comparison)
    {
        if (source.IndexOf(oldValue, comparison) < 0)
        {
            throw new InvalidOperationException($"Required learning markup anchor was not found: {DescribeAnchor(oldValue)}");
        }

        return source.Replace(oldValue, newValue, comparison);
    }

    private static string ReplaceBlock(string source, string startToken, string endToken, string replacement)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Required learning block start was not found: {DescribeAnchor(startToken)}");
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Required learning block end was not found: {DescribeAnchor(endToken)}");
        }

        return source[..start] + replacement + source[end..];
    }

    private static string DescribeAnchor(string anchor)
    {
        const int maxLength = 80;
        var singleLine = anchor.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
        return singleLine.Length <= maxLength ? singleLine : singleLine[..maxLength] + "...";
    }

    private static string ReplaceBundledProfiles(string html, string profileName)
    {
        const string startToken = "profiles:[\n";
        const string endToken = "\n    session:null";

        var start = html.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Required profiles block start was not found: {DescribeAnchor(startToken)}");
        }

        var end = html.IndexOf(endToken, start, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Required profiles block end was not found: {DescribeAnchor(endToken)}");
        }

        var escapedName = JsonSerializer.Serialize(profileName);
        var replacement =
            "profiles:[\n" +
            $"      {{name:{escapedName},grade:1,color:'#4ad991',streak:0,stars:0,xp:0,{BeginnerMasteryMarkup}}},\n" +
            "    ],";

        return html[..start] + replacement + html[end..];
    }
}
