namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchSoroban(string markup)
    {
        markup = ReplaceBlock(markup, "  pickSoroban(p){", "\n  pickSeikatsu(p){", BuildSorobanScript());
        markup = ReplaceRequired(markup, "dataFigure:S.screen===", "sorobanView:S.screen==='quiz'&&S.session?this.sorobanView(this.cur()):null, dataFigure:S.screen===", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "      {{ dataFigure }}", "      {{ sorobanView }}\n      {{ dataFigure }}", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "this._answerBusy=false;return {hsStep:0", "this._answerBusy=false;return {sbBoard:null,sbStep:0,sbMiss:0,sbMoved:false,sbHint:'',hsStep:0", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "const questionMistakes=modeWrittenSteps?", "const questionMistakes=S.screen==='quiz'&&S.session&&this.cur()?.mode==='soroban'?(S.sbMiss||0):modeWrittenSteps?", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "previous.waStep!==current.waStep", "previous.sbBoard!==current.sbBoard||previous.sbStep!==current.sbStep||previous.sbMiss!==current.sbMiss||previous.sbMoved!==current.sbMoved||previous.sbHint!==current.sbHint||previous.waStep!==current.waStep", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "waStep:Number(S.waStep)||0", "sbBoard:S.sbBoard||null,sbStep:Number(S.sbStep)||0,sbMiss:Number(S.sbMiss)||0,sbMoved:!!S.sbMoved,sbHint:S.sbHint||'',waStep:Number(S.waStep)||0", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "waStep:Number(checkpoint.waStep)||0", "sbBoard:checkpoint.sbBoard||null,sbStep:Number(checkpoint.sbStep)||0,sbMiss:Number(checkpoint.sbMiss)||0,sbMoved:!!checkpoint.sbMoved,sbHint:checkpoint.sbHint||'',waStep:Number(checkpoint.waStep)||0", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "if(!plan)return waStep===0;", "if(q.mode==='soroban'&&!this.validSorobanCheckpoint(q,value))return false;if(!plan)return waStep===0;", StringComparison.Ordinal);
        return ReplaceRequired(markup, "</head>", BuildSorobanStyles() + "\n</head>", StringComparison.Ordinal);
    }

    private static string BuildSorobanScript() => """
  pickSoroban(p){
    const stage=this.topicStage(p,'soroban'),buckets=[[0,1,2,3,4],[5,6,7],[8,9,10],[11,12],[13,14]];
    return this.pickStage(stage,buckets.map(items=>items.map(index=>()=>this.sorobanQuestion(index))),0);
  }
  sorobanQuestion(index){
    const read=value=>({kind:'read',initial:value,steps:[],prompt:'そろばんの はりに よせた たまを よもう。いくつ？',answer:value,explanation:'右から一・十・百の位。はりによせた五珠は5、一珠は1として数えると '+value+'。'});
    const place=value=>({kind:'place',initial:0,steps:[{target:value,prompt:value+'を そろばんに おこう。'}],prompt:value+'を そろばんに おこう。',answer:value,explanation:'位ごとに、5と1のたまを組み合わせると '+value+'。'});
    const cases=[...([0,1,2,3,4,5,7,9].map(read)),...([10,23,50].map(place)),
      {kind:'add',initial:6,steps:[{target:9,prompt:'一の位の 下のたまを 3こ分 たそう。'}],prompt:'そろばんで 6に3を たそう。',answer:9,explanation:'6は五珠1こと一珠1こ。一珠をさらに3こよせると9。'},
      {kind:'subtract',initial:12,steps:[{target:18,prompt:'4をひくには、まず 6をたしてから10をひくよ。まず一の位に6をたそう。'},{target:8,prompt:'次に 十の位から1こ（10）を ひこう。'}],prompt:'そろばんで 12から4を ひこう。',answer:8,explanation:'4をひくかわりに6をたして10をひく。12+6=18、18−10=8。'},
      {kind:'add',initial:7,steps:[{target:17,prompt:'8をたすには、まず10をたしてから2をひくよ。十の位に1こ（10）をたそう。'},{target:15,prompt:'次に 一の位から2を ひこう。'}],prompt:'そろばんで 7に8を たそう。',answer:15,explanation:'8をたすかわりに10をたして2をひく。7+10=17、17−2=15。'},
      {kind:'subtract',initial:23,steps:[{target:26,prompt:'7をひくには、まず3をたしてから10をひくよ。一の位に3をたそう。'},{target:16,prompt:'次に 十の位から1こ（10）を ひこう。'}],prompt:'そろばんで 23から7を ひこう。',answer:16,explanation:'7をひくかわりに3をたして10をひく。23+3=26、26−10=16。'}];
    const spec=cases[index];if(!spec)throw new Error('Unknown soroban question '+index);
    return{topic:'soroban',mode:spec.kind==='read'?'num':'soroban',subtype:'soroban-'+spec.kind,prompt:spec.prompt,answer:String(spec.answer),explanation:spec.explanation,soroban:{kind:spec.kind,initial:spec.initial,steps:spec.steps},activityPrompt:'実物のそろばんでも同じ数をおき、はりによせたたまと位を指で確かめよう。'};
  }
  sorobanDigits(value){if(!Number.isInteger(value)||value<0||value>999)return null;return[2,1,0].map(power=>Math.floor(value/Math.pow(10,power))%10);}
  sorobanNumber(digits){return Array.isArray(digits)&&digits.length===3&&digits.every(digit=>Number.isInteger(digit)&&digit>=0&&digit<=9)?digits[0]*100+digits[1]*10+digits[2]:null;}
  sorobanBoard(q){return this.state.sbBoard||this.sorobanDigits(q.soroban.initial);}
  sorobanCanMove(q){return !!q&&q.mode==='soroban'&&this.state.screen==='quiz'&&!this._answerBusy&&this._terminalQuestionToken!==this.currentQuestionToken();}
  moveSorobanBead(column,bead){
    const q=this.state.session&&this.cur();if(!this.sorobanCanMove(q)||!Number.isInteger(column)||column<0||column>2||!Number.isInteger(bead)||bead<0||bead>4)return;
    const board=this.sorobanBoard(q)?.slice();if(this.sorobanNumber(board)===null)return;
    const upper=board[column]>=5?5:0,lower=board[column]%5;
    board[column]=bead===0?(upper?0:5)+lower:upper+(bead<=lower?bead-1:bead);
    this.sfx('tap');this.setState({sbBoard:board,sbMoved:true,sbHint:''});
  }
  resetSoroban(){const q=this.state.session&&this.cur();if(!this.sorobanCanMove(q))return;this.setState({sbBoard:this.sorobanDigits(q.soroban.initial),sbStep:0,sbMoved:false,sbHint:'最初の盤面に戻したよ。手順をもう一度確かめよう。'});}
  submitSoroban(){
    const q=this.state.session&&this.cur();if(!this.sorobanCanMove(q))return;this._answerBusy=true;
    const index=Number(this.state.sbStep)||0,step=q.soroban?.steps?.[index],value=this.sorobanNumber(this.sorobanBoard(q));
    if(!step||value===null){this._answerBusy=false;this.revealAnswer();return;}
    if(!this.state.sbMoved){this._answerBusy=false;this.setState({sbHint:'たまを動かしてから、盤面を確かめよう。'});return;}
    if(value!==step.target){const misses=(this.state.sbMiss||0)+1;this._answerBusy=false;if(misses>=3){this.exhaustQuestion(String(value));return;}this.sfx('wrong');this.setState({sbMiss:misses,combo:0,sbHint:'もう一度、位とたまを確かめよう。'+step.prompt});return;}
    if(index===q.soroban.steps.length-1){this._answerBusy=false;this.finishScoredQuestion(q,this.state.sbMiss||0,{viaSteps:true,userAnswer:String(value)});return;}
    this.sfx('step');this._answerBusy=false;this.setState({sbStep:index+1,sbMoved:false,sbHint:'ここまでできたよ。次の操作に進もう。'});
  }
  validSorobanCheckpoint(q,value){
    const spec=q.soroban,index=Number(value.sbStep)||0;
    if((value.sbStep!=null&&!Number.isInteger(value.sbStep))||(value.sbMiss!=null&&!Number.isInteger(value.sbMiss))||(value.sbMoved!=null&&typeof value.sbMoved!=='boolean'))return false;
    if(!spec||!this.sorobanDigits(spec.initial)||!Array.isArray(spec.steps)||!spec.steps.length||spec.steps.length>4||!spec.steps.every(step=>step&&this.sorobanDigits(step.target)&&typeof step.prompt==='string')||!Number.isInteger(index)||index<0||index>=spec.steps.length)return false;
    if(value.sbBoard!=null&&this.sorobanNumber(value.sbBoard)===null)return false;
    if(index>0&&value.sbBoard==null)return false;
    return Number.isInteger(Number(value.sbMiss)||0)&&(Number(value.sbMiss)||0)>=0&&(Number(value.sbMiss)||0)<3;
  }
  sorobanView(q){
    if(!q?.soroban)return null;
    const h=(tag,props,...children)=>React.createElement(tag,props,...children),read=q.soroban.kind==='read',board=read?this.sorobanDigits(q.soroban.initial):this.sorobanBoard(q),names=['百','十','一'],index=Number(this.state.sbStep)||0,step=q.soroban.steps[index],disabled=!this.sorobanCanMove(q);
    if(this.sorobanNumber(board)===null)return h('p',{role:'alert'},'そろばんの盤面を確認できません。');
    const rods=board.map((digit,column)=>{
      const beads=[0,1,2,3,4].map(bead=>{
        const engaged=bead===0?digit>=5:bead<=digit%5,top=bead===0?(engaged?48:0):112+(bead-1)*44+(engaged?0:44);
        const label=names[column]+'の位・'+(bead===0?'五珠':'一珠'+bead)+'。'+(engaged?'はりによせている':'はりから離れている');
        return h('button',{key:bead,type:'button',className:'kt-soroban-bead',style:{top:top+'px'},'aria-label':label,'aria-pressed':engaged,disabled:disabled,onClick:()=>this.moveSorobanBead(column,bead)},h('span',{'aria-hidden':true,className:engaged?'is-engaged':''},bead===0?'5':'1'));
      });
      return h('div',{key:column,className:'kt-soroban-column'},h('div',{className:'kt-soroban-place'},names[column]+'の位'),h('div',{className:'kt-soroban-rod'},...beads,h('div',{className:'kt-soroban-beam','aria-label':column===2?'はり・定位点（一の位）':'はり'},column===2?'●':'')));
    });
    return h('section',{className:'kt-soroban','aria-label':'そろばん'},!read?h('h3',{},q.prompt):null,h('p',{className:'kt-soroban-help'},'はりによせた珠を数えるよ。上の五珠は5、下の一珠は1。'+(read?'':'Tabで珠を選び、Enter・Space、またはクリックで動かせるよ。')),h('div',{className:'kt-soroban-board'},...rods),!read?h('div',{className:'kt-soroban-controls'},h('p',{role:'status','aria-live':'polite'},'手順 '+(index+1)+' / '+q.soroban.steps.length+'：'+(step?.prompt||'')),this.state.sbHint?h('p',{role:'alert'},this.state.sbHint):null,h('div',{className:'kt-soroban-actions'},h('button',{type:'button',disabled:disabled,onClick:()=>this.resetSoroban()},'最初の盤面に戻す'),h('button',{type:'button',disabled:disabled,onClick:()=>this.submitSoroban()},'この盤面で決定'))):null);
  }
""";

    private static string BuildSorobanStyles() => """
<style id="kt-soroban-style">
 .kt-soroban{box-sizing:border-box;width:min(100%,680px);margin:12px auto;padding:16px;background:#fffdf8;border:3px solid #ead9bd;border-radius:20px;color:#3a3326;font-size:17px;line-height:1.6;}
 .kt-soroban h3{font-size:24px;margin:0 0 8px;text-align:center;}
 .kt-soroban-help{margin:0 0 12px;}
 .kt-soroban-board{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;width:min(100%,360px);margin:auto;padding:10px;box-sizing:border-box;border:5px solid #855a36;background:#fff5df;border-radius:12px;}
 .kt-soroban-place{text-align:center;font-weight:900;margin-bottom:8px;}
 .kt-soroban-rod{position:relative;height:340px;background:linear-gradient(to right,transparent calc(50% - 2px),#9a8662 calc(50% - 2px),#9a8662 calc(50% + 2px),transparent calc(50% + 2px));}
 .kt-soroban-beam{position:absolute;top:100px;height:8px;background:#855a36;left:-5px;right:-5px;text-align:center;font-size:12px;line-height:8px;color:#fff;}
 .kt-soroban-bead{position:absolute;left:50%;transform:translateX(-50%);width:64px;max-width:100%;height:44px;padding:3px 0;border:0;background:transparent;cursor:pointer;}
 .kt-soroban-bead span{display:grid;place-items:center;width:100%;height:100%;clip-path:polygon(20% 0,80% 0,100% 50%,80% 100%,20% 100%,0 50%);background:#bc8750;color:#23190e;font-size:16px;font-weight:900;box-shadow:inset 0 -5px #936335;}
 .kt-soroban-bead span.is-engaged{background:#197e88;color:#fff;box-shadow:inset 0 -5px #105b63;}
 .kt-soroban-bead:disabled{cursor:default;opacity:1;}
 .kt-soroban button:focus-visible{outline:3px solid #265ed7;outline-offset:2px;border-radius:6px;}
 .kt-soroban-actions{display:flex;flex-wrap:wrap;gap:10px;}
 .kt-soroban-actions button{flex:1;min-height:48px;border:2px solid #947448;border-radius:12px;background:#fff3d8;color:#3a3326;font:inherit;font-weight:800;padding:8px;cursor:pointer;}
 .kt-soroban-actions button:last-child{background:#197e88;color:#fff;border-color:#105b63;}
 .kt-soroban-actions button:disabled{opacity:.6;cursor:default;}
 @media(max-width:480px){div:has(> .kt-question-metadata):has(.kt-soroban){padding:16px!important;} .kt-soroban{padding:10px;font-size:16px;} .kt-soroban h3{font-size:22px;} .kt-soroban-board{gap:6px;padding:6px;}}
</style>
""";
}
