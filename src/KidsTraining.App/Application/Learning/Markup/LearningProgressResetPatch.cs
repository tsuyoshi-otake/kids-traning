namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchLearningProgressReset(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "muted:false, setupName:'', setupGrade:1, calib:null, settings:null,",
            "muted:false, setupName:'', setupGrade:1, calib:null, settings:null, resetConfirming:false, resetMode:'history', resetPin:'', resetError:'',",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "backStart(){this.sfx('select');this.setState({screen:'start'});}",
            "backStart(){this.sfx('select');this.setState({screen:'start',resetConfirming:false,resetPin:'',resetError:''});}\n" + BuildLearningResetMethods(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "masteryRows:masteryRows,",
            "masteryRows:masteryRows, resetConfirming:!!S.resetConfirming, resetModeFull:S.resetMode==='full', resetDialogTitle:S.resetMode==='full'?'すべての学習データをリセットしますか？':'学習履歴をリセットしますか？', resetDialogDescription:S.resetMode==='full'?'レベル・XP・星を含む学習データを最初に戻します。学年、保護者PIN、問題数、合格ライン、教科設定は変更しません。':'習熟度・復習予定・クリア履歴・れんぞく記録を最初に戻します。レベル・XP・星はそのまま残します。', resetHasError:!!S.resetError, resetError:S.resetError, requestHistoryReset:()=>this.requestLearningReset('history'), requestFullReset:()=>this.requestLearningReset('full'), cancelLearningReset:()=>this.cancelLearningReset(), resetPinDots:[0,1,2,3].map(i=>({char:i<S.resetPin.length?'●':''})), resetPinKeys:['1','2','3','4','5','6','7','8','9'].map(n=>({label:n,onClick:()=>this.resetPinPress(n),style:keyTile})).concat([{label:'けす',onClick:()=>this.resetPinDel(),style:keyClear},{label:'0',onClick:()=>this.resetPinPress('0'),style:keyTile},{label:'OK',onClick:()=>this.confirmLearningReset(),style:keyOk}]),",
            StringComparison.Ordinal);

        const string parentDashboardEnd =
            "      </div>\n    </div>\n  </sc-if>\n\n  <!-- ============ EMERGENCY ============ -->";
        return ReplaceRequired(
            markup,
            parentDashboardEnd,
            BuildLearningResetMarkup(),
            StringComparison.Ordinal);
    }

    private static string BuildLearningResetMethods()
    {
        return """
  requestLearningReset(mode){const next=mode==='full'?'full':'history';this.sfx('select');this.setState({resetConfirming:true,resetMode:next,resetPin:'',resetError:''});}
  cancelLearningReset(){this.sfx('select');this.setState({resetConfirming:false,resetPin:'',resetError:''});}
  resetPinPress(d){if(!this.state.resetConfirming||this.state.resetPin.length>=4)return;this.sfx('tap');this.setState({resetPin:this.state.resetPin+String(d),resetError:''});}
  resetPinDel(){this.sfx('tap');this.setState({resetPin:this.state.resetPin.slice(0,-1),resetError:''});}
  confirmLearningReset(){if(!this.state.resetConfirming)return;if(this.state.resetPin.length!==4){this.sfx('wrong');this.setState({resetError:'保護者PINを4桁で入力してください。'});return;}if(this.state.resetPin!==this.parentPin()){this.sfx('wrong');this.setState({resetPin:'',resetError:'保護者PINが違います。'});return;}this.applyLearningReset(this.state.resetMode,{screen:'parent'});}
  applyLearningReset(mode,options){if(mode!=='history'&&mode!=='full')return false;const opts=options||{},current=this.curP();if(!current)return false;this.ensureLearningProfile(current);const mastery={},skillStats={},blank={attempts:0,independent:0,assisted:0,revealed:0,errors:0,confidence:.05,reviewStep:0,retentionStep:0,lastAttemptAt:null,nextReviewAt:null,retentionStartedAt:null,masteredAt:null,level:1,stageAttempts:0,stageIndependent:0};for(const k of Object.keys(this.topics)){mastery[k]=.05;skillStats[k]={...blank};}const reset={...current,streak:0,stars:mode==='full'?0:(Number(current.stars)||0),xp:mode==='full'?0:(Number(current.xp)||0),mastery:mastery,skillStats:skillStats,cleared:{},learningSchema:4,progressResetAt:Date.now()},profiles=this.state.profiles.slice();profiles[this.state.profileIdx]=reset;const persisted=JSON.stringify(profiles);try{localStorage.setItem('kt_profiles_v1',persisted);this._lastSaved=persisted;}catch(e){this.setState({resetError:'学習データを保存できませんでした。もう一度お試しください。'});return false;}if(typeof this.clearLearningCheckpoint==='function')this.clearLearningCheckpoint();this.clearAutoAdvance();this._terminalQuestionToken='';this.sfx('select');this.setState({profiles:profiles,session:null,lastResult:null,input:'',combo:0,numMiss:0,numChoices:null,choiceMiss:0,hsStep:0,hsMistakes:0,hsStepMiss:0,hsStepChoices:null,waStep:0,waMistakes:0,waStepMiss:0,waStepChoices:null,waHint:'',waError:'',typed:'',typeMiss:0,resetConfirming:false,resetPin:'',resetError:'',screen:opts.screen||'parent'},()=>{if(opts.notifyHost&&window.chrome&&window.chrome.webview&&typeof window.chrome.webview.postMessage==='function')window.chrome.webview.postMessage('kidsTraining.resetApplied:'+mode);});return true;}
""";
    }

    private static string BuildLearningResetMarkup()
    {
        return """
      </div>
      <div style="margin-top:20px; display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:16px;">
        <div style="background:#fffaf0; border:4px solid #dfb878; border-radius:24px; padding:18px 22px; display:flex; flex-direction:column; gap:12px;">
          <div style="font-size:20px; font-weight:900; color:#80551f;">学習履歴のみリセット</div>
          <div style="font-size:15px; color:#765f55; line-height:1.6;">習熟度・復習予定・クリア履歴を最初に戻します。レベル・XP・星は残ります。</div>
          <button type="button" onclick="{{ requestHistoryReset }}" aria-label="レベルと経験値を残して学習履歴をリセット" style="margin-top:auto; background:#fff; color:#80551f; border:3px solid #c9974c; border-radius:18px; padding:11px 20px; font-size:17px; font-weight:900; cursor:pointer;">履歴のみリセット</button>
        </div>
        <div style="background:#fff8f4; border:4px solid #efb3a2; border-radius:24px; padding:18px 22px; display:flex; flex-direction:column; gap:12px;">
          <div style="font-size:20px; font-weight:900; color:#9b3f2f;">すべてリセット</div>
          <div style="font-size:15px; color:#765f55; line-height:1.6;">履歴に加えてレベル・XP・星も最初に戻します。学年・PIN・出題設定は残ります。</div>
          <button type="button" onclick="{{ requestFullReset }}" aria-label="学習データをすべてリセット" style="margin-top:auto; background:#fff; color:#b64232; border:3px solid #d96857; border-radius:18px; padding:11px 20px; font-size:17px; font-weight:900; cursor:pointer;">すべてリセット</button>
        </div>
      </div>
      <sc-if value="{{ resetConfirming }}" hint-placeholder-val="{{ false }}">
        <div role="dialog" aria-modal="true" aria-labelledby="learning-reset-title" style="position:fixed; inset:0; z-index:1000; background:rgba(35,28,22,.62); display:flex; align-items:center; justify-content:center; padding:32px; overflow:auto;">
          <div style="width:min(620px,92vw); background:#fff; border:5px solid #d96857; border-radius:28px; padding:30px; box-shadow:0 18px 50px rgba(0,0,0,.28);">
            <div id="learning-reset-title" style="font-size:27px; font-weight:900; color:#9b3f2f;">{{ resetDialogTitle }}</div>
            <div style="font-size:17px; line-height:1.8; color:#5f5146; margin-top:14px;">{{ resetDialogDescription }}</div>
            <div style="font-size:16px; font-weight:900; color:#5f5146; margin-top:18px; text-align:center;">保護者PINを入力してください</div>
            <div style="display:flex; justify-content:center; gap:14px; margin:12px 0; height:28px;"><sc-for list="{{ resetPinDots }}" as="d" hint-placeholder-count="4"><span style="width:24px; text-align:center; font-size:22px;">{{ d.char }}</span></sc-for></div>
            <div style="display:grid; grid-template-columns:repeat(3,1fr); gap:10px;"><sc-for list="{{ resetPinKeys }}" as="k" hint-placeholder-count="12"><div role="button" tabindex="0" onclick="{{ k.onClick }}" style="{{ k.style }}">{{ k.label }}</div></sc-for></div>
            <sc-if value="{{ resetHasError }}" hint-placeholder-val="{{ false }}"><div role="alert" style="color:#b42318; font-weight:900; text-align:center; margin-top:12px;">{{ resetError }}</div></sc-if>
            <div style="display:flex; justify-content:flex-end; margin-top:20px;"><button type="button" onclick="{{ cancelLearningReset }}" aria-label="学習データのリセットをやめる" style="background:#fff; color:#5f5146; border:3px solid #cfc0ad; border-radius:18px; padding:12px 24px; font-size:18px; font-weight:900; cursor:pointer;">やめる</button></div>
          </div>
        </div>
      </sc-if>
    </div>
  </sc-if>

  <!-- ============ EMERGENCY ============ -->
""";
    }
}
