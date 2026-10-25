namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildSupplementalMathScript()
    {
        return """
  pickMoney(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'money'),coin=(values,prompt)=>{const total=values.reduce((a,b)=>a+b,0),near=[total+1,Math.max(1,total-1),total+5,total+10].filter(x=>x!==total).map(x=>x+'円');return{topic:'money',mode:'choices',isMoney:true,moneyPieces:values,prompt:prompt||'おかねは ぜんぶで いくら？',answer:total+'円',choices:this.pick4(total+'円',near),explanation:values.join('円 + ')+'円 = '+total+'円。'};},mc=(pr,ans,pool,ex)=>({topic:'money',mode:'choices',prompt:pr,answer:ans,choices:this.pick4(ans,pool),explanation:ex}),priceSum=(p1,p2)=>{const goods=this.shuffle(['ノート','けしゴム','えんぴつ','ものさし','のり','ふでばこ']),total=p1+p2;return{...mc(goods[0]+'が '+p1+'円、'+goods[1]+'が '+p2+'円。2つ かうと いくら？',total+'円',[(total+10)+'円',(total-10)+'円',(total+100)+'円',Math.abs(p1-p2)+'円'],p1+'円 + '+p2+'円 = '+total+'円。'),subtype:'price-sum'};};let buckets;if(g<=1){buckets=[
    [()=>coin(Array.from({length:this.rand(2,7)},()=>1))],
    [()=>{const tens=this.rand(1,4),ones=this.rand(0,4);return coin(Array(tens).fill(10).concat(Array(ones).fill(1)));}],
    [()=>{const fives=this.rand(1,3),ones=this.rand(1,4);return coin(Array(fives).fill(5).concat(Array(ones).fill(1)));}],
    [()=>{const tens=this.rand(2,8),ones=this.rand(0,9);return coin(Array(tens).fill(10).concat(ones?Array(ones).fill(1):[]));}],
    // おつりが 代金と 同じ（50円）だと 見ただけで えらべてしまうので さける。
    [()=>{let price=this.rand(2,9)*10;if(price===50)price=this.rand(0,1)?40:60;const change=100-price;return mc('100円を もって '+price+'円の ものを かうと、おつりは？',change+'円',[price+'円',(change+10)+'円',Math.max(0,change-10)+'円',(change+20)+'円'],'100−'+price+'='+change+'円。');}]
  ];}else{buckets=[
    [()=>mc('100円玉 1まいは 10円玉 なんまい？','10まい',['1まい','5まい','100まい'],'10円が 10まいで 100円。')],
    [()=>mc('500円玉 1まいは 100円玉 なんまい？','5まい',['10まい','50まい','2まい'],'100円が 5まいで 500円。')],
    [()=>{const hundreds=this.rand(1,4),tens=this.rand(1,9);return coin(Array(hundreds).fill(100).concat(Array(tens).fill(10)));},
    // 2つの ものを かう 代金（120円の ノートと 80円の けしゴム）。
    ()=>priceSum(this.rand(3,9)*10+(Math.random()<.5?100:0),this.rand(2,9)*10)],
    [()=>mc('1000円札 1まいは 100円玉 なんまい？','10まい',['100まい','5まい','1まい'],'100円が 10まいで 1000円。')],
    [()=>{const paid=[500,1000][this.rand(0,1)];let price=this.rand(2,Math.floor(paid/10)-1)*10;if(price*2===paid)price+=10;const change=paid-price;return mc(paid+'円を はらって '+price+'円の ものを かうと、おつりは？',change+'円',[price+'円',(change+10)+'円',Math.max(0,change-10)+'円',(change+20)+'円'],paid+'−'+price+'='+change+'円。');},
    // 1000円を こえる 代金（650円の 本と 480円の ふでばこ）。
    ()=>priceSum(this.rand(3,8)*100+this.rand(1,9)*10,this.rand(3,7)*100+this.rand(0,9)*10)]
  ];}return this.pickStage(stage,buckets,0);}
  pickGroups(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'groups'),make=(count,size,prompt,answer,ex)=>({topic:'groups',mode:'choices',isGroups:true,groupCount:count,groupSize:size,prompt:prompt,answer:''+answer,choices:this.pick4(''+answer,[answer+1,answer-1,answer+size,count,size,answer+count,answer+2].filter(v=>v>0&&v!==answer).map(String)),explanation:ex});const buckets=[
    [()=>{const count=this.rand(2,4),size=this.rand(2,4),total=count*size;return make(count,size,size+'こずつの まとまりが '+count+'つ。ぜんぶで いくつ？',total,size+'こが '+count+'つで '+total+'こ。');}],
    [()=>{const count=this.rand(2,5),size=this.rand(2,5),total=count*size;return make(count,size,total+'こを '+size+'こずつ かこむと、まとまりは いくつ？',count,size+'こずつで '+count+'まとまり。');}],
    [()=>{const people=this.rand(2,5),each=this.rand(2,5),total=people*each;return make(people,each,total+'こを '+people+'人に おなじかずずつ わけると、ひとりぶんは？',each,total+'こを '+people+'人に わけると '+each+'こずつ。');}],
    [()=>{const count=this.rand(2,6),size=this.rand(2,6),total=count*size;return make(count,size,'1さらに '+size+'こずつ、'+count+'さら。ぜんぶで いくつ？',total,size+'こずつが '+count+'つで '+total+'こ。');}],
    [()=>{const count=this.rand(3,8),size=this.rand(3,8),total=count*size;return make(count,size,size+'人の チームが '+count+'チーム。みんなで 何人？',total,size+'人が '+count+'チームで '+total+'人。');},
    // ばい: 4cmの 3ばいの 長さ、12cmは 4cmの 何ばい（ばいは 2年から）。
    ...(g<2?[]:[()=>{const size=this.rand(2,6),count=this.rand(2,5),total=size*count,ask=Math.random()<.5,answer=ask?total+'cm':count+'ばい';return{topic:'groups',mode:'choices',subtype:'times-of',prompt:ask?size+'cmの テープの '+count+'ばいの 長さは？':total+'cmの テープは、'+size+'cmの テープの 何ばい？',answer:answer,choices:this.pick4(answer,ask?[total+size,total-size,size+count,total+1].filter(v=>v>0&&v!==total).map(v=>v+'cm'):[count+1,count-1,size,total].filter(v=>v>0&&v!==count).map(v=>v+'ばい')),explanation:ask?size+'cmの '+count+'つ分が '+count+'ばい。'+Array(count).fill(size).join('+')+'='+total+'cm。':total+'cmは '+size+'cmの '+count+'つ分。だから '+count+'ばい。'};}])]
  ];return this.pickStage(stage,buckets,0);}
  pickOrder(p){const stage=this.topicStage(p,'order'),cmp=(left,right,lv,rv)=>{const ans=lv===rv?'＝':(lv>rv?'＞':'＜');return{topic:'order',mode:'choices',prompt:left+' □ '+right+'　□に はいる しるしは？',answer:ans,choices:this.shuffle(['＞','＜','＝']),explanation:lv+' と '+rv+' を くらべると '+lv+ans+rv+'。'};};const buckets=[
    [()=>{const a=this.rand(5,30),same=Math.random()<.25,b=same?a:this.rand(5,30);return cmp(''+a,''+b,a,b);}],
    // a=b=c だと 部分和が ぜんぶ 同じに なって 3たくに つぶれるので、
    // かならず ちがう 数（ans±1・ans+2）を うしろに たしておく。
    [()=>{const a=this.rand(1,9),b=this.rand(1,9),c=this.rand(1,9),ans=a+b+c;return{topic:'order',mode:'choices',subtype:'paren-add',prompt:a+' + ('+b+' + '+c+') は？',answer:''+ans,choices:this.pick4(''+ans,[a+b,a+c,b+c,ans+1,ans-1,ans+2].map(String)),explanation:'（ ）の なかを さきに。'+b+'+'+c+'='+(b+c)+'、'+a+'+'+(b+c)+'='+ans+'。'};},
    // No.44: （ ）が 前に ある 形（(4+5)+7）。たし算は どこから まとめても おなじ。
    ()=>{const a=this.rand(1,9),b=this.rand(1,9),c=this.rand(1,9),ans=a+b+c;return{topic:'order',mode:'choices',subtype:'paren-add',prompt:'('+a+' + '+b+') + '+c+' は？',answer:''+ans,choices:this.pick4(''+ans,[a+b,a+c,b+c,ans+1,ans-1,ans+2].map(String)),explanation:'（ ）の なかを さきに。'+a+'+'+b+'='+(a+b)+'、'+(a+b)+'+'+c+'='+ans+'。'};}],
    // c=1 だと a−b と ans+1 が 同じ 数に なるので、こちらも 予備の distractor を おく。
    [()=>{const b=this.rand(1,6),c=this.rand(1,6),a=this.rand(b+c+1,25),ans=a-(b+c);return{topic:'order',mode:'choices',prompt:a+' − ('+b+' + '+c+') は？',answer:''+ans,choices:this.pick4(''+ans,[a-b+c,a-b,ans+1,ans-1,ans+2].map(String)),explanation:'（ ）の なかを さきに。'+b+'+'+c+'='+(b+c)+'、'+a+'−'+(b+c)+'='+ans+'。'};}],
    [()=>{const a=this.rand(2,9),b=this.rand(1,9),c=this.rand(2,9),d=this.rand(1,9);return cmp(a+' + '+b,c+' + '+d,a+b,c+d);},
    // No.28-29: しきと 数の くらべ（60 + 70 □ 120、140 - 60 □ 90）。
    ()=>{const a=this.rand(2,9)*10,b=this.rand(1,9)*10,lv=a+b,rv=Math.random()<.25?lv:this.rand(8,19)*10;const q=cmp(a+' + '+b,''+rv,lv,rv);q.subtype='expr-cmp';return q;},
    ()=>{const a=this.rand(12,19)*10,b=this.rand(1,9)*10,lv=a-b,rv=Math.random()<.25?lv:this.rand(4,16)*10;const q=cmp(a+' - '+b,''+rv,lv,rv);q.subtype='expr-cmp';return q;}],
    // b=c だと 「a − b」と「a − c」が 同じ しきに なるので、
    // かならず ちがう しき「a + b」も 用意する。'c + b' は 正解と 同じ 意味に なるので だめ。
    [()=>{const b=this.rand(2,7),c=this.rand(1,6),a=this.rand(b+c+2,30),inside=b+c,ans=a-inside;return{topic:'order',mode:'choices',prompt:a+' − ('+b+' + '+c+')　さいしょに けいさんするのは？',answer:b+' + '+c,choices:this.pick4(b+' + '+c,[a+' − '+b,a+' − '+c,a+' − '+inside,a+' + '+b]),explanation:'（ ）の なかの '+b+'+'+c+' を さいしょに けいさんする。こたえは '+ans+'。'};},
    // 式と 計算: 3つの 代金を （ ）で まとめて 1つの しきに する（120 + (60 + 40)）。
    ()=>{const a=this.rand(8,15)*10,b=this.rand(2,8)*10,c=100-b,answer=a+' + ('+b+' + '+c+')';return{topic:'order',mode:'choices',subtype:'paren-story',prompt:'ノート '+a+'円と、えんぴつ '+b+'円・けしゴム '+c+'円を かいます。えんぴつと けしゴムの 代金を さきに まとめる しきは？',answer:answer,choices:this.pick4(answer,['('+a+' + '+b+') + '+c,a+' − ('+b+' + '+c+')',a+' + ('+b+' − '+c+')',a+' + '+b+' − '+c]),explanation:'えんぴつと けしゴムを （ ）で まとめる。'+b+' + '+c+' = 100、'+a+' + 100 = '+(a+100)+'円。'};}]
  ];return this.pickStage(stage,buckets,0);}
""";
    }
}
