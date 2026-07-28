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
            "componentDidUpdate(prevProps,prevState){this.scheduleProfilesSave(prevState);this.scheduleLearningCheckpoint(prevState);}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);if(this._typeKeyHandler)document.removeEventListener('keydown',this._typeKeyHandler);if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);this.stopEnglishSpeech();}",
            "componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);if(this._typeKeyHandler)document.removeEventListener('keydown',this._typeKeyHandler);if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);this.flushProfilesSave(true);this.flushLearningCheckpoint(true);this.clearAutoAdvance();this.stopEnglishSpeech();if(window.__kidsTrainingPause)delete window.__kidsTrainingPause;if(window.__kidsTrainingDiscard)delete window.__kidsTrainingDiscard;if(window.__kidsTrainingReset)delete window.__kidsTrainingReset;}",
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
  scheduleProfilesSave(previous){if(!this.profilesChangedSince(previous))return false;this._profilesSavePending=true;if(this._profilesSaveTimer)return true;this._profilesSaveTimer=setTimeout(()=>{this._profilesSaveTimer=null;if(!this._profilesSavePending)return;const saved=this.saveProfiles();this._profilesSavePending=!saved;},120);return true;}
  flushProfilesSave(force){if(this._profilesSaveTimer){clearTimeout(this._profilesSaveTimer);this._profilesSaveTimer=null;}if(!force&&!this._profilesSavePending)return true;const saved=this.saveProfiles();this._profilesSavePending=!saved;return saved;}
  checkpointChangedSince(previous){const current=this.state;if(!previous)return true;return previous.screen!==current.screen||previous.session!==current.session||previous.combo!==current.combo||previous.lastResult!==current.lastResult||previous.input!==current.input||previous.numMiss!==current.numMiss||previous.numChoices!==current.numChoices||previous.choiceMiss!==current.choiceMiss||previous.hsStep!==current.hsStep||previous.hsOnes!==current.hsOnes||previous.hsTens!==current.hsTens||previous.hsCarry!==current.hsCarry||previous.hsBorrow!==current.hsBorrow||previous.hsMistakes!==current.hsMistakes||previous.hsHint!==current.hsHint||previous.hsStepMiss!==current.hsStepMiss||previous.hsStepChoices!==current.hsStepChoices||previous.waStep!==current.waStep||previous.waMistakes!==current.waMistakes||previous.waStepMiss!==current.waStepMiss||previous.waStepChoices!==current.waStepChoices||previous.waHint!==current.waHint||previous.waError!==current.waError||previous.typed!==current.typed||previous.typeMiss!==current.typeMiss;}
  scheduleLearningCheckpoint(previous){if(!this.checkpointChangedSince(previous))return false;if(!this.checkpointState()){this._checkpointSavePending=false;return false;}this._checkpointSavePending=true;if(this._checkpointSaveTimer)return true;this._checkpointSaveTimer=setTimeout(()=>{this._checkpointSaveTimer=null;if(!this._checkpointSavePending)return;const saved=this.saveLearningCheckpoint(false);this._checkpointSavePending=!saved;},120);return true;}
  flushLearningCheckpoint(force){if(this._checkpointSaveTimer){clearTimeout(this._checkpointSaveTimer);this._checkpointSaveTimer=null;}if(!this.checkpointState()){this._checkpointSavePending=false;return true;}if(!force&&!this._checkpointSavePending)return true;const saved=this.saveLearningCheckpoint(force);this._checkpointSavePending=!saved;return saved;}
  clearLearningCheckpoint(){if(this._checkpointSaveTimer){clearTimeout(this._checkpointSaveTimer);this._checkpointSaveTimer=null;}this._checkpointSavePending=false;this._lastCheckpoint='';try{localStorage.removeItem(this.learningCheckpointKey());}catch(e){}}
  checkpointState(){const S=this.state,s=S.session;if(!s||!Array.isArray(s.rolePlan)||!Array.isArray(s.questions)||(S.screen!=='quiz'&&S.screen!=='feedback'))return null;return{version:1,profileName:String(this.curP()&&this.curP().name||''),savedAt:Date.now(),screen:S.screen,session:s,combo:Number(S.combo)||0,lastResult:S.lastResult||null,input:String(S.input||''),numMiss:Number(S.numMiss)||0,numChoices:S.numChoices||null,choiceMiss:Number(S.choiceMiss)||0,hsStep:Number(S.hsStep)||0,hsOnes:S.hsOnes||'',hsTens:S.hsTens||'',hsCarry:!!S.hsCarry,hsBorrow:!!S.hsBorrow,hsMistakes:Number(S.hsMistakes)||0,hsHint:S.hsHint||'',hsStepMiss:Number(S.hsStepMiss)||0,hsStepChoices:S.hsStepChoices||null,waStep:Number(S.waStep)||0,waMistakes:Number(S.waMistakes)||0,waStepMiss:Number(S.waStepMiss)||0,waStepChoices:S.waStepChoices||null,waHint:S.waHint||'',waError:S.waError||'',typed:S.typed||'',typeMiss:Number(S.typeMiss)||0};}
  saveLearningCheckpoint(force){const checkpoint=this.checkpointState();if(!checkpoint)return false;try{const serialized=JSON.stringify(checkpoint);if(force||serialized!==this._lastCheckpoint){localStorage.setItem(this.learningCheckpointKey(),serialized);this._lastCheckpoint=serialized;}return true;}catch(e){return false;}}
  validLearningCheckpoint(value){if(!value||value.version!==1||(value.screen!=='quiz'&&value.screen!=='feedback'))return false;const s=value.session,p=this.curP();if(!s||!p||value.profileName!==String(p.name||'')||!Array.isArray(s.rolePlan)||!Array.isArray(s.questions)||!Number.isInteger(s.idx)||s.idx<0||s.idx>=s.rolePlan.length||!s.questions[s.idx]||s.rolePlan.length<1||s.rolePlan.length>30)return false;const unlocked=this.unlockedGradeTopics(p),unlockedIds=new Set(unlocked),topology=this.curriculumTopology(p),grades=[...topology.unitIdsByGrade.keys()].sort((a,b)=>a-b),minimumGrade=grades[0]||this.activeCurriculumGrade(p),activeGrade=this.activeCurriculumGrade(p),questionsValid=s.questions.every(q=>{if(!q)return false;if(q.unitId)return unlockedIds.has(q.unitId);const grade=Number(q.grade);return Number.isInteger(grade)&&grade>=minimumGrade&&grade<=activeGrade;});if(!questionsValid)return false;const q=s.questions[s.idx],plan=typeof this.writtenArithmeticPlan==='function'?this.writtenArithmeticPlan(q):null,waStep=Number(value.waStep)||0;if(!plan)return waStep===0;const choices=value.waStepChoices;if(!Number.isInteger(waStep)||waStep<0||waStep>=plan.steps.length)return false;if(choices!=null&&(!Array.isArray(choices)||!choices.map(String).includes(String(plan.steps[waStep].expect))))return false;return true;}
  restoreLearningCheckpoint(){let checkpoint=null;try{const raw=localStorage.getItem(this.learningCheckpointKey());if(raw){checkpoint=JSON.parse(raw);this._lastCheckpoint=raw;}}catch(e){this.clearLearningCheckpoint();return false;}if(!this.validLearningCheckpoint(checkpoint)){if(checkpoint)this.clearLearningCheckpoint();return false;}const next={screen:checkpoint.screen,session:checkpoint.session,combo:Number(checkpoint.combo)||0,lastResult:checkpoint.lastResult||null,input:String(checkpoint.input||''),numMiss:Number(checkpoint.numMiss)||0,numChoices:checkpoint.numChoices||null,choiceMiss:Number(checkpoint.choiceMiss)||0,hsStep:Number(checkpoint.hsStep)||0,hsOnes:checkpoint.hsOnes||'',hsTens:checkpoint.hsTens||'',hsCarry:!!checkpoint.hsCarry,hsBorrow:!!checkpoint.hsBorrow,hsMistakes:Number(checkpoint.hsMistakes)||0,hsHint:checkpoint.hsHint||'',hsStepMiss:Number(checkpoint.hsStepMiss)||0,hsStepChoices:checkpoint.hsStepChoices||null,waStep:Number(checkpoint.waStep)||0,waMistakes:Number(checkpoint.waMistakes)||0,waStepMiss:Number(checkpoint.waStepMiss)||0,waStepChoices:checkpoint.waStepChoices||null,waHint:checkpoint.waHint||'',waError:checkpoint.waError||'',typed:checkpoint.typed||'',typeMiss:Number(checkpoint.typeMiss)||0};this.setState(next,()=>{this._terminalQuestionToken=next.screen==='feedback'?this.currentQuestionToken():'';if(next.screen==='feedback'&&next.lastResult&&next.lastResult.exhausted)this.scheduleAutoAdvance();});return true;}
  pauseLearning(notifyHost){this.clearAutoAdvance();const profilesSaved=this.flushProfilesSave(true),checkpointSaved=this.flushLearningCheckpoint(true);if(!profilesSaved||!checkpointSaved){if(this.state.screen==='feedback'&&this.state.lastResult&&this.state.lastResult.exhausted)this.scheduleAutoAdvance();return false;}if(notifyHost&&window.chrome&&window.chrome.webview&&typeof window.chrome.webview.postMessage==='function')window.chrome.webview.postMessage('kidsTraining.pause');return true;}
""";
    }
}
