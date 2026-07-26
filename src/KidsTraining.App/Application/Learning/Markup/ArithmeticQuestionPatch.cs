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
genHissan(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'hissan'),tag=(q,d)=>{q.difficulty=d;return q;};if(stage>=5){if(g>=3){const kind=this.rand(0,3);if(kind===0){const a=this.rand(12,89),b=this.rand(11,39),ans=a*b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' × '+b,answer:''+ans,explanation:'くらいごとの せきに 分ける。'+a+' × '+b+' = '+ans+'。'};}if(kind===1){const a=this.rand(123,899),b=this.rand(2,9),ans=a*b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' × '+b,answer:''+ans,explanation:'一、十、百の くらいごとに かける。'+a+' × '+b+' = '+ans+'。'};}if(kind===2){const a=this.rand(1234,7899),b=this.rand(111,Math.min(2000,9999-a)),ans=a+b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' + '+b,answer:''+ans,explanation:'一、十、百、千の くらいを そろえて たす。'+a+' + '+b+' = '+ans+'。'};}const a=this.rand(2000,9999),b=this.rand(111,a-1),ans=a-b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' - '+b,answer:''+ans,explanation:'一、十、百、千の くらいを そろえて ひく。'+a+' - '+b+' = '+ans+'。'};}const a=this.rand(123,868),b=this.rand(111,Math.min(999-a,499)),ans=a+b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' + '+b,answer:''+ans,explanation:'くらいごとに たす。'+a+' + '+b+' = '+ans+'。'};}if(stage<=1){let a,b;do{a=this.rand(12,48);b=this.rand(2,9);}while((a%10+b)>=10);return tag(this.hissanAdd(a,b),1);}if(stage===2){let a,b;do{a=this.rand(12,58);b=this.rand(2,9);}while((a%10+b)<10);return tag(this.hissanAdd(a,b),2);}if(stage===3){if(Math.random()<0.5){let a,b;do{a=this.rand(14,48);b=this.rand(12,38);}while((a%10+b%10)<10||a+b>99);return tag(this.hissanAdd(a,b),3);}let a,b;do{a=this.rand(22,79);b=this.rand(11,Math.min(48,a-1));}while(!(a%10<b%10&&Math.floor(a/10)>=Math.floor(b/10)+1));return tag(this.hissanSub(a,b),3);}if(Math.random()<0.5){let a,b;do{a=this.rand(14,68);b=this.rand(14,68);}while((a%10+b%10)<10||a+b>99);return tag(this.hissanAdd(a,b),4);}let a,b;do{a=this.rand(22,99);b=this.rand(13,a-1);}while(!((a%10)<(b%10)&&Math.floor(a/10)>=Math.floor(b/10)+1));return tag(this.hissanSub(a,b),4);}
""";
    }

    private static string BuildPickMulScript()
    {
        return """
pickMul(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'mul'),make=(a,b)=>{const ans=a*b;return{topic:'mul',mode:'choices',op:'mul',a:a,b:b,prompt:a+' × '+b,answer:''+ans,choices:this.pick4(''+ans,[ans+a,ans-a,ans+b,ans-b,a+b,Math.max(1,ans+a+b)].map(String)),explanation:a+' × '+b+' = '+ans+'。'+a+' こずつが '+b+' つ。'};},fromPairs=pairs=>()=>{const pair=pairs[this.rand(0,pairs.length-1)];return make(pair[0],pair[1]);},fromTables=tables=>()=>{const a=tables[this.rand(0,tables.length-1)],b=this.rand(1,9);return make(a,b);};const buckets=[
    [fromPairs([[1,2],[2,1],[2,2],[1,3],[3,1]])],
    [fromPairs([[2,3],[3,2],[2,4],[4,2],[5,2],[2,5]])],
    [fromTables([1,2,3,4,5,10])],
    [fromTables([3,4,5,10])],
    [fromTables([6,7,8,9]),()=>{const a=this.rand(2,9);let b=this.rand(2,9);if(b===a)b=a===9?2:a+1;const ans=a*b,swap=b+' × '+a;return{topic:'mul',mode:'choices',prompt:a+' × '+b+' と こたえが おなじ しきは？',answer:swap,choices:this.pick4(swap,[a+' + '+b,a+' × '+(b+1),b+' + '+a,a+' × '+(b-1)]),explanation:'かける 数と かけられる 数を いれかえても、こたえは '+ans+'。'};},()=>{const each=this.rand(2,9),groups=this.rand(2,9),ans=each*groups;return{topic:'mul',mode:'choices',op:'mul',a:each,b:groups,prompt:'1さらに りんごが '+each+'こずつ、'+groups+'さら。ぜんぶで？',answer:''+ans,choices:this.pick4(''+ans,[ans+each,Math.max(1,ans-each),each+groups,ans+1].map(String)),explanation:'同じ数ずつが いくつ分かは かけ算。'+each+'×'+groups+'='+ans+'。'};}]
  ];return this.pickStage(stage,buckets,0);}
""";
    }

}
