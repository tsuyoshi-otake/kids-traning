namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    // Clearing a session always required two conditions, but only the score was ever shown.
    // The hidden one - answer today's target topic without hints - sent a child who had already
    // reached the pass line back to the retry screen reading "ごうかくまで あと 0てん", with no way
    // to work out what was still missing and no guarantee of ever getting out. So: state both
    // goals up front, word the retry screen after whichever goal is actually short, and let a
    // session through once the target goal alone has blocked it twice in a row.
    private static string PatchSessionPassGate(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "const globalPass=(Number(s.correct)||0)>=this.passLine(),targetPass=s.targetAsked>=4&&s.targetIndependent/s.targetAsked>=.7,pass=globalPass&&targetPass;",
            "const pass=this.sessionPassOutcome(this.curP(),s).pass;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "retryRemaining:this.formatScore(Math.max(0,this.passLine()-(Number(sess.correct)||0))),",
            "retryRemaining:this.formatScore(Math.max(0,this.passLine()-(Number(sess.correct)||0))), retryGoalText:this.retryGoalText(sess), retryAdvice:this.retryAdviceText(sess), missionTargetText:sc==='start'?this.missionTargetText(p):'',",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:16px; color:#9a8662; margin-top:2px;\">{{ passLine }}もん せいかいで ごうかく</div>",
            "<div style=\"font-size:16px; color:#9a8662; margin-top:2px;\">{{ passLine }}てん いじょうで ごうかく</div>\n              <div style=\"font-size:16px; color:#9a8662; margin-top:2px;\">そして {{ missionTargetText }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "{{ clearCorrect }} / {{ total }} てん ・ ごうかくまで あと <b>{{ retryRemaining }}てん</b>",
            "{{ clearCorrect }} / {{ total }} てん ・ <b>{{ retryGoalText }}</b>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "まちがえた もんだいを ふくしゅうして もういちど！",
            "{{ retryAdvice }}",
            StringComparison.Ordinal);

        // Injected last: the advice helper repeats the retry-screen wording, which would make the
        // markup anchor above ambiguous if the script were already in place.
        markup = ReplaceRequired(
            markup,
            "  formatScore(value){const n=",
            BuildSessionPassGateScript() + "  formatScore(value){const n=",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildSessionPassGateScript() => """
  targetGoalRatio(){return .7;}
  targetGoalMinimum(){return 4;}
  passGraceLimit(){return 2;}
  targetQuota(s){if(!s)return 0;const planned=Array.isArray(s.rolePlan)?s.rolePlan.filter(r=>r==='target'||r==='exit').length:0;return Math.max(planned,Number(s.targetAsked)||0);}
  targetNeeded(s){const quota=this.targetQuota(s);return quota?Math.ceil(quota*this.targetGoalRatio()):0;}
  targetGoalMet(s){if(!s)return false;return (Number(s.targetAsked)||0)>=this.targetGoalMinimum()&&(Number(s.targetIndependent)||0)>=this.targetNeeded(s);}
  targetShortfall(s){if(this.targetGoalMet(s))return 0;return Math.max(1,this.targetNeeded(s)-(Number(s&&s.targetIndependent)||0));}
  topicLabelOf(key){const t=key&&this.topics?this.topics[key]:null;return t&&t.label?t.label:'きょうの めあて';}
  sessionTargetLabel(s){return this.topicLabelOf(s&&s.activeTargetTopic);}
  plannedTargetQuota(){return Math.max(this.targetGoalMinimum(),Math.floor(this.total()*.25));}
  missionTargetText(p){try{return '「'+this.topicLabelOf(this.nextCurriculumTopic(p))+'」を ヒントなしで '+Math.ceil(this.plannedTargetQuota()*this.targetGoalRatio())+'もん';}catch(e){return '';}}
  scoreShortfall(s){return Math.max(0,this.passLine()-(Number(s&&s.correct)||0));}
  sessionPassOutcome(p,s){const globalPass=this.scoreShortfall(s)===0,targetPass=this.targetGoalMet(s),blocked=Number(p&&p.passBlockedStreak)||0,grace=globalPass&&!targetPass&&blocked+1>=this.passGraceLimit(),pass=globalPass&&(targetPass||grace);if(p)p.passBlockedStreak=(globalPass&&!targetPass&&!grace)?blocked+1:0;return{pass:pass,globalPass:globalPass,targetPass:targetPass,grace:grace};}
  retryGoalText(s){if(!s||!s.rolePlan)return '';if(this.scoreShortfall(s)>0)return 'ごうかくまで あと '+this.formatScore(this.scoreShortfall(s))+'てん';return 'てんすうは ごうかく！ 「'+this.sessionTargetLabel(s)+'」が あと '+this.targetShortfall(s)+'もん';}
  retryAdviceText(s){if(!s||!s.rolePlan||this.scoreShortfall(s)>0)return 'まちがえた もんだいを ふくしゅうして もういちど！';return '「'+this.sessionTargetLabel(s)+'」の もんだいを ヒントなしで とけたら ごうかく！';}

""";
}
