namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchDataFigures(string markup)
    {
        markup = ReplaceRequired(markup, "\n  renderVals(){", BuildDataFigureScript() + "\n  renderVals(){", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "modeNumeric:modeNumeric,", "dataFigure:S.screen==='quiz'&&S.session?this.dataFigureView(this.cur()?.figure):null, modeNumeric:modeNumeric,", StringComparison.Ordinal);
        markup = ReplaceRequired(markup, "      <!-- INTERACTIVE WRITTEN ARITHMETIC -->", "      {{ dataFigure }}\n      <!-- INTERACTIVE WRITTEN ARITHMETIC -->", StringComparison.Ordinal);
        return ReplaceRequired(markup, "</head>", """
<style id="kt-data-figures">
 .kt-data-figure{box-sizing:border-box;width:min(100%,680px);margin:12px auto;padding:12px;border:3px solid #ead9bd;border-radius:18px;background:#fffdf8;color:#3a3326;font-size:16px;line-height:1.5;}
 .kt-data-figure figcaption{font-weight:900;text-align:center;margin-bottom:6px;}
 .kt-data-figure svg{display:block;width:100%;height:auto;max-height:300px;overflow:visible;font-family:"Noto Sans JP",sans-serif;}
 .kt-data-figure table{width:100%;border-collapse:collapse;font-size:16px;}
 .kt-data-figure th,.kt-data-figure td{border:1px solid #9a8662;padding:6px;text-align:center;overflow-wrap:anywhere;}
 .kt-data-figure summary{cursor:pointer;min-height:44px;display:list-item;align-content:center;font-weight:800;}
 .kt-data-figure summary:focus-visible{outline:3px solid #167d8d;outline-offset:3px;}
 .kt-data-legend{display:flex;flex-wrap:wrap;gap:6px 16px;margin:6px 0;}
 .kt-data-legend span{display:inline-flex;align-items:center;gap:5px;}
 .kt-data-legend i{display:inline-block;width:16px;height:16px;border:1px solid #3a3326;}
 @media(max-width:480px){
  div:has(> .kt-question-metadata):has(.kt-data-figure){padding:16px!important;}
  .kt-data-figure{padding:8px;} .kt-data-figure svg{min-height:190px;}
  .kt-data-figure svg text{font-size:22px;}
  .kt-data-figure th,.kt-data-figure td{padding:4px;font-size:14px;}
  div:has(> .kt-question-metadata):has(.kt-data-figure) .kt-question-prompt{font-size:24px!important;line-height:1.6!important;}
 }
</style>
</head>
""", StringComparison.Ordinal);
    }

    private static string BuildDataFigureScript() => """
  dataFigureView(f){
    if(!f)return null;
    const h=(tag,props,...children)=>React.createElement(tag,props,...children),labels=Array.isArray(f.labels)?f.labels.slice(0,32):[],values=Array.isArray(f.values)?f.values.slice(0,32).map(Number):[];
    if(!labels.length||values.some(v=>!Number.isFinite(v)))return h('p',{role:'alert'},'図表データを確認できません。');
    const colors=['#167d8d','#e5a23b','#ba5473','#7764a6'],title=String(f.title||''),xLabel=String(f.xLabel||''),yLabel=String(f.yLabel||''),shapes=[];
    const text=(x,y,value,extra={})=>h('text',{x:x,y:y,fontSize:16,fill:'#3a3326',textAnchor:'middle',...extra},String(value));
    const line=(x1,y1,x2,y2,extra={})=>h('line',{x1,y1,x2,y2,stroke:'#75684f',strokeWidth:1,...extra});
    const body=f.kind==='table'?(f.table||[]).slice(0,32).map((row,index)=>h('tr',{key:index},h('th',{scope:'row'},f.rowLabels?.[index]||''),...row.slice(0,32).map((value,col)=>h('td',{key:col},String(value))))):labels.map((label,index)=>h('tr',{key:index},h('th',{scope:'row'},label),h('td',{},String(values[index]))));
    const headers=f.kind==='table'?[(f.rowHeading||'項目')+'／'+xLabel,...labels]:f.kind==='box'?['項目',xLabel]:[xLabel||'項目',yLabel];
    const table=h('table',{},h('caption',{},title+'・データ表'+(f.kind==='table'?'（'+yLabel+'）':'')),h('thead',{},h('tr',{},...headers.map((label,index)=>h('th',{key:index,scope:'col'},label)))),h('tbody',{},...body));
    if(f.kind==='table')return h('figure',{className:'kt-data-figure'},h('figcaption',{},title),table);
    let legend=null;
    if(f.kind==='pie'||f.kind==='band'){
      const total=values.reduce((a,b)=>a+b,0);let sum=0;
      values.forEach((value,index)=>{
        const start=sum/total,end=(sum+value)/total,color=colors[index%colors.length];sum+=value;
        if(f.kind==='pie'){
          const point=t=>[240+105*Math.cos(t*2*Math.PI-Math.PI/2),135+105*Math.sin(t*2*Math.PI-Math.PI/2)],a=point(start),b=point(end),mid=point((start+end)/2);
          shapes.push(h('path',{d:`M240 135 L${a[0]} ${a[1]} A105 105 0 ${end-start>.5?1:0} 1 ${b[0]} ${b[1]} Z`,fill:color,stroke:'#fffdf8',strokeWidth:2}));
          shapes.push(text(240+(mid[0]-240)*.7,140+(mid[1]-135)*.7,index+1,{fill:'#fff',fontWeight:900}));
        }else{
          shapes.push(h('rect',{x:50+start*400,y:90,width:(end-start)*400,height:70,fill:color,stroke:'#fffdf8',strokeWidth:2}));
          shapes.push(text(50+(start+end)*200,132,index+1,{fill:'#fff',fontWeight:900}));
        }
      });
      if(f.kind==='band')for(let tick=0;tick<=100;tick+=10){const x=50+tick*4;shapes.push(line(x,165,x,173),text(x,195,tick));}
      shapes.push(text(250,270,'割合（%）'));
      legend=h('div',{className:'kt-data-legend'},...labels.map((label,index)=>h('span',{key:index},h('i',{'aria-hidden':true,style:{backgroundColor:colors[index%colors.length]}}),`${index+1} ${label} ${values[index]}%`)));
    }else if(f.kind==='box'){
      const max=Math.ceil(Math.max(...values)/2)*2,x=v=>50+400*v/max;
      for(let tick=0;tick<=max;tick+=2){shapes.push(line(x(tick),180,x(tick),188),text(x(tick),211,tick));}
      shapes.push(line(50,180,450,180),line(x(values[0]),110,x(values[4]),110),h('rect',{x:x(values[1]),y:80,width:x(values[3])-x(values[1]),height:60,fill:'#cfe7e8',stroke:colors[0],strokeWidth:3}));
      for(const index of [0,2,4])shapes.push(line(x(values[index]),80,x(values[index]),140,{stroke:colors[0],strokeWidth:3}));
      shapes.push(text(250,260,xLabel));
    }else{
      const lower=Math.min(0,...values),upper=Math.max(1,...values),step=Math.max(1,Math.ceil((upper-lower)/6)),min=Math.floor(lower/step)*step,max=Math.ceil(upper/step)*step,y=v=>235-(v-min)/(max-min)*190;
      const bar=f.kind==='bar',x=index=>bar?60+(index+.5)*390/labels.length:60+index*390/Math.max(1,labels.length-1);
      const coordinate=xLabel==='x',zeroIndex=labels.indexOf('0'),axisX=zeroIndex>=0&&!bar?x(zeroIndex):60;
      for(let tick=min;tick<=max;tick+=step){shapes.push(line(60,y(tick),450,y(tick),{stroke:'#e0d7c7'}));if(!coordinate||tick!==0)shapes.push(text(coordinate?axisX-12:48,y(tick)+5,tick,{textAnchor:'end'}));}
      if(coordinate)labels.forEach((label,index)=>shapes.push(line(x(index),45,x(index),235,{stroke:'#e0d7c7'})));
      shapes.push(line(60,y(0),450,y(0),{strokeWidth:2}),line(axisX,45,axisX,235,{strokeWidth:2}));
      labels.forEach((label,index)=>{if(!coordinate||label!=='0')shapes.push(text(x(index),coordinate?y(0)+23:257,label,{fontSize:bar?14:16}));if(bar)shapes.push(h('rect',{x:60+index*390/labels.length,y:y(values[index]),width:390/labels.length,height:y(0)-y(values[index]),fill:'#8ec7c9',stroke:colors[0],strokeWidth:2}));});
      if(!bar){
        if(f.kind==='quadratic'){
          const mid=(values.length-1)/2,controlY=2*y(values[mid])-(y(values[0])+y(values.at(-1)))/2;
          shapes.push(h('path',{d:`M${x(0)} ${y(values[0])} Q${x(mid)} ${controlY} ${x(values.length-1)} ${y(values.at(-1))}`,fill:'none',stroke:colors[0],strokeWidth:3}));
        }else shapes.push(h('polyline',{points:values.map((value,index)=>`${x(index)},${y(value)}`).join(' '),fill:'none',stroke:colors[0],strokeWidth:3}));
        values.forEach((value,index)=>shapes.push(h('circle',{cx:x(index),cy:y(value),r:4,fill:colors[0]})));
      }
      if(coordinate)shapes.push(text(axisX-15,y(0)+23,'O'),text(475,y(0)+6,'x'),text(axisX,28,'y'));
      else shapes.push(text(255,288,xLabel),text(60,24,yLabel,{textAnchor:'start'}));
    }
    return h('figure',{className:'kt-data-figure'},h('figcaption',{},title),h('svg',{viewBox:'0 0 500 300',role:'img','aria-label':title+'。'+[xLabel,yLabel].filter(Boolean).join('、')+'。同じ値を下のデータ表で確認できます。'},...shapes),legend,h('details',{},h('summary',{},'図と同じデータを表で読む'),table));
  }
""";
}
