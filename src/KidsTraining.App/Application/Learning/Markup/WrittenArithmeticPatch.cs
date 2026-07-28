namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    // This patch runs after the keyboard and typography patches. It therefore owns
    // the dedicated written-work render mode and declares accessibility semantics
    // explicitly for every interactive element it adds.
    private static string PatchWrittenArithmetic(string markup)
    {
        markup = ReplaceRequired(markup, "\n  renderVals(){", BuildWrittenArithmeticMethodsScript() + "\n  renderVals(){", StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[],typeSlots=[],typeKana='',typeHint='',typeShowHint=false,typeShowBoard=false,typeKeyRows=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[],typeSlots=[],typeKana='',typeHint='',typeShowHint=false,typeShowBoard=false,typeKeyRows=[],modeWrittenSteps=false,writtenStepLabel='',writtenStepPrompt='',writtenAria='',writtenNote='',writtenHasNote=false,writtenBoardLines=[],writtenDensity='normal',writtenPrevious='',writtenHasPrevious=false,writtenChoiceTiles=[],showWrittenChoices=false,writtenHasError=false,writtenError='';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "if(modeTyping){const canonical=String(q.answer||'').toLowerCase(),",
            "const writtenPlan=this.writtenArithmeticPlan(q);if(writtenPlan){modeWrittenSteps=true;modeNumeric=false;modeChoices=false;modeHissan=false;isPlainEq=false;const writtenView=this.writtenArithmeticView(writtenPlan,S.waStep);writtenStepLabel=writtenView.stepLabel;writtenStepPrompt=writtenView.stepPrompt;writtenAria=writtenView.aria;writtenNote=writtenView.note;writtenHasNote=!!writtenView.note;writtenBoardLines=writtenView.lines;writtenDensity=writtenView.lines.length>7?'dense':'normal';writtenPrevious=writtenView.previous;writtenHasPrevious=!!writtenView.previous;writtenHasError=!!S.waError;writtenError=S.waError||'';if(S.waStepChoices){showWrittenChoices=true;writtenChoiceTiles=S.waStepChoices.map(c=>({text:c,ariaLabel:c+' を途中の答えとして入力',style:choiceSm,onClick:()=>this.submitWrittenStep(c)}));}}\n      if(modeTyping){const canonical=String(q.answer||'').toLowerCase(),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, modeTyping:modeTyping, isHissan:false, isPlainEq:isPlainEq, isClock:isClock, typeSlots:typeSlots, typeKana:typeKana, typeHint:typeHint, typeShowHint:typeShowHint, typeShowBoard:typeShowBoard, typeKeyRows:typeKeyRows,",
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, modeTyping:modeTyping, modeWrittenSteps:modeWrittenSteps, isHissan:false, isPlainEq:isPlainEq, isClock:isClock, typeSlots:typeSlots, typeKana:typeKana, typeHint:typeHint, typeShowHint:typeShowHint, typeShowBoard:typeShowBoard, typeKeyRows:typeKeyRows, writtenStepLabel:writtenStepLabel, writtenStepPrompt:writtenStepPrompt, writtenAria:writtenAria, writtenNote:writtenNote, writtenHasNote:writtenHasNote, writtenBoardLines:writtenBoardLines, writtenDensity:writtenDensity, writtenPrevious:writtenPrevious, writtenHasPrevious:writtenHasPrevious, writtenPad:writtenPad, writtenHasHint:!!S.waHint, writtenHint:S.waHint, showWrittenChoices:showWrittenChoices, writtenChoiceTiles:writtenChoiceTiles, writtenHasError:writtenHasError, writtenError:writtenError,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "\n    const choiceSm=",
            "\n" + BuildWrittenArithmeticPadScript() + "\n    const choiceSm=",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "      <!-- NUMERIC -->",
            BuildWrittenArithmeticTemplate() + "\n\n      <!-- NUMERIC -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup, "</head>", BuildWrittenArithmeticStyles() + "\n</head>", StringComparison.Ordinal);
        return markup;
    }

    private static string BuildWrittenArithmeticMethodsScript() => """
  writtenArithmeticExpression(q){
    const raw=String(q&&q.prompt||'').replace(/\s+/g,'').replace(/は？$/,'').replace(/[？?]$/,'');
    const match=raw.match(/^(\d+(?:\.\d+)?)([+\-−×÷])(\d+(?:\.\d+)?)$/);
    if(!match)return null;
    const op=match[2]==='−'?'-':match[2],fixed=new Set(['864÷24','3.6×4','2.4×0.5','3.6÷0.9']);
    const advancedHissan=!!q&&q.topic==='hissan'&&Number(q.difficulty)>=5;
    const advancedDivision=!!q&&q.topic==='div'&&Number(q.difficulty)>=5&&op==='÷';
    if(!fixed.has(raw)&&!advancedHissan&&!advancedDivision)return null;
    return{raw:raw,left:match[1],op:op,right:match[3]};
  }
  writtenArithmeticPlan(q){
    const expression=this.writtenArithmeticExpression(q);if(!expression)return null;
    if(expression.op==='×')return this.writtenMultiplicationPlan(expression);
    if(expression.op==='÷')return this.writtenDivisionPlan(expression);
    if(expression.op==='+'||expression.op==='-')return this.writtenAddSubPlan(expression);
    return null;
  }
  writtenPlaceName(column){const names=['一','十','百','千','万'];return(names[column]||String(Math.pow(10,column)))+'の位';}
  writtenDecimalPlaces(value){const point=String(value).indexOf('.');return point<0?0:String(value).length-point-1;}
  writtenIntegerDigits(value){const digits=String(value).replace('.','').replace(/^0+(?=\d)/,'');return digits||'0';}
  writtenAddSubPlan(expression){
    if(expression.left.includes('.')||expression.right.includes('.'))return null;
    const left=expression.left,right=expression.right,op=expression.op,a=[...left].reverse().map(Number),b=[...right].reverse().map(Number),width=Math.max(a.length,b.length),steps=[];
    if(op==='+'){
      let carry=0;
      for(let column=0;column<width;column++){
        const av=a[column]||0,bv=b[column]||0,incoming=carry,total=av+bv+incoming,digit=total%10;carry=Math.floor(total/10);
        const terms=(incoming?[String(incoming),String(av),String(bv)]:[String(av),String(bv)]).join(' ＋ ');
        steps.push({phase:'column',column:column,expect:String(total),writeDigit:String(digit),carry:carry,prompt:this.writtenPlaceName(column)+'：'+terms+' は？',explain:'この位をたして、答えを入力しよう。',completeText:terms+'＝'+total+(carry?'。'+digit+'を書いて、'+carry+'をくり上げる。':'。'+digit+'を書く。')});
      }
    }else{
      let borrow=0;
      for(let column=0;column<width;column++){
        const original=a[column]||0,bv=b[column]||0,afterBorrow=original-borrow,needsBorrow=afterBorrow<bv,top=afterBorrow+(needsBorrow?10:0),digit=top-bv;
        steps.push({phase:'column',column:column,expect:String(digit),writeDigit:String(digit),borrow:needsBorrow?1:0,prompt:this.writtenPlaceName(column)+'：'+(needsBorrow?(afterBorrow+'から'+bv+'は引けないので、10を借りて '+top+' − '+bv+' は？'):(afterBorrow+' − '+bv+' は？')),explain:needsBorrow?'左の位から1を借りると、この位には10が増えるよ。':'この位どうしを引こう。',completeText:(needsBorrow?('10を借りて '+top):String(afterBorrow))+' − '+bv+'＝'+digit+(needsBorrow?'。左の位は1減る。':'。')});
        borrow=needsBorrow?1:0;
      }
    }
    return{kind:op==='+'?'addition':'subtraction',op:op,left:left,right:right,width:width,steps:steps,note:'右の位から、一つずつ計算します。',aria:left+(op==='+'?' たす ':' ひく ')+right+'の筆算。まだ答えていない位は空欄です。'};
  }
  writtenMultiplicationPlan(expression){
    const left=expression.left,right=expression.right,leftDigits=this.writtenIntegerDigits(left),rightDigits=this.writtenIntegerDigits(right),decimalPlaces=this.writtenDecimalPlaces(left)+this.writtenDecimalPlaces(right),steps=[],prepare=[];
    if(this.writtenDecimalPlaces(left)>0)prepare.push({phase:'prepare',target:'left',expect:leftDigits,prompt:left+' の小数点をいったん外すと、どの整数になる？',explain:'数字の並びを変えずに、小数点だけをいったん外そう。',completeText:left+' を整数に直して '+leftDigits+'。'});
    if(this.writtenDecimalPlaces(right)>0)prepare.push({phase:'prepare',target:'right',expect:rightDigits,prompt:right+' の小数点をいったん外すと、どの整数になる？',explain:'数字の並びを変えずに、小数点だけをいったん外そう。',completeText:right+' を整数に直して '+rightDigits+'。'});
    steps.push(...prepare);
    const a=[...leftDigits].reverse().map(Number),b=[...rightDigits].reverse().map(Number),partials=[];
    for(let row=0;row<b.length;row++){
      const multiplier=b[row];let carry=0;
      for(let column=0;column<a.length;column++){
        const incoming=carry,total=a[column]*multiplier+incoming,last=column===a.length-1,digit=total%10;carry=Math.floor(total/10);
        const writes=[];
        if(last){[...String(total)].reverse().forEach((ch,offset)=>writes.push({position:row+column+offset,digit:ch}));}
        else writes.push({position:row+column,digit:String(digit)});
        steps.push({phase:'partial',row:row,column:column,expect:String(total),writes:writes,carry:last?0:carry,prompt:(row?('十の位から'+row+'けた分、左へずらす。'):'')+a[column]+' × '+multiplier+(incoming?' ＋ くり上がり '+incoming:'')+' は？',explain:'かけた答えを入力し、一の位を書いて残りをくり上げよう。',completeText:a[column]+' × '+multiplier+(incoming?' ＋ '+incoming:'')+'＝'+total+(last?'。残りをまとめて書く。':('。'+digit+'を書いて、'+carry+'をくり上げる。'))});
      }
      partials.push(Number(leftDigits)*multiplier*Math.pow(10,row));
    }
    if(partials.length>1){
      const product=Number(leftDigits)*Number(rightDigits),sumWidth=String(product).length;let carry=0;
      for(let column=0;column<sumWidth;column++){
        const values=partials.map(value=>Math.floor(value/Math.pow(10,column))%10),incoming=carry,total=values.reduce((sum,value)=>sum+value,0)+incoming,digit=total%10;carry=Math.floor(total/10);
        steps.push({phase:'sum',column:column,expect:String(total),writeDigit:String(digit),carry:carry,prompt:'部分積の'+this.writtenPlaceName(column)+'：'+values.join(' ＋ ')+(incoming?' ＋ くり上がり '+incoming:'')+' は？',explain:'そろえた部分積を、右の位から足そう。',completeText:values.join(' ＋ ')+(incoming?' ＋ '+incoming:'')+'＝'+total+(carry?'。'+digit+'を書いて、'+carry+'をくり上げる。':'。'+digit+'を書く。')});
      }
    }
    if(decimalPlaces>0)steps.push({phase:'decimal',expect:String(decimalPlaces),prompt:'もとの2つの数で、小数点より右の数字は合わせて何けた？',explain:'2つの数の、小数点より右のけた数を足そう。',completeText:'小数点より右は合わせて'+decimalPlaces+'けた。整数の積の右から'+decimalPlaces+'けた戻す。'});
    const width=Math.max(leftDigits.length+rightDigits.length,String(Number(leftDigits)*Number(rightDigits)).length);
    return{kind:'multiplication',op:'×',left:left,right:right,leftDigits:leftDigits,rightDigits:rightDigits,decimalPlaces:decimalPlaces,prepareCount:prepare.length,partials:partials,width:width,steps:steps,note:decimalPlaces?'小数点をいったん外して整数の筆算をし、最後に小数点を戻します。':'右の位から部分積を作り、位をそろえて足します。',aria:left+' かける '+right+'の筆算。正解した途中の数字だけを表示します。'};
  }
  writtenDivisionPlan(expression){
    const left=expression.left,right=expression.right,scale=Math.max(this.writtenDecimalPlaces(left),this.writtenDecimalPlaces(right)),factor=Math.pow(10,scale),dividend=String(Math.round(Number(left)*factor)),divisor=String(Math.round(Number(right)*factor)),steps=[],prepare=[];
    if(scale>0){
      prepare.push({phase:'prepare',target:'dividend',expect:dividend,prompt:left+' を '+factor+'倍すると？',explain:'割られる数の小数点を右へ'+scale+'けた動かそう。',completeText:left+'を'+factor+'倍して'+dividend+'。'});
      prepare.push({phase:'prepare',target:'divisor',expect:divisor,prompt:right+' も同じように '+factor+'倍すると？',explain:'割る数も、同じだけ小数点を動かそう。',completeText:right+'を'+factor+'倍して'+divisor+'。'});
    }
    steps.push(...prepare);
    const digits=[...dividend].map(Number),d=Number(divisor),iterations=[];let end=0,current=digits[0];
    while(current<d&&end<digits.length-1){end++;current=current*10+digits[end];}
    while(end<digits.length){
      const quotient=Math.floor(current/d),product=quotient*d,remainder=current-product,iteration={end:end,current:current,quotient:quotient,product:product,remainder:remainder};iterations.push(iteration);
      steps.push({phase:'quotient',iteration:iterations.length-1,expect:String(quotient),prompt:d+' は '+current+' の中に何回入る？ 商の1けたを入力しよう。',explain:'大きくなりすぎない回数を考えよう。',completeText:d+'は'+current+'の中に'+quotient+'回入る。商に'+quotient+'を書く。'});
      steps.push({phase:'product',iteration:iterations.length-1,expect:String(product),prompt:d+' × いま書いた商 '+quotient+' は？',explain:'割る数と、いま書いた商をかけよう。',completeText:d+' × '+quotient+'＝'+product+'。'+current+'の下にそろえて書く。'});
      steps.push({phase:'remainder',iteration:iterations.length-1,expect:String(remainder),prompt:current+' − '+product+' は？',explain:'上の数から、かけ戻した数を引こう。',completeText:current+' − '+product+'＝'+remainder+'。'});
      if(end>=digits.length-1)break;
      const nextDigit=digits[end+1],nextCurrent=remainder*10+nextDigit;
      iteration.nextDigit=nextDigit;iteration.nextCurrent=nextCurrent;
      steps.push({phase:'bring',iteration:iterations.length-1,expect:String(nextCurrent),prompt:'あまり '+remainder+' の右に、次の数字 '+nextDigit+' を下ろすと？',explain:'あまりの右に、次の位の数字をそのまま下ろそう。',completeText:remainder+'の右に'+nextDigit+'を下ろして'+nextCurrent+'。'});
      end++;current=nextCurrent;
    }
    return{kind:'division',op:'÷',left:left,right:right,dividend:dividend,divisor:divisor,scale:scale,prepareCount:prepare.length,iterations:iterations,steps:steps,note:scale?'割られる数と割る数を同じだけ10倍して、整数の割り算にします。':'商・かけ戻し・引き算・次の位の順に進めます。',aria:left+' わる '+right+'の筆算。正解した途中の数字だけを表示します。'};
  }
  writtenArithmeticView(plan,completedValue){
    const completed=Math.max(0,Math.min(plan.steps.length,Number(completedValue)||0)),active=plan.steps[completed]||null,done=plan.steps.slice(0,completed),lines=[];
    const blank=' ',pad=(value,width)=>String(value).padStart(width,' '),doneHas=(phase,target)=>done.some(step=>step.phase===phase&&(!target||step.target===target));
    if(plan.kind==='addition'||plan.kind==='subtraction'){
      const width=plan.width+1,result=Array(plan.width+1).fill(blank),marks=Array(plan.width+1).fill(' ');
      const superscript=['⁰','¹','²','³','⁴','⁵','⁶','⁷','⁸','⁹'];
      for(const step of done){result[step.column]=step.writeDigit;if(step.carry)marks[step.column+1]=superscript[step.carry]||String(step.carry);if(step.borrow)marks[step.column+1]='↘';}
      while(result.length>1&&result[result.length-1]===blank&&result.length>plan.width)result.pop();
      lines.push({text:'  '+marks.reverse().join(''),tone:'marks'});
      lines.push({text:'  '+pad(plan.left,width),tone:'number'});
      lines.push({text:(plan.op==='+'?'＋':'−')+' '+pad(plan.right,width),tone:'number'});
      lines.push({text:'─'.repeat(width+2),tone:'rule'});
      lines.push({text:'  '+result.reverse().join(''),tone:'result'});
    }else if(plan.kind==='multiplication'){
      const readyLeft=!plan.left.includes('.')||doneHas('prepare','left'),readyRight=!plan.right.includes('.')||doneHas('prepare','right'),width=plan.width;
      if(plan.decimalPlaces)lines.push({text:'もとの式　'+plan.left+' × '+plan.right,tone:'caption'});
      lines.push({text:'  '+pad(readyLeft?plan.leftDigits:blank,width),tone:'number'});
      lines.push({text:'× '+pad(readyRight?plan.rightDigits:blank,width),tone:'number'});
      lines.push({text:'─'.repeat(width+2),tone:'rule'});
      const partialRows=plan.partials.map((value,row)=>{const chars=Array(width).fill(blank);for(let i=0;i<row;i++)chars[i]='0';for(const step of done.filter(item=>item.phase==='partial'&&item.row===row))for(const write of step.writes)chars[write.position]=write.digit;return chars.reverse().join('');});
      partialRows.forEach(text=>lines.push({text:'  '+text,tone:'partial'}));
      if(plan.partials.length>1){const result=Array(width).fill(blank);for(const step of done.filter(item=>item.phase==='sum')){result[step.column]=step.writeDigit;if(step.carry&&step.column+1<result.length)result[step.column+1]=String(step.carry);}lines.push({text:'─'.repeat(width+2),tone:'rule'});lines.push({text:'  '+result.reverse().join(''),tone:'result'});}
      if(plan.decimalPlaces)lines.push({text:doneHas('decimal')?'小数点を右から'+plan.decimalPlaces+'けた戻す':'小数点の位置　'+blank,tone:'caption'});
    }else if(plan.kind==='division'){
      const readyDividend=!plan.scale||doneHas('prepare','dividend'),readyDivisor=!plan.scale||doneHas('prepare','divisor'),digits=plan.dividend.length,divisorWidth=plan.divisor.length,dividendStart=divisorWidth+3,quotient=Array(digits).fill(' ');
      for(let i=0;i<plan.iterations.length;i++){const iteration=plan.iterations[i],qDone=done.some(step=>step.phase==='quotient'&&step.iteration===i);quotient[iteration.end]=qDone?String(iteration.quotient):blank;}
      if(plan.scale)lines.push({text:'もとの式　'+plan.left+' ÷ '+plan.right,tone:'caption'});
      lines.push({text:' '.repeat(dividendStart)+quotient.join(''),tone:'result'});
      lines.push({text:' '.repeat(divisorWidth+1)+'┌'+'─'.repeat(digits+1),tone:'rule'});
      lines.push({text:pad(readyDivisor?plan.divisor:blank,divisorWidth)+' │ '+(readyDividend?plan.dividend:blank),tone:'number'});
      for(let i=0;i<plan.iterations.length;i++){
        const iteration=plan.iterations[i],qDone=done.some(step=>step.phase==='quotient'&&step.iteration===i),productDone=done.some(step=>step.phase==='product'&&step.iteration===i),remainderDone=done.some(step=>step.phase==='remainder'&&step.iteration===i),bringDone=done.some(step=>step.phase==='bring'&&step.iteration===i);
        if(!qDone)break;
        if(!productDone)break;
        const productText=String(iteration.product),currentWidth=String(iteration.current).length,currentStart=iteration.end-currentWidth+1;
        lines.push({text:' '.repeat(dividendStart+currentStart)+pad(productText,currentWidth),tone:'partial'});
        lines.push({text:' '.repeat(dividendStart+currentStart)+'─'.repeat(Math.max(currentWidth,productText.length)),tone:'rule'});
        if(!remainderDone)break;
        const shown=bringDone?String(iteration.nextCurrent):String(iteration.remainder),shownEnd=bringDone?iteration.end+1:iteration.end;
        lines.push({text:' '.repeat(dividendStart+shownEnd-shown.length+1)+shown,tone:'result'});
      }
    }
    const arithmeticLines=lines.filter(line=>line.tone!=='caption'),boardWidth=Math.max(1,...arithmeticLines.map(line=>String(line.text).length));
    for(const line of arithmeticLines)line.text=String(line.text).padEnd(boardWidth,' ');
    return{stepLabel:active?('ステップ '+(completed+1)+' / '+plan.steps.length):'途中式 完了',stepPrompt:active?active.prompt:'',previous:completed?done[done.length-1].completeText:'',lines:lines,note:plan.note,aria:plan.aria+' '+(active?('現在はステップ'+(completed+1)+'、'+active.prompt):'途中式は完了しました。')};
  }
""";

    private static string BuildWrittenArithmeticPadScript() => """
    const writtenPad=['1','2','3','4','5','6','7','8','9'].map(n=>({label:n,ariaLabel:n+' を入力',style:keyTile,onClick:()=>this.press(n)}));
    writtenPad.push({label:'けす',ariaLabel:'入力した数字を1けた消す',style:keyClear,onClick:()=>this.del()});
    writtenPad.push({label:'0',ariaLabel:'0 を入力',style:keyTile,onClick:()=>this.press('0')});
    writtenPad.push({label:'OK',ariaLabel:'この途中の答えを決定',style:keyOk,onClick:()=>this.submitWrittenStep()});
""";

    private static string BuildWrittenArithmeticTemplate() => """
      <!-- INTERACTIVE WRITTEN ARITHMETIC -->
      <sc-if value="{{ modeWrittenSteps }}" hint-placeholder-val="{{ false }}">
        <div class="kt-written-step-shell">
          <div class="kt-written-step-work">
            <sc-if value="{{ writtenHasNote }}" hint-placeholder-val="{{ false }}"><div class="kt-written-step-note">{{ writtenNote }}</div></sc-if>
            <div class="kt-written-step-board" data-density="{{ writtenDensity }}" role="img" aria-label="{{ writtenAria }}">
              <sc-for list="{{ writtenBoardLines }}" as="line" hint-placeholder-count="8"><div class="kt-written-step-line" data-tone="{{ line.tone }}" aria-hidden="true">{{ line.text }}</div></sc-for>
            </div>
            <div class="kt-written-step-instruction" role="status" aria-live="polite" aria-atomic="true">
              <div class="kt-written-step-count">{{ writtenStepLabel }}</div>
              <div class="kt-written-step-prompt">{{ writtenStepPrompt }}</div>
            </div>
            <sc-if value="{{ writtenHasPrevious }}" hint-placeholder-val="{{ false }}"><div class="kt-written-step-previous" role="status">✓ {{ writtenPrevious }}</div></sc-if>
          </div>
          <div class="kt-written-step-controls">
            <div class="kt-written-step-answer" aria-label="入力中の途中の答え">{{ ansBoxShown }}</div>
            <sc-if value="{{ writtenHasHint }}" hint-placeholder-val="{{ false }}"><div class="kt-written-step-hint" role="alert">💡 {{ writtenHint }}</div></sc-if>
            <sc-if value="{{ writtenHasError }}" hint-placeholder-val="{{ false }}"><div class="kt-written-step-error" role="alert">{{ writtenError }}</div></sc-if>
            <sc-if value="{{ showWrittenChoices }}" hint-placeholder-val="{{ false }}">
              <div class="kt-written-step-choices">
                <div class="kt-written-step-choice-label">むずかしいときは、途中の答えを選べます</div>
                <div class="kt-written-step-choice-grid"><sc-for list="{{ writtenChoiceTiles }}" as="c" hint-placeholder-count="4"><div role="button" tabindex="0" aria-label="{{ c.ariaLabel }}" onclick="{{ c.onClick }}" style="{{ c.style }}">{{ c.text }}</div></sc-for></div>
              </div>
            </sc-if>
            <div class="kt-written-step-pad" aria-label="数字入力パッド">
              <sc-for list="{{ writtenPad }}" as="k" hint-placeholder-count="12"><div role="button" tabindex="0" aria-label="{{ k.ariaLabel }}" onclick="{{ k.onClick }}" style="{{ k.style }}">{{ k.label }}</div></sc-for>
            </div>
          </div>
        </div>
      </sc-if>
""";

    private static string BuildWrittenArithmeticStyles() => """
<style id="kt-written-arithmetic-steps">
  .kt-written-step-shell{flex:1;display:grid;grid-template-columns:minmax(0,1fr) 300px;gap:28px;margin-top:14px;align-items:start;}
  .kt-written-step-work{min-width:0;display:flex;flex-direction:column;gap:12px;align-items:stretch;}
  .kt-written-step-controls{min-width:0;display:flex;flex-direction:column;gap:12px;align-items:stretch;}
  .kt-written-step-note{background:#fff7ec;border:3px solid #f0e2c8;border-radius:16px;padding:9px 14px;color:#765f3d;font-size:17px;font-weight:800;text-align:center;}
  .kt-written-step-board{box-sizing:border-box;max-width:100%;min-height:190px;overflow-x:auto;background:#fffdf8;border:4px solid #f0e2c8;border-radius:26px;padding:18px 24px;box-shadow:0 5px 0 #ead9bd;color:#3a3326;font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;font-size:clamp(29px,4.6vw,48px);font-weight:900;line-height:1.08;font-variant-numeric:tabular-nums;}
  .kt-written-step-board[data-density="dense"]{font-size:clamp(27px,3.5vw,38px);}
  .kt-written-step-line{width:max-content;min-width:0;margin-inline:auto;white-space:pre;text-align:left;}
  .kt-written-step-line[data-tone="marks"]{color:#b85e1f;line-height:.9;}
  .kt-written-step-line[data-tone="caption"]{width:100%;color:#b85e1f;font-family:'Zen Maru Gothic',sans-serif;font-size:.42em;line-height:1.5;text-align:center;}
  .kt-written-step-line[data-tone="rule"]{color:#3a3326;}.kt-written-step-line[data-tone="result"]{color:#167a58;}.kt-written-step-line[data-tone="partial"]{color:#425f9e;}
  .kt-written-step-instruction{background:#fff7ec;border:3px solid #f0e2c8;border-radius:18px;padding:13px 18px;}
  .kt-written-step-count{font-size:15px;color:#e0552f;font-weight:900;}.kt-written-step-prompt{font-size:clamp(20px,2.5vw,26px);font-weight:900;margin-top:3px;line-height:1.45;}
  .kt-written-step-previous{background:#eafaef;border:3px solid #9fd8ae;border-radius:14px;padding:8px 13px;color:#246c3e;font-size:17px;font-weight:800;}
  .kt-written-step-answer{background:#fff;border:4px dashed #d8c4a0;border-radius:16px;min-height:74px;display:flex;align-items:center;justify-content:center;font-size:44px;font-weight:900;color:#3a3326;}
  .kt-written-step-hint{background:#fff6db;border:3px solid #ffd24a;border-radius:16px;padding:11px 15px;font-size:18px;color:#7a5d00;font-weight:800;}.kt-written-step-error{background:#fdeeee;border:3px solid #d2503f;border-radius:16px;padding:11px 15px;color:#8f271f;font-weight:900;}
  .kt-written-step-choice-label{font-size:16px;color:#13776f;font-weight:800;margin-bottom:8px;}.kt-written-step-choice-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;}
  .kt-written-step-pad{width:300px;display:grid;grid-template-columns:repeat(3,1fr);gap:12px;align-content:start;}
  .kt-written-step-pad [role="button"],.kt-written-step-choice-grid [role="button"]{min-width:44px;min-height:44px;touch-action:manipulation;}
  .kt-written-step-pad [role="button"]:hover,.kt-written-step-choice-grid [role="button"]:hover{filter:brightness(.97);transform:translateY(-1px);}.kt-written-step-pad [role="button"]:active,.kt-written-step-choice-grid [role="button"]:active{transform:translateY(3px);box-shadow:0 2px 0 #d8c4a0!important;}.kt-written-step-pad [role="button"]:focus-visible,.kt-written-step-choice-grid [role="button"]:focus-visible{outline:4px solid #155eef;outline-offset:3px;}
  @media (prefers-reduced-motion:reduce){.kt-written-step-pad [role="button"],.kt-written-step-choice-grid [role="button"]{transition:none!important;}}
  @media (min-width:761px) and (max-height:900px){
    .kt-written-step-shell{gap:18px;margin-top:6px;}.kt-written-step-work,.kt-written-step-controls{gap:8px;}
    .kt-written-step-note{padding:5px 10px;border-width:2px;border-radius:12px;font-size:14px;line-height:1.25;}
    .kt-written-step-board{min-height:0;padding:8px 14px;border-width:3px;border-radius:20px;box-shadow:0 3px 0 #ead9bd;font-size:clamp(28px,4.2vh,38px);line-height:1.02;}
    .kt-written-step-instruction{padding:7px 12px;border-width:2px;border-radius:14px;}.kt-written-step-count{font-size:13px;}.kt-written-step-prompt{font-size:clamp(17px,2.4vh,22px);line-height:1.25;margin-top:2px;}
    .kt-written-step-previous{padding:5px 10px;border-width:2px;border-radius:11px;font-size:15px;line-height:1.25;}
    .kt-written-step-answer{min-height:52px;border-width:3px;border-radius:13px;font-size:32px;}.kt-written-step-hint,.kt-written-step-error{padding:7px 10px;border-width:2px;border-radius:12px;font-size:15px;line-height:1.25;}
    .kt-written-step-choice-label{font-size:14px;margin-bottom:5px;}.kt-written-step-choice-grid{gap:8px;}.kt-written-step-pad{gap:8px;}.kt-written-step-pad [role="button"]{height:clamp(48px,7.2vh,64px)!important;min-height:48px!important;font-size:clamp(21px,3vh,28px)!important;border-radius:14px!important;}
  }
  @media (min-width:761px) and (max-height:700px){
    .kt-written-step-shell{gap:18px;margin-top:6px;}.kt-written-step-work,.kt-written-step-controls{gap:5px;}
    .kt-written-step-note{display:none;}.kt-written-step-board{min-height:0;padding:5px 12px;border-width:3px;border-radius:18px;box-shadow:0 3px 0 #ead9bd;font-size:clamp(24px,5.1vh,30px);line-height:1.01;}.kt-written-step-board[data-density="dense"]{font-size:clamp(20px,3.9vh,23px);}
    .kt-written-step-instruction{padding:5px 10px;border-width:2px;border-radius:12px;}.kt-written-step-count{font-size:12px;}.kt-written-step-prompt{font-size:clamp(16px,2.8vh,20px);line-height:1.2;margin-top:1px;}
    .kt-written-step-previous{padding:4px 9px;border-width:2px;border-radius:10px;font-size:13px;line-height:1.2;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
    .kt-written-step-answer{min-height:44px;border-width:3px;border-radius:12px;font-size:28px;}.kt-written-step-hint,.kt-written-step-error{padding:5px 9px;border-width:2px;border-radius:10px;font-size:13px;line-height:1.2;}
    .kt-written-step-choice-label{font-size:13px;margin-bottom:4px;}.kt-written-step-choice-grid{gap:6px;}.kt-written-step-pad{gap:6px;}.kt-written-step-pad [role="button"]{height:44px!important;min-height:44px!important;font-size:20px!important;border-radius:12px!important;}
    .kt-written-step-controls:has(.kt-written-step-choices) .kt-written-step-pad{display:none;}
  }
  @media (max-width:760px){.kt-written-step-shell{grid-template-columns:1fr;gap:16px;}.kt-written-step-controls{width:100%;max-width:360px;margin:0 auto;}.kt-written-step-pad{width:100%;}.kt-written-step-board{min-height:160px;padding:14px 12px;font-size:clamp(25px,8.7vw,42px);}.kt-written-step-choice-grid{grid-template-columns:repeat(2,minmax(0,1fr));}}
  @media (max-width:760px) and (max-height:900px){
    div:has(> .kt-written-step-shell) > .kt-question-metadata{display:grid!important;grid-template-columns:repeat(2,minmax(0,1fr));gap:4px!important;margin-top:4px!important;align-items:stretch!important;}
    div:has(> .kt-written-step-shell) > .kt-question-metadata > span{box-sizing:border-box!important;width:auto!important;height:32px!important;min-height:32px!important;padding:3px 6px!important;border-width:2px!important;border-radius:10px!important;font-size:12px!important;line-height:1.1!important;white-space:nowrap!important;overflow:hidden!important;text-overflow:ellipsis!important;}
    .kt-written-step-shell{gap:4px;margin-top:4px;}.kt-written-step-work,.kt-written-step-controls{gap:3px;}
    .kt-written-step-note{display:none;}.kt-written-step-board{min-height:0;padding:5px 7px;border-width:3px;border-radius:16px;box-shadow:0 3px 0 #ead9bd;font-size:clamp(20px,5.4vw,22px);line-height:1.01;}.kt-written-step-board[data-density="dense"]{font-size:clamp(17px,4.7vw,19px);}
    .kt-written-step-instruction{padding:4px 8px;border-width:2px;border-radius:11px;}.kt-written-step-count{font-size:11px;}.kt-written-step-prompt{font-size:16px;line-height:1.18;margin-top:0;}
    .kt-written-step-previous{position:absolute!important;width:1px!important;height:1px!important;padding:0!important;margin:-1px!important;overflow:hidden!important;clip:rect(0,0,0,0)!important;white-space:nowrap!important;border:0!important;}
    .kt-written-step-answer{min-height:44px;border-width:3px;border-radius:11px;font-size:27px;}.kt-written-step-hint,.kt-written-step-error{padding:4px 7px;border-width:2px;border-radius:9px;font-size:12px;line-height:1.15;}
    .kt-written-step-choice-label{font-size:12px;margin-bottom:3px;}.kt-written-step-choice-grid{grid-template-columns:repeat(4,minmax(0,1fr));gap:4px;}.kt-written-step-pad{gap:4px;}.kt-written-step-pad [role="button"],.kt-written-step-choice-grid [role="button"]{height:44px!important;min-height:44px!important;font-size:18px!important;border-radius:11px!important;}
    .kt-written-step-controls:has(.kt-written-step-choices) .kt-written-step-pad{display:none;}
  }
</style>
""";
}
