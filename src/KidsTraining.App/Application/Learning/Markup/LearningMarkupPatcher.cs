using System.Globalization;
using System.Text.Json;
using KidsTraining.App.Domain.Learning;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static readonly string BeginnerMasteryObjectMarkup = BuildBeginnerMasteryObjectMarkup();
    private static readonly string BeginnerMasteryMarkup = "mastery:" + BeginnerMasteryObjectMarkup;

    public static string Apply(string markup, string profileName, string parentPassword)
    {
        markup = ReplaceRequired(markup, "screen:'profile', profileIdx:0,", "screen:'start', profileIdx:0,", StringComparison.Ordinal);
        markup = ReplaceRequired(markup,
            "unlockPC(){this.sfx('unlock');this.setState({screen:'profile',session:null,combo:0,pin:'',emergencyDone:false});}",
            "unlockPC(){this.sfx('unlock');this.setState({screen:'start',session:null,combo:0,pin:'',emergencyDone:false});if(window.chrome&&window.chrome.webview&&typeof window.chrome.webview.postMessage==='function')window.chrome.webview.postMessage('kidsTraining.unlock');}",
            StringComparison.Ordinal);
        markup = PatchBeginnerProgression(markup);
        markup = PatchParentPassword(markup, parentPassword);

        return ReplaceBundledProfiles(markup, profileName);
    }

    private static string PatchParentPassword(string markup, string parentPassword)
    {
        var password = ParentPin.FromOrDefault(parentPassword).Value;
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
            "defaultSettings(){return {topics:{add:true,sub:true,hissan:true,mul:true,clock:true,kokugo:true,moji:true,measure:true,kazu:true,shape:true,div:true,frac:true,chart:true,story:true,bun:true,goi:true,dokkai:true,eigo:true,money:true,groups:true,order:true,soroban:true,seikatsu:true,shakai:true,rika:true,kateika:true,gijutsu:true,doutoku:true,jouhou:true,sougou:true,tokubetsu:true,keyboard:true,thinking:true},count:this.props.questionCount??20,pass:this.props.passLine??15,preferSchoolGrade:false};}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "countDelta(d){this.sfx('tap');const s=this.state.settings;const count=this.clamp(s.count+d,4,15);this.setSettings({count:count,pass:Math.min(s.pass,count)});}",
            "countDelta(d){this.sfx('tap');const s=this.state.settings;const count=this.clamp((Number(s.count)||20)+d,10,30);this.setSettings({count:count,pass:Math.min(Math.max(1,Number(s.pass)||15),count)});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "    kokugo:{label:'こくご',color:'#d2691e'},\n  };",
            "    kokugo:{label:'こくご',color:'#d2691e'},\n    moji:{label:'もじ',color:'#4f7edb'},\n    measure:{label:'たんい',color:'#3aa655'},\n    kazu:{label:'かず',color:'#c2891f'},\n    shape:{label:'かたち',color:'#9a4fd6'},\n    div:{label:'わりざん',color:'#0f8fbf'},\n    frac:{label:'ぶんすう',color:'#d64f8e'},\n    chart:{label:'グラフ',color:'#5a8f29'},\n    story:{label:'ぶんしょうだい',color:'#8a6d3b'},\n    bun:{label:'ぶん',color:'#7a5cd6'},\n    goi:{label:'ことば',color:'#2f9e8f'},\n    dokkai:{label:'よみとり',color:'#c2503f'},\n    eigo:{label:'えいご',color:'#2563eb'},\n    money:{label:'おかね',color:'#b7791f'},\n    groups:{label:'おなじかず',color:'#0f8f78'},\n    order:{label:'しきのじゅんじょ',color:'#8b5cf6'},\n    soroban:{label:'そろばん',color:'#8b6f47'},\n    seikatsu:{label:'せいかつ',color:'#2f855a'},\n    shakai:{label:'しゃかい',color:'#9c6b30'},\n    rika:{label:'りか',color:'#16846b'},\n    doutoku:{label:'どうとく',color:'#b05279'},\n    jouhou:{label:'じょうほう',color:'#3366a8'},\n    sougou:{label:'そうごう',color:'#6b5bb5'},\n    tokubetsu:{label:'学校かつどう',color:'#b45f45'},\n    keyboard:{label:'キーボード',color:'#0d9488'},\n    thinking:{label:'かんがえる',color:'#d97706'},\n  };",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "    rika:{label:'りか',color:'#16846b'},",
            "    rika:{label:'りか',color:'#16846b'},\n    kateika:{label:'家庭科',color:'#d97706'},\n    gijutsu:{label:'技術',color:'#4b6b45'},",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "buildCalib(){const order=['add','sub','hissan','mul','kokugo'];return order.map(t=>{const q=this.genFor(t);return{q:q,choices:this.calibChoicesFor(q)};});}",
            "buildCalib(){const core=this.state.setupGrade>=2?['add','sub','hissan','mul','kokugo','moji']:['add','sub','clock','kokugo','moji','kazu'],order=[];for(let round=0;round<3;round++)for(const topic of core)order.push(topic);const p={grade:this.state.setupGrade," + BeginnerMasteryMarkup + "};return order.map(t=>{const q=this.genFor(t,p);return{q:q,choices:this.calibChoicesFor(q)};});}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "calibAnswer(c){",
            "\n\n  genAdd(){",
            BuildCalibrationAnswerScript());

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
            BuildKanjiCurriculumScript() + "\n  " + BuildPickKokugoScript() + "\n  " + BuildCrossCurriculumScript());

        markup = ReplaceRequired(markup,
            "genFor(k){return k==='add'?this.genAdd():k==='sub'?this.genSub():k==='hissan'?this.genHissan():k==='mul'?this.pickMul():k==='clock'?this.pickClock():this.pickKokugo();}",
            "curriculumBankPool(unit,stage){const actual=this.canonicalUnitStage(unit,stage),pool=[];for(const item of unit.questions||[])if((Number(item.stage)||1)===actual)pool.push(item);return pool;}\n  pickCurriculumBank(unit,stage,itemOverride){const pool=this.curriculumBankPool(unit,stage);if(!pool.length)throw new Error('No curriculum questions for '+unit.id);const item=itemOverride||pool[this.rand(0,pool.length-1)],q=this.activityChoice(unit.topicId,item.prompt,item.answer,item.distractors,item.explanation,item.activityPrompt);if(item.display){const authored=[item.answer].concat(item.distractors||[]),presented=[item.display.answer].concat(item.display.choices||[]),displayChoices=q.choices.map(choice=>{const index=authored.findIndex(value=>String(value)===String(choice));return index>=0?presented[index]:null;});q.display={...item.display,choices:displayChoices};}return q;}\n  genFor(k,p,stageOverride,bankItem){const id=this.resolveUnitId(p,k),unit=this.curriculumUnit(id);if(!unit)throw new Error('Unknown curriculum unit '+k);const requested=Number(stageOverride),stage=this.canonicalUnitStage(unit,Number.isFinite(requested)?requested:this.reviewStage(p,id)),sp=this.profileAtStage(p,id,stage),g=unit.generatorKey;const q=g==='curriculum-bank'?this.pickCurriculumBank(unit,stage,bankItem):g==='add'?this.genAdd(sp):g==='sub'?this.genSub(sp):g==='hissan'?this.genHissan(sp):g==='mul'?this.pickMul(sp):g==='clock'?this.pickClock(sp):g==='measure'?this.pickMeasure(sp):g==='kazu'?this.pickKazu(sp):g==='shape'?this.pickShape(sp):g==='div'?this.pickDiv(sp):g==='frac'?this.pickFrac(sp):g==='chart'?this.pickChart(sp):g==='story'?this.pickStory(sp):g==='money'?this.pickMoney(sp):g==='groups'?this.pickGroups(sp):g==='order'?this.pickOrder(sp):g==='soroban'?this.pickSoroban(sp):g==='seikatsu'?this.pickSeikatsu(sp):g==='shakai'?this.pickShakai(sp):g==='rika'?this.pickRika(sp):g==='doutoku'?this.pickDoutoku(sp):g==='jouhou'?this.pickJouhou(sp):g==='sougou'?this.pickSougou(sp):g==='tokubetsu'?this.pickTokubetsu(sp):g==='kokugo'?this.pickKokugo(sp):g==='bun'?this.pickBun(sp):g==='goi'?this.pickGoi(sp):g==='dokkai'?this.pickDokkai(sp):g==='eigo'?this.pickEigo(sp):g==='keyboard'?this.pickKeyboard(sp):this.pickMoji(sp);q.difficulty=stage;q.grade=unit.grade;q.unitGrade=unit.grade;q.unitId=unit.id;q.topic=unit.topicId;q.unitLabel=unit.label;q.assessmentMode=unit.assessmentMode;return q;}",
            StringComparison.Ordinal);
        markup = ReplaceBlock(
            markup,
            "weightedPick(p){",
            "\n  total(){",
            BuildProgressionScript());

        markup = ReplaceRequired(markup,
            "buildSession(p,attempt){const n=this.total(),qs=[];for(let i=0;i<n;i++)qs.push(this.genFor(this.weightedPick(p)));return{questions:qs,idx:0,correct:0,attempt:attempt,startStars:p.stars};}",
            BuildSessionScript(),
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "next(){",
            "\n  retry(){",
            "next(){this.sfx('select');const s=this.state.session;if(s.idx>=s.rolePlan.length-1){const pass=this.sessionPassOutcome(this.curP(),s).pass;if(pass)setTimeout(()=>this.sfx('clear'),280);this.setState({screen:pass?'clear':'retry'});}else{const nextIndex=s.idx+1,p=this.curP(),q=this.generateSessionQuestion(p,s,s.rolePlan[nextIndex]);s.questions[nextIndex]=q;s.idx=nextIndex;this.setState({screen:'quiz',...this.freshQ()});}}");

        markup = ReplaceBlock(
            markup,
            "lvl(p){",
            "\n\n  selectProfile",
            "skillLevel(p){const values=Object.values(p.mastery||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));const avg=this.skillAverage(p),top=values.length?Math.max(...values):0.05,stars=Math.min(Number(p.stars)||0,180);const score=Math.min(1,avg*0.45+top*0.35+stars/320);return Math.max(1,Math.min(5,Math.floor(score*5)));}\n  xpLevel(p){return Math.max(1,Math.floor((Number(p&&p.xp)||0)/100)+1);}\n  lvl(p){return 'レベル '+this.xpLevel(p);}");

        markup = ReplaceRequired(markup,
            "const weakKeys=Object.keys(T).filter(k=>p.mastery[k]<0.5);",
            "const progression=this.progressionView(p);\n    const weakKeys=Object.keys(T).filter(k=>progression.weakTopicIds.has(k));",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "const m=p.mastery[k];const pct=Math.round(m*100);const weak=m<0.5;const status=m>=0.75?'とくい':m>=0.5?'ふつう':'にがて';const bc=m>=0.75?'#3aa655':m>=0.5?'#9fd17a':'#ff8a8a';const sc2=m<0.5?'#d2503f':'#6b5e45';",
            "const m=Number(p.mastery[k])||0.05;const pct=Math.round(m*100);const masteryLevel=this.topicStage(p,k);const weak=masteryLevel<=2;const available=this.allowedTopics(p).includes(k);const achieved=this.topicComplete(p,k),ready=this.topicReady(p,k),due=this.topicDue(p,k);const status=(!available?'対象外':ready?'じりつ':due?'ふくしゅう':achieved?'さいかくにん':'れんしゅう')+(achieved?' ★':'');const levelColors=['#ff8a8a','#f2a03d','#e0c13d','#79b85a','#3aa655'];const bc=available?levelColors[masteryLevel-1]:'#d8d1c4';const sc2=!available?'#9a9388':ready?'#2f7d44':'#6b5e45';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "const available=this.allowedTopics(p).includes(k);",
            "const available=progression.availableTopicIds.has(k),introduced=progression.introducedTopicIds.has(k);",
            StringComparison.Ordinal);
        markup = ReplaceRequired(markup,
            "const status=(!available?'対象外':ready?",
            "const status=(!available?'対象外':!introduced?'これから':ready?",
            StringComparison.Ordinal);
        markup = ReplaceRequired(markup,
            "const bc=available?levelColors[masteryLevel-1]:'#d8d1c4';const sc2=!available?",
            "const bc=introduced?levelColors[masteryLevel-1]:'#d8d1c4';const sc2=!available||!introduced?",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "const masteryLevel=this.topicStage(p,k);",
            "const masteryLevel=this.topicLearningStage(p,k);",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "const achieved=this.topicComplete(p,k),ready=this.topicReady(p,k),due=this.topicDue(p,k);",
            "const achieved=this.topicMastered(p,k),complete=this.topicComplete(p,k),ready=this.topicReady(p,k),due=this.topicDue(p,k),retaining=complete&&!achieved,retentionStep=this.topicStat(p,k).retentionStep||0;",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "const currentLearningUnit=this.curriculumUnit(this.nextCurriculumTopic(p));",
            "const currentLearningUnit=this.curriculumUnit(progression.nextTopic);",
            StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "const status=", "const legacyStatus=", StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "const levelColors=['#ff8a8a','#f2a03d','#e0c13d','#79b85a','#3aa655'];",
            "const status=(!available?'対象外':!introduced?'これから':ready?'定着':due?'復習':retaining?'定着確認 '+retentionStep+'/3':achieved?'再確認':'練習')+(achieved?' ★':'');const levelColors=['#ff8a8a','#f2a03d','#e0c13d','#79b85a','#3aa655','#5470c6'];",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "const gradeOpts=[1,2,3,4,5,6].map(g=>",
            "const gradeOpts=[1,2,3,4,5,6,7,8,9].map(g=>",
            StringComparison.Ordinal);

        markup = PatchEducationalPersistence(markup);
        markup = PatchLearningProgressReset(markup);

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
        markup = PatchLearningAccessibility(markup);
        markup = PatchQuestionFurigana(markup);
        markup = PatchLayoutAndTypography(markup);
        markup = PatchQuestionMetadata(markup);
        markup = PatchKeyboardQuestion(markup);
        markup = PatchWrittenArithmetic(markup);
        markup = PatchFractionalScoring(markup);
        markup = PatchSessionPassGate(markup);
        markup = PatchLearningCheckpoint(markup);
        markup = PatchKanjiPictureQuestions(markup);

        return markup;
    }

    private static string BuildCalibrationAnswerScript()
    {
        return """
calibAnswer(c){const cb=this.state.calib,it=cb.items[cb.idx],ok=String(c)===String(it.q.answer);this.sfx(ok?'correct':'wrong');const results={...cb.results},prior=results[it.q.topic]||{attempts:0,correct:0};results[it.q.topic]={attempts:prior.attempts+1,correct:prior.correct+(ok?1:0)};const ni=cb.idx+1;if(ni>=cb.items.length){const mastery={},skillStats={},topics=Object.keys(this.topics),now=Date.now();for(const t of topics){const r=results[t],score=(!r||!r.attempts)?0.05:(r.correct===3?0.55:r.correct===2?0.35:0.12);mastery[t]=score;skillStats[t]={attempts:r?r.attempts:0,independent:r?r.correct:0,assisted:0,revealed:0,errors:r?r.attempts-r.correct:0,confidence:score,reviewStep:0,lastAttemptAt:r?now:null,nextReviewAt:null,masteredAt:null};}const ps=this.state.profiles.slice(),colors=['#4ad991','#f0883e','#6aa0ff','#d96ad9','#23b5a8'];ps.push({name:this.state.setupName.trim(),grade:this.state.setupGrade,color:colors[ps.length%colors.length],streak:0,stars:0,xp:0,mastery:mastery,skillStats:skillStats,cleared:{}});this.setState({profiles:ps,profileIdx:ps.length-1,screen:'start',calib:null});}else{this.setState({calib:{...cb,idx:ni,results:results}});}}
""";
    }

    private static string BuildSessionScript()
    {
        return """
questionIdentity(q){const omit=new Set(['choices','explanation','display','sessionRole','difficulty']),normalize=v=>{if(Array.isArray(v))return v.map(normalize);if(v&&typeof v==='object'){const out={};for(const k of Object.keys(v).sort())if(!omit.has(k))out[k]=normalize(v[k]);return out;}return v;};return JSON.stringify(normalize(q));}
  questionFingerprint(key){let a=2166136261,b=2246822507;for(let i=0;i<key.length;i++){const c=key.charCodeAt(i);a=Math.imul(a^c,16777619);b=Math.imul(b^c,3266489917);}return(a>>>0).toString(36)+(b>>>0).toString(36);}
  questionCandidateScore(s,recent,key){const count=Number(s.questionCounts[key])||0,index=recent.indexOf(this.questionFingerprint(key));return count*100+(key===s.lastQuestionKey?50:0)+(index<0?0:index+1);}
  rememberQuestion(p,q,key){const id=q.unitId||this.resolveUnitId(p,q.topic),stat=this.topicStat(p,id),fingerprint=this.questionFingerprint(key),recent=(Array.isArray(stat.recentQuestionFingerprints)?stat.recentQuestionFingerprints:[]).filter(item=>item!==fingerprint);recent.push(fingerprint);stat.recentQuestionFingerprints=recent.slice(-12);}
  refreshSessionTarget(p,s){const current=s.activeTargetTopic;if(current&&!this.topicComplete(p,current))return current;const next=this.nextCurriculumTopic(p);if(next&&next!==current)s.targetTopics.push(next);s.activeTargetTopic=next||current;return s.activeTargetTopic;}
  sessionTopic(p,s,role){const target=this.refreshSessionTarget(p,s);if(role==='review'){const due=s.reviewTopics.filter(k=>this.topicDue(p,k));if(due.length){const topic=this.weightedPickExact(p,due),index=s.reviewTopics.indexOf(topic);if(index>=0)s.reviewTopics.splice(index,1);return topic;}}if(role==='target'||role==='exit'||role==='review')return target;const allowed=this.allowedTopics(p),practice=allowed.filter(k=>!this.topicComplete(p,k)||this.topicDue(p,k)),mixed=practice.filter(k=>k!==target),pool=mixed.length?mixed:(practice.length?practice:allowed);return this.weightedPick(p,pool);}
  sessionStage(p,s,topic,role){const base=role==='review'?this.reviewStage(p,topic):this.topicStage(p,topic),id=this.resolveUnitId(p,topic),support=Math.max(0,Math.min(2,Number(s.supportTopics[id])||Number(this.topicStat(p,id).supportDepth)||0));return this.canonicalUnitStage(id,this.clamp(base-support,1,5));}
  registerSessionQuestion(p,s,q,key,role){q.sessionRole=role;s.questionCounts[key]=(s.questionCounts[key]||0)+1;s.lastQuestionKey=key;this.rememberQuestion(p,q,key);return q;}
  generateSessionQuestion(p,s,role){const previous=this._progressionScope,scope={profile:p,values:new Map()};this._progressionScope=scope;try{const topic=this.sessionTopic(p,s,role),stage=this.sessionStage(p,s,topic,role),unit=this.curriculumUnit(this.resolveUnitId(p,topic)),unitStat=this.topicStat(p,unit.id),recent=Array.isArray(unitStat.recentQuestionFingerprints)?unitStat.recentQuestionFingerprints:[],bankItems=unit.generatorKey==='curriculum-bank'?this.curriculumBankPool(unit,stage):null;let best=null,bestKey='',bestScore=Infinity;const consider=candidate=>{const key=this.questionIdentity(candidate),score=this.questionCandidateScore(s,recent,key);if(score<bestScore){best=candidate;bestKey=key;bestScore=score;}return score;};if(bankItems){for(const item of bankItems)if(consider(this.genFor(topic,p,stage,item))===0)break;}else for(let attempt=0;attempt<24;attempt++)if(consider(this.genFor(topic,p,stage))===0)break;if(!best)throw new Error('Unable to generate a learning question');return this.registerSessionQuestion(p,s,best,bestKey,role);}finally{this._progressionScope=previous;}}
  buildSession(p,attempt){this.ensureLearningProfile(p);const n=this.total(),due=this.dueTopics(p),target=this.nextCurriculumTopic(p),reviewCount=due.length?Math.min(due.length,Math.max(1,Math.floor(n*.2))):0,targetTotal=Math.max(4,Math.floor(n*.25)),targetCount=targetTotal-1,mixedCount=n-reviewCount-targetCount-1,rolePlan=[];for(let i=0;i<reviewCount;i++)rolePlan.push('review');for(let i=0;i<targetCount;i++)rolePlan.push('target');for(let i=0;i<mixedCount;i++)rolePlan.push('mixed');rolePlan.push('exit');const session={questions:[],rolePlan:rolePlan,idx:0,correct:0,activeTargetTopic:target,targetTopics:[target],targetAsked:0,targetIndependent:0,reviewTopics:due.slice(),supportTopics:{},questionCounts:{},lastQuestionKey:'',attempt:attempt,startStars:p.stars,startXp:Number(p.xp)||0};session.questions.push(this.generateSessionQuestion(p,session,rolePlan[0]));return session;}
""";
    }

    private static string PatchEducationalPersistence(string markup)
    {
        return ReplaceBlock(
            markup,
            "componentDidMount(){",
            "\n  setSettings(",
            BuildEducationalPersistenceScript());
    }

    private static string BuildEducationalPersistenceScript()
    {
        return $$$"""
componentDidMount(){
    let profiles=this.state.profiles;
    const host=window.__kidsTrainingHost&&typeof window.__kidsTrainingHost==='object'?window.__kidsTrainingHost:{};
    const bundled=Array.isArray(profiles)&&profiles.length?profiles[0]:{};
    const profileName=typeof host.profileName==='string'&&host.profileName.length?host.profileName:String(bundled.name||'');
    const parentPin=typeof host.parentPin==='string'&&/^\d{4}$/.test(host.parentPin)?host.parentPin:this.parentPin();
    const beginnerMastery={{{BeginnerMasteryObjectMarkup}}},masteryKeys=Object.keys(beginnerMastery);
    const numberOrDefault=(value,fallback)=>{const number=Number(value);return Number.isFinite(number)?number:fallback;};
    let storedSettings=null;
    try{const raw=localStorage.getItem('kt_settings_v1');if(raw)storedSettings=JSON.parse(raw);}catch(e){}
    const def=this.defaultSettings(),sourceSettings=storedSettings&&typeof storedSettings==='object'?storedSettings:{};
    const schoolGrade=this.clamp(numberOrDefault(host.schoolGrade,numberOrDefault(sourceSettings.schoolGrade,1)),1,9);
    const preferSchoolGrade=typeof host.preferSchoolGrade==='boolean'?host.preferSchoolGrade:sourceSettings.preferSchoolGrade===true;
    const isDefaultishMastery=mastery=>masteryKeys.every(key=>{const value=Number(mastery&&mastery[key]);return !Number.isFinite(value)||Math.abs(value-.5)<.001||Math.abs(value-beginnerMastery[key])<.001;});
    const hasMeaningfulProgress=profile=>numberOrDefault(profile.stars,0)>0||numberOrDefault(profile.streak,0)>0||numberOrDefault(profile.xp,0)>0||!isDefaultishMastery(profile.mastery);
    const defaultProfile={...bundled,name:profileName,grade:schoolGrade,color:bundled.color||'#4ad991',streak:0,stars:0,xp:0,mastery:{...beginnerMastery}};
    const normalizeProfile=source=>{const profile=source&&typeof source==='object'?source:{},mastery=profile.mastery&&typeof profile.mastery==='object'?profile.mastery:{},resetToBeginner=!hasMeaningfulProgress(profile)&&!profile.progressResetAt;return{...defaultProfile,...profile,name:profileName,grade:schoolGrade,streak:numberOrDefault(profile.streak,defaultProfile.streak),stars:numberOrDefault(profile.stars,defaultProfile.stars),xp:numberOrDefault(profile.xp,defaultProfile.xp),color:profile.color||defaultProfile.color,mastery:resetToBeginner?{...beginnerMastery}:{...defaultProfile.mastery,...mastery}};};
    let savedProfile=bundled;
    try{const raw=localStorage.getItem('kt_profiles_v1');if(raw){const parsed=JSON.parse(raw);savedProfile=Array.isArray(parsed)?(parsed[0]||bundled):(parsed&&typeof parsed==='object'?parsed:bundled);}}catch(e){}
    profiles=this.migrateProfiles([normalizeProfile(savedProfile)]);
    const migrated=JSON.stringify(profiles);this._lastSaved=migrated;
    try{localStorage.setItem('kt_profiles_v1',migrated);localStorage.setItem('kt_parent_pin_v1',parentPin);}catch(e){}
    const storedTopics=sourceSettings.topics&&typeof sourceSettings.topics==='object'?sourceSettings.topics:{};
    const count=this.clamp(numberOrDefault(host.questionCount,numberOrDefault(sourceSettings.count,def.count)),10,30);
    const pass=this.clamp(numberOrDefault(host.passLine,numberOrDefault(sourceSettings.pass,def.pass)),1,count);
    const settings={...def,...sourceSettings,topics:{...def.topics,...storedTopics},count:count,pass:pass,schoolGrade:schoolGrade,preferSchoolGrade:preferSchoolGrade};
    try{localStorage.setItem('kt_settings_v1',JSON.stringify(settings));}catch(e){}
    window.__kidsTrainingApplySchoolGrade=value=>{const grade=Number(value);if(!Number.isInteger(grade)||grade<1||grade>9)return false;const nextProfiles=this.state.profiles.map((profile,index)=>index===0?{...profile,grade:grade}:profile),nextSettings={...this.state.settings,schoolGrade:grade};try{localStorage.setItem('kt_profiles_v1',JSON.stringify(nextProfiles));localStorage.setItem('kt_settings_v1',JSON.stringify(nextSettings));}catch(e){}this.setState({profiles:nextProfiles,settings:nextSettings});return true;};
    let muted=this.state.muted;try{const value=localStorage.getItem('kt_muted_v1');if(value!=null)muted=value==='1';}catch(e){}
    this.setState({profiles:profiles,settings:settings,muted:muted});
}
""";
    }

    private static string BuildBeginnerMasteryObjectMarkup()
    {
        var mastery = LearningDefaults.BeginnerMastery
            .ToString("0.##", CultureInfo.InvariantCulture)
            .TrimStart('0');
        return "{" + string.Join(',', CurriculumPolicy.AllTopics.Select(topic => topic + ":" + mastery)) + "}";
    }


    private static string ReplaceRequired(
        string source,
        string oldValue,
        string newValue,
        StringComparison comparison)
    {
        var index = FindUniqueAnchor(source, oldValue, comparison, "learning markup");
        return source[..index] + newValue + source[(index + oldValue.Length)..];
    }

    private static string ReplaceRequiredOccurrences(
        string source,
        string oldValue,
        string newValue,
        StringComparison comparison,
        int expectedOccurrences)
    {
        if (oldValue.Length == 0)
        {
            throw new ArgumentException("A required learning markup anchor cannot be empty.", nameof(oldValue));
        }

        if (expectedOccurrences <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedOccurrences));
        }

        var count = 0;
        var searchIndex = 0;
        while (searchIndex <= source.Length - oldValue.Length)
        {
            var index = source.IndexOf(oldValue, searchIndex, comparison);
            if (index < 0)
            {
                break;
            }

            count++;
            searchIndex = index + oldValue.Length;
        }

        if (count != expectedOccurrences)
        {
            throw new InvalidOperationException(
                $"Required learning markup anchor must occur exactly {expectedOccurrences} times but occurred {count}: {DescribeAnchor(oldValue)}");
        }

        return source.Replace(oldValue, newValue, comparison);
    }

    private static string ReplaceBlock(string source, string startToken, string endToken, string replacement)
    {
        var start = FindUniqueAnchor(source, startToken, StringComparison.Ordinal, "learning block start");
        var end = FindUniqueAnchor(source, endToken, StringComparison.Ordinal, "learning block end");
        if (end < start + startToken.Length)
        {
            throw new InvalidOperationException(
                $"Required learning block end occurs before its start: {DescribeAnchor(endToken)}");
        }

        return source[..start] + replacement + source[end..];
    }

    private static int FindUniqueAnchor(
        string source,
        string anchor,
        StringComparison comparison,
        string anchorRole)
    {
        if (anchor.Length == 0)
        {
            throw new ArgumentException("A required learning markup anchor cannot be empty.", nameof(anchor));
        }

        var first = source.IndexOf(anchor, comparison);
        if (first < 0)
        {
            throw new InvalidOperationException($"Required {anchorRole} anchor was not found: {DescribeAnchor(anchor)}");
        }

        var duplicate = source.IndexOf(anchor, first + 1, comparison);
        if (duplicate >= 0)
        {
            throw new InvalidOperationException(
                $"Required {anchorRole} anchor must occur exactly once but was duplicated: {DescribeAnchor(anchor)}");
        }

        return first;
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

        var start = FindUniqueAnchor(html, startToken, StringComparison.Ordinal, "profiles block start");
        var end = FindUniqueAnchor(html, endToken, StringComparison.Ordinal, "profiles block end");
        if (end < start + startToken.Length)
        {
            throw new InvalidOperationException(
                $"Required profiles block end occurs before its start: {DescribeAnchor(endToken)}");
        }

        var escapedName = JsonSerializer.Serialize(profileName);
        var replacement =
            "profiles:[\n" +
            $"      {{name:{escapedName},grade:1,color:'#4ad991',streak:0,stars:0,xp:0,{BeginnerMasteryMarkup}}},\n" +
            "    ],";

        return html[..start] + replacement + html[end..];
    }
}
