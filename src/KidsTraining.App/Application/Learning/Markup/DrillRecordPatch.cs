namespace KidsTraining.App.Application.Learning.Markup;

/// <summary>
/// The child's record screen (きろく, Issue #123). Every finished drill stage run is kept in its own
/// log (latest 200), with the questions missed on the run's first pass, and the screen lists those
/// runs newest first. When learning sessions have left answers in the learning history, a second
/// tab lists them by day. The drill (DrillModePatch) reports a run through three calls only:
/// clearDrillRunMisses when a run starts fresh, noteDrillRunMiss on a first-pass miss, and
/// recordDrillRun when a run is counted. This patch runs after PatchDrillMode, so it spells out
/// <c>role="button" tabindex="0"</c> itself on the markup it adds.
/// </summary>
internal static partial class LearningMarkupPatcher
{
    private static string PatchDrillRecord(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "document.addEventListener('keydown',this._mathKeyHandler);",
            "document.addEventListener('keydown',this._mathKeyHandler);" +
            "this._drillLogKeyHandler=e=>{if(e.key!=='Escape'||this.state.screen!=='drill-log')return;e.preventDefault();this.closeDrillLog();};" +
            "document.addEventListener('keydown',this._drillLogKeyHandler);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);",
            "if(this._mathKeyHandler)document.removeEventListener('keydown',this._mathKeyHandler);" +
            "if(this._drillLogKeyHandler)document.removeEventListener('keydown',this._drillLogKeyHandler);",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            "\n" + BuildDrillRecordMethodsScript() + "\n  renderVals(){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n    return {\n      isProfile:",
            "\n" + BuildDrillRecordViewScript() + "\n    return {\n      isProfile:",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "      profiles:profiles, goCalib:()=>this.goCalib(),",
            "      profiles:profiles, goCalib:()=>this.goCalib(),\n" +
            "      isDrillLog:sc==='drill-log', drillLogView:drillLogView, drillLogEntryText:drillLogEntryText, goDrillLog:()=>this.openDrillLog(),",
            StringComparison.Ordinal);

        // The entry sits in the hero row under the drill courses, so the drill-only APK shows it too.
        markup = ReplaceRequired(
            markup,
            "\n          </div>\n        </div>\n      </div>\n      <!-- start button -->",
            "\n" + BuildDrillRecordEntryTemplate() + "\n          </div>\n        </div>\n      </div>\n      <!-- start button -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "  <!-- ============ QUIZ ============ -->",
            BuildDrillRecordScreenTemplate() + "\n\n  <!-- ============ QUIZ ============ -->",
            StringComparison.Ordinal);

