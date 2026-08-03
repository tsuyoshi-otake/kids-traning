namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildGenAddScript()
    {
        return """
genAdd(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'add'),make=(a,b,extra)=>{const ans=a+b;return{topic:'add',mode:'num',n1:a,n2:b,prompt:extra?extra.prompt:a+' + '+b,answer:''+(extra?extra.answer:ans),explanation:extra?extra.explanation:a+' + '+b+' = '+ans};},makeHissan=(presetA,presetB)=>{let a=presetA,b=presetB;if(!Number.isInteger(a)||!Number.isInteger(b)){do{a=this.rand(11,79);b=this.rand(11,Math.min(88,99-a));}while((a%10+b%10)<10);}const q=this.hissanAdd(a,b);q.topic='add';q.difficulty=5;return q;};const buckets=[
    [()=>{let a=this.rand(1,9),b=this.rand(1,9);if(a+b>10)b=Math.max(1,10-a);return make(a,b);},()=>make(3,7,{prompt:'3 + 7',answer:10,explanation:'3 + 7 = 10。'}),()=>make(7,3,{prompt:'7 + 3',answer:10,explanation:'7 + 3 = 10。'})],
    [()=>{let a=this.rand(2,9),b=this.rand(1,9);if(a+b>18)b=Math.max(1,18-a);return make(a,b);},()=>{const a=this.rand(1,9),b=this.rand(1,Math.max(1,10-a)),total=a+b;return{topic:'add',mode:'num',subtype:'missing-add',prompt:'□ + '+b+' = '+total,answer:''+a,explanation:total+' から '+b+' を ひくと '+a+'。'};}],
    [()=>{let a=this.rand(10,89);if(a%10===9)a-=1;const b=this.rand(1,9-a%10);return make(a,b);},()=>{const a=this.rand(1,8)*10,b=this.rand(1,9-a/10)*10,ans=a+b;return make(a,b,{prompt:a+' + '+b,answer:ans,explanation:'10のまとまりで かんがえる。'+a+' + '+b+' = '+ans+'。'});},()=>{const a=this.rand(10,50),b=this.rand(1,9),total=a+b;return{topic:'add',mode:'num',subtype:'missing-add',prompt:a+' + □ = '+total,answer:''+b,explanation:total+' から '+a+' を ひくと '+b+'。'};},()=>make(10,2,{prompt:'10 + 2',answer:12,explanation:'10 + 2 = 12。'}),()=>make(10,5,{prompt:'10 + 5',answer:15,explanation:'10 + 5 = 15。'})],
    [()=>{let a=this.rand(18,89);if(a%10===0)a+=1;const b=this.rand(Math.max(2,10-a%10),9);return make(a,b);},()=>{const x=this.rand(1,9),y=this.rand(1,9),z=this.rand(1,9),s=x+y+z;return make(x+y,z,{prompt:x+' + '+y+' + '+z,answer:s,explanation:'まえから じゅんに。'+x+'+'+y+'='+(x+y)+'、'+(x+y)+'+'+z+'='+s+'。'});} ],
    g<=1?[()=>{const x=this.rand(3,9),y=this.rand(3,9),z=this.rand(2,9),s=x+y+z;return make(x+y,z,{prompt:x+' + '+y+' + '+z,answer:s,explanation:'まえから じゅんに。'+x+'+'+y+'='+(x+y)+'、'+(x+y)+'+'+z+'='+s+'。'});},()=>{const a=this.rand(0,20);return make(a,0,{prompt:a+' + 0',answer:a,explanation:'0を たしても 数は '+a+'の まま。'});}]:[()=>makeHissan(),()=>makeHissan(58,29),()=>makeHissan(68,22),()=>makeHissan(35,25),()=>makeHissan(19,43)]
  ];return this.pickStage(stage,buckets,0);}
""";
    }

    private static string BuildGenSubScript()
    {
        return """
genSub(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'sub'),make=(a,b,extra)=>{const ans=a-b;return{topic:'sub',mode:'num',a:a,b:b,prompt:extra?extra.prompt:a+' - '+b,answer:''+(extra?extra.answer:ans),explanation:extra?extra.explanation:a+' - '+b+' = '+ans};},mixed=()=>({topic:'sub',mode:'num',prompt:'16 - 6 + 7',answer:'17',explanation:'まえから じゅんに。16−6=10、10+7=17。'}),makeHissan=()=>{let a,b;do{a=this.rand(22,99);b=this.rand(11,Math.min(88,a-1));}while(a%10>=b%10||Math.floor(a/10)<=Math.floor(b/10));const q=this.hissanSub(a,b);q.topic='sub';q.difficulty=5;return q;};
  const basic=()=>{const a=this.rand(2,10),b=this.rand(1,a-1);return make(a,b);};
  const zeroReview=()=>{const a=this.rand(1,10);return make(a,0,{prompt:a+' - 0',answer:a,explanation:'0を ひいても 数は '+a+'の まま。'});};
  const noBorrowWithinTwenty=()=>{const a=this.rand(11,18),b=this.rand(1,a%10);return make(a,b);};
  const borrowWithinTwenty=()=>{const a=this.rand(11,18),b=this.rand(a%10+1,9);return make(a,b);};
  const missingBorrow=()=>{const a=this.rand(11,18),b=this.rand(a%10+1,9),answer=a-b;return{topic:'sub',mode:'num',subtype:'missing-sub',prompt:a+' - □ = '+answer,answer:''+b,explanation:a+' から '+answer+' になるには '+b+' を ひく。'};};
  const threeTerm=()=>{const x=this.rand(11,18),y=this.rand(x%10+1,9),z=this.rand(1,Math.min(9,x-y-1)),s=x-y-z;return make(x,y+z,{prompt:x+' - '+y+' - '+z,answer:s,explanation:'まえから じゅんに。'+x+'−'+y+'='+(x-y)+'、'+(x-y)+'−'+z+'='+s+'。'});};
  const gradeOneBuckets=[
    [()=>Math.random()<.08?zeroReview():basic()],
    [noBorrowWithinTwenty],
    [borrowWithinTwenty],
    [borrowWithinTwenty,missingBorrow,()=>({topic:'sub',mode:'num',prompt:'9 - 3 - 2',answer:'4',explanation:'まえから じゅんに。9−3=6、6−2=4。'})],
    [threeTerm,missingBorrow,mixed]
  ];
  const upperGradeBuckets=[
    [basic],
    [()=>{const a=this.rand(11,18),b=this.rand(1,Math.max(1,a%10));return make(a,b);},()=>{const a=this.rand(5,18),b=this.rand(1,a-1),answer=a-b;return{topic:'sub',mode:'num',subtype:'missing-sub',prompt:a+' - □ = '+answer,answer:''+b,explanation:a+' から '+answer+' になるには '+b+' を ひく。'};}],
    [()=>{let a=this.rand(21,89);if(a%10===0)a++;const b=this.rand(1,a%10);return make(a,b);},()=>{const a=this.rand(2,9)*10,b=this.rand(1,a/10-1)*10;return make(a,b);},()=>{const b=this.rand(2,9),answer=this.rand(10,40),a=answer+b;return{topic:'sub',mode:'num',subtype:'missing-sub',prompt:'□ - '+b+' = '+answer,answer:''+a,explanation:answer+' に '+b+' を たすと '+a+'。'};}],
    [()=>{const a=this.rand(30,99),b=this.rand(a%10+1,Math.min(19,a-1));return make(a,b);},()=>{const x=this.rand(12,28),y=this.rand(1,8),z=this.rand(1,Math.max(1,x-y-1)),s=x-y-z;return make(x,y+z,{prompt:x+' - '+y+' - '+z,answer:s,explanation:'まえから じゅんに。'+x+'−'+y+'='+(x-y)+'、'+(x-y)+'−'+z+'='+s+'。'});},()=>({topic:'sub',mode:'num',prompt:'9 - 3 - 2',answer:'4',explanation:'まえから じゅんに。9−3=6、6−2=4。'})],
    [makeHissan]
  ];
  return this.pickStage(stage,g<=1?gradeOneBuckets:upperGradeBuckets,0);}
""";
    }

    private static string BuildGenHissanScript()
    {
        return """
genHissan(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'hissan'),tag=(q,d)=>{q.difficulty=d;q.writtenArithmetic=true;return q;},add=(a,b,d)=>tag({topic:'hissan',mode:'num',op:'＋',a:a,b:b,prompt:a+' + '+b,answer:''+(a+b),explanation:'くらいを そろえて、右の くらいから じゅんに たす。'+a+' + '+b+' = '+(a+b)+'。'},d),sub=(a,b,d)=>tag({topic:'hissan',mode:'num',op:'−',a:a,b:b,prompt:a+' - '+b,answer:''+(a-b),explanation:'くらいを そろえて、右の くらいから じゅんに ひく。'+a+' - '+b+' = '+(a-b)+'。'},d),mul=(a,b,d)=>tag({topic:'hissan',mode:'num',op:'mul',a:a,b:b,prompt:a+' × '+b,answer:''+(a*b),explanation:'くらいを そろえて、右の くらいから じゅんに かける。'+a+' × '+b+' = '+(a*b)+'。'},d),choose=items=>items[this.rand(0,items.length-1)](),find=(factory,test,fallback)=>{for(let i=0;i<40;i++){const pair=factory();if(test(pair[0],pair[1]))return pair;}return fallback;},carry=(a,b)=>{let count=0,incoming=0;while(a||b||incoming){const total=a%10+b%10+incoming;if(total>=10)count++;incoming=total>=10?1:0;a=Math.floor(a/10);b=Math.floor(b/10);}return count;},borrow=(a,b)=>{let count=0,loan=0;while(a||b){let digit=a%10-loan;const take=b%10;loan=0;if(digit<take){count++;loan=1;}a=Math.floor(a/10);b=Math.floor(b/10);}return count;};
  const gradeTwo=[
    [()=>{const pair=find(()=>[this.rand(21,87),this.rand(11,Math.min(87,99))],(a,b)=>a+b<=99&&carry(a,b)===0,[42,35]);return add(pair[0],pair[1],1);},()=>{const pair=find(()=>{const a=this.rand(32,98);return[a,this.rand(11,a-1)];},(a,b)=>borrow(a,b)===0,[86,43]);return sub(pair[0],pair[1],1);}],
    [()=>{const pair=find(()=>[this.rand(24,79),this.rand(16,79)],(a,b)=>a+b<=99&&carry(a,b)>=1,[48,27]);return add(pair[0],pair[1],2);}],
    [()=>{const pair=find(()=>{const a=this.rand(31,98);return[a,this.rand(12,a-1)];},(a,b)=>borrow(a,b)>=1,[72,48]);return sub(pair[0],pair[1],3);}],
    [()=>{const pair=find(()=>[this.rand(123,799),this.rand(111,799)],(a,b)=>a+b<=999&&carry(a,b)>=1,[368,457]);return add(pair[0],pair[1],4);},()=>{const pair=find(()=>{const a=this.rand(301,999);return[a,this.rand(111,a-1)];},(a,b)=>borrow(a,b)>=1,[704,286]);return sub(pair[0],pair[1],4);}],
    [()=>{const pair=find(()=>[this.rand(248,799),this.rand(178,699)],(a,b)=>a+b<=999&&carry(a,b)>=2,[468,357]);return add(pair[0],pair[1],5);},()=>{const pair=find(()=>{const a=this.rand(401,999);return[a,this.rand(112,a-1)];},(a,b)=>borrow(a,b)>=2,[802,467]);return sub(pair[0],pair[1],5);},()=>sub(700,286,5)]
  ];
  const gradeThree=[
    [()=>{const pair=find(()=>[this.rand(123,899),this.rand(111,699)],(a,b)=>a+b<=999&&carry(a,b)>=1,[368,457]);return add(pair[0],pair[1],1);},()=>{const a=this.rand(321,999),b=this.rand(111,a-1);return sub(a,b,1);}],
    [()=>mul(this.rand(21,89),this.rand(2,9),2)],
    [()=>mul(this.rand(123,899),this.rand(2,9),3)],
    [()=>mul(this.rand(12,89),this.rand(11,89),4)],
    [()=>mul(this.rand(101,909),this.rand(2,9),5),()=>mul(this.rand(21,89),this.rand(11,49),5),()=>{const pair=find(()=>[this.rand(1234,7899),this.rand(111,1999)],(a,b)=>a+b<=9999&&carry(a,b)>=2,[4687,2358]);return add(pair[0],pair[1],5);},()=>{const a=this.rand(2000,9999),b=this.rand(111,a-1);return sub(a,b,5);},()=>mul(this.rand(20,90),this.rand(10,40),5)]
  ];return choose((g>=3?gradeThree:gradeTwo)[Math.max(0,Math.min(4,stage-1))]);}
""";
    }

    private static string BuildPickMulScript()
    {
        return """
pickMul(p){const stage=this.topicStage(p,'mul'),stat=this.topicStat(p,'mul'),facts=stat.multiplicationFacts||{},keys=this.multiplicationStageFactKeys(stage),score=key=>{const f=facts[key];if(!f)return 0;const strength=Math.max(0,Math.min(2,Number(f.strength)||0)),attempts=Math.max(0,Number(f.attempts)||0),errors=Math.max(0,Number(f.errors)||0),age=(Number(f.lastAttemptAt)||0)/1e13;return strength*1000000+(attempts===0?0:errors>0?-100000:1000)+age;},ranked=keys.slice().sort((a,b)=>score(a)-score(b)),limit=Math.min(6,ranked.length),total=limit*(limit+1)/2,pick=()=>{let ticket=this.rand(1,total);for(let i=0;i<limit;i++){ticket-=limit-i;if(ticket<=0)return ranked[i];}return ranked[0];},recall=key=>{const parts=key.split('x'),a=Number(parts[0]),b=Number(parts[1]),ans=a*b;return{topic:'mul',mode:'num',op:'mul',a:a,b:b,prompt:a+' × '+b,answer:''+ans,multiplicationFactKey:key,memoryAssessment:true,explanation:a+' × '+b+' = '+ans+'。声に出して たしかめよう。'};},progress=this.multiplicationFactProgress(stat,stage);if(stage<5||progress.secure<progress.total||Math.random()<.75)return recall(pick());if(Math.random()<.5){const a=this.rand(2,9),b=this.rand(2,9),ans=a*b,swap=b+' × '+a;return{topic:'mul',mode:'choices',prompt:a+' × '+b+' と こたえが おなじ しきは？',answer:swap,choices:this.pick4(swap,[a+' + '+b,a+' × '+(b+1),b+' + '+a,a+' × '+Math.max(1,b-1)]),explanation:'かける 数と かけられる 数を いれかえても、こたえは '+ans+'。'};}const each=this.rand(2,9),groups=this.rand(2,9),ans=each*groups;return{topic:'mul',mode:'num',op:'mul',a:each,b:groups,prompt:'1さらに りんごが '+each+'こずつ、'+groups+'さら。ぜんぶで？',answer:''+ans,explanation:'同じ数ずつが いくつ分かは かけ算。'+each+' × '+groups+' = '+ans+'。'};}
""";
    }

}
