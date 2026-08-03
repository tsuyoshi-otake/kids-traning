namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    // Picture questions are generated at runtime so they follow the same grade/stage
    // progression as the existing Japanese drills. The picture itself is built from a
    // fixed SVG vocabulary below; no user supplied markup is ever inserted into the DOM.
    private static string PatchKanjiPictureQuestions(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "pickKokugo(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kokugo');const L=",
            "pickKokugo(p){const g=this.effectiveGrade(p),stage=this.topicStage(p,'kokugo');" + BuildKanjiPictureQuestionScript() + "const L=",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "if(q.isKokugo){isKokugo=true;",
            "if(q.isKokugo&&q.subtype!=='kanji-picture'){isKokugo=true;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            BuildKanjiPictureSvgScript() + "\n  renderVals(){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "writtenError='';",
            "writtenError='',isKanjiPicture=false,kanjiPicture=null;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibIsKokugo=false,calibIsPlain=true,calibPrompt='',calibEq='',calibKokuPre='',calibKokuWord='',calibKokuPost='';",
            "calibIsKokugo=false,calibIsPlain=true,calibIsKanjiPicture=false,calibPicture=null,calibPrompt='',calibEq='',calibKokuPre='',calibKokuWord='',calibKokuPost='';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibIsKokugo=!!cq.isKokugo;calibIsPlain=!calibIsKokugo;if(calibIsKokugo){calibKokuPre=cq.pre;calibKokuWord=cq.word;calibKokuPost=cq.post;}else{calibPrompt=this.questionRich(cq,'prompt',cq.prompt);}calibEq=cq.mode==='num'&&!/[=?]/.test(String(cq.prompt||''))?' = ?':'';",
            "calibIsKanjiPicture=!!(cq.subtype==='kanji-picture'&&cq.pictureKind==='svg');calibIsKokugo=!!cq.isKokugo&&!calibIsKanjiPicture;calibIsPlain=!calibIsKokugo&&!calibIsKanjiPicture;calibPicture=calibIsKanjiPicture?this.kanjiPictureSvg(cq.pictureId,cq.pictureLabel||'かんじの え'):null;if(calibIsKokugo){calibKokuPre=cq.pre;calibKokuWord=cq.word;calibKokuPost=cq.post;}else{calibPrompt=this.questionRich(cq,'prompt',cq.prompt);}calibEq=cq.mode==='num'&&!/[=?]/.test(String(cq.prompt||''))?' = ?':'';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "if(modeChoices&&q.isShape){isShapeViz=true;shapeStyle=q.shapeStyle||'';}",
            "if(modeChoices&&q.isShape){isShapeViz=true;shapeStyle=q.shapeStyle||'';}\n      if(modeChoices&&q.subtype==='kanji-picture'&&q.pictureKind==='svg'){isKanjiPicture=true;kanjiPicture=this.kanjiPictureSvg(q.pictureId,q.pictureLabel||'かんじの え');}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div class=\"kt-question-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}</div>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">",
            "<sc-if value=\"{{ isKanjiPicture }}\" hint-placeholder-val=\"{{ false }}\">\n              <div style=\"display:flex; justify-content:center; margin:8px auto 12px; width:100%; max-width:100%; box-sizing:border-box;\">{{ kanjiPicture }}</div>\n            </sc-if>\n            <div class=\"kt-question-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}</div>\n            <sc-if value=\"{{ isShapeViz }}\" hint-placeholder-val=\"{{ false }}\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "shapeStyle:shapeStyle, promptStyle:promptStyle,\n      isKokugo",
            "shapeStyle:shapeStyle, promptStyle:promptStyle, isKanjiPicture:isKanjiPicture, kanjiPicture:kanjiPicture,\n      isKokugo",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibIsKokugo:calibIsKokugo, calibIsPlain:calibIsPlain, calibPrompt:calibPrompt,",
            "calibIsKokugo:calibIsKokugo, calibIsPlain:calibIsPlain, calibIsKanjiPicture:calibIsKanjiPicture, calibPicture:calibPicture, calibPrompt:calibPrompt,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<sc-if value=\"{{ calibIsKokugo }}\" hint-placeholder-val=\"{{ false }}\">",
            "<sc-if value=\"{{ calibIsKanjiPicture }}\" hint-placeholder-val=\"{{ false }}\">\n            <div style=\"display:flex; justify-content:center; margin:8px auto 14px; width:100%;\">{{ calibPicture }}</div>\n          </sc-if>\n          <sc-if value=\"{{ calibIsKokugo }}\" hint-placeholder-val=\"{{ false }}\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "'kind','mode','category','op','a','b','multiplicationFactKey','memoryAssessment','writtenArithmetic'];",
            "'kind','mode','category','op','a','b','multiplicationFactKey','memoryAssessment','writtenArithmetic','subtype','pictureKind','pictureId','pictureLabel'];",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildKanjiPictureQuestionScript()
    {
        return """
if(g<=3&&stage>=1&&Math.random()<0.2){
  const P=[
    {id:'mountain',g:1,k:'山',label:'山の え',ex:'山が えがかれています。'},
    {id:'river',g:1,k:'川',label:'川の え',ex:'川が ながれています。'},
    {id:'tree',g:1,k:'木',label:'木の え',ex:'木が たっています。'},
    {id:'flower',g:1,k:'花',label:'花の え',ex:'花が さいています。'},
    {id:'rain',g:1,k:'雨',label:'雨の え',ex:'雨が ふっています。'},
    {id:'fire',g:1,k:'火',label:'火の え',ex:'火が もえています。'},
    {id:'dog',g:1,k:'犬',label:'犬の え',ex:'犬が います。'},
    {id:'car',g:1,k:'車',label:'車の え',ex:'車が はしっています。'},
    {id:'book',g:1,k:'本',label:'本の え',ex:'本が ひらいてあります。'},
    {id:'fish',g:2,k:'魚',label:'魚の え',ex:'魚が およいでいます。'}
  ];
  const picturePool=P.filter(x=>x.g<=g),it=picturePool[this.rand(0,picturePool.length-1)],distractors=this.shuffle(picturePool.filter(x=>x.k!==it.k)).slice(0,3).map(x=>x.k);
  return{topic:'kokugo',mode:'choices',isKokugo:true,subtype:'kanji-picture',pictureKind:'svg',pictureId:it.id,pictureLabel:it.label,prompt:'この えに あう かんじは？',answer:it.k,choices:this.pick4(it.k,distractors),explanation:it.ex+' こたえは「'+it.k+'」です。'};
}
""";
    }

    private static string BuildKanjiPictureSvgScript()
    {
        return """
  kanjiPictureSvg(id,label){const e=React.createElement,n=(tag,props,...children)=>e(tag,props,...children),c={fill:'none',stroke:'#3a3326',strokeWidth:5,strokeLinecap:'round',strokeLinejoin:'round'},b={viewBox:'0 0 240 160',role:'img','aria-label':String(label||'かんじの え'),focusable:'false',style:{width:'min(240px,72vw)',height:'auto',maxWidth:'100%',display:'block',background:'#fffdf5',border:'3px solid #ead7aa',borderRadius:'18px',padding:'8px',boxSizing:'border-box'}};let s=[];switch(String(id||'')){case'mountain':s=[n('circle',{key:'sun',cx:190,cy:35,r:18,fill:'#f5bd45',stroke:'none'}),n('path',{key:'mountains',d:'M18 132L86 48l35 45 21-37 80 76z',...c,fill:'#78b58a'}),n('path',{key:'snow',d:'M76 60l10-12 12 15M136 66l6-10 11 14',...c,stroke:'#fffdf5'})];break;case'river':s=[n('path',{key:'bank',d:'M25 26c40 25 48 62 25 108M215 26c-40 25-48 62-25 108',...c,stroke:'#9a6b3f'}),n('path',{key:'water',d:'M56 18c50 22 82 32 128 10M58 57c42 20 77 28 124 8M54 98c44 19 80 27 130 8M52 134c48 15 84 19 137 4',...c,stroke:'#4f9ed1',strokeWidth:7})];break;case'tree':s=[n('rect',{key:'trunk',x:105,y:90,width:30,height:48,rx:8,fill:'#a86d3c',...c}),n('circle',{key:'c1',cx:93,cy:66,r:32,fill:'#78b56f',...c}),n('circle',{key:'c2',cx:140,cy:65,r:32,fill:'#8bc978',...c}),n('circle',{key:'c3',cx:117,cy:43,r:33,fill:'#6eae65',...c})];break;case'flower':s=[n('path',{key:'stem',d:'M120 132V67M120 104c-25-16-38-4-44 8 18 2 31-1 44-8',...c,stroke:'#4f9d61'}),n('circle',{key:'center',cx:120,cy:48,r:17,fill:'#f5bd45',...c}),n('circle',{key:'p1',cx:120,cy:23,r:18,fill:'#ee8b82',...c}),n('circle',{key:'p2',cx:94,cy:43,r:18,fill:'#ee8b82',...c}),n('circle',{key:'p3',cx:146,cy:43,r:18,fill:'#ee8b82',...c})];break;case'rain':s=[n('path',{key:'cloud',d:'M48 78c-6-28 17-49 42-42 16-30 66-25 75 10 28-6 50 11 47 36-2 20-19 29-39 29H78c-18 0-28-13-30-33',...c,fill:'#dcecff'}),n('path',{key:'drops',d:'M83 113l-8 22M119 113l-8 22M155 113l-8 22',...c,stroke:'#4f9ed1'})];break;case'fire':s=[n('path',{key:'outer',d:'M120 143c-37 0-57-22-48-53 5-17 19-27 34-45 6 16 18 22 17 38 13-11 18-27 15-43 30 22 42 51 29 77-8 17-24 26-47 26',...c,fill:'#f28d4b',stroke:'#b95c30'}),n('path',{key:'inner',d:'M120 127c-18 0-27-12-22-27 3-9 11-14 18-24 4 9 9 14 8 23 6-5 9-11 8-19 14 13 17 29 10 39-5 6-12 8-22 8',...c,fill:'#f8d35b',stroke:'#c88928'})];break;case'dog':s=[n('ellipse',{key:'body',cx:115,cy:101,rx:58,ry:29,fill:'#d99b62',...c}),n('circle',{key:'head',cx:65,cy:76,r:31,fill:'#d99b62',...c}),n('path',{key:'ears',d:'M48 52L39 26l25 17M78 51l15-23-2 31',...c,fill:'#b97846'}),n('path',{key:'tail',d:'M169 94c31-18 39-3 30 14-7 13-18 13-28 6M84 122v18M124 123v17M151 119v19',...c,stroke:'#b97846'}),n('circle',{key:'eye',cx:74,cy:69,r:4,fill:'#3a3326'}),n('circle',{key:'nose',cx:41,cy:81,r:5,fill:'#3a3326'})];break;case'car':s=[n('path',{key:'body',d:'M31 111h178c5 0 8-5 5-10l-15-28c-2-5-7-8-12-8H88c-7 0-13 3-17 9L48 95H31c-8 0-11 16 0 16',...c,fill:'#e67b58'}),n('path',{key:'window',d:'M91 69h38l21 27H75l16-27M137 69h42l10 27h-31l-21-27',...c,fill:'#dcecff'}),n('circle',{key:'w1',cx:72,cy:116,r:18,fill:'#3a3326'}),n('circle',{key:'w2',cx:174,cy:116,r:18,fill:'#3a3326'}),n('circle',{key:'h1',cx:72,cy:116,r:7,fill:'#fffdf5'}),n('circle',{key:'h2',cx:174,cy:116,r:7,fill:'#fffdf5'})];break;case'book':s=[n('path',{key:'left',d:'M30 43c31-10 61-4 90 12v84c-29-16-59-22-90-12z',...c,fill:'#dcecff'}),n('path',{key:'right',d:'M210 43c-31-10-61-4-90 12v84c29-16 59-22 90 12z',...c,fill:'#f6d7de'}),n('path',{key:'spine',d:'M120 55v84',...c}),n('path',{key:'lines',d:'M49 67c20-4 40 0 57 9M49 86c20-4 40 0 57 9M191 67c-20-4-40 0-57 9M191 86c-20-4-40 0-57 9',...c,stroke:'#6889b7',strokeWidth:3})];break;case'fish':s=[n('ellipse',{key:'body',cx:118,cy:83,rx:58,ry:35,fill:'#f2a15b',...c}),n('polygon',{key:'tail',points:'62,83 25,51 25,115',...c,fill:'#f5bd45'}),n('path',{key:'fin',d:'M117 48c8-19 25-21 31-18-1 17-10 28-25 35M117 118c8 18 25 20 31 16-1-15-10-25-25-31',...c,fill:'#ee8b82'}),n('circle',{key:'eye',cx:150,cy:76,r:6,fill:'#3a3326'}),n('circle',{key:'shine',cx:152,cy:74,r:2,fill:'#fffdf5'}),n('path',{key:'bubbles',d:'M185 42c0-8 12-8 12 0s-12 8-12 0M205 25c0-5 8-5 8 0s-8 5-8 0',...c,stroke:'#4f9ed1',strokeWidth:3})];break;default:s=[n('circle',{key:'fallback',cx:120,cy:80,r:48,fill:'#f0e2c8',...c})];}return e('svg',b,...s);}
""";
    }
}
