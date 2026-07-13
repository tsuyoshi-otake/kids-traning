namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildPickClockScript()
    {
        return """
clockExplain(h,m,ask,a){if(ask==='hour')return 'みじかい はり が '+h+' を さして いるね。こたえは '+a+'。';if(ask==='minute')return 'ながい はり が さす すうじ ×5 が ふん。'+(m/5)+'×5='+m+'ふん。こたえは '+a+'。';return 'みじかい はり＝じ、ながい はり＝ふん。こたえは '+a+'。';}
  pickTimeUnits(g){const eh=this.rand(1,10),em=[5,10,15,20][this.rand(0,3)],ed=[10,20,30][this.rand(0,2)];const elapsed=[eh+'時'+em+'分 の '+ed+'分後は？',eh+'時'+(em+ed)+'分',[eh+'時'+em+'分',(eh+1)+'時'+em+'分',eh+'時'+(em+ed-5)+'分'],em+'分に '+ed+'分を たすと '+(em+ed)+'分。'];const L=g>=3?[
    ['1分 は 何秒？','60秒',['30秒','100秒','10秒'],'1分 = 60秒。'],
    ['2分 は 何秒？','120秒',['60秒','100秒','200秒'],'1分=60秒 だから 2分=120秒。'],
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    elapsed
  ]:[
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    ['ごぜん は 何時間？','12時間',['10時間','24時間','6時間'],'ごぜんは 12時間、ごごも 12時間。'],
    elapsed
  ];const it=L[this.rand(0,L.length-1)];return{topic:'clock',mode:'choices',prompt:it[0],answer:it[1],choices:this.pick4(it[1],it[2]),explanation:it[3]};}
  pickClock(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'clock');if(g>=2&&stage>=5&&Math.random()<0.7)return this.pickTimeUnits(g);if(g>=2&&stage===4&&Math.random()<0.2)return this.pickTimeUnits(g);const hourStr=x=>((x-1+12)%12+1)+'じ';const kinds=stage<=1?['hour','hour']:stage===2?['hour','hour','half']:stage===3?['hour','half','minute']:(g>=2?['hour','half','minute','both']:['hour','hour','half','minute']);const k=kinds[this.rand(0,kinds.length-1)];let h=this.rand(1,12),m=0,ask='hour',prompt='なんじ？',a='',pool=[];
    if(k==='hour'){m=0;ask='hour';prompt='とけいを よもう ・ なんじ？';a=h+'じ';pool=[hourStr(h+1),hourStr(h-1),hourStr(h+2),hourStr(h+3)];}
    else if(k==='half'){m=30;ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ30ぷん';pool=[hourStr(h+1).replace('じ','じ30ぷん'),h+'じ',hourStr(h-1).replace('じ','じ30ぷん'),hourStr(h+2).replace('じ','じ30ぷん')];}
    else if(k==='minute'){const mins=[5,10,15,20,25,35,40,45,50,55];m=mins[this.rand(0,mins.length-1)];ask='minute';prompt='ながい はりを よもう ・ なんぷん？';a=m+'ふん';pool=[5,10,15,20,25,30,35,40,45,50,55].filter(x=>x!==m).map(x=>x+'ふん');}
    else{const mins=[10,15,20,40,45,50];m=mins[this.rand(0,mins.length-1)];ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ'+m+'ふん';pool=[hourStr(h+1).replace('じ','じ'+m+'ふん'),h+'じ'+(m===15?45:15)+'ふん',hourStr(h-1).replace('じ','じ'+m+'ふん'),h+'じ'];}
    return{topic:'clock',mode:'choices',isClock:true,h:h,m:m,ask:ask,prompt:prompt,answer:a,choices:this.pick4(a,pool),explanation:this.clockExplain(h,m,ask,a)};}
  measureCompare(){const kinds=[['length','どちらが ながい？','ながい','ます','こぶん'],['volume','どちらが たくさん はいる？','たくさん はいる','コップ','はいぶん'],['area','どちらが ひろい？','ひろい','ます','こぶん']];const kk=kinds[this.rand(0,2)];let n1=this.rand(3,9),n2=this.rand(3,9);while(n2===n1)n2=this.rand(3,9);const win=n1>n2?'あか':'あお';return{topic:'measure',mode:'choices',isMeasure:true,mkind:kk[0],m1:n1,m2:n2,prompt:kk[1],answer:win,choices:this.shuffle(['あか','あお']),explanation:'あかは '+kk[3]+' '+n1+kk[4]+'、あおは '+kk[3]+' '+n2+kk[4]+'。'+win+'の ほうが '+kk[2]+'。'};}
  pickMeasure(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'measure');if(g<=1)return this.measureCompare();const mc=(pr,ans,pool,ex)=>({topic:'measure',mode:'choices',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    Q.push(()=>mc('1cm は 何mm？','10mm',['1mm','100mm','5mm'],'1cm = 10mm。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'cm は 何mm？',(k*10)+'mm',[k+'mm',(k*100)+'mm',(k*10+5)+'mm'],'1cm = 10mm。'+k+'cm = '+(k*10)+'mm。');});
    Q.push(()=>{const k=this.rand(2,9);return mc((k*10)+'mm は 何cm？',k+'cm',[(k*10)+'cm',(k+1)+'cm',(k*100)+'cm'],'10mm = 1cm。'+(k*10)+'mm = '+k+'cm。');});
    Q.push(()=>{const k=this.rand(1,9);return mc(k+'m は 何cm？',(k*100)+'cm',[(k*10)+'cm',(k*1000)+'cm',(k*100+10)+'cm'],'1m = 100cm。'+k+'m = '+(k*100)+'cm。');});
    Q.push(()=>mc('1L は 何dL？','10dL',['100dL','1dL','5dL'],'1L = 10dL。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'L は 何dL？',(k*10)+'dL',[k+'dL',(k*100)+'dL',(k*10+5)+'dL'],'1L = 10dL。'+k+'L = '+(k*10)+'dL。'); });
    Q.push(()=>mc('1L は 何mL？','1000mL',['100mL','10mL','500mL'],'1L = 1000mL。'));
    Q.push(()=>mc('1dL は 何mL？','100mL',['10mL','1000mL','50mL'],'1dL = 100mL。'));
    Q.push(()=>{const its=[['えんぴつの ながさ','cm',['mm','m','L']],['プールの たての ながさ','m',['cm','mm','dL']],['ありの おおきさ','mm',['cm','m','kg']],['ぎゅうにゅうパックの かさ','L',['cm','m','g']]];const it=its[this.rand(0,its.length-1)];return mc(it[0]+' に あう たんいは？',it[1],it[2],it[0]+' は '+it[1]+' が ぴったり。');});
    if(stage>=3){
    Q.push(()=>{const x=this.rand(1,6)*10,y=this.rand(1,Math.min(6,Math.floor((90-x)/10)))*10;return mc(x+'cm + '+y+'cm は？',(x+y)+'cm',[(x+y-10)+'cm',(x+y+10)+'cm',(x+y)+'mm'],x+'cm + '+y+'cm = '+(x+y)+'cm。');});
    Q.push(()=>{const a2=this.rand(2,4),b2=this.rand(1,5);return mc(a2+'L'+b2+'dL は 何dL？',(a2*10+b2)+'dL',[(a2+b2)+'dL',(a2*10)+'dL',(a2*100+b2)+'dL'],a2+'L = '+(a2*10)+'dL。あわせて '+(a2*10+b2)+'dL。');});
    Q.push(()=>{const c=this.rand(2,8),d=this.rand(1,9);return mc(c+'cm'+d+'mm は 何mm？',(c*10+d)+'mm',[(c+d)+'mm',(c*10)+'mm',(c*100+d)+'mm'],c+'cm = '+(c*10)+'mm。あわせて '+(c*10+d)+'mm。');});
    }
    if(stage>=5){
    Q.push(()=>{const m=this.rand(2,8),cm=this.rand(1,9)*10,total=m*100+cm;return mc(m+'m'+cm+'cm は 何cm？',total+'cm',[(m*10+cm)+'cm',(total+10)+'cm',(total-10)+'cm'],m+'m = '+(m*100)+'cm。あわせて '+total+'cm。');});
    Q.push(()=>{const l=this.rand(2,8),dl=this.rand(1,9),total=l*10+dl;return mc(l+'L'+dl+'dL は 何dL？',total+'dL',[(l+dl)+'dL',(l*100+dl)+'dL',(total-1)+'dL'],l+'L = '+(l*10)+'dL。あわせて '+total+'dL。');});
    }
    if(g>=3){
    Q.push(()=>mc('1km は 何m？','1000m',['100m','10m','10000m'],'1km = 1000m。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'km は 何m？',(k*1000)+'m',[(k*100)+'m',(k*10)+'m',(k*10000)+'m'],'1km = 1000m。'+k+'km = '+(k*1000)+'m。');});
    Q.push(()=>mc('1kg は 何g？','1000g',['100g','10g','10000g'],'1kg = 1000g。'));
    Q.push(()=>{const k=this.rand(2,9);return mc(k+'kg は 何g？',(k*1000)+'g',[(k*100)+'g',(k*10)+'g',k+'g'],'1kg = 1000g。'+k+'kg = '+(k*1000)+'g。');});
    Q.push(()=>{const k=this.rand(1,9);return mc('1kg'+(k*100)+'g は 何g？',(1000+k*100)+'g',[(100+k*100)+'g',(k*100)+'g',(1000+k*10)+'g'],'1kg = 1000g。あわせて '+(1000+k*100)+'g。');});
    Q.push(()=>{const x=this.rand(2,7)*100,y=1000-x;return mc(x+'g + '+y+'g は 何kg？','1kg',['2kg','10kg','100g'],x+'g + '+y+'g = 1000g = 1kg。');});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickKazu(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kazu');const mc=(pr,ans,pool,ex)=>({topic:'kazu',mode:'choices',prompt:pr,answer:''+ans,choices:this.pick4(''+ans,pool.map(String)),explanation:ex});const cmp=(a,b)=>({topic:'kazu',mode:'choices',prompt:'大きい ほうは？ '+a+' か '+b,answer:''+Math.max(a,b),choices:this.shuffle([''+a,''+b]),explanation:'くらべると '+Math.max(a,b)+' の ほうが 大きい。'});if(stage>=5&&Math.random()<0.65){const place=g<=1?10:(g===2?100:1000),base=this.rand(2,8)*place,delta=this.rand(1,9)*(place/10),ans=base+delta;return mc(base+' より '+delta+' 大きい 数は？',ans,[base-delta,base+place,ans+place/10],base+' + '+delta+' = '+ans+'。');}const Q=[];
    if(g<=1){
    Q.push(()=>{const t=this.rand(1,9),o=this.rand(1,9),n=t*10+o;return mc('10が '+t+'こ と 1が '+o+'こ で いくつ？',n,[n+1,n-1,t+o],'10が '+t+'こで '+(t*10)+'。あと '+o+' で '+n+'。');});
    Q.push(()=>{const n=this.rand(11,98);return mc(n+' の つぎの 数は？',n+1,[n-1,n+2,n+10],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const n=this.rand(12,99);return mc(n+' の 1つ まえの 数は？',n-1,[n+1,n-2,n-10],n+' の まえは '+(n-1)+'。');});
    Q.push(()=>{let a=this.rand(10,99),b=this.rand(10,99);while(b===a)b=this.rand(10,99);return cmp(a,b);});
    Q.push(()=>{const t=this.rand(2,9);return mc('10を '+t+'こ あつめた 数は？',t*10,[t,t*10+1,t+10],'10が '+t+'こで '+(t*10)+'。');});
    Q.push(()=>{const n=this.rand(4,8),pos=this.rand(1,n),dir=Math.random()<0.5?'ひだり':'みぎ';const pool=[pos-1,pos+1,pos-2,pos+2,n-pos+1,n-pos].filter(v=>v>=1&&v<=n&&v!==pos).map(v=>v+'ばんめ');return{topic:'kazu',mode:'choices',isOrder:true,oc:n,op:pos,od:dir,prompt:'オレンジの ますは '+dir+'から なんばんめ？',answer:pos+'ばんめ',choices:this.pick4(pos+'ばんめ',pool),explanation:dir+'から かぞえて '+pos+'ばんめ だよ。'};});
    }else if(g===2){
    Q.push(()=>{const h=this.rand(1,9),t=this.rand(0,9),o=this.rand(0,9),n=h*100+t*10+o;return mc('100が '+h+'こ、10が '+t+'こ、1が '+o+'こ の 数は？',n,[n+100,n+10,h+t+o],'100が '+h+'こで '+(h*100)+'。あわせて '+n+'。');});
    Q.push(()=>{const n=this.rand(101,998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const t=this.rand(11,99);return mc('10を '+t+'こ あつめた 数は？',t*10,[t*100,t+10,t*10+10],'10が '+t+'こで '+(t*10)+'。');});
    Q.push(()=>{const a=this.rand(100,900),b=a+this.rand(1,90);return cmp(a,b);});
    Q.push(()=>{const h=this.rand(2,9);return mc((h*100)+' は 100を なんこ あつめた 数？',h+'こ',[(h*10)+'こ',(h+1)+'こ',(h*100)+'こ'],'100が '+h+'こで '+(h*100)+'。');});
    Q.push(()=>{const th=this.rand(1,9),h=this.rand(1,9),n=th*1000+h*100;return mc('1000が '+th+'こ と 100が '+h+'こ の 数は？',n,[n+1000,th*100+h*10,n+100],'1000が '+th+'こで '+(th*1000)+'。あわせて '+n+'。');});
    Q.push(()=>{const n=this.rand(1001,9998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    }else{
    Q.push(()=>{const m=this.rand(1,9),s=this.rand(1,9),n=m*10000+s*1000;return mc('一万を '+m+'こ、千を '+s+'こ あわせた 数は？',n,[m*1000+s*100,n+1000,n-1000],'一万が '+m+'こで '+(m*10000)+'。あわせて '+n+'。');});
    Q.push(()=>mc('1000万を 10こ あつめた 数は？',100000000,[10000000,1000000,1000000000],'1000万が 10こで 1億（100000000）。'));
    Q.push(()=>{const t=this.rand(2,9),n=t*10000000;return mc('1000万を '+t+'こ あつめた 数は？',n,[t*1000000,n+10000000,n-1000000],'1000万が '+t+'こで '+n+'。');});
    Q.push(()=>{const n=this.rand(2,9)*10;return mc(n+' を 10ばい した 数は？',n*10,[n,n*100,n+10],n+'×10='+(n*10)+'。');});
    Q.push(()=>{const n=this.rand(2,9)*100;return mc(n+' を 10で わった 数は？',n/10,[n*10,n/100,n],n+'÷10='+(n/10)+'。');});
    Q.push(()=>{const n=this.rand(2,9);return mc(n+' を 100ばい した 数は？',n*100,[n*10,n*1000,n+100],n+'×100='+(n*100)+'。');});
    Q.push(()=>{const n=this.rand(1001,9998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');});
    Q.push(()=>{const a=this.rand(1000,9000),b=a+this.rand(10,900);return cmp(a,b);});
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickShape(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'shape');const S={maru:'width:120px;height:120px;border-radius:50%;background:#f2a03d;border:4px solid #d18426;',shikaku:'width:110px;height:110px;background:#4f9dde;border:4px solid #3a7db8;',chouhoukei:'width:170px;height:95px;background:#4f9dde;border:4px solid #3a7db8;',sankaku:'width:0;height:0;border-left:70px solid transparent;border-right:70px solid transparent;border-bottom:115px solid #52b788;',seisankaku:'width:0;height:0;border-left:65px solid transparent;border-right:65px solid transparent;border-bottom:113px solid #52b788;',nitohen:'width:0;height:0;border-left:45px solid transparent;border-right:45px solid transparent;border-bottom:125px solid #b788d4;',chokkaku:'width:0;height:0;border-bottom:110px solid #e0708a;border-right:110px solid transparent;'};const sq=(pr,ans,pool,ex,style)=>({topic:'shape',mode:'choices',isShape:!!style,shapeStyle:style||'',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    if(g<=1||stage<=1){
    Q.push(()=>sq('この かたちの なまえは？','まる',['さんかく','しかく','ながしかく'],'まるい かたちは「まる」。',S.maru));
    Q.push(()=>sq('この かたちの なまえは？','さんかく',['まる','しかく','ながしかく'],'かどが 3つ ある かたちは「さんかく」。',S.sankaku));
    Q.push(()=>sq('この かたちの なまえは？','しかく',['まる','さんかく','ほし'],'かどが 4つ ある かたちは「しかく」。',S.shikaku));
    Q.push(()=>sq('さんかくの かどは いくつ？','3つ',['4つ','2つ','5つ'],'さんかくには かどが 3つ あるよ。',S.sankaku));
    }
    if(g>=2&&stage>=2){
    Q.push(()=>sq('この かたちの なまえは？','正方形',['長方形','直角三角形','円'],'4つの へんの 長さが みんな 同じ 四角形は 正方形。',S.shikaku));
    Q.push(()=>sq('この かたちの なまえは？','長方形',['正方形','直角三角形','円'],'かどが みんな 直角で、むかいあう へんの 長さが 同じ 四角形は 長方形。',S.chouhoukei));
    Q.push(()=>sq('この かたちの なまえは？','直角三角形',['正三角形','長方形','円'],'直角の かどが ある 三角形は 直角三角形。',S.chokkaku));
    Q.push(()=>sq('三角形の へんの 数は？','3',['4','2','6'],'三角形は 3本の 直線で かこまれた 形。'));
    Q.push(()=>sq('四角形の ちょう点の 数は？','4',['3','2','6'],'四角形には ちょう点が 4つ。'));
    Q.push(()=>sq('はこの形の 面の 数は？','6',['4','8','12'],'はこの形には 面が 6つ。'));
    Q.push(()=>sq('はこの形の ちょう点の 数は？','8',['6','4','12'],'はこの形には ちょう点が 8つ。'));
    Q.push(()=>sq('はこの形の へんの 数は？','12',['6','8','10'],'はこの形には へんが 12本。'));
    }
    if(g>=3&&stage>=4){
    Q.push(()=>sq('この 三角形の なまえは？','正三角形',['二等辺三角形','直角三角形','長方形'],'3つの へんが みんな 同じ 長さの 三角形は 正三角形。',S.seisankaku));
    Q.push(()=>sq('この 三角形の なまえは？','二等辺三角形',['正三角形','直角三角形','正方形'],'2つの へんの 長さが 同じ 三角形は 二等辺三角形。',S.nitohen));
    Q.push(()=>{const r=this.rand(2,9);return sq('半径 '+r+'cm の 円の 直径は？',(r*2)+'cm',[r+'cm',(r+2)+'cm',(r*4)+'cm'],'直径は 半径の 2ばい。'+r+'×2='+(r*2)+'cm。',S.maru);});
    Q.push(()=>{const d=this.rand(2,8)*2;return sq('直径 '+d+'cm の 円の 半径は？',(d/2)+'cm',[d+'cm',(d/2+1)+'cm',(d*2)+'cm'],'半径は 直径の 半分。'+d+'÷2='+(d/2)+'cm。',S.maru);});
    Q.push(()=>sq('どこから 見ても 円に 見える 形は？','球',['円','正方形','はこの形'],'ボールのような 形は 球。'));
    Q.push(()=>sq('1つの ちょう点から 出た 2本の 直線が つくる 形を なんと いう？','角',['円','へん','ちょう点'],'2本の 直線の 間に できる 形が 角。'));
    Q.push(()=>sq('紙を きちんと 2回 おって できる かどを なんと いう？','直角',['角','半円','正三角形'],'きちんと 2回 おって できる かどが 直角。三角じょうぎにも あるよ。'));
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickDiv(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'div'),exact=(d,q0)=>{const n=d*q0;return{topic:'div',mode:'choices',op:'div',d:d,q0:q0,n:n,prompt:n+' ÷ '+d,answer:''+q0,choices:this.pick4(''+q0,[q0+1,Math.max(1,q0-1),q0+2,q0+3,d,d+1].map(String)),explanation:d+'×'+q0+'='+n+' だから '+n+'÷'+d+'='+q0+'。'};};const buckets=[
    [()=>exact(2,1),()=>exact(2,2),()=>exact(2,3)],
    [()=>exact([2,3,4][this.rand(0,2)],this.rand(1,5))],
    [()=>exact(this.rand(2,9),this.rand(1,9))],
    [()=>{const d=this.rand(2,5),q0=this.rand(10,19);return exact(d,q0);}],
    [()=>{const d=this.rand(2,9),q0=this.rand(3,19),r=this.rand(1,d-1),n=d*q0+r,ans=q0+' あまり '+r;return{topic:'div',mode:'choices',op:'div',d:d,q0:q0,n:n,prompt:n+' ÷ '+d,answer:ans,choices:this.pick4(ans,[q0+' あまり '+(r===1?2:r-1),(q0+1)+' あまり '+r,(q0-1)+' あまり '+r]),explanation:d+'×'+q0+'='+(d*q0)+'。'+n+'−'+(d*q0)+'='+r+' だから '+ans+'。'};},()=>{const d=this.rand(2,6),q0=this.rand(15,29);return exact(d,q0);}]
  ];return this.pickStage(stage,buckets,0);}
  pickFrac(p){const stage=this.topicStage(p,'frac');const buckets=[
    [()=>{const d=[2,4][this.rand(0,1)];return{topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:1,prompt:'いろの ついた ところは もとの 大きさの どれだけ？',answer:'1/'+d,choices:this.pick4('1/'+d,['1/'+(d===2?4:2),'1/3',d+'/1']),explanation:'同じ 大きさに '+d+'つに 分けた 1つ分は 1/'+d+'。'};}],
    [()=>{const d=this.rand(3,8),k=this.rand(1,d-1);return{topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:k,prompt:'いろの ついた ところは ぜんたいの どれだけ？',answer:k+'/'+d,choices:this.pick4(k+'/'+d,[(d-k)+'/'+d,k+'/'+(d+1),d+'/'+d]),explanation:'ぜんたいを '+d+'つに 分けた '+k+'つ分で '+k+'/'+d+'。'};}],
    [()=>({topic:'frac',mode:'choices',prompt:'1を 10こに 分けた 1こ分を 小数で あらわすと？',answer:'0.1',choices:this.pick4('0.1',['0.01','1.0','10']),explanation:'1の 1/10 は 0.1。'}),()=>{const k=this.rand(2,9);return{topic:'frac',mode:'choices',prompt:'0.1を '+k+'こ あつめた 数は？',answer:'0.'+k,choices:this.pick4('0.'+k,[''+k,'0.'+(k===9?8:k+1),k+'.0']),explanation:'0.1が '+k+'こで 0.'+k+'。'};}],
    [()=>{const d=this.rand(4,9),a=this.rand(1,d-2),b=this.rand(1,d-1-a);return{topic:'frac',mode:'choices',prompt:a+'/'+d+' + '+b+'/'+d+' は？',answer:(a+b)+'/'+d,choices:this.pick4((a+b)+'/'+d,[(a+b)+'/'+(d*2),Math.max(1,a+b-1)+'/'+d,(a+b+1)+'/'+d]),explanation:'1/'+d+' が '+(a+b)+'こ分で '+(a+b)+'/'+d+'。'};}],
    [()=>{const d=this.rand(5,9),a=this.rand(2,d-1),b=this.rand(1,a-1);return{topic:'frac',mode:'choices',prompt:a+'/'+d+' − '+b+'/'+d+' は？',answer:(a-b)+'/'+d,choices:this.pick4((a-b)+'/'+d,[(a+b)+'/'+d,Math.max(1,a-b-1)+'/'+d,(a-b)+'/'+(d+1)]),explanation:'1/'+d+' が '+(a-b)+'こ分 のこるので '+(a-b)+'/'+d+'。'};},()=>{const a=this.rand(5,9),b=this.rand(1,a-1);return{topic:'frac',mode:'choices',prompt:'0.'+a+' − 0.'+b+' は？',answer:'0.'+(a-b),choices:this.pick4('0.'+(a-b),['0.'+(a-b+1),(a-b)+'','0.'+Math.min(9,a-b+2)]),explanation:'0.1が '+(a-b)+'こ分で 0.'+(a-b)+'。'};}]
  ];return this.pickStage(stage,buckets,0);}
  pickChart(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'chart');const items=this.shuffle([['りんご','#e05a4e','#b8443a'],['みかん','#f2a03d','#d18426'],['ばなな','#d4c22f','#b0a020'],['ぶどう','#9a4fd6','#7a3aad']]).slice(0,3);let counts;do{counts=[this.rand(2,9),this.rand(2,9),this.rand(2,9)];}while(new Set(counts).size<3);const scale=(g>=3&&stage>=4)?5:(g>=3&&stage>=3?2:1),unit=scale>1?'人':'こ';const rows=items.map((it,i)=>({label:it[0],color:it[1],border:it[2],count:counts[i]}));const maxI=counts.indexOf(Math.max.apply(null,counts)),minI=counts.indexOf(Math.min.apply(null,counts));if(stage>=5){const diff=(counts[maxI]-counts[minI])*scale;return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:items[maxI][0]+' は '+items[minI][0]+' より いくつ 多い？',answer:''+diff,choices:this.pick4(''+diff,[diff+scale,Math.max(scale,diff-scale),diff+2*scale].map(String)),explanation:(counts[maxI]*scale)+'−'+(counts[minI]*scale)+'='+diff+unit+'。'};}const kind=this.rand(0,2);
    if(kind===0)return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:'いちばん 多いのは どれ？',answer:items[maxI][0],choices:this.shuffle(items.map(x=>x[0])),explanation:items[maxI][0]+' が '+(counts[maxI]*scale)+unit+' で いちばん 多い。'};
    if(kind===1)return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:'いちばん 少ないのは どれ？',answer:items[minI][0],choices:this.shuffle(items.map(x=>x[0])),explanation:items[minI][0]+' が '+(counts[minI]*scale)+unit+' で いちばん 少ない。'};
    const t=this.rand(0,2);return{topic:'chart',mode:'choices',isChart:true,rows:rows,prompt:(scale>1?'1ますは '+scale+'人。':'')+items[t][0]+' は いくつ？',answer:''+(counts[t]*scale),choices:this.pick4(''+(counts[t]*scale),[counts[t]*scale+scale,Math.max(1,counts[t]*scale-scale),counts[t]*scale+2*scale].map(String)),explanation:'ますを 数えると '+counts[t]+'こ。'+(scale>1?'1ます '+scale+'人 だから '+(counts[t]*scale)+'人。':'')};}
  pickStory(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'story');const items=[['りんご','こ'],['えんぴつ','本'],['シール','まい'],['おはじき','こ']];const it=items[this.rand(0,items.length-1)];const Q=[];
    Q.push(()=>{const a=this.rand(3,9),b=this.rand(2,9);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' もらうと ぜんぶで なん'+it[1]+'？',answer:''+(a+b),explanation:'あわせる ときは たしざん。'+a+'+'+b+'='+(a+b)+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' つかうと のこりは なん'+it[1]+'？',answer:''+(a-b),explanation:'のこりを もとめる ときは ひきざん。'+a+'−'+b+'='+(a-b)+'。'};});
    Q.push(()=>{const a=this.rand(3,9);let b=this.rand(2,9);while(b===a)b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' ふえた。あう しきは？',answer:a+'＋'+b,choices:this.pick4(a+'＋'+b,[a+'−'+b,b+'−'+a,a+'×'+b]),explanation:'ふえる ときは たしざん。しきは '+a+'＋'+b+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' あげた。あう しきは？',answer:a+'−'+b,choices:this.pick4(a+'−'+b,[a+'＋'+b,b+'−'+a,a+'×'+b]),explanation:'へる ときは ひきざん。しきは '+a+'−'+b+'。'};});
    if(g>=2&&stage>=3&&this.topicComplete(p,'mul'))Q.push(()=>{const a=this.rand(2,9),b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:'1さらに '+it[0]+'が '+a+it[1]+'ずつ、'+b+'さら分。あう しきは？',answer:a+'×'+b,choices:this.pick4(a+'×'+b,[a+'＋'+b,a+'−'+b,a+'÷'+b]),explanation:'同じ数ずつ あるときは かけざん。しきは '+a+'×'+b+'。'};});
    if(g>=3&&stage>=4){
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ + '+b+' = '+(ans+b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans+b)+' から '+b+' を ひくと '+ans+'。'};});
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ × '+b+' = '+(ans*b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans*b)+' ÷ '+b+' = '+ans+'。'};});
    if(this.topicComplete(p,'div'))Q.push(()=>{const d=this.rand(2,9),q0=this.rand(2,9);return{topic:'story',mode:'num',prompt:(d*q0)+it[1]+'の '+it[0]+'を '+d+'人で 同じ数ずつ 分けると 1人分は なん'+it[1]+'？',answer:''+q0,explanation:'分ける ときは わりざん。'+(d*q0)+'÷'+d+'='+q0+'。'};});
    }
    if(stage>=5)Q.push(()=>{const a=this.rand(8,18),b=this.rand(3,9),c=this.rand(2,Math.min(8,a+b-1)),ans=a+b-c;return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' もらって、'+c+it[1]+' つかった。のこりは？',answer:''+ans,explanation:a+'+'+b+'='+(a+b)+'、'+(a+b)+'−'+c+'='+ans+'。'};});
    return Q[this.rand(0,Q.length-1)]();}
""";
    }

}
