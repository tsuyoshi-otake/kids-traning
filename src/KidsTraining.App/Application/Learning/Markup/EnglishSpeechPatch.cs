namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchEnglishSpeech(string markup)
    {
        markup = ReplaceRequired(markup,
            "muted:false, setupName:",
            "muted:false, speakingEnglish:'', setupName:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            "englishSpeechAvailable(){return typeof window!=='undefined'&&!!window.speechSynthesis&&typeof window.SpeechSynthesisUtterance==='function';}\n  stopEnglishSpeech(){this._speechToken=(this._speechToken||0)+1;if(this.englishSpeechAvailable())window.speechSynthesis.cancel();if(this.state&&this.state.speakingEnglish)this.setState({speakingEnglish:''});}\n  speakEnglish(text){const value=String(text||'').trim();if(!value||this.state.muted||!this.englishSpeechAvailable())return;const synth=window.speechSynthesis,token=(this._speechToken||0)+1;this._speechToken=token;synth.cancel();const utterance=new window.SpeechSynthesisUtterance(value);utterance.lang='en-US';utterance.rate=.85;const voices=synth.getVoices?synth.getVoices():[];utterance.voice=voices.find(v=>String(v.lang).toLowerCase()==='en-us')||voices.find(v=>String(v.lang).toLowerCase().startsWith('en'))||null;const done=()=>{if(token===this._speechToken){this._speechToken=token+1;if(this.state&&this.state.speakingEnglish===value)this.setState({speakingEnglish:''});}};utterance.onend=done;utterance.onerror=done;this.setState({speakingEnglish:value});synth.speak(utterance);}\n  freshQ(){return {hsStep:0,hsOnes:'',hsTens:'',hsCarry:false,hsBorrow:false,hsMistakes:0,hsHint:'',input:'',numMiss:0,numChoices:null,hsStepMiss:0,hsStepChoices:null};}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "submit(ans){const q=this.cur(),correct=",
            "submit(ans){this.stopEnglishSpeech();const q=this.cur(),correct=",
            StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "next(){this.sfx('select');", "next(){this.stopEnglishSpeech();this.sfx('select');", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "quitQuiz(){this.sfx('select');", "quitQuiz(){this.stopEnglishSpeech();this.sfx('select');", StringComparison.Ordinal);
        markup = ReplaceRequired(markup,
            "toggleMute(){const m=!this.state.muted;this.setState({muted:m});",
            "toggleMute(){const m=!this.state.muted;if(m)this.stopEnglishSpeech();this.setState({muted:m});",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));",
            "if(modeChoices)choices=q.choices.map((c,index)=>{const skipFurigana=this.kanjiTargetChoices(q),showSpeak=!!q.speakChoices,speakEnabled=showSpeak&&!S.muted&&this.englishSpeechAvailable(),speaking=speakEnabled&&S.speakingEnglish===String(c);return{text:this.questionChoiceRich(q,index,c,skipFurigana),rowStyle:'display:grid;grid-template-columns:'+(showSpeak?'minmax(0,1fr) 64px':'1fr')+';gap:10px;align-items:stretch;',style:choiceTile+'width:100%;font-family:inherit;padding:10px 14px;',onClick:()=>this.submit(c),showSpeak:showSpeak,speakEnabled:speakEnabled,speakDisabled:showSpeak&&!speakEnabled,speakStyle:'min-width:64px;min-height:64px;border-radius:18px;font-size:28px;font-family:inherit;font-weight:900;cursor:'+(speakEnabled?'pointer':'not-allowed')+';border:3px solid '+(speaking?'#1d4ed8':'#b8c9ef')+';background:'+(speaking?'#2563eb':'#eef3ff')+';color:'+(speaking?'#fff':'#2563eb')+';box-shadow:0 5px 0 '+(speaking?'#1d4ed8':'#cbd7f3')+';',speakLabel:speaking?'🔊':'🔈',speakTitle:S.muted?'音をオンにしてね':(!this.englishSpeechAvailable()?'この端末では発音できません':'「'+c+'」の発音を聞く'),onSpeak:()=>this.speakEnglish(c)};});",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "<div style=\"{{ c.style }}\" onclick=\"{{ c.onClick }}\">{{ c.text }}</div>",
            "<div style=\"{{ c.rowStyle }}\"><button type=\"button\" class=\"kt-choice-button\" style=\"{{ c.style }}\" onclick=\"{{ c.onClick }}\">{{ c.text }}</button><sc-if value=\"{{ c.speakEnabled }}\" hint-placeholder-val=\"{{ false }}\"><button type=\"button\" class=\"kt-speech-button\" style=\"{{ c.speakStyle }}\" onclick=\"{{ c.onSpeak }}\" title=\"{{ c.speakTitle }}\" aria-label=\"{{ c.speakTitle }}\">{{ c.speakLabel }}</button></sc-if><sc-if value=\"{{ c.speakDisabled }}\" hint-placeholder-val=\"{{ false }}\"><button type=\"button\" class=\"kt-speech-button\" style=\"{{ c.speakStyle }}\" disabled title=\"{{ c.speakTitle }}\" aria-label=\"{{ c.speakTitle }}\">{{ c.speakLabel }}</button></sc-if></div>",
            StringComparison.Ordinal);
        markup = markup.Replace(
            "<div onclick=\"{{ c.onClick }}\" style=\"{{ c.style }}\">{{ c.text }}</div>",
            "<div style=\"{{ c.rowStyle }}\"><button type=\"button\" class=\"kt-choice-button\" style=\"{{ c.style }}\" onclick=\"{{ c.onClick }}\">{{ c.text }}</button><sc-if value=\"{{ c.speakEnabled }}\" hint-placeholder-val=\"{{ false }}\"><button type=\"button\" class=\"kt-speech-button\" style=\"{{ c.speakStyle }}\" onclick=\"{{ c.onSpeak }}\" title=\"{{ c.speakTitle }}\" aria-label=\"{{ c.speakTitle }}\">{{ c.speakLabel }}</button></sc-if><sc-if value=\"{{ c.speakDisabled }}\" hint-placeholder-val=\"{{ false }}\"><button type=\"button\" class=\"kt-speech-button\" style=\"{{ c.speakStyle }}\" disabled title=\"{{ c.speakTitle }}\" aria-label=\"{{ c.speakTitle }}\">{{ c.speakLabel }}</button></sc-if></div>",
            StringComparison.Ordinal);

        if (!markup.Contains("<button type=\"button\" class=\"kt-choice-button\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Neither supported learning choice markup variant was found.");
        }

        markup = ReplaceRequired(markup,
            "</helmet>",
            "<style>.kt-choice-button,.kt-speech-button{transition:transform .12s ease,filter .12s ease,box-shadow .12s ease}.kt-choice-button:hover,.kt-speech-button:not(:disabled):hover{filter:brightness(.98);transform:translateY(-1px)}.kt-choice-button:active,.kt-speech-button:not(:disabled):active{transform:translateY(3px);box-shadow:0 2px 0 #c9b997!important}.kt-choice-button:focus-visible,.kt-speech-button:focus-visible{outline:5px solid #2563eb;outline-offset:4px}.kt-speech-button:disabled{opacity:.52}@media(prefers-reduced-motion:reduce){.kt-choice-button,.kt-speech-button{transition:none}}</style>\n</helmet>",
            StringComparison.Ordinal);

        return markup;
    }

}
