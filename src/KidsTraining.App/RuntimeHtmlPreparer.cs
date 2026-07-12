using System.Text;
using System.Text.Json;

namespace KidsTraining.App;

internal static class RuntimeHtmlPreparer
{
    public const string DefaultEmergencyPin = "1234";
    private const string TemplateOpenTag = "<script type=\"__bundler/template\">";
    private const string TemplateCloseTag = "</script>";
    public const string BeginnerMasteryMarkup = "mastery:{add:.05,sub:.05,mul:.05,clock:.05,kokugo:.05,hissan:.05,moji:.05,measure:.05,kazu:.05,shape:.05,div:.05,frac:.05,chart:.05,story:.05,bun:.05,goi:.05,dokkai:.05}";

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
        html = PatchBundledTemplate(html, PrimaryProfileName, ParentSettings.GetParentPassword());

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

    private static string PatchBundledTemplate(string html, string profileName, string parentPassword)
    {
        if (!TryFindBundledTemplate(html, out var contentStart, out var contentEnd))
        {
            return PatchLearningMarkup(html, profileName, parentPassword);
        }

        var template = ExtractBundledTemplate(html)
            ?? throw new InvalidOperationException("Bundled learning template could not be decoded.");
        var patchedTemplate = PatchLearningMarkup(template, profileName, parentPassword);
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

    private static string PatchLearningMarkup(string markup, string profileName, string parentPassword)
    {
        markup = markup.Replace("screen:'profile', profileIdx:0,", "screen:'start', profileIdx:0,", StringComparison.Ordinal);
        markup = markup.Replace(
            "unlockPC(){this.sfx('unlock');this.setState({screen:'profile',session:null,combo:0,pin:'',emergencyDone:false});}",
            "unlockPC(){this.sfx('unlock');this.setState({screen:'start',session:null,combo:0,pin:'',emergencyDone:false});}",
            StringComparison.Ordinal);
        markup = PatchBeginnerProgression(markup);
        markup = PatchParentPassword(markup, parentPassword);

        return ReplaceBundledProfiles(markup, profileName);
    }

    private static string PatchParentPassword(string markup, string parentPassword)
    {
        var password = ParentSettings.NormalizePassword(parentPassword) ?? DefaultEmergencyPin;
        markup = markup.Replace(
            "pinPress(d){if(this.state.emergencyDone||this.state.pin.length>=4)return;",
            $"parentPin(){{try{{return localStorage.getItem('kt_parent_pin_v1')||'{password}';}}catch{{return '{password}';}}}}\n  pinPress(d){{if(this.state.emergencyDone||this.state.pin.length>=4)return;",
            StringComparison.Ordinal);
        markup = markup.Replace("const ok=np==='1234';", "const ok=np===this.parentPin();", StringComparison.Ordinal);
        return markup;
    }

    private static string PatchBeginnerProgression(string markup)
    {
        markup = markup.Replace(
            "mastery:{add:.5,sub:.5,mul:.5,clock:.5,kokugo:.5,hissan:.5}",
            BeginnerMasteryMarkup,
            StringComparison.Ordinal);

        markup = markup.Replace(
            "defaultSettings(){return {topics:{add:true,sub:true,hissan:true,mul:true,clock:true,kokugo:true},count:this.props.questionCount??10,pass:this.props.passLine??8};}",
            "defaultSettings(){return {topics:{add:true,sub:true,hissan:true,mul:true,clock:true,kokugo:true,moji:true,measure:true,kazu:true,shape:true,div:true,frac:true,chart:true,story:true,bun:true,goi:true,dokkai:true},count:this.props.questionCount??20,pass:this.props.passLine??15};}",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "    kokugo:{label:'こくご',color:'#d2691e'},\n  };",
            "    kokugo:{label:'こくご',color:'#d2691e'},\n    moji:{label:'もじ',color:'#4f7edb'},\n    measure:{label:'たんい',color:'#3aa655'},\n    kazu:{label:'かず',color:'#c2891f'},\n    shape:{label:'かたち',color:'#9a4fd6'},\n    div:{label:'わりざん',color:'#0f8fbf'},\n    frac:{label:'ぶんすう',color:'#d64f8e'},\n    chart:{label:'グラフ',color:'#5a8f29'},\n    story:{label:'ぶんしょうだい',color:'#8a6d3b'},\n    bun:{label:'ぶん',color:'#7a5cd6'},\n    goi:{label:'ことば',color:'#2f9e8f'},\n    dokkai:{label:'よみとり',color:'#c2503f'},\n  };",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "buildCalib(){const order=['add','sub','hissan','mul','kokugo'];return order.map(t=>{const q=this.genFor(t);return{q:q,choices:this.calibChoicesFor(q)};});}",
            "buildCalib(){const order=['add','sub','hissan','mul','kokugo','moji'];return order.map(t=>{const q=this.genFor(t);return{q:q,choices:this.calibChoicesFor(q)};});}",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "['add','sub','hissan','mul','clock','kokugo'].forEach(t=>{mastery[t]=results[t]===undefined?0.5:(results[t]?0.72:0.32);});",
            "['add','sub','hissan','mul','clock','kokugo','moji','measure','kazu','shape','div','frac','chart','story','bun','goi','dokkai'].forEach(t=>{mastery[t]=results[t]===undefined?0.5:(results[t]?0.72:0.32);});",
            StringComparison.Ordinal);

        // pick4 originally padded missing distractors with the answer plus invisible
        // ideographic spaces, producing a choice that looks identical to the correct
        // one but counts as wrong. Return fewer choices instead of fake duplicates.
        markup = markup.Replace(
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

        markup = markup.Replace(
            "genFor(k){return k==='add'?this.genAdd():k==='sub'?this.genSub():k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            "genFor(k,p){return k==='add'?this.genAdd(p):k==='sub'?this.genSub(p):k==='hissan'?this.genHissan(p):k==='mul'?this.pickMul(p):k==='clock'?this.pickClock(p):k==='measure'?this.pickMeasure(p):k==='kazu'?this.pickKazu(p):k==='shape'?this.pickShape(p):k==='div'?this.pickDiv(p):k==='frac'?this.pickFrac(p):k==='chart'?this.pickChart(p):k==='story'?this.pickStory(p):k==='kokugo'?this.pickKokugo(p):k==='bun'?this.pickBun(p):k==='goi'?this.pickGoi(p):k==='dokkai'?this.pickDokkai(p):this.pickMoji(p);}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "weightedPick(p){",
            "\n  total(){",
            BuildProgressionScript());

        markup = markup.Replace(
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p)));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p),p));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars,startXp:Number(p.xp)||0};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "lvl(p){",
            "\n\n  selectProfile",
            "skillLevel(p){const values=Object.values(p.mastery||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const avg=this.skillAverage(p),top=values.length?Math.max(...values):0.05,stars=Math.min(Number(p.stars)||0,180);const score=Math.min(1,avg*0.45+top*0.35+stars/320);return Math.max(1,Math.min(5,Math.floor(score*5)));}\n  xpLevel(p){return Math.max(1,Math.floor((Number(p&&p.xp)||0)/100)+1);}\n  lvl(p){return 'レベル '+this.xpLevel(p);}");

        markup = markup.Replace(
            "const weakKeys=Object.keys(T).filter(k=>p.mastery[k]<0.5);",
            "const weakKeys=this.allowedTopics(p).filter(k=>(Number(p.mastery[k])||0.05)<0.5);",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "const m=p.mastery[k];const pct=Math.round(m*100);const weak=m<0.5;const status=m>=0.75?'とくい':m>=0.5?'ふつう':'にがて';const bc=m>=0.75?'#3aa655':m>=0.5?'#9fd17a':'#ff8a8a';const sc2=m<0.5?'#d2503f':'#6b5e45';",
            "const m=Number(p.mastery[k])||0.05;const pct=Math.round(m*100);const weak=m<0.5;const unlocked=this.allowedTopics(p).includes(k);const cleared=this.topicComplete(p,k);const status=!unlocked?'🔒まだ':cleared?'クリア！':m>=0.5?'ふつう':'にがて';const bc=cleared?'#e0a020':m>=0.5?'#9fd17a':'#ff8a8a';const sc2=!unlocked?'#b7a98a':cleared?'#c07a10':m<0.5?'#d2503f':'#6b5e45';",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "p.mastery[q.topic]=this.clamp(p.mastery[q.topic]+(perfect?0.12:-0.08),0.05,0.99);",
            "p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.08),0.05,0.99);",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "p.mastery[q.topic]=this.clamp(p.mastery[q.topic]+(correct?0.12:-0.16),0.05,0.99);",
            "p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(correct?0.12:-0.16),0.05,0.99);",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "p.mastery[q.topic]=this.clamp(p.mastery[q.topic]+(perfect?0.12:-0.05),0.05,0.99);",
            "p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.05),0.05,0.99);",
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

    private static string PatchRewardSystem(string markup)
    {
        markup = markup.Replace(
            "freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            BuildRewardMethodsScript() + "\n  freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "finishNumeric(){",
            "\n  submit(ans){",
            "finishNumeric(){const q=this.cur(),p=this.curP();const perfect=(this.state.numMiss||0)===0;p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.08),0.05,0.99);this.markCleared(p,q.topic);const combo=perfect?this.state.combo+1:0;const stars=perfect?(combo>=3?2:1):1,xpInfo=this.gainXp(p,perfect?(combo>=3?18:12):6);p.stars+=stars;if(perfect)this.state.session.correct++;this.sfx(perfect&&combo>=3?'combo':'correct');this.setState({screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,helped:!perfect,xp:xpInfo.amount,levelUp:xpInfo.levelUp},input:'',numChoices:null});}");

        markup = ReplaceBlock(
            markup,
            "submit(ans){",
            "\n  next(){",
            "submit(ans){const q=this.cur(),correct=String(ans)===String(q.answer),p=this.curP();p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(correct?0.12:-0.16),0.05,0.99);this.markCleared(p,q.topic);const combo=correct?this.state.combo+1:0,stars=correct?(combo>=3?2:1):0,xpInfo=correct?this.gainXp(p,combo>=3?18:12):{amount:0,levelUp:false};if(correct){p.stars+=stars;this.state.session.correct++;this.sfx(combo>=3?'combo':'correct');}else{this.sfx('wrong');}this.setState({screen:'feedback',combo:combo,lastResult:{correct:correct,q:q,userAns:ans,stars:stars,combo:combo,xp:xpInfo.amount,levelUp:xpInfo.levelUp},input:''});}");

        markup = ReplaceBlock(
            markup,
            "submitHissanStep(val){",
            "\n  unlockPC(){",
            "submitHissanStep(val){const q=this.cur(),st=q.steps[this.state.hsStep];const v=val!=null?val:this.state.input;if(v!==st.expect){this.sfx('wrong');const sm=(this.state.hsStepMiss||0)+1;const upd={hsHint:st.explain,input:'',hsMistakes:(this.state.hsMistakes||0)+1,hsStepMiss:sm};if(sm>=2)upd.hsStepChoices=this.numChoicesFor(st.expect);this.setState(upd);return;}const ns={input:'',hsHint:'',hsStepMiss:0,hsStepChoices:null};if(st.place==='ones'){ns.hsOnes=st.writeOnes;if(st.carry)ns.hsCarry=true;if(st.borrow)ns.hsBorrow=true;}else ns.hsTens=st.writeTens;const last=this.state.hsStep>=q.steps.length-1;if(last){const p=this.curP();const perfect=(this.state.hsMistakes||0)===0;p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.05),0.05,0.99);this.markCleared(p,q.topic);const combo=perfect?this.state.combo+1:0;const stars=perfect?(combo>=3?2:1):1,xpInfo=this.gainXp(p,perfect?(combo>=3?20:14):8);p.stars+=stars;this.state.session.correct++;this.sfx(perfect&&combo>=3?'combo':'correct');this.setState({...ns,screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,viaSteps:true,perfect:perfect,xp:xpInfo.amount,levelUp:xpInfo.levelUp}});}else{this.sfx('step');this.setState({...ns,hsStep:this.state.hsStep+1});}}");

        markup = markup.Replace(
            "      <!-- center -->",
            "      <div style=\"margin-top:18px; background:#fff; border:4px solid #f0e2c8; border-radius:20px; padding:12px 16px; display:grid; grid-template-columns:auto 1fr auto; gap:12px; align-items:center;\">\n        <div style=\"font-size:18px; font-weight:900; color:#4f7edb; white-space:nowrap;\">EXP {{ xpText }}</div>\n        <div style=\"height:18px; background:#eef3ff; border:3px solid #3a3326; border-radius:12px; overflow:hidden;\"><span style=\"{{ xpBarStyle }}\"></span></div>\n        <div style=\"font-size:15px; font-weight:700; color:#6b5e45; white-space:nowrap;\">あと {{ xpToNext }} XP</div>\n      </div>\n      <!-- center -->",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "      <div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            "      <div style=\"margin-top:12px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:10px 22px; min-width:280px; text-align:center;\">\n        <div style=\"font-size:15px; color:#4f7edb; font-weight:900;\">けいけんち</div>\n        <div style=\"font-size:34px; color:#4f7edb; font-weight:900;\">+{{ fbXp }} XP</div>\n        <sc-if value=\"{{ fbLevelUp }}\" hint-placeholder-val=\"{{ false }}\"><div style=\"font-size:22px; color:#e09020; font-weight:900; animation:popIn .45s ease-out;\">レベルアップ！ {{ level }}</div></sc-if>\n      </div>\n      <div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#9a8662;\">ごうけい ★</div>\n          <div style=\"font-size:36px; font-weight:900;\">{{ totalStars }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            "        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#9a8662;\">ごうけい ★</div>\n          <div style=\"font-size:36px; font-weight:900;\">{{ totalStars }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #c9d8ff; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#4f7edb;\">きょうの XP</div>\n          <div style=\"font-size:36px; font-weight:900; color:#4f7edb;\">+{{ earnedXp }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "      <div onclick=\"{{ unlockPC }}\" style=\"margin-top:28px; background:#3aa655; color:#fff; border:5px solid #2f8a46; border-radius:28px; padding:20px 64px; font-size:36px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #2a7d3f;\">🔓 パソコンを つかう</div>",
            "      <div style=\"display:flex; gap:16px; margin-top:28px; flex-wrap:wrap; justify-content:center;\">\n        <div onclick=\"{{ goStart }}\" style=\"background:#ff8a3d; color:#fff; border:5px solid #e07d2a; border-radius:28px; padding:18px 44px; font-size:30px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #d96a26; min-width:300px; text-align:center;\">▶ べんきょうを つづける</div>\n        <div onclick=\"{{ unlockPC }}\" style=\"background:#3aa655; color:#fff; border:5px solid #2f8a46; border-radius:28px; padding:18px 44px; font-size:30px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #2a7d3f; min-width:300px; text-align:center;\">🔓 パソコンを つかう</div>\n      </div>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "const sess=S.session||{};\n    const earned=sess.startStars!=null?(p.stars-sess.startStars):0;\n    const fbBgColor=lr.correct?'#eafbe8':'#fdeeee';",
            BuildRewardRenderScript(),
            StringComparison.Ordinal);

        markup = markup.Replace(
            "profileName:p.name, profileInitial:p.name.charAt(0), avatarStyle:avatar(p.color,56,26), profileGrade:p.grade+'年生', stars:p.stars, streak:p.streak, level:this.lvl(p),",
            "profileName:p.name, profileGrade:p.grade+'年生', stars:p.stars, streak:p.streak, level:this.lvl(p), xpText:xpText, xpToNext:xpToNext, xpBarStyle:xpBarStyle,",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "clearCorrect:sess.correct||0, earnedStars:earned, totalStars:p.stars, weakNextLabel:weakLabels||'なし', unlockPC:()=>this.unlockPC(),",
            "clearCorrect:sess.correct||0, earnedStars:earned, earnedXp:earnedXp, totalStars:p.stars, weakNextLabel:weakLabels||'なし', unlockPC:()=>this.unlockPC(),",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:fb.prompt||'', fbAnswer:fb.answer||'', fbExplanation:fb.explanation||'', fbStarText:lr.stars||0, fbCombo:(lr.combo||0)>=3, fbTopicLabel:fb.topic?T[fb.topic].label:'',",
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:fb.prompt||'', fbAnswer:fb.answer||'', fbExplanation:fb.explanation||'', fbStarText:lr.stars||0, fbXp:lr.xp||0, fbLevelUp:!!lr.levelUp, fbCombo:(lr.combo||0)>=3, fbTopicLabel:fb.topic?T[fb.topic].label:'',",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildRewardMethodsScript()
    {
        return """
gainXp(p,amount){const before=this.xpLevel(p);p.xp=(Number(p.xp)||0)+amount;const after=this.xpLevel(p);return{amount:amount,levelUp:after>before};}
""";
    }

    private static string BuildRewardRenderScript()
    {
        return """
const sess=S.session||{};
    const earned=sess.startStars!=null?(p.stars-sess.startStars):0;
    const earnedXp=sess.startXp!=null?((Number(p.xp)||0)-sess.startXp):0;
    const fbBgColor=lr.correct?'#eafbe8':'#fdeeee';
    const xpValue=Number(p.xp)||0,xpLevel=this.xpLevel(p),xpInto=xpValue%100,xpToNext=100-xpInto,xpText=xpInto+' / 100';
    const xpBarStyle='display:block;height:100%;width:'+Math.max(4,Math.min(100,xpInto))+'%;background:#4f7edb;transition:width .35s;border-radius:10px;';
""";
    }

    private static string BuildGenAddScript()
    {
        return """
genAdd(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'add'),m=p&&p.mastery?Number(p.mastery.add):0.05,mentalAddendMax=9;if(stage>=2&&Math.random()<0.18){const x=this.rand(1,9),y=Math.random()<0.5?10-x:this.rand(1,Math.min(9,15-x)),z=this.rand(1,Math.min(9,19-x-y)),s=x+y+z;return{topic:'add',mode:'num',n1:x+y,n2:z,prompt:x+' + '+y+' + '+z,answer:''+s,explanation:'まえから じゅんに。'+x+'+'+y+'='+(x+y)+'、'+(x+y)+'+'+z+'='+s+'。'};}const tensReady=g>=2||(stage>=3&&m>=0.65);if(tensReady&&Math.random()<0.35){const ta=this.rand(1,8),tb=this.rand(1,Math.min(9,10-ta)),ax=ta*10,bx=tb*10,tans=ax+bx;return{topic:'add',mode:'num',n1:ax,n2:bx,prompt:ax+' + '+bx,answer:''+tans,explanation:'10のまとまりで かんがえる。'+ta+' + '+tb+' = '+(ta+tb)+' だから '+ax+' + '+bx+' = '+tans+'。'};}let a,b;if(g<=1){if(stage<=1||m<0.30){a=this.rand(1,5);b=this.rand(1,5);if(a+b>10)b=Math.max(1,10-a);}else if(stage<=2||m<0.65){a=this.rand(2,9);b=this.rand(1,9);if(a+b>18)b=Math.max(1,18-a);}else{a=this.rand(10,29);b=this.rand(1,mentalAddendMax);}}else if(g===2){if(stage<=2||m<0.45){a=this.rand(10,88);if(a%10===9)a--;b=this.rand(1,Math.min(mentalAddendMax,9-(a%10)));}else{a=this.rand(18,89);b=this.rand(1,mentalAddendMax);}}else{if(stage<=3||m<0.55){a=this.rand(25,89);b=this.rand(1,mentalAddendMax);}else{a=this.rand(35,89);b=this.rand(1,mentalAddendMax);}}const ans=a+b;return{topic:'add',mode:'num',n1:a,n2:b,prompt:a+' + '+b,answer:''+ans,explanation:a+' + '+b+' = '+ans};}
""";
    }

    private static string BuildGenSubScript()
    {
        return """
genSub(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'sub'),m=p&&p.mastery?Number(p.mastery.sub):0.05,mentalSubtrahendMax=9;if(stage>=2&&Math.random()<0.18){const x=this.rand(8,18),y=this.rand(1,x-2),z=this.rand(1,x-y-1),s=x-y-z;return{topic:'sub',mode:'num',a:x,b:y+z,prompt:x+' - '+y+' - '+z,answer:''+s,explanation:'まえから じゅんに。'+x+'−'+y+'='+(x-y)+'、'+(x-y)+'−'+z+'='+s+'。'};}const tensReady=g>=2||(stage>=3&&m>=0.65);if(tensReady&&Math.random()<0.35){const tb=this.rand(1,8),ta=this.rand(tb+1,9),ax=ta*10,bx=tb*10,tans=ax-bx;return{topic:'sub',mode:'num',a:ax,b:bx,prompt:ax+' - '+bx,answer:''+tans,explanation:'10のまとまりで かんがえる。'+ta+' - '+tb+' = '+(ta-tb)+' だから '+ax+' - '+bx+' = '+tans+'。'};}let a,b;if(g<=1){if(stage<=1||m<0.30){a=this.rand(2,10);b=this.rand(1,a-1);}else if(stage<=2||m<0.65){a=this.rand(11,18);b=this.rand(1,Math.max(1,a%10));}else{a=this.rand(11,29);b=this.rand(1,mentalSubtrahendMax);}}else if(g===2){if(stage<=2||m<0.45){a=this.rand(21,89);if(a%10===0)a++;b=this.rand(1,Math.min(mentalSubtrahendMax,a%10));}else{a=this.rand(30,99);b=this.rand(1,mentalSubtrahendMax);}}else{if(stage<=3||m<0.55){a=this.rand(35,99);b=this.rand(1,mentalSubtrahendMax);}else{a=this.rand(50,99);b=this.rand(1,mentalSubtrahendMax);}}const ans=a-b;return{topic:'sub',mode:'num',a:a,b:b,prompt:a+' - '+b,answer:''+ans,explanation:a+' - '+b+' = '+ans};}
""";
    }

    private static string BuildGenHissanScript()
    {
        return """
genHissan(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'hissan');if(stage>=4){const r=Math.random();if(g>=3&&r<0.2){const v=this.rand(0,2);let a,b;if(v===0){a=this.rand(12,89);b=this.rand(2,9);}else if(v===1){a=this.rand(112,489);b=this.rand(2,9);}else{a=this.rand(12,48);b=this.rand(11,29);}const ans=a*b,oa=a%10;return{topic:'hissan',mode:'num',prompt:a+' × '+b,answer:''+ans,explanation:v===0?'ひっ算は 一のくらいから。'+oa+'×'+b+'='+(oa*b)+'、'+Math.floor(a/10)+'0×'+b+'='+(Math.floor(a/10)*10*b)+'。あわせて '+ans+'。':'ひっ算は 一のくらいから じゅんに かける。'+a+' × '+b+' = '+ans+'。'};}if(g>=2&&r>=0.2&&r<0.4){const four=g>=3&&Math.random()<0.4;if(Math.random()<0.5){const a=four?this.rand(1234,8642):this.rand(123,868),b=four?this.rand(1111,9999-a):this.rand(111,999-a),ans=a+b;return{topic:'hissan',mode:'num',prompt:a+' + '+b,answer:''+ans,explanation:'くらいごとに たす。'+a+' + '+b+' = '+ans+'。'};}const a=four?this.rand(3210,9876):this.rand(213,987),b=four?this.rand(1111,a-1000):this.rand(111,a-100),ans=a-b;return{topic:'hissan',mode:'num',prompt:a+' − '+b,answer:''+ans,explanation:'くらいごとに ひく。'+a+' − '+b+' = '+ans+'。'};}}if(stage<=1){let a,b;do{a=this.rand(12,28);b=this.rand(2,9);}while((a%10+b)<10);return this.hissanAdd(a,b);}if(stage===2){if(Math.random()<0.55){let a,b;do{a=this.rand(12,38);b=this.rand(2,9);}while((a%10+b)<10);return this.hissanAdd(a,b);}let a,b;do{a=this.rand(21,58);b=this.rand(2,9);}while(a%10>=b);return this.hissanSub(a,b);}if(stage===3){if(Math.random()<0.5){let a,b;do{a=this.rand(14,48);b=this.rand(12,38);}while((a%10+b%10)<10||a+b>99);return this.hissanAdd(a,b);}let a,b;do{a=this.rand(22,79);b=this.rand(11,Math.min(48,a-1));}while(!(a%10<b%10&&Math.floor(a/10)>=Math.floor(b/10)+1));return this.hissanSub(a,b);}if(Math.random()<0.5){let a,b;do{a=this.rand(14,68);b=this.rand(14,68);}while((a%10+b%10)<10||a+b>99);return this.hissanAdd(a,b);}let a,b;do{a=this.rand(22,99);b=this.rand(13,a-1);}while(!((a%10)<(b%10)&&Math.floor(a/10)>=Math.floor(b/10)+1));return this.hissanSub(a,b);}
""";
    }

    private static string BuildPickMulScript()
    {
        return """
pickMul(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'mul'),m=p&&p.mastery?Number(p.mastery.mul):0.05;let pairs;if(stage<=1||m<0.25){pairs=[[1,2],[2,1],[2,2],[1,3],[3,1]];}else if(stage<=2||m<0.45){pairs=[[1,2],[2,1],[2,2],[1,3],[3,1],[2,3],[3,2],[2,4],[4,2],[5,2],[2,5]];}else if(stage<=3||m<0.65){const tables=[1,2,3,4,5,10];pairs=[];tables.forEach(t=>{for(let x=1;x<=9;x++)pairs.push([t,x]);});}else{const tables=g>=3?[1,2,3,4,5,6,7,8,9]:[1,2,3,4,5,10];pairs=[];tables.forEach(t=>{for(let x=1;x<=9;x++)pairs.push([t,x]);});}const pair=pairs[this.rand(0,pairs.length-1)],a=pair[0],b=pair[1],ans=a*b;return{topic:'mul',mode:'choices',op:'mul',a:a,b:b,prompt:a+' x '+b,answer:''+ans,choices:this.pick4(''+ans,[ans+a,ans-a,ans+b,ans-b,a+b,Math.max(1,ans+a+b)].map(String)),explanation:a+' x '+b+' = '+ans+'。'+a+' こずつが '+b+' つ。'};}
""";
    }

    private static string BuildPickClockScript()
    {
        return """
clockExplain(h,m,ask,a){if(ask==='hour')return 'みじかい はり が '+h+' を さして いるね。こたえは '+a+'。';if(ask==='minute')return 'ながい はり が さす すうじ ×5 が ふん。'+(m/5)+'×5='+m+'ふん。こたえは '+a+'。';return 'みじかい はり＝じ、ながい はり＝ふん。こたえは '+a+'。';}
  pickTimeUnits(g){const eh=this.rand(1,10),em=[5,10,15,20][this.rand(0,3)],ed=[10,20,30][this.rand(0,2)];const elapsed=[eh+'時'+em+'分 の '+ed+'分後は？',eh+'時'+(em+ed)+'分',[eh+'時'+em+'分',(eh+1)+'時'+em+'分',eh+'時'+(em+ed-5)+'分'],em+'分に '+ed+'分を たすと '+(em+ed)+'分。'];const L=g>=3?[
    ['1分 は 何秒？','60秒',['30秒','100秒','10秒'],'1分 = 60秒。'],
    ['2分 は 何秒？','120秒',['60秒','100秒','200秒'],'1分=60秒 だから 2分=120秒。'],
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    elapsed
  ]:[
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    ['ごぜん は 何時間？','12時間',['10時間','24時間','6時間'],'ごぜんは 12時間、ごごも 12時間。'],
    elapsed
  ];const it=L[this.rand(0,L.length-1)];return{topic:'clock',mode:'choices',prompt:it[0],answer:it[1],choices:this.pick4(it[1],it[2]),explanation:it[3]};}
  pickClock(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'clock');if(g>=2&&stage>=4&&Math.random()<0.3)return this.pickTimeUnits(g);const hourStr=x=>((x-1+12)%12+1)+'じ';const kinds=stage<=1?['hour','hour']:stage===2?['hour','hour','half']:stage===3?['hour','half','minute']:(g>=2?['hour','half','minute','both']:['hour','hour','half','minute']);const k=kinds[this.rand(0,kinds.length-1)];let h=this.rand(1,12),m=0,ask='hour',prompt='なんじ？',a='',pool=[];
    if(k==='hour'){m=0;ask='hour';prompt='とけいを よもう ・ なんじ？';a=h+'じ';pool=[hourStr(h+1),hourStr(h-1),hourStr(h+2),hourStr(h+3)];}
    else if(k==='half'){m=30;ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ30ぷん';pool=[hourStr(h+1).replace('じ','じ30ぷん'),h+'じ',hourStr(h-1).replace('じ','じ30ぷん'),hourStr(h+2).replace('じ','じ30ぷん')];}
    else if(k==='minute'){const mins=[5,10,15,20,25,35,40,45,50,55];m=mins[this.rand(0,mins.length-1)];ask='minute';prompt='ながい はりを よもう ・ なんぷん？';a=m+'ふん';pool=[5,10,15,20,25,30,35,40,45,50,55].filter(x=>x!==m).map(x=>x+'ふん');}
    else{const mins=[10,15,20,40,45,50];m=mins[this.rand(0,mins.length-1)];ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ'+m+'ふん';pool=[hourStr(h+1).replace('じ','じ'+m+'ふん'),h+'じ'+(m===15?45:15)+'ふん',hourStr(h-1).replace('じ','じ'+m+'ふん'),h+'じ'];}
    return{topic:'clock',mode:'choices',isClock:true,h:h,m:m,ask:ask,prompt:prompt,answer:a,choices:this.pick4(a,pool),explanation:this.clockExplain(h,m,ask,a)};}
  measureCompare(){const kinds=[['length','どちらが ながい？','ながい','ます','こぶん'],['volume','どちらが たくさん はいる？','たくさん はいる','コップ','はいぶん'],['area','どちらが ひろい？','ひろい','ます','こぶん']];const kk=kinds[this.rand(0,2)];let n1=this.rand(3,9),n2=this.rand(3,9);while(n2===n1)n2=this.rand(3,9);const win=n1>n2?'あか':'あお';return{topic:'measure',mode:'choices',isMeasure:true,mkind:kk[0],m1:n1,m2:n2,prompt:kk[1],answer:win,choices:this.shuffle(['あか','あお']),explanation:'あかは '+kk[3]+' '+n1+kk[4]+'、あおは '+kk[3]+' '+n2+kk[4]+'。'+win+'の ほうが '+kk[2]+'。'};}
  pickMeasure(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'measure');if(g<=1)return this.measureCompare();const mc=(pr,ans,pool,ex)=>({topic:'measure',mode:'choices',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    Q.push(()=>mc('1cm は 何mm？','10mm',['1mm','100mm','5mm'],'1cm = 10mm。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'cm は 何mm？',(k*10)+'mm',[k+'mm',(k*100)+'mm',(k*10+5)+'mm'],'1cm = 10mm。'+k+'cm = '+(k*10)+'mm。');});
    Q.push(()=>{const k=this.rand(2,9);return mc((k*10)+'mm は 何cm？',k+'cm',[(k*10)+'cm',(k+1)+'cm',(k*100)+'cm'],'10mm = 1cm。'+(k*10)+'mm = '+k+'cm。');});
    Q.push(()=>{const k=this.rand(1,9);return mc(k+'m は 何cm？',(k*100)+'cm',[(k*10)+'cm',(k*1000)+'cm',(k*100+10)+'cm'],'1m = 100cm。'+k+'m = '+(k*100)+'cm。');});
    Q.push(()=>mc('1L は 何dL？','10dL',['100dL','1dL','5dL'],'1L = 10dL。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'L は 何dL？',(k*10)+'dL',[k+'dL',(k*100)+'dL',(k*10+5)+'dL'],'1L = 10dL。'+k+'L = '+(k*10)+'dL。'); });
    Q.push(()=>mc('1L は 何mL？','1000mL',['100mL','10mL','500mL'],'1L = 1000mL。'));
    Q.push(()=>mc('1dL は 何mL？','100mL',['10mL','1000mL','50mL'],'1dL = 100mL。'));
    Q.push(()=>{const its=[['えんぴつの ながさ','cm',['mm','m','L']],['プールの たての ながさ','m',['cm','mm','dL']],['ありの おおきさ','mm',['cm','m','kg']],['ぎゅうにゅうパックの かさ','L',['cm','m','g']]];const it=its[this.rand(0,its.length-1)];return mc(it[0]+' に あう たんいは？',it[1],it[2],it[0]+' は '+it[1]+' が ぴったり。');});
    if(stage>=3){
    Q.push(()=>{const x=this.rand(1,6)*10,y=this.rand(1,Math.min(6,Math.floor((90-x)/10)))*10;return mc(x+'cm + '+y+'cm は？',(x+y)+'cm',[(x+y-10)+'cm',(x+y+10)+'cm',(x+y)+'mm'],x+'cm + '+y+'cm = '+(x+y)+'cm。');});
    Q.push(()=>{const a2=this.rand(2,4),b2=this.rand(1,5);return mc(a2+'L'+b2+'dL は 何dL？',(a2*10+b2)+'dL',[(a2+b2)+'dL',(a2*10)+'dL',(a2*100+b2)+'dL'],a2+'L = '+(a2*10)+'dL。あわせて '+(a2*10+b2)+'dL。');});
    Q.push(()=>{const c=this.rand(2,8),d=this.rand(1,9);return mc(c+'cm'+d+'mm は 何mm？',(c*10+d)+'mm',[(c+d)+'mm',(c*10)+'mm',(c*100+d)+'mm'],c+'cm = '+(c*10)+'mm。あわせて '+(c*10+d)+'mm。');});
    }
    if(g>=3){
    Q.push(()=>mc('1km は 何m？','1000m',['100m','10m','10000m'],'1km = 1000m。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'km は 何m？',(k*1000)+'m',[(k*100)+'m',(k*10)+'m',(k*10000)+'m'],'1km = 1000m。'+k+'km = '+(k*1000)+'m。');});
    Q.push(()=>mc('1kg は 何g？','1000g',['100g','10g','10000g'],'1kg = 1000g。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'kg は 何g？',(k*1000)+'g',[(k*100)+'g',(k*10)+'g',k+'g'],'1kg = 1000g。'+k+'kg = '+(k*1000)+'g。');});
    Q.push(()=>{const k=this.rand(1,9);return mc('1kg'+(k*100)+'g は 何g？',(1000+k*100)+'g',[(100+k*100)+'g',(k*100)+'g',(1000+k*10)+'g'],'1kg = 1000g。あわせて '+(1000+k*100)+'g。');});
    Q.push(()=>{const x=this.rand(2,7)*100,y=1000-x;return mc(x+'g + '+y+'g は 何kg？','1kg',['2kg','10kg','100g'],x+'g + '+y+'g = 1000g = 1kg。');});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickKazu(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kazu');const mc=(pr,ans,pool,ex)=>({topic:'kazu',mode:'choices',prompt:pr,answer:''+ans,choices:this.pick4(''+ans,pool.map(String)),explanation:ex});const cmp=(a,b)=>({topic:'kazu',mode:'choices',prompt:'大きい ほうは？ '+a+' か '+b,answer:''+Math.max(a,b),choices:this.shuffle([''+a,''+b]),explanation:'くらべると '+Math.max(a,b)+' の ほうが 大きい。'});const Q=[];
    if(g<=1){
    Q.push(()=>{const t=this.rand(1,9),o=this.rand(1,9),n=t*10+o;return mc('10が '+t+'こ と 1が '+o+'こ で いくつ？',n,[n+1,n-1,t+o],'10が '+t+'こで '+(t*10)+'。あと '+o+' で '+n+'。');});
    Q.push(()=>{const n=this.rand(11,98);return mc(n+' の つぎの 数は？',n+1,[n-1,n+2,n+10],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const n=this.rand(12,99);return mc(n+' の 1つ まえの 数は？',n-1,[n+1,n-2,n-10],n+' の まえは '+(n-1)+'。');});
    Q.push(()=>{let a=this.rand(10,99),b=this.rand(10,99);while(b===a)b=this.rand(10,99);return cmp(a,b);});
    Q.push(()=>{const t=this.rand(2,9);return mc('10を '+t+'こ あつめた 数は？',t*10,[t,t*10+1,t+10],'10が '+t+'こで '+(t*10)+'。');});
    Q.push(()=>{const n=this.rand(4,8),pos=this.rand(1,n),dir=Math.random()<0.5?'ひだり':'みぎ';const pool=[pos-1,pos+1,pos-2,pos+2,n-pos+1,n-pos].filter(v=>v>=1&&v<=n&&v!==pos).map(v=>v+'ばんめ');return{topic:'kazu',mode:'choices',isOrder:true,oc:n,op:pos,od:dir,prompt:'オレンジの ますは '+dir+'から なんばんめ？',answer:pos+'ばんめ',choices:this.pick4(pos+'ばんめ',pool),explanation:dir+'から かぞえて '+pos+'ばんめ だよ。'};});
    }else if(g===2){
    Q.push(()=>{const h=this.rand(1,9),t=this.rand(0,9),o=this.rand(0,9),n=h*100+t*10+o;return mc('100が '+h+'こ、10が '+t+'こ、1が '+o+'こ の 数は？',n,[n+100,n+10,h+t+o],'100が '+h+'こで '+(h*100)+'。あわせて '+n+'。');});
    Q.push(()=>{const n=this.rand(101,998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const t=this.rand(11,99);return mc('10を '+t+'こ あつめた 数は？',t*10,[t*100,t+10,t*10+10],'10が '+t+'こで '+(t*10)+'。');});
    Q.push(()=>{const a=this.rand(100,900),b=a+this.rand(1,90);return cmp(a,b);});
    Q.push(()=>{const h=this.rand(2,9);return mc((h*100)+' は 100を なんこ あつめた 数？',h+'こ',[(h*10)+'こ',(h+1)+'こ',(h*100)+'こ'],'100が '+h+'こで '+(h*100)+'。');});
    Q.push(()=>{const th=this.rand(1,9),h=this.rand(1,9),n=th*1000+h*100;return mc('1000が '+th+'こ と 100が '+h+'こ の 数は？',n,[n+1000,th*100+h*10,n+100],'1000が '+th+'こで '+(th*1000)+'。あわせて '+n+'。');});
    Q.push(()=>{const n=this.rand(1001,9998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    }else{
    Q.push(()=>{const m=this.rand(1,9),s=this.rand(1,9),n=m*10000+s*1000;return mc('一万を '+m+'こ、千を '+s+'こ あわせた 数は？',n,[m*1000+s*100,n+1000,n-1000],'一万が '+m+'こで '+(m*10000)+'。あわせて '+n+'。');});
    Q.push(()=>{const n=this.rand(2,9)*10;return mc(n+' を 10ばい した 数は？',n*10,[n,n*100,n+10],n+'×10='+(n*10)+'。');});
    Q.push(()=>{const n=this.rand(2,9)*100;return mc(n+' を 10で わった 数は？',n/10,[n*10,n/100,n],n+'÷10='+(n/10)+'。');});
    Q.push(()=>{const n=this.rand(2,9);return mc(n+' を 100ばい した 数は？',n*100,[n*10,n*1000,n+100],n+'×100='+(n*100)+'。');});
    Q.push(()=>{const n=this.rand(1001,9998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const a=this.rand(1000,9000),b=a+this.rand(10,900);return cmp(a,b);});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickShape(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'shape');const S={maru:'width:120px;height:120px;border-radius:50%;background:#f2a03d;border:4px solid #d18426;',shikaku:'width:110px;height:110px;background:#4f9dde;border:4px solid #3a7db8;',chouhoukei:'width:170px;height:95px;background:#4f9dde;border:4px solid #3a7db8;',sankaku:'width:0;height:0;border-left:70px solid transparent;border-right:70px solid transparent;border-bottom:115px solid #52b788;',seisankaku:'width:0;height:0;border-left:65px solid transparent;border-right:65px solid transparent;border-bottom:113px solid #52b788;',nitohen:'width:0;height:0;border-left:45px solid transparent;border-right:45px solid transparent;border-bottom:125px solid #b788d4;',chokkaku:'width:0;height:0;border-bottom:110px solid #e0708a;border-right:110px solid transparent;'};const sq=(pr,ans,pool,ex,style)=>({topic:'shape',mode:'choices',isShape:!!style,shapeStyle:style||'',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    if(g<=1||stage<=1){
    Q.push(()=>sq('この かたちの なまえは？','まる',['さんかく','しかく','ながしかく'],'まるい かたちは「まる」。',S.maru));
    Q.push(()=>sq('この かたちの なまえは？','さんかく',['まる','しかく','ながしかく'],'かどが 3つ ある かたちは「さんかく」。',S.sankaku));
    Q.push(()=>sq('この かたちの なまえは？','しかく',['まる','さんかく','ほし'],'かどが 4つ ある かたちは「しかく」。',S.shikaku));
    Q.push(()=>sq('さんかくの かどは いくつ？','3つ',['4つ','2つ','5つ'],'さんかくには かどが 3つ あるよ。',S.sankaku));
    }
    if(g>=2){
    Q.push(()=>sq('この かたちの なまえは？','正方形',['長方形','直角三角形','円'],'4つの へんの 長さが みんな 同じ 四角形は 正方形。',S.shikaku));
    Q.push(()=>sq('この かたちの なまえは？','長方形',['正方形','直角三角形','円'],'かどが みんな 直角で、むかいあう へんの 長さが 同じ 四角形は 長方形。',S.chouhoukei));
    Q.push(()=>sq('この かたちの なまえは？','直角三角形',['正三角形','長方形','円'],'直角の かどが ある 三角形は 直角三角形。',S.chokkaku));
    Q.push(()=>sq('三角形の へんの 数は？','3',['4','2','6'],'三角形は 3本の 直線で かこまれた 形。'));
    Q.push(()=>sq('四角形の ちょう点の 数は？','4',['3','2','6'],'四角形には ちょう点が 4つ。'));
    Q.push(()=>sq('はこの形の 面の 数は？','6',['4','8','12'],'はこの形には 面が 6つ。'));
    Q.push(()=>sq('はこの形の ちょう点の 数は？','8',['6','4','12'],'はこの形には ちょう点が 8つ。'));
    Q.push(()=>sq('はこの形の へんの 数は？','12',['6','8','10'],'はこの形には へんが 12本。'));
    }
    if(g>=3){
    Q.push(()=>sq('この 三角形の なまえは？','正三角形',['二等辺三角形','直角三角形','長方形'],'3つの へんが みんな 同じ 長さの 三角形は 正三角形。',S.seisankaku));
    Q.push(()=>sq('この 三角形の なまえは？','二等辺三角形',['正三角形','直角三角形','正方形'],'2つの へんの 長さが 同じ 三角形は 二等辺三角形。',S.nitohen));
    Q.push(()=>{const r=this.rand(2,9);return sq('半径 '+r+'cm の 円の 直径は？',(r*2)+'cm',[r+'cm',(r+2)+'cm',(r*4)+'cm'],'直径は 半径の 2ばい。'+r+'×2='+(r*2)+'cm。',S.maru);});
    Q.push(()=>{const d=this.rand(2,8)*2;return sq('直径 '+d+'cm の 円の 半径は？',(d/2)+'cm',[d+'cm',(d/2+1)+'cm',(d*2)+'cm'],'半径は 直径の 半分。'+d+'÷2='+(d/2)+'cm。',S.maru);});
    Q.push(()=>sq('どこから 見ても 円に 見える 形は？','球',['円','正方形','はこの形'],'ボールのような 形は 球。'));
    Q.push(()=>sq('1つの ちょう点から 出た 2本の 直線が つくる 形を なんと いう？','角',['円','へん','ちょう点'],'2本の 直線の 間に できる 形が 角。'));
    Q.push(()=>sq('紙を きちんと 2回 おって できる かどを なんと いう？','直角',['角','半円','正三角形'],'きちんと 2回 おって できる かどが 直角。三角じょうぎにも あるよ。'));
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickDiv(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'div');if(g>=3&&stage>=3&&Math.random()<0.35){const d=this.rand(2,4),t=this.rand(1,Math.floor(9/d)),o=Math.random()<0.5?0:this.rand(1,Math.floor(9/d)),q1=t*10+o,n=q1*d;return{topic:'div',mode:'choices',op:'div',prompt:n+' ÷ '+d,answer:''+q1,choices:this.pick4(''+q1,[q1+10,Math.max(1,q1-10),q1+d].map(String)),explanation:'わけて 考える。'+(t*10*d)+'÷'+d+'='+(t*10)+(o>0?'、'+(o*d)+'÷'+d+'='+o:'')+'。あわせて '+q1+'。'};}if(stage>=3&&Math.random()<0.5){const d=this.rand(2,9),q0=this.rand(2,9),r=this.rand(1,d-1),n=d*q0+r,ans=q0+' あまり '+r;return{topic:'div',mode:'choices',op:'div',prompt:n+' ÷ '+d,answer:ans,choices:this.pick4(ans,[q0+' あまり '+(r===1?2:r-1),(q0+1)+' あまり '+r,(q0-1)+' あまり '+r]),explanation:d+'×'+q0+'='+(d*q0)+'。'+n+'−'+(d*q0)+'='+r+' だから '+ans+'。'};}const d=this.rand(2,9),q0=this.rand(1,9),n=d*q0;return{topic:'div',mode:'choices',op:'div',prompt:n+' ÷ '+d,answer:''+q0,choices:this.pick4(''+q0,[q0+1,Math.max(1,q0-1),q0+2,q0+3,d,d+1].map(String)),explanation:d+'×'+q0+'='+n+' だから '+n+'÷'+d+'='+q0+'。'};}
  pickFrac(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'frac');const Q=[];
    Q.push(()=>{const n=[2,4][this.rand(0,1)];return{topic:'frac',mode:'choices',isFracViz:true,fd:n,fn:1,prompt:'いろの ついた ところは もとの 大きさの どれだけ？',answer:'1/'+n,choices:this.pick4('1/'+n,['1/'+(n===2?4:2),'1/3',n+'/1']),explanation:'同じ 大きさに '+n+'つに 分けた 1つ分は 1/'+n+'（'+(n===2?'二':'四')+'分の一）。'};});
    if(g>=3){
    Q.push(()=>{const d=this.rand(3,8),k=this.rand(1,d-1);return{topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:k,prompt:'いろの ついた ところは ぜんたいの どれだけ？',answer:k+'/'+d,choices:this.pick4(k+'/'+d,[(d-k)+'/'+d,k+'/'+(d+1),d+'/'+d]),explanation:'ぜんたいを '+d+'つに 分けた '+k+'つ分で '+k+'/'+d+'。'};});
    Q.push(()=>{const d=this.rand(4,9),a=this.rand(1,d-2),b=this.rand(1,d-1-a);return{topic:'frac',mode:'choices',prompt:a+'/'+d+' + '+b+'/'+d+' は？',answer:(a+b)+'/'+d,choices:this.pick4((a+b)+'/'+d,[(a+b)+'/'+(d*2),Math.max(1,a+b-1)+'/'+d,(a+b+1)+'/'+d]),explanation:'1/'+d+' が '+(a+b)+'こ分で '+(a+b)+'/'+d+'。'};});
    Q.push(()=>{const d=this.rand(4,9),s=this.rand(2,d-1),b=this.rand(1,s-1);return{topic:'frac',mode:'choices',prompt:s+'/'+d+' − '+b+'/'+d+' は？',answer:(s-b)+'/'+d,choices:this.pick4((s-b)+'/'+d,[(s-b)+'/'+(d*2),s+'/'+d,(s-b+1)+'/'+d,Math.max(1,s-b-1)+'/'+d,b+'/'+(d*2)]),explanation:'1/'+d+' が '+(s-b)+'こ分で '+(s-b)+'/'+d+'。'};});
    Q.push(()=>{const a=this.rand(1,8),b=this.rand(1,9-a);return{topic:'frac',mode:'choices',prompt:'0.'+a+' + 0.'+b+' は？',answer:(a+b===10?'1':'0.'+(a+b)),choices:this.pick4(a+b===10?'1':'0.'+(a+b),['0.'+Math.max(1,a+b-1),(a+b)+'',(a+b>=9?'0.1':'0.'+(a+b+1))]),explanation:'0.1が '+(a+b)+'こ分で '+(a+b===10?'1':'0.'+(a+b))+'。'};});
    Q.push(()=>{const a=this.rand(2,9),b=this.rand(1,a-1);return{topic:'frac',mode:'choices',prompt:'0.'+a+' − 0.'+b+' は？',answer:'0.'+(a-b),choices:this.pick4('0.'+(a-b),['0.'+(a-b+1),(a-b)+'','0.'+Math.min(9,a-b+2)]),explanation:'0.1が '+(a-b)+'こ分で 0.'+(a-b)+'。'};});
    Q.push(()=>({topic:'frac',mode:'choices',prompt:'1を 10こに 分けた 1こ分を 小数で あらわすと？',answer:'0.1',choices:this.pick4('0.1',['0.01','1.0','10']),explanation:'1の 1/10 は 0.1。'}));
    Q.push(()=>{const k=this.rand(2,9);return{topic:'frac',mode:'choices',prompt:'0.1を '+k+'こ あつめた 数は？',answer:'0.'+k,choices:this.pick4('0.'+k,[''+k,'0.'+(k===9?8:k+1),k+'.0']),explanation:'0.1が '+k+'こで 0.'+k+'。'};});
    Q.push(()=>{const k=this.rand(2,9);return{topic:'frac',mode:'choices',prompt:'0.'+k+' は 0.1を なんこ あつめた 数？',answer:k+'こ',choices:this.pick4(k+'こ',[(k+1)+'こ',(k-1)+'こ',(k*10)+'こ']),explanation:'0.'+k+' は 0.1が '+k+'こ あつまった 数だよ。'};});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickChart(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'chart');const items=this.shuffle([['りんご','#e05a4e','#b8443a'],['みかん','#f2a03d','#d18426'],['ばなな','#d4c22f','#b0a020'],['ぶどう','#9a4fd6','#7a3aad']]).slice(0,3);let counts;do{counts=[this.rand(2,9),this.rand(2,9),this.rand(2,9)];}while(new Set(counts).size<3);const scale=(g>=3&&stage>=3)?2:1,unit=scale>1?'人':'こ';const rows=items.map((it,i)=>({label:it[0],color:it[1],border:it[2],count:counts[i]}));const maxI=counts.indexOf(Math.max.apply(null,counts)),minI=counts.indexOf(Math.min.apply(null,counts));const kind=this.rand(0,2);
    if(kind===0)return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:'いちばん 多いのは どれ？',answer:items[maxI][0],choices:this.shuffle(items.map(x=>x[0])),explanation:items[maxI][0]+' が '+(counts[maxI]*scale)+unit+' で いちばん 多い。'};
    if(kind===1)return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:'いちばん 少ないのは どれ？',answer:items[minI][0],choices:this.shuffle(items.map(x=>x[0])),explanation:items[minI][0]+' が '+(counts[minI]*scale)+unit+' で いちばん 少ない。'};
    const t=this.rand(0,2);return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:(scale>1?'1ますは 2人。':'')+items[t][0]+' は いくつ？',answer:''+(counts[t]*scale),choices:this.pick4(''+(counts[t]*scale),[counts[t]*scale+scale,Math.max(1,counts[t]*scale-scale),counts[t]*scale+2*scale].map(String)),explanation:'ますを 数えると '+counts[t]+'こ。'+(scale>1?'1ます 2人 だから '+(counts[t]*2)+'人。':'')};}
  pickStory(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'story');const items=[['りんご','こ'],['えんぴつ','本'],['シール','まい'],['おはじき','こ']];const it=items[this.rand(0,items.length-1)];const Q=[];
    Q.push(()=>{const a=this.rand(3,9),b=this.rand(2,9);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' もらうと ぜんぶで なん'+it[1]+'？',answer:''+(a+b),explanation:'あわせる ときは たしざん。'+a+'+'+b+'='+(a+b)+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' つかうと のこりは なん'+it[1]+'？',answer:''+(a-b),explanation:'のこりを もとめる ときは ひきざん。'+a+'−'+b+'='+(a-b)+'。'};});
    Q.push(()=>{const a=this.rand(3,9);let b=this.rand(2,9);while(b===a)b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' ふえた。あう しきは？',answer:a+'＋'+b,choices:this.pick4(a+'＋'+b,[a+'−'+b,b+'−'+a,a+'×'+b]),explanation:'ふえる ときは たしざん。しきは '+a+'＋'+b+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' あげた。あう しきは？',answer:a+'−'+b,choices:this.pick4(a+'−'+b,[a+'＋'+b,b+'−'+a,a+'×'+b]),explanation:'へる ときは ひきざん。しきは '+a+'−'+b+'。'};});
    if(g>=2)Q.push(()=>{const a=this.rand(2,9),b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:'1さらに '+it[0]+'が '+a+it[1]+'ずつ、'+b+'さら分。あう しきは？',answer:a+'×'+b,choices:this.pick4(a+'×'+b,[a+'＋'+b,a+'−'+b,a+'÷'+b]),explanation:'同じ数ずつ あるときは かけざん。しきは '+a+'×'+b+'。'};});
    if(g>=3){
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ + '+b+' = '+(ans+b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans+b)+' から '+b+' を ひくと '+ans+'。'};});
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ × '+b+' = '+(ans*b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans*b)+' ÷ '+b+' = '+ans+'。'};});
    Q.push(()=>{const d=this.rand(2,9),q0=this.rand(2,9);return{topic:'story',mode:'num',prompt:(d*q0)+it[1]+'の '+it[0]+'を '+d+'人で 同じ数ずつ 分けると 1人分は なん'+it[1]+'？',answer:''+q0,explanation:'分ける ときは わりざん。'+(d*q0)+'÷'+d+'='+q0+'。'};});
    }
    return Q[this.rand(0,Q.length-1)]();}
""";
    }

    private static string BuildPickKokugoScript()
    {
        return """
pickKokugo(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kokugo');const L=[
  {g:1,k:'山',r:'やま',pre:'',post:' に のぼる',mean:'たかい ところ'},{g:1,k:'川',r:'かわ',pre:'',post:' で あそぶ',mean:'みずが ながれる ところ'},{g:1,k:'花',r:'はな',pre:'あかい ',post:' が さく',mean:'くさきに さく もの'},{g:1,k:'空',r:'そら',pre:'あおい ',post:' を みる',mean:'あたまの うえ'},{g:1,k:'学校',r:'がっこう',pre:'まいにち ',post:' へ いく',mean:'べんきょうする ところ'},{g:1,k:'先生',r:'せんせい',pre:'やさしい ',post:'',mean:'おしえて くれる ひと'},{g:1,k:'雨',r:'あめ',pre:'',post:' が ふる',mean:'そらから おちる みず'},{g:1,k:'水',r:'みず',pre:'',post:' を のむ',mean:'のむ もの'},{g:1,k:'木',r:'き',pre:'大きな ',post:'',mean:'えだや はが ある'},{g:1,k:'犬',r:'いぬ',pre:'',post:' と あるく',mean:'どうぶつ'},{g:1,k:'耳',r:'みみ',pre:'',post:' で きく',mean:'おとを きく ところ'},{g:1,k:'手',r:'て',pre:'',post:' を あげる',mean:'ものを もつ ところ'},{g:1,k:'足',r:'あし',pre:'',post:' で はしる',mean:'あるく ところ'},{g:1,k:'町',r:'まち',pre:'',post:' を あるく',mean:'いえや みせが ある'},{g:1,k:'森',r:'もり',pre:'',post:' に 木が ある',mean:'木が たくさん ある'},{g:1,k:'名まえ',r:'なまえ',pre:'',post:' を 書く',mean:'よびかた'},{g:1,k:'石',r:'いし',pre:'',post:' を ひろう',mean:'かたい つぶ'},{g:1,k:'貝',r:'かい',pre:'うみで ',post:' を ひろう',mean:'うみに いる いきものの から'},{g:1,k:'虫',r:'むし',pre:'',post:' を つかまえる',mean:'ちいさな いきもの'},{g:1,k:'竹',r:'たけ',pre:'',post:' が のびる',mean:'ふしの ある くさき'},{g:1,k:'糸',r:'いと',pre:'',post:' を むすぶ',mean:'ほそながい もの'},{g:1,k:'車',r:'くるま',pre:'',post:' に のる',mean:'タイヤで はしる もの'},{g:1,k:'音',r:'おと',pre:'',post:' が きこえる',mean:'みみに きこえる もの'},{g:1,k:'草',r:'くさ',pre:'',post:' が はえる',mean:'みどりの しょくぶつ'},{g:1,k:'目',r:'め',pre:'',post:' で みる',mean:'ものを みる ところ'},{g:1,k:'口',r:'くち',pre:'',post:' を あける',mean:'たべる ところ'},{g:1,k:'月',r:'つき',pre:'よるの ',post:' が きれい',mean:'よぞらに ひかる もの'},{g:1,k:'立つ',r:'たつ',pre:'せきを ',post:'',mean:'あしで まっすぐに なる'},{g:1,k:'見る',r:'みる',pre:'そとを ',post:'',mean:'目で たしかめる'},{g:1,k:'白い',r:'しろい',pre:'',post:' くも',mean:'ゆきの いろ'},{g:1,k:'大きい',r:'おおきい',pre:'',post:' こえ',mean:'ちいさくない'},
  {g:2,k:'春',r:'はる',pre:'',post:' に 花が さく',mean:'あたたかい きせつ'},{g:2,k:'夏',r:'なつ',pre:'',post:' は あつい',mean:'あつい きせつ'},{g:2,k:'秋',r:'あき',pre:'',post:' に 木のはが かわる',mean:'すずしい きせつ'},{g:2,k:'冬',r:'ふゆ',pre:'',post:' は さむい',mean:'さむい きせつ'},{g:2,k:'朝',r:'あさ',pre:'',post:' に おきる',mean:'一日の はじまり'},{g:2,k:'昼',r:'ひる',pre:'',post:' に ごはんを 食べる',mean:'日中'},{g:2,k:'夜',r:'よる',pre:'',post:' に ねる',mean:'くらい じかん'},{g:2,k:'魚',r:'さかな',pre:'',post:' が およぐ',mean:'みずの 中の いきもの'},{g:2,k:'鳥',r:'とり',pre:'',post:' が とぶ',mean:'はねの ある いきもの'},{g:2,k:'馬',r:'うま',pre:'',post:' が はしる',mean:'どうぶつ'},{g:2,k:'歩く',r:'あるく',pre:'みちを ',post:'',mean:'足で すすむ'},{g:2,k:'走る',r:'はしる',pre:'校ていを ',post:'',mean:'はやく すすむ'},{g:2,k:'近い',r:'ちかい',pre:'家が ',post:'',mean:'きょりが みじかい'},{g:2,k:'遠い',r:'とおい',pre:'駅が ',post:'',mean:'きょりが ながい'},{g:2,k:'高い',r:'たかい',pre:'',post:' 山',mean:'上まで 大きい'},{g:2,k:'新しい',r:'あたらしい',pre:'',post:' 本',mean:'できた ばかり'},{g:2,k:'読む',r:'よむ',pre:'本を ',post:'',mean:'文字を こえに する'},{g:2,k:'書く',r:'かく',pre:'字を ',post:'',mean:'文字を しるす'},{g:2,k:'聞く',r:'きく',pre:'話を ',post:'',mean:'耳で うける'},{g:2,k:'考える',r:'かんがえる',pre:'こたえを ',post:'',mean:'あたまで くらべる'},{g:2,k:'雲',r:'くも',pre:'白い ',post:' が うかぶ',mean:'空に うかぶ もの'},{g:2,k:'風',r:'かぜ',pre:'つよい ',post:' が ふく',mean:'空気の ながれ'},{g:2,k:'星',r:'ほし',pre:'よぞらの ',post:'',mean:'夜に ひかる もの'},{g:2,k:'海',r:'うみ',pre:'',post:' で およぐ',mean:'ひろい しおみず'},{g:2,k:'岩',r:'いわ',pre:'大きな ',post:'',mean:'とても 大きな 石'},{g:2,k:'弟',r:'おとうと',pre:'',post:' と あそぶ',mean:'年下の 男の きょうだい'},{g:2,k:'妹',r:'いもうと',pre:'',post:' と うたう',mean:'年下の 女の きょうだい'},{g:2,k:'兄',r:'あに',pre:'',post:' に ならう',mean:'年上の 男の きょうだい'},{g:2,k:'姉',r:'あね',pre:'',post:' と 出かける',mean:'年上の 女の きょうだい'},{g:2,k:'声',r:'こえ',pre:'大きな ',post:' で うたう',mean:'口から 出る 音'},{g:2,k:'顔',r:'かお',pre:'',post:' を あらう',mean:'目や 口が ある ところ'},{g:2,k:'体',r:'からだ',pre:'',post:' を うごかす',mean:'あたまから 足まで ぜんぶ'},{g:2,k:'教える',r:'おしえる',pre:'かん字を ',post:'',mean:'わかるように つたえる'},{g:2,k:'帰る',r:'かえる',pre:'家に ',post:'',mean:'もとの ばしょに もどる'},{g:2,k:'作る',r:'つくる',pre:'おかしを ',post:'',mean:'ざいりょうから こしらえる'},{g:2,k:'楽しい',r:'たのしい',pre:'',post:' 時間',mean:'うれしい きもち'},{g:2,k:'明るい',r:'あかるい',pre:'',post:' へや',mean:'ひかりが ある'},{g:2,k:'強い',r:'つよい',pre:'',post:' ちから',mean:'よわくない'},
  {g:3,k:'漢字',r:'かんじ',pre:'',post:' を おぼえる',mean:'日本語の 文字'},{g:3,k:'病院',r:'びょういん',pre:'',post:' へ 行く',mean:'びょうきを みる ところ'},{g:3,k:'薬',r:'くすり',pre:'',post:' を のむ',mean:'からだを よくする もの'},{g:3,k:'医者',r:'いしゃ',pre:'',post:' に みてもらう',mean:'びょうきを みる ひと'},{g:3,k:'神社',r:'じんじゃ',pre:'',post:' へ 行く',mean:'おまいりする ところ'},{g:3,k:'研究',r:'けんきゅう',pre:'虫を ',post:' する',mean:'くわしく しらべる'},{g:3,k:'宿題',r:'しゅくだい',pre:'',post:' を する',mean:'家でする べんきょう'},{g:3,k:'運動',r:'うんどう',pre:'',post:' を する',mean:'からだを うごかす'},{g:3,k:'始める',r:'はじめる',pre:'会を ',post:'',mean:'スタートする'},{g:3,k:'終わる',r:'おわる',pre:'じゅぎょうが ',post:'',mean:'おしまいに なる'},{g:3,k:'急ぐ',r:'いそぐ',pre:'駅へ ',post:'',mean:'はやく する'},{g:3,k:'泳ぐ',r:'およぐ',pre:'プールで ',post:'',mean:'水の 中を すすむ'},{g:3,k:'橋',r:'はし',pre:'',post:' を わたる',mean:'川などを こえる もの'},{g:3,k:'湖',r:'みずうみ',pre:'',post:' を 見る',mean:'大きな 水たまり'},{g:3,k:'祭り',r:'まつり',pre:'',post:' に 行く',mean:'みんなで たのしむ 行事'},{g:3,k:'緑',r:'みどり',pre:'',post:' の 葉',mean:'草や 葉の 色'},{g:3,k:'短い',r:'みじかい',pre:'',post:' えんぴつ',mean:'ながくない'},{g:3,k:'深い',r:'ふかい',pre:'',post:' 池',mean:'そこまで とおい'},{g:3,k:'世界',r:'せかい',pre:'',post:' を 知る',mean:'たくさんの 国や 人'},{g:3,k:'写真',r:'しゃしん',pre:'',post:' を とる',mean:'カメラで うつした もの'},{g:3,k:'図書館',r:'としょかん',pre:'',post:' で 本を かりる',mean:'本が たくさん ある ところ'},{g:3,k:'駅',r:'えき',pre:'',post:' で 電車に のる',mean:'電車が とまる ところ'},{g:3,k:'旅行',r:'りょこう',pre:'家族で ',post:' に 行く',mean:'とおくへ 出かける こと'},{g:3,k:'練習',r:'れんしゅう',pre:'サッカーの ',post:'',mean:'くりかえし ならう こと'},{g:3,k:'勉強',r:'べんきょう',pre:'',post:' を がんばる',mean:'学んで 力を つける こと'},{g:3,k:'動物',r:'どうぶつ',pre:'',post:' の せわを する',mean:'生きて うごく もの'},{g:3,k:'昔',r:'むかし',pre:'',post:' の 話を 聞く',mean:'ずっと まえの こと'},{g:3,k:'荷物',r:'にもつ',pre:'おもい ',post:' を はこぶ',mean:'もちはこぶ もの'},{g:3,k:'港',r:'みなと',pre:'',post:' に ふねが つく',mean:'ふねが とまる ところ'},{g:3,k:'島',r:'しま',pre:'小さな ',post:' に わたる',mean:'海に かこまれた りく'},{g:3,k:'坂',r:'さか',pre:'',post:' を のぼる',mean:'かたむいた みち'},{g:3,k:'柱',r:'はしら',pre:'家の ',post:'',mean:'たてものを ささえる もの'},{g:3,k:'洋服',r:'ようふく',pre:'あたらしい ',post:'',mean:'きる もの'},{g:3,k:'悲しい',r:'かなしい',pre:'',post:' 気もち',mean:'なきたく なる 気もち'},{g:3,k:'重い',r:'おもい',pre:'',post:' かばん',mean:'かるくない'},{g:3,k:'速い',r:'はやい',pre:'',post:' 電車',mean:'スピードが ある'}
];const pool=L.filter(x=>x.g<=g);const early=stage<=2?pool.filter(x=>x.g===1):pool;const use=early.length?early:pool;const it=use[this.rand(0,use.length-1)];const choose=stage>=3&&Math.random()<0.5;const others=this.shuffle(use.filter(x=>x.k!==it.k));if(choose){const choices=this.shuffle([it.k].concat(others.slice(0,3).map(x=>x.k)));return{topic:'kokugo',mode:'choices',isKokugo:true,subtype:'kanji-choice',pre:it.pre,word:it.r,post:it.post,mean:'あう かんじを えらぼう',prompt:it.pre+it.r+it.post,answer:it.k,choices:choices,explanation:'「'+it.r+'」は 「'+it.k+'」と 書くよ。いみ：'+it.mean};}const choices=this.shuffle([it.r].concat(others.filter(x=>x.r!==it.r).slice(0,3).map(x=>x.r)));return{topic:'kokugo',mode:'choices',isKokugo:true,subtype:'reading',pre:it.pre,word:it.k,post:it.post,mean:it.mean,prompt:it.pre+it.k+it.post,answer:it.r,choices:choices,explanation:'「'+it.k+'」は 「'+it.r+'」と よむよ。いみ：'+it.mean};}
  pickMoji(p){const stage=this.topicStage(p,'moji');const alphabet=[
    ['A','エー'],['B','ビー'],['C','シー'],['D','ディー'],['E','イー'],['F','エフ'],['G','ジー'],['H','エイチ'],['I','アイ'],['J','ジェイ'],['K','ケー'],['L','エル'],['M','エム'],['N','エヌ'],['O','オー'],['P','ピー'],['Q','キュー'],['R','アール'],['S','エス'],['T','ティー'],['U','ユー'],['V','ブイ'],['W','ダブリュー'],['X','エックス'],['Y','ワイ'],['Z','ゼット']
  ];const hira=[
    ['あ','ア'],['い','イ'],['う','ウ'],['え','エ'],['お','オ'],['か','カ'],['き','キ'],['く','ク'],['け','ケ'],['こ','コ'],['さ','サ'],['し','シ'],['す','ス'],['せ','セ'],['そ','ソ'],['た','タ'],['ち','チ'],['つ','ツ'],['て','テ'],['と','ト'],['な','ナ'],['に','ニ'],['ぬ','ヌ'],['ね','ネ'],['の','ノ'],['は','ハ'],['ひ','ヒ'],['ふ','フ'],['へ','ヘ'],['ほ','ホ'],['ま','マ'],['み','ミ'],['む','ム'],['め','メ'],['も','モ'],['や','ヤ'],['ゆ','ユ'],['よ','ヨ'],['ら','ラ'],['り','リ'],['る','ル'],['れ','レ'],['ろ','ロ'],['わ','ワ'],['を','ヲ'],['ん','ン']
  ];const romaji=[['a','あ'],['i','い'],['u','う'],['e','え'],['o','お'],['ka','か'],['ki','き'],['ku','く'],['ke','け'],['ko','こ'],['sa','さ'],['shi','し'],['su','す'],['se','せ'],['so','そ'],['ta','た'],['chi','ち'],['tsu','つ'],['te','て'],['to','と'],['na','な'],['ni','に'],['ne','ね'],['no','の'],['ha','は'],['hi','ひ'],['fu','ふ'],['ho','ほ'],['ma','ま'],['mi','み'],['mu','む'],['me','め'],['mo','も'],['ya','や'],['yu','ゆ'],['yo','よ'],['ra','ら'],['ri','り'],['ru','る'],['re','れ'],['ro','ろ'],['wa','わ'],['n','ん']];let kinds=stage<=1?['hiragana']:stage===2?['hiragana','katakana','kotoba']:['hiragana','katakana','alphabet','alphabet','kotoba'];const gm=this.effectiveGrade(p);if(gm>=3&&stage>=3)kinds=kinds.concat(['romaji','romaji']);const subtype=kinds[this.rand(0,kinds.length-1)];if(subtype==='kotoba'){const W=[
    ['がっこう','がつこう','がここう','がっこお','べんきょうする ところ'],
    ['きって','きつて','きて','きっで','てがみに はる もの'],
    ['ざっし','ざつし','ざし','ざっしい','しゃしんや きじが のった 本'],
    ['らっぱ','らつぱ','らぱ','らっぷ','ぷーっと ならす がっき'],
    ['でんしゃ','でんしや','でんさや','てんしゃ','せんろを はしる のりもの'],
    ['きんぎょ','きんぎよ','きんきょ','ぎんぎょ','あかい ちいさな さかな'],
    ['おちゃ','おちや','おっちゃ','おちゃあ','きゅうすで いれる のみもの'],
    ['じてんしゃ','じてんしや','じでんしゃ','じてんさ','ペダルを こいで すすむ のりもの'],
    ['おかあさん','おかーさん','おかさん','おがあさん','やさしい かぞく（女の人）'],
    ['おにいさん','おにーさん','おにさん','おねいさん','としうえの 男の きょうだい'],
    ['こおり','こうり','こおうり','こり','つめたい 水の かたまり'],
    ['とけい','とけえ','とっけい','どけい','じかんを しる どうぐ'],
    ['しゃしん','しやしん','さしん','しゃっしん','カメラで とる もの'],
    ['ぎゅうにゅう','ぎゆうにゆう','ぎゅうにゅ','ぎゅーにゅー','うしから とれる のみもの']
  ];const it=W[this.rand(0,W.length-1)];return{topic:'moji',mode:'choices',subtype:'kotoba',prompt:it[4]+'。ただしい かきかたは どれ？',answer:it[0],choices:this.shuffle(it.slice(0,4)),explanation:'ただしくは 「'+it[0]+'」と かくよ。'};}if(subtype==='romaji'){const it=romaji[this.rand(0,romaji.length-1)],others=this.shuffle(romaji.filter(x=>x[1]!==it[1]));return{topic:'moji',mode:'choices',subtype:'romaji',prompt:'ローマ字「'+it[0]+'」は ひらがなで？',answer:it[1],choices:this.shuffle([it[1]].concat(others.slice(0,3).map(x=>x[1]))),explanation:'ローマ字「'+it[0]+'」は 「'+it[1]+'」と よむよ。'};}if(subtype==='alphabet'){const it=alphabet[this.rand(0,alphabet.length-1)],others=this.shuffle(alphabet.filter(x=>x[0]!==it[0]));return{topic:'moji',mode:'choices',subtype:'alphabet',prompt:'アルファベット「'+it[0]+'」の よみは？',answer:it[1],choices:this.shuffle([it[1]].concat(others.slice(0,3).map(x=>x[1]))),explanation:'「'+it[0]+'」は 「'+it[1]+'」と よむよ。'};}const it=hira[this.rand(0,hira.length-1)],others=this.shuffle(hira.filter(x=>x[0]!==it[0]));if(subtype==='katakana'){return{topic:'moji',mode:'choices',subtype:'katakana',prompt:'ひらがな「'+it[0]+'」と おなじ カタカナは？',answer:it[1],choices:this.shuffle([it[1]].concat(others.slice(0,3).map(x=>x[1]))),explanation:'「'+it[0]+'」は カタカナで 「'+it[1]+'」。'};}return{topic:'moji',mode:'choices',subtype:'hiragana',prompt:'カタカナ「'+it[1]+'」と おなじ ひらがなは？',answer:it[0],choices:this.shuffle([it[0]].concat(others.slice(0,3).map(x=>x[0]))),explanation:'「'+it[1]+'」は ひらがなで 「'+it[0]+'」。'};}
  pickBun(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'bun');const mc=(pr,ans,pool,ex)=>({topic:'bun',mode:'choices',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    const J=[['わたし（　）えほんを よむ','は','くっつきの「は」。「わ」と よむけど 「は」と 書くよ。'],['ぼく（　）一ねんせいです','は','くっつきの「は」。「わ」と よむけど 「は」と 書くよ。'],['おかあさん（　）やさしい','は','くっつきの「は」。「わ」と よむけど 「は」と 書くよ。'],['りんご（　）たべる','を','「なにを」の ときは 「を」を つかうよ。'],['みず（　）のむ','を','「なにを」の ときは 「を」を つかうよ。'],['えほん（　）よむ','を','「なにを」の ときは 「を」を つかうよ。'],['ボール（　）なげる','を','「なにを」の ときは 「を」を つかうよ。'],['がっこう（　）いく','へ','いく ばしょには 「へ」。「え」と よむけど 「へ」と 書くよ。'],['こうえん（　）いく','へ','いく ばしょには 「へ」。「え」と よむけど 「へ」と 書くよ。'],['おばあさんの いえ（　）いく','へ','いく ばしょには 「へ」。「え」と よむけど 「へ」と 書くよ。']];
    Q.push(()=>{const it=J[this.rand(0,J.length-1)];const wrong={'は':'わ','を':'お','へ':'え'}[it[1]];return mc(it[0]+'　（　）に はいる じは？',it[1],['は','を','へ'].filter(x=>x!==it[1]).concat([wrong]),it[2]);});
    if(stage>=2||g>=2){
    Q.push(()=>mc('ぶんの おわりに つける 「。」の なまえは？','まる（くてん）',['てん（とうてん）','かぎかっこ','なかてん'],'ぶんの おわりには 「。」（まる）を つけるよ。'));
    Q.push(()=>mc('ぶんの とちゅうの くぎりに つける 「、」の なまえは？','てん（とうてん）',['まる（くてん）','かぎかっこ','はてな'],'ぶんの とちゅうには 「、」（てん）を つけるよ。'));
    Q.push(()=>mc('はなした ことばに つける しるしは？','「　」（かぎかっこ）',['。（まる）','、（てん）','・（なかてん）'],'はなした ことばは 「　」（かぎかっこ）で かこむよ。'));
    Q.push(()=>mc('ただしい ぶんは どれ？','「おはよう。」と いった。',['おはよう。と いった。','「おはよう」。と いった','おはよう と。いった。'],'はなした ことばは 「　」で かこんで、おわりに 「。」を つけるよ。'));
    }
    if(g>=2){
    const K=[['ぱん','パン','がいこくから きた ことば'],['けーき','ケーキ','がいこくから きた ことば'],['ばす','バス','がいこくから きた ことば'],['てれび','テレビ','がいこくから きた ことば'],['じゅーす','ジュース','がいこくから きた ことば'],['ぴあの','ピアノ','がいこくから きた ことば'],['わんわん','ワンワン','どうぶつの なきごえ'],['にゃーにゃー','ニャーニャー','どうぶつの なきごえ'],['がちゃん','ガチャン','ものの 音'],['あめりか','アメリカ','がいこくの 国や 土地の 名前']];
    Q.push(()=>{const it=K[this.rand(0,K.length-1)];return mc('「'+it[0]+'」の ただしい 書きかたは？',it[1],[it[0]],it[2]+'だから カタカナで 「'+it[1]+'」と 書くよ。');});
    Q.push(()=>{const it=K[this.rand(0,K.length-1)];const native=this.shuffle(['やま','かわ','はな','そら','うみ','いし']).slice(0,3);return mc('カタカナで 書く ことばは どれ？',it[0],native,'「'+it[0]+'」は '+it[2]+'だから カタカナで 「'+it[1]+'」と 書くよ。');});
    const SJ=[['犬が','走る','元気に','犬が 元気に 走る。'],['花が','さく','庭で','庭で 花が さく。'],['雨が','ふる','朝から','朝から 雨が ふる。'],['鳥が','とぶ','空を','空を 鳥が とぶ。'],['弟が','わらう','にこにこ','弟が にこにこ わらう。'],['先生が','話す','しずかに','先生が しずかに 話す。'],['ねこが','ねる','まどべで','ねこが まどべで ねる。']];
    Q.push(()=>{const it=SJ[this.rand(0,SJ.length-1)];const isShu=Math.random()<0.5;const ans=isShu?it[0]:it[1];return mc('「'+it[3]+'」の '+(isShu?'しゅご（だれが・なにが）':'じゅつご（どうする）')+'は？',ans,[isShu?it[1]:it[0],it[2]],isShu?'「だれが・なにが」に あたる ことばが しゅご。「'+it[0]+'」だね。':'「どうする」に あたる ことばが じゅつご。「'+it[1]+'」だね。');});
    }
    if(g>=3){
    const SH=[['大きな','犬',['歩く','ゆっくり'],'大きな 犬が ゆっくり 歩く。'],['白い','花',['さいた','庭に'],'庭に 白い 花が さいた。'],['ゆっくり','歩く',['犬が','大きな'],'大きな 犬が ゆっくり 歩く。'],['きれいな','声',['うたう','姉が'],'姉が きれいな 声で うたう。'],['あまい','ケーキ',['食べた','おやつに'],'おやつに あまい ケーキを 食べた。'],['はやく','走る',['うさぎが','野原を'],'うさぎが 野原を はやく 走る。']];
    Q.push(()=>{const it=SH[this.rand(0,SH.length-1)];return mc('「'+it[3]+'」で 「'+it[0]+'」が くわしく して いる ことばは？',it[1],it[2],'「'+it[0]+'」は 「'+it[1]+'」を くわしく する ことば（しゅうしょくご）だよ。');});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickGoi(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'goi');const mc=(pr,ans,pool,ex)=>({topic:'goi',mode:'choices',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    const H=[['大きい','小さい'],['たかい','ひくい'],['ながい','みじかい'],['あつい','さむい'],['はやい','おそい'],['あかるい','くらい'],['おもい','かるい'],['つよい','よわい'],['ひろい','せまい'],['あたらしい','ふるい'],['うえ','した'],['まえ','うしろ'],['みぎ','ひだり'],['あさ','よる'],['いく','くる'],['あける','しめる']];
    Q.push(()=>{const it=H[this.rand(0,H.length-1)];const flip=Math.random()<0.5;const w=flip?it[1]:it[0],ans=flip?it[0]:it[1];const others=this.shuffle(H.filter(x=>x!==it)).slice(0,3).map(x=>x[this.rand(0,1)]);return mc('「'+w+'」の はんたいの ことばは？',ans,others,'「'+w+'」の はんたいは 「'+ans+'」だよ。');});
    const N=[['くだもの',['りんご','みかん','ばなな','ぶどう']],['どうぶつ',['いぬ','ねこ','うま','ぞう']],['のりもの',['バス','でんしゃ','ふね','ひこうき']],['やさい',['にんじん','だいこん','きゅうり','なす']],['いろ',['あか','あお','しろ','きいろ']],['てんき',['はれ','あめ','くもり','ゆき']]];
    Q.push(()=>{const it=N[this.rand(0,N.length-1)];const members=this.shuffle(it[1].slice()).slice(0,3);const others=this.shuffle(N.filter(x=>x!==it)).slice(0,3).map(x=>x[0]);return mc(members.join('・')+' は なんの なかま？',it[0],others,members.join('・')+' は みんな '+it[0]+' の なかまだよ。');});
    Q.push(()=>{const it=N[this.rand(0,N.length-1)];const other=this.shuffle(N.filter(x=>x!==it))[0];const odd=other[1][this.rand(0,other[1].length-1)];const members=this.shuffle(it[1].slice()).slice(0,3);return mc('なかまはずれは どれ？',odd,members,'「'+odd+'」は '+other[0]+'。ほかは みんな '+it[0]+' の なかまだよ。');});
    if(g>=3){
    const KW=[['さるも木から落ちる','じょうずな 人でも しっぱいする ことが ある'],['犬も歩けばぼうに当たる','出歩くと 思いがけない ことに 出会う'],['石の上にも三年','がまんして つづければ うまくいく'],['ねこの手もかりたい','とても いそがしい'],['花よりだんご','見た目より 役に立つ ものが よい'],['口がかるい','ひみつを すぐ 話して しまう'],['頭をひねる','いっしょうけんめい 考える'],['耳にたこができる','同じ 話を なんども 聞かされる'],['馬が合う','気が 合う'],['手をかす','手つだう']];
    Q.push(()=>{const it=KW[this.rand(0,KW.length-1)];const others=this.shuffle(KW.filter(x=>x!==it)).slice(0,3).map(x=>x[1]);return mc('「'+it[0]+'」の いみは？',it[1],others,'「'+it[0]+'」は 「'+it[1]+'」と いう いみだよ。');});
    const JW=['あき','あさ','あめ','いえ','いぬ','いす','うた','うみ','えき','かい','かき','かさ','きた','くも','さかな','しか','すいか','そら','たけ','つき','ねこ','はし','はな','ふね','ほし','みかん','むし','やま','ゆき'];
    Q.push(()=>{const ws=this.shuffle(JW.slice()).slice(0,3);const sorted=ws.slice().sort();const first=Math.random()<0.5;const ans=first?sorted[0]:sorted[2];return{topic:'goi',mode:'choices',prompt:ws.join('・')+' を 国語じてんの じゅんに ならべると、いちばん '+(first?'はじめ':'あと')+'に 出てくるのは？',answer:ans,choices:this.shuffle(ws.slice()),explanation:'五十音じゅんに ならべると '+sorted.join('→')+' だよ。'};});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickDokkai(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'dokkai');const D=[
    {g:1,t:'ねこが にわで ねて います。いぬが その よこで あそんで います。',q:'ねこは どこに いますか？',a:'にわ',c:['いえの 中','こうえん','やねの 上'],e:'「ねこが にわで ねて います」と 書いて あるよ。'},
    {g:1,t:'たろうは あさ パンを たべました。それから がっこうへ いきました。',q:'たろうが たべた ものは？',a:'パン',c:['ごはん','みかん','たまご'],e:'はじめの ぶんに 「パンを たべました」と あるよ。'},
    {g:1,t:'はなこは あかい ぼうしを かぶって、こうえんへ いきました。',q:'ぼうしの いろは？',a:'あか',c:['あお','しろ','きいろ'],e:'「あかい ぼうし」と 書いて あるよ。'},
    {g:1,t:'きのうは あめが ふりました。きょうは はれて います。',q:'きょうの てんきは？',a:'はれ',c:['あめ','ゆき','くもり'],e:'「きょうは はれて います」と あるよ。'},
    {g:1,t:'みかんが 3こ、りんごが 2こ あります。',q:'おおい くだものは どっち？',a:'みかん',c:['りんご'],e:'みかんは 3こ、りんごは 2こ。3こ の ほうが おおいね。'},
    {g:1,t:'ゆうとは いぬと いっしょに かわへ いきました。',q:'ゆうとは どこへ いった？',a:'かわ',c:['うみ','やま','がっこう'],e:'「かわへ いきました」と 書いて あるよ。'},
    {g:2,t:'日曜日、かなは 家ぞくと 海へ 行きました。かなは きれいな 貝を 三つ ひろいました。',q:'かなが ひろった ものは？',a:'貝',c:['石','花','魚'],e:'「貝を 三つ ひろいました」と あるよ。'},
    {g:2,t:'けんたは 朝 早く おきて、犬の さんぽに 行きました。とちゅうで 友だちの ゆきさんに 会いました。',q:'けんたが 会ったのは だれ？',a:'ゆきさん',c:['先生','お母さん','おじいさん'],e:'「友だちの ゆきさんに 会いました」と あるよ。'},
    {g:2,t:'学校の 帰りに 雨が ふって きました。ぼくは かさが なかったので、走って 家に 帰りました。',q:'ぼくが 走った わけは？',a:'かさが なかったから',c:['あそびたかったから','おなかが すいたから','さむかったから'],e:'「かさが なかったので、走って」と あるよ。'},
    {g:2,t:'みほは 花だんに 花の たねを まきました。まい日 水を やると、小さな めが 出ました。',q:'めが 出るまで みほが まい日 した ことは？',a:'水を やった',c:['たねを まいた','花を つんだ','土を ほった'],e:'「まい日 水を やると、めが 出ました」と あるよ。'},
    {g:2,t:'たけしは 本が すきです。としょかんで 毎週 二さつ かりて、ねる 前に 読みます。',q:'たけしが 本を 読むのは いつ？',a:'ねる 前',c:['朝ごはんの 前','学校の 帰り','昼休み'],e:'「ねる 前に 読みます」と あるよ。'},
    {g:3,t:'図書館で 本を かりるには、カードが いります。カードは うけつけで 作って もらえます。',q:'カードを 作れる ばしょは？',a:'うけつけ',c:['学校','ゆうびんきょく','本だな'],e:'「カードは うけつけで 作って もらえます」と あるよ。'},
    {g:3,t:'ひまわりは 夏に さく 花です。せが 高く のびて、太陽の ほうを むいて さきます。',q:'ひまわりが さく きせつは？',a:'夏',c:['春','秋','冬'],e:'「ひまわりは 夏に さく 花です」と あるよ。'},
    {g:3,t:'ペンギンは 鳥の なかまですが、空を とぶ ことは できません。そのかわり、海の 中を じょうずに およぎます。',q:'ペンギンが じょうずに できる ことは？',a:'およぐ こと',c:['とぶ こと','木に のぼる こと','あなを ほる こと'],e:'「海の 中を じょうずに およぎます」と あるよ。'},
    {g:3,t:'あすかは 九時に ねて、六時に おきます。朝ごはんの 前に、なわとびを 五十回 とびます。',q:'あすかが 朝ごはんの 前に する ことは？',a:'なわとび',c:['さんぽ','べんきょう','そうじ'],e:'「朝ごはんの 前に、なわとびを」と あるよ。'},
    {g:3,t:'カブトムシは 夜に なると 木に あつまり、木の しるを なめます。昼の あいだは 土の 中で 休んで います。',q:'カブトムシが 昼に いる ばしょは？',a:'土の 中',c:['木の 上','空','水の 中'],e:'「昼の あいだは 土の 中で 休んで います」と あるよ。'}
  ];const pool=D.filter(x=>x.g<=g);const early=stage<=2?pool.filter(x=>x.g===1):pool;const use=early.length?early:pool;const it=use[this.rand(0,use.length-1)];return{topic:'dokkai',mode:'choices',prompt:it.t+'　◆　'+it.q,answer:it.a,choices:this.pick4(it.a,it.c),explanation:it.e};}
""";
    }

    private static string BuildProgressionScript()
    {
        return """
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  effectiveGrade(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1));const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const top=values.length?Math.max(...values):0.05,stars=Number(p&&p.stars)||0;const byProgress=stars>=150&&top>=0.65?3:(stars>=55&&top>=0.45?2:1);return Math.max(base,byProgress);}
  gradeLabel(p){const base=Math.max(1,Math.min(3,Number(p&&p.grade)||1)),g=this.effectiveGrade(p);return g+'年生'+(g>base?' 範囲':'');}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;return 4;}
  topicStage(p,k){const m=Number((p&&p.mastery&&p.mastery[k])||0.05);if(m<0.25)return 1;if(m<0.45)return 2;if(m<0.65)return 3;return 4;}
  topicComplete(p,k){if(p&&p.cleared&&p.cleared[k])return true;return this.topicStage(p,k)>=4;}
  markCleared(p,k){if(this.topicStage(p,k)>=4)(p.cleared=p.cleared||{})[k]=true;}
  hissanComplete(p){return this.topicComplete(p,'hissan');}
  allowedTopics(p){const all=Object.keys(this.topics);const cfg=this.state.settings;const en=(cfg&&cfg.topics)?all.filter(k=>cfg.topics[k]):all;const enabled=en.length?en:all;const grade=this.effectiveGrade(p),done=k=>this.topicComplete(p,k);const staged=['add'];if(done('add'))staged.push('sub','moji');if(done('sub'))staged.push('kazu','clock','story');if(done('moji'))staged.push('kokugo','bun');if(done('bun'))staged.push('goi');if(done('kokugo'))staged.push('dokkai');if(done('kazu'))staged.push('measure','chart');if(done('measure'))staged.push('shape');if(grade>=2&&done('kazu'))staged.push('hissan');if(grade>=2&&done('hissan'))staged.push('mul');if(grade>=2&&done('mul'))staged.push('frac');if(grade>=3&&done('mul'))staged.push('div');const allowed=staged.filter(k=>enabled.includes(k));return allowed.length?allowed:staged;}
  weightedPick(p){const ks=this.allowedTopics(p);const w=ks.map(k=>{let base=0.25+(1-(Number(p.mastery[k])||0.05))*1.7;if(k==='hissan'&&!this.hissanComplete(p))base*=1.25;if(k==='mul'&&this.topicStage(p,'mul')<=1)base*=0.7;return base;});let s=w.reduce((a,b)=>a+b,0),r=Math.random()*s;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}
""";
    }

    private static string PatchArithmeticVisuals(string markup)
    {
        markup = markup.Replace(
            "let isAddViz=false,addFrames=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));",
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));\n      if(modeChoices&&q.topic==='mul'){isMulViz=true;const a=Number(q.a)||0,b=Number(q.b)||0;for(let g=0;g<b;g++){const cells=[];for(let i=0;i<a;i++)cells.push({style:'width:16px;height:16px;border-radius:50%;background:#1fa39a;border:2px solid #178a82;'});mulGroups.push({cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+Math.min(a,5)+',16px);gap:4px;padding:8px;border-radius:12px;border:3px solid #b8e8e2;background:#e6fbf7;'});}}\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;[['あか','#e05a4e','#b8443a',Number(q.m1)||0],['あお','#4f7edb','#3a5fb0',Number(q.m2)||0]].forEach(r=>{const cells=[];for(let i=0;i<r[3];i++)cells.push({style:'width:24px;height:24px;border-radius:6px;background:'+r[1]+';border:2px solid '+r[2]+';'});measureRows.push({label:r[0],labelStyle:'font-size:22px;font-weight:900;min-width:56px;color:'+r[1]+';',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+r[3]+',24px);gap:4px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});});}\n      if(modeChoices&&q.isChart){isMeasureViz=true;(q.rows||[]).forEach(r=>{const cells=[];for(let i=0;i<r.count;i++)cells.push({style:'width:24px;height:24px;border-radius:6px;background:'+r.color+';border:2px solid '+r.border+';'});measureRows.push({label:r.label,labelStyle:'font-size:20px;font-weight:900;min-width:76px;color:#5b5040;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+r.count+',24px);gap:4px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});});}\n      if(modeChoices&&q.isFracViz){isMeasureViz=true;const fd=Number(q.fd)||2,fn=Number(q.fn)||1,cells=[];for(let i=0;i<fd;i++)cells.push({style:'width:34px;height:34px;'+(i<fn?'background:#d64f8e;border:2px solid #b03a72;':'background:#fff;border:2px dashed #d8c4a0;')});measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+fd+',34px);gap:0px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});}\n      if(modeChoices&&q.isOrder){isMeasureViz=true;const oc=Number(q.oc)||5,op=Number(q.op)||1,cells=[];for(let i=0;i<oc;i++){const idx=q.od==='ひだり'?i+1:oc-i;cells.push({style:'width:36px;height:36px;border-radius:9px;'+(idx===op?'background:#f2a03d;border:3px solid #b07a10;':'background:#cfe3f7;border:2px solid #9db8d4;')});}measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+oc+',36px);gap:6px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});}\n      if(modeChoices&&q.isShape){isShapeViz=true;shapeStyle=q.shapeStyle||'';}\n      const plen=String(q.prompt||'').length;promptStyle='font-size:'+(plen>16?30:(plen>11?40:54))+'px; font-weight:900; text-align:center; margin-bottom:6px; white-space:'+(plen>16?'normal':'nowrap')+'; max-width:880px; line-height:1.35;';",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "if(q.topic==='add'){isAddViz=true;",
            "\n        if(S.numChoices)",
            "isAddViz=q.topic==='add'||q.topic==='sub';if(isAddViz){const isSub=q.topic==='sub',base=isSub?Number(q.a||0):Number(q.n1||0),delta=isSub?Number(q.b||0):Number(q.n2||0),total=isSub?base:base+delta,frames=Math.max(1,Math.ceil(Math.max(base,total)/10));for(let f=0;f<frames;f++){const cells=[];let fill=0;for(let i=0;i<10;i++){const idx=f*10+i;let st='background:#fff;border:2px dashed #d8c4a0;';if(idx<base){fill++;if(isSub&&idx>=base-delta)st='background:linear-gradient(135deg,#ffdad4 0 42%,#d2503f 44% 56%,#ffdad4 58% 100%);border:2px solid #d2503f;';else st='background:#ff8a3d;border:2px solid #e07d2a;';}else if(!isSub&&idx<total){fill++;st='background:#2aa39a;border:2px solid #178a82;';}cells.push({style:'width:26px;height:26px;border-radius:50%;'+st});}addFrames.push({full:fill===10,cells:cells,boxStyle:'display:inline-grid;grid-template-columns:repeat(5,26px);gap:6px;padding:10px;border-radius:14px;'+(fill===10?'border:3px solid #3aa655;background:#eafaef;':'border:3px solid #f0e2c8;background:#fff;')});}}\n        ");

        markup = markup.Replace(
            "<div style=\"font-size:54px; font-weight:900; text-align:center; margin-bottom:6px; white-space:nowrap;\">{{ prompt }}</div>",
            "<div style=\"{{ promptStyle }}\">{{ prompt }}</div>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; justify-content:center; margin:10px 0 12px;\"><div style=\"{{ shapeStyle }}\"></div></div>\n            </sc-if>\n            <sc-if value=\"{{ isMulViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; flex-wrap:wrap; gap:10px; justify-content:center; align-items:center; margin:8px 0 10px; max-width:720px;\">\n                <sc-for list=\"{{ mulGroups }}\" as=\"grp\" hint-placeholder-count=\"6\">\n                  <div style=\"{{ grp.style }}\">\n                    <sc-for list=\"{{ grp.cells }}\" as=\"cell\" hint-placeholder-count=\"8\"><div style=\"{{ cell.style }}\"></div></sc-for>\n                  </div>\n                </sc-for>\n              </div>\n            </sc-if>\n            <sc-if value=\"{{ isMeasureViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; flex-direction:column; gap:10px; align-items:flex-start; margin:8px auto 10px; width:max-content;\">\n                <sc-for list=\"{{ measureRows }}\" as=\"mrow\" hint-placeholder-count=\"2\">\n                  <div style=\"display:flex; align-items:center; gap:10px;\">\n                    <div style=\"{{ mrow.labelStyle }}\">{{ mrow.label }}</div>\n                    <div style=\"{{ mrow.style }}\">\n                      <sc-for list=\"{{ mrow.cells }}\" as=\"mcell\" hint-placeholder-count=\"6\"><div style=\"{{ mcell.style }}\"></div></sc-for>\n                    </div>\n                  </div>\n                </sc-for>\n              </div>\n            </sc-if>",
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
            "isAddViz:isAddViz, addFrames:addFrames, isMulViz:isMulViz, mulGroups:mulGroups, isMeasureViz:isMeasureViz, measureRows:measureRows, isShapeViz:isShapeViz, shapeStyle:shapeStyle, promptStyle:promptStyle,\n      isKokugo:isKokugo",
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
            $"      {{name:{escapedName},grade:1,color:'#4ad991',streak:0,stars:0,xp:0,{BeginnerMasteryMarkup}}},\n" +
            "    ],";

        return html[..start] + replacement + html[end..];
    }
}
