namespace KidsTraining.App.Application.Learning.Markup;

/// <summary>
/// Makes a learning-session miss end with the child entering the shown answer (issue #103).
///
/// After three misses or 「わからない」 the feedback screen shows the answer, and the next
/// question only follows once the child has chosen or typed that answer: the same choices for a
/// choice question, a number pad for a whole-number answer, the physical keys for a keyboard
/// question. The copy is practice, not an attempt: scoring and learning evidence were already
/// recorded by the terminal state (exhaustQuestion / revealAnswer) and do not change here.
///
/// The gate is derived from <c>lastResult</c> (a miss that is not yet <c>copied</c>), so a session
/// restored from its checkpoint on the feedback screen still asks for the answer. Keystrokes in
/// progress live in <c>answerCopy</c>, keyed by the question token, so they never leak into the
/// next question. A question with no fitting entry method keeps つぎへ, so every feedback screen
/// can be left.
/// </summary>
internal static partial class LearningMarkupPatcher
{
    private static string PatchAnswerCopyGate(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "next(){this.stopEnglishSpeech();if(this._advanceBusy)return;",
            "next(){if(this.answerCopyPending())return;this.stopEnglishSpeech();if(this._advanceBusy)return;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "document.addEventListener('keydown',this._mathKeyHandler);",
            "document.addEventListener('keydown',this._mathKeyHandler);" + BuildAnswerCopyKeyHandlerScript(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);",
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);" +
            "if(this._answerCopyKeyHandler)document.removeEventListener('keydown',this._answerCopyKeyHandler);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            "\n" + BuildAnswerCopyMethodsScript() + "\n  renderVals(){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n    return {\n      isProfile:",
            "\n" + BuildAnswerCopyViewScript() + "\n    return {\n      isProfile:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "fbStarText:lr.stars||0,",
            "answerCopy:answerCopyView, fbStarText:lr.stars||0,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      <div class=\"kt-feedback-next\" role=\"button\" tabindex=\"0\" onclick=\"{{ next }}\">つぎへ ▶</div>",
            BuildAnswerCopyTemplate(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "</head>",
            BuildAnswerCopyStyle() + "\n</head>",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildAnswerCopyKeyHandlerScript()
    {
        return """
this._answerCopyKeyHandler=e=>{if(e.repeat||e.isComposing||e.key==='Process'||e.ctrlKey||e.altKey||e.metaKey||!this.answerCopyPending())return;const mode=this.answerCopyMode(this.state.lastResult.q);if((mode==='digits'&&/^[0-9]$/.test(e.key))||(mode==='type'&&/^[a-zA-Z]$/.test(e.key))){e.preventDefault();this.answerCopyInput(e.key);}};document.addEventListener('keydown',this._answerCopyKeyHandler);
""".TrimEnd('\r', '\n');
    }

    private static string BuildAnswerCopyMethodsScript()
    {
        return """
  answerCopyMode(q){
    // How a missed answer is entered again: 'choices', 'digits', 'type', or '' when nothing fits
    // (the feedback screen then keeps つぎへ).
    if(!q)return '';
    const answer=String(q.answer==null?'':q.answer),choices=Array.isArray(q.choices)?q.choices.map(String):[];
    if(q.mode==='choices')return choices.includes(answer)?'choices':'';
    if(q.mode==='type')return /^[a-z]+$/i.test(answer)?'type':'';
    if(/^\d+$/.test(answer))return 'digits';
    return choices.includes(answer)?'choices':'';
  }
  answerCopyPending(){const S=this.state,lr=S.lastResult;return S.screen==='feedback'&&!!lr&&lr.correct===false&&!lr.copied&&this.answerCopyMode(lr.q)!=='';}
  answerCopyState(){const c=this.state.answerCopy,token=this.currentQuestionToken();return c&&c.token===token?c:{token:token,input:'',miss:0};}
  answerCopyInput(ch){
    if(!this.answerCopyPending())return;
    const q=this.state.lastResult.q,mode=this.answerCopyMode(q),key=String(ch==null?'':ch);
    if(!(mode==='digits'&&/^[0-9]$/.test(key))&&!(mode==='type'&&/^[a-z]$/i.test(key)))return;
    // Each key is checked against the shown answer as it is typed, so a slip clears the entry at
    // once instead of waiting for a submit.
    const answer=mode==='type'?String(q.answer).toLowerCase():String(q.answer),c=this.answerCopyState(),typed=c.input+(mode==='type'?key.toLowerCase():key);
    if(answer.indexOf(typed)!==0){this.sfx('wrong');this.setState({answerCopy:{token:c.token,input:'',miss:c.miss+1}});return;}
    if(typed===answer){this.setState({answerCopy:{token:c.token,input:typed,miss:c.miss}},()=>this.answerCopyDone());return;}
    this.sfx(mode==='type'?'step':'tap');this.setState({answerCopy:{token:c.token,input:typed,miss:c.miss}});
  }
  answerCopyChoose(value){
    if(!this.answerCopyPending()||this.answerCopyMode(this.state.lastResult.q)!=='choices')return;
    if(String(value)===String(this.state.lastResult.q.answer)){this.answerCopyDone();return;}
    const c=this.answerCopyState();this.sfx('wrong');this.setState({answerCopy:{token:c.token,input:'',miss:c.miss+1}});
  }
  answerCopyDone(){const lr=this.state.lastResult;if(!lr||lr.copied)return;this.sfx('correct');this.setState({lastResult:Object.assign({},lr,{copied:true})},()=>this.scheduleAutoAdvance());}
  answerCopyAdvanceMs(){return 700;}
  clearAutoAdvance(){if(this._autoAdvanceTimer){clearTimeout(this._autoAdvanceTimer);this._autoAdvanceTimer=null;}}
  scheduleAutoAdvance(){
    // Only a copied answer moves on by itself; a miss waits on the feedback screen for the copy.
    this.clearAutoAdvance();const lr=this.state.lastResult;if(this.state.screen!=='feedback'||!lr||!lr.copied)return;
    const token=this.currentQuestionToken();
    this._autoAdvanceTimer=setTimeout(()=>{this._autoAdvanceTimer=null;const now=this.state.lastResult;if(this.state.screen==='feedback'&&this.currentQuestionToken()===token&&now&&now.copied)this.next();},this.answerCopyAdvanceMs());
  }
""".TrimEnd('\r', '\n');
    }

    private static string BuildAnswerCopyViewScript()
    {
        return """
    let answerCopyView={show:false,done:false,showNext:true,showBox:false,box:'',showPad:false,pad:[],showChoices:false,choices:[],missed:false,missText:'',ask:''};
    if(sc==='feedback'&&S.lastResult){
      const acResult=S.lastResult,acQuestion=acResult.q||null,acMode=acResult.correct===false?this.answerCopyMode(acQuestion):'',acCopied=!!acResult.copied,acState=this.answerCopyState(),acAnswer=acQuestion?String(acQuestion.answer):'',acTyped=acMode==='digits'||acMode==='type';
      answerCopyView={
        show:!!acMode&&!acCopied, done:!!acMode&&acCopied, showNext:!acMode||acCopied,
        ask:acMode==='choices'?'うえの こたえを えらんで すすもう':(acMode==='type'?'うえの こたえを キーボードで うって すすもう':'うえの こたえを いれて すすもう'),
        showBox:acTyped, box:acTyped?acState.input+'＿'.repeat(Math.max(0,acAnswer.length-acState.input.length)):'',
        showPad:acMode==='digits', pad:['1','2','3','4','5','6','7','8','9','0'].map(n=>({label:n,ariaLabel:n,onClick:()=>this.answerCopyInput(n)})),
        showChoices:acMode==='choices', choices:acMode==='choices'?acQuestion.choices.map((c,index)=>({text:this.questionChoiceRich(acQuestion,index,c,this.kanjiTargetChoices(acQuestion)),onClick:()=>this.answerCopyChoose(c)})):[],
        missed:!!acMode&&!acCopied&&acState.miss>0, missText:acMode==='choices'?'もういちど。うえの こたえと おなじ ものを えらんでね':'もういちど。うえの こたえを よく みてね'
      };
    }
""".TrimEnd('\r', '\n');
    }

    private static string BuildAnswerCopyTemplate()
    {
        return """
      <sc-if value="{{ answerCopy.show }}" hint-placeholder-val="{{ false }}">
        <div class="kt-answer-copy" role="group" aria-label="こたえを いれて すすむ">
          <div class="kt-answer-copy-ask">{{ answerCopy.ask }}</div>
          <sc-if value="{{ answerCopy.showBox }}" hint-placeholder-val="{{ false }}">
            <div class="kt-answer-copy-box" aria-live="polite">{{ answerCopy.box }}</div>
          </sc-if>
          <sc-if value="{{ answerCopy.showPad }}" hint-placeholder-val="{{ false }}">
            <div class="kt-answer-copy-pad" aria-label="数字入力パッド">
              <sc-for list="{{ answerCopy.pad }}" as="k" hint-placeholder-count="10">
                <button type="button" class="kt-answer-copy-key" aria-label="{{ k.ariaLabel }}" onclick="{{ k.onClick }}">{{ k.label }}</button>
              </sc-for>
            </div>
          </sc-if>
          <sc-if value="{{ answerCopy.showChoices }}" hint-placeholder-val="{{ false }}">
            <div class="kt-answer-copy-choices">
              <sc-for list="{{ answerCopy.choices }}" as="c" hint-placeholder-count="4">
                <button type="button" class="kt-answer-copy-choice" onclick="{{ c.onClick }}">{{ c.text }}</button>
              </sc-for>
            </div>
          </sc-if>
          <sc-if value="{{ answerCopy.missed }}" hint-placeholder-val="{{ false }}">
            <div class="kt-answer-copy-note" role="status">{{ answerCopy.missText }}</div>
          </sc-if>
        </div>
      </sc-if>
      <sc-if value="{{ answerCopy.done }}" hint-placeholder-val="{{ false }}">
        <div class="kt-answer-copy-ok" role="status">○ できたね！ つぎへ すすむよ</div>
      </sc-if>
      <sc-if value="{{ answerCopy.showNext }}" hint-placeholder-val="{{ true }}">
        <div class="kt-feedback-next" role="button" tabindex="0" onclick="{{ next }}">つぎへ ▶</div>
      </sc-if>
""".TrimEnd('\r', '\n');
    }

    private static string BuildAnswerCopyStyle()
    {
        return """
<style id="kt-answer-copy">
  .kt-answer-copy{width:var(--kt-feedback-measure);max-width:100%;box-sizing:border-box;margin-top:var(--kt-space-4);padding:var(--kt-space-3) var(--kt-space-4);border:4px solid #ffd24a;border-radius:24px;background:#fffaf0;display:flex;flex-direction:column;align-items:center;gap:var(--kt-space-2);animation:rise .3s ease-out both;}
  .kt-answer-copy-ask{color:#7a5d00;font-size:22px;font-weight:900;text-align:center;}
  .kt-answer-copy-box{min-width:160px;padding:2px 20px;border:4px solid #ecd9b9;border-radius:18px;background:#fff;color:#3a3326;font-size:40px;font-weight:900;letter-spacing:.12em;text-align:center;font-variant-numeric:tabular-nums;}
  .kt-answer-copy-pad{display:flex;flex-wrap:wrap;justify-content:center;gap:8px;}
  .kt-answer-copy-key{width:56px;height:56px;border:3px solid #ecd9b9;border-radius:16px;background:#fff;color:#3a3326;font-family:inherit;font-size:28px;font-weight:900;box-shadow:0 4px 0 #ecd9b9;cursor:pointer;user-select:none;}
  .kt-answer-copy-choices{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;width:100%;}
  .kt-answer-copy-choice{min-height:60px;padding:8px 12px;border:3px solid #ecd9b9;border-radius:18px;background:#fff;color:#3a3326;font-family:inherit;font-size:26px;font-weight:900;box-shadow:0 4px 0 #ecd9b9;cursor:pointer;}
  .kt-answer-copy-key:active,.kt-answer-copy-choice:active{transform:translateY(3px);box-shadow:0 1px 0 #ecd9b9;}
  .kt-answer-copy-key:focus-visible,.kt-answer-copy-choice:focus-visible{outline:5px solid #2563eb;outline-offset:3px;}
  .kt-answer-copy-note{color:#b23b23;font-size:18px;font-weight:800;text-align:center;}
  .kt-answer-copy-ok{margin-top:var(--kt-space-4);padding:var(--kt-space-2) var(--kt-space-6);border:4px solid #3aa655;border-radius:20px;background:#e8f7ec;color:#22683c;font-size:24px;font-weight:900;}
  @media (max-height:900px){
    .kt-answer-copy{margin-top:var(--kt-space-3);padding:var(--kt-space-2) var(--kt-space-3);}
    .kt-answer-copy-ask{font-size:19px;}
    .kt-answer-copy-box{font-size:32px;}
    .kt-answer-copy-key{width:50px;height:50px;font-size:24px;}
    .kt-answer-copy-choice{min-height:52px;font-size:22px;}
  }
  @media (prefers-reduced-motion:reduce){.kt-answer-copy{animation:none;}}
</style>
""".TrimEnd('\r', '\n');
    }
}
