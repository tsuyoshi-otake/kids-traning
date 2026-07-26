namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchKeyboardQuestion(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            BuildKeyboardMethodsScript() + "\n  freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null,typed:'',typeMiss:0};}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "componentDidMount(){this._keyActivate=e=>{if((e.key==='Enter'||e.key===' ')&&e.target&&e.target.getAttribute&&e.target.getAttribute('role')==='button'){e.preventDefault();e.target.click();}};document.addEventListener('keydown',this._keyActivate);\n    let profiles=this.state.profiles;",
            "componentDidMount(){this._keyActivate=e=>{if((e.key==='Enter'||e.key===' ')&&e.target&&e.target.getAttribute&&e.target.getAttribute('role')==='button'){e.preventDefault();e.target.click();}};document.addEventListener('keydown',this._keyActivate);this._typeKeyHandler=e=>{if(e.repeat||e.isComposing||e.key==='Process'||e.ctrlKey||e.altKey||e.metaKey)return;if(!/^[a-zA-Z]$/.test(e.key))return;if(this.state.screen!=='quiz'||!this.state.session)return;const q=this.cur();if(!q||q.mode!=='type')return;e.preventDefault();this.typeKey(e.key);};document.addEventListener('keydown',this._typeKeyHandler);\n    let profiles=this.state.profiles;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);this.stopEnglishSpeech();}",
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);if(this._typeKeyHandler)document.removeEventListener('keydown',this._typeKeyHandler);this.stopEnglishSpeech();}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "let q=null,choices=[],practicePrompt='',modeNumeric=false,modeChoices=false,modeHissan=false,isHissan=false,isPlainEq=false,isClock=false,topicLabel='',topicColor='#ccc',isWeakTopic=false,clockHourStyle='',clockMinStyle='';",
            "let q=null,choices=[],practicePrompt='',modeNumeric=false,modeChoices=false,modeHissan=false,modeTyping=false,isHissan=false,isPlainEq=false,isClock=false,topicLabel='',topicColor='#ccc',isWeakTopic=false,clockHourStyle='',clockMinStyle='';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[],typeSlots=[],typeKana='',typeHint='',typeShowHint=false,typeShowBoard=false,typeKeyRows=[];",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "modeNumeric=q.mode==='num';modeChoices=q.mode==='choices';modeHissan=q.mode==='hissan-steps';isPlainEq=modeNumeric&&q.topic!=='story';isClock=!!q.isClock;",
            "modeNumeric=q.mode==='num';modeChoices=q.mode==='choices';modeHissan=q.mode==='hissan-steps';modeTyping=q.mode==='type';isPlainEq=modeNumeric&&q.topic!=='story';isClock=!!q.isClock;" + BuildKeyboardRenderScript(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, isHissan:false, isPlainEq:isPlainEq, isClock:isClock,",
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, modeTyping:modeTyping, isHissan:false, isPlainEq:isPlainEq, isClock:isClock, typeSlots:typeSlots, typeKana:typeKana, typeHint:typeHint, typeShowHint:typeShowHint, typeShowBoard:typeShowBoard, typeKeyRows:typeKeyRows,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      <!-- CHOICES -->",
            BuildKeyboardTemplate() + "\n\n      <!-- CHOICES -->",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildKeyboardMethodsScript()
    {
        return """
pickKeyboard(p){const stage=this.topicStage(p,'keyboard'),make=(answer,kana,alternatives=[])=>({topic:'keyboard',mode:'type',kana:kana,prompt:kana||answer,answer:answer,acceptedAnswers:[answer].concat(alternatives),explanation:kana?'「'+kana+'」は ローマじで「'+answer+'」。':'「'+answer+'」の キーを おそう。'}),word=pool=>()=>{const item=pool[this.rand(0,pool.length-1)];return make(item[0],item[1],item.slice(2));},letters='abcdefghijklmnopqrstuvwxyz';const buckets=[
    [()=>make(letters.charAt(this.rand(0,letters.length-1)),'')],
    [word([['te','て'],['me','め'],['ha','は'],['hi','ひ'],['ki','き'],['ka','か'],['yu','ゆ'],['ho','ほ'],['wa','わ'],['mi','み'],['su','す'],['to','と'],['ta','た'],['ni','に'],['ne','ね'],['ya','や'],['ko','こ'],['na','な'],['shi','し','si'],['chi','ち','ti'],['tsu','つ','tu'],['fu','ふ','hu']])],
    [word([['inu','いぬ'],['kao','かお'],['aka','あか'],['ike','いけ'],['uta','うた'],['ame','あめ'],['umi','うみ'],['eki','えき'],['oka','おか'],['uma','うま'],['kai','かい'],['ari','あり'],['ito','いと'],['asa','あさ'],['isu','いす']])],
    [word([['neko','ねこ'],['sora','そら'],['yama','やま'],['kasa','かさ'],['kani','かに'],['kame','かめ'],['momo','もも'],['mimi','みみ'],['hana','はな'],['tori','とり'],['kuma','くま'],['buta','ぶた'],['tako','たこ'],['yuki','ゆき'],['kaze','かぜ'],['niwa','にわ'],['hako','はこ'],['mado','まど'],['yubi','ゆび']])],
    [word([['usagi','うさぎ'],['kaeru','かえる'],['tokei','とけい'],['budou','ぶどう'],['ringo','りんご'],['mikan','みかん'],['otona','おとな'],['inaka','いなか'],['obake','おばけ'],['unagi','うなぎ']])]
  ];return this.pickStage(stage,buckets,0);}
  normalizeRomajiInput(value){return String(value||'').trim().toLowerCase().replace(/sya/g,'sha').replace(/syu/g,'shu').replace(/syo/g,'sho').replace(/(?:tya|cya)/g,'cha').replace(/(?:tyu|cyu)/g,'chu').replace(/(?:tyo|cyo)/g,'cho').replace(/(?:zya|jya)/g,'ja').replace(/(?:zyu|jyu)/g,'ju').replace(/(?:zyo|jyo)/g,'jo').replace(/si/g,'shi').replace(/ti/g,'chi').replace(/tu/g,'tsu').replace(/hu/g,'fu').replace(/zi/g,'ji');}
  romajiInputEquivalent(left,right){return this.normalizeRomajiInput(left)===this.normalizeRomajiInput(right);}
  typeKey(ch){const q=this.cur(),typed=String(this.state.typed||'').toLowerCase(),answers=[...new Set((q&&q.acceptedAnswers||[q&&q.answer]).map(value=>String(value||'').toLowerCase()))],key=String(ch||'').toLowerCase(),next=typed+key;if(!answers.some(answer=>answer.startsWith(next))){this.sfx('wrong');this.setState({typeMiss:(this.state.typeMiss||0)+1});return;}if(answers.includes(next)){this.setState({typed:next},()=>this.finishTyping());return;}this.sfx('step');this.setState({typed:next});}
  finishTyping(){const q=this.cur(),p=this.curP(),perfect=(this.state.typeMiss||0)===0;this.recordEvidence(p,q,perfect?'independent':'assisted');const combo=perfect?this.state.combo+1:0,stars=perfect?(combo>=3?2:1):1,xpInfo=this.gainXp(p,perfect?(combo>=3?18:12):6);p.stars+=stars;this.sfx(perfect&&combo>=3?'combo':'correct');this.setState({screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,helped:!perfect,xp:xpInfo.amount,levelUp:xpInfo.levelUp},typed:'',typeMiss:0});}
""";
    }

    private static string BuildKeyboardRenderScript()
    {
        return """
if(modeTyping){const canonical=String(q.answer||'').toLowerCase(),typed=String(S.typed||''),answers=[...new Set((q.acceptedAnswers||[canonical]).map(value=>String(value).toLowerCase()))],answer=answers.find(value=>value.startsWith(typed))||canonical;typeKana=q.kana||q.prompt||'';typeSlots=answer.split('').map((ch,i)=>{const done=i<typed.length,current=i===typed.length,style='width:64px;height:72px;border-radius:16px;display:flex;align-items:center;justify-content:center;font-size:42px;font-weight:900;text-transform:uppercase;'+(done?'background:#eafaef;border:3px solid #3aa655;color:#2f7d44;':(current?'background:#fff7ec;border:4px dashed #ff8a3d;color:#c96c1a;':'background:#fff;border:3px dashed #d8c4a0;color:#8d7a5a;'));return{text:ch,style:style};});const nextKeys=[...new Set(answers.filter(value=>value.startsWith(typed)).map(value=>value.charAt(typed.length)).filter(Boolean))],next=nextKeys[0]||'';typeShowHint=(S.typeMiss||0)>=2&&nextKeys.length>0;typeHint=nextKeys.length?'つぎは「'+nextKeys.join(' / ')+'」だよ':'';typeShowBoard=this.topicStage(p,'keyboard')<=2;if(typeShowBoard){const rows=['qwertyuiop','asdfghjkl','zxcvbnm'],fingerColors={little:{background:'#36c8ae',border:'#117a70'},ring:{background:'#f06a4c',border:'#a83a26'},middle:{background:'#5795da',border:'#275f9e'},index:{background:'#f5b82e',border:'#a96f00'}},fingerFor=key=>'qazp'.includes(key)?'little':('wsxol'.includes(key)?'ring':('edcik'.includes(key)?'middle':'index'));typeKeyRows=rows.map((row,ri)=>({style:'display:flex;gap:7px;justify-content:center;margin-left:'+(ri*22)+'px;',keys:row.split('').map(key=>{const colors=fingerColors[fingerFor(key)],active=nextKeys.includes(key);return{label:key,style:'width:48px;height:48px;border-radius:10px;display:flex;align-items:center;justify-content:center;font-size:23px;font-weight:900;text-transform:uppercase;background:'+colors.background+';border:3px solid '+colors.border+';color:#17202a;box-shadow:0 3px 0 '+colors.border+';'+(active?'outline:4px solid #fff;outline-offset:2px;transform:translateY(-4px);box-shadow:0 7px 0 '+colors.border+',0 0 0 7px #17202a;':'')};})}));}}
""";
    }

    private static string BuildKeyboardTemplate()
    {
        return """
      <!-- PHYSICAL KEYBOARD TYPING -->
      <sc-if value="{{ modeTyping }}" hint-placeholder-val="{{ false }}">
        <div style="flex:1; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:18px; margin-top:8px;">
          <div style="font-size:22px; color:#0f766e; font-weight:900;">キーボードで じゅんばんに うとう</div>
          <div style="font-size:56px; line-height:1.2; font-weight:900; color:#3a3326; min-height:68px;">{{ typeKana }}</div>
          <div style="display:flex; gap:12px; justify-content:center; flex-wrap:wrap;">
            <sc-for list="{{ typeSlots }}" as="slot" hint-placeholder-count="5"><div style="{{ slot.style }}">{{ slot.text }}</div></sc-for>
          </div>
          <sc-if value="{{ typeShowHint }}" hint-placeholder-val="{{ false }}"><div style="background:#fff6db; border:3px solid #ffd24a; border-radius:16px; padding:10px 18px; font-size:21px; color:#7a5d00; font-weight:900;">💡 {{ typeHint }}</div></sc-if>
          <sc-if value="{{ typeShowBoard }}" hint-placeholder-val="{{ false }}">
            <div role="img" aria-label="指ごとに色分けしたキーボード。白い枠のキーが次に押すキーです。" style="background:#242832; border:4px solid #11141b; border-radius:20px; padding:16px 22px; display:flex; flex-direction:column; gap:9px; max-width:94vw; overflow:hidden; box-shadow:0 6px 0 #11141b;">
              <div style="color:#fff; font-size:16px; font-weight:900; text-align:center; margin-bottom:3px;">おなじ いろを さがして、白い わくの キーを おそう</div>
              <sc-for list="{{ typeKeyRows }}" as="row" hint-placeholder-count="3"><div style="{{ row.style }}"><sc-for list="{{ row.keys }}" as="key" hint-placeholder-count="10"><div style="{{ key.style }}">{{ key.label }}</div></sc-for></div></sc-for>
            </div>
          </sc-if>
        </div>
      </sc-if>
""";
    }
}
