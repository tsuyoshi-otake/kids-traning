namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchLearningAccessibility(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "      </sc-if>\n    </div>\n  </sc-if>\n\n  <!-- ============ FEEDBACK ============ -->",
            "      </sc-if>\n      <div onclick=\"{{ reveal }}\" aria-label=\"答えと説明を見る\" style=\"align-self:center; margin-top:12px; color:#765f3d; background:#fff; border:3px solid #d8c4a0; border-radius:18px; padding:10px 24px; font-size:17px; font-weight:700; cursor:pointer;\">わからない・答えと説明を見る</div>\n    </div>\n  </sc-if>\n\n  <!-- ============ FEEDBACK ============ -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "muted:S.muted, muteIcon:S.muted?'🔇':'🔊', toggleMute:()=>this.toggleMute(), quitQuiz:()=>this.quitQuiz(),",
            "muted:S.muted, muteIcon:S.muted?'🔇':'🔊', toggleMute:()=>this.toggleMute(), quitQuiz:()=>this.quitQuiz(), reveal:()=>this.revealAnswer(),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "let q=null,choices=[],modeNumeric=",
            "let q=null,choices=[],practicePrompt='',modeNumeric=",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "q=this.cur();const t=T[q.topic];topicLabel=t.label;",
            "q=this.cur();const t=T[q.topic];practicePrompt=q.activityPrompt?('活動カード＋振り返り：'+q.activityPrompt):(q.topic==='eigo'?'補助活動：音声を聞き、声に出してまねしてから答えよう。':(q.topic==='kokugo'&&q.subtype==='kanji-choice'?'ノートに漢字を書いてから答えよう。':''));topicLabel=t.label;",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "topicLabel:topicLabel, topicChipStyle:",
            "topicLabel:topicLabel, hasPracticePrompt:!!practicePrompt, practicePrompt:practicePrompt, topicChipStyle:",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "      </div>\n\n      <!-- NUMERIC -->",
            "      </div>\n      <sc-if value=\"{{ hasPracticePrompt }}\" hint-placeholder-val=\"{{ false }}\"><div style=\"margin-top:10px; background:#eef5ff; border:2px solid #9bb7ef; border-radius:14px; padding:8px 14px; color:#274b8b; font-size:17px; font-weight:700;\">{{ practicePrompt }}</div></sc-if>\n\n      <!-- NUMERIC -->",
            StringComparison.Ordinal);

        markup = markup.Replace(" onclick=", " role=\"button\" tabindex=\"0\" onclick=", StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" title=\"おと の オン/オフ\"",
            "role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" title=\"おと の オン/オフ\" aria-label=\"音声のオン・オフ\"",
            StringComparison.Ordinal);
        markup = markup.Replace(
            "data-screen-label=\"フィードバック\"",
            "data-screen-label=\"フィードバック\" role=\"status\" aria-live=\"polite\"",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "componentDidMount(){\n    let profiles=this.state.profiles;",
            "componentDidMount(){this._keyActivate=e=>{if((e.key==='Enter'||e.key===' ')&&e.target&&e.target.getAttribute&&e.target.getAttribute('role')==='button'){e.preventDefault();e.target.click();}};document.addEventListener('keydown',this._keyActivate);\n    let profiles=this.state.profiles;",
            StringComparison.Ordinal);
        markup = ReplaceRequired(
            markup,
            "\n  setSettings(patch){",
            "\n  componentWillUnmount(){if(this._keyActivate)document.removeEventListener('keydown',this._keyActivate);this.stopEnglishSpeech();}\n  setSettings(patch){",
            StringComparison.Ordinal);

        return markup;
    }
}
