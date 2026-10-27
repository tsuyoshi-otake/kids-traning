namespace KidsTraining.App.Application.Learning.Markup;

/// <summary>
/// Adds the fixed arithmetic drill mode (とっくんモード) selected from the start screen (issues #62 and #64).
///
/// The adaptive session decides what a learner sees next, which is right for daily study but wrong
/// for the number facts that have to become automatic. The drill is therefore a separate screen with
/// its own fixed course, its own storage key, and no connection to curriculum evidence: drilling can
/// never inflate mastery, stars, or XP.
///
/// A course is asked in stages of 25 questions (issue #117): course card → stage → answer mode →
/// drill. A stage opens once the stage before it has finished a run.
///
/// This patch runs last, so every anchor below is matched against markup that earlier patches have
/// already rewritten. In particular the accessibility patch has already expanded every `onclick=`
/// into a focusable button, so the markup added here spells out `role="button" tabindex="0"` itself.
/// </summary>
internal static partial class LearningMarkupPatcher
{
    private static string PatchDrillMode(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "    muted:false, speakingEnglish:'', setupName:''",
            "    drill:null, drillAsk:'', drillCourseChoice:'', drillStageChoice:0,\n    muted:false, speakingEnglish:'', setupName:''",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "document.addEventListener('keydown',this._mathKeyHandler);",
            "document.addEventListener('keydown',this._mathKeyHandler);" + BuildDrillKeyHandlerScript(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);",
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);" +
            "if(this._drillKeyHandler)document.removeEventListener('keydown',this._drillKeyHandler);" +
            "this.clearDrillEcho();this.stopDrillSpeech();" +
            "if(this.state.drill)this.saveDrillProgress(this.state.drill,false);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "toggleMute(){const m=!this.state.muted;if(m)this.stopEnglishSpeech();",
            "toggleMute(){const m=!this.state.muted;if(m){this.stopEnglishSpeech();this.stopDrillSpeech();}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            "\n" + BuildDrillMethodsScript() + "\n  renderVals(){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n    return {\n      isProfile:",
            "\n" + BuildDrillViewScript() + "\n    return {\n      isProfile:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      profiles:profiles, goCalib:()=>this.goCalib(),",
            "      profiles:profiles, goCalib:()=>this.goCalib(),\n" +
            "      isDrillStage:sc==='drill-stage', drillStageView:drillStageView,\n" +
            "      isDrillMode:sc==='drill-mode', drillModeView:drillModeView,\n" +
            "      isDrill:sc==='drill', drillCards:drillCards, drillView:drillView,",
            StringComparison.Ordinal);

        // The drill courses belong with "きょうの ミッション" / "いまの にがて": they are the same kind of
        // choice, so they share the hero card row (on a line of their own) and the card design instead
        // of owning a separate panel.
        markup = ReplaceRequired(
            markup,
            "        <div style=\"max-width:520px;\">",
            "        <div style=\"max-width:1000px;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "          <div style=\"display:flex; gap:16px; margin-top:22px;\">",
            "          <div class=\"kt-hero-row\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "            <div style=\"flex:1; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px 18px;\">",
            "            <div class=\"kt-hero-card\" style=\"background:#fff; border-color:#f0e2c8;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "              <div style=\"flex:1; background:#ffe6e0; border:4px solid #ff8a8a; border-radius:22px; padding:16px 18px;\">",
            "              <div class=\"kt-hero-card kt-hero-weak\" style=\"background:#ffe6e0; border-color:#ff8a8a;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "            </sc-if>\n          </div>\n        </div>\n      </div>\n      <!-- start button -->",
            "            </sc-if>\n" + BuildDrillEntryTemplate() +
            "\n          </div>\n        </div>\n      </div>\n      <!-- start button -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "  <!-- ============ QUIZ ============ -->",
            BuildDrillStageScreenTemplate() + "\n\n" + BuildDrillModeScreenTemplate() + "\n\n" + BuildDrillScreenTemplate() + "\n\n  <!-- ============ QUIZ ============ -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "</head>",
            BuildDrillStyle() + "\n</head>",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildDrillKeyHandlerScript()
    {
        return """
this._drillKeyHandler=e=>{if(e.repeat||e.isComposing||e.key==='Process'||e.ctrlKey||e.altKey||e.metaKey)return;const sc=this.state.screen;if(sc==='drill-stage'){if(e.key==='Escape'){e.preventDefault();this.cancelDrillStage();return;}const stage=/^[1-9]$/.test(e.key)?Number(e.key)-1:-1;if(stage>=0&&stage<this.drillStageCount(this.drillCourse(String(this.state.drillCourseChoice||'')))){e.preventDefault();this.selectDrillStage(stage);}return;}if(sc==='drill-mode'){const id=String(this.state.drillCourseChoice||'');if(e.key==='Escape'){e.preventDefault();this.cancelDrillMode();return;}if((e.key==='1'||e.key==='2')&&id){e.preventDefault();const kanji=id==='k1'||id==='k2',mode=e.key==='1'?(kanji?'reading':'input'):(kanji?'writing':'choice');this.startDrill(id,false,mode,this.state.drillStageChoice);}return;}if(sc!=='drill')return;const d=this.state.drill;if(!d||d.done)return;const roleButton=!!(e.target&&e.target.getAttribute&&(e.target.getAttribute('role')==='button'||e.target.tagName==='BUTTON'));if(roleButton&&(e.key==='Enter'||e.key===' '))return;if(d.echo){if(e.key==='Enter'||e.key===' '){e.preventDefault();this.drillFlushEcho();}return;}const base=this.drillQuestionAt(d,d.idx),dq=this.drillPresentedQuestion(d,base),choices=this.drillChoices(d,dq);if(choices.length){const choiceIndex=Number(e.key)-1;if(choiceIndex>=0&&choiceIndex<choices.length){e.preventDefault();this.drillChoose(choices[choiceIndex]);}return;}if(/^[0-9]$/.test(e.key)){e.preventDefault();this.drillPress(e.key);return;}if(e.key==='Backspace'||e.key==='Delete'){e.preventDefault();this.del();return;}if(e.key!=='Enter'||!this.state.input)return;e.preventDefault();this.drillSubmit();};document.addEventListener('keydown',this._drillKeyHandler);
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillMethodsScript()
    {
        return """
  drillStorageKey(){return 'kt_drill_v1';}
  drillCourses(){return [
    {id:'g1',grade:1,total:100,badge:'1年生',title:'たしざん・ひきざん',sub:'1＋1から じゅんばんに 100もん',color:'#ff8a3d',edge:'#e07d2a',shade:'#fff3e6'},
    {id:'g2',grade:2,total:100,badge:'2年生',title:'かけざん（九九）',sub:'2のだんから じゅんばんに 100もん',color:'#4a9bf0',edge:'#2f7ccd',shade:'#eaf3ff'},
    // ひっ算は くりあがり・くりさがりの ない コースと ある コースに わける（Issue #95）。
    // h2 は もとの「くりあがり・くりさがりの たしざん・ひきざん」コースの id を ひきつぐ。
    // かいしがめんの カードは せまいので、cardTitle と cardTag に わけて 1ぎょうに おさめる（Issue #98）。
    {id:'h2n',grade:2,total:100,badge:'2年生',title:'ひっ算（くりあがりなし）',cardTitle:'ひっ算',cardTag:'くりあがり なし',sub:'くりあがり・くりさがりの ない たしざん・ひきざんを 100もん',color:'#f2b134',edge:'#a8750a',shade:'#fff8e6'},
    {id:'h2',grade:2,total:100,badge:'2年生',title:'ひっ算（くりあがりあり）',cardTitle:'ひっ算',cardTag:'くりあがり あり',sub:'くりあがり・くりさがりの たしざん・ひきざんを 100もん',color:'#e39b2e',edge:'#c47d16',shade:'#fff4e0'},
    {id:'p2',grade:2,total:100,badge:'2年生',title:'（ ）の ある けいさん',sub:'（ ）の なかを さきに けいさんする しきを 100もん',color:'#e0607e',edge:'#b8395a',shade:'#ffeef2'},
    {id:'d3',grade:3,total:100,badge:'3年生',title:'わりざん',sub:'九九の ぎゃくさんを 100もん',color:'#3cb8a8',edge:'#2a8f83',shade:'#e5f7f4'},
    {id:'k1',grade:1,total:100,badge:'1年生',title:'かんじ・ことば',sub:'1年生の 漢字と くみあわせた ことばを 100もん',color:'#5fbf7a',edge:'#3f9a5b',shade:'#eaf7ee'},
    {id:'k2',grade:2,total:100,badge:'2年生',title:'かんじ・ことば',sub:'1・2年生で ならった 漢字の ことばを 100もん',color:'#b07ae0',edge:'#8d55c4',shade:'#f4ecff'}
  ];}
  drillCourse(id){const list=this.drillCourses();for(let i=0;i<list.length;i++)if(list[i].id===id)return list[i];return null;}
  // A course is asked one stage of 25 questions at a time (issue #117): stage s holds the bank
  // positions [from, to). Stage 1 is always open, and stage s+1 opens once stage s has finished
  // a run, so the run counts double as the unlock record.
  drillStageSize(){return 25;}
  drillStageCount(c){return c?Math.max(1,Math.ceil(c.total/this.drillStageSize())):0;}
  drillStageRange(c,stage){const from=stage*this.drillStageSize();return {from:from,to:Math.min(c.total,from+this.drillStageSize())};}
  drillStageOpen(record,stage){return stage===0||!!(record&&record.stages&&record.stages[stage-1]&&record.stages[stage-1].runs>0);}
  drillDefaultRecord(c){const stages=[];for(let s=0;s<this.drillStageCount(c);s++)stages.push({idx:0,perfect:0,mistakes:0,runs:0,best:0});return {opts:{},stages:stages};}
  readDrillStages(c,row,whole){
    // Each stage keeps its own run: idx is the position inside the stage, runs the finished runs.
    const stages=[];
    if(Array.isArray(row.stages)){
      for(let s=0;s<this.drillStageCount(c);s++){
        const range=this.drillStageRange(c,s),len=range.to-range.from,st=row.stages[s]&&typeof row.stages[s]==='object'?row.stages[s]:{};
        stages.push({idx:this.clamp(whole(st.idx),0,len),perfect:this.clamp(whole(st.perfect),0,len),mistakes:whole(st.mistakes),runs:whole(st.runs),best:this.clamp(whole(st.best),0,len)});
      }
      return stages;
    }
    // A record saved before stages kept one position over the whole course. Every stage that
    // position has passed finished a run, so retirements stamped with the course run keep their
    // meaning; the stage holding the position resumes there. Its first-try count is the part of
    // the saved count the earlier stages cannot hold, so it never claims more than was answered.
    const at=this.clamp(whole(row.idx),0,c.total),runs=whole(row.runs),perfect=whole(row.perfect);
    for(let s=0;s<this.drillStageCount(c);s++){
      const range=this.drillStageRange(c,s),here=at>range.from&&at<range.to,local=here?at-range.from:0;
      stages.push({idx:local,perfect:here?this.clamp(perfect-range.from,0,local):0,mistakes:0,runs:runs+(at>=range.to?1:0),best:0});
    }
    return stages;
  }
  readDrillProgress(){
    let raw=null;try{raw=localStorage.getItem(this.drillStorageKey());}catch(e){raw=null;}
    let data=null;try{data=raw?JSON.parse(raw):null;}catch(e){data=null;}
    const src=data&&typeof data==='object'?data:{},whole=v=>{const n=Math.floor(Number(v));return Number.isFinite(n)&&n>0?n:0;},out={};
    this.drillCourses().forEach(c=>{
      const row=src[c.id]&&typeof src[c.id]==='object'?src[c.id]:{},opts={};
      if(row.opts&&typeof row.opts==='object'&&!Array.isArray(row.opts)){
        for(const key of Object.keys(row.opts)){
          const no=Math.floor(Number(key)),entry=row.opts[key];
          // Each question carries its own option count (2-6) and, at six, its run of first-try
          // answers toward retirement (t, 1-3; see drillBumpOption); anything else is dropped.
          if(!Number.isInteger(no)||no<1||no>c.total)continue;
          if(!entry||typeof entry!=='object'||!Number.isInteger(entry.c)||entry.c<2||entry.c>6)continue;
          // A retired question (t=3) also keeps the run that retired it (r); without one it
          // falls back to two answers so the next first-try answer retires it properly.
          const run=entry.c===6&&Number.isInteger(entry.t)?this.clamp(entry.t,0,3):0,retired=run===3&&Number.isInteger(entry.r)&&entry.r>=0;
          opts[no]=retired?{c:6,t:3,r:entry.r}:run>0?{c:6,t:Math.min(2,run)}:{c:entry.c};
        }
      }
      out[c.id]={opts:opts,stages:this.readDrillStages(c,row,whole)};
    });
    return out;
  }
  writeDrillProgress(map){try{localStorage.setItem(this.drillStorageKey(),JSON.stringify(map));}catch(e){}}
  saveDrillProgress(d,countRun){
    if(!d)return;const course=this.drillCourse(d.id);if(!course)return;
    const progress=this.readDrillProgress(),record=progress[d.id],stage=this.clamp(Math.floor(Number(d.stage))||0,0,record.stages.length-1),range=this.drillStageRange(course,stage),prev=record.stages[stage],finished=d.idx>=range.to;
    record.stages[stage]={idx:finished?0:this.clamp(d.idx-range.from,0,range.to-range.from),perfect:finished?0:d.perfect,mistakes:finished?0:d.mistakes,runs:prev.runs+(countRun?1:0),best:countRun?Math.max(prev.best,d.perfect):prev.best};
    record.opts=d.opts&&typeof d.opts==='object'?d.opts:{};
    this.writeDrillProgress(progress);
  }
  drillBank(id){if(!this._drillBanks)this._drillBanks={};if(!this._drillBanks[id])this._drillBanks[id]=this.buildDrillBank(id);return this._drillBanks[id];}
  drillKanjiEntries(grade){if(!this._drillKanji)this._drillKanji={};if(!this._drillKanji[grade])this._drillKanji[grade]=this.kanjiCurriculumEntries().filter(e=>e.g===grade);return this._drillKanji[grade];}
  drillKanjiWords(grade){
    const rows=grade===1?[
      ['一年','いちねん'],['一日','いちにち'],['一人','ひとり'],['二人','ふたり'],['三人','さんにん'],['四月','しがつ'],['五月','ごがつ'],['六月','ろくがつ'],['七月','しちがつ'],['八月','はちがつ'],['九月','くがつ'],['十月','じゅうがつ'],
      ['上下','じょうげ'],['左右','さゆう'],['大小','だいしょう'],['火山','かざん'],['青空','あおぞら'],['夕日','ゆうひ'],['入口','いりぐち'],['出口','でぐち'],['人口','じんこう'],['学校','がっこう'],['先生','せんせい'],['学年','がくねん'],
      ['正月','しょうがつ'],['文字','もじ'],['天気','てんき'],['名字','みょうじ'],['本名','ほんみょう'],['小学校','しょうがっこう'],['中学校','ちゅうがっこう'],['大学','だいがく'],['男子','だんし'],['女子','じょし'],['王子','おうじ'],['手足','てあし'],['山林','さんりん'],
      ['森林','しんりん'],['水田','すいでん'],['川上','かわかみ'],['川下','かわしも'],['川口','かわぐち'],['小石','こいし'],['大木','たいぼく'],['小川','おがわ'],['青虫','あおむし'],['草木','くさき'],['花火','はなび'],['火花','ひばな'],
      ['竹林','ちくりん'],['糸口','いとぐち'],['目玉','めだま'],['目上','めうえ'],['目下','めした'],['手本','てほん'],['百円','ひゃくえん'],['千円','せんえん'],['休日','きゅうじつ'],['見学','けんがく'],['入学','にゅうがく'],['学力','がくりょく']
    ]:[
      ['公園','こうえん'],['遠足','えんそく'],['夏休み','なつやすみ'],['家出','いえで'],['歌声','うたごえ'],['絵本','えほん'],['外国','がいこく'],['外国人','がいこくじん'],['三角','さんかく'],['四角','しかく'],
      ['音楽','おんがく'],['楽園','らくえん'],['活火山','かっかざん'],['時間','じかん'],['丸太','まるた'],['岩山','いわやま'],['顔色','かおいろ'],['汽車','きしゃ'],['日記','にっき'],['帰国','きこく'],
      ['弓矢','ゆみや'],['牛肉','ぎゅうにく'],['金魚','きんぎょ'],['東京','とうきょう'],['強力','きょうりょく'],['教室','きょうしつ'],['近道','ちかみち'],['兄弟','きょうだい'],['図形','ずけい'],['時計','とけい'],
      ['元気','げんき'],['言語','げんご'],['原文','げんぶん'],['戸口','とぐち'],['古本','ふるほん'],['午前','ごぜん'],['午後','ごご'],['国語','こくご'],['工作','こうさく'],['広場','ひろば']
    ];
    return rows.map(row=>({word:row[0],reading:row[1]}));
  }
  drillFreeOnReadings(){return ['いち','に','さん','ご','ろく','しち','はち','きゅう','じゅう','ひゃく','せん','まん','えん','おう','ほん'];}
  drillStandaloneOnKey(e){return e&&e.k&&e.on?e.k+':'+e.on:'';}
  drillStandaloneOnKeys(){return new Set(['一:いち','二:に','三:さん','五:ご','六:ろく','七:しち','八:はち','九:きゅう','十:じゅう','百:ひゃく','千:せん','万:まん','円:えん','王:おう','本:ほん']);}
  drillStandaloneTargets(entries){
    // These valid but context-bound readings do not teach a useful isolated word to beginners.
    const freeOn=this.drillStandaloneOnKeys(),contextualKun=new Set(['字','千','天']),out=[];
    for(const e of entries||[]){
      if(e.kun&&!contextualKun.has(e.k))out.push({e:e,type:'kun',reading:e.kun,word:e.kunWord||e.k});
      if(freeOn.has(this.drillStandaloneOnKey(e)))out.push({e:e,type:'on',reading:e.on,word:e.k});
    }
    return out;
  }
  buildKanjiDrillBank(id){
    const grade=id==='k1'?1:2,entries=this.drillKanjiEntries(grade),list=[];
    const standalone=this.drillStandaloneTargets(entries);
    const words=this.drillKanjiWords(grade),priority=grade===1?['文字','千円','天気']:[];
    const wordTargets=words.filter(item=>priority.includes(item.word)).sort((a,b)=>priority.indexOf(a.word)-priority.indexOf(b.word)).concat(words.filter(item=>!priority.includes(item.word))).map(item=>({e:null,type:'word',reading:item.reading,word:item.word})),choiceTargets=standalone.concat(wordTargets);
    const uniqueReadings=Array.from(new Set(choiceTargets.map(target=>target.reading)));
    const ask=(sec,target,index)=>{
      const opts=[target.reading],base=Math.max(0,choiceTargets.findIndex(candidate=>candidate===target||(target.e&&candidate.e===target.e&&candidate.type===target.type)));
      [13,29,47].forEach(step=>{let j=(base+step)%choiceTargets.length,guard=0;while(guard<choiceTargets.length&&opts.indexOf(choiceTargets[j].reading)>=0){j=(j+1)%choiceTargets.length;guard++;}if(guard<choiceTargets.length)opts.push(choiceTargets[j].reading);});
      for(let i=0;i<uniqueReadings.length&&opts.length<4;i++)if(opts.indexOf(uniqueReadings[i])<0)opts.push(uniqueReadings[i]);
      const order=opts.slice(1);order.splice(index%4,0,target.reading);const label=target.type==='on'?'\u97f3\u8aad\u307f':target.type==='kun'?'\u8a13\u8aad\u307f':'\u3053\u3068\u3070';
      list.push({no:list.length+1,sec:sec,text:target.word,ans:target.reading,hint:label+'だよ。さいしょの もじは 「'+Array.from(target.reading)[0]+'」だよ。',kind:'pick',choices:order,kanji:target.e?target.e.k:'',kanjiWord:target.type==='word',readingType:target.type});
    };
    const A='\u3042\u305f\u3089\u3057\u3044 \u304b\u3093\u3058',B='\u306a\u3089\u3063\u305f \u304b\u3093\u3058\u306e \u3053\u3068\u3070',C='\u3057\u3042\u3052\u306e \u30df\u30c3\u30af\u30b9';
    const cap=100,wordSlots=Math.min(20,wordTargets.length,cap),ons=standalone.filter(target=>target.type==='on'),kuns=standalone.filter(target=>target.type==='kun'),ordered=ons.concat(kuns),kanjiSlots=Math.min(ordered.length,cap-wordSlots);
    for(let i=0;i<kanjiSlots;i++)ask(A,ordered[i],i);
    const extra=cap-list.length;
    for(let i=0;i<extra;i++)ask(i<wordTargets.length?B:C,wordTargets[i%wordTargets.length],kanjiSlots+i);
    return list;
  }
  buildHissanDrillBank(id){
    const list=[],carry=(a,b)=>{let c=0,i=0;while(a||b||i){const t=a%10+b%10+i;if(t>=10)c++;i=t>=10?1:0;a=Math.floor(a/10);b=Math.floor(b/10);}return c;},
      borrow=(a,b)=>{let c=0,l=0;while(a||b){let d=a%10-l;const t=b%10;l=0;if(d<t){c++;l=1;}a=Math.floor(a/10);b=Math.floor(b/10);}return c;},
      push=(sec,a,op,b,hint)=>list.push({no:list.length+1,sec:sec,text:a+' '+op+' '+b,ans:op==='＋'?a+b:a-b,hint:hint,kind:'num',choices:null}),
      pick=(pool,n)=>{const out=[],seen=new Set();for(let i=0;i<n;i++){const item=pool[Math.min(pool.length-1,Math.floor(i*pool.length/n))];const key=item.join(',');if(seen.has(key))continue;seen.add(key);out.push(item);}for(let i=0;i<pool.length&&out.length<n;i++){const key=pool[i].join(',');if(seen.has(key))continue;seen.add(key);out.push(pool[i]);}return out;},
      dump=(sec,op,pairs,hint)=>{for(const pair of pairs)push(sec,pair[0],op,pair[1],hint);};
    const hA='くらいを そろえて、右の くらいから じゅんに けいさんしよう。',hD='3けたも おなじ。右の くらいから じゅんに けいさんしよう。';
    if(id==='h2n'){
      // くりあがり・くりさがりが どの くらいにも ない 二けた・三けたの ひっ算。
      const A='2けたの たしざん',B='2けたの ひきざん',C='3けたの たしざん',D='3けたの ひきざん';
      const twoAdd=[],twoSub=[],threeAdd=[],threeAddTwo=[],threeSub=[],threeSubTwo=[];
      for(let a=21;a<=87;a++)for(let b=11;b<=99-a;b++)if(carry(a,b)===0)twoAdd.push([a,b]);
      for(let a=32;a<=98;a++)for(let b=11;b<a;b++)if(borrow(a,b)===0)twoSub.push([a,b]);
      for(let a=123;a<=876;a+=3)for(let b=111;b<=876;b+=5)if(a+b<=999&&carry(a,b)===0)threeAdd.push([a,b]);
      for(let a=211;a<=887;a+=7)for(let b=11;b<=88;b+=3)if(carry(a,b)===0)threeAddTwo.push([a,b]);
      for(let a=345;a<=999;a+=3)for(let b=111;b<a;b+=5)if(borrow(a,b)===0)threeSub.push([a,b]);
      for(let a=211;a<=999;a+=7)for(let b=11;b<=99;b+=3)if(borrow(a,b)===0)threeSubTwo.push([a,b]);
      dump(A,'＋',pick(twoAdd,25),hA);dump(B,'−',pick(twoSub,25),hA);
      dump(C,'＋',pick(threeAdd,15),hD);dump(C,'＋',pick(threeAddTwo,10),hD);
      dump(D,'−',pick(threeSub,15),hD);dump(D,'−',pick(threeSubTwo,10),hD);
      return list;
    }
    const B='くりあがりの たしざん',C='くりさがりの ひきざん',F='こたえが 3けたの たしざん',D='3けたの ひっ算',E='0が ある ひっ算';
    const hB='一のくらいが 10を こえたら、十のくらいに 1 くりあげよう。',hC='たりない ときは となりの くらいから 1 かりよう。',hF='十のくらいが 10を こえたら、百のくらいに 1 くりあげよう。',hE='0の くらいは、となりから 1 かりて 10を つくろう。';
    const carryAdd=[],borrowSub=[],hundredAdd=[],threeAdd=[],threeSub=[],hardSub=[[700,286],[500,173],[800,405],[600,258],[900,327],[802,467],[400,236],[601,385],[703,458],[504,267],[300,147],[905,478],[100,8],[100,46],[103,96]];
    for(let a=24;a<=79;a++)for(let b=16;b<=79;b++)if(a+b<=99&&carry(a,b)>=1)carryAdd.push([a,b]);
    for(let a=31;a<=98;a++)for(let b=12;b<a;b++)if(borrow(a,b)>=1)borrowSub.push([a,b]);
    for(let a=38;a<=98;a++)for(let b=24;b<=98;b++)if(a+b>=100&&a+b<=180)hundredAdd.push([a,b]);
    for(let a=123;a<=799;a+=3)for(let b=111;b<=799;b+=5)if(a+b<=999&&carry(a,b)>=1)threeAdd.push([a,b]);
    for(let a=301;a<=999;a+=3)for(let b=111;b<a;b+=5)if(borrow(a,b)>=1)threeSub.push([a,b]);
    dump(B,'＋',pick(carryAdd,20),hB);dump(C,'−',pick(borrowSub,20),hC);
    dump(F,'＋',pick(hundredAdd,15),hF);
    dump(D,'＋',pick(threeAdd,15),hD);dump(D,'−',pick(threeSub,15),hD);
    dump(E,'−',pick(hardSub,15),hE);
    return list;
  }
  buildParenDrillBank(){
    // （ ）の ある しき（2年「計算の きまり」）: （ ）の なかを さきに けいさんする。
    // どの こたえも 100いか、とちゅうの こたえも 0より 大きい。
    const list=[],add=(sec,text,ans,hint)=>list.push({no:list.length+1,sec:sec,text:text,ans:ans,hint:hint,kind:'num',choices:null}),
      front=(sec,a,b,c,hint)=>add(sec,'('+a+' ＋ '+b+') ＋ '+c,a+b+c,hint),back=(sec,a,b,c,hint)=>add(sec,a+' ＋ ('+b+' ＋ '+c+')',a+b+c,hint),
      minus=(sec,a,b,c,hint)=>add(sec,a+' − ('+b+' ＋ '+c+')',a-(b+c),hint);
    const A='（ ）の なかを さきに',B='10を つくって たす',C='ひきざんと （ ）',D='しあげの ミックス';
    const hA='（ ）の なかを さきに けいさんしよう。',hB='（ ）の なかで 10を つくると、けいさんが かんたんに なるよ。',hC='さきに （ ）の なかを たして、その こたえを ひこう。',hD='（ ）の なかを さきに。10の まとまりを さがそう。';
    for(let i=0;i<25;i++){const a=2+(i*3)%8,b=1+(i*5)%9,c=1+(i*7)%9;if(i%2===0)front(A,a,b,c,hA);else back(A,a,b,c,hA);}
    for(let i=0;i<25;i++){const u=1+(i%9);if(i%2===0)back(B,11+(i*7)%59,u,10-u,hB);else front(B,10*(1+(i%6))+u,10-u,3+(i*7)%27,hB);}
    for(let i=0;i<25;i++){const b=i<15?1+(i%9):2+(i*3)%8,c=i<15?10-b:2+(i*5)%8;minus(C,b+c+5+(i*11)%60,b,c,hC);}
    for(let i=0;i<25;i++){
      const k=i%4,u=1+(i*7)%9;
      if(k===0)back(D,21+(i*5)%40,10+u,10-u,hD);
      else if(k===1)front(D,10*(2+(i%5))+u,10-u,11+(i*7)%20,hD);
      else if(k===2)minus(D,60+(i*3)%40,20+u,10-u,hD);
      else front(D,11+(i*5)%30,12+(i*3)%20,13+(i*7)%15,hD);
    }
    return list;
  }
  buildDrillBank(id){
    if(id==='k1'||id==='k2')return this.buildKanjiDrillBank(id);
    if(id==='h2'||id==='h2n')return this.buildHissanDrillBank(id);
    if(id==='p2')return this.buildParenDrillBank();
    const list=[],add=(sec,text,ans,hint,choices)=>{list.push({no:list.length+1,sec:sec,text:text,ans:ans,hint:hint,kind:choices?'pick':'num',choices:choices||null});};
    if(id==='d3'){
      const A='九九で わる',B='□に はいる かず',C='しあげの ミックス';
      [2,5,3,4,6,7,8,9,1].forEach(a=>{for(let b=1;b<=9;b++)add(A,(a*b)+' ÷ '+b,a,b+'ずつ の だん。'+(a*b)+' ÷ '+b+' は '+a+'。');});
      [[56,7,8],[56,8,7],[48,6,8],[54,6,9],[63,7,9],[72,8,9],[49,7,7],[64,8,8],[81,9,9],[36,6,6],[24,6,4],[32,8,4],[27,9,3],[35,7,5],[45,9,5],[16,4,4],[18,6,3],[21,7,3],[28,7,4]].forEach((pr,i)=>{
        const prod=pr[0],div=pr[1],quo=pr[2];
        if(i%2===0)add(B,prod+' ÷ □ ＝ '+quo,div,quo+'のだんを となえて '+prod+'を さがそう。');
        else add(C,prod+' ÷ '+div,quo,div+'のだん。'+prod+' ÷ '+div+' は '+quo+'。');
      });
      return list;
    }
    if(id==='g2'){
      const A='九九を おぼえる',B='しあげの ミックス';
      [2,5,3,4,6,7,8,9,1].forEach(a=>{for(let b=1;b<=9;b++)add(A,a+' × '+b,a*b,a+'のだんを 1から となえて みよう。');});
      [[7,8],[6,7],[8,7],[7,6],[9,7],[7,9],[8,6],[6,8],[9,6],[6,9],[8,8],[7,7],[9,8],[8,9],[4,7],[7,4],[6,4],[4,6],[9,9]].forEach(pr=>add(B,pr[0]+' × '+pr[1],pr[0]*pr[1],pr[0]+'のだんを 1から おもいだそう。'));
      return list;
    }
    const A='たしざんの きほん',B='10の ともだち',C='ひきざんの きほん',D='10から ひく',E='くりあがりの たしざん',F='くりさがりの ひきざん';
    const hA='ゆびを つかって かぞえて みよう。',hB='あと いくつで 10に なるかな。',hC='ゆびを おって かぞえよう。',hD='10ぽんの ゆびから おろして みよう。',hE='さきに 10を つくって、のこりを たそう。',hF='10から ひいて、のこった かずを たそう。';
    for(let s=2;s<=6;s++)for(let a=1;a<s;a++)add(A,a+' ＋ '+(s-a),s,hA);
    for(let a=1;a<=9;a++)add(B,a+' ＋ '+(10-a),10,hB);
    for(let a=9;a>=1;a--)add(B,a+' ＋ '+(10-a),10,hB);
    for(let a=1;a<=9;a++)add(B,a+' ＋ □ ＝ 10',10-a,hB);
    for(let a=2;a<=6;a++)for(let b=1;b<a;b++)add(C,a+' − '+b,a-b,hC);
    for(let b=1;b<=9;b++)add(D,'10 − '+b,10-b,hD);
    for(let b=9;b>=1;b--)add(D,'10 − '+b,10-b,hD);
    for(let b=1;b<=9;b++)add(D,'10 − □ ＝ '+(10-b),b,hD);
    for(let a=9,n=0;a>=2&&n<8;a--)for(let b=11-a;b<=9&&n<8;b++,n++)add(E,a+' ＋ '+b,a+b,hE);
    for(let m=11,n=0;m<=18&&n<8;m++)for(let b=m-9;b<=9&&n<8;b++,n++)add(F,m+' − '+b,m-b,hF);
    return list;
  }
  drillAnswerLine(q){if(q.kind==='pick')return q.text+' → '+q.ans;return q.text.indexOf('□')>=0?q.text.replace('□',String(q.ans)):q.text+' ＝ '+q.ans;}
  drillMatches(q,value){const raw=String(value==null?'':value);if(!raw.length)return false;return q.kind==='pick'?raw===String(q.ans):Number(raw)===Number(q.ans);}
  drillReadingLabel(q){return q&&q.readingType==='on'?'\u97f3\u8aad\u307f':q&&q.readingType==='kun'?'\u8a13\u8aad\u307f':'\u3053\u3068\u3070';}
  drillPrompt(q){return q&&q.readingType?q.text+' ['+this.drillReadingLabel(q)+']':(q?q.text:'');}
  drillPair(d,q){
    if(!q)return null;
    if(q.kind!=='pick')return {main:this.drillAnswerLine(q),sub:''};
    const writing=!!(d&&d.answerMode==='writing');
    return {main:writing?String(q.ans):String(q.text),sub:writing?String(q.text):String(q.ans)};
  }
  // The run asks its stage's positions up to drillEnd(d), then the re-queued "again" questions.
  drillEnd(d){const n=this.drillBank(d.id).length;return Number.isInteger(d.to)?this.clamp(d.to,0,n):n;}
  drillQuestionAt(d,index){
    if(!d)return null;const bank=this.drillBank(d.id),again=Array.isArray(d.again)?d.again:[],end=this.drillEnd(d);
    if(index<end)return bank[index]||null;
    const extra=index-end;
    return extra<again.length?(bank[again[extra]]||null):null;
  }
  drillQuestion(){const d=this.state.drill;return d?this.drillQuestionAt(d,d.idx):null;}
  // course card -> stage -> answer mode -> drill; every back step goes up exactly one screen.
  selectDrillCourse(id){const course=this.drillCourse(id);if(!course)return;this.sfx('select');this.setState({screen:'drill-stage',drillCourseChoice:id,drillStageChoice:0});}
  selectDrillStage(stage){
    const course=this.drillCourse(String(this.state.drillCourseChoice||''));if(!course)return;
    const s=Math.floor(Number(stage));if(!Number.isInteger(s)||s<0||s>=this.drillStageCount(course))return;
    if(!this.drillStageOpen(this.readDrillProgress()[course.id],s)){this.sfx('wrong');return;}
    this.sfx('select');this.setState({screen:'drill-mode',drillStageChoice:s});
  }
  cancelDrillStage(){this.sfx('tap');this.setState({screen:'start',drillCourseChoice:'',drillStageChoice:0});}
  cancelDrillMode(){this.sfx('tap');this.setState({screen:'drill-stage'});}
  drillChoiceOrder(total,seed){
    // Slots cycle through 0..59 (the lcm of every possible option count, 2-6) before the
    // seeded shuffle, so the correct position spreads evenly no matter how many choices a
    // question currently offers.
    const count=Math.max(0,Math.floor(Number(total)||0)),order=[];for(let i=0;i<count;i++)order.push(i%60);
    let state=(Number(seed)>>>0)||0x6d2b79f5,next=()=>{state^=state<<13;state^=state>>>17;state^=state<<5;return state>>>0;};
    for(let i=order.length-1;i>0;i--){const j=next()%(i+1),value=order[i];order[i]=order[j];order[j]=value;}
    return order;
  }
  drillChoicePosition(d,q,count){
    // A run's order covers its own stage, so the index counts from the stage's first position.
    const size=Math.max(1,Math.floor(Number(count))||1),index=Math.max(0,Number(q&&q.no)-1-(Number(d&&d.from)||0)),order=d&&Array.isArray(d.choiceOrder)?d.choiceOrder:[];
    if(order.length)return order[index%order.length]%size;
    return this.drillChoiceOrder(index+1,(Number(q&&q.no)||1)*2654435761)[index]%size;
  }
  drillOptionCount(d,q){
    const opts=d&&typeof d.opts==='object'?d.opts:null,row=opts?opts[Math.floor(Number(q&&q.no))]:null;
    return row&&Number.isInteger(row.c)?this.clamp(row.c,2,6):2;
  }
  drillBumpOption(d,q,correct){
    // One step on a question's own dial: a first-attempt correct answer raises its option
    // count by one (max 6) and a first-attempt miss lowers it (min 2). Since a question
    // only reappears on the next run, each correct answer makes the next run harder.
    // At six the dial cannot rise, so first-attempt correct answers count up in t instead; the
    // third in a row retires the question (issue #104, see drillRetiredIndexes) and records the
    // run that retired it in r. A miss lowers the count and drops the run.
    const no=Math.floor(Number(q&&q.no)),opts=Object.assign({},(d&&d.opts&&typeof d.opts==='object')?d.opts:{});
    if(!Number.isInteger(no)||no<1)return opts;
    const row=opts[no]||{c:2},count=Number.isInteger(row.c)?this.clamp(row.c,2,6):2,run=count===6&&Number.isInteger(row.t)?this.clamp(row.t,0,3):0;
    opts[no]=!correct?{c:Math.max(2,count-1)}:count<6?{c:count+1}:run+1>=3?{c:6,t:3,r:Math.floor(Number(d&&d.run))||0}:{c:6,t:run+1};
    return opts;
  }
  drillRetiredIndexes(opts,runs,from,to){
    // A retired question finishes the run that retired it (r === runs), so a resumed run keeps
    // its length; every later run leaves it out. Bank positions are no-1 because every bank
    // builder numbers its rows list.length+1. A question's r is a run of its own stage, so only
    // the positions [from, to) of that stage are judged against that stage's runs.
    const map=opts&&typeof opts==='object'?opts:{},current=Number(runs)||0,lo=Number(from)||0,hi=to==null?Infinity:Number(to),out=[];
    for(const key of Object.keys(map)){const no=Math.floor(Number(key)),row=map[key];if(Number.isInteger(no)&&no-1>=lo&&no-1<hi&&row&&row.c===6&&row.t>=3&&Number.isInteger(row.r)&&row.r<current)out.push(no-1);}
    return out.sort((a,b)=>a-b);
  }
  drillActiveFrom(skip,from,total){const list=Array.isArray(skip)?skip:[];let i=from;while(i<total&&list.indexOf(i)>=0)i++;return i;}
  drillNumericChoices(d,q){
    if(!d||!q||q.kind!=='num')return [];
    const answer=Number(q.ans);if(!Number.isFinite(answer))return [];
    const count=this.clamp(Math.floor(Number.isFinite(d.count)?d.count:this.drillOptionCount(d,q)),2,6),pool=[],seen=new Set([answer]);
    const push=value=>{if(Number.isInteger(value)&&value>=0&&!seen.has(value)){seen.add(value);pool.push(value);}};
    const fact=/^(\d+) × (\d+)$/.exec(String(q.text));
    if(d.id==='g2'&&fact){const left=Number(fact[1]),right=Number(fact[2]);for(let step=1;step<=8;step++){if(right+step<=9)push(left*(right+step));if(right-step>=1)push(left*(right-step));}}
    if(d.id==='d3')for(let step=1;step<=3;step++){push(answer+step);push(answer-step);}
    for(let step=1;step<=5;step++){push(answer+step);push(answer-step);}
    push(answer+10);push(answer-10);push(answer+20);push(answer-20);
    const options=pool.slice(0,Math.max(0,count-1));
    options.splice(this.drillChoicePosition(d,q,count),0,answer);
    return options;
  }
  drillKanjiWritingChoices(d,q){
    if(!d||!q||(d.id!=='k1'&&d.id!=='k2'))return [];
    const bank=this.drillBank(d.id),answer=String(q.text),reading=String(q.ans),opts=[],base=Math.max(0,Number(q.no)-1),add=candidate=>{const spelling=candidate?String(candidate.text):'';if(spelling&&spelling!==answer&&String(candidate.ans)!==reading&&opts.indexOf(spelling)<0&&!bank.some(row=>String(row.text)===spelling&&String(row.ans)===reading))opts.push(spelling);};
    [13,29,47].forEach(step=>{let i=(base+step)%bank.length,guard=0;while(guard<bank.length&&opts.length<3){add(bank[i]);i=(i+1)%bank.length;guard++;}});
    for(let i=0;i<bank.length&&opts.length<3;i++)add(bank[i]);
    const order=opts.slice(0,3);order.splice(base%4,0,answer);return order;
  }
  drillPresentedQuestion(d,q){
    if(!d||!q||d.answerMode!=='writing')return q;
    const label=this.drillReadingLabel(q),answer=String(q.text);
    return Object.assign({},q,{text:String(q.ans),ans:answer,choices:this.drillKanjiWritingChoices(d,q),hint:label+'だよ。おなじ がくねんで ならう かんじから えらぼう。'});
  }
  drillChoices(d,q){if(!d||!q)return [];if(q.kind==='pick')return Array.isArray(q.choices)?q.choices:[];return d.answerMode==='choice'?this.drillNumericChoices(d,q):[];}
  startDrill(id,restart,answerMode,stage){
    const course=this.drillCourse(id);if(!course)return;
    const progress=this.readDrillProgress(),record=progress[id],s=this.clamp(Math.floor(Number(stage))||0,0,record.stages.length-1);
    // A stage that is not open yet never starts; its button on the stage screen is locked.
    if(!this.drillStageOpen(record,s))return;
    const range=this.drillStageRange(course,s),from=range.from,to=range.to,saved=record.stages[s],bank=this.drillBank(id),skip=this.drillRetiredIndexes(record.opts,saved.runs,from,to);
    let idx=restart?from:from+this.clamp(saved.idx,0,to-from);
    if(this.drillActiveFrom(skip,idx,to)>=to)idx=from;
    const fresh=restart||idx===from;
    idx=this.drillActiveFrom(skip,idx,to);
    // Every question of the stage retired: nothing is left to ask, so the run ends before it starts.
    const allRetired=idx>=to;
    const mode=id==='k1'||id==='k2'?(answerMode==='writing'?'writing':'reading'):(answerMode==='choice'?'choice':'input'),choiceSeed=this.rand(1,2147483646),choiceOrder=this.drillChoiceOrder(to-from,choiceSeed);
    if(fresh){record.stages[s]={idx:0,perfect:0,mistakes:0,runs:saved.runs,best:saved.best};this.writeDrillProgress(progress);}
    this.clearDrillEcho();this.stopDrillSpeech();
    this.sfx('select');
    const opts=Object.assign({},record.opts);
    this.setState({screen:'drill',drillAsk:'',drillCourseChoice:'',input:'',drill:{id:id,stage:s,from:from,to:to,answerMode:mode,choiceSeed:choiceSeed,choiceOrder:choiceOrder,idx:idx,miss:0,copyMiss:0,mark:'',hint:'',revealed:false,streak:0,perfect:fresh?0:saved.perfect,mistakes:fresh?0:saved.mistakes,again:[],counted:false,done:allRetired,allRetired:allRetired,echo:null,opts:opts,run:saved.runs,skip:skip,count:allRetired?2:this.drillOptionCount({opts:opts},bank[idx])}});
  }
  drillAdvance(patch){
    const d=this.state.drill;if(!d)return;
    const end=this.drillEnd(d),next=Object.assign({},d,{miss:0,copyMiss:0,mark:'',hint:'',revealed:false,echo:null},patch||{});
    next.idx=this.drillActiveFrom(d.skip,d.idx+1,end);
    const again=Array.isArray(next.again)?next.again:[],countRun=!d.counted&&next.idx>=end;
    if(countRun)next.counted=true;
    next.done=next.idx>=end+again.length;
    next.count=this.drillOptionCount(next,this.drillQuestionAt(next,next.idx));
    this.saveDrillProgress(next,countRun);
    if(next.done)this.sfx('clear');
    this.setState({drill:next,input:''});
  }
  drillEchoMs(){return 700;}
  drillSpeechMaxMs(){return 4000;}
  clearDrillEcho(){if(this._drillEchoTimer){clearTimeout(this._drillEchoTimer);this._drillEchoTimer=null;}this._drillEchoPatch=null;this._drillEchoToken=(this._drillEchoToken||0)+1;}
  scheduleDrillAdvance(patch,speech){
    // The echo stays up for drillEchoMs and, while the answer is read aloud (issue #105), until
    // the reading ends; drillSpeechMaxMs caps the wait so a stuck engine never holds the drill.
    this.clearDrillEcho();
    this._drillEchoPatch=patch;
    const token=this._drillEchoToken,started=Date.now(),flushIn=ms=>{if(this._drillEchoToken!==token)return;if(this._drillEchoTimer)clearTimeout(this._drillEchoTimer);this._drillEchoTimer=setTimeout(()=>this.drillFlushEcho(),Math.max(0,ms));};
    let ended=false;
    const speaking=this.drillSpeak(speech,()=>{ended=true;flushIn(this.drillEchoMs()-(Date.now()-started));});
    if(!ended)flushIn(speaking?this.drillSpeechMaxMs():this.drillEchoMs());
  }
  drillFlushEcho(){
    const patch=this._drillEchoPatch,cur=this.state.drill;
    this.clearDrillEcho();this.stopDrillSpeech();
    if(!patch||this.state.screen!=='drill'||!cur||!cur.echo)return;
    this.drillAdvance(patch);
  }
  drillSpeechText(d,q){
    // What is read aloud on a correct answer: a kanji question's reading, or a calculation and
    // its answer in words ("6 × 5" -> 6かける5は30). q is the bank row, never the writing-mode view.
    if(!q)return '';
    if(q.kind==='pick')return String(q.ans==null?'':q.ans);
    const answer=String(q.ans),text=String(q.text),line=text.indexOf('□')>=0?text.replace('□',answer):text+' ＝ '+answer;
    const words={'(':'かっこ',')':'かっことじ','＋':'たす','−':'ひく','-':'ひく','×':'かける','÷':'わる','＝':'は'};
    return line.replace(/\)\s*(?=[＋−×÷\-])/g,'かっことじ、').replace(/[()＋−×÷＝\-]/g,ch=>words[ch]).replace(/\s+/g,'');
  }
  drillSpeechEngine(){
    // The Android drill app reads aloud through its TextToSpeech bridge (MainActivity.AndroidBridge);
    // everywhere else the Web Speech API does, when it exists.
    try{if(typeof KtAndroid!=='undefined'&&KtAndroid&&typeof KtAndroid.speak==='function')return 'android';}catch(e){}
    return typeof window!=='undefined'&&window.speechSynthesis&&typeof SpeechSynthesisUtterance!=='undefined'?'web':'';
  }
  drillSpeak(text,done){
    // Returns true when the reading started; done then runs once, when it ends or fails. A newer
    // reading or stopDrillSpeech retires the token, so a stale end never advances the drill.
    this.stopDrillSpeech();
    const value=String(text||''),engine=value&&!this.state.muted?this.drillSpeechEngine():'';
    if(!engine)return false;
    const token=this._drillSpeechToken=(this._drillSpeechToken||0)+1,end=()=>{if(this._drillSpeechToken!==token||!this._drillSpeechActive)return;this._drillSpeechActive=false;if(typeof done==='function')done();};
    this._drillSpeechActive=true;this._drillSpeechFinish=done;
    try{
      if(engine==='android'){
        window.__ktDrillSpeechEnd=id=>{if(String(id)===String(token))end();};
        if(KtAndroid.speak(value,String(token))!==true){this._drillSpeechActive=false;return false;}
        return true;
      }
      const synth=window.speechSynthesis,voices=typeof synth.getVoices==='function'?(synth.getVoices()||[]):[],voice=voices.find(v=>/^ja([-_]|$)/i.test(String(v&&v.lang||'')))||null;
      // Voices that are loaded but none of them Japanese would read the numbers in English.
      if(voices.length&&!voice){this._drillSpeechActive=false;return false;}
      const utterance=new SpeechSynthesisUtterance(value);utterance.lang='ja-JP';if(voice)utterance.voice=voice;utterance.rate=0.95;utterance.onend=end;utterance.onerror=end;
      synth.speak(utterance);
      return true;
    }catch(e){this._drillSpeechActive=false;return false;}
  }
  stopDrillSpeech(){
    // A stopped reading counts as ended, so a pair waiting on it (muting mid-sentence) moves on
    // without the cap; callers that drop the pair clear the echo first, which makes that a no-op.
    if(!this._drillSpeechActive)return;
    const finish=this._drillSpeechFinish;
    this._drillSpeechActive=false;this._drillSpeechToken=(this._drillSpeechToken||0)+1;
    try{if(this.drillSpeechEngine()==='android'){if(typeof KtAndroid.stopSpeaking==='function')KtAndroid.stopSpeaking();}else if(typeof window!=='undefined'&&window.speechSynthesis)window.speechSynthesis.cancel();}catch(e){}
    if(typeof finish==='function')finish();
  }
  drillHissanParts(d,q){return (d&&(d.id==='h2'||d.id==='h2n')&&q)?/^(\d+) (＋|−) (\d+)$/.exec(String(q.text)):null;}
  drillHissanColumns(d,q){
    // The written-arithmetic courses take an answer one digit at a time from the ones place
    // (issue #106), into as many cells as the widest of the two numbers and the answer.
    const parts=d&&d.answerMode==='input'?this.drillHissanParts(d,q):null;
    return parts?Math.max(parts[1].length,parts[3].length,String(q.ans).length):0;
  }
  drillPress(n){
    const d=this.state.drill,columns=d?this.drillHissanColumns(d,this.drillQuestion()):0;
    if(columns&&String(this.state.input||'').length>=columns)return;
    this.press(n);
  }
  drillTypedValue(){
    // Column input arrives ones place first, so the number reads the presses backwards.
    const d=this.state.drill,raw=String(this.state.input||'');
    return d&&this.drillHissanColumns(d,this.drillQuestion())?raw.split('').reverse().join(''):raw;
  }
  drillSubmit(){this.drillAnswerWith(this.drillTypedValue());}
  drillChoose(value){this.drillAnswerWith(value);}
  drillAnswerWith(value){
    const d=this.state.drill;if(!d||d.done||d.echo)return;
    const base=this.drillQuestion(),q=this.drillPresentedQuestion(d,base);if(!q)return;
    const raw=String(value==null?'':value);if(!raw.length)return;
    if(d.revealed){
      // After the second miss the answer stays up until the child enters or picks it (issue
      // #103). Copying it scores nothing and leaves the dial alone; the question still goes to
      // the again queue, exactly as the つぎへ button that this replaces did.
      if(!this.drillMatches(q,raw)){this.sfx('wrong');this.setState({input:'',drill:Object.assign({},d,{copyMiss:(d.copyMiss||0)+1})});return;}
      const again=Array.isArray(d.again)?d.again.slice():[];
      if(d.idx<this.drillEnd(d)&&again.indexOf(d.idx)<0)again.push(d.idx);
      this.sfx('correct');
      this.setState({drill:Object.assign({},d,{mark:'',hint:'',echo:this.drillPair(d,q)})});
      this.scheduleDrillAdvance({again:again,streak:0},this.drillSpeechText(d,base));
      return;
    }
    if(this.drillMatches(q,raw)){
      const clean=d.miss===0,firstPass=d.idx<this.drillEnd(d);
      // Only the first pass of the first attempt moves a question's dial; retries and the
      // re-queued "again" questions leave the record alone, and the on-screen count stays
      // frozen in d.count until the question is replaced.
      const opts=firstPass&&clean&&q.kind==='num'?this.drillBumpOption(d,q,true):d.opts;
      this.sfx(clean&&d.streak>=4?'combo':'correct');
      this.setState({drill:Object.assign({},d,{mark:'',hint:'',opts:opts,echo:this.drillPair(d,q)})});
      this.scheduleDrillAdvance({perfect:d.perfect+(clean&&firstPass?1:0),streak:clean?d.streak+1:0,opts:opts},this.drillSpeechText(d,base));
      return;
    }
    this.sfx('wrong');
    const miss=d.miss+1,revealed=miss>=2,firstMiss=miss===1&&q.kind==='num'&&d.idx<this.drillEnd(d);
    const nextDrill=Object.assign({},d,{miss:miss,mistakes:d.mistakes+1,mark:revealed?'answer':'wrong',hint:q.hint,revealed:revealed,streak:0,opts:firstMiss?this.drillBumpOption(d,q,false):d.opts});
    this.setState({input:'',drill:nextDrill});
    if(firstMiss)this.saveDrillProgress(nextDrill,false);
  }
  exitDrill(){const d=this.state.drill;this.clearDrillEcho();this.stopDrillSpeech();this.saveDrillProgress(d,false);this.sfx('tap');this.setState({screen:'start',drill:null,drillAsk:'',drillCourseChoice:'',input:''});}
  // From the finish screen: back to the course's stages, or on to the answer mode of the next stage.
  leaveDrillTo(screen,stage){const d=this.state.drill;if(!d)return;this.clearDrillEcho();this.stopDrillSpeech();this.saveDrillProgress(d,false);this.sfx('tap');this.setState({screen:screen,drill:null,drillAsk:'',drillCourseChoice:d.id,drillStageChoice:stage,input:''});}
  drillToStages(){const d=this.state.drill;if(d)this.leaveDrillTo('drill-stage',d.stage||0);}
  drillGoNextStage(){const d=this.state.drill;if(d)this.leaveDrillTo('drill-mode',(d.stage||0)+1);}
  askDrillReset(id){this.sfx('tap');this.setState({drillAsk:this.state.drillAsk===id?'':id});}
  drillClearRecord(id,stage){
    // Back to the first question with every dial at two options, which also brings retired
    // questions back: one stage when a stage is given, otherwise the whole course. Run counts
    // stay, so every stage that was open stays open.
    const course=this.drillCourse(id);if(!course)return;
    const progress=this.readDrillProgress(),record=progress[id],only=stage==null?-1:stage;
    record.stages=record.stages.map((st,s)=>only<0||s===only?{idx:0,perfect:0,mistakes:0,runs:st.runs,best:st.best}:st);
    if(only<0)record.opts={};
    else{const range=this.drillStageRange(course,only);for(const key of Object.keys(record.opts)){const no=Number(key);if(no>range.from&&no<=range.to)delete record.opts[key];}}
    this.writeDrillProgress(progress);
  }
  confirmDrillReset(id){this.drillClearRecord(id);this.sfx('select');this.setState({drillAsk:''});}
  relearnDrill(){const d=this.state.drill;if(!d)return;this.drillClearRecord(d.id,d.stage||0);this.startDrill(d.id,true,d.answerMode,d.stage||0);}
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillViewScript()
    {
        return """
    let drillCards=[],drillStageView=null,drillModeView=null,drillView=null;
    // One stage as the next run of it would ask: retired questions (issue #104) leave its count.
    const drillStageState=(c,record,s)=>{
      const st=record.stages[s],range=this.drillStageRange(c,s),skip=this.drillRetiredIndexes(record.opts,st.runs,range.from,range.to);
      const active=Math.max(0,range.to-range.from-skip.length),done=st.idx-skip.filter(i=>i<range.from+st.idx).length;
      return {range:range,active:active,done:done,retired:skip.length,open:this.drillStageOpen(record,s),cleared:st.runs>0,started:st.idx>0};
    };
    if(sc==='start'){
      const drillProgress=this.readDrillProgress(),drillAsk=String(S.drillAsk||'');
      drillCards=this.drillCourses().map(c=>{
        const record=drillProgress[c.id],stages=record.stages.map((st,s)=>drillStageState(c,record,s)),count=stages.length;
        const active=stages.reduce((sum,x)=>sum+x.active,0),retired=stages.reduce((sum,x)=>sum+x.retired,0),cleared=stages.filter(x=>x.cleared).length,moved=stages.some(x=>x.cleared||x.started);
        const fill=stages.reduce((sum,x)=>sum+(x.cleared||!x.active?1:this.clamp(x.done/x.active,0,1)),0);
        const pct=Math.round((count?fill/count:1)*100),asking=drillAsk===c.id;
        return {
          id:c.id, badge:c.badge, cardTitle:c.cardTitle||c.title, cardTag:c.cardTag||'', tagStyle:c.cardTag?'':'display:none;',
          cardStyle:'background:'+c.shade+'; border-color:'+c.edge+'; color:'+c.edge+';',
          barStyle:'width:'+pct+'%; background:'+c.color+';',
          progressText:!active?'ぜんぶ おぼえた':moved?('クリア '+cleared+' / '+count):(count+'ステージ'),
          actionText:moved?'つづきから ▶':'はじめる ▶',
          resetText:asking?'ほんとうに もどす？':'さいしょから',
          resetStyle:asking?'background:#ffe0da; border-color:#e08a7a; color:#b23b23;':'',
          ariaLabel:c.badge+'の とっくんモード、'+c.title+'。'+c.sub+'。'+count+'ステージの うち '+cleared+'ステージ クリア'+(retired?'。おぼえた '+retired+'もんは でない':''),
          resetAria:asking?c.title+'の しんちょくを 0にもどす':c.title+'を さいしょから',
          onStart:()=>this.selectDrillCourse(c.id),
          onReset:()=>{if(this.state.drillAsk===c.id)this.confirmDrillReset(c.id);else this.askDrillReset(c.id);}
        };
      });
    }
    const stageCourse=sc==='drill-stage'?this.drillCourse(String(S.drillCourseChoice||'')):null;
    if(stageCourse){
      const record=this.readDrillProgress()[stageCourse.id];
      drillStageView={
        badge:stageCourse.badge, title:stageCourse.title, headStyle:'background:'+stageCourse.color+';',
        stages:record.stages.map((st,s)=>{
          const x=drillStageState(stageCourse,record,s),name='ステージ '+(s+1);
          const status=!x.open?'🔒 ステージ '+s+'を クリアしてね':!x.active?'ぜんぶ おぼえた':x.started?('つづき '+x.done+' / '+x.active):x.cleared?'クリア ✓':(x.active+'もん');
          const pct=Math.round((x.started&&x.active?this.clamp(x.done/x.active,0,1):x.cleared||!x.active?1:0)*100);
          return {
            no:String(s+1), name:name, range:(x.range.from+1)+'〜'+x.range.to+'もんめ', status:status,
            style:!x.open?'background:#f4f1ea; border-color:#ddd5c4; color:#a39579; box-shadow:none; cursor:default;':x.cleared?'background:#f0faf3; border-color:#9dd3ae;':'border-color:'+stageCourse.edge+';',
            barStyle:'width:'+(x.open?pct:0)+'%; background:'+stageCourse.color+';',
            ariaDisabled:x.open?'false':'true',
            ariaLabel:(s+1)+'ばん、'+name+'、'+(x.range.from+1)+'もんめから'+x.range.to+'もんめ。'+status.replace('🔒 ',''),
            onClick:()=>this.selectDrillStage(s)
          };
        }),
        onBack:()=>this.cancelDrillStage()
      };
    }
    if(sc==='drill-mode'){
      const modeCourse=this.drillCourse(String(S.drillCourseChoice||''))||{};
      const kanjiMode=modeCourse.id==='k1'||modeCourse.id==='k2',modeStage=Math.floor(Number(S.drillStageChoice))||0,modeRange=modeCourse.total?this.drillStageRange(modeCourse,modeStage):{from:0,to:0};
      drillModeView={
        badge:modeCourse.badge||'',title:modeCourse.title||'',headStyle:'background:'+(modeCourse.color||'#ff8a3d')+';',
        stageText:'ステージ '+(modeStage+1)+'（'+(modeRange.from+1)+'〜'+modeRange.to+'もんめ）',
        firstAria:kanjiMode?'1ばん、読みを選ぶ':'1ばん、数字を入力',firstSymbol:kanjiMode?'漢 → かん':'123',firstName:kanjiMode?'よみを えらぶ':'すうじを 入力',firstHelp:kanjiMode?'かんじの よみかたを 4つから':'じぶんで こたえを いれる',
        secondAria:kanjiMode?'2ばん、漢字を選ぶ':'2ばん、こたえをえらぶ',secondSymbol:kanjiMode?'かん → 漢':'A / B',secondName:kanjiMode?'かんじを えらぶ':'えらんで こたえる',secondHelp:kanjiMode?'ただしい かきかたを 4つから':'ただしい こたえを えらぶ',
        onFirst:()=>this.startDrill(modeCourse.id,false,kanjiMode?'reading':'input',modeStage),onSecond:()=>this.startDrill(modeCourse.id,false,kanjiMode?'writing':'choice',modeStage),onBack:()=>this.cancelDrillMode()
      };
    }
    if(sc==='drill'&&S.drill){
      const d=S.drill,course=this.drillCourse(d.id)||{},from=Number(d.from)||0,end=this.drillEnd(d),stageNo=(Number(d.stage)||0)+1,baseQuestion=this.drillQuestionAt(d,d.idx),dq=this.drillPresentedQuestion(d,baseQuestion);
      const dAgain=Array.isArray(d.again)?d.again:[],dSkip=Array.isArray(d.skip)?d.skip:[],passed=Math.min(d.idx,end),inMain=d.idx<end;
      const activeTotal=Math.max(0,end-from-dSkip.length),seen=passed-from-dSkip.filter(i=>i<passed).length,retiredNow=this.drillRetiredIndexes(d.opts,Infinity,from,end).length;
      // The next stage opens with the first finished run of this one; the finish screen says so once.
      const nextStage=!!d.done&&!!course.total&&stageNo<this.drillStageCount(course)&&this.drillStageOpen(this.readDrillProgress()[d.id],stageNo);
      const unlockedNow=nextStage&&!d.allRetired&&!!d.counted&&(Number(d.run)||0)===0;
      const drillPad=['1','2','3','4','5','6','7','8','9'].map(n=>({label:n,ariaLabel:n,style:keyTile,onClick:()=>this.drillPress(n)}));
      drillPad.push({label:'けす',ariaLabel:'ひとつ けす',style:keyClear,onClick:()=>this.del()});
      drillPad.push({label:'0',ariaLabel:'0',style:keyTile,onClick:()=>this.drillPress('0')});
      drillPad.push({label:'OK',ariaLabel:'こたえる',style:keyOk,onClick:()=>this.drillSubmit()});
      const dChoices=this.drillChoices(d,dq),dPick=dChoices.length>0,dKanji=!!(dq&&dq.kind==='pick'),dWriting=d.answerMode==='writing';
      const hissan=this.drillHissanParts(d,dq);
      // The pair is what has to be memorised, so it is shown in one fixed place: green while the
      // correct answer is echoed, red when the second mistake reveals it, and never anywhere else.
      const dEcho=d.echo&&typeof d.echo==='object'?d.echo:null,dPair=this.drillPair(d,dq)||{main:'',sub:''};
      const dSolved=!!dEcho||!!d.revealed,dAnswer=dq?String(dq.ans):'';
      const drillPicks=dChoices.map((choice,i)=>({
        no:String(i+1), label:String(choice), ariaLabel:(i+1)+'ばん、'+choice,
        style:dSolved&&String(choice)===dAnswer?'background:#e8f7ec; border-color:#3aa655; color:#22683c; box-shadow:0 4px 0 #9dd3ae;':'',
        onClick:()=>this.drillChoose(choice)}));
      // Column cells, left to right: cell i holds the digit of place columns-1-i, and presses
      // fill them from the ones place, so the next empty place is the one to highlight. The
      // right/wrong colours go on the cells themselves, never on the wider column they sit in.
      const hissanColumns=this.drillHissanColumns(d,dq),typed=String(S.input||''),hissanCells=[];
      for(let i=0;i<hissanColumns;i++){const place=hissanColumns-1-i;hissanCells.push({text:place<typed.length?typed[place]:'',style:dEcho?'background:#e8f7ec; box-shadow:inset 0 0 0 3px #3aa655; color:#22683c;':place===typed.length?'box-shadow:inset 0 0 0 5px #ff8a3d; background:#fff0de;':(d.mark?'box-shadow:inset 0 0 0 3px #e08a7a; color:#b23b23;':'')});}
      const copyAsk=dPick?'おなじ こたえを えらんで すすもう':hissanColumns?'おなじ こたえを みぎの くらいから いれよう':'おなじ こたえを いれて すすもう';
      drillView={
        badge:course.badge||'', title:course.title||'', stageText:'ステージ '+stageNo, section:dq?dq.sec:'', sectionStyle:dq&&dq.sec?'':'display:none;',
        countText:d.done?(d.allRetired?'':activeTotal+' / '+activeTotal):inMain?(seen+' / '+activeTotal):('なおし '+(d.idx-end+1)+' / '+dAgain.length),
        headStyle:'background:'+(course.color||'#ff8a3d')+';',
        barStyle:'width:'+Math.round((activeTotal?this.clamp(seen/activeTotal,0,1):1)*100)+'%; background:'+(course.color||'#ff8a3d')+';',
        prompt:dq?this.drillPrompt(dq):'', ansBox:S.input||'?',
        showHissan:!!hissan, showPlainPrompt:!hissan, hissanLeft:hissan?hissan[1]:'',hissanOp:hissan?hissan[2]:'',hissanRight:hissan?hissan[3]:'',
        hissanAria:hissan?hissan[1]+' '+hissan[2]+' '+hissan[3]+' の筆算':'',showHissanInput:hissanColumns>0,
        hissanCells:hissanCells, hissanCellsAria:'こたえ '+(typed?typed.split('').reverse().join(''):'まだ ない'),
        showColumnAsk:hissanColumns>0&&!dSolved, columnAsk:'みぎの くらいから 1けたずつ いれてね',
        ansStyle:dEcho?'background:#e8f7ec; border-color:#3aa655; color:#22683c;':(d.mark?'border-color:#e08a7a; color:#b23b23;':''),
        showAns:!dPick&&!hissan, showAsk:dPick, askText:dWriting?'ただしい かんじを えらんでね':(dKanji?'よみかたを えらんでね':'こたえを '+dChoices.length+'つから えらんでね'),
        showPad:!dPick, showPick:dPick, pickStyle:dChoices.length>4?'grid-template-columns:repeat(3,minmax(0,1fr));':'', pickAria:dWriting?'かんじの えらびもんだい':(dKanji?'よみかたの えらびもんだい':'こたえの '+dChoices.length+'たくもんだい'), picks:drillPicks,
        showHint:d.mark==='wrong', hint:d.hint||'',
        showEcho:!!dEcho, echoMain:dEcho?dEcho.main:'', echoSub:dEcho?dEcho.sub:'', echoHasSub:!!(dEcho&&dEcho.sub),
        showAnswer:!!d.revealed&&!dEcho, answerMain:dPair.main, answerSub:dPair.sub, answerHasSub:!!dPair.sub,
        showCopy:!!d.revealed&&!dEcho, copyText:d.copyMiss>0?'もういちど。うえの こたえを よく みてね':copyAsk,
        streakHidden:d.streak>=3?'false':'true', streakText:'れんぞく '+d.streak+'もん！',
        playing:!d.done, finished:!!d.done,
        finishMark:d.allRetired?'🎓':'🏅', finishTitle:d.allRetired?'ぜんぶ おぼえたよ！':'ステージ '+stageNo+' クリア！',
        clearBody:d.allRetired?'どの もんだいも 6つから 3かい つづけて せいかい できたよ':(d.perfect+' / '+activeTotal+'もんを 1かいめで せいかい'),
        showRetiredNote:!d.allRetired&&retiredNow>0, retiredNote:'おぼえた もんだい '+retiredNow+'もんは つぎから でないよ',
        showUnlockNote:unlockedNow, unlockNote:'ステージ '+(stageNo+1)+'が あそべるように なったよ！',
        showGoNextStage:nextStage, goNextStageText:'つぎの ステージへ ▶',
        restartText:d.allRetired?'さいしょから':'もういちど',
        restartStyle:nextStage?'background:#fff; color:#6b5e45; border-color:#f0e2c8; box-shadow:none;':'',
        pad:drillPad,
        onExit:()=>this.exitDrill(), onStages:()=>this.drillToStages(), onGoNextStage:()=>this.drillGoNextStage(),
        onRestart:()=>{if(d.allRetired)this.relearnDrill();else this.startDrill(d.id,true,d.answerMode,d.stage);}
      };
    }
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillEntryTemplate()
    {
        return """
            <!-- とっくんモード -->
            <div class="kt-drill-grid">
              <sc-for list="{{ drillCards }}" as="d" hint-placeholder-count="8">
                <div class="kt-hero-card kt-drill-card" style="{{ d.cardStyle }}">
                  <div class="kt-drill-card-top">
                    <span class="kt-drill-card-kicker">とっくん・{{ d.badge }}</span>
                    <span class="kt-drill-reset" style="{{ d.resetStyle }}" role="button" tabindex="0" aria-label="{{ d.resetAria }}" onclick="{{ d.onReset }}">{{ d.resetText }}</span>
                  </div>
                  <div class="kt-drill-card-body" role="button" tabindex="0" aria-label="{{ d.ariaLabel }}" onclick="{{ d.onStart }}">
                    <div class="kt-drill-card-title">{{ d.cardTitle }}<span class="kt-drill-card-tag" style="{{ d.tagStyle }}">{{ d.cardTag }}</span></div>
                    <div class="kt-drill-card-foot">
                      <span class="kt-drill-card-sub">{{ d.progressText }}</span>
                      <div class="kt-drill-bar"><span style="{{ d.barStyle }}"></span></div>
                      <span class="kt-drill-card-go">{{ d.actionText }}</span>
                    </div>
                  </div>
                </div>
              </sc-for>
            </div>
""".TrimEnd('\r', '\n');
    }

    // The stage screen shares the answer-mode screen's layout and back button class, so the
    // Android back key (MainActivity.BACK_STEP_SCRIPT) steps out of it the same way.
    private static string BuildDrillStageScreenTemplate()
    {
        return """
  <!-- ============ DRILL STAGE ============ -->
  <sc-if value="{{ isDrillStage }}" hint-placeholder-val="{{ false }}">
    <div data-screen-label="ステージ" class="kt-drill-mode-screen">
      <button type="button" class="kt-drill-mode-back" aria-label="とっくんの いちらんに もどる" onclick="{{ drillStageView.onBack }}">← もどる</button>
      <div class="kt-drill-mode-panel">
        <span class="kt-drill-badge" style="{{ drillStageView.headStyle }}">{{ drillStageView.badge }}</span>
        <div class="kt-drill-mode-title">{{ drillStageView.title }}</div>
        <div class="kt-drill-mode-question">ステージを えらんでね</div>
        <div class="kt-drill-stage-options" aria-label="ステージ">
          <sc-for list="{{ drillStageView.stages }}" as="t" hint-placeholder-count="4">
            <button type="button" class="kt-drill-stage-option" style="{{ t.style }}" aria-disabled="{{ t.ariaDisabled }}" aria-label="{{ t.ariaLabel }}" onclick="{{ t.onClick }}">
              <span class="kt-drill-mode-number">{{ t.no }}</span>
              <span class="kt-drill-stage-name">{{ t.name }}</span>
              <span class="kt-drill-stage-range">{{ t.range }}</span>
              <span class="kt-drill-bar"><span style="{{ t.barStyle }}"></span></span>
              <span class="kt-drill-stage-status">{{ t.status }}</span>
            </button>
          </sc-for>
        </div>
        <div class="kt-drill-mode-key-help">キーボードの 1〜4 でも えらべるよ</div>
      </div>
    </div>
  </sc-if>
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillModeScreenTemplate()
    {
        return """
  <!-- ============ DRILL ANSWER MODE ============ -->
  <sc-if value="{{ isDrillMode }}" hint-placeholder-val="{{ false }}">
    <div data-screen-label="こたえかた" class="kt-drill-mode-screen">
      <button type="button" class="kt-drill-mode-back" aria-label="ステージの いちらんに もどる" onclick="{{ drillModeView.onBack }}">← もどる</button>
      <div class="kt-drill-mode-panel">
        <span class="kt-drill-badge" style="{{ drillModeView.headStyle }}">{{ drillModeView.badge }}</span>
        <div class="kt-drill-mode-title">{{ drillModeView.title }}</div>
        <div class="kt-drill-mode-stage">{{ drillModeView.stageText }}</div>
        <div class="kt-drill-mode-question">こたえかたを えらんでね</div>
        <div class="kt-drill-mode-options" aria-label="こたえかた">
          <button type="button" class="kt-drill-mode-option" aria-label="{{ drillModeView.firstAria }}" onclick="{{ drillModeView.onFirst }}">
            <span class="kt-drill-mode-number">1</span>
            <span class="kt-drill-mode-symbol">{{ drillModeView.firstSymbol }}</span>
            <span class="kt-drill-mode-name">{{ drillModeView.firstName }}</span>
            <span class="kt-drill-mode-help">{{ drillModeView.firstHelp }}</span>
          </button>
          <button type="button" class="kt-drill-mode-option" aria-label="{{ drillModeView.secondAria }}" onclick="{{ drillModeView.onSecond }}">
            <span class="kt-drill-mode-number">2</span>
            <span class="kt-drill-mode-symbol">{{ drillModeView.secondSymbol }}</span>
            <span class="kt-drill-mode-name">{{ drillModeView.secondName }}</span>
            <span class="kt-drill-mode-help">{{ drillModeView.secondHelp }}</span>
          </button>
        </div>
        <div class="kt-drill-mode-key-help">キーボードの 1・2 でも えらべるよ</div>
      </div>
    </div>
  </sc-if>
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillScreenTemplate()
    {
        return """
  <!-- ============ DRILL ============ -->
  <sc-if value="{{ isDrill }}" hint-placeholder-val="{{ false }}">
    <div data-screen-label="とっくん" class="kt-drill-screen">
      <div class="kt-drill-head">
        <div class="kt-drill-quit" role="button" tabindex="0" aria-label="とっくんを やめて トップに もどる" onclick="{{ drillView.onExit }}">✕ やめる</div>
        <div class="kt-drill-titles">
          <span class="kt-drill-badge" style="{{ drillView.headStyle }}">{{ drillView.badge }}</span>
          <span class="kt-drill-title">{{ drillView.title }}</span>
          <span class="kt-drill-stage-chip">{{ drillView.stageText }}</span>
          <span class="kt-drill-section" style="{{ drillView.sectionStyle }}">{{ drillView.section }}</span>
        </div>
        <div class="kt-drill-streak" aria-hidden="{{ drillView.streakHidden }}">🔥 {{ drillView.streakText }}</div>
        <div class="kt-drill-count">{{ drillView.countText }}</div>
      </div>
      <div class="kt-drill-bar kt-drill-bar-wide"><span style="{{ drillView.barStyle }}"></span></div>
      <sc-if value="{{ drillView.playing }}" hint-placeholder-val="{{ true }}">
        <div class="kt-drill-body">
          <div class="kt-drill-stage">
            <sc-if value="{{ drillView.showHissan }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-hissan" aria-label="{{ drillView.hissanAria }}">
                <div class="kt-drill-hissan-row"><span aria-hidden="true"></span><span>{{ drillView.hissanLeft }}</span></div>
                <div class="kt-drill-hissan-row"><span>{{ drillView.hissanOp }}</span><span>{{ drillView.hissanRight }}</span></div>
                <div class="kt-drill-hissan-rule" aria-hidden="true"></div>
                <sc-if value="{{ drillView.showHissanInput }}" hint-placeholder-val="{{ false }}">
                  <div class="kt-drill-hissan-row"><span aria-hidden="true"></span><span class="kt-drill-hissan-cells" role="group" aria-live="polite" aria-label="{{ drillView.hissanCellsAria }}">
                    <sc-for list="{{ drillView.hissanCells }}" as="h" hint-placeholder-count="3"><span class="kt-drill-hissan-cell" style="{{ h.style }}" aria-hidden="true">{{ h.text }}</span></sc-for>
                  </span></div>
                </sc-if>
              </div>
            </sc-if>
            <sc-if value="{{ drillView.showColumnAsk }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-ask">{{ drillView.columnAsk }}</div>
            </sc-if>
            <sc-if value="{{ drillView.showPlainPrompt }}" hint-placeholder-val="{{ true }}">
              <div class="kt-drill-prompt">{{ drillView.prompt }}</div>
            </sc-if>
            <div class="kt-drill-answering">
              <sc-if value="{{ drillView.showAns }}" hint-placeholder-val="{{ true }}">
                <div class="kt-drill-ans" style="{{ drillView.ansStyle }}">{{ drillView.ansBox }}</div>
              </sc-if>
              <sc-if value="{{ drillView.showPick }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-ask">{{ drillView.askText }}</div>
                <div class="kt-drill-pick" style="{{ drillView.pickStyle }}" aria-label="{{ drillView.pickAria }}">
                  <sc-for list="{{ drillView.picks }}" as="p" hint-placeholder-count="6">
                    <div class="kt-drill-pick-btn" role="button" tabindex="0" style="{{ p.style }}" aria-label="{{ p.ariaLabel }}" onclick="{{ p.onClick }}"><span class="kt-drill-pick-no">{{ p.no }}</span><span>{{ p.label }}</span></div>
                  </sc-for>
                </div>
              </sc-if>
            </div>
            <div class="kt-drill-feedback">
              <sc-if value="{{ drillView.showEcho }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-pair is-ok" role="status">
                  <span class="kt-drill-pair-main">○ {{ drillView.echoMain }}</span>
                  <sc-if value="{{ drillView.echoHasSub }}" hint-placeholder-val="{{ false }}">
                    <span class="kt-drill-pair-sub">{{ drillView.echoSub }}</span>
                  </sc-if>
                </div>
              </sc-if>
              <sc-if value="{{ drillView.showHint }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-note is-hint" role="status">💡 {{ drillView.hint }}</div>
              </sc-if>
              <sc-if value="{{ drillView.showAnswer }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-pair is-answer" role="status">
                  <span class="kt-drill-pair-label">こたえは</span>
                  <span class="kt-drill-pair-main">{{ drillView.answerMain }}</span>
                  <sc-if value="{{ drillView.answerHasSub }}" hint-placeholder-val="{{ false }}">
                    <span class="kt-drill-pair-sub">{{ drillView.answerSub }}</span>
                  </sc-if>
                </div>
              </sc-if>
              <sc-if value="{{ drillView.showCopy }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-note is-copy" role="status">✏️ {{ drillView.copyText }}</div>
              </sc-if>
            </div>
          </div>
          <sc-if value="{{ drillView.showPad }}" hint-placeholder-val="{{ true }}">
            <div class="kt-drill-side">
              <div class="kt-drill-pad" aria-label="数字入力パッド">
                <sc-for list="{{ drillView.pad }}" as="k" hint-placeholder-count="12">
                  <div role="button" tabindex="0" aria-label="{{ k.ariaLabel }}" onclick="{{ k.onClick }}" style="{{ k.style }}">{{ k.label }}</div>
                </sc-for>
              </div>
            </div>
          </sc-if>
        </div>
      </sc-if>
      <sc-if value="{{ drillView.finished }}" hint-placeholder-val="{{ false }}">
        <div class="kt-drill-finish">
          <div class="kt-drill-finish-mark">{{ drillView.finishMark }}</div>
          <div class="kt-drill-finish-title">{{ drillView.finishTitle }}</div>
          <div class="kt-drill-finish-body">{{ drillView.clearBody }}</div>
          <sc-if value="{{ drillView.showUnlockNote }}" hint-placeholder-val="{{ false }}">
            <div class="kt-drill-finish-note is-unlock" role="status">🔓 {{ drillView.unlockNote }}</div>
          </sc-if>
          <sc-if value="{{ drillView.showRetiredNote }}" hint-placeholder-val="{{ false }}">
            <div class="kt-drill-finish-note">🎓 {{ drillView.retiredNote }}</div>
          </sc-if>
          <div class="kt-drill-finish-row">
            <sc-if value="{{ drillView.showGoNextStage }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-next" role="button" tabindex="0" onclick="{{ drillView.onGoNextStage }}">{{ drillView.goNextStageText }}</div>
            </sc-if>
            <div class="kt-drill-next" role="button" tabindex="0" style="{{ drillView.restartStyle }}" onclick="{{ drillView.onRestart }}">{{ drillView.restartText }}</div>
            <div class="kt-drill-back" role="button" tabindex="0" onclick="{{ drillView.onStages }}">ステージを えらぶ</div>
          </div>
        </div>
      </sc-if>
    </div>
  </sc-if>
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillStyle()
    {
        return """
<style id="kt-drill-mode">
  .kt-hero-row{display:flex;flex-wrap:wrap;gap:14px;margin:22px 0 12px;align-items:stretch;}
  .kt-hero-card{flex:1 1 200px;min-width:0;border:4px solid #f0e2c8;border-radius:22px;padding:14px 16px;word-break:keep-all;overflow-wrap:anywhere;}
  .kt-hero-weak{word-break:normal;overflow-wrap:break-word;}
  /* The courses take their own full-width line under the mission cards, so a growing course list
     adds columns instead of stretching those cards. A column is never narrower than the card's
     kicker-and-reset line and its footer, so neither wraps (four columns at the usual widths). */
  .kt-drill-grid{flex:1 1 100%;min-width:0;display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,228px),1fr));gap:10px;}
  .kt-drill-card{border-radius:18px;padding:8px 12px;display:flex;flex-direction:column;gap:2px;}
  .kt-drill-card-top{display:flex;align-items:center;justify-content:space-between;gap:6px;}
  .kt-drill-card-body{flex:1;display:flex;flex-direction:column;justify-content:center;cursor:pointer;}
  /* The kicker yields to "ほんとうに もどす？" instead of wrapping, so the second tap of the reset
     confirmation lands on a card that has not moved. */
  .kt-drill-card-kicker{min-width:0;font-size:13px;font-weight:700;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
  .kt-drill-card-title{display:flex;align-items:center;flex-wrap:wrap;gap:2px 8px;font-size:19px;font-weight:900;line-height:1.15;}
  .kt-drill-card-tag{flex:none;background:#fff;border:2px solid currentColor;border-radius:999px;padding:0 8px;font-size:13px;line-height:1.2;}
  .kt-drill-card-foot{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-top:4px;}
  .kt-drill-card-foot .kt-drill-bar{flex:1;min-width:24px;margin-top:0;}
  .kt-drill-card-sub{flex:none;font-size:13px;font-weight:700;}
  .kt-drill-card-go{flex:none;font-size:14px;font-weight:900;}
  .kt-drill-bar{height:8px;margin-top:5px;background:#fff;border:3px solid #ecd9b9;border-radius:10px;overflow:hidden;}
  .kt-drill-bar > span{display:block;height:100%;border-radius:8px;}
  .kt-drill-badge{flex:none;color:#fff;border-radius:14px;padding:2px 10px;font-size:14px;font-weight:900;}
  .kt-drill-reset{flex:none;background:#fff;border:2px solid #ecd9b9;border-radius:12px;padding:0 8px;font-size:12px;font-weight:700;color:#9a8662;cursor:pointer;}
  .kt-drill-mode-screen{min-height:100vh;padding:22px 44px 30px;display:flex;flex-direction:column;gap:18px;}
  .kt-drill-mode-back{align-self:flex-start;min-height:44px;background:#fff;border:3px solid #f0e2c8;border-radius:20px;padding:6px 16px;font-family:inherit;font-size:16px;font-weight:700;color:#6b5e45;cursor:pointer;}
  .kt-drill-mode-panel{flex:1;max-width:760px;width:100%;margin:0 auto;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;text-align:center;}
  .kt-drill-mode-title{font-size:26px;font-weight:900;color:#3a3326;}
  .kt-drill-mode-question{font-size:38px;font-weight:900;color:#3a3326;line-height:1.2;}
  .kt-drill-mode-options{width:100%;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px;margin-top:8px;}
  .kt-drill-mode-option{position:relative;min-height:220px;background:#fff;border:4px solid #f0e2c8;border-radius:26px;padding:22px 18px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;font:inherit;color:#3a3326;box-shadow:0 6px 0 #ecd9b9;cursor:pointer;transition:border-color .2s ease,background .2s ease,box-shadow .2s ease,transform .2s ease;}
  .kt-drill-mode-number{position:absolute;top:14px;left:14px;min-width:38px;height:38px;border-radius:12px;background:#fff3e0;color:#8a6940;display:flex;align-items:center;justify-content:center;font-size:20px;font-weight:900;}
  .kt-drill-mode-symbol{font-size:44px;font-weight:900;letter-spacing:2px;color:#ff7b2e;}
  .kt-drill-mode-name{font-size:27px;font-weight:900;}
  .kt-drill-mode-help{font-size:16px;font-weight:700;color:#6b5e45;}
  .kt-drill-mode-key-help{font-size:15px;font-weight:700;color:#7b6a4d;}
  .kt-drill-mode-stage{background:#fff;border:3px solid #f0e2c8;border-radius:16px;padding:3px 14px;font-size:18px;font-weight:800;color:#6b5e45;}
  /* Four stages read left to right in order; the number in the corner is the key that picks one. */
  .kt-drill-stage-options{width:100%;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px;margin-top:8px;}
  .kt-drill-stage-option{position:relative;min-height:190px;min-width:0;background:#fff;border:4px solid #f0e2c8;border-radius:24px;padding:46px 12px 16px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;font:inherit;color:#3a3326;box-shadow:0 6px 0 #ecd9b9;cursor:pointer;word-break:keep-all;overflow-wrap:anywhere;transition:border-color .2s ease,background .2s ease,box-shadow .2s ease,transform .2s ease;}
  .kt-drill-stage-option .kt-drill-mode-number{top:10px;left:10px;min-width:30px;height:30px;border-radius:10px;font-size:16px;}
  .kt-drill-stage-name{font-size:24px;font-weight:900;white-space:nowrap;}
  .kt-drill-stage-range{font-size:15px;font-weight:700;color:#6b5e45;}
  .kt-drill-stage-option .kt-drill-bar{display:block;width:100%;margin-top:2px;}
  .kt-drill-stage-status{font-size:15px;font-weight:800;line-height:1.3;}
  .kt-drill-stage-chip{flex:none;background:#fff;border:3px solid #f0e2c8;border-radius:16px;padding:3px 12px;font-size:15px;font-weight:900;color:#3a3326;}
  .kt-drill-screen{position:relative;min-height:100vh;display:flex;flex-direction:column;gap:12px;padding:22px 44px 30px;}
  .kt-drill-head{display:flex;align-items:center;gap:14px;}
  .kt-drill-quit{flex:none;background:#fff;border:3px solid #f0e2c8;border-radius:20px;padding:6px 14px;font-size:16px;font-weight:700;color:#6b5e45;cursor:pointer;}
  .kt-drill-titles{flex:1;min-width:0;display:flex;align-items:center;gap:10px;flex-wrap:wrap;}
  .kt-drill-title{font-size:22px;font-weight:900;}
  .kt-drill-section{background:#fff;border:3px solid #f0e2c8;border-radius:16px;padding:3px 12px;font-size:15px;font-weight:700;color:#6b5e45;}
  .kt-drill-count{flex:none;font-size:20px;font-weight:900;color:#6b5e45;}
  .kt-drill-bar-wide{height:16px;}
  .kt-drill-body{flex:1;display:flex;gap:28px;align-items:stretch;}
  .kt-drill-stage{flex:1;min-width:0;background:#fff;border:4px solid #f0e2c8;border-radius:26px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;padding:18px;}
  .kt-drill-prompt{font-size:72px;font-weight:900;line-height:1.15;letter-spacing:2px;color:#3a3326;text-align:center;}
  .kt-drill-hissan{width:calc(1em + 4ch);font-size:72px;font-weight:900;line-height:1.12;font-variant-numeric:tabular-nums;color:#3a3326;}
  .kt-drill-hissan-row{display:grid;grid-template-columns:1em 4ch;align-items:baseline;}
  .kt-drill-hissan-row span:last-child{text-align:right;}
  .kt-drill-hissan-rule{border-top:5px solid currentColor;margin:3px 0 8px;}
  /* Answer cells line up under the digits above: each is one ch of the same font, and the
     outline is an inset shadow so it never widens a cell (issue #106). */
  .kt-drill-hissan-cells{display:flex;justify-content:flex-end;}
  .kt-drill-hissan-cell{display:inline-block;width:1ch;height:1.18em;text-align:center;background:#fff7ec;border-radius:10px;box-shadow:inset 0 0 0 3px #d8c4a0;}
  .kt-drill-answering{width:100%;display:flex;flex-direction:column;align-items:center;gap:10px;}
  .kt-drill-ans{min-width:220px;background:#fff7ec;border:4px dashed #d8c4a0;border-radius:20px;padding:4px 24px;font-size:52px;font-weight:900;color:#3a3326;text-align:center;}
  /* The feedback slot keeps its height while it is empty, hinting, or showing the pair, so the
     question above it never moves across the 100 questions of a course. */
  .kt-drill-feedback{width:100%;min-height:248px;display:flex;flex-direction:column;align-items:center;justify-content:flex-start;gap:12px;}
  .kt-drill-note{border-radius:16px;padding:9px 16px;font-size:19px;font-weight:700;text-align:center;}
  .kt-drill-note.is-hint{background:#fff6db;border:3px solid #ffd24a;color:#7a5d00;}
  .kt-drill-note.is-copy{background:#fff0f0;border:3px solid #e08a7a;color:#9c2f1c;}
  .kt-drill-pair{min-width:300px;display:flex;flex-direction:column;align-items:center;gap:2px;border-radius:20px;padding:10px 32px;text-align:center;}
  .kt-drill-pair.is-ok{background:#e8f7ec;border:4px solid #3aa655;color:#22683c;}
  .kt-drill-pair.is-answer{background:#ffe0da;border:4px solid #e08a7a;color:#b23b23;}
  .kt-drill-pair-label{font-size:17px;font-weight:700;}
  .kt-drill-pair-main{font-size:44px;font-weight:900;line-height:1.15;letter-spacing:1px;}
  .kt-drill-pair-sub{font-size:32px;font-weight:900;line-height:1.2;}
  .kt-drill-side{width:300px;flex:none;display:flex;flex-direction:column;gap:10px;}
  .kt-drill-streak{flex:none;background:#fff6db;border:3px solid #ffd24a;border-radius:18px;padding:4px 12px;text-align:center;font-size:16px;font-weight:900;color:#7a5d00;white-space:nowrap;}
  .kt-drill-streak[aria-hidden="true"]{display:none;}
  .kt-drill-ask{font-size:20px;font-weight:700;color:#6b5e45;}
  .kt-drill-pad{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;align-content:start;}
  /* The choices sit directly under the question in a grid: up to four fill two columns, and five
     or six switch to three columns so the whole set still fits the screen in a single glance. */
  .kt-drill-pick{width:100%;max-width:720px;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px;}
  .kt-drill-pick-btn{position:relative;min-height:84px;display:flex;align-items:center;justify-content:center;background:#fff;border:4px solid #f0e2c8;border-radius:20px;padding:8px 44px;font-size:38px;font-weight:900;color:#3a3326;box-shadow:0 4px 0 #ecd9b9;cursor:pointer;overflow-wrap:anywhere;}
  .kt-drill-pick-no{position:absolute;left:12px;top:50%;transform:translateY(-50%);background:#fff3e0;border-radius:12px;padding:0 10px;font-size:16px;font-weight:900;color:#a1855a;}
  .kt-drill-next{background:#ff8a3d;color:#fff;border:4px solid #e07d2a;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;box-shadow:0 5px 0 #d96a26;cursor:pointer;}
  .kt-drill-back{background:#fff;color:#6b5e45;border:4px solid #f0e2c8;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;cursor:pointer;}
  .kt-drill-finish{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;}
  .kt-drill-finish-mark{font-size:96px;line-height:1;}
  .kt-drill-finish-title{font-size:44px;font-weight:900;}
  .kt-drill-finish-body{font-size:24px;font-weight:700;color:#6b5e45;}
  .kt-drill-finish-note{font-size:20px;font-weight:700;color:#3f7a4f;background:#e8f7ec;border:3px solid #9dd3ae;border-radius:16px;padding:6px 16px;}
  .kt-drill-finish-note.is-unlock{color:#2f5f9e;background:#eaf3ff;border-color:#a8c8f0;}
  .kt-drill-finish-row{display:flex;flex-wrap:wrap;justify-content:center;gap:16px;margin-top:8px;}
  .kt-drill-mode-option:hover,.kt-drill-stage-option:not([aria-disabled="true"]):hover,.kt-drill-pick-btn:hover{border-color:#ffb170;background:#fffaf4;transform:translateY(-2px);box-shadow:0 8px 0 #ecd9b9;}
  .kt-drill-mode-option:active,.kt-drill-stage-option:not([aria-disabled="true"]):active,.kt-drill-pick-btn:active{transform:translateY(3px);box-shadow:0 2px 0 #dcc49c;}
  .kt-drill-mode-back:hover,.kt-drill-quit:hover,.kt-drill-back:hover,.kt-drill-reset:hover{background:#fff8ee;border-color:#d9bd8c;}
  .kt-drill-mode-back:active,.kt-drill-quit:active,.kt-drill-back:active,.kt-drill-reset:active{transform:translateY(2px);}
  .kt-drill-mode-option:focus-visible,.kt-drill-stage-option:focus-visible,.kt-drill-mode-back:focus-visible,.kt-drill-card-body:focus-visible,.kt-drill-reset:focus-visible,.kt-drill-quit:focus-visible,.kt-drill-pick-btn:focus-visible,.kt-drill-next:focus-visible,.kt-drill-back:focus-visible,.kt-drill-pad [role="button"]:focus-visible{outline:4px solid #2f7ccd;outline-offset:3px;}

  @media (max-width: 1100px) {
    .kt-drill-screen{padding:18px 26px 24px;}
    .kt-drill-side{width:250px;}
    .kt-drill-prompt{font-size:58px;}
    .kt-drill-hissan{font-size:58px;}
    .kt-drill-pick-btn{font-size:34px;}
  }

  @media (max-height: 820px) {
    .kt-hero-row{margin:10px 0 8px;gap:8px;}
    .kt-hero-card{padding:10px 12px;}
    .kt-drill-grid{gap:6px;grid-template-columns:repeat(auto-fill,minmax(min(100%,224px),1fr));}
    .kt-drill-card{padding:6px 10px;}
    .kt-drill-card-title{font-size:17px;}
    .kt-drill-stage{gap:10px;}
    .kt-drill-prompt{font-size:56px;}
    .kt-drill-hissan{font-size:56px;}
    .kt-drill-ans{font-size:42px;min-width:180px;}
    .kt-drill-feedback{min-height:220px;gap:8px;}
    .kt-drill-pair{min-width:260px;padding:8px 24px;}
    .kt-drill-pair-main{font-size:36px;}
    .kt-drill-pair-sub{font-size:28px;}
    .kt-drill-pick{gap:10px;}
    .kt-drill-pick-btn{font-size:30px;min-height:66px;padding:6px 40px;}
    .kt-drill-finish-mark{font-size:72px;}
    .kt-drill-finish-title{font-size:36px;}
    .kt-drill-mode-screen{padding-top:16px;padding-bottom:20px;}
    .kt-drill-mode-panel{gap:9px;}
    .kt-drill-mode-option{min-height:170px;padding:14px;}
    .kt-drill-stage-option{min-height:150px;padding-top:40px;}
    .kt-drill-mode-symbol{font-size:36px;}
  }

  @media (max-width: 800px) {
    .kt-hero-card{flex:1 1 100%;}
    .kt-drill-body{flex-direction:column;}
    .kt-drill-side{width:100%;}
    .kt-drill-pad{grid-template-columns:repeat(6,minmax(0,1fr));}
    .kt-drill-mode-screen{padding:18px 20px 24px;}
    .kt-drill-mode-question{font-size:30px;}
    .kt-drill-mode-options{grid-template-columns:minmax(0,1fr);gap:14px;}
    .kt-drill-mode-option{min-height:150px;}
    .kt-drill-stage-options{grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;}
    .kt-drill-stage-option{min-height:140px;padding:44px 10px 12px;}
    .kt-drill-stage-name{font-size:20px;}
  }

  @media (prefers-reduced-motion: reduce) {
    .kt-drill-mode-option,.kt-drill-stage-option{transition:none;}
  }
</style>
""".TrimEnd('\r', '\n');
    }
}
