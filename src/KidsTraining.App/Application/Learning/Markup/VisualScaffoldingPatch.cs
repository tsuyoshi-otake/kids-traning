namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchArithmeticVisuals(string markup)
    {
        markup = ReplaceRequired(markup,
            "let isAddViz=false,addFrames=[],isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            "let isAddViz=false,addFrames=[],isMulViz=false,mulGroups=[],isMeasureViz=false,measureRows=[],isShapeViz=false,shapeStyle='',promptStyle='',isKokugo=false,isNotKokugo=false,kokuPre='',kokuWord='',kokuPost='',kokuMean='',kokuInstruction='',kokuShowMean=false,clockMarks=[],clockAskLabel='',showNumChoices=false,numChoiceTiles=[],showHsChoices=false,hsChoiceTiles=[];",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));",
            "if(modeChoices)choices=q.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.submit(c)}));\n      if(modeChoices&&q.topic==='mul'&&this.topicStage(p,'mul')<=2){isMulViz=true;const a=Number(q.a)||0,b=Number(q.b)||0;for(let g=0;g<b;g++){const cells=[];for(let i=0;i<a;i++)cells.push({style:'width:16px;height:16px;border-radius:50%;background:#1fa39a;border:2px solid #178a82;'});mulGroups.push({cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+Math.min(a,5)+',16px);gap:4px;padding:8px;border-radius:12px;border:3px solid #b8e8e2;background:#e6fbf7;'});}}\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;[['あか','#e05a4e','#b8443a',Number(q.m1)||0],['あお','#4f7edb','#3a5fb0',Number(q.m2)||0],['きいろ','#e0aa2e','#b3831a',Number(q.m3)||0]].filter(r=>r[3]>0).forEach(r=>{const cells=[];for(let i=0;i<r[3];i++)cells.push({style:'width:24px;height:24px;border-radius:6px;background:'+r[1]+';border:2px solid '+r[2]+';'});measureRows.push({label:r[0],labelStyle:'font-size:22px;font-weight:900;min-width:80px;color:'+r[1]+';',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+r[3]+',24px);gap:4px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});});}\n      if(modeChoices&&q.isChart){isMeasureViz=true;(q.rows||[]).forEach(r=>{const cells=[];for(let i=0;i<r.count;i++)cells.push({style:'width:24px;height:24px;border-radius:6px;background:'+r.color+';border:2px solid '+r.border+';'});measureRows.push({label:r.label,labelStyle:'font-size:20px;font-weight:900;min-width:76px;color:#5b5040;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+r.count+',24px);gap:4px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});});}\n      if(modeChoices&&q.isFracViz){isMeasureViz=true;const fd=Number(q.fd)||2,fn=Number(q.fn)||1,cells=[];for(let i=0;i<fd;i++)cells.push({style:'width:34px;height:34px;'+(i<fn?'background:#d64f8e;border:2px solid #b03a72;':'background:#fff;border:2px dashed #d8c4a0;')});measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+fd+',34px);gap:0px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});}\n      if(modeChoices&&q.isOrder){isMeasureViz=true;const oc=Number(q.oc)||5,op=Number(q.op)||1,cells=[];for(let i=0;i<oc;i++){const idx=q.od==='ひだり'?i+1:oc-i;cells.push({style:'width:36px;height:36px;border-radius:9px;'+(idx===op?'background:#f2a03d;border:3px solid #b07a10;':'background:#cfe3f7;border:2px solid #9db8d4;')});}measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+oc+',36px);gap:6px;padding:8px;border-radius:12px;border:3px solid #f0e2c8;background:#fff;'});}\n      if(modeChoices&&q.isShape){isShapeViz=true;shapeStyle=q.shapeStyle||'';}\n      const plen=String(q.prompt||'').length;promptStyle='font-size:'+(plen>16?30:(plen>11?40:54))+'px; font-weight:900; text-align:center; margin-bottom:6px; white-space:'+(plen>16?'normal':'nowrap')+'; max-width:880px; line-height:1.35;';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;",
            "\n      if(modeChoices&&q.topic==='div'&&this.topicStage(p,'div')<=2){isMulViz=true;const groups=Number(q.d)||0,each=Number(q.q0)||0;for(let g=0;g<groups;g++){const cells=[];for(let i=0;i<each;i++)cells.push({style:'width:16px;height:16px;border-radius:50%;background:#4f7edb;border:2px solid #3a5fb0;'});mulGroups.push({cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+Math.min(each,5)+',16px);gap:4px;padding:8px;border-radius:12px;border:3px solid #b8c9ef;background:#eef3ff;'});}}\n      if(modeChoices&&q.isGroups){isMulViz=true;const groups=Number(q.groupCount)||0,each=Number(q.groupSize)||0;for(let g=0;g<groups;g++){const cells=[];for(let i=0;i<each;i++)cells.push({style:'width:18px;height:18px;border-radius:50%;background:#22a68b;border:2px solid #16836f;'});mulGroups.push({cells:cells,style:'display:inline-grid;grid-template-columns:repeat('+Math.min(each,5)+',18px);gap:5px;padding:9px;border-radius:14px;border:3px solid #95d9cb;background:#effcf8;'});}}\n      if(modeChoices&&q.isMoney){isMeasureViz=true;const cells=(q.moneyPieces||[]).map(v=>({text:v+'円',style:'width:'+(v>=100?58:44)+'px;height:44px;border-radius:'+(v>=100?'9px':'50%')+';display:flex;align-items:center;justify-content:center;background:'+(v>=100?'#d8efd0':'#f2deb0')+';border:3px solid '+(v>=100?'#70a861':'#b78a32')+';font-size:14px;font-weight:900;color:#5b4a25;'}));measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-flex;flex-wrap:wrap;gap:8px;padding:12px;border-radius:14px;border:3px solid #ead7aa;background:#fffdf5;'});}\n      if(modeChoices&&q.isCount){isMeasureViz=true;const cells=[];for(let i=0;i<(Number(q.count)||0);i++)cells.push({style:'width:28px;height:28px;border-radius:50%;background:#f2a03d;border:2px solid #c77b16;'});measureRows.push({label:'',labelStyle:'display:none;',cells:cells,style:'display:inline-grid;grid-template-columns:repeat(5,28px);gap:8px;padding:12px;border-radius:14px;border:3px solid #f0e2c8;background:#fff;'});}\n      if(modeChoices&&q.isTape){isMeasureViz=true;const cells=(q.tapeParts||[]).map(v=>({text:v,style:'min-width:90px;height:44px;display:flex;align-items:center;justify-content:center;background:'+(v==='□'?'#fff4cf':'#dcecff')+';border:3px solid '+(v==='□'?'#d29a23':'#6b9bd1')+';font-size:22px;font-weight:900;color:#4b5d75;'}));measureRows.push({label:'テープ図',labelStyle:'font-size:18px;font-weight:900;color:#6b5e45;',cells:cells,style:'display:inline-flex;gap:0;padding:8px;border-radius:12px;background:#fff;'});}\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;",
            "\n      if(modeChoices&&q.isTable){isMeasureViz=true;for(const row of (q.tableRows||[])){measureRows.push({label:row.label,labelStyle:'font-size:20px;font-weight:900;min-width:90px;color:#5b5040;',cells:[{text:row.value,style:'min-width:72px;height:40px;display:flex;align-items:center;justify-content:center;background:'+(row.value==='□'?'#fff4cf':'#edf5ff')+';border:3px solid '+(row.value==='□'?'#d29a23':'#7ba6d8')+';font-size:21px;font-weight:900;color:#4b5d75;'}],style:'display:inline-flex;'});}}\n      if(modeChoices&&q.isMeasure){isMeasureViz=true;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "\n      const plen=String(q.prompt||'').length;",
            "\n      for(const row of measureRows)for(const cell of (row.cells||[]))if(cell.text===undefined)cell.text='';\n      const plen=String(q.prompt||'').length;",
            StringComparison.Ordinal);

        markup = ReplaceBlock(
            markup,
            "if(q.topic==='add'){isAddViz=true;",
            "\n        if(S.numChoices)",
            "isAddViz=(q.topic==='add'||q.topic==='sub')&&this.topicStage(p,q.topic)<=2;if(isAddViz){const isSub=q.topic==='sub',base=isSub?Number(q.a||0):Number(q.n1||0),delta=isSub?Number(q.b||0):Number(q.n2||0),total=isSub?base:base+delta,frames=Math.max(1,Math.ceil(Math.max(base,total)/10));for(let f=0;f<frames;f++){const cells=[];let fill=0;for(let i=0;i<10;i++){const idx=f*10+i;let st='background:#fff;border:2px dashed #d8c4a0;';if(idx<base){fill++;if(isSub&&idx>=base-delta)st='background:linear-gradient(135deg,#ffdad4 0 42%,#d2503f 44% 56%,#ffdad4 58% 100%);border:2px solid #d2503f;';else st='background:#ff8a3d;border:2px solid #e07d2a;';}else if(!isSub&&idx<total){fill++;st='background:#2aa39a;border:2px solid #178a82;';}cells.push({style:'width:26px;height:26px;border-radius:50%;'+st});}addFrames.push({full:fill===10,cells:cells,boxStyle:'display:inline-grid;grid-template-columns:repeat(5,26px);gap:6px;padding:10px;border-radius:14px;'+(fill===10?'border:3px solid #3aa655;background:#eafaef;':'border:3px solid #f0e2c8;background:#fff;')});}}\n        ");

        markup = ReplaceRequired(markup,
            "<div style=\"font-size:54px; font-weight:900; text-align:center; margin-bottom:6px; white-space:nowrap;\">{{ prompt }}</div>",
            "<div style=\"{{ promptStyle }}\">{{ prompt }}</div>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; justify-content:center; margin:10px 0 12px;\"><div style=\"{{ shapeStyle }}\"></div></div>\n            </sc-if>\n            <sc-if value=\"{{ isMulViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; flex-wrap:wrap; gap:10px; justify-content:center; align-items:center; margin:8px 0 10px; max-width:720px;\">\n                <sc-for list=\"{{ mulGroups }}\" as=\"grp\" hint-placeholder-count=\"6\">\n                  <div style=\"{{ grp.style }}\">\n                    <sc-for list=\"{{ grp.cells }}\" as=\"cell\" hint-placeholder-count=\"8\"><div style=\"{{ cell.style }}\"></div></sc-for>\n                  </div>\n                </sc-for>\n              </div>\n            </sc-if>\n            <sc-if value=\"{{ isMeasureViz }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; flex-direction:column; gap:10px; align-items:flex-start; margin:8px auto 10px; width:max-content;\">\n                <sc-for list=\"{{ measureRows }}\" as=\"mrow\" hint-placeholder-count=\"2\">\n                  <div style=\"display:flex; align-items:center; gap:10px;\">\n                    <div style=\"{{ mrow.labelStyle }}\">{{ mrow.label }}</div>\n                    <div style=\"{{ mrow.style }}\">\n                      <sc-for list=\"{{ mrow.cells }}\" as=\"mcell\" hint-placeholder-count=\"6\"><div style=\"{{ mcell.style }}\"></div></sc-for>\n                    </div>\n                  </div>\n                </sc-for>\n              </div>\n            </sc-if>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"{{ mcell.style }}\"></div>",
            "<div style=\"{{ mcell.style }}\">{{ mcell.text }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "if(q.isKokugo){isKokugo=true;kokuPre=q.pre;kokuWord=q.word;kokuPost=q.post;kokuMean=q.mean;}",
            "if(q.isKokugo){isKokugo=true;kokuPre=q.pre;kokuWord=q.word;kokuPost=q.post;kokuMean=q.mean;kokuInstruction=q.subtype==='kanji-choice'?'ただしい かんじを えらぼう':'したせんの ことばは なんと よむ？';kokuShowMean=this.topicStage(p,'kokugo')<=2;}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"font-size:22px; color:#9a8662; font-weight:700;\">したせんの ことばは なんと よむ？</div>",
            "<div style=\"font-size:22px; color:#9a8662; font-weight:700;\">{{ kokuInstruction }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<div style=\"background:#fdf0e2; border:2px solid #e8c9a6; border-radius:16px; padding:6px 18px; font-size:19px; color:#9a6a2e; margin-bottom:10px;\">いみ：{{ kokuMean }}</div>",
            "<sc-if value=\"{{ kokuShowMean }}\" hint-placeholder-val=\"{{ false }}\"><div style=\"background:#fdf0e2; border:2px solid #e8c9a6; border-radius:16px; padding:6px 18px; font-size:19px; color:#9a6a2e; margin-bottom:10px;\">いみ：{{ kokuMean }}</div></sc-if>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "isAddViz:isAddViz, addFrames:addFrames,\n      isKokugo:isKokugo",
            "isAddViz:isAddViz, addFrames:addFrames, isMulViz:isMulViz, mulGroups:mulGroups, isMeasureViz:isMeasureViz, measureRows:measureRows, isShapeViz:isShapeViz, shapeStyle:shapeStyle, promptStyle:promptStyle,\n      isKokugo:isKokugo",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "kokuPre:kokuPre, kokuWord:kokuWord, kokuPost:kokuPost, kokuMean:kokuMean,",
            "kokuPre:kokuPre, kokuWord:kokuWord, kokuPost:kokuPost, kokuMean:kokuMean, kokuInstruction:kokuInstruction, kokuShowMean:kokuShowMean,",
            StringComparison.Ordinal);

        return markup;
    }

}
