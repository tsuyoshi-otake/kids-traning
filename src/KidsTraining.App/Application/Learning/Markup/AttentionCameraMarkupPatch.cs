namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchAttentionCamera(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "componentDidUpdate(){try{const s=JSON.stringify(this.state.profiles);if(s!==this._lastSaved){localStorage.setItem('kt_profiles_v1',s);this._lastSaved=s;}}catch(e){}this.saveLearningCheckpoint(false);}",
            "componentDidUpdate(){try{const s=JSON.stringify(this.state.profiles);if(s!==this._lastSaved){localStorage.setItem('kt_profiles_v1',s);this._lastSaved=s;}}catch(e){}this.saveLearningCheckpoint(false);this.syncAttentionCamera();}\n  componentWillUnmount(){this._attentionDisposed=true;this.stopAttentionCamera('stopped',true);}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "toggleMute(){const m=!this.state.muted;if(m)this.stopEnglishSpeech();this.setState({muted:m});try{localStorage.setItem('kt_muted_v1',m?'1':'0');}catch(e){}if(!m)setTimeout(()=>this.sfx('select'),0);}",
            "toggleMute(){const m=!this.state.muted;if(m)this.stopEnglishSpeech();this.setState({muted:m});try{localStorage.setItem('kt_muted_v1',m?'1':'0');}catch(e){}if(!m)setTimeout(()=>this.sfx('select'),0);}\n" + BuildAttentionCameraMethods(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "goStart(){this.clearLearningCheckpoint();this.sfx('select');const p=this.curP();this.setState({session:this.buildSession(p,1),screen:'quiz',combo:0,...this.freshQ()});}",
            "goStart(){this.clearLearningCheckpoint();this.sfx('select');const p=this.curP();this._attentionManualStop=false;this.setState({session:this.buildSession(p,1),screen:'quiz',combo:0,...this.freshQ()},()=>this.startAttentionCamera());}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "retry(){this.sfx('select');const p=this.curP();this.setState({session:this.buildSession(p,this.state.session.attempt+1),screen:'quiz',combo:0,...this.freshQ()});}",
            "retry(){this.sfx('select');const p=this.curP();this._attentionManualStop=false;this.setState({session:this.buildSession(p,this.state.session.attempt+1),screen:'quiz',combo:0,...this.freshQ()},()=>this.startAttentionCamera());}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "masteryRows:masteryRows, resetConfirming:",
            "masteryRows:masteryRows, attentionEnabled:!S.settings||S.settings.attentionEnabled!==false, attentionToggleLabel:(!S.settings||S.settings.attentionEnabled!==false)?'ON':'OFF', toggleAttention:()=>this.toggleAttention(), resetConfirming:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      <div style=\"margin-top:20px; display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:16px;\">",
            BuildAttentionParentSettingMarkup() + "\n      <div style=\"margin-top:20px; display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:16px;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "</helmet>",
            BuildAttentionCameraStyles() + "\n</helmet>",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildAttentionCameraMethods() =>
        """
  toggleAttention(){const enabled=!(this.state.settings&&this.state.settings.attentionEnabled!==false);this.sfx('tap');this.setSettings({attentionEnabled:enabled});if(!enabled){this._attentionManualStop=true;this.stopAttentionCamera('stopped',true);}}
  attentionLearningScreen(){return this.state.screen==='quiz'||this.state.screen==='feedback';}
  syncAttentionCamera(){const learning=this.attentionLearningScreen(),enabled=!this.state.settings||this.state.settings.attentionEnabled!==false;if(!learning){if(this._attentionStream)this.stopAttentionCamera('stopped',true);return;}if(enabled&&!this._attentionStream&&!this._attentionStarting)this.setAttentionUi('stopped',null,this._attentionManualStop?'このセッションでは停止しています':'「開始」を押すとカメラを使います');}
  ensureAttentionUi(){let root=document.getElementById('kt-attention-camera');if(root)return root;root=document.createElement('aside');root.id='kt-attention-camera';root.className='kt-attention-camera';root.setAttribute('aria-live','polite');root.innerHTML='<div class="kt-attention-head"><span id="kt-attention-state">カメラ準備中</span><button type="button" id="kt-attention-action" aria-label="カメラを開始">開始</button></div><video id="kt-attention-video" muted playsinline></video><div class="kt-attention-label">画面を見ている目安 <b id="kt-attention-score">--%</b></div><div class="kt-attention-meter"><span id="kt-attention-fill"></span></div><div id="kt-attention-note">映像は保存・送信しません</div>';document.body.appendChild(root);root.querySelector('#kt-attention-action').addEventListener('click',()=>{if(this._attentionStream||this._attentionStarting){this._attentionManualStop=true;this.stopAttentionCamera('stopped',false);}else{this._attentionManualStop=false;this.startAttentionCamera();}});return root;}
  setAttentionUi(status,score,note){const root=this.ensureAttentionUi(),state=root.querySelector('#kt-attention-state'),action=root.querySelector('#kt-attention-action'),scoreEl=root.querySelector('#kt-attention-score'),fill=root.querySelector('#kt-attention-fill'),noteEl=root.querySelector('#kt-attention-note');root.hidden=false;root.dataset.status=status;state.textContent=status==='active'?'カメラ使用中':status==='starting'?'カメラ準備中':status==='denied'?'カメラを許可してね':status==='unavailable'?'判定を利用できません':'カメラ停止中';const running=status==='active'||status==='starting';action.textContent=running?'停止':'開始';action.setAttribute('aria-label',running?'このセッションのカメラを停止':'カメラを開始');const pct=Number.isFinite(score)?Math.round(this.clamp(score,0,1)*100):null;scoreEl.textContent=pct==null?'--%':pct+'%';fill.style.width=(pct==null?0:pct)+'%';noteEl.textContent=note||'映像は保存・送信しません';}
  showAttentionPrompt(){let prompt=document.getElementById('kt-attention-prompt');if(!prompt){prompt=document.createElement('div');prompt.id='kt-attention-prompt';prompt.className='kt-attention-prompt';prompt.setAttribute('role','status');document.body.appendChild(prompt);}prompt.textContent='集中してね';prompt.hidden=false;clearTimeout(this._attentionPromptTimer);this._attentionPromptTimer=setTimeout(()=>{prompt.hidden=true;},5000);}
  async startAttentionCamera(){const enabled=!this.state.settings||this.state.settings.attentionEnabled!==false;if(!enabled||this._attentionDisposed||this._attentionManualStop||this._attentionStream||this._attentionStarting||!this.attentionLearningScreen())return;this._attentionStarting=true;this.setAttentionUi('starting',null,'初回だけカメラの許可が必要です');try{if(!window.isSecureContext||!navigator.mediaDevices||typeof navigator.mediaDevices.getUserMedia!=='function')throw new Error('camera-unavailable');const stream=await navigator.mediaDevices.getUserMedia({audio:false,video:{facingMode:'user',width:{ideal:320},height:{ideal:240},frameRate:{ideal:10,max:15}}});this._attentionStream=stream;if(this._attentionDisposed||!this.attentionLearningScreen()||this._attentionManualStop){this.stopAttentionCamera('stopped',true);return;}if(typeof FaceDetector!=='function'){this.stopAttentionCamera('unavailable',false);this.setAttentionUi('unavailable',null,'この端末では顔の位置を判定できません');return;}const video=this.ensureAttentionUi().querySelector('#kt-attention-video');video.srcObject=stream;await video.play();if(this._attentionDisposed){this.stopAttentionCamera('stopped',true);return;}this._attentionDetector=new FaceDetector({fastMode:true,maxDetectedFaces:1});this._attentionAverage=null;this._attentionLowSamples=0;this._attentionNextPromptAt=0;this.setAttentionUi('active',null,'映像は端末内だけで処理します');this.scheduleAttentionSample(0);}catch(error){const denied=error&&(['NotAllowedError','SecurityError'].includes(error.name));this.stopAttentionCamera(denied?'denied':'unavailable',this._attentionDisposed);if(!this._attentionDisposed)this.setAttentionUi(denied?'denied':'unavailable',null,denied?'保護者といっしょにカメラを許可してください':'カメラを利用できません');}finally{this._attentionStarting=false;}}
  scheduleAttentionSample(delay=1500){clearTimeout(this._attentionTimer);if(!this._attentionStream)return;this._attentionTimer=setTimeout(()=>this.sampleAttention(),Math.max(500,delay));}
  async sampleAttention(){if(!this._attentionStream||this._attentionDetecting||!this.attentionLearningScreen())return;this._attentionDetecting=true;try{const video=document.getElementById('kt-attention-video');if(!video||video.readyState<2){this.scheduleAttentionSample();return;}const faces=await this._attentionDetector.detect(video);let instant=0;if(faces&&faces.length){const box=faces[0].boundingBox,w=Math.max(1,video.videoWidth),h=Math.max(1,video.videoHeight),cx=(box.x+box.width/2)/w,cy=(box.y+box.height/2)/h,center=this.clamp(1-(Math.abs(cx-.5)/.45+Math.abs(cy-.5)/.5)/2,0,1),size=this.clamp((box.width*box.height)/(w*h)/.12,0,1);instant=.6+.25*center+.15*size;}this._attentionAverage=this._attentionAverage==null?instant:this._attentionAverage*.75+instant*.25;this.setAttentionUi('active',this._attentionAverage,faces&&faces.length?'顔の位置から算出した目安です':'顔が画面に映っているか確認してね');if(this._attentionAverage<.45)this._attentionLowSamples=(this._attentionLowSamples||0)+1;else if(this._attentionAverage>.6)this._attentionLowSamples=0;const now=Date.now();if(this._attentionLowSamples>=4&&now>=(this._attentionNextPromptAt||0)){this._attentionNextPromptAt=now+30000;this._attentionLowSamples=0;this.showAttentionPrompt();}}catch(error){this.stopAttentionCamera('unavailable',false);this.setAttentionUi('unavailable',null,'顔の位置を判定できません');return;}finally{this._attentionDetecting=false;}this.scheduleAttentionSample();}
  stopAttentionCamera(status='stopped',hide=false){clearTimeout(this._attentionTimer);clearTimeout(this._attentionPromptTimer);this._attentionTimer=null;this._attentionDetecting=false;if(this._attentionStream){this._attentionStream.getTracks().forEach(track=>track.stop());this._attentionStream=null;}const video=document.getElementById('kt-attention-video');if(video)video.srcObject=null;const prompt=document.getElementById('kt-attention-prompt');if(prompt)prompt.hidden=true;const root=document.getElementById('kt-attention-camera');if(root){if(hide)root.hidden=true;else this.setAttentionUi(status,null,status==='unavailable'?'顔の位置を判定できません':'このセッションでは停止しています');}}
""";

    private static string BuildAttentionParentSettingMarkup() =>
        """
      <div style="margin-top:20px; background:#f3f8ff; border:4px solid #9bb7e8; border-radius:24px; padding:20px 22px; display:flex; align-items:center; justify-content:space-between; gap:20px;">
        <div>
          <div style="font-size:20px; font-weight:900; color:#294d8f;">画面を見ている目安</div>
          <div style="font-size:15px; color:#53657f; line-height:1.6; margin-top:4px;">初期設定はONです。映像は端末内で処理し、保存・送信・本人識別はしません。集中そのものを断定する機能ではありません。</div>
        </div>
        <button type="button" onclick="{{ toggleAttention }}" aria-pressed="{{ attentionEnabled }}" style="min-width:100px; background:#fff; color:#294d8f; border:3px solid #6f94d4; border-radius:18px; padding:11px 18px; font-size:19px; font-weight:900; cursor:pointer;">{{ attentionToggleLabel }}</button>
      </div>
""";

    private static string BuildAttentionCameraStyles() =>
        """
<style id="kt-attention-camera-style">
  .kt-attention-camera{position:fixed;right:18px;top:18px;z-index:80;width:220px;padding:10px;background:#fff;border:3px solid #6f94d4;border-radius:18px;box-shadow:0 8px 24px rgba(29,55,98,.2);font-family:'Zen Maru Gothic',sans-serif;color:#294d8f}
  .kt-attention-camera[hidden]{display:none}.kt-attention-head{display:flex;align-items:center;justify-content:space-between;gap:8px;font-size:14px;font-weight:900}.kt-attention-head button{border:2px solid #9bb7e8;border-radius:10px;background:#fff;color:#53657f;font:inherit;padding:3px 8px;cursor:pointer}
  #kt-attention-video{display:block;width:100%;height:112px;margin-top:7px;border-radius:12px;background:#1f2937;object-fit:cover;transform:scaleX(-1)}.kt-attention-label{display:flex;justify-content:space-between;gap:8px;margin-top:7px;font-size:13px;font-weight:700}.kt-attention-meter{height:10px;margin-top:4px;background:#dce7f8;border-radius:8px;overflow:hidden}.kt-attention-meter span{display:block;height:100%;width:0;background:#4f7edb;transition:width .25s}.kt-attention-camera[data-status='denied'],.kt-attention-camera[data-status='unavailable']{border-color:#d99a54}.kt-attention-camera[data-status='denied'] .kt-attention-meter span,.kt-attention-camera[data-status='unavailable'] .kt-attention-meter span{background:#d99a54}#kt-attention-note{margin-top:5px;font-size:11px;line-height:1.35;color:#64748b}
  .kt-attention-prompt{position:fixed;left:50%;top:26px;transform:translateX(-50%);z-index:90;padding:14px 28px;border:4px solid #e0a72f;border-radius:22px;background:#fff6cf;color:#715300;font-size:24px;font-weight:900;box-shadow:0 8px 24px rgba(113,83,0,.2)}.kt-attention-prompt[hidden]{display:none}
  @media(max-width:720px),(max-height:760px){.kt-attention-camera{width:168px}.kt-attention-camera video{height:82px}.kt-attention-prompt{font-size:19px;padding:10px 20px}}
  @media(prefers-reduced-motion:reduce){.kt-attention-meter span{transition:none}}
</style>
""";
}
