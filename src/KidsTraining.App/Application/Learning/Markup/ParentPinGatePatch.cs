namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    /// <summary>
    /// The parent dashboard exposes every learning setting and the progress reset, so a child
    /// must not be able to open it by tapping the header chip. Both parent entry points now run
    /// through the existing PIN keypad; <c>pinIntent</c> tells the shared screen which caller to
    /// return to once the PIN matches.
    /// </summary>
    private static string PatchParentPinGate(string markup)
    {
        markup = ReplaceRequired(markup,
            "session:null, lastResult:null, input:'', combo:0, pin:'', emergencyDone:false,",
            "session:null, lastResult:null, input:'', combo:0, pin:'', emergencyDone:false, pinIntent:'unlock', pinError:false,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "goParent(){this.sfx('select');this.setState({screen:'parent'});}",
            "goParent(){this.sfx('select');this.setState({screen:'emergency',pin:'',emergencyDone:false,pinIntent:'parent',pinError:false});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "goEmergency(){this.sfx('select');this.setState({screen:'emergency',pin:'',emergencyDone:false});}",
            "goEmergency(){this.sfx('select');this.setState({screen:'emergency',pin:'',emergencyDone:false,pinIntent:'unlock',pinError:false});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "this.sfx(ok?'unlock':'wrong');this.setState({pin:np,emergencyDone:ok});",
            "this.sfx(ok?'unlock':'wrong');if(ok&&this.state.pinIntent==='parent'){this.setState({pin:'',emergencyDone:false,pinError:false,screen:'parent'});return;}this.setState({pin:np,emergencyDone:ok,pinError:!ok});",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "pinDel(){this.setState({pin:this.state.pin.slice(0,-1),emergencyDone:false});}",
            "pinDel(){this.setState({pin:this.state.pin.slice(0,-1),emergencyDone:false,pinError:false});}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "pinDots:[0,1,2,3].map(i=>({char:i<S.pin.length?'●':''})),",
            "pinDots:[0,1,2,3].map(i=>({char:i<S.pin.length?'●':''})),\n      pinTitle:S.pinIntent==='parent'?'保護者ダッシュボード':'保護者用 ・ すぐに解除',\n      pinSubtitle:S.pinIntent==='parent'?'PINをいれると 学習せっていを ひらけます':'トレーニングを とばして パソコンを使えます',\n      pinNote:S.pinError?'PINがちがいます。もういちど いれてください。':(S.pinIntent==='parent'?'学習せっていと きろくは 保護者PINで まもられています':'PINで認証 ・ 誤入力が続くとロック ・ 緊急解除はログに記録'),\n      pinNoteStyle:'font-size:15px; color:'+(S.pinError?'#d2503f':'#9a8662')+'; margin-top:18px; text-align:center; line-height:1.6;',",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"font-size:32px; font-weight:900;\">保護者用 ・ すぐに解除</div>",
            "<div style=\"font-size:32px; font-weight:900;\">{{ pinTitle }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"font-size:18px; color:#9a8662; margin-top:4px;\">トレーニングを とばして パソコンを使えます</div>",
            "<div style=\"font-size:18px; color:#9a8662; margin-top:4px;\">{{ pinSubtitle }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"font-size:15px; color:#9a8662; margin-top:18px; text-align:center; line-height:1.6;\">PIN／OSパスワード／生体認証で認証 ・ 誤入力が続くとロック ・ 緊急解除はログに記録</div>",
            "<div style=\"{{ pinNoteStyle }}\">{{ pinNote }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<span style=\"font-size:15px; color:#9a8662; border:2px dashed #d8c4a0; border-radius:10px; padding:6px 12px; display:flex; align-items:center;\">🔑 PINで保護</span>",
            "<span style=\"font-size:15px; color:#2f7d44; background:#eafbe8; border:2px solid #a8d8ae; border-radius:10px; padding:6px 12px; display:flex; align-items:center;\">🔑 PIN認証ずみ</span>",
            StringComparison.Ordinal);

        // The retry screen button opens the PIN unlock flow, not a "contact your parent" message.
        markup = ReplaceRequired(markup,
            ">⚙ 保護者に れんらく</div>",
            ">🔑 保護者が かいじょ</div>",
            StringComparison.Ordinal);

        return markup;
    }
}
