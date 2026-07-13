namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchRewardSystem(string markup)
    {
        markup = ReplaceRequired(markup,
            "freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            BuildRewardMethodsScript() + "\n  freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "finishNumeric(){",
            "\n  submit(ans){",
            "finishNumeric(){const q=this.cur(),p=this.curP();const perfect=(this.state.numMiss||0)===0;p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.08),0.05,0.99);this.markCleared(p,q.topic);const combo=perfect?this.state.combo+1:0;const stars=perfect?(combo>=3?2:1):1,xpInfo=this.gainXp(p,perfect?(combo>=3?18:12):6);p.stars+=stars;if(perfect)this.state.session.correct++;this.sfx(perfect&&combo>=3?'combo':'correct');this.setState({screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,helped:!perfect,xp:xpInfo.amount,levelUp:xpInfo.levelUp},input:'',numChoices:null});}");

        markup = ReplaceBlock(
            markup,
            "submit(ans){",
            "\n  next(){",
            "submit(ans){const q=this.cur(),correct=String(ans)===String(q.answer),p=this.curP();p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(correct?0.12:-0.16),0.05,0.99);this.markCleared(p,q.topic);const combo=correct?this.state.combo+1:0,stars=correct?(combo>=3?2:1):0,xpInfo=correct?this.gainXp(p,combo>=3?18:12):{amount:0,levelUp:false};if(correct){p.stars+=stars;this.state.session.correct++;this.sfx(combo>=3?'combo':'correct');}else{this.sfx('wrong');}this.setState({screen:'feedback',combo:combo,lastResult:{correct:correct,q:q,userAns:ans,stars:stars,combo:combo,xp:xpInfo.amount,levelUp:xpInfo.levelUp},input:''});}");

        markup = ReplaceBlock(
            markup,
            "submitHissanStep(val){",
            "\n  unlockPC(){",
            "submitHissanStep(val){const q=this.cur(),st=q.steps[this.state.hsStep];const v=val!=null?val:this.state.input;if(v!==st.expect){this.sfx('wrong');const sm=(this.state.hsStepMiss||0)+1;const upd={hsHint:st.explain,input:'',hsMistakes:(this.state.hsMistakes||0)+1,hsStepMiss:sm};if(sm>=2)upd.hsStepChoices=this.numChoicesFor(st.expect);this.setState(upd);return;}const ns={input:'',hsHint:'',hsStepMiss:0,hsStepChoices:null};if(st.place==='ones'){ns.hsOnes=st.writeOnes;if(st.carry)ns.hsCarry=true;if(st.borrow)ns.hsBorrow=true;}else ns.hsTens=st.writeTens;const last=this.state.hsStep>=q.steps.length-1;if(last){const p=this.curP();const perfect=(this.state.hsMistakes||0)===0;p.mastery[q.topic]=this.clamp((Number(p.mastery[q.topic])||0.05)+(perfect?0.12:-0.05),0.05,0.99);this.markCleared(p,q.topic);const combo=perfect?this.state.combo+1:0;const stars=perfect?(combo>=3?2:1):1,xpInfo=this.gainXp(p,perfect?(combo>=3?20:14):8);p.stars+=stars;this.state.session.correct++;this.sfx(perfect&&combo>=3?'combo':'correct');this.setState({...ns,screen:'feedback',combo:combo,lastResult:{correct:true,q:q,userAns:q.answer,stars:stars,combo:combo,viaSteps:true,perfect:perfect,xp:xpInfo.amount,levelUp:xpInfo.levelUp}});}else{this.sfx('step');this.setState({...ns,hsStep:this.state.hsStep+1});}}");

        markup = ReplaceRequired(markup,
            "      <!-- center -->",
            "      <div style=\"margin-top:18px; background:#fff; border:4px solid #f0e2c8; border-radius:20px; padding:12px 16px; display:grid; grid-template-columns:auto 1fr auto; gap:12px; align-items:center;\">\n        <div style=\"font-size:18px; font-weight:900; color:#4f7edb; white-space:nowrap;\">EXP {{ xpText }}</div>\n        <div style=\"height:18px; background:#eef3ff; border:3px solid #3a3326; border-radius:12px; overflow:hidden;\"><span style=\"{{ xpBarStyle }}\"></span></div>\n        <div style=\"font-size:15px; font-weight:700; color:#6b5e45; white-space:nowrap;\">あと {{ xpToNext }} XP</div>\n      </div>\n      <!-- center -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "      <div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            "      <div style=\"margin-top:12px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:10px 22px; min-width:280px; text-align:center;\">\n        <div style=\"font-size:15px; color:#4f7edb; font-weight:900;\">けいけんち</div>\n        <div style=\"font-size:34px; color:#4f7edb; font-weight:900;\">+{{ fbXp }} XP</div>\n        <sc-if value=\"{{ fbLevelUp }}\" hint-placeholder-val=\"{{ false }}\"><div style=\"font-size:22px; color:#e09020; font-weight:900; animation:popIn .45s ease-out;\">レベルアップ！ {{ level }}</div></sc-if>\n      </div>\n      <div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#9a8662;\">ごうけい ★</div>\n          <div style=\"font-size:36px; font-weight:900;\">{{ totalStars }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            "        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#9a8662;\">ごうけい ★</div>\n          <div style=\"font-size:36px; font-weight:900;\">{{ totalStars }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #c9d8ff; border-radius:22px; padding:16px; text-align:center;\">\n          <div style=\"font-size:15px; color:#4f7edb;\">きょうの XP</div>\n          <div style=\"font-size:36px; font-weight:900; color:#4f7edb;\">+{{ earnedXp }}</div>\n        </div>\n        <div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "      <div onclick=\"{{ unlockPC }}\" style=\"margin-top:28px; background:#3aa655; color:#fff; border:5px solid #2f8a46; border-radius:28px; padding:20px 64px; font-size:36px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #2a7d3f;\">🔓 パソコンを つかう</div>",
            "      <div style=\"display:flex; gap:16px; margin-top:28px; flex-wrap:wrap; justify-content:center;\">\n        <div onclick=\"{{ goStart }}\" style=\"background:#ff8a3d; color:#fff; border:5px solid #e07d2a; border-radius:28px; padding:18px 44px; font-size:30px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #d96a26; min-width:300px; text-align:center;\">▶ べんきょうを つづける</div>\n        <div onclick=\"{{ unlockPC }}\" style=\"background:#3aa655; color:#fff; border:5px solid #2f8a46; border-radius:28px; padding:18px 44px; font-size:30px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #2a7d3f; min-width:300px; text-align:center;\">🔓 パソコンを つかう</div>\n      </div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "const sess=S.session||{};\n    const earned=sess.startStars!=null?(p.stars-sess.startStars):0;\n    const fbBgColor=lr.correct?'#eafbe8':'#fdeeee';",
            BuildRewardRenderScript(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "profileName:p.name, profileInitial:p.name.charAt(0), avatarStyle:avatar(p.color,56,26), profileGrade:p.grade+'年生', stars:p.stars, streak:p.streak, level:this.lvl(p),",
            "profileName:p.name, profileGrade:p.grade+'年生', stars:p.stars, streak:p.streak, level:this.lvl(p), xpText:xpText, xpToNext:xpToNext, xpBarStyle:xpBarStyle,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "clearCorrect:sess.correct||0, earnedStars:earned, totalStars:p.stars, weakNextLabel:weakLabels||'なし', unlockPC:()=>this.unlockPC(),",
            "clearCorrect:sess.correct||0, earnedStars:earned, earnedXp:earnedXp, totalStars:p.stars, weakNextLabel:weakLabels||'なし', unlockPC:()=>this.unlockPC(),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:fb.prompt||'', fbAnswer:fb.answer||'', fbExplanation:fb.explanation||'', fbStarText:lr.stars||0, fbCombo:(lr.combo||0)>=3, fbTopicLabel:fb.topic?T[fb.topic].label:'',",
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:fb.prompt||'', fbAnswer:fb.answer||'', fbExplanation:fb.explanation||'', fbStarText:lr.stars||0, fbXp:lr.xp||0, fbLevelUp:!!lr.levelUp, fbCombo:(lr.combo||0)>=3, fbTopicLabel:fb.topic?T[fb.topic].label:'',",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildRewardMethodsScript()
    {
        return """
gainXp(p,amount){const before=this.xpLevel(p);p.xp=(Number(p.xp)||0)+amount;const after=this.xpLevel(p);return{amount:amount,levelUp:after>before};}
""";
    }

    private static string BuildRewardRenderScript()
    {
        return """
const sess=S.session||{};
    const earned=sess.startStars!=null?(p.stars-sess.startStars):0;
    const earnedXp=sess.startXp!=null?((Number(p.xp)||0)-sess.startXp):0;
    const fbBgColor=lr.correct?'#eafbe8':'#fdeeee';
    const xpValue=Number(p.xp)||0,xpLevel=this.xpLevel(p),xpInto=xpValue%100,xpToNext=100-xpInto,xpText=xpInto+' / 100';
    const xpBarStyle='display:block;height:100%;width:'+Math.max(4,Math.min(100,xpInto))+'%;background:#4f7edb;transition:width .35s;border-radius:10px;';
""";
    }

}