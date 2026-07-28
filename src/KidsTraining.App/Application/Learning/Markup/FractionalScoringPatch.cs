namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchFractionalScoring(string markup)
    {
        markup = ReplaceBlock(markup, "typeKey(ch){", "\n  freshQ(){", BuildFractionalTypingScript());
        markup = ReplaceBlock(markup, "freshQ(){", "\n  numChoicesFor(", BuildFractionalFreshQuestionScript());
        markup = ReplaceBlock(markup, "checkNumeric(val){", "\n  finishNumeric(){", BuildFractionalNumericCheckScript());
        markup = ReplaceBlock(markup, "finishNumeric(){", "\n  submit(ans){", "finishNumeric(){const q=this.cur();this.finishScoredQuestion(q,this.state.numMiss||0);}");
        markup = ReplaceBlock(markup, "submit(ans){", "\n  next(){", BuildFractionalChoiceSubmitScript());
        markup = ReplaceBlock(markup, "next(){", "\n  retry(){", BuildFractionalNextScript());
        markup = ReplaceBlock(markup, "submitHissanStep(val){", "\n  unlockPC(){", BuildFractionalHissanScript());
        markup = ReplaceBlock(markup, "revealAnswer(){", "\n  englishSpeechAvailable(", BuildFractionalRevealScript());

        markup = ReplaceRequired(
            markup,
            "const total=this.total();\n    const prog=S.session?Math.round(S.session.idx/total*100):0;",
            "const total=S.session&&Array.isArray(S.session.rolePlan)?S.session.rolePlan.length:this.total();\n    const questionMistakes=modeWrittenSteps?(S.waMistakes||0):modeNumeric?(S.numMiss||0):modeChoices?(S.choiceMiss||0):modeHissan?(S.hsMistakes||0):modeTyping?(S.typeMiss||0):0;\n    const attemptScore=this.formatScore(this.scoreForMistakes(questionMistakes));\n    const prog=S.session?Math.round(S.session.idx/total*100):0;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "total:total, passLine:this.passLine(),",
            "total:total, passLine:this.passLine(), hasAttemptNotice:questionMistakes>0, attemptNotice:questionMistakes>0?('あと '+(3-questionMistakes)+'かい・いま せいかいで '+attemptScore+'てん'):'',",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "fbStarText:lr.stars||0,",
            "fbScore:this.formatScore(lr.points||0), fbAutoAdvance:!!lr.exhausted, fbStarText:lr.stars||0,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "clearCorrect:sess.correct||0, earnedStars:earned,",
            "clearCorrect:this.formatScore(sess.correct||0), earnedStars:earned,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "retryRemaining:Math.max(0,this.passLine()-(sess.correct||0)),",
            "retryRemaining:this.formatScore(Math.max(0,this.passLine()-(Number(sess.correct)||0))),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      <!-- NUMERIC -->",
            """
      <sc-if value="{{ hasAttemptNotice }}" hint-placeholder-val="{{ false }}">
        <div role="status" style="align-self:center; margin-top:10px; background:#fff6db; border:3px solid #ffd24a; border-radius:18px; padding:8px 18px; color:#7a5d00; font-size:18px; font-weight:900;">{{ attemptNotice }}</div>
      </sc-if>

      <!-- NUMERIC -->
""",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            """
      <sc-if value="{{ fbCorrect }}" hint-placeholder-val="{{ false }}">
        <div class="kt-feedback-rewards">
""",
            """
      <div class="kt-feedback-score">この もんだい：<b>{{ fbScore }}てん</b></div>
      <sc-if value="{{ fbAutoAdvance }}" hint-placeholder-val="{{ false }}">
        <div class="kt-feedback-auto-advance" role="status">こたえを かくにんして、つぎへ すすみます</div>
      </sc-if>

      <sc-if value="{{ fbCorrect }}" hint-placeholder-val="{{ false }}">
        <div class="kt-feedback-rewards">
""",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup, "{{ clearCorrect }} / {{ total }}</b> せいかい", "{{ clearCorrect }} / {{ total }}</b> てん", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "{{ clearCorrect }} / {{ total }} せいかい ・ ごうかくまで あと <b>{{ retryRemaining }}もん</b>", "{{ clearCorrect }} / {{ total }} てん ・ ごうかくまで あと <b>{{ retryRemaining }}てん</b>", StringComparison.Ordinal);
        return markup;
    }

    private static string BuildFractionalTypingScript() => """
scoreForMistakes(misses){const n=Math.max(0,Math.min(3,Number(misses)||0));return n===0?1:(n===1?0.5:(n===2?0.25:0));}
  formatScore(value){const n=Math.max(0,Number(value)||0);return Number.isInteger(n)?String(n):n.toFixed(2).replace(/0+$/,'').replace(/\.$/,'');}
  currentQuestionToken(){const s=this.state.session;return s?String(s.attempt||1)+':'+String(s.idx||0):'';}
  claimQuestionTerminal(){const token=this.currentQuestionToken();if(!token||this._terminalQuestionToken===token)return false;this._terminalQuestionToken=token;return true;}
  clearAutoAdvance(){if(this._autoAdvanceTimer){clearTimeout(this._autoAdvanceTimer);this._autoAdvanceTimer=null;}}
  scheduleAutoAdvance(){this.clearAutoAdvance();const token=this.currentQuestionToken();this._autoAdvanceTimer=setTimeout(()=>{this._autoAdvanceTimer=null;if(this.state.screen==='feedback'&&this.currentQuestionToken()===token&&this.state.lastResult&&this.state.lastResult.exhausted)this.next();},1800);}
  finishScoredQuestion(q,misses,options){if(!this.claimQuestionTerminal())return;const p=this.curP(),points=this.scoreForMistakes(misses),independent=points===1,combo=independent?this.state.combo+1:0,stars=independent?(combo>=3?2:1):1,opts=options||{},perfectXp=Number(opts.perfectXp)||12,comboXp=Number(opts.comboXp)||18,assistedXp=Number(opts.assistedXp)||6,xpInfo=this.gainXp(p,independent?(combo>=3?comboXp:perfectXp):assistedXp);this.recordEvidence(p,q,independent?'independent':'assisted',points);p.stars+=stars;this.sfx(independent&&combo>=3?'combo':'correct');this.setState({screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,helped:!independent,viaSteps:!!opts.viaSteps,perfect:independent,points:points,xp:xpInfo.amount,levelUp:xpInfo.levelUp},input:'',numChoices:null,hsStepChoices:null,waStepChoices:null,waHint:'',waError:'',typed:'',attemptNotice:''});}
  exhaustQuestion(userAns){const q=this.cur();if(!q||!this.claimQuestionTerminal())return;const p=this.curP();this.recordEvidence(p,q,'incorrect',0);this.sfx('wrong');this.setState({screen:'feedback',combo:0,lastResult:{correct:false,exhausted:true,q:q,userAns:userAns==null?'':String(userAns),stars:0,combo:0,points:0,xp:0,levelUp:false},input:'',numChoices:null,hsStepChoices:null,waStepChoices:null,waHint:'',waError:'',typed:'',attemptNotice:''},()=>this.scheduleAutoAdvance());}
  typeKey(ch){const q=this.cur(),answer=String(q&&q.answer||'').toLowerCase(),typed=String(this.state.typed||'');if(typed.length>=answer.length||this._terminalQuestionToken===this.currentQuestionToken())return;const key=String(ch||'').toLowerCase();if(key!==answer.charAt(typed.length)){const miss=(this.state.typeMiss||0)+1;if(miss>=3){this.exhaustQuestion(typed+key);return;}this.sfx('wrong');this.setState({typeMiss:miss,combo:0});return;}const next=typed+key;if(next.length>=answer.length){this.setState({typed:next},()=>this.finishTyping());return;}this.sfx('step');this.setState({typed:next});}
  finishTyping(){const q=this.cur();this.finishScoredQuestion(q,this.state.typeMiss||0);}
""";

    private static string BuildFractionalFreshQuestionScript() => """
freshQ(){this.clearAutoAdvance();this._terminalQuestionToken='';this._answerBusy=false;return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null,waStep:0,waMistakes:0,waStepMiss:0,waStepChoices:null,waHint:'',waError:'',typed:'',typeMiss:0,choiceMiss:0,attemptNotice:''};}
""";

    private static string BuildFractionalNumericCheckScript() => """
checkNumeric(val){if(this._answerBusy||this._terminalQuestionToken===this.currentQuestionToken())return;this._answerBusy=true;const q=this.cur();if(String(val)===String(q.answer)){this._answerBusy=false;this.finishNumeric();return;}const miss=(this.state.numMiss||0)+1;if(miss>=3){this._answerBusy=false;this.exhaustQuestion(val);return;}this.sfx('wrong');const upd={numMiss:miss,input:'',combo:0,hsHint:miss===1?'もういちど よく みて かんがえよう':'えらんで こたえても いいよ'};if(miss>=2)upd.numChoices=this.numChoicesFor(q.answer);this.setState(upd,()=>{this._answerBusy=false;});}
""";

    private static string BuildFractionalChoiceSubmitScript() => """
submit(ans){this.stopEnglishSpeech();if(this._answerBusy||this._terminalQuestionToken===this.currentQuestionToken())return;this._answerBusy=true;const q=this.cur(),correct=String(ans)===String(q.answer);if(correct){this._answerBusy=false;this.finishScoredQuestion(q,this.state.choiceMiss||0);return;}const miss=(this.state.choiceMiss||0)+1;if(miss>=3){this._answerBusy=false;this.exhaustQuestion(ans);return;}this.sfx('wrong');this.setState({choiceMiss:miss,combo:0},()=>{this._answerBusy=false;});}
""";

    private static string BuildFractionalNextScript() => """
next(){this.stopEnglishSpeech();if(this._advanceBusy)return;this._advanceBusy=true;this.clearAutoAdvance();this.sfx('select');const s=this.state.session;if(!s){this._advanceBusy=false;return;}if(s.idx>=s.rolePlan.length-1){const globalPass=(Number(s.correct)||0)>=this.passLine(),targetPass=s.targetAsked>=4&&s.targetIndependent/s.targetAsked>=.7,pass=globalPass&&targetPass;if(pass)setTimeout(()=>this.sfx('clear'),280);this._terminalQuestionToken='';if(typeof this.clearLearningCheckpoint==='function')this.clearLearningCheckpoint();this.setState({screen:pass?'clear':'retry'},()=>{this._advanceBusy=false;});}else{const nextIndex=s.idx+1,p=this.curP(),q=this.generateSessionQuestion(p,s,s.rolePlan[nextIndex]);s.questions[nextIndex]=q;s.idx=nextIndex;const fresh=this.freshQ();this.setState({screen:'quiz',...fresh},()=>{this._advanceBusy=false;});}}
""";

    private static string BuildFractionalHissanScript() => """
  submitHissanStep(val){if(this._answerBusy||this._terminalQuestionToken===this.currentQuestionToken())return;this._answerBusy=true;const q=this.cur(),st=q.steps[this.state.hsStep],v=val!=null?val:this.state.input;if(v!==st.expect){const totalMiss=(this.state.hsMistakes||0)+1;if(totalMiss>=3){this._answerBusy=false;this.exhaustQuestion(v);return;}this.sfx('wrong');const stepMiss=(this.state.hsStepMiss||0)+1,upd={hsHint:st.explain,input:'',combo:0,hsMistakes:totalMiss,hsStepMiss:stepMiss};if(stepMiss>=2)upd.hsStepChoices=this.numChoicesFor(st.expect);this.setState(upd,()=>{this._answerBusy=false;});return;}const ns={input:'',hsHint:'',hsStepMiss:0,hsStepChoices:null};if(st.place==='ones'){ns.hsOnes=st.writeOnes;if(st.carry)ns.hsCarry=true;if(st.borrow)ns.hsBorrow=true;}else ns.hsTens=st.writeTens;const last=this.state.hsStep>=q.steps.length-1;if(last){this._answerBusy=false;this.setState(ns,()=>this.finishScoredQuestion(q,this.state.hsMistakes||0,{viaSteps:true,perfectXp:14,comboXp:20,assistedXp:8}));}else{this.sfx('step');this.setState({...ns,hsStep:this.state.hsStep+1},()=>{this._answerBusy=false;});}}
  submitWrittenStep(val){if(this._answerBusy||this._terminalQuestionToken===this.currentQuestionToken())return;this._answerBusy=true;const q=this.cur(),plan=q&&typeof this.writtenArithmeticPlan==='function'?this.writtenArithmeticPlan(q):null,index=Number(this.state.waStep)||0,st=plan&&plan.steps[index],v=String(val!=null?val:this.state.input||'');if(!plan||!st){this._answerBusy=false;this.setState({waError:'この途中式を続けられません。答えと説明を確認します。'},()=>this.revealAnswer());return;}if(v!==String(st.expect)){const totalMiss=(this.state.waMistakes||0)+1;if(totalMiss>=3){this._answerBusy=false;this.exhaustQuestion(v);return;}this.sfx('wrong');const stepMiss=(this.state.waStepMiss||0)+1,upd={waHint:st.explain,input:'',combo:0,waMistakes:totalMiss,waStepMiss:stepMiss,waError:''};if(stepMiss>=2)upd.waStepChoices=this.numChoicesFor(st.expect);this.setState(upd,()=>{this._answerBusy=false;});return;}const next={input:'',waHint:'',waStepMiss:0,waStepChoices:null,waError:''},last=index>=plan.steps.length-1;if(last){this.setState(next,()=>{this._answerBusy=false;this.finishScoredQuestion(q,this.state.waMistakes||0,{viaSteps:true,perfectXp:14,comboXp:20,assistedXp:8});});return;}this.sfx('step');this.setState({...next,waStep:index+1},()=>{this._answerBusy=false;});}
""";

    private static string BuildFractionalRevealScript() => """
revealAnswer(){const q=this.cur();if(!q||!this.claimQuestionTerminal())return;const p=this.curP();this.recordEvidence(p,q,'revealed',0);this.sfx('wrong');this.setState({screen:'feedback',combo:0,lastResult:{correct:false,revealed:true,q:q,userAns:'わからない',stars:0,combo:0,points:0,xp:0,levelUp:false},input:'',numChoices:null,hsStepChoices:null,waStepChoices:null,waHint:'',waError:'',typed:''});}
""";
}
