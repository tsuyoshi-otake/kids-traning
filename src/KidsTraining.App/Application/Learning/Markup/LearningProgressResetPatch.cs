namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchLearningProgressReset(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "muted:false, setupName:'', setupGrade:1, calib:null, settings:null,",
            "muted:false, setupName:'', setupGrade:1, calib:null, settings:null, resetConfirming:false,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "backStart(){this.sfx('select');this.setState({screen:'start'});}",
            "backStart(){this.sfx('select');this.setState({screen:'start',resetConfirming:false});}\n" + BuildLearningResetMethods(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "masteryRows:masteryRows,",
            "masteryRows:masteryRows, resetConfirming:!!S.resetConfirming, requestLearningReset:()=>this.requestLearningReset(), cancelLearningReset:()=>this.cancelLearningReset(), confirmLearningReset:()=>this.resetLearningProgress(),",
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
  requestLearningReset(){this.sfx('select');this.setState({resetConfirming:true});}
  cancelLearningReset(){this.sfx('select');this.setState({resetConfirming:false});}
  resetLearningProgress(){if(!this.state.resetConfirming)return;const current=this.curP();this.ensureLearningProfile(current);const mastery={},skillStats={},blank={attempts:0,independent:0,assisted:0,revealed:0,errors:0,confidence:.05,reviewStep:0,lastAttemptAt:null,nextReviewAt:null,masteredAt:null,level:1,stageAttempts:0,stageIndependent:0};for(const k of Object.keys(this.topics)){mastery[k]=.05;skillStats[k]={...blank};}const reset={...current,streak:0,stars:0,xp:0,mastery:mastery,skillStats:skillStats,cleared:{},learningSchema:3,progressResetAt:Date.now()},profiles=this.state.profiles.slice();profiles[this.state.profileIdx]=reset;const persisted=JSON.stringify(profiles);try{localStorage.setItem('kt_profiles_v1',persisted);this._lastSaved=persisted;}catch(e){}this.sfx('select');this.setState({profiles:profiles,session:null,lastResult:null,input:'',combo:0,numMiss:0,numChoices:null,hsStep:0,hsMistakes:0,hsStepMiss:0,hsStepChoices:null,resetConfirming:false,screen:'parent'});}
""";
    }

    private static string BuildLearningResetMarkup()
    {
        return """
      </div>
      <div style="margin-top:20px; background:#fff8f4; border:4px solid #efb3a2; border-radius:24px; padding:18px 22px; display:flex; align-items:center; justify-content:space-between; gap:24px;">
        <div>
          <div style="font-size:19px; font-weight:900; color:#9b3f2f;">学習状況のリセット</div>
          <div style="font-size:15px; color:#765f55; margin-top:5px; line-height:1.6;">星・XP・れんぞく記録・習熟度・復習予定を最初に戻します。学年・PIN・出題設定は残ります。</div>
        </div>
        <button type="button" onclick="{{ requestLearningReset }}" aria-label="学習状況のリセット確認を開く" style="flex:none; background:#fff; color:#b64232; border:3px solid #d96857; border-radius:18px; padding:11px 20px; font-size:17px; font-weight:900; cursor:pointer;">学習状況をリセット</button>
      </div>
      <sc-if value="{{ resetConfirming }}" hint-placeholder-val="{{ false }}">
        <div role="dialog" aria-modal="true" aria-labelledby="learning-reset-title" style="position:fixed; inset:0; z-index:1000; background:rgba(35,28,22,.62); display:flex; align-items:center; justify-content:center; padding:32px;">
          <div style="width:min(620px,92vw); background:#fff; border:5px solid #d96857; border-radius:28px; padding:30px; box-shadow:0 18px 50px rgba(0,0,0,.28);">
            <div id="learning-reset-title" style="font-size:27px; font-weight:900; color:#9b3f2f;">本当に学習状況をリセットしますか？</div>
            <div style="font-size:17px; line-height:1.8; color:#5f5146; margin-top:14px;">この操作は取り消せません。星・XP・れんぞく記録・全単元の習熟度と復習履歴が消えます。学年、保護者PIN、問題数、合格ライン、教科設定は変更しません。</div>
            <div style="display:flex; justify-content:flex-end; gap:14px; margin-top:26px;">
              <button type="button" onclick="{{ cancelLearningReset }}" aria-label="学習状況のリセットをやめる" style="background:#fff; color:#5f5146; border:3px solid #cfc0ad; border-radius:18px; padding:12px 24px; font-size:18px; font-weight:900; cursor:pointer;">やめる</button>
              <button type="button" onclick="{{ confirmLearningReset }}" aria-label="学習状況を完全にリセットする" style="background:#d94d3d; color:#fff; border:3px solid #a93427; border-radius:18px; padding:12px 24px; font-size:18px; font-weight:900; cursor:pointer;">リセットする</button>
            </div>
          </div>
        </div>
      </sc-if>
    </div>
  </sc-if>

  <!-- ============ EMERGENCY ============ -->
""";
    }
}
