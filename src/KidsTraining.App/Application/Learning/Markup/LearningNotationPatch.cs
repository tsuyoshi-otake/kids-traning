namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildLearningNotationScript()
    {
        return """
  notationIsAsciiLetter(ch){return !!ch&&/[A-Za-z]/.test(ch);}
  notationIsOperand(text,index){const ch=text[index]||'';if(/[0-9π□²³)\]}]/.test(ch))return true;if(!this.notationIsAsciiLetter(ch))return false;return !this.notationIsAsciiLetter(text[index-1])&&!this.notationIsAsciiLetter(text[index+1]);}
  notationNeighbor(text,index,direction){let cursor=index+direction;while(/\s/.test(text[cursor]||''))cursor+=direction;return{index:cursor,ch:text[cursor]||'',operand:this.notationIsOperand(text,cursor)};}
  notationFractionAt(text,index){const match=text.slice(index).match(/^((?:(?:\d+(?:\.\d+)?|[A-Za-zπ□])(?:[×*](?:\d+(?:\.\d+)?|[A-Za-zπ□]))*)\/(?:\d+(?:\.\d+)?|[A-Za-zπ□]))/);if(!match)return null;const source=match[1],before=text[index-1]||'',after=text[index+source.length]||'';if(/[A-Za-z0-9]/.test(before)||/[A-Za-z0-9]/.test(after))return null;const slash=source.lastIndexOf('/');return{source:source,numerator:source.slice(0,slash),denominator:source.slice(slash+1)};}
  notationRatioAt(text,index){const match=text.slice(index).match(/^(\d+(?:\.\d+)?)([:：])(\d+(?:\.\d+)?)/);if(!match)return null;const source=match[0],before=text[index-1]||'',after=text[index+source.length]||'';if(/[A-Za-z0-9]/.test(before)||/[A-Za-z0-9]/.test(after))return null;return{source:source,left:match[1],mark:match[2],right:match[3]};}
  notationUnaryAt(text,index){const ch=text[index]||'',before=this.notationNeighbor(text,index,-1),after=this.notationNeighbor(text,index,1);return (ch==='-'||ch==='−')&&after.operand&&(!before.ch||before.ch==='='||before.ch==='('||before.ch==='['||before.ch==='{'||before.ch===','||before.ch==='、'||before.ch===';'||before.ch==='；'||before.ch===':'||before.ch==='：'||before.ch==='+'||before.ch==='−'||before.ch==='-'||before.ch==='±');}
  notationOperatorAt(text,index){const ch=text[index]||'',before=this.notationNeighbor(text,index,-1),after=this.notationNeighbor(text,index,1),prev=before.operand,next=after.operand;if(ch==='-'||ch==='−')return (prev&&next)||this.notationUnaryAt(text,index);if(ch==='+'||ch==='*'||ch==='×'||ch==='÷'||ch==='='||ch==='<'||ch==='>'||ch==='≤'||ch==='≥'||ch==='±'){if(ch==='±'&&(before.ch==='='||before.ch==='(')&&next)return true;if(prev&&next)return true;if((ch==='='||ch==='±')&&(after.ch==='-'||after.ch==='−'||after.ch==='±'))return this.notationNeighbor(text,after.index,1).operand;}return false;}
  notationFraction(numerator,denominator,key){const label=denominator+'分の'+numerator;return React.createElement('span',{key:key,className:'kt-math kt-fraction',role:'math','aria-label':label,title:label},React.createElement('span',{className:'kt-fraction-numerator','aria-hidden':true},numerator),React.createElement('span',{className:'kt-fraction-denominator','aria-hidden':true},denominator));}
  notationRatio(left,mark,right,key){const label=left+'対'+right;return React.createElement('span',{key:key,className:'kt-math kt-ratio',role:'math','aria-label':label,title:label},React.createElement('span',{className:'kt-ratio-left','aria-hidden':true},left),React.createElement('span',{className:'kt-ratio-mark','aria-hidden':true},mark),React.createElement('span',{className:'kt-ratio-right','aria-hidden':true},right));}
  notationRadical(radicand,key){const label=radicand+'の平方根';return React.createElement('span',{key:key,className:'kt-math kt-radical',role:'math','aria-label':label,title:label},React.createElement('span',{className:'kt-radical-sign','aria-hidden':true},'√'),React.createElement('span',{className:'kt-radicand','aria-hidden':true},radicand));}
  notationSuperscript(base,power,key){const label=base+(power==='²'?'の2乗':'の3乗');return React.createElement('span',{key:key,className:'kt-math kt-power',role:'math','aria-label':label,title:label},React.createElement('span',{'aria-hidden':true},base),React.createElement('sup',{className:'kt-superscript','aria-hidden':true},power==='²'?'2':'3'));}
  notationOperator(op,key,unary){const labels={'+':'たす','−':unary?'マイナス':'ひく','×':'かける','÷':'わる','=':'イコール','<':'より小さい','>':'より大きい','≤':'以下','≥':'以上','±':'プラスマイナス'},label=labels[op]||op;return React.createElement('span',{key:key,className:'kt-math kt-math-operator',role:'math','aria-label':label,title:label},op);}
  withLearningNotation(value){if(value===null||value===undefined||Array.isArray(value)||React.isValidElement(value))return value;const text=String(value);let out=[],plain='',changed=false,i=0;const flush=()=>{if(plain){out.push(plain);plain='';}};while(i<text.length){const fraction=this.notationFractionAt(text,i);if(fraction){flush();out.push(this.notationFraction(fraction.numerator,fraction.denominator,'fraction-'+i));changed=true;i+=fraction.source.length;continue;}const ratio=this.notationRatioAt(text,i);if(ratio){flush();out.push(this.notationRatio(ratio.left,ratio.mark,ratio.right,'ratio-'+i));changed=true;i+=ratio.source.length;continue;}const radical=text[i]==='√'&&text.slice(i+1).match(/^(\d+(?:\.\d+)?|[A-Za-zπ])/);if(radical){flush();const radicand=radical[1];out.push(this.notationRadical(radicand,'radical-'+i));changed=true;i+=1+radicand.length;continue;}const power=(text[i+1]==='²'||text[i+1]==='³')&&(/[0-9A-Za-zπ□)]/.test(text[i]||''));if(power){flush();out.push(this.notationSuperscript(text[i],text[i+1],'power-'+i));changed=true;i+=2;continue;}if(this.notationOperatorAt(text,i)){flush();const sourceOp=text[i],op=sourceOp==='-'?'−':sourceOp==='*'?'×':sourceOp;out.push(this.notationOperator(op,'operator-'+i,this.notationUnaryAt(text,i)));changed=true;i++;continue;}plain+=text[i];i++;}flush();return changed?out:text;}
""";
    }

    private static string BuildLearningNotationStyles()
    {
        return """
<style id="kt-learning-notation">
  .kt-math { font-family: ui-rounded, "Arial Rounded MT Bold", "Noto Sans JP", sans-serif; font-variant-numeric: tabular-nums; }
  .kt-fraction { display:inline-grid; grid-template-rows:auto auto; vertical-align:middle; min-width:1.14em; margin:0 .07em; text-align:center; line-height:1.02; white-space:nowrap; }
  .kt-fraction-numerator { display:block; padding:0 .16em .06em; border-bottom:.075em solid currentColor; }
  .kt-fraction-denominator { display:block; padding:.06em .16em 0; }
  .kt-power { display:inline-flex; align-items:baseline; vertical-align:baseline; white-space:nowrap; }
  .kt-superscript { margin-left:.025em; font-size:.58em; font-weight:900; line-height:1; vertical-align:super; }
  .kt-radical { display:inline-flex; align-items:flex-start; vertical-align:middle; white-space:nowrap; line-height:1.02; }
  .kt-radical-sign { font-size:1.14em; line-height:.96; }
  .kt-radicand { border-top:.075em solid currentColor; padding:.03em .08em 0 .1em; }
  .kt-ratio { display:inline-flex; align-items:baseline; vertical-align:baseline; white-space:nowrap; }
  .kt-ratio-mark { padding:0 .08em; font-weight:900; }
  .kt-math-operator { display:inline-block; min-width:.45em; text-align:center; white-space:pre; }
  @media (max-width:480px) { .kt-fraction { margin:0 .045em; } .kt-fraction-numerator,.kt-fraction-denominator { padding-left:.1em; padding-right:.1em; } }
</style>
""";
    }
}
