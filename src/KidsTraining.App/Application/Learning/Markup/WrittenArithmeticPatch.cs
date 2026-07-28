namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    // Call after PatchLayoutAndTypography and PatchKeyboardQuestion: those patches
    // establish the final numeric prompt and quiz render state that this augments.
    private static string PatchWrittenArithmetic(string markup)
    {
        markup = ReplaceRequired(markup, "\n  renderVals(){", BuildWrittenArithmeticMethodsScript() + "\n  renderVals(){", StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[],typeSlots=[],typeKana='',typeHint='',typeShowHint=false,typeShowBoard=false,typeKeyRows=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[],typeSlots=[],typeKana='',typeHint='',typeShowHint=false,typeShowBoard=false,typeKeyRows=[],isWrittenArithmetic=false,isRegularNumericPrompt=true,isRegularChoicePrompt=true,writtenIsDivision=false,writtenIsColumn=false,writtenHasNote=false,writtenNote='',writtenRows=[],writtenAria='',writtenDivisor='',writtenDividend='',writtenQuotient='';", StringComparison.Ordinal);

        markup = ReplaceRequired(markup, "if(modeTyping){const canonical=String(q.answer||'').toLowerCase(),",
            "isPlainEq=isPlainEq&&!/[=?]/.test(String(q.prompt||''));const written=this.writtenArithmeticLayout(q);if(written){isWrittenArithmetic=true;isRegularNumericPrompt=false;isRegularChoicePrompt=false;writtenIsDivision=written.kind==='division';writtenIsColumn=written.kind==='column';writtenHasNote=!!written.note;writtenNote=written.note||'';writtenRows=written.rows||[];writtenAria=written.aria;writtenDivisor=written.divisor||'';writtenDividend=written.dividend||'';writtenQuotient=written.quotient||'';}\n      if(modeTyping){const canonical=String(q.answer||'').toLowerCase(),", StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, modeTyping:modeTyping, isHissan:false, isPlainEq:isPlainEq, isClock:isClock, typeSlots:typeSlots, typeKana:typeKana, typeHint:typeHint, typeShowHint:typeShowHint, typeShowBoard:typeShowBoard, typeKeyRows:typeKeyRows,",
            "modeNumeric:modeNumeric, modeChoices:modeChoices, modeHissanSteps:modeHissan, modeTyping:modeTyping, isHissan:false, isPlainEq:isPlainEq, isClock:isClock, typeSlots:typeSlots, typeKana:typeKana, typeHint:typeHint, typeShowHint:typeShowHint, typeShowBoard:typeShowBoard, typeKeyRows:typeKeyRows, isWrittenArithmetic:isWrittenArithmetic, isRegularNumericPrompt:isRegularNumericPrompt, isRegularChoicePrompt:isRegularChoicePrompt, writtenIsDivision:writtenIsDivision, writtenIsColumn:writtenIsColumn, writtenHasNote:writtenHasNote, writtenNote:writtenNote, writtenRows:writtenRows, writtenAria:writtenAria, writtenDivisor:writtenDivisor, writtenDividend:writtenDividend, writtenQuotient:writtenQuotient,", StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div class=\"kt-question-prompt kt-numeric-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}<sc-if value=\"{{ isPlainEq }}\" hint-placeholder-val=\"{{ true }}\"> = ?</sc-if></div>",
            "<sc-if value=\"{{ isWrittenArithmetic }}\" hint-placeholder-val=\"{{ false }}\">" + BuildWrittenArithmeticTemplate() + "</sc-if><sc-if value=\"{{ isRegularNumericPrompt }}\" hint-placeholder-val=\"{{ true }}\"><div class=\"kt-question-prompt kt-numeric-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}<sc-if value=\"{{ isPlainEq }}\" hint-placeholder-val=\"{{ true }}\"> = ?</sc-if></div></sc-if>", StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div class=\"kt-question-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}</div>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">",
            "<sc-if value=\"{{ isWrittenArithmetic }}\" hint-placeholder-val=\"{{ false }}\">" + BuildWrittenArithmeticTemplate() + "</sc-if><sc-if value=\"{{ isRegularChoicePrompt }}\" hint-placeholder-val=\"{{ true }}\"><div class=\"kt-question-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}</div></sc-if>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">", StringComparison.Ordinal);

        markup = ReplaceRequired(markup, "</head>", BuildWrittenArithmeticStyles() + "\n</head>", StringComparison.Ordinal);
        return markup;
    }

    private static string BuildWrittenArithmeticMethodsScript() => """
  writtenArithmeticLayout(q){const raw=String(q&&q.prompt||'').replace(/\s+/g,'').replace(/は？$/,'').replace(/[？?]$/,'');const upper=new Set(['864÷24','3.6×4','2.4×0.5','3.6÷0.9']),advanced=!!q&&q.topic==='hissan'&&Number(q.difficulty)>=5;if(!advanced&&!upper.has(raw))return null;const match=raw.match(/^(\d+(?:\.\d+)?)([+\-−×÷])(\d+(?:\.\d+)?)$/);if(!match)return null;const left=match[1],op=match[2]==='−'?'-':match[2],right=match[3];if(op==='÷'){if(raw!=='864÷24'&&raw!=='3.6÷0.9')return null;return raw==='3.6÷0.9'?{kind:'division',rows:[],divisor:'9',dividend:'36',quotient:'',note:'両方を10倍して　36 ÷ 9',aria:'3.6 わる 0.9。両方を10倍して、9 で 36 を割るひっ算です。'}:{kind:'division',rows:[],divisor:right,dividend:left,quotient:'',aria:left+' わる '+right+' のわり算のひっ算です。'};}const split=value=>{const parts=String(value).split('.');return{whole:parts[0],point:parts.length===2?'.':'',fraction:parts[1]||''};},a=split(left),b=split(right),decimalColumns=op!=='×',whole=decimalColumns?Math.max(a.whole.length,b.whole.length):Math.max(left.length,right.length),fraction=decimalColumns?Math.max(a.fraction.length,b.fraction.length):0,row=(operator,value,line)=>{const n=decimalColumns?split(value):{whole:value,point:'',fraction:''};return{operator:operator,whole:n.whole,point:n.point,fraction:n.fraction,style:'grid-template-columns:1.2ch minmax('+whole+'ch,max-content)'+(decimalColumns?' .45ch minmax('+fraction+'ch,max-content);':';')+(line?'border-bottom:5px solid #3a3326;padding-bottom:8px;':'')};},spoken=op==='+'?'たす':op==='-'?'ひく':'かける';return{kind:'column',rows:[row('',left,false),row(op,right,true)],aria:left+' '+spoken+' '+right+' のひっ算です。'};}
""";

    private static string BuildWrittenArithmeticTemplate() => """
<div class="kt-written-arithmetic" role="img" aria-label="{{ writtenAria }}">
  <sc-if value="{{ writtenHasNote }}" hint-placeholder-val="{{ false }}"><div class="kt-written-note">{{ writtenNote }}</div></sc-if>
  <sc-if value="{{ writtenIsDivision }}" hint-placeholder-val="{{ false }}"><div class="kt-written-division"><span class="kt-written-quotient">{{ writtenQuotient }}</span><span class="kt-written-divisor">{{ writtenDivisor }}</span><span class="kt-written-dividend">{{ writtenDividend }}</span></div></sc-if>
  <sc-if value="{{ writtenIsColumn }}" hint-placeholder-val="{{ false }}"><div class="kt-written-column"><sc-for list="{{ writtenRows }}" as="row" hint-placeholder-count="2"><div class="kt-written-row" style="{{ row.style }}"><span class="kt-written-operator">{{ row.operator }}</span><span class="kt-written-whole">{{ row.whole }}</span><span class="kt-written-point">{{ row.point }}</span><span class="kt-written-fraction">{{ row.fraction }}</span></div></sc-for></div></sc-if>
</div>
""";

    private static string BuildWrittenArithmeticStyles() => """
<style id="kt-written-arithmetic">
  .kt-written-arithmetic{display:flex;justify-content:center;align-items:center;box-sizing:border-box;max-width:100%;min-width:min(22rem,88vw);padding:18px 26px;background:#fffdf8;border:4px solid #f0e2c8;border-radius:26px;box-shadow:0 5px 0 #ead9bd;color:#3a3326;font-family:ui-monospace,SFMono-Regular,Menlo,monospace;font-size:clamp(38px,6vw,68px);font-weight:900;line-height:1.08;font-variant-numeric:tabular-nums;}
  .kt-written-column{display:grid;gap:7px;min-width:8ch;}.kt-written-row{display:grid;align-items:baseline;justify-content:end;}.kt-written-row span{display:block;min-width:0;}.kt-written-operator{text-align:left;padding-right:.16ch;}.kt-written-whole{text-align:right;}.kt-written-point{text-align:center;}.kt-written-fraction{text-align:left;}.kt-written-note{font-family:'Zen Maru Gothic',sans-serif;font-size:clamp(15px,2vw,20px);font-weight:800;color:#79633f;margin:0 18px 8px 0;align-self:flex-start;}
  .kt-written-division{display:grid;grid-template-columns:max-content max-content;grid-template-rows:auto auto;align-items:end;column-gap:14px;row-gap:6px;min-width:8ch;}.kt-written-quotient{grid-column:2;grid-row:1;text-align:left;padding-left:.22ch;}.kt-written-divisor{grid-column:1;grid-row:2;text-align:right;padding-right:.2ch;}.kt-written-dividend{grid-column:2;grid-row:2;border-top:5px solid #3a3326;border-left:5px solid #3a3326;padding:8px 0 0 .25ch;text-align:left;}
  @media (max-width:620px){.kt-written-arithmetic{min-width:0;width:min(88vw,24rem);padding:14px 18px;font-size:clamp(34px,12vw,54px);flex-direction:column;}.kt-written-note{margin:0 0 8px;}.kt-written-row{border-bottom-width:4px!important;}}
</style>
""";
}
