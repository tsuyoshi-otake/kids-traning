namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildGenAddScript()
    {
        return """
genAdd(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'add'),make=(a,b,extra)=>{const ans=a+b;return{topic:'add',mode:'num',n1:a,n2:b,prompt:extra?extra.prompt:a+' + '+b,answer:''+(extra?extra.answer:ans),explanation:extra?extra.explanation:a+' + '+b+' = '+ans};};const buckets=[
    [()=>{let a=this.rand(1,9),b=this.rand(1,9);if(a+b>10)b=Math.max(1,10-a);return make(a,b);}],
    [()=>{let a=this.rand(2,9),b=this.rand(1,9);if(a+b>18)b=Math.max(1,18-a);return make(a,b);}],
    [()=>{const a=this.rand(10,89),max=Math.max(1,9-a%10),b=this.rand(1,max);return make(a,b);},()=>{const a=this.rand(1,8)*10,b=this.rand(1,9-a/10)*10,ans=a+b;return make(a,b,{prompt:a+' + '+b,answer:ans,explanation:'10のまとまりで かんがえる。'+a+' + '+b+' = '+ans+'。'});}],
    [()=>{const a=this.rand(18,89),b=this.rand(2,9);return make(a,b);},()=>{const x=this.rand(1,9),y=this.rand(1,9),z=this.rand(1,9),s=x+y+z;return make(x+y,z,{prompt:x+' + '+y+' + '+z,answer:s,explanation:'まえから じゅんに。'+x+'+'+y+'='+(x+y)+'、'+(x+y)+'+'+z+'='+s+'。'});} ],
    [()=>{if(g<=1){const x=this.rand(3,9),y=this.rand(3,9),z=this.rand(2,9),s=x+y+z;return make(x+y,z,{prompt:x+' + '+y+' + '+z,answer:s,explanation:'まえから じゅんに。'+x+'+'+y+'='+(x+y)+'、'+(x+y)+'+'+z+'='+s+'。'});}const a=this.rand(28,79),b=this.rand(12,Math.min(99-a,49));return make(a,b);}]
  ];return this.pickStage(stage,buckets,0);}
""";
    }

    private static string BuildGenSubScript()
    {
        return """
genSub(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'sub'),make=(a,b,extra)=>{const ans=a-b;return{topic:'sub',mode:'num',a:a,b:b,prompt:extra?extra.prompt:a+' - '+b,answer:''+(extra?extra.answer:ans),explanation:extra?extra.explanation:a+' - '+b+' = '+ans};};const buckets=[
    [()=>{const a=this.rand(2,10),b=this.rand(1,a-1);return make(a,b);}],
    [()=>{const a=this.rand(11,18),b=this.rand(1,Math.max(1,a%10));return make(a,b);}],
    [()=>{let a=this.rand(21,89);if(a%10===0)a++;const b=this.rand(1,a%10);return make(a,b);},()=>{const a=this.rand(2,9)*10,b=this.rand(1,a/10-1)*10;return make(a,b);}],
    [()=>{const a=this.rand(30,99),b=this.rand(a%10+1,Math.min(19,a-1));return make(a,b);},()=>{const x=this.rand(12,28),y=this.rand(1,8),z=this.rand(1,Math.max(1,x-y-1)),s=x-y-z;return make(x,y+z,{prompt:x+' - '+y+' - '+z,answer:s,explanation:'まえから じゅんに。'+x+'−'+y+'='+(x-y)+'、'+(x-y)+'−'+z+'='+s+'。'});} ],
    [()=>{if(g<=1){const x=this.rand(15,30),y=this.rand(2,8),z=this.rand(2,Math.max(2,x-y-1)),s=x-y-z;return make(x,y+z,{prompt:x+' - '+y+' - '+z,answer:s,explanation:'まえから じゅんに。'+x+'−'+y+'='+(x-y)+'、'+(x-y)+'−'+z+'='+s+'。'});}const a=this.rand(51,99),b=this.rand(12,a-10);return make(a,b);}]
  ];return this.pickStage(stage,buckets,0);}
""";
    }

    private static string BuildGenHissanScript()
    {
        return """
genHissan(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'hissan'),tag=(q,d)=>{q.difficulty=d;return q;};if(stage>=5){if(g>=3&&Math.random()<0.5){const a=this.rand(12,89),b=this.rand(2,9),ans=a*b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' × '+b,answer:''+ans,explanation:a+' × '+b+' = '+ans+'。'};}const a=this.rand(123,868),b=this.rand(111,Math.min(999-a,499)),ans=a+b;return{topic:'hissan',mode:'num',difficulty:5,prompt:a+' + '+b,answer:''+ans,explanation:'くらいごとに たす。'+a+' + '+b+' = '+ans+'。'};}if(stage<=1){let a,b;do{a=this.rand(12,48);b=this.rand(2,9);}while((a%10+b)>=10);return tag(this.hissanAdd(a,b),1);}if(stage===2){let a,b;do{a=this.rand(12,58);b=this.rand(2,9);}while((a%10+b)<10);return tag(this.hissanAdd(a,b),2);}if(stage===3){if(Math.random()<0.5){let a,b;do{a=this.rand(14,48);b=this.rand(12,38);}while((a%10+b%10)<10||a+b>99);return tag(this.hissanAdd(a,b),3);}let a,b;do{a=this.rand(22,79);b=this.rand(11,Math.min(48,a-1));}while(!(a%10<b%10&&Math.floor(a/10)>=Math.floor(b/10)+1));return tag(this.hissanSub(a,b),3);}if(Math.random()<0.5){let a,b;do{a=this.rand(14,68);b=this.rand(14,68);}while((a%10+b%10)<10||a+b>99);return tag(this.hissanAdd(a,b),4);}let a,b;do{a=this.rand(22,99);b=this.rand(13,a-1);}while(!((a%10)<(b%10)&&Math.floor(a/10)>=Math.floor(b/10)+1));return tag(this.hissanSub(a,b),4);}
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
    [fromTables([6,7,8,9])]
  ];return this.pickStage(stage,buckets,0);}
""";
    }

}