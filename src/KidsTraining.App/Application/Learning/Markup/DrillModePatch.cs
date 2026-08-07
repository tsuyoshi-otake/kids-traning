namespace KidsTraining.App.Application.Learning.Markup;

/// <summary>
/// Adds the fixed arithmetic drill mode (とっくんモード) selected from the start screen (issue #62).
///
/// The adaptive session decides what a learner sees next, which is right for daily study but wrong
/// for the number facts that have to become automatic. The drill is therefore a separate screen with
/// its own fixed course, its own storage key, and no connection to curriculum evidence: drilling can
/// never inflate mastery, stars, or XP.
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
            "    drill:null, drillAsk:'',\n    muted:false, speakingEnglish:'', setupName:''",
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
            "if(this.state.drill)this.saveDrillProgress(this.state.drill,false);",
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
            "      isDrill:sc==='drill', drillCards:drillCards, drillView:drillView,",
            StringComparison.Ordinal);

        // The drill courses belong beside "きょうの ミッション" / "いまの にがて": they are the same kind of
        // choice, so they share the hero card row and the card design instead of owning a separate panel.
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
            BuildDrillScreenTemplate() + "\n\n  <!-- ============ QUIZ ============ -->",
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
this._drillKeyHandler=e=>{if(e.repeat||e.isComposing||e.key==='Process'||e.ctrlKey||e.altKey||e.metaKey||this.state.screen!=='drill')return;const d=this.state.drill;if(!d||d.done)return;const roleButton=!!(e.target&&e.target.getAttribute&&e.target.getAttribute('role')==='button');if(roleButton&&(e.key==='Enter'||e.key===' '))return;if(d.revealed){if(e.key==='Enter'||e.key===' '){e.preventDefault();this.drillNext();}return;}const dq=this.drillQuestionAt(d,d.idx);if(dq&&dq.kind==='pick'){if(/^[1-4]$/.test(e.key)){e.preventDefault();const choice=(dq.choices||[])[Number(e.key)-1];if(choice)this.drillChoose(choice);}return;}if(/^[0-9]$/.test(e.key)){e.preventDefault();this.press(e.key);return;}if(e.key==='Backspace'||e.key==='Delete'){e.preventDefault();this.del();return;}if(e.key!=='Enter'||!this.state.input)return;e.preventDefault();this.drillSubmit();};document.addEventListener('keydown',this._drillKeyHandler);
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillMethodsScript()
    {
        return """
  drillStorageKey(){return 'kt_drill_v1';}
  drillCourses(){return [
    {id:'g1',grade:1,total:200,badge:'1年生',title:'たしざん・ひきざん',sub:'1＋1から じゅんばんに 200もん',color:'#ff8a3d',edge:'#e07d2a',shade:'#fff3e6'},
    {id:'g2',grade:2,total:200,badge:'2年生',title:'かけざん（九九）',sub:'2のだんから じゅんばんに 200もん',color:'#4a9bf0',edge:'#2f7ccd',shade:'#eaf3ff'},
    {id:'k1',grade:1,total:200,badge:'1年生',title:'かんじの よみ',sub:'1年生の 漢字 80字を よむ 200もん',color:'#5fbf7a',edge:'#3f9a5b',shade:'#eaf7ee'},
    {id:'k2',grade:2,total:200,badge:'2年生',title:'かんじの よみ',sub:'2年生の 漢字 160字を よむ 200もん',color:'#b07ae0',edge:'#8d55c4',shade:'#f4ecff'}
  ];}
  drillCourse(id){const list=this.drillCourses();for(let i=0;i<list.length;i++)if(list[i].id===id)return list[i];return null;}
  drillDefaultRecord(){return {idx:0,perfect:0,mistakes:0,runs:0,best:0};}
  readDrillProgress(){
    let raw=null;try{raw=localStorage.getItem(this.drillStorageKey());}catch(e){raw=null;}
    let data=null;try{data=raw?JSON.parse(raw):null;}catch(e){data=null;}
    const src=data&&typeof data==='object'?data:{},whole=v=>{const n=Math.floor(Number(v));return Number.isFinite(n)&&n>0?n:0;},out={};
    this.drillCourses().forEach(c=>{
      const row=src[c.id]&&typeof src[c.id]==='object'?src[c.id]:{};
      out[c.id]={idx:this.clamp(whole(row.idx),0,c.total),perfect:this.clamp(whole(row.perfect),0,c.total),mistakes:whole(row.mistakes),runs:whole(row.runs),best:this.clamp(whole(row.best),0,c.total)};
    });
    return out;
  }
  writeDrillProgress(map){try{localStorage.setItem(this.drillStorageKey(),JSON.stringify(map));}catch(e){}}
  saveDrillProgress(d,countRun){
    if(!d)return;const course=this.drillCourse(d.id);if(!course)return;
    const total=course.total,progress=this.readDrillProgress(),prev=progress[d.id]||this.drillDefaultRecord(),finished=d.idx>=total;
    progress[d.id]={idx:finished?0:this.clamp(d.idx,0,total),perfect:finished?0:d.perfect,mistakes:finished?0:d.mistakes,runs:prev.runs+(countRun?1:0),best:countRun?Math.max(prev.best,d.perfect):prev.best};
    this.writeDrillProgress(progress);
  }
  drillBank(id){if(!this._drillBanks)this._drillBanks={};if(!this._drillBanks[id])this._drillBanks[id]=this.buildDrillBank(id);return this._drillBanks[id];}
  drillKanjiEntries(grade){if(!this._drillKanji)this._drillKanji={};if(!this._drillKanji[grade])this._drillKanji[grade]=this.kanjiCurriculumEntries().filter(e=>e.g===grade);return this._drillKanji[grade];}
  buildDrillBank(id){
    const list=[],add=(sec,text,ans,hint,choices)=>{list.push({no:list.length+1,sec:sec,text:text,ans:ans,hint:hint,kind:choices?'pick':'num',choices:choices||null});};
    if(id==='k1'||id==='k2'){
      // A reading has no keypad, so the kanji course asks the child to choose. The three
      // distractors are readings of other kanji of the same grade, taken at fixed strides so
      // the course is the same every run, and skipped when they repeat a reading already on
      // screen: two identical choices would make a right answer look wrong.
      const grade=id==='k1'?1:2,entries=this.drillKanjiEntries(grade),n=entries.length,readings=entries.map(e=>e.r);
      const ask=(sec,at)=>{
        const e=entries[at],opts=[e.r];
        [13,29,47].forEach(step=>{let j=(at+step)%n,guard=0;while(guard<n&&opts.indexOf(readings[j])>=0){j=(j+1)%n;guard++;}opts.push(readings[j]);});
        const order=opts.slice(1);order.splice(at%4,0,e.r);
        add(sec,e.word,e.r,'さいしょの もじは 「'+Array.from(e.r)[0]+'」だよ。',order);
      };
      const A='あたらしい かんじ',B='ふくしゅう',C='しあげの ミックス';
      for(let i=0;i<n;i++)ask(A,i);
      if(grade===1)for(let i=0;i<n;i++)ask(B,(i*7)%n);
      const stride=grade===1?3:7,mix=200-list.length;
      for(let i=0;i<mix;i++)ask(C,(i*stride+1)%n);
      return list;
    }
    if(id==='g2'){
      const A='九九を おぼえる',B='むずかしい だんの ふくしゅう',C='ひっくりかえしても おなじ',D='□に はいる かず',E='しあげの ミックス';
      [2,5,3,4,6,7,8,9,1].forEach(a=>{for(let b=1;b<=9;b++)add(A,a+' × '+b,a*b,a+'のだんを 1から となえて みよう。');});
      [6,7,8,9].forEach(a=>{for(let b=9;b>=1;b--)add(B,a+' × '+b,a*b,a+'のだんを 9から ぎゃくに となえて みよう。');});
      [[3,4],[6,7],[2,8],[4,9],[5,6],[3,8],[7,9],[2,6],[4,7],[3,9],[6,8],[5,7],[2,9],[4,8],[3,6],[7,8],[5,9],[6,9]].forEach(pr=>{
        add(C,pr[0]+' × '+pr[1],pr[0]*pr[1],'かける じゅんばんを かえても こたえは おなじだよ。');
        add(C,pr[1]+' × '+pr[0],pr[0]*pr[1],'さっきと おなじ こたえに なるよ。');
      });
      [[3,4],[4,3],[6,2],[7,5],[8,3],[9,6],[2,7],[5,8],[6,9],[7,4],[8,6],[9,3],[4,7],[3,9],[6,5],[5,4],[8,8],[7,7],[9,9],[6,6],[4,4],[3,3],[8,4],[9,5],[7,8],[6,7],[9,8]].forEach((pr,i)=>{
        const a=pr[0],b=pr[1];
        if(i%2===0)add(D,a+' × □ ＝ '+(a*b),b,a+'のだんを となえて '+(a*b)+'を さがそう。');
        else add(D,'□ × '+b+' ＝ '+(a*b),a,b+'ずつ たして '+(a*b)+'に なる かずだよ。');
      });
      [[7,8],[6,7],[8,7],[7,6],[9,7],[7,9],[8,6],[6,8],[9,6],[6,9],[8,8],[7,7],[9,8],[8,9],[4,7],[7,4],[6,4],[4,6],[9,9],[3,8]].forEach(pr=>add(E,pr[0]+' × '+pr[1],pr[0]*pr[1],pr[0]+'のだんを 1から おもいだそう。'));
      return list;
    }
    const A='たしざんの きほん',B='10の ともだち',C='ひきざんの きほん',D='10から ひく',E='くりあがりの たしざん',F='くりさがりの ひきざん',G='しあげの ミックス';
    const hA='ゆびを つかって かぞえて みよう。',hB='あと いくつで 10に なるかな。',hC='ゆびを おって かぞえよう。',hD='10ぽんの ゆびから おろして みよう。',hE='さきに 10を つくって、のこりを たそう。',hF='10から ひいて、のこった かずを たそう。';
    for(let s=2;s<=7;s++)for(let a=1;a<s;a++)add(A,a+' ＋ '+(s-a),s,hA);
    for(let a=1;a<=9;a++)add(B,a+' ＋ '+(10-a),10,hB);
    for(let a=9;a>=1;a--)add(B,a+' ＋ '+(10-a),10,hB);
    for(let a=1;a<=9;a++)add(B,a+' ＋ □ ＝ 10',10-a,hB);
    for(let a=2;a<=7;a++)for(let b=1;b<a;b++)add(C,a+' − '+b,a-b,hC);
    for(let b=1;b<=9;b++)add(D,'10 − '+b,10-b,hD);
    for(let b=9;b>=1;b--)add(D,'10 − '+b,10-b,hD);
    for(let b=1;b<=9;b++)add(D,'10 − □ ＝ '+(10-b),b,hD);
    for(let a=9;a>=2;a--)for(let b=11-a;b<=9;b++)add(E,a+' ＋ '+b,a+b,hE);
    for(let m=11;m<=18;m++)for(let b=m-9;b<=9;b++)add(F,m+' − '+b,m-b,hF);
    const mixB=[3,7,4,8,2,6,1,9],mixD=[7,3,9,5,8,4,6,2];
    const mixE=[[9,4],[8,7],[7,6],[6,8],[9,7],[8,5],[7,9],[6,6]],mixF=[[13,7],[15,8],[12,6],[16,9],[14,5],[17,8],[11,4],[18,9]];
    for(let i=0;i<8;i++){
      add(G,'□ ＋ '+mixB[i]+' ＝ 10',10-mixB[i],hB);
      add(G,'10 − '+mixD[i],10-mixD[i],hD);
      add(G,mixE[i][0]+' ＋ '+mixE[i][1],mixE[i][0]+mixE[i][1],hE);
      add(G,mixF[i][0]+' − '+mixF[i][1],mixF[i][0]-mixF[i][1],hF);
    }
    return list;
  }
  drillAnswerLine(q){if(q.kind==='pick')return q.text+' → '+q.ans;return q.text.indexOf('□')>=0?q.text.replace('□',String(q.ans)):q.text+' ＝ '+q.ans;}
  drillMatches(q,value){const raw=String(value==null?'':value);if(!raw.length)return false;return q.kind==='pick'?raw===String(q.ans):Number(raw)===Number(q.ans);}
  drillQuestionAt(d,index){
    if(!d)return null;const bank=this.drillBank(d.id),again=Array.isArray(d.again)?d.again:[];
    if(index<bank.length)return bank[index]||null;
    const extra=index-bank.length;
    return extra<again.length?(bank[again[extra]]||null):null;
  }
  drillQuestion(){const d=this.state.drill;return d?this.drillQuestionAt(d,d.idx):null;}
  startDrill(id,restart){
    const course=this.drillCourse(id);if(!course)return;
    const progress=this.readDrillProgress(),saved=progress[id]||this.drillDefaultRecord(),bank=this.drillBank(id);
    let idx=restart?0:this.clamp(saved.idx,0,bank.length);
    if(idx>=bank.length)idx=0;
    const fresh=restart||idx===0;
    if(fresh){progress[id]={idx:0,perfect:0,mistakes:0,runs:saved.runs,best:saved.best};this.writeDrillProgress(progress);}
    this.sfx('select');
    this.setState({screen:'drill',drillAsk:'',input:'',drill:{id:id,idx:idx,miss:0,mark:'',hint:'',revealed:false,streak:0,perfect:fresh?0:saved.perfect,mistakes:fresh?0:saved.mistakes,again:[],counted:false,done:false,last:null}});
  }
  drillAdvance(patch){
    const d=this.state.drill;if(!d)return;
    const total=this.drillBank(d.id).length,next=Object.assign({},d,{miss:0,mark:'',hint:'',revealed:false},patch||{});
    next.idx=d.idx+1;
    const again=Array.isArray(next.again)?next.again:[],countRun=!d.counted&&next.idx>=total;
    if(countRun)next.counted=true;
    next.done=next.idx>=total+again.length;
    this.saveDrillProgress(next,countRun);
    if(next.done)this.sfx('clear');
    this.setState({drill:next,input:''});
  }
  drillSubmit(){this.drillAnswerWith(this.state.input);}
  drillChoose(value){this.drillAnswerWith(value);}
  drillAnswerWith(value){
    const d=this.state.drill;if(!d||d.done||d.revealed)return;
    const q=this.drillQuestion();if(!q)return;
    const raw=String(value==null?'':value);if(!raw.length)return;
    if(this.drillMatches(q,raw)){
      const clean=d.miss===0,firstPass=d.idx<this.drillBank(d.id).length;
      this.sfx(clean&&d.streak>=4?'combo':'correct');
      this.drillAdvance({perfect:d.perfect+(clean&&firstPass?1:0),streak:clean?d.streak+1:0,last:{ok:true,text:this.drillAnswerLine(q)}});
      return;
    }
    this.sfx('wrong');
    const miss=d.miss+1,revealed=miss>=2;
    this.setState({input:'',drill:Object.assign({},d,{miss:miss,mistakes:d.mistakes+1,mark:revealed?'answer':'wrong',hint:q.hint,revealed:revealed,streak:0})});
  }
  drillNext(){
    const d=this.state.drill;if(!d||!d.revealed)return;
    const q=this.drillQuestion(),bank=this.drillBank(d.id),again=Array.isArray(d.again)?d.again.slice():[];
    if(d.idx<bank.length&&again.indexOf(d.idx)<0)again.push(d.idx);
    this.drillAdvance({again:again,streak:0,last:{ok:false,text:q?this.drillAnswerLine(q):''}});
  }
  exitDrill(){const d=this.state.drill;this.saveDrillProgress(d,false);this.sfx('tap');this.setState({screen:'start',drill:null,drillAsk:'',input:''});}
  askDrillReset(id){this.sfx('tap');this.setState({drillAsk:this.state.drillAsk===id?'':id});}
  confirmDrillReset(id){
    const progress=this.readDrillProgress(),prev=progress[id]||this.drillDefaultRecord();
    progress[id]={idx:0,perfect:0,mistakes:0,runs:prev.runs,best:prev.best};
    this.writeDrillProgress(progress);this.sfx('select');this.setState({drillAsk:''});
  }
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillViewScript()
    {
        return """
    let drillCards=[],drillView=null;
    if(sc==='start'){
      const drillProgress=this.readDrillProgress(),drillAsk=String(S.drillAsk||'');
      drillCards=this.drillCourses().map(c=>{
        const row=drillProgress[c.id]||this.drillDefaultRecord(),pct=Math.round(this.clamp(row.idx/c.total,0,1)*100),asking=drillAsk===c.id;
        return {
          id:c.id, badge:c.badge, title:c.title,
          cardStyle:'background:'+c.shade+'; border-color:'+c.edge+'; color:'+c.edge+';',
          barStyle:'width:'+pct+'%; background:'+c.color+';',
          progressText:row.idx>0?(row.idx+' / '+c.total):(c.total+'もん'),
          actionText:row.idx>0?'つづきから ▶':'はじめる ▶',
          resetText:asking?'ほんとうに もどす？':'さいしょから',
          resetStyle:asking?'background:#ffe0da; border-color:#e08a7a; color:#b23b23;':'',
          ariaLabel:c.badge+'の とっくんモード、'+c.title+'。'+c.sub+'。'+c.total+'もん中 '+row.idx+'もん',
          resetAria:asking?c.title+'の しんちょくを 0にもどす':c.title+'を さいしょから',
          onStart:()=>this.startDrill(c.id,false),
          onReset:()=>{if(this.state.drillAsk===c.id)this.confirmDrillReset(c.id);else this.askDrillReset(c.id);}
        };
      });
    }
    if(sc==='drill'&&S.drill){
      const d=S.drill,course=this.drillCourse(d.id)||{},total=this.drillBank(d.id).length,dq=this.drillQuestionAt(d,d.idx);
      const dAgain=Array.isArray(d.again)?d.again:[],seen=Math.min(d.idx,total),inMain=d.idx<total;
      const dLast=d.last&&typeof d.last==='object'?d.last:null;
      const drillPad=['1','2','3','4','5','6','7','8','9'].map(n=>({label:n,ariaLabel:n,style:keyTile,onClick:()=>this.press(n)}));
      drillPad.push({label:'けす',ariaLabel:'ひとつ けす',style:keyClear,onClick:()=>this.del()});
      drillPad.push({label:'0',ariaLabel:'0',style:keyTile,onClick:()=>this.press('0')});
      drillPad.push({label:'OK',ariaLabel:'こたえる',style:keyOk,onClick:()=>this.drillSubmit()});
      const dPick=!!(dq&&dq.kind==='pick');
      const drillPicks=dPick?(dq.choices||[]).map((choice,i)=>({no:String(i+1),label:choice,ariaLabel:(i+1)+'ばん、'+choice,onClick:()=>this.drillChoose(choice)})):[];
      drillView={
        badge:course.badge||'', title:course.title||'', section:dq?dq.sec:'',
        countText:inMain?(seen+' / '+total):('なおし '+(d.idx-total+1)+' / '+dAgain.length),
        headStyle:'background:'+(course.color||'#ff8a3d')+';',
        barStyle:'width:'+Math.round(this.clamp(seen/(total||1),0,1)*100)+'%; background:'+(course.color||'#ff8a3d')+';',
        prompt:dq?dq.text:'', ansBox:S.input||'?',
        ansStyle:d.mark?'border-color:#e08a7a; color:#b23b23;':'',
        showAns:!dPick, showAsk:dPick, showPad:!dPick, showPick:dPick, picks:drillPicks,
        showHint:d.mark==='wrong', hint:d.hint||'',
        showAnswer:!!d.revealed, answerText:dq?this.drillAnswerLine(dq):'',
        showLast:!!dLast&&!d.mark, lastText:dLast?dLast.text:'',
        lastMark:dLast&&dLast.ok?'○':'✗',
        lastStyle:dLast&&dLast.ok?'':'background:#ffe0da; border-color:#e08a7a; color:#b23b23;',
        showStreak:d.streak>=3, streakText:'れんぞく '+d.streak+'もん！',
        playing:!d.done, finished:!!d.done,
        clearBody:d.perfect+' / '+total+'もんを 1かいめで せいかい',
        pad:drillPad,
        onNext:()=>this.drillNext(), onExit:()=>this.exitDrill(), onRestart:()=>this.startDrill(d.id,true)
      };
    }
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillEntryTemplate()
    {
        return """
            <!-- とっくんモード -->
            <div class="kt-drill-grid">
              <sc-for list="{{ drillCards }}" as="d" hint-placeholder-count="4">
                <div class="kt-hero-card kt-drill-card" style="{{ d.cardStyle }}">
                  <div class="kt-drill-card-top">
                    <span class="kt-drill-card-kicker">とっくん・{{ d.badge }}</span>
                    <span class="kt-drill-reset" style="{{ d.resetStyle }}" role="button" tabindex="0" aria-label="{{ d.resetAria }}" onclick="{{ d.onReset }}">{{ d.resetText }}</span>
                  </div>
                  <div class="kt-drill-card-body" role="button" tabindex="0" aria-label="{{ d.ariaLabel }}" onclick="{{ d.onStart }}">
                    <div class="kt-drill-card-title">{{ d.title }}</div>
                    <div class="kt-drill-bar"><span style="{{ d.barStyle }}"></span></div>
                    <div class="kt-drill-card-foot">
                      <span class="kt-drill-card-sub">{{ d.progressText }}</span>
                      <span class="kt-drill-card-go">{{ d.actionText }}</span>
                    </div>
                  </div>
                </div>
              </sc-for>
            </div>
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
          <span class="kt-drill-section">{{ drillView.section }}</span>
        </div>
        <div class="kt-drill-count">{{ drillView.countText }}</div>
      </div>
      <div class="kt-drill-bar kt-drill-bar-wide"><span style="{{ drillView.barStyle }}"></span></div>
      <sc-if value="{{ drillView.playing }}" hint-placeholder-val="{{ true }}">
        <div class="kt-drill-body">
          <div class="kt-drill-stage">
            <div class="kt-drill-prompt">{{ drillView.prompt }}</div>
            <sc-if value="{{ drillView.showAns }}" hint-placeholder-val="{{ true }}">
              <div class="kt-drill-ans" style="{{ drillView.ansStyle }}">{{ drillView.ansBox }}</div>
            </sc-if>
            <sc-if value="{{ drillView.showAsk }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-ask">よみかたを えらんでね</div>
            </sc-if>
            <sc-if value="{{ drillView.showHint }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-note is-hint" role="status">💡 {{ drillView.hint }}</div>
            </sc-if>
            <sc-if value="{{ drillView.showAnswer }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-note is-answer" role="status">こたえは {{ drillView.answerText }}</div>
              <div class="kt-drill-next" role="button" tabindex="0" onclick="{{ drillView.onNext }}">つぎへ ▶</div>
            </sc-if>
            <sc-if value="{{ drillView.showLast }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-last" style="{{ drillView.lastStyle }}">{{ drillView.lastMark }} {{ drillView.lastText }}</div>
            </sc-if>
          </div>
          <div class="kt-drill-side">
            <sc-if value="{{ drillView.showStreak }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-streak">🔥 {{ drillView.streakText }}</div>
            </sc-if>
            <sc-if value="{{ drillView.showPad }}" hint-placeholder-val="{{ true }}">
              <div class="kt-drill-pad" aria-label="数字入力パッド">
                <sc-for list="{{ drillView.pad }}" as="k" hint-placeholder-count="12">
                  <div role="button" tabindex="0" aria-label="{{ k.ariaLabel }}" onclick="{{ k.onClick }}" style="{{ k.style }}">{{ k.label }}</div>
                </sc-for>
              </div>
            </sc-if>
            <sc-if value="{{ drillView.showPick }}" hint-placeholder-val="{{ false }}">
              <div class="kt-drill-pick" aria-label="よみかたの えらびもんだい">
                <sc-for list="{{ drillView.picks }}" as="p" hint-placeholder-count="4">
                  <div class="kt-drill-pick-btn" role="button" tabindex="0" aria-label="{{ p.ariaLabel }}" onclick="{{ p.onClick }}"><span class="kt-drill-pick-no">{{ p.no }}</span><span>{{ p.label }}</span></div>
                </sc-for>
              </div>
            </sc-if>
          </div>
        </div>
      </sc-if>
      <sc-if value="{{ drillView.finished }}" hint-placeholder-val="{{ false }}">
        <div class="kt-drill-finish">
          <div class="kt-drill-finish-mark">🏅</div>
          <div class="kt-drill-finish-title">ぜんぶ おわったよ！</div>
          <div class="kt-drill-finish-body">{{ drillView.clearBody }}</div>
          <div class="kt-drill-finish-row">
            <div class="kt-drill-next" role="button" tabindex="0" onclick="{{ drillView.onRestart }}">もういちど</div>
            <div class="kt-drill-back" role="button" tabindex="0" onclick="{{ drillView.onExit }}">トップへ</div>
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
  .kt-hero-row{display:flex;flex-wrap:wrap;gap:14px;margin-top:22px;align-items:stretch;}
  .kt-hero-card{flex:1 1 200px;min-width:0;border:4px solid #f0e2c8;border-radius:22px;padding:14px 16px;word-break:keep-all;overflow-wrap:anywhere;}
  .kt-hero-weak{word-break:normal;overflow-wrap:break-word;}
  .kt-drill-grid{flex:2 1 420px;min-width:0;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;}
  .kt-drill-card{border-radius:18px;padding:8px 12px;display:flex;flex-direction:column;gap:2px;}
  .kt-drill-card-top{display:flex;align-items:center;justify-content:space-between;gap:6px;}
  .kt-drill-card-body{flex:1;display:flex;flex-direction:column;justify-content:center;cursor:pointer;}
  .kt-drill-card-kicker{font-size:13px;font-weight:700;}
  .kt-drill-card-title{font-size:19px;font-weight:900;line-height:1.15;}
  .kt-drill-card-foot{display:flex;align-items:baseline;justify-content:space-between;gap:8px;margin-top:2px;}
  .kt-drill-card-sub{font-size:13px;font-weight:700;}
  .kt-drill-card-go{flex:none;font-size:14px;font-weight:900;}
  .kt-drill-bar{height:8px;margin-top:5px;background:#fff;border:3px solid #ecd9b9;border-radius:10px;overflow:hidden;}
  .kt-drill-bar > span{display:block;height:100%;border-radius:8px;}
  .kt-drill-badge{flex:none;color:#fff;border-radius:14px;padding:2px 10px;font-size:14px;font-weight:900;}
  .kt-drill-reset{flex:none;background:#fff;border:2px solid #ecd9b9;border-radius:12px;padding:0 8px;font-size:12px;font-weight:700;color:#9a8662;cursor:pointer;}
  .kt-drill-screen{position:relative;min-height:100vh;display:flex;flex-direction:column;gap:12px;padding:22px 44px 30px;}
  .kt-drill-head{display:flex;align-items:center;gap:14px;}
  .kt-drill-quit{flex:none;background:#fff;border:3px solid #f0e2c8;border-radius:20px;padding:6px 14px;font-size:16px;font-weight:700;color:#6b5e45;cursor:pointer;}
  .kt-drill-titles{flex:1;min-width:0;display:flex;align-items:center;gap:10px;flex-wrap:wrap;}
  .kt-drill-title{font-size:22px;font-weight:900;}
  .kt-drill-section{background:#fff;border:3px solid #f0e2c8;border-radius:16px;padding:3px 12px;font-size:15px;font-weight:700;color:#6b5e45;}
  .kt-drill-count{flex:none;font-size:20px;font-weight:900;color:#6b5e45;}
  .kt-drill-bar-wide{height:16px;}
  .kt-drill-body{flex:1;display:flex;gap:28px;align-items:stretch;}
  .kt-drill-stage{flex:1;min-width:0;background:#fff;border:4px solid #f0e2c8;border-radius:26px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;padding:18px;}
  .kt-drill-prompt{font-size:72px;font-weight:900;line-height:1.15;letter-spacing:2px;color:#3a3326;text-align:center;}
  .kt-drill-ans{min-width:220px;background:#fff7ec;border:4px dashed #d8c4a0;border-radius:20px;padding:4px 24px;font-size:52px;font-weight:900;color:#3a3326;text-align:center;}
  .kt-drill-note{border-radius:16px;padding:9px 16px;font-size:19px;font-weight:700;text-align:center;}
  .kt-drill-note.is-hint{background:#fff6db;border:3px solid #ffd24a;color:#7a5d00;}
  .kt-drill-note.is-answer{background:#ffe0da;border:3px solid #e08a7a;color:#b23b23;font-size:28px;font-weight:900;}
  .kt-drill-last{background:#e8f7ec;border:3px solid #9dd3ae;border-radius:16px;padding:4px 14px;font-size:17px;font-weight:700;color:#2f6b41;}
  .kt-drill-side{width:300px;flex:none;display:flex;flex-direction:column;gap:10px;}
  .kt-drill-streak{background:#fff6db;border:3px solid #ffd24a;border-radius:18px;padding:6px 12px;text-align:center;font-size:17px;font-weight:900;color:#7a5d00;}
  .kt-drill-ask{font-size:22px;font-weight:700;color:#6b5e45;}
  .kt-drill-pad{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;align-content:start;}
  .kt-drill-pick{display:flex;flex-direction:column;gap:10px;align-content:start;}
  .kt-drill-pick-btn{display:flex;align-items:center;gap:12px;background:#fff;border:4px solid #f0e2c8;border-radius:20px;padding:8px 16px;font-size:30px;font-weight:900;color:#3a3326;box-shadow:0 4px 0 #ecd9b9;cursor:pointer;}
  .kt-drill-pick-no{flex:none;background:#fff3e0;border-radius:12px;padding:0 10px;font-size:17px;font-weight:900;color:#a1855a;}
  .kt-drill-next{background:#ff8a3d;color:#fff;border:4px solid #e07d2a;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;box-shadow:0 5px 0 #d96a26;cursor:pointer;}
  .kt-drill-back{background:#fff;color:#6b5e45;border:4px solid #f0e2c8;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;cursor:pointer;}
  .kt-drill-finish{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;}
  .kt-drill-finish-mark{font-size:96px;line-height:1;}
  .kt-drill-finish-title{font-size:44px;font-weight:900;}
  .kt-drill-finish-body{font-size:24px;font-weight:700;color:#6b5e45;}
  .kt-drill-finish-row{display:flex;gap:16px;margin-top:8px;}

  @media (max-width: 1100px) {
    .kt-hero-card{flex:1 1 44%;}
    .kt-drill-screen{padding:18px 26px 24px;}
    .kt-drill-side{width:250px;}
    .kt-drill-prompt{font-size:58px;}
  }

  @media (max-height: 820px) {
    .kt-hero-row{margin-top:14px;gap:10px;}
    .kt-hero-card{padding:10px 12px;}
    .kt-drill-grid{gap:8px;}
    .kt-drill-card{padding:6px 10px;}
    .kt-drill-card-title{font-size:17px;}
    .kt-drill-prompt{font-size:56px;}
    .kt-drill-ans{font-size:42px;min-width:180px;}
    .kt-drill-pick-btn{font-size:26px;padding:6px 14px;}
    .kt-drill-finish-mark{font-size:72px;}
    .kt-drill-finish-title{font-size:36px;}
  }

  @media (max-width: 800px) {
    .kt-hero-card{flex:1 1 100%;}
    .kt-drill-grid{flex:1 1 100%;grid-template-columns:minmax(0,1fr);}
    .kt-drill-body{flex-direction:column;}
    .kt-drill-side{width:100%;}
    .kt-drill-pad{grid-template-columns:repeat(6,minmax(0,1fr));}
  }
</style>
""".TrimEnd('\r', '\n');
    }
}