        return ReplaceRequired(markup, "</head>", BuildDrillRecordStyle() + "\n</head>", StringComparison.Ordinal);
    }

    private static string BuildDrillRecordMethodsScript()
    {
        return """
  drillLogKey(){return 'kt_drill_log_v1';}
  drillLogLimit(){return 200;}
  // The log is {runs:[oldest..newest], open:{courseId:[question indexes missed in the run going on]}}.
  // Anything malformed is dropped on read, so a broken log never stops the drill or the screen.
  readDrillLog(){
    let raw=null;try{raw=JSON.parse(localStorage.getItem(this.drillLogKey())||'null');}catch(e){raw=null;}
    const src=raw&&typeof raw==='object'&&!Array.isArray(raw)?raw:{},open={};
    const runs=(Array.isArray(src.runs)?src.runs:[]).map(r=>this.drillLogRun(r)).filter(Boolean).slice(-this.drillLogLimit());
    const srcOpen=src.open&&typeof src.open==='object'?src.open:{};
    for(const c of this.drillCourses()){const list=Array.isArray(srcOpen[c.id])?srcOpen[c.id]:[],seen=[];for(const n of list){const i=Number(n);if(Number.isInteger(i)&&i>=0&&i<c.total&&seen.indexOf(i)<0)seen.push(i);}if(seen.length)open[c.id]=seen;}
    return {runs:runs,open:open};
  }
  drillLogRun(r){
    if(!r||typeof r!=='object')return null;
    const at=String(r.at||''),time=Date.parse(at),course=String(r.course||'');
    if(!at||!Number.isFinite(time)||!course)return null;
    const int=(v,max)=>{const n=Math.floor(Number(v));return Number.isFinite(n)?this.clamp(n,0,max):0;},text=(v,max)=>String(v==null?'':v).slice(0,max);
    const total=int(r.total,1000),missed=(Array.isArray(r.missed)?r.missed:[]).filter(m=>m&&typeof m==='object').slice(0,100).map(m=>({q:text(m.q,40),a:text(m.a,20)}));
    return {at:at,course:course,title:text(r.title,40),stage:int(r.stage,99),name:text(r.name,40),mode:text(r.mode,10),perfect:Math.min(int(r.perfect,1000),total),total:total,mistakes:int(r.mistakes,10000),missed:missed};
  }
  writeDrillLog(log){try{localStorage.setItem(this.drillLogKey(),JSON.stringify(log));}catch(e){}}
  // A fresh run of a stage forgets the misses an abandoned run of it left behind.
  clearDrillRunMisses(id,from,to){
    const log=this.readDrillLog(),list=log.open[id];if(!list)return;
    const left=list.filter(i=>i<from||i>=to);if(left.length)log.open[id]=left;else delete log.open[id];
    this.writeDrillLog(log);
  }
  noteDrillRunMiss(d){
    if(!d)return;const log=this.readDrillLog(),list=log.open[d.id]||[];
    if(list.indexOf(d.idx)>=0)return;
    list.push(d.idx);log.open[d.id]=list;this.writeDrillLog(log);
  }
  recordDrillRun(d){
    const course=d&&this.drillCourse(d.id);if(!course)return;
    const log=this.readDrillLog(),bank=this.drillBank(d.id),from=d.from,to=d.to,skip=Array.isArray(d.skip)?d.skip:[];
    const inRun=i=>i>=from&&i<to,open=log.open[d.id]||[],missed=open.filter(inRun).sort((a,b)=>a-b);
    const left=open.filter(i=>!inRun(i));if(left.length)log.open[d.id]=left;else delete log.open[d.id];
    const stage=Math.floor(Number(d.stage))||0;
    log.runs.push(this.drillLogRun({
      at:new Date().toISOString(),course:d.id,title:course.title,stage:stage,name:this.drillStageName(course,stage),mode:d.answerMode,
      perfect:d.perfect,total:(to-from)-skip.filter(inRun).length,mistakes:d.mistakes,
      missed:missed.filter(i=>bank[i]).map(i=>({q:String(bank[i].text),a:String(bank[i].ans)}))
    }));
    log.runs=log.runs.slice(-this.drillLogLimit());
    this.writeDrillLog(log);
  }
  openDrillLog(){this.sfx('select');this.setState({screen:'drill-log',drillLogTab:'drill'});}
  closeDrillLog(){this.sfx('tap');this.setState({screen:'start'});}
  selectDrillLogTab(tab){this.sfx('tap');this.setState({drillLogTab:tab==='learning'?'learning':'drill'});}
  drillLogDate(at,withTime){
    const t=new Date(at);if(!Number.isFinite(t.getTime()))return '';
    const day=(t.getMonth()+1)+'/'+t.getDate();
    return withTime?day+' '+t.getHours()+':'+String(t.getMinutes()).padStart(2,'0'):day;
  }
  // A missed question as the child can read it back: 4 × 6 ＝ 24, 10 − □ ＝ 3（□＝7）, or 山 → やま.
  drillLogMissLine(m){return m.q.indexOf('□')>=0?m.q+'（□＝'+m.a+'）':/[＋−×÷]/.test(m.q)?m.q+' ＝ '+m.a:m.q+' → '+m.a;}
  drillLogModeName(mode){return {input:'すうじを 入力',choice:'えらんで こたえる',reading:'よみを えらぶ',writing:'かんじを えらぶ'}[mode]||'';}
  // Learning-session answers by local day, newest first; a day lists what was not answered alone.
  learningLogDays(){
    const days=[],byKey={};
    for(const e of this.readLearningHistory()){
      if(!e||typeof e!=='object')continue;const t=new Date(e.timestamp);if(!Number.isFinite(t.getTime()))continue;
      const key=t.getFullYear()+'-'+(t.getMonth()+1)+'-'+t.getDate();
      let day=byKey[key];if(!day){day={at:e.timestamp,total:0,alone:0,missed:[]};byKey[key]=day;days.push(day);}
      day.total+=1;
      if(e.outcome==='independent'){day.alone+=1;continue;}
      const q=e.question&&typeof e.question==='object'?e.question:{},line=String(q.prompt==null?'':q.prompt).replace(/\s+/g,' ').trim().slice(0,30)+' → '+String(q.answer==null?'':q.answer).slice(0,20);
      if(day.missed.indexOf(line)<0)day.missed.push(line);
    }
    return days.reverse();
  }
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillRecordViewScript()
    {
        return """
    let drillLogView=null,drillLogEntryText='';
    if(sc==='start'){const n=this.readDrillLog().runs.length;drillLogEntryText=n?'とっくん '+n+'かい':'まだ ないよ';}
    if(sc==='drill-log'){
      const learningDays=this.learningLogDays(),tab=learningDays.length&&S.drillLogTab==='learning'?'learning':'drill';
      const missedLine=list=>list.length?'まちがえた もんだい：'+list.join(' ／ '):'';
      const rows=tab==='learning'?learningDays.map(day=>({
        date:this.drillLogDate(day.at,false),badge:'がくしゅう',badgeStyle:'background:#ff8a3d;',
        title:day.total+'もん といた',sub:'ひとりで できた もんだい',score:day.alone+' / '+day.total,
        missedText:missedLine(day.missed),missedStyle:day.missed.length?'':'display:none;',
        style:'border-color:#f0e2c8;'
      })):this.readDrillLog().runs.slice().reverse().map(r=>{
        const c=this.drillCourse(r.course)||{},full=r.total>0&&r.perfect===r.total;
        return {
          date:this.drillLogDate(r.at,true),badge:c.badge||'とっくん',badgeStyle:'background:'+(c.color||'#ff8a3d')+';',
          title:(r.title||c.title||'')+' ステージ '+(r.stage+1),sub:[r.name,this.drillLogModeName(r.mode)].filter(Boolean).join('・'),
          score:(full?'💯 ':'')+r.perfect+' / '+r.total,
          missedText:missedLine(r.missed.map(m=>this.drillLogMissLine(m))),missedStyle:r.missed.length?'':'display:none;',
          style:'border-color:'+(c.edge||'#f0e2c8')+';'
        };
      });
      drillLogView={
        rows:rows,tabsStyle:learningDays.length?'':'display:none;',
        drillPressed:String(tab==='drill'),learningPressed:String(tab==='learning'),
        emptyText:tab==='drill'?'まだ きろくが ないよ。とっくんの ステージを さいごまで やると ここに のこるよ。':'',
        emptyStyle:rows.length?'display:none;':'',
        onDrill:()=>this.selectDrillLogTab('drill'),onLearning:()=>this.selectDrillLogTab('learning'),onBack:()=>this.closeDrillLog()
      };
    }
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillRecordEntryTemplate()
    {
        return """
            <!-- きろく -->
            <div class="kt-log-entry" role="button" tabindex="0" aria-label="これまでの きろくを みる" onclick="{{ goDrillLog }}">
              <span class="kt-log-entry-title">📒 きろくを みる</span>
              <span class="kt-log-entry-sub">{{ drillLogEntryText }}</span>
            </div>
""".TrimEnd('\r', '\n');
    }

    // The back button shares the drill screens' class, so the Android back key
    // (MainActivity.BACK_STEP_SCRIPT) leaves this screen the same way.
    private static string BuildDrillRecordScreenTemplate()
    {
        return """
  <!-- ============ DRILL LOG ============ -->
  <sc-if value="{{ isDrillLog }}" hint-placeholder-val="{{ false }}">
    <div data-screen-label="きろく" class="kt-drill-mode-screen">
      <button type="button" class="kt-drill-mode-back" aria-label="はじめの がめんに もどる" onclick="{{ drillLogView.onBack }}">← もどる</button>
      <div class="kt-log-panel">
        <div class="kt-drill-mode-title">📒 きろく</div>
        <div class="kt-log-tabs" style="{{ drillLogView.tabsStyle }}">
          <button type="button" class="kt-log-tab" aria-pressed="{{ drillLogView.drillPressed }}" onclick="{{ drillLogView.onDrill }}">とっくん</button>
          <button type="button" class="kt-log-tab" aria-pressed="{{ drillLogView.learningPressed }}" onclick="{{ drillLogView.onLearning }}">がくしゅう</button>
        </div>
        <div class="kt-log-empty" style="{{ drillLogView.emptyStyle }}">{{ drillLogView.emptyText }}</div>
        <div class="kt-log-list">
          <sc-for list="{{ drillLogView.rows }}" as="r" hint-placeholder-count="3">
            <div class="kt-log-row" style="{{ r.style }}">
              <div class="kt-log-row-head">
                <span class="kt-log-date">{{ r.date }}</span>
                <span class="kt-drill-badge" style="{{ r.badgeStyle }}">{{ r.badge }}</span>
                <span class="kt-log-title">{{ r.title }}</span>
                <span class="kt-log-score">{{ r.score }}</span>
              </div>
              <div class="kt-log-sub">{{ r.sub }}</div>
              <div class="kt-log-missed" style="{{ r.missedStyle }}">{{ r.missedText }}</div>
            </div>
          </sc-for>
        </div>
      </div>
    </div>
  </sc-if>
""".TrimEnd('\r', '\n');
    }

    private static string BuildDrillRecordStyle()
    {
        return """
<style id="kt-drill-log">
  .kt-log-entry{flex:1 1 100%;min-width:0;box-sizing:border-box;display:flex;align-items:center;justify-content:space-between;gap:12px;background:#fff;border:3px solid #f0e2c8;border-radius:20px;padding:10px 18px;cursor:pointer;color:#6b5e45;}
  .kt-log-entry:hover{background:#fff8ee;border-color:#d9bd8c;}
  .kt-log-entry:active{transform:translateY(2px);}
  .kt-log-entry:focus-visible,.kt-log-tab:focus-visible{outline:4px solid #2f7ccd;outline-offset:3px;}
  .kt-log-entry-title{font-size:18px;font-weight:900;color:#3a3326;}
  .kt-log-entry-sub{font-size:15px;font-weight:700;}
  .kt-log-panel{flex:1;max-width:760px;width:100%;margin:0 auto;display:flex;flex-direction:column;gap:12px;}
  .kt-log-panel .kt-drill-mode-title{text-align:center;}
  .kt-log-tabs{display:flex;gap:10px;justify-content:center;}
  .kt-log-tab{min-height:44px;min-width:120px;background:#fff;border:3px solid #f0e2c8;border-radius:20px;padding:6px 16px;font-family:inherit;font-size:17px;font-weight:800;color:#6b5e45;cursor:pointer;}
  .kt-log-tab[aria-pressed="true"]{background:#ff8a3d;border-color:#e07d2a;color:#fff;}
  .kt-log-empty{background:#fff;border:3px dashed #f0e2c8;border-radius:18px;padding:18px;text-align:center;font-size:17px;color:#6b5e45;line-height:1.6;}
  .kt-log-list{display:flex;flex-direction:column;gap:10px;}
  .kt-log-row{background:#fff;border:3px solid #f0e2c8;border-radius:18px;padding:10px 14px;display:flex;flex-direction:column;gap:4px;min-width:0;}
  .kt-log-row-head{display:flex;align-items:center;gap:8px;flex-wrap:wrap;}
  .kt-log-date{font-size:15px;font-weight:700;color:#8a7a5c;}
  .kt-log-title{flex:1 1 auto;min-width:0;font-size:17px;font-weight:900;color:#3a3326;}
  .kt-log-score{margin-left:auto;font-size:20px;font-weight:900;color:#3a3326;white-space:nowrap;}
  .kt-log-sub{font-size:14px;color:#6b5e45;}
  .kt-log-missed{font-size:15px;color:#b23b23;line-height:1.5;overflow-wrap:anywhere;}
</style>
""".TrimEnd('\r', '\n');
    }
}
