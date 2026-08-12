namespace KidsTraining.App.Application.Learning.Markup;

/// <summary>
/// Adds the fixed arithmetic drill mode (とっくんモード) selected from the start screen (issues #62 and #64).
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
            "    drill:null, drillAsk:'', drillCourseChoice:'',\n    muted:false, speakingEnglish:'', setupName:''",
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
            "this.clearDrillEcho();" +
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
            "      isDrillMode:sc==='drill-mode', drillModeView:drillModeView,\n" +
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
            BuildDrillModeScreenTemplate() + "\n\n" + BuildDrillScreenTemplate() + "\n\n  <!-- ============ QUIZ ============ -->",
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
this._drillKeyHandler=e=>{if(e.repeat||e.isComposing||e.key==='Process'||e.ctrlKey||e.altKey||e.metaKey)return;const sc=this.state.screen;if(sc==='drill-mode'){const id=String(this.state.drillCourseChoice||'');if(e.key==='Escape'){e.preventDefault();this.cancelDrillMode();return;}if((e.key==='1'||e.key==='2')&&id){e.preventDefault();const kanji=id==='k1'||id==='k2',mode=e.key==='1'?(kanji?'reading':'input'):(kanji?'writing':'choice');this.startDrill(id,false,mode);}return;}if(sc!=='drill')return;const d=this.state.drill;if(!d||d.done)return;const roleButton=!!(e.target&&e.target.getAttribute&&(e.target.getAttribute('role')==='button'||e.target.tagName==='BUTTON'));if(roleButton&&(e.key==='Enter'||e.key===' '))return;if(d.echo){if(e.key==='Enter'||e.key===' '){e.preventDefault();this.drillFlushEcho();}return;}if(d.revealed){if(e.key==='Enter'||e.key===' '){e.preventDefault();this.drillNext();}return;}const base=this.drillQuestionAt(d,d.idx),dq=this.drillPresentedQuestion(d,base),choices=this.drillChoices(d,dq);if(choices.length){const choiceIndex=Number(e.key)-1;if(choiceIndex>=0&&choiceIndex<choices.length){e.preventDefault();this.drillChoose(choices[choiceIndex]);}return;}if(/^[0-9]$/.test(e.key)){e.preventDefault();this.press(e.key);return;}if(e.key==='Backspace'||e.key==='Delete'){e.preventDefault();this.del();return;}if(e.key!=='Enter'||!this.state.input)return;e.preventDefault();this.drillSubmit();};document.addEventListener('keydown',this._drillKeyHandler);
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillMethodsScript()
    {
        return """
  drillStorageKey(){return 'kt_drill_v1';}
  drillCourses(){return [
    {id:'g1',grade:1,total:200,badge:'1年生',title:'たしざん・ひきざん',sub:'1＋1から じゅんばんに 200もん',color:'#ff8a3d',edge:'#e07d2a',shade:'#fff3e6'},
    {id:'g2',grade:2,total:200,badge:'2年生',title:'かけざん（九九）',sub:'2のだんから じゅんばんに 200もん',color:'#4a9bf0',edge:'#2f7ccd',shade:'#eaf3ff'},
    {id:'k1',grade:1,total:200,badge:'1年生',title:'かんじ・ことば',sub:'1年生の 漢字と くみあわせた ことばを 200もん',color:'#5fbf7a',edge:'#3f9a5b',shade:'#eaf7ee'},
    {id:'k2',grade:2,total:200,badge:'2年生',title:'かんじ・ことば',sub:'1・2年生で ならった 漢字の ことばを 200もん',color:'#b07ae0',edge:'#8d55c4',shade:'#f4ecff'}
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
  drillKanjiWords(grade){
    const rows=grade===1?[
      ['一年','いちねん'],['一日','いちにち'],['一人','ひとり'],['二人','ふたり'],['三人','さんにん'],['四月','しがつ'],['五月','ごがつ'],['六月','ろくがつ'],['七月','しちがつ'],['八月','はちがつ'],['九月','くがつ'],['十月','じゅうがつ'],
      ['上下','じょうげ'],['左右','さゆう'],['大小','だいしょう'],['火山','かざん'],['青空','あおぞら'],['夕日','ゆうひ'],['入口','いりぐち'],['出口','でぐち'],['人口','じんこう'],['学校','がっこう'],['先生','せんせい'],['学年','がくねん'],
      ['正月','しょうがつ'],['文字','もじ'],['名字','みょうじ'],['本名','ほんみょう'],['小学校','しょうがっこう'],['中学校','ちゅうがっこう'],['大学','だいがく'],['男子','だんし'],['女子','じょし'],['王子','おうじ'],['手足','てあし'],['山林','さんりん'],
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
  buildKanjiDrillBank(id){
    const grade=id==='k1'?1:2,entries=this.drillKanjiEntries(grade),list=[],targets=[];
    entries.forEach(e=>{if(e.on)targets.push({e:e,type:'on',reading:e.on,word:e.k});if(e.kun)targets.push({e:e,type:'kun',reading:e.kun,word:e.kunWord||e.k});});
    const wordTargets=this.drillKanjiWords(grade).map(item=>({e:null,type:'word',reading:item.reading,word:item.word})),choiceTargets=targets.concat(wordTargets);
    const uniqueReadings=Array.from(new Set(choiceTargets.map(target=>target.reading)));
    const targetFor=(e,preferred)=>{const type=preferred==='on'&&e.on?'on':preferred==='kun'&&e.kun?'kun':e.on?'on':'kun';return{e:e,type:type,reading:type==='on'?e.on:e.kun,word:type==='on'?e.k:e.kunWord||e.k};};
    const ask=(sec,target,index)=>{
      const opts=[target.reading],base=Math.max(0,choiceTargets.findIndex(candidate=>candidate===target||(target.e&&candidate.e===target.e&&candidate.type===target.type)));
      [13,29,47].forEach(step=>{let j=(base+step)%choiceTargets.length,guard=0;while(guard<choiceTargets.length&&opts.indexOf(choiceTargets[j].reading)>=0){j=(j+1)%choiceTargets.length;guard++;}if(guard<choiceTargets.length)opts.push(choiceTargets[j].reading);});
      for(let i=0;i<uniqueReadings.length&&opts.length<4;i++)if(opts.indexOf(uniqueReadings[i])<0)opts.push(uniqueReadings[i]);
      const order=opts.slice(1);order.splice(index%4,0,target.reading);const label=target.type==='on'?'\u97f3\u8aad\u307f':target.type==='kun'?'\u8a13\u8aad\u307f':'\u3053\u3068\u3070';
      list.push({no:list.length+1,sec:sec,text:target.word,ans:target.reading,hint:label+'だよ。さいしょの もじは 「'+Array.from(target.reading)[0]+'」だよ。',kind:'pick',choices:order,kanji:target.e?target.e.k:'',kanjiWord:target.type==='word',readingType:target.type});
    };
    const A='\u3042\u305f\u3089\u3057\u3044 \u304b\u3093\u3058',B='\u306a\u3089\u3063\u305f \u304b\u3093\u3058\u306e \u3053\u3068\u3070',C='\u3057\u3042\u3052\u306e \u30df\u30c3\u30af\u30b9';
    const primary=entries.map((entry,index)=>targetFor(entry,index%2===0?'on':'kun'));
    for(let i=0;i<primary.length;i++)ask(A,primary[i],i);
    const extra=200-list.length;
    for(let i=0;i<extra;i++)ask(i<wordTargets.length?B:C,wordTargets[i%wordTargets.length],entries.length+i);
    return list;
  }
  buildDrillBank(id){
    if(id==='k1'||id==='k2')return this.buildKanjiDrillBank(id);
    const list=[],add=(sec,text,ans,hint,choices)=>{list.push({no:list.length+1,sec:sec,text:text,ans:ans,hint:hint,kind:choices?'pick':'num',choices:choices||null});};
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
  drillReadingLabel(q){return q&&q.readingType==='on'?'\u97f3\u8aad\u307f':q&&q.readingType==='kun'?'\u8a13\u8aad\u307f':'\u3053\u3068\u3070';}
  drillPrompt(q){return q&&q.readingType?q.text+' ['+this.drillReadingLabel(q)+']':(q?q.text:'');}
  drillPair(d,q){
    if(!q)return null;
    if(q.kind!=='pick')return {main:this.drillAnswerLine(q),sub:''};
    const writing=!!(d&&d.answerMode==='writing');
    return {main:writing?String(q.ans):String(q.text),sub:writing?String(q.text):String(q.ans)};
  }
  drillQuestionAt(d,index){
    if(!d)return null;const bank=this.drillBank(d.id),again=Array.isArray(d.again)?d.again:[];
    if(index<bank.length)return bank[index]||null;
    const extra=index-bank.length;
    return extra<again.length?(bank[again[extra]]||null):null;
  }
  drillQuestion(){const d=this.state.drill;return d?this.drillQuestionAt(d,d.idx):null;}
  selectDrillCourse(id){const course=this.drillCourse(id);if(!course)return;this.sfx('select');this.setState({screen:'drill-mode',drillCourseChoice:id});}
  cancelDrillMode(){this.sfx('tap');this.setState({screen:'start',drillCourseChoice:''});}
  drillChoiceOrder(total,seed){
    const count=Math.max(0,Math.floor(Number(total)||0)),order=[];for(let i=0;i<count;i++)order.push(i%2);
    let state=(Number(seed)>>>0)||0x6d2b79f5,next=()=>{state^=state<<13;state^=state>>>17;state^=state<<5;return state>>>0;};
    for(let i=order.length-1;i>0;i--){const j=next()%(i+1),value=order[i];order[i]=order[j];order[j]=value;}
    return order;
  }
  drillChoicePosition(d,q){const order=d&&Array.isArray(d.choiceOrder)?d.choiceOrder:[];if(order.length)return order[Math.max(0,Number(q&&q.no)-1)%order.length];return this.drillChoiceOrder(2,(Number(q&&q.no)||1)*2654435761)[0];}
  drillNumericChoices(d,q){
    if(!d||!q||q.kind!=='num')return [];
    const answer=Number(q.ans);if(!Number.isFinite(answer))return [];
    let distractor=null;
    if(d.id==='g2'&&String(q.text).indexOf('□')<0){const match=/^(\d+) × (\d+)$/.exec(String(q.text));if(match){const left=Number(match[1]),right=Number(match[2]),near=right<9?right+1:right-1;distractor=left*near;}}
    if(!Number.isFinite(distractor)||distractor===answer||distractor<0){distractor=Number(q.no)%2===0&&answer>0?answer-1:answer+1;}
    return this.drillChoicePosition(d,q)===0?[answer,distractor]:[distractor,answer];
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
  startDrill(id,restart,answerMode){
    const course=this.drillCourse(id);if(!course)return;
    const progress=this.readDrillProgress(),saved=progress[id]||this.drillDefaultRecord(),bank=this.drillBank(id);
    let idx=restart?0:this.clamp(saved.idx,0,bank.length);
    if(idx>=bank.length)idx=0;
    const fresh=restart||idx===0;
    const mode=id==='g1'||id==='g2'?(answerMode==='choice'?'choice':'input'):(answerMode==='writing'?'writing':'reading'),choiceSeed=this.rand(1,2147483646),choiceOrder=this.drillChoiceOrder(bank.length,choiceSeed);
    if(fresh){progress[id]={idx:0,perfect:0,mistakes:0,runs:saved.runs,best:saved.best};this.writeDrillProgress(progress);}
    this.clearDrillEcho();
    this.sfx('select');
    this.setState({screen:'drill',drillAsk:'',drillCourseChoice:'',input:'',drill:{id:id,answerMode:mode,choiceSeed:choiceSeed,choiceOrder:choiceOrder,idx:idx,miss:0,mark:'',hint:'',revealed:false,streak:0,perfect:fresh?0:saved.perfect,mistakes:fresh?0:saved.mistakes,again:[],counted:false,done:false,echo:null}});
  }
  drillAdvance(patch){
    const d=this.state.drill;if(!d)return;
    const total=this.drillBank(d.id).length,next=Object.assign({},d,{miss:0,mark:'',hint:'',revealed:false,echo:null},patch||{});
    next.idx=d.idx+1;
    const again=Array.isArray(next.again)?next.again:[],countRun=!d.counted&&next.idx>=total;
    if(countRun)next.counted=true;
    next.done=next.idx>=total+again.length;
    this.saveDrillProgress(next,countRun);
    if(next.done)this.sfx('clear');
    this.setState({drill:next,input:''});
  }
  drillEchoMs(){return 700;}
  clearDrillEcho(){if(this._drillEchoTimer){clearTimeout(this._drillEchoTimer);this._drillEchoTimer=null;}this._drillEchoPatch=null;}
  scheduleDrillAdvance(patch){
    this.clearDrillEcho();
    this._drillEchoPatch=patch;
    this._drillEchoTimer=setTimeout(()=>this.drillFlushEcho(),this.drillEchoMs());
  }
  drillFlushEcho(){
    const patch=this._drillEchoPatch,cur=this.state.drill;
    this.clearDrillEcho();
    if(!patch||this.state.screen!=='drill'||!cur||!cur.echo)return;
    this.drillAdvance(patch);
  }
  drillSubmit(){this.drillAnswerWith(this.state.input);}
  drillChoose(value){this.drillAnswerWith(value);}
  drillAnswerWith(value){
    const d=this.state.drill;if(!d||d.done||d.revealed||d.echo)return;
    const q=this.drillPresentedQuestion(d,this.drillQuestion());if(!q)return;
    const raw=String(value==null?'':value);if(!raw.length)return;
    if(this.drillMatches(q,raw)){
      const clean=d.miss===0,firstPass=d.idx<this.drillBank(d.id).length;
      this.sfx(clean&&d.streak>=4?'combo':'correct');
      this.setState({drill:Object.assign({},d,{mark:'',hint:'',echo:this.drillPair(d,q)})});
      this.scheduleDrillAdvance({perfect:d.perfect+(clean&&firstPass?1:0),streak:clean?d.streak+1:0});
      return;
    }
    this.sfx('wrong');
    const miss=d.miss+1,revealed=miss>=2;
    this.setState({input:'',drill:Object.assign({},d,{miss:miss,mistakes:d.mistakes+1,mark:revealed?'answer':'wrong',hint:q.hint,revealed:revealed,streak:0})});
  }
  drillNext(){
    const d=this.state.drill;if(!d||!d.revealed)return;
    const bank=this.drillBank(d.id),again=Array.isArray(d.again)?d.again.slice():[];
    if(d.idx<bank.length&&again.indexOf(d.idx)<0)again.push(d.idx);
    this.drillAdvance({again:again,streak:0});
  }
  exitDrill(){const d=this.state.drill;this.clearDrillEcho();this.saveDrillProgress(d,false);this.sfx('tap');this.setState({screen:'start',drill:null,drillAsk:'',drillCourseChoice:'',input:''});}
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
    let drillCards=[],drillModeView=null,drillView=null;
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
          onStart:()=>this.selectDrillCourse(c.id),
          onReset:()=>{if(this.state.drillAsk===c.id)this.confirmDrillReset(c.id);else this.askDrillReset(c.id);}
        };
      });
    }
    if(sc==='drill-mode'){
      const modeCourse=this.drillCourse(String(S.drillCourseChoice||''))||{};
      const kanjiMode=modeCourse.id==='k1'||modeCourse.id==='k2';
      drillModeView={
        badge:modeCourse.badge||'',title:modeCourse.title||'',headStyle:'background:'+(modeCourse.color||'#ff8a3d')+';',
        firstAria:kanjiMode?'1ばん、読みを選ぶ':'1ばん、数字を入力',firstSymbol:kanjiMode?'漢 → かん':'123',firstName:kanjiMode?'よみを えらぶ':'すうじを 入力',firstHelp:kanjiMode?'かんじの よみかたを 4つから':'じぶんで こたえを いれる',
        secondAria:kanjiMode?'2ばん、漢字を選ぶ':'2ばん、2つから選ぶ',secondSymbol:kanjiMode?'かん → 漢':'A / B',secondName:kanjiMode?'かんじを えらぶ':'2つから えらぶ',secondHelp:kanjiMode?'ただしい かきかたを 4つから':'ただしい こたえを えらぶ',
        onFirst:()=>this.startDrill(modeCourse.id,false,kanjiMode?'reading':'input'),onSecond:()=>this.startDrill(modeCourse.id,false,kanjiMode?'writing':'choice'),onBack:()=>this.cancelDrillMode()
      };
    }
    if(sc==='drill'&&S.drill){
      const d=S.drill,course=this.drillCourse(d.id)||{},total=this.drillBank(d.id).length,baseQuestion=this.drillQuestionAt(d,d.idx),dq=this.drillPresentedQuestion(d,baseQuestion);
      const dAgain=Array.isArray(d.again)?d.again:[],seen=Math.min(d.idx,total),inMain=d.idx<total;
      const drillPad=['1','2','3','4','5','6','7','8','9'].map(n=>({label:n,ariaLabel:n,style:keyTile,onClick:()=>this.press(n)}));
      drillPad.push({label:'けす',ariaLabel:'ひとつ けす',style:keyClear,onClick:()=>this.del()});
      drillPad.push({label:'0',ariaLabel:'0',style:keyTile,onClick:()=>this.press('0')});
      drillPad.push({label:'OK',ariaLabel:'こたえる',style:keyOk,onClick:()=>this.drillSubmit()});
      const dChoices=this.drillChoices(d,dq),dPick=dChoices.length>0,dKanji=!!(dq&&dq.kind==='pick'),dWriting=d.answerMode==='writing';
      // The pair is what has to be memorised, so it is shown in one fixed place: green while the
      // correct answer is echoed, red when the second mistake reveals it, and never anywhere else.
      const dEcho=d.echo&&typeof d.echo==='object'?d.echo:null,dPair=this.drillPair(d,dq)||{main:'',sub:''};
      const dSolved=!!dEcho||!!d.revealed,dAnswer=dq?String(dq.ans):'';
      const drillPicks=dChoices.map((choice,i)=>({
        no:String(i+1), label:String(choice), ariaLabel:(i+1)+'ばん、'+choice,
        style:dSolved&&String(choice)===dAnswer?'background:#e8f7ec; border-color:#3aa655; color:#22683c; box-shadow:0 4px 0 #9dd3ae;':'',
        onClick:()=>this.drillChoose(choice)}));
      drillView={
        badge:course.badge||'', title:course.title||'', section:dq?dq.sec:'',
        countText:inMain?(seen+' / '+total):('なおし '+(d.idx-total+1)+' / '+dAgain.length),
        headStyle:'background:'+(course.color||'#ff8a3d')+';',
        barStyle:'width:'+Math.round(this.clamp(seen/(total||1),0,1)*100)+'%; background:'+(course.color||'#ff8a3d')+';',
        prompt:dq?this.drillPrompt(dq):'', ansBox:S.input||'?',
        ansStyle:dEcho?'background:#e8f7ec; border-color:#3aa655; color:#22683c;':(d.mark?'border-color:#e08a7a; color:#b23b23;':''),
        showAns:!dPick, showAsk:dPick, askText:dWriting?'ただしい かんじを えらんでね':(dKanji?'よみかたを えらんでね':'こたえを 2つから えらんでね'),
        showPad:!dPick, showPick:dPick, pickAria:dWriting?'かんじの えらびもんだい':(dKanji?'よみかたの えらびもんだい':'こたえの 2たくもんだい'), picks:drillPicks,
        showHint:d.mark==='wrong', hint:d.hint||'',
        showEcho:!!dEcho, echoMain:dEcho?dEcho.main:'', echoSub:dEcho?dEcho.sub:'', echoHasSub:!!(dEcho&&dEcho.sub),
        showAnswer:!!d.revealed, answerMain:dPair.main, answerSub:dPair.sub, answerHasSub:!!dPair.sub,
        showStreak:d.streak>=3, streakText:'れんぞく '+d.streak+'もん！',
        playing:!d.done, finished:!!d.done,
        clearBody:d.perfect+' / '+total+'もんを 1かいめで せいかい',
        pad:drillPad,
        onNext:()=>this.drillNext(), onExit:()=>this.exitDrill(), onRestart:()=>this.startDrill(d.id,true,d.answerMode)
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

    private static string BuildDrillModeScreenTemplate()
    {
        return """
  <!-- ============ DRILL ANSWER MODE ============ -->
  <sc-if value="{{ isDrillMode }}" hint-placeholder-val="{{ false }}">
    <div data-screen-label="こたえかた" class="kt-drill-mode-screen">
      <button type="button" class="kt-drill-mode-back" aria-label="とっくんの いちらんに もどる" onclick="{{ drillModeView.onBack }}">← もどる</button>
      <div class="kt-drill-mode-panel">
        <span class="kt-drill-badge" style="{{ drillModeView.headStyle }}">{{ drillModeView.badge }}</span>
        <div class="kt-drill-mode-title">{{ drillModeView.title }}</div>
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
          <span class="kt-drill-section">{{ drillView.section }}</span>
        </div>
        <sc-if value="{{ drillView.showStreak }}" hint-placeholder-val="{{ false }}">
          <div class="kt-drill-streak">🔥 {{ drillView.streakText }}</div>
        </sc-if>
        <div class="kt-drill-count">{{ drillView.countText }}</div>
      </div>
      <div class="kt-drill-bar kt-drill-bar-wide"><span style="{{ drillView.barStyle }}"></span></div>
      <sc-if value="{{ drillView.playing }}" hint-placeholder-val="{{ true }}">
        <div class="kt-drill-body">
          <div class="kt-drill-stage">
            <div class="kt-drill-prompt">{{ drillView.prompt }}</div>
            <div class="kt-drill-answering">
              <sc-if value="{{ drillView.showAns }}" hint-placeholder-val="{{ true }}">
                <div class="kt-drill-ans" style="{{ drillView.ansStyle }}">{{ drillView.ansBox }}</div>
              </sc-if>
              <sc-if value="{{ drillView.showPick }}" hint-placeholder-val="{{ false }}">
                <div class="kt-drill-ask">{{ drillView.askText }}</div>
                <div class="kt-drill-pick" aria-label="{{ drillView.pickAria }}">
                  <sc-for list="{{ drillView.picks }}" as="p" hint-placeholder-count="4">
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
                <div class="kt-drill-next" role="button" tabindex="0" onclick="{{ drillView.onNext }}">つぎへ ▶</div>
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
  .kt-drill-answering{width:100%;display:flex;flex-direction:column;align-items:center;gap:10px;}
  .kt-drill-ans{min-width:220px;background:#fff7ec;border:4px dashed #d8c4a0;border-radius:20px;padding:4px 24px;font-size:52px;font-weight:900;color:#3a3326;text-align:center;}
  /* The feedback slot keeps its height while it is empty, hinting, or showing the pair, so the
     question above it never moves across the 200 questions of a course. */
  .kt-drill-feedback{width:100%;min-height:248px;display:flex;flex-direction:column;align-items:center;justify-content:flex-start;gap:12px;}
  .kt-drill-note{border-radius:16px;padding:9px 16px;font-size:19px;font-weight:700;text-align:center;}
  .kt-drill-note.is-hint{background:#fff6db;border:3px solid #ffd24a;color:#7a5d00;}
  .kt-drill-pair{min-width:300px;display:flex;flex-direction:column;align-items:center;gap:2px;border-radius:20px;padding:10px 32px;text-align:center;}
  .kt-drill-pair.is-ok{background:#e8f7ec;border:4px solid #3aa655;color:#22683c;}
  .kt-drill-pair.is-answer{background:#ffe0da;border:4px solid #e08a7a;color:#b23b23;}
  .kt-drill-pair-label{font-size:17px;font-weight:700;}
  .kt-drill-pair-main{font-size:44px;font-weight:900;line-height:1.15;letter-spacing:1px;}
  .kt-drill-pair-sub{font-size:32px;font-weight:900;line-height:1.2;}
  .kt-drill-side{width:300px;flex:none;display:flex;flex-direction:column;gap:10px;}
  .kt-drill-streak{flex:none;background:#fff6db;border:3px solid #ffd24a;border-radius:18px;padding:4px 12px;text-align:center;font-size:16px;font-weight:900;color:#7a5d00;white-space:nowrap;}
  .kt-drill-ask{font-size:20px;font-weight:700;color:#6b5e45;}
  .kt-drill-pad{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;align-content:start;}
  /* The choices sit directly under the question in two columns: two-choice arithmetic fills one row
     and the four-choice kanji questions fill a 2x2 block, so both stay in a single glance. */
  .kt-drill-pick{width:100%;max-width:720px;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px;}
  .kt-drill-pick-btn{position:relative;min-height:84px;display:flex;align-items:center;justify-content:center;background:#fff;border:4px solid #f0e2c8;border-radius:20px;padding:8px 44px;font-size:38px;font-weight:900;color:#3a3326;box-shadow:0 4px 0 #ecd9b9;cursor:pointer;overflow-wrap:anywhere;}
  .kt-drill-pick-no{position:absolute;left:12px;top:50%;transform:translateY(-50%);background:#fff3e0;border-radius:12px;padding:0 10px;font-size:16px;font-weight:900;color:#a1855a;}
  .kt-drill-next{background:#ff8a3d;color:#fff;border:4px solid #e07d2a;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;box-shadow:0 5px 0 #d96a26;cursor:pointer;}
  .kt-drill-back{background:#fff;color:#6b5e45;border:4px solid #f0e2c8;border-radius:22px;padding:8px 28px;font-size:26px;font-weight:900;cursor:pointer;}
  .kt-drill-finish{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:14px;}
  .kt-drill-finish-mark{font-size:96px;line-height:1;}
  .kt-drill-finish-title{font-size:44px;font-weight:900;}
  .kt-drill-finish-body{font-size:24px;font-weight:700;color:#6b5e45;}
  .kt-drill-finish-row{display:flex;gap:16px;margin-top:8px;}
  .kt-drill-mode-option:hover,.kt-drill-pick-btn:hover{border-color:#ffb170;background:#fffaf4;transform:translateY(-2px);box-shadow:0 8px 0 #ecd9b9;}
  .kt-drill-mode-option:active,.kt-drill-pick-btn:active{transform:translateY(3px);box-shadow:0 2px 0 #dcc49c;}
  .kt-drill-mode-back:hover,.kt-drill-quit:hover,.kt-drill-back:hover,.kt-drill-reset:hover{background:#fff8ee;border-color:#d9bd8c;}
  .kt-drill-mode-back:active,.kt-drill-quit:active,.kt-drill-back:active,.kt-drill-reset:active{transform:translateY(2px);}
  .kt-drill-mode-option:focus-visible,.kt-drill-mode-back:focus-visible,.kt-drill-card-body:focus-visible,.kt-drill-reset:focus-visible,.kt-drill-quit:focus-visible,.kt-drill-pick-btn:focus-visible,.kt-drill-next:focus-visible,.kt-drill-back:focus-visible,.kt-drill-pad [role="button"]:focus-visible{outline:4px solid #2f7ccd;outline-offset:3px;}

  @media (max-width: 1100px) {
    .kt-hero-card{flex:1 1 44%;}
    .kt-drill-screen{padding:18px 26px 24px;}
    .kt-drill-side{width:250px;}
    .kt-drill-prompt{font-size:58px;}
    .kt-drill-pick-btn{font-size:34px;}
  }

  @media (max-height: 820px) {
    .kt-hero-row{margin-top:14px;gap:10px;}
    .kt-hero-card{padding:10px 12px;}
    .kt-drill-grid{gap:8px;}
    .kt-drill-card{padding:6px 10px;}
    .kt-drill-card-title{font-size:17px;}
    .kt-drill-stage{gap:10px;}
    .kt-drill-prompt{font-size:56px;}
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
    .kt-drill-mode-symbol{font-size:36px;}
  }

  @media (max-width: 800px) {
    .kt-hero-card{flex:1 1 100%;}
    .kt-drill-grid{flex:1 1 100%;grid-template-columns:minmax(0,1fr);}
    .kt-drill-body{flex-direction:column;}
    .kt-drill-side{width:100%;}
    .kt-drill-pad{grid-template-columns:repeat(6,minmax(0,1fr));}
    .kt-drill-mode-screen{padding:18px 20px 24px;}
    .kt-drill-mode-question{font-size:30px;}
    .kt-drill-mode-options{grid-template-columns:minmax(0,1fr);gap:14px;}
    .kt-drill-mode-option{min-height:150px;}
  }

  @media (prefers-reduced-motion: reduce) {
    .kt-drill-mode-option{transition:none;}
  }
</style>
""".TrimEnd('\r', '\n');
    }
}
