namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchLearningCheckpoint(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "\n  setSettings(patch){",
            "\n" + BuildLearningCheckpointMethods() + "\n  setSettings(patch){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "this.setState({profiles:profiles,settings:settings,muted:muted});\n}",
            "window.__kidsTrainingPause=notifyHost=>this.pauseLearning(notifyHost!==false);window.__kidsTrainingDiscard=()=>{this.clearLearningCheckpoint();return true;};window.__kidsTrainingReset=mode=>this.applyLearningReset(mode,{screen:'start'});this.setState({profiles:profiles,settings:settings,muted:muted},()=>{const pending=host.pendingLearningReset;if((pending==='history'||pending==='full')&&this.applyLearningReset(pending,{screen:'start',notifyHost:true}))return;this.restoreLearningCheckpoint();});\n}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "componentDidUpdate(){try{const s=JSON.stringify(this.state.profiles);if(s!==this._lastSaved){localStorage.setItem('kt_profiles_v1',s);this._lastSaved=s;}}catch(e){}}",
            "componentDidUpdate(prevProps,prevState){if(this.profilesChangedSince(prevState))this.saveProfiles();this.scheduleLearningCheckpoint(prevState);}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);if(this._typeKeyHandler)document.removeEventListener('keydown',this._typeKeyHandler);this.stopEnglishSpeech();}",
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);if(this._typeKeyHandler)document.removeEventListener('keydown',this._typeKeyHandler);this.flushLearningCheckpoint(true);this.clearAutoAdvance();this.stopEnglishSpeech();if(window.__kidsTrainingPause)delete window.__kidsTrainingPause;if(window.__kidsTrainingDiscard)delete window.__kidsTrainingDiscard;if(window.__kidsTrainingReset)delete window.__kidsTrainingReset;}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "quitQuiz(){this.stopEnglishSpeech();this.sfx('select');this.setState({screen:'start',session:null,combo:0});}",
            "quitQuiz(){this.stopEnglishSpeech();this.clearLearningCheckpoint();this.sfx('select');this.setState({screen:'start',session:null,combo:0});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "unlockPC(){this.sfx('unlock');",
            "unlockPC(){this.clearLearningCheckpoint();this.sfx('unlock');",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "goStart(){this.sfx('select');",
            "goStart(){this.clearLearningCheckpoint();this.sfx('select');",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "goStart:()=>this.goStart(), goParent:()=>this.goParent(),",
            "goStart:()=>this.goStart(), goParent:()=>this.goParent(), pauseLearning:()=>this.pauseLearning(true),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "          <span role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\" style=\"background:#fff; border:3px solid #f0e2c8; border-radius:22px; padding:9px 22px; font-size:18px; font-weight:700; cursor:pointer;\">← もどる</span>",
            "          <span role=\"button\" tabindex=\"0\" onclick=\"{{ pauseLearning }}\" style=\"background:#8b5a21; color:#fff; border:3px solid #6f4517; border-radius:22px; padding:9px 22px; font-size:18px; font-weight:900; cursor:pointer;\">⏸ 一時停止してデスクトップへ</span>\n          <span role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\" style=\"background:#fff; border:3px solid #f0e2c8; border-radius:22px; padding:9px 22px; font-size:18px; font-weight:700; cursor:pointer;\">← もどる</span>",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildLearningCheckpointMethods()
    {
        return """
  learningCheckpointKey(){return 'kt_session_checkpoint_v1';}
  profilesChangedSince(previous){const current=this.state;if(!previous)return true;return previous.profiles!==current.profiles||previous.lastResult!==current.lastResult;}
  saveProfiles(){try{const serialized=JSON.stringify(this.state.profiles);if(serialized!==this._lastSaved){localStorage.setItem('kt_profiles_v1',serialized);this._lastSaved=serialized;}return true;}catch(e){return false;}}
  checkpointChangedSince(previous){const current=this.state;if(!previous)return true;return previous.screen!==current.screen||previous.session!==current.session||previous.combo!==current.combo||previous.lastResult!==current.lastResult||previous.input!==current.input||previous.numMiss!==current.numMiss||previous.numChoices!==current.numChoices||previous.choiceMiss!==current.choiceMiss||previous.hsStep!==current.hsStep||previous.hsOnes!==current.hsOnes||previous.hsTens!==current.hsTens||previous.hsCarry!==current.hsCarry||previous.hsBorrow!==current.hsBorrow||previous.hsMistakes!==current.hsMistakes||previous.hsHint!==current.hsHint||previous.hsStepMiss!==current.hsStepMiss||previous.hsStepChoices!==current.hsStepChoices||previous.typed!==current.typed||previous.typeMiss!==current.typeMiss;}
  scheduleLearningCheckpoint(previous){if(!this.checkpointChangedSince(previous)||!this.checkpointState()||this._checkpointSaveTimer)return;this._checkpointSaveTimer=setTimeout(()=>{this._checkpointSaveTimer=null;this.saveLearningCheckpoint(false);},120);}
  flushLearningCheckpoint(force){if(this._checkpointSaveTimer){clearTimeout(this._checkpointSaveTimer);this._checkpointSaveTimer=null;}return this.saveLearningCheckpoint(force);}
  clearLearningCheckpoint(){if(this._checkpointSaveTimer){clearTimeout(this._checkpointSaveTimer);this._checkpointSaveTimer=null;}this._lastCheckpoint='';try{localStorage.removeItem(this.learningCheckpointKey());}catch(e){}}
  checkpointState(){const S=this.state,s=S.session;if(!s||!Array.isArray(s.rolePlan)||!Array.isArray(s.questions)||(S.screen!=='quiz'&&S.screen!=='feedback'))return null;return{version:1,profileName:String(this.curP()&&this.curP().name||''),savedAt:Date.now(),screen:S.screen,session:s,combo:Number(S.combo)||0,lastResult:S.lastResult||null,input:String(S.input||''),numMiss:Number(S.numMiss)||0,numChoices:S.numChoices||null,choiceMiss:Number(S.choiceMiss)||0,hsStep:Number(S.hsStep)||0,hsOnes:S.hsOnes||'',hsTens:S.hsTens||'',hsCarry:!!S.hsCarry,hsBorrow:!!S.hsBorrow,hsMistakes:Number(S.hsMistakes)||0,hsHint:S.hsHint||'',hsStepMiss:Number(S.hsStepMiss)||0,hsStepChoices:S.hsStepChoices||null,typed:S.typed||'',typeMiss:Number(S.typeMiss)||0};}
  saveLearningCheckpoint(force){const checkpoint=this.checkpointState();if(!checkpoint)return false;try{const serialized=JSON.stringify(checkpoint);if(force||serialized!==this._lastCheckpoint){localStorage.setItem(this.learningCheckpointKey(),serialized);this._lastCheckpoint=serialized;}return true;}catch(e){return false;}}
  validLearningCheckpoint(value){if(!value||value.version!==1||(value.screen!=='quiz'&&value.screen!=='feedback'))return false;const s=value.session,p=this.curP();if(!s||!p||value.profileName!==String(p.name||'')||!Array.isArray(s.rolePlan)||!Array.isArray(s.questions)||!Number.isInteger(s.idx)||s.idx<0||s.idx>=s.rolePlan.length||!s.questions[s.idx])return false;return s.rolePlan.length>=1&&s.rolePlan.length<=30;}
  restoreLearningCheckpoint(){let checkpoint=null;try{const raw=localStorage.getItem(this.learningCheckpointKey());if(raw){checkpoint=JSON.parse(raw);this._lastCheckpoint=raw;}}catch(e){this.clearLearningCheckpoint();return false;}if(!this.validLearningCheckpoint(checkpoint)){if(checkpoint)this.clearLearningCheckpoint();return false;}const next={screen:checkpoint.screen,session:checkpoint.session,combo:Number(checkpoint.combo)||0,lastResult:checkpoint.lastResult||null,input:String(checkpoint.input||''),numMiss:Number(checkpoint.numMiss)||0,numChoices:checkpoint.numChoices||null,choiceMiss:Number(checkpoint.choiceMiss)||0,hsStep:Number(checkpoint.hsStep)||0,hsOnes:checkpoint.hsOnes||'',hsTens:checkpoint.hsTens||'',hsCarry:!!checkpoint.hsCarry,hsBorrow:!!checkpoint.hsBorrow,hsMistakes:Number(checkpoint.hsMistakes)||0,hsHint:checkpoint.hsHint||'',hsStepMiss:Number(checkpoint.hsStepMiss)||0,hsStepChoices:checkpoint.hsStepChoices||null,typed:checkpoint.typed||'',typeMiss:Number(checkpoint.typeMiss)||0};this.setState(next,()=>{this._terminalQuestionToken=next.screen==='feedback'?this.currentQuestionToken():'';if(next.screen==='feedback'&&next.lastResult&&next.lastResult.exhausted)this.scheduleAutoAdvance();});return true;}
  pauseLearning(notifyHost){this.clearAutoAdvance();const checkpoint=this.checkpointState();if(checkpoint&&!this.flushLearningCheckpoint(true)){if(this.state.screen==='feedback'&&this.state.lastResult&&this.state.lastResult.exhausted)this.scheduleAutoAdvance();return false;}if(notifyHost&&window.chrome&&window.chrome.webview&&typeof window.chrome.webview.postMessage==='function')window.chrome.webview.postMessage('kidsTraining.pause');return true;}
""";
    }
}
