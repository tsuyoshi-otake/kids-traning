namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildPickClockScript()
    {
        return """
// 1・3・4・6・8・10 で おわる 分は「ぷん」、それ以外は「ふん」と よむ。
  minuteWord(m){return [0,1,3,4,6,8].indexOf(m%10)>=0?'ぷん':'ふん';}
  minuteText(m){return m+this.minuteWord(m);}
  clockExplain(h,m,ask,a){if(ask==='hour')return 'みじかい はり が '+h+' を さして いるね。こたえは '+a+'。';if(ask==='minute')return 'ながい はり が さす すうじ ×5 が ふん。'+(m/5)+'×5='+this.minuteText(m)+'。こたえは '+a+'。';return 'みじかい はり＝じ、ながい はり＝ふん。こたえは '+a+'。';}
  pickTimeUnits(g){const eh=this.rand(1,10),em=[5,10,15,20][this.rand(0,3)],ed=[10,20,30][this.rand(0,2)];const elapsed=[eh+'時'+em+'分 の '+ed+'分後は？',eh+'時'+(em+ed)+'分',[eh+'時'+em+'分',(eh+1)+'時'+em+'分',eh+'時'+(em+ed-5)+'分'],em+'分に '+ed+'分を たすと '+(em+ed)+'分。'];
    // No.7-8: なん分まえ・経過時間・午前と午後。
    const bh=this.rand(1,10),bm=[25,30,40,50][this.rand(0,3)],bd=[10,15,20][this.rand(0,2)];
    const before=[bh+'時'+bm+'分 の '+bd+'分前は？',bh+'時'+(bm-bd)+'分',[bh+'時'+bm+'分',(bh+1)+'時'+(bm-bd)+'分',bh+'時'+(bm-bd+5)+'分'],bh+'時'+bm+'分の '+bd+'分まえは '+bh+'時'+(bm-bd)+'分。','minutes-before'];
    const h1=this.rand(8,10),h2=this.rand(1,8),hours=h2+12-h1;
    const hoursElapsed=['午前'+h1+'時 から 午後'+h2+'時 まで 何時間？',hours+'時間',[(hours-1)+'時間',(hours+1)+'時間',(hours+2)+'時間'],'午後'+h2+'時は '+(h2+12)+'時。'+(h2+12)+'−'+h1+'='+hours+'時間。','elapsed-hours'];
    const mh=this.rand(1,10),mm=[5,10,15,20][this.rand(0,3)],md=[10,20,30][this.rand(0,2)];
    const minsElapsed=[mh+'時'+mm+'分 から '+mh+'時'+(mm+md)+'分 まで 何分？',md+'分',[(md+5)+'分',(md-5)+'分',(md+10)+'分'],(mm+md)+'−'+mm+'='+md+'分。','elapsed-minutes'];
    const L=g>=3?[
    ['1分 は 何秒？','60秒',['30秒','100秒','10秒'],'1分 = 60秒。'],
    ['2分 は 何秒？','120秒',['60秒','100秒','200秒'],'1分=60秒 だから 2分=120秒。'],
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    elapsed
  ]:[
    ['1時間 は 何分？','60分',['30分','100分','10分'],'1時間 = 60分。'],
    ['1日 は 何時間？','24時間',['12時間','20時間','10時間'],'1日 = 24時間。'],
    ['ごぜん は 何時間？','12時間',['10時間','24時間','6時間'],'ごぜんは 12時間、ごごも 12時間。'],
    elapsed,
    ...(g>=2?[before,hoursElapsed,minsElapsed]:[])
  ];const it=L[this.rand(0,L.length-1)];return{topic:'clock',mode:'choices',subtype:it[4],prompt:it[0],answer:it[1],choices:this.pick4(it[1],it[2]),explanation:it[3]};}
  pickWeekday(stage){const days=['月曜日','火曜日','水曜日','木曜日','金曜日','土曜日','日曜日'],short=['月','火','水','木','金','土','日'],base=this.rand(0,6),direction=stage===2&&Math.random()<.5?-1:1,steps=stage<=2?1:(stage<=4?this.rand(2,3):[4,5,6,8,9,10][this.rand(0,5)]),answerIndex=(base+direction*steps%7+7)%7,answer=days[answerIndex],pool=days.filter(x=>x!==answer);let prompt,explanation;if(stage<=1){prompt=days[base]+' の つぎの ようびは？';explanation=days[base]+' の つぎは '+answer+'。';}else if(stage===2&&direction<0){prompt=days[base]+' の まえの ようびは？';explanation=days[base]+' の まえは '+answer+'。';}else{prompt=days[base]+' の '+steps+'日'+(direction<0?'まえ':'あと')+'は 何曜日？';explanation=short.join('→')+' の じゅんに '+steps+'日 '+(direction<0?'もどる':'すすむ')+'と '+answer+'。';}return{topic:'clock',mode:'choices',subtype:'weekday',prompt:prompt,answer:answer,choices:this.pick4(answer,pool),explanation:explanation};}
  pickClock(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'clock');if(Math.random()<(stage>=3?.3:.2))return this.pickWeekday(stage);if(g>=2&&stage>=5&&Math.random()<0.7)return this.pickTimeUnits(g);if(g>=2&&stage===4&&Math.random()<0.2)return this.pickTimeUnits(g);const hourStr=x=>((x-1+12)%12+1)+'じ';const kinds=stage<=1?['hour','hour']:stage===2?['hour','hour','half']:stage===3?['hour','half','minute']:(g>=2?['hour','half','minute','both']:['hour','hour','half','minute']);const k=kinds[this.rand(0,kinds.length-1)];let h=this.rand(1,12),m=0,ask='hour',prompt='なんじ？',a='',pool=[];
    if(k==='hour'){m=0;ask='hour';prompt='とけいを よもう ・ なんじ？';a=h+'じ';pool=[hourStr(h+1),hourStr(h-1),hourStr(h+2),hourStr(h+3)];}
    else if(k==='half'){m=30;ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ30ぷん';pool=[hourStr(h+1).replace('じ','じ30ぷん'),h+'じ',hourStr(h-1).replace('じ','じ30ぷん'),hourStr(h+2).replace('じ','じ30ぷん')];}
    else if(k==='minute'){const mins=[5,10,15,20,25,35,40,45,50,55];m=mins[this.rand(0,mins.length-1)];ask='minute';prompt='ながい はりを よもう ・ なんぷん？';a=this.minuteText(m);pool=[5,10,15,20,25,30,35,40,45,50,55].filter(x=>x!==m).map(x=>this.minuteText(x));}
    else{const mins=[10,15,20,40,45,50];m=mins[this.rand(0,mins.length-1)];ask='both';prompt='とけいを よもう ・ なんじ なんぷん？';a=h+'じ'+this.minuteText(m);pool=[hourStr(h+1).replace('じ','じ'+this.minuteText(m)),h+'じ'+this.minuteText(m===15?45:15),hourStr(h-1).replace('じ','じ'+this.minuteText(m)),h+'じ'];}
    return{topic:'clock',mode:'choices',isClock:true,h:h,m:m,ask:ask,prompt:prompt,answer:a,choices:this.pick4(a,pool),explanation:this.clockExplain(h,m,ask,a)};}
  // 3本を くらべる。ときどき ぜんぶ同じにして「どれも おなじ」も 正解になるようにし、
  // 2たくの あてずっぽう（せいかい率50%）を なくす。
  measureCompare(){const kinds=[['length','いちばん ながいのは どれ？','ながい','ます','こぶん'],['volume','いちばん たくさん はいるのは どれ？','たくさん はいる','コップ','はいぶん'],['area','いちばん ひろいのは どれ？','ひろい','ます','こぶん']];const kk=kinds[this.rand(0,2)];const same=this.rand(1,5)===1;let n1=this.rand(3,9),n2=this.rand(3,9),n3=this.rand(3,9);if(same){n2=n1;n3=n1;}else{while(n2===n1)n2=this.rand(3,9);while(n3===n1||n3===n2)n3=this.rand(3,9);}
    const names=['あか','あお','きいろ'],vals=[n1,n2,n3],top=Math.max(n1,n2,n3),win=same?'どれも おなじ':names[vals.indexOf(top)];return{topic:'measure',mode:'choices',isMeasure:true,mkind:kk[0],m1:n1,m2:n2,m3:n3,prompt:kk[1],answer:win,choices:this.shuffle(['あか','あお','きいろ','どれも おなじ']),explanation:names.map((nm,i)=>nm+'は '+kk[3]+' '+vals[i]+kk[4]).join('、')+'。'+(same?'どれも おなじだね。':(win+'が いちばん '+kk[2]+'。'))};}
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
    if(g===2&&stage>=3){const cmmm=t=>t%10===0?(t/10)+'cm':Math.floor(t/10)+'cm'+(t%10)+'mm';
    // No.9-10: mm を cm・mm に かえて よむ（46mm = 4cm6mm）。
    Q.push(()=>{const c=this.rand(1,9),d=this.rand(1,9),t=c*10+d,q=mc(t+'mm は 何cm何mm？',c+'cm'+d+'mm',[(c+1)+'cm'+d+'mm',c+'cm',t+'cm'],'10mm で 1cm。'+t+'mm = '+c+'cm'+d+'mm。');q.subtype='mm-cmmm';return q;});
    // No.10: cm・mm の たしざん・ひきざん。
    Q.push(()=>{const t1=this.rand(11,59),t2=this.rand(11,Math.min(59,99-t1)),t=t1+t2,q=mc(cmmm(t1)+' + '+cmmm(t2)+' は？',cmmm(t),[cmmm(t+10),cmmm(t+20),t+'mm'],cmmm(t1)+' と '+cmmm(t2)+' を あわせて '+cmmm(t)+'。');q.subtype='cmmm-add';return q;});
    Q.push(()=>{const t1=this.rand(22,99),t2=this.rand(11,t1-11),t=t1-t2,q=mc(cmmm(t1)+' - '+cmmm(t2)+' は？',cmmm(t),[cmmm(t+10),cmmm(t+20),t+'mm'],'mm に なおして ひくと '+t+'mm = '+cmmm(t)+'。');q.subtype='cmmm-sub';return q;});
    }
    if(g===2&&stage>=5){
    // No.30: dL を L・dL に かえる（15dL = 1L5dL）。
    Q.push(()=>{const l=this.rand(1,8),d=this.rand(1,9),t=l*10+d,q=mc(t+'dL は 何L何dL？',l+'L'+d+'dL',[(l+1)+'L'+d+'dL',l+'L',t+'L'],'10dL で 1L。'+t+'dL = '+l+'L'+d+'dL。');q.subtype='dl-ldl';return q;});
    // No.31: L・dL の たしざん・ひきざん。
    Q.push(()=>{const a=this.rand(2,9),b=this.rand(11-a,9),t=a+b,q=mc(a+'dL + '+b+'dL は？','1L'+(t-10)+'dL',[t+'dL','2L'+(t-10)+'dL','1L'+t+'dL'],'10dL で 1L。'+t+'dL = 1L'+(t-10)+'dL。');q.subtype='dl-add';return q;});
    Q.push(()=>{const d=this.rand(1,9),q=mc('1L - '+d+'dL は？',(10-d)+'dL',[(11-d)+'dL',((11-d)*100)+'mL','1L'+d+'dL'],'1L = 10dL。10−'+d+'='+(10-d)+'dL。');q.subtype='dl-sub';return q;});
    // No.31: L・dL の まじった たしざん・ひきざん（4L2dL + 1L3dL、2L5dL + 5dL = 3L、9L5dL - 7L2dL、1L3dL - 3dL = 1L、5L6dL - 1L）。
    const ldl=t=>t<10?t+'dL':t%10===0?(t/10)+'L':Math.floor(t/10)+'L'+(t%10)+'dL';
    Q.push(()=>{const aL=this.rand(1,7),aD=this.rand(1,9),bL=Math.random()<.5?this.rand(1,7-aL):0,bD=Math.random()<.4?10-aD:this.rand(1,9),a=aL*10+aD,b=bL*10+bD,t=a+b,q=mc(ldl(a)+' + '+ldl(b)+' は？',ldl(t),[ldl(t+10),ldl(t>=21?t-10:t+20),ldl(t+1),ldl(t-1),bL?ldl(aL*10+aD+bD):ldl(t+2)],'dL に なおすと '+a+'dL + '+b+'dL = '+t+'dL = '+ldl(t)+'。');q.subtype='ldl-mixed';return q;});
    Q.push(()=>{const aL=this.rand(2,9),aD=this.rand(1,9),a=aL*10+aD,form=this.rand(0,2);let b;
      if(form===0){const bD=this.rand(0,9),maxL=bD>aD?aL-2:aL-1,bL=maxL>=1?this.rand(1,maxL):0;b=bL*10+bD;}
      else if(form===1)b=aD;
      else b=this.rand(1,aL-1)*10;
      const t=a-b,q=mc(ldl(a)+' - '+ldl(b)+' は？',ldl(t),[ldl(t+10),ldl(t>=21?t-10:t+20),ldl(t+1),ldl(t-1),ldl(t+2)],'dL に なおして ひくと '+a+'dL - '+b+'dL = '+t+'dL = '+ldl(t)+'。');q.subtype='ldl-mixed';return q;});
    // No.30: mL と L・dL の かえ算（2L = 2000mL、3dL = 300mL、1500mL = 1L5dL）。
    Q.push(()=>{const k=this.rand(2,9),q=mc(k+'L は 何mL？',(k*1000)+'mL',[(k*100)+'mL',(k*10)+'mL',(k*1000+100)+'mL'],'1L = 1000mL。'+k+'L = '+(k*1000)+'mL。');q.subtype='l-ml';return q;});
    Q.push(()=>{const k=this.rand(2,9),q=mc(k+'dL は 何mL？',(k*100)+'mL',[(k*10)+'mL',(k*1000)+'mL',(k*100+10)+'mL'],'1dL = 100mL。'+k+'dL = '+(k*100)+'mL。');q.subtype='dl-ml';return q;});
    Q.push(()=>{const l=this.rand(1,8),d=this.rand(1,9),t=l*10+d,q=mc((t*100)+'mL は 何L何dL？',l+'L'+d+'dL',[(l+1)+'L'+d+'dL',l+'L'+(d+10)+'dL',t+'dL'],'100mL で 1dL、10dL で 1L。'+(t*100)+'mL = '+l+'L'+d+'dL。');q.subtype='ml-ldl';return q;});
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
  pickKazu(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kazu');const mc=(pr,ans,pool,ex)=>({topic:'kazu',mode:'choices',prompt:pr,answer:''+ans,choices:this.pick4(''+ans,pool.map(String)),explanation:ex});const cmp=(a,b)=>{const lo=Math.min(a,b),hi=Math.max(a,b),span=Math.max(4,hi-lo),vals=[a,b];let guard=0;while(vals.length<4&&guard++<50){const v=this.rand(Math.max(0,lo-span),hi+span);if(vals.indexOf(v)<0)vals.push(v);}let extra=hi+1;while(vals.length<4){if(vals.indexOf(extra)<0)vals.push(extra);extra++;}const shown=this.shuffle(vals.slice()),ans=Math.max.apply(null,vals);return{topic:'kazu',mode:'choices',prompt:'いちばん 大きい かずは？ '+shown.join('・'),answer:''+ans,choices:shown.map(String),explanation:'くらべると '+ans+' が いちばん 大きい。'};},count=(n,pr)=>({topic:'kazu',mode:'choices',isCount:true,count:n,prompt:pr||'まるは いくつ？',answer:''+n,choices:this.pick4(''+n,[n+1,Math.max(0,n-1),n+2].map(String)),explanation:'1つずつ かぞえると '+n+'こ。'});if(g<=1){const buckets=[
    [()=>count(this.rand(1,10)),()=>{const n=this.rand(4,8),pos=this.rand(1,n),dir=Math.random()<0.5?'ひだり':'みぎ';const pool=[pos-1,pos+1,n-pos+1,pos+2,pos-2,n,1].filter(v=>v>=1&&v<=n&&v!==pos).map(v=>v+'ばんめ');return{topic:'kazu',mode:'choices',isOrder:true,oc:n,op:pos,od:dir,prompt:'オレンジの ますは '+dir+'から なんばんめ？',answer:pos+'ばんめ',choices:this.pick4(pos+'ばんめ',pool),explanation:dir+'から かぞえて '+pos+'ばんめ。'};},()=>{const start=this.rand(0,5),answer=start+2;return{...mc(start+'、'+(start+1)+'、□、'+(start+3)+'　□は？',answer,[answer-1,answer+1,answer+2],'1ずつ ふえる ならび。□は '+answer+'。'),subtype:'number-sequence'};}],
    [()=>{const total=this.rand(3,10),a=this.rand(1,total-1),b=total-a;return mc(total+'は '+a+'と いくつ？',b,[a,total,b+1,Math.max(0,b-1),total+1],a+'と '+b+'で '+total+'。');},()=>{const a=this.rand(1,8),b=this.rand(1,10-a);return count(a+b,a+'こと '+b+'こを あわせると？');},()=>{const a=this.rand(1,8),b=this.rand(1,10-a),total=a+b;return{...mc(a+'と '+b+'で いくつ？',total,[Math.abs(a-b),total+1,Math.max(0,total-1)],a+'と '+b+'を あわせて '+total+'。'),subtype:'number-compose'};}],
    [()=>{const n=this.rand(11,20);return mc(n+' の 1つ まえは？',n-1,[n+1,n-2,n+10],n+' の 1つ まえは '+(n-1)+'。');},()=>{let a=this.rand(1,20),b=this.rand(1,20);while(a===b)b=this.rand(1,20);return cmp(a,b);},()=>{const start=this.rand(10,15),step=Math.random()<.5?1:2,answer=start+step*2;return{...mc(start+'、'+(start+step)+'、□、'+(start+step*3)+'　□は？',answer,[answer-step,answer+step,answer+1,answer-1,answer+2*step],step+'ずつ ふえる ならび。□は '+answer+'。'),subtype:'number-sequence'};}],
    [()=>{const t=this.rand(1,9),o=this.rand(0,9),n=t*10+o;return mc('10が '+t+'こ と 1が '+o+'こで？',n,[n+10,n-1,t+o],'10が '+t+'こで '+(t*10)+'。あわせて '+n+'。');},()=>{const n=this.rand(21,119);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');}],
    [()=>{const n=this.rand(2,12)*10;return mc('10を '+(n/10)+'こ あつめた 数は？',n,[n/10,n+1,n-10],'10が '+(n/10)+'こで '+n+'。');},()=>{let a=this.rand(21,120),b=this.rand(21,120);while(a===b)b=this.rand(21,120);return cmp(a,b);}]
  ];return this.pickStage(stage,buckets,0);}if(g===2){const kj=n=>{const k='一二三四五六七八九',h=Math.floor(n/100),t=Math.floor(n%100/10),o=n%10;return (h?(h>1?k[h-1]:'')+'百':'')+(t?(t>1?k[t-1]:'')+'十':'')+(o?k[o-1]:'');};const buckets=[
    [()=>{const h=this.rand(1,9),t=this.rand(0,9),o=this.rand(0,9),n=h*100+t*10+o;return mc('100が '+h+'こ、10が '+t+'こ、1が '+o+'こで？',n,[n+100,n+10,h+t+o],'くらいを あわせると '+n+'。');},
    // No.26: 漢字の 数を 数字に おきかえる。
    ()=>{const h=this.rand(1,9),t=this.rand(0,9),o=this.rand(0,9),n=h*100+t*10+o,w=kj(n),q=mc('「'+w+'」を すうじで かくと？',n,[n+100,Math.max(1,n-100),h*10+t],'百・十・一の くらいを あわせると '+n+'。');q.subtype='kanji-num';q.readingTarget=w;return q;}],
    [()=>{const n=this.rand(101,998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');},()=>{let a=this.rand(100,999),b=this.rand(100,999);while(a===b)b=this.rand(100,999);return cmp(a,b);},
    // No.27: 数直線に そった ならび（10・100 きざみ）。
    ()=>{const step=Math.random()<.5?10:100,start=Math.random()<.5?this.rand(2,6)*step:this.rand(11,60)*10,miss=start+step*2,q=mc('数直線の ならび '+start+'、'+(start+step)+'、□、'+(start+step*3)+'　□は？',miss,[miss+step,miss-step,miss+1],step+'ずつ ふえる ならび。□は '+miss+'。');q.subtype='number-line';return q;}],
    [()=>{const t=this.rand(11,99);return mc('10を '+t+'こ あつめた 数は？',t*10,[t*100,t+10,t*10+10],'10が '+t+'こで '+(t*10)+'。');},
    // No.26: 1000の でき方（100を 10こ、999の つぎ、900+100）。
    ()=>{const q=mc('100を 10こ あつめた 数は？',1000,[100,10000,110],'100が 10こで 1000。');q.subtype='make-1000';return q;},
    ()=>{const q=mc('999 の つぎの 数は？',1000,[998,1099,909],'999 の つぎは 1000。');q.subtype='make-1000';return q;},
    ()=>{const h=this.rand(1,9)*100,q=mc(h+' と '+(1000-h)+' で？',1000,[1000-h-100,1000-h+100,h],'1000 に あと '+(1000-h)+'。あわせて 1000。');q.subtype='make-1000';return q;}],
    [()=>{const th=this.rand(1,9),h=this.rand(0,9),n=th*1000+h*100;return mc('1000が '+th+'こ と 100が '+h+'こで？',n,[n+1000,n+100,th*100+h],'あわせて '+n+'。');}],
    [()=>{const n=this.rand(1001,9998);return mc(n+' の つぎの 数は？',n+1,[n-1,n+10,n+100],n+' の つぎは '+(n+1)+'。');}]
  ];return this.pickStage(stage,buckets,0);}const buckets=[
    [()=>{const m=this.rand(1,9),s=this.rand(0,9),n=m*10000+s*1000;return mc('一万を '+m+'こ、千を '+s+'こで？',n,[n+10000,n+1000,m*1000+s*100],'あわせて '+n+'。');}],
    [()=>{const n=this.rand(10001,99999);return mc(n+' の 1つ まえは？',n-1,[n+1,n-10,n-100],n+' の 1つ まえは '+(n-1)+'。');}],
    [()=>{const n=this.rand(2,9)*10;return mc(n+' を 10ばいすると？',n*10,[n,n*100,n+10],n+'×10='+(n*10)+'。');},()=>{const n=this.rand(2,9);return mc(n+' を 100ばいすると？',n*100,[n*10,n*1000,n+100],n+'×100='+(n*100)+'。');},()=>{const n=this.rand(2,9);return mc(n+' を 1000ばいすると？',n*1000,[n*100,n*10000,n+1000],n+'×1000='+(n*1000)+'。');},()=>{const n=this.rand(2,9)*100;return mc(n+' を 10で わると？',n/10,[n,n/100,n*10],n+'÷10='+(n/10)+'。10分の1に なる。');}],
    [()=>{let a=this.rand(10000,90000),b=this.rand(10000,90000);while(a===b)b=this.rand(10000,90000);return cmp(a,b);}],
    [()=>mc('1000万を 10こ あつめた 数は？',100000000,[10000000,1000000,1000000000],'1000万が 10こで 1億。')]
  ];return this.pickStage(stage,buckets,0);}
  pickShape(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'shape');const S={maru:'width:120px;height:120px;border-radius:50%;background:#f2a03d;border:4px solid #d18426;',shikaku:'width:110px;height:110px;background:#4f9dde;border:4px solid #3a7db8;',chouhoukei:'width:170px;height:95px;background:#4f9dde;border:4px solid #3a7db8;',sankaku:'width:0;height:0;border-left:70px solid transparent;border-right:70px solid transparent;border-bottom:115px solid #52b788;',seisankaku:'width:0;height:0;border-left:65px solid transparent;border-right:65px solid transparent;border-bottom:113px solid #52b788;',nitohen:'width:0;height:0;border-left:45px solid transparent;border-right:45px solid transparent;border-bottom:125px solid #b788d4;',chokkaku:'width:0;height:0;border-bottom:110px solid #e0708a;border-right:110px solid transparent;'};const sq=(pr,ans,pool,ex,style)=>({topic:'shape',mode:'choices',isShape:!!style,shapeStyle:style||'',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex});const Q=[];
    if(stage<=1){
    Q.push(()=>sq('この かたちの なまえは？','まる',['さんかく','しかく','ながしかく'],'まるい かたちは「まる」。',S.maru));
    Q.push(()=>sq('この かたちの なまえは？','さんかく',['まる','しかく','ながしかく'],'かどが 3つ ある かたちは「さんかく」。',S.sankaku));
    Q.push(()=>sq('この かたちの なまえは？','しかく',['まる','さんかく','ほし'],'かどが 4つ ある かたちは「しかく」。',S.shikaku));
    Q.push(()=>sq('さんかくの かどは いくつ？','3つ',['4つ','2つ','5つ'],'さんかくには かどが 3つ あるよ。',S.sankaku));
    }
    if(g<=1&&stage>=2){
    Q.push(()=>sq('ボールの ような かたちは？','きゅう',['はこのかたち','つつのかたち','しかく'],'どこから 見ても まるい かたちは「きゅう」。','width:120px;height:120px;border-radius:50%;background:radial-gradient(circle at 35% 30%,#fff5b8,#f2a03d 55%,#b86a12);border:4px solid #a96518;'));
    Q.push(()=>sq('ティッシュの はこの ような かたちは？','はこのかたち',['きゅう','つつのかたち','まる'],'しかくい 面で かこまれた かたちは「はこのかたち」。','width:150px;height:95px;background:linear-gradient(135deg,#8fc5ef,#4f9dde);border:5px solid #356f9b;box-shadow:18px -14px 0 #c8e3f6;'));
    Q.push(()=>sq('かんづめの ような かたちは？','つつのかたち',['きゅう','はこのかたち','さんかく'],'まるい 面が 上と下に あるのが「つつのかたち」。','width:105px;height:125px;border-radius:50% / 14%;background:linear-gradient(90deg,#d9f0e5,#58b887,#d9f0e5);border:5px solid #34845f;'));
    }
    if(g<=1&&stage>=3){
    Q.push(()=>sq('ころがしても、どこから見ても まるい かたちは？','きゅう',['はこのかたち','しかく','さんかく'],'きゅうは どの むきにも ころがる。','width:120px;height:120px;border-radius:50%;background:radial-gradient(circle at 35% 30%,#fff5b8,#f2a03d 55%,#b86a12);border:4px solid #a96518;'));
    Q.push(()=>sq('つみかさねやすい かたちは？','はこのかたち',['きゅう','まる','さんかく'],'はこのかたちは たいらな 面が あるので つみやすい。','width:150px;height:95px;background:#7ab6e5;border:5px solid #356f9b;'));
    }
    if(g<=1&&stage>=4){
    Q.push(()=>sq('おなじ さんかくを 2まい あわせて つくれる かたちは？','しかく',['まる','きゅう','つつ'],'さんかくを むきあわせると しかくを つくれる。'));
    Q.push(()=>sq('はこの たいらな ところを なんという？','めん',['へん','かど','まる'],'はこの たいらな ところが「めん」。'));
    }
    if(g>=2&&stage>=2){
    Q.push(()=>sq('この かたちの なまえは？','正方形',['三角形','直角三角形','円'],'4つの へんの 長さが みんな 同じで、4つの かどが 直角の 四角形は 正方形。',S.shikaku));
    Q.push(()=>sq('この かたちの なまえは？','長方形',['正方形','直角三角形','円'],'かどが みんな 直角で、むかいあう へんの 長さが 同じ 四角形は 長方形。',S.chouhoukei));
    Q.push(()=>sq('この かたちの なまえは？','直角三角形',['正三角形','長方形','円'],'直角の かどが ある 三角形は 直角三角形。',S.chokkaku));
    Q.push(()=>sq('三角形の へんの 数は？','3',['4','2','6'],'三角形は 3本の 直線で かこまれた 形。'));
    Q.push(()=>sq('四角形の ちょう点の 数は？','4',['3','2','6'],'四角形には ちょう点が 4つ。'));
    Q.push(()=>sq('はこの形の 面の 数は？','6',['4','8','12'],'はこの形には 面が 6つ。'));
    Q.push(()=>sq('はこの形の ちょう点の 数は？','8',['6','4','12'],'はこの形には ちょう点が 8つ。'));
    Q.push(()=>sq('はこの形の へんの 数は？','12',['6','8','10'],'はこの形には へんが 12本。'));
    }
    if(g>=3&&stage>=4){
    Q.push(()=>sq('この 三角形の なまえは？','正三角形',['直角三角形','長方形','円'],'3つの へんが みんな 同じ 長さの 三角形は 正三角形。',S.seisankaku));
    Q.push(()=>sq('この 三角形の なまえは？','二等辺三角形',['正三角形','直角三角形','正方形'],'2つの へんの 長さが 同じ 三角形は 二等辺三角形。',S.nitohen));
    Q.push(()=>{const r=this.rand(2,9);return sq('半径 '+r+'cm の 円の 直径は？',(r*2)+'cm',[r+'cm',(r*4)+'cm',(r*2+2)+'cm',(r+2)+'cm'],'直径は 半径の 2ばい。'+r+'×2='+(r*2)+'cm。',S.maru);});
    Q.push(()=>{const d=this.rand(2,8)*2;return sq('直径 '+d+'cm の 円の 半径は？',(d/2)+'cm',[d+'cm',(d/2+1)+'cm',(d*2)+'cm'],'半径は 直径の 半分。'+d+'÷2='+(d/2)+'cm。',S.maru);});
    Q.push(()=>sq('どこから 見ても 円に 見える 形は？','球',['円','正方形','はこの形'],'ボールのような 形は 球。'));
    Q.push(()=>sq('コンパスで 円を かくとき、はりを おく 点は？','中心',['半径','直径','円周'],'コンパスの はりを おく 点が 円の 中心。',S.maru));
    Q.push(()=>sq('コンパスの ひらきは 円の 何を あらわす？','半径',['直径','円周','角'],'中心から 円までの コンパスの ひらきが 半径。',S.maru));
    Q.push(()=>sq('1つの ちょう点から 出た 2本の 直線が つくる 形を なんと いう？','角',['円','へん','ちょう点'],'2本の 直線の 間に できる 形が 角。'));
    Q.push(()=>sq('紙を きちんと 2回 おって できる かどを なんと いう？','直角',['角','半円','正三角形'],'きちんと 2回 おって できる かどが 直角。三角じょうぎにも あるよ。'));
    }
    return Q[this.rand(0,Q.length-1)]();}
  pickDiv(p){const stage=this.topicStage(p,'div'),exact=(d,q0)=>{const n=d*q0;return{topic:'div',mode:'choices',op:'div',d:d,q0:q0,n:n,prompt:n+' ÷ '+d,answer:''+q0,choices:this.pick4(''+q0,[q0+1,Math.max(1,q0-1),q0+2,q0+3,d,d+1].map(String)),explanation:d+'×'+q0+'='+n+' だから '+n+'÷'+d+'='+q0+'。'};},share=()=>{const people=this.rand(2,5),each=this.rand(2,6),n=people*each;return{topic:'div',mode:'choices',op:'div',d:people,q0:each,n:n,prompt:n+'この あめを '+people+'人に おなじかずずつ わけると、1人ぶんは？',answer:''+each,choices:this.pick4(''+each,[each+1,Math.max(1,each-1),people,n].map(String)),explanation:'ぜんぶの数÷人数。'+n+'÷'+people+'='+each+'。1人ぶんを もとめる わりざん。'};},group=()=>{const groups=this.rand(2,5),each=this.rand(2,6),n=groups*each;return{topic:'div',mode:'choices',op:'div',d:groups,q0:each,n:n,prompt:n+'この あめを '+each+'こずつ ふくろに いれると、何ふくろ？',answer:''+groups,choices:this.pick4(''+groups,[groups+1,Math.max(1,groups-1),each,n].map(String)),explanation:'ぜんぶの数÷1ふくろの数。'+n+'÷'+each+'='+groups+'。いくつ分かを もとめる わりざん。'};};const buckets=[
    [share,group],
    [()=>exact([2,3,4][this.rand(0,2)],this.rand(1,5)),()=>{const people=this.rand(2,5),each=this.rand(2,6),n=people*each,split=Math.random()<.5,ans=split?'1人ぶんの こすう':'わけられる 人数';return{topic:'div',mode:'choices',prompt:(split?(n+'この あめを '+people+'人に おなじかずずつ わけます。'):(n+'この あめを 1人に '+each+'こずつ わけます。'))+'もとめるのは どれ？',answer:ans,choices:this.pick4(ans,['1人ぶんの こすう','わけられる 人数','ぜんぶの こすう','のこりの こすう'].filter(x=>x!==ans)),explanation:split?('人数で わけるから '+n+'÷'+people+'。1人ぶんの こすうが わかるよ。'):('1人ぶんの こすうで わけるから '+n+'÷'+each+'。なん人に わけられるかが わかるよ。')};}],
    [()=>exact(this.rand(2,9),this.rand(1,9)),share,group],
    [()=>{const base=this.rand(2,9),times=this.rand(2,6),total=base*times;return{topic:'div',mode:'choices',prompt:total+'は '+base+'の 何ばい？',answer:''+times,choices:this.pick4(''+times,[times+1,Math.max(1,times-1),base,total].map(String)),explanation:total+'÷'+base+'='+times+'だから '+times+'ばい。'};},()=>{const used=this.rand(2,6),people=this.rand(2,5),each=this.rand(2,6),total=used+people*each,ans=each;return{topic:'div',mode:'choices',prompt:total+'この あめから '+used+'こ つかい、のこりを '+people+'人で わけると 1人ぶんは？',answer:''+ans,choices:this.pick4(''+ans,[ans+1,Math.max(1,ans-1),people,total-used].map(String)),explanation:total+'−'+used+'='+(total-used)+'、'+(total-used)+'÷'+people+'='+ans+'。'};}],
    [()=>{const d=this.rand(2,9),q0=this.rand(3,19),r=this.rand(1,d-1),n=d*q0+r,ans=q0+' あまり '+r;return{topic:'div',mode:'choices',op:'div',d:d,q0:q0,n:n,prompt:n+' ÷ '+d,answer:ans,choices:this.pick4(ans,[q0+' あまり '+(r===1?2:r-1),(q0+1)+' あまり '+r,(q0-1)+' あまり '+r]),explanation:d+'×'+q0+'='+(d*q0)+'。'+n+'−'+(d*q0)+'='+r+' だから '+ans+'。'};},()=>{const d=this.rand(2,6),q0=this.rand(15,29);return exact(d,q0);}]
  ];return this.pickStage(stage,buckets,0);}
  pickFrac(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'frac'),unit=()=>{const d=[2,4][this.rand(0,1)];return{topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:1,prompt:'いろの ついた ところは もとの 大きさの どれだけ？',answer:'1/'+d,choices:this.pick4('1/'+d,['1/'+(d===2?4:2),'1/3',d+'/1']),explanation:'同じ 大きさに '+d+'つに 分けた 1つ分は 1/'+d+'。'};},visual=()=>{const d=this.rand(3,8),k=this.rand(1,d-1);return{topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:k,prompt:'いろの ついた ところは ぜんたいの どれだけ？',answer:k+'/'+d,choices:this.pick4(k+'/'+d,[(d-k)+'/'+d,k+'/'+(d+1),d+'/'+d,(k+1)+'/'+d]),explanation:'ぜんたいを '+d+'つに 分けた '+k+'つ分で '+k+'/'+d+'。'};};if(g<=2){
    const denominators=stage===1?[2]:stage===2?[2,4]:stage===3?[3,4]:[2,3,4,8],d=denominators[this.rand(0,denominators.length-1)],answer='1/'+d;
    const q={topic:'frac',mode:'choices',isFracViz:true,fd:d,fn:1,answer:answer,choices:this.pick4(answer,[2,3,4,8].filter(n=>n!==d).map(n=>'1/'+n)),prompt:'1まいの 紙を 同じ 大きさに 分けました。色の ついた 1つ分は、もとの 紙の どれだけ？',explanation:'もとの 紙を 同じ 大きさに '+d+'つに 分けた 1つ分なので、1/'+d+'。'};
    if(stage===4){q.prompt='図の 紙を 同じ 大きさに 分けています。色の ついた 1つ分を「1/'+d+'」と いうのは なぜ？';q.answer='同じ 大きさの '+d+'つに 分けた 1つ分だから';q.choices=this.pick4(q.answer,['色の ついた ところが '+d+'つ あるから','同じ 大きさに 分けなくても よいから','もとの 紙が '+d+'まい あるから']);}
    if(stage===5){q.prompt='図の 色の ついた 1つ分と 同じ 大きさの 紙を すきまなく ならべて、もとの 大きさに もどします。いくつ 必要？';q.answer=String(d);q.choices=this.pick4(q.answer,[d-1,d+1,d+2].map(String));q.explanation='もとの 紙を 同じ 大きさに '+d+'つに 分けたので、1/'+d+' の 紙を '+d+'つ ならべると もとに もどるよ。';}
    return q;
  }const buckets=[
    [unit],
    [visual],
    [()=>({topic:'frac',mode:'choices',prompt:'1を 10こに 分けた 1こ分を 小数で あらわすと？',answer:'0.1',choices:this.pick4('0.1',['0.01','1.0','10']),explanation:'1の 1/10 は 0.1。'}),()=>{const k=this.rand(2,9);return{topic:'frac',mode:'choices',prompt:'0.1を '+k+'こ あつめた 数は？',answer:'0.'+k,choices:this.pick4('0.'+k,[''+k,'0.'+(k===9?8:k+1),k+'.0']),explanation:'0.1が '+k+'こで 0.'+k+'。'};}],
    [()=>{const d=this.rand(4,9),a=this.rand(1,d-2),b=this.rand(1,d-1-a);return{topic:'frac',mode:'choices',prompt:a+'/'+d+' + '+b+'/'+d+' は？',answer:(a+b)+'/'+d,choices:this.pick4((a+b)+'/'+d,[(a+b)+'/'+(d*2),Math.max(1,a+b-1)+'/'+d,(a+b+1)+'/'+d]),explanation:'1/'+d+' が '+(a+b)+'こ分で '+(a+b)+'/'+d+'。'};}],
    [()=>{const d=this.rand(5,9),a=this.rand(3,d-1);let b=this.rand(1,a-1);if(a===2*b)b-=1;return{topic:'frac',mode:'choices',prompt:a+'/'+d+' − '+b+'/'+d+' は？',answer:(a-b)+'/'+d,choices:this.pick4((a-b)+'/'+d,[(a+b)+'/'+d,Math.max(1,a-b-1)+'/'+d,(a-b)+'/'+(d+1),(a-b+1)+'/'+d]),explanation:'1/'+d+' が '+(a-b)+'こ分 のこるので '+(a-b)+'/'+d+'。'};},()=>{const a=this.rand(1,7),b=this.rand(1,9-a);return{topic:'frac',mode:'choices',prompt:'0.'+a+' + 0.'+b+' は？',answer:'0.'+(a+b),choices:this.pick4('0.'+(a+b),['0.'+Math.max(1,a+b-1),(a+b)+'','0.'+Math.min(9,a+b+1),'1.0']),explanation:'0.1が '+(a+b)+'こ分で 0.'+(a+b)+'。'};},()=>{const a=this.rand(5,9);let b=this.rand(1,a-1);if(a===2*b)b-=1;return{topic:'frac',mode:'choices',prompt:'0.'+a+' − 0.'+b+' は？',answer:'0.'+(a-b),choices:this.pick4('0.'+(a-b),['0.'+(a-b+1),(a-b)+'','0.'+Math.min(9,a-b+2),'0.'+Math.max(1,a-b-1)]),explanation:'0.1が '+(a-b)+'こ分で 0.'+(a-b)+'。'};}]
  ];return this.pickStage(stage,buckets,0);}
  pickChart(p){
    const g=this.effectiveGrade(p),stage=this.topicStage(p,'chart');
    const items=this.shuffle([['りんご','#e05a4e','#b8443a'],['みかん','#f2a03d','#d18426'],['ばなな','#d4c22f','#b0a020'],['ぶどう','#9a4fd6','#7a3aad']]);
    const counts=this.shuffle([2,3,4,5,6,7,8,9]).slice(0,4),scale=g>=3&&stage>=4?5:g>=3&&stage>=3?2:1,unit=scale>1?'人':'こ';
    const rows=items.map((it,i)=>({label:it[0],color:it[1],border:it[2],count:counts[i]})),maxI=counts.indexOf(Math.max(...counts)),minI=counts.indexOf(Math.min(...counts));
    const mission=stage>=3?'活動：身の回りのものを4つの種類に分け、正の字で数えて表やグラフにしよう。':'';
    const chart=(prompt,answer,pool,explanation,extra={})=>({topic:'chart',mode:'choices',isChart:true,rows:rows,chartScale:scale,chartUnit:unit,prompt:'1ますは '+scale+unit+'。'+prompt,answer:String(answer),choices:this.pick4(String(answer),pool.map(String)),explanation:explanation,...extra});
    if(stage>=4&&Math.random()<.25)return{topic:'chart',mode:'choices',prompt:'「すきな遊び」の記録を グラフにする前に、まず することは？',answer:'同じ なかまごとに 分けて 数える',choices:this.pick4('同じ なかまごとに 分けて 数える',['色を でたらめに ぬる','いちばん多いものだけ 書く','数を かえずに ふやす']),explanation:'何について調べるかを決め、同じなかまに分類して数えると、表やグラフにできる。',activityPrompt:mission};
    if(g>=2&&stage<=2&&Math.random()<.5){
      const hidden=this.rand(0,3),answer=counts[hidden]*scale,tableRows=items.map((it,i)=>({label:it[0],value:i===hidden?'□':counts[i]*scale+unit}));
      return chart('グラフを みて、表の □ に はいる 数は？',answer,[answer+scale,Math.max(scale,answer-scale),answer+2*scale],'グラフの ますを 数えると '+items[hidden][0]+'は '+answer+unit+'。表の □ は '+answer+'。',{isTable:true,tableRows:tableRows,activityPrompt:mission});
    }
    if(stage>=5){
      const diff=(counts[maxI]-counts[minI])*scale;
      return chart(items[maxI][0]+' は '+items[minI][0]+' より いくつ 多い？',diff,[diff+scale,Math.max(scale,diff-scale),diff+2*scale],counts[maxI]*scale+'−'+counts[minI]*scale+'='+diff+unit+'。',{activityPrompt:mission});
    }
    const kind=this.rand(0,2);
    if(kind<2){const index=kind===0?maxI:minI;return chart('いちばん '+(kind===0?'多い':'少ない')+'のは どれ？',items[index][0],items.map(it=>it[0]),items[index][0]+' が '+counts[index]*scale+unit+' で いちばん '+(kind===0?'多い':'少ない')+'。');}
    if(stage>=3&&Math.random()<.2){
      const total=counts.reduce((a,b)=>a+b,0)*scale;
      return chart('表の 数を ぜんぶ あわせると？',total,[total+scale,Math.max(scale,total-scale),total-2*scale],counts.map((c,i)=>items[i][0]+' '+c*scale).join('＋')+'='+total+unit+'。',{isTable:true,tableRows:items.map((it,i)=>({label:it[0],value:counts[i]*scale+unit})),activityPrompt:mission,subtype:'chart-total'});
    }
    const t=this.rand(0,3),answer=counts[t]*scale;
    return chart(items[t][0]+' は いくつ？',answer,[answer+scale,Math.max(1,answer-scale),answer+2*scale],'ますを 数えると '+counts[t]+'こ。1ます '+scale+unit+' だから '+answer+unit+'。');
  }
  pickStory(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'story');const items=[['りんご','こ'],['えんぴつ','本'],['シール','まい'],['おはじき','こ'],['どんぐり','こ'],['カード','まい'],['ノート','さつ'],['あめ','こ'],['花','本'],['ボール','こ'],['クッキー','まい'],['色紙','まい']];const it=items[this.rand(0,items.length-1)];const Q=[];
    Q.push(()=>({topic:'story',mode:'num',prompt:'大人が 7人、子供が 9人 います。あわせて 何人 いますか？',answer:'16',explanation:'大人 7人と 子供 9人を あわせるので、7+9=16。16人 います。'}));
    Q.push(()=>({topic:'story',mode:'choices',prompt:'切手が 6枚、封筒が 15枚 あります。どちらが 何枚 おおいですか？',answer:'封筒が 9枚 おおい',choices:this.pick4('封筒が 9枚 おおい',['切手が 9枚 おおい','封筒が 21枚 おおい','切手が 21枚 おおい']),explanation:'封筒の 15枚から 切手の 6枚を ひくと、15−6=9。封筒が 9枚 おおいです。'}));
    Q.push(()=>{const a=this.rand(3,9),b=this.rand(2,9);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' もらうと ぜんぶで なん'+it[1]+'？',answer:''+(a+b),explanation:'あわせる ときは たしざん。'+a+'+'+b+'='+(a+b)+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' つかうと のこりは なん'+it[1]+'？',answer:''+(a-b),explanation:'のこりを もとめる ときは ひきざん。'+a+'−'+b+'='+(a-b)+'。'};});
    Q.push(()=>{const a=this.rand(3,9);let b=this.rand(2,9);while(b===a)b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' ふえた。あう しきは？',answer:a+'＋'+b,choices:this.pick4(a+'＋'+b,[a+'−'+b,b+'−'+a,a+'×'+b]),explanation:'ふえる ときは たしざん。しきは '+a+'＋'+b+'。'};});
    Q.push(()=>{const a=this.rand(5,12),b=this.rand(2,a-1);return{topic:'story',mode:'choices',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' あげた。あう しきは？',answer:a+'−'+b,choices:this.pick4(a+'−'+b,[a+'＋'+b,b+'−'+a,a+'×'+b]),explanation:'へる ときは ひきざん。しきは '+a+'−'+b+'。'};});
    if(g>=2&&stage>=2){
    Q.push(()=>{const known=this.rand(8,24),hidden=this.rand(3,16),total=known+hidden;return{topic:'story',mode:'choices',isTape:true,tapeParts:[known+'','□'],prompt:'テープ図の ぜんぶが '+total+'。□は いくつ？',answer:''+hidden,choices:this.pick4(''+hidden,[hidden+1,Math.max(1,hidden-1),known,total].map(String)),explanation:total+'−'+known+'='+hidden+'。'};});
    Q.push(()=>{const total=this.rand(18,40),used=this.rand(3,total-5),left=total-used;return{topic:'story',mode:'choices',isTape:true,tapeParts:[used+'',left+''],prompt:'ぜんぶで '+total+'。はじめの ぶぶんが '+used+'なら、のこりは？',answer:''+left,choices:this.pick4(''+left,[left+1,Math.max(1,left-1),used,total].map(String)),explanation:total+'−'+used+'='+left+'。'};});
    // No.3/42: 二けたと 十の まとまりの ぶんしょうもんだい。
    Q.push(()=>{const a=this.rand(21,69),b=this.rand(1,7)*10;return{topic:'story',mode:'num',subtype:'story-tens',prompt:it[0]+'が '+a+it[1]+'。あと '+b+it[1]+' かいました。ぜんぶで なん'+it[1]+'？',answer:''+(a+b),explanation:'あわせる ときは たしざん。'+a+'+'+b+'='+(a+b)+'。'};});
    Q.push(()=>{const a=this.rand(41,99),b=this.rand(1,Math.floor((a-1)/10))*10;return{topic:'story',mode:'num',subtype:'story-tens',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' つかいました。のこりは なん'+it[1]+'？',answer:''+(a-b),explanation:'のこりを もとめる ときは ひきざん。'+a+'−'+b+'='+(a-b)+'。'};});
    // No.43: 二けたどうしを あわせる ぶんしょうもんだい。
    Q.push(()=>{const a=this.rand(11,49),b=this.rand(11,49);return{topic:'story',mode:'num',subtype:'story-combine',prompt:'1くみが '+a+'人、2くみが '+b+'人。あわせて 何人？',answer:''+(a+b),explanation:'あわせる ときは たしざん。'+a+'+'+b+'='+(a+b)+'。'};});
    // No.42: まとめて ふえる ぶんしょうもんだい（バスに 10人、つぎに 4人と 2人）。
    Q.push(()=>{const a=this.rand(8,30),b=this.rand(2,9),c=this.rand(2,9);return{topic:'story',mode:'num',subtype:'story-group',prompt:'バスに '+a+'人 のっていました。つぎの バスていで '+b+'人と '+c+'人が のりました。ぜんぶで 何人？',answer:''+(a+b+c),explanation:'ふえた ぶんを まとめて たす。'+a+'+'+b+'+'+c+'='+(a+b+c)+'。'};});
    }
    if(g>=2&&stage>=3){
    // No.22-25: はじめの かずを さかのぼって もとめる 逆思考（テープ図）。
    Q.push(()=>{const used=this.rand(4,18),left=this.rand(6,30),total=used+left;return{topic:'story',mode:'choices',isTape:true,tapeParts:[used+'',left+''],subtype:'story-reverse',prompt:'テープ図の ぜんぶが □。'+used+it[1]+' つかうと のこりが '+left+it[1]+'。ぜんぶは なん'+it[1]+'？',answer:''+total,choices:this.pick4(''+total,[total+1,Math.max(1,total-1),total+10,Math.max(1,left-used)].map(String)),explanation:'つかった 分と のこりを あわせると ぜんぶ。'+used+'+'+left+'='+total+'。'};});
    }
    if(g>=2&&stage>=3&&this.topicComplete(p,'mul'))Q.push(()=>{const a=this.rand(2,9),b=this.rand(2,9);return{topic:'story',mode:'choices',prompt:'1さらに '+it[0]+'が '+a+it[1]+'ずつ、'+b+'さら分。あう しきは？',answer:a+'×'+b,choices:this.pick4(a+'×'+b,[a+'＋'+b,a+'−'+b,a+'÷'+b]),explanation:'同じ数ずつ あるときは かけざん。しきは '+a+'×'+b+'。'};});
    if(g>=3&&stage>=4){
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ + '+b+' = '+(ans+b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans+b)+' から '+b+' を ひくと '+ans+'。'};});
    Q.push(()=>{const b=this.rand(2,9),ans=this.rand(2,9);return{topic:'story',mode:'num',prompt:'□ × '+b+' = '+(ans*b)+'　□に あてはまる 数は？',answer:''+ans,explanation:(ans*b)+' ÷ '+b+' = '+ans+'。'};});
    if(this.topicComplete(p,'div'))Q.push(()=>{const d=this.rand(2,9),q0=this.rand(2,9);return{topic:'story',mode:'num',prompt:(d*q0)+it[1]+'の '+it[0]+'を '+d+'人で 同じ数ずつ 分けると 1人分は なん'+it[1]+'？',answer:''+q0,explanation:'分ける ときは わりざん。'+(d*q0)+'÷'+d+'='+q0+'。'};});
    }
    if(stage>=5)Q.push(()=>{const a=this.rand(8,18),b=this.rand(3,9),c=this.rand(2,Math.min(8,a+b-1)),ans=a+b-c;return{topic:'story',mode:'num',prompt:it[0]+'が '+a+it[1]+'。'+b+it[1]+' もらって、'+c+it[1]+' つかった。のこりは？',answer:''+ans,explanation:a+'+'+b+'='+(a+b)+'、'+(a+b)+'−'+c+'='+ans+'。'};});
    return Q[this.rand(0,Q.length-1)]();}
""" + BuildSupplementalMathScript();
    }

}
