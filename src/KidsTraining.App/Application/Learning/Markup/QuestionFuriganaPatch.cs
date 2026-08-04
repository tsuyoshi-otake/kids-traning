namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchQuestionFurigana(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            BuildQuestionFuriganaScript() + "\n" + BuildLearningNotationScript() + "\n  renderVals(){",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "</head>",
            BuildLearningNotationStyles() + "\n</head>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibChoices=it.choices.map(c=>({text:c,style:choiceTile,onClick:()=>this.calibAnswer(c)}));",
            "calibChoices=it.choices.map((c,index)=>{const skipFurigana=this.kanjiTargetChoices(cq);return{text:this.questionChoiceRich(cq,index,c,skipFurigana),style:choiceTile,onClick:()=>this.calibAnswer(c)};});",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "else{calibPrompt=cq.prompt;}}",
            "else{calibPrompt=this.questionRich(cq,'prompt',cq.prompt);}}",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "calibKokuPre:calibKokuPre, calibKokuWord:calibKokuWord, calibKokuPost:calibKokuPost,",
            "calibKokuPre:this.withFurigana(calibKokuPre), calibKokuWord:calibKokuWord, calibKokuPost:this.withFurigana(calibKokuPost),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "prompt:q?q.prompt:'', choices:choices,",
            "prompt:q?this.questionRich(q,'prompt',q.prompt):'', choices:choices,",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "hsStepLabel:hsStepLabel, hsStepPrompt:hsStepPrompt, hsPad:hsPad, hsHasHint:!!S.hsHint, hsHint:S.hsHint,",
            "hsStepLabel:hsStepLabel, hsStepPrompt:this.withFurigana(hsStepPrompt), hsPad:hsPad, hsHasHint:!!S.hsHint, hsHint:this.withFurigana(S.hsHint),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "kokuPre:kokuPre, kokuWord:kokuWord, kokuPost:kokuPost, kokuMean:kokuMean,",
            "kokuPre:this.withFurigana(kokuPre), kokuWord:kokuWord, kokuPost:this.withFurigana(kokuPost), kokuMean:this.withFurigana(kokuMean),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:fb.prompt||'', fbAnswer:fb.answer||'', fbExplanation:fb.explanation||'',",
            "fbCorrect:!!lr.correct, fbWrong:lr.correct===false, fbPrompt:this.questionRich(fb,'prompt',fb.prompt||''), fbAnswer:this.questionRich(fb,'answer',fb.answer||''), fbExplanation:this.questionRich(fb,'explanation',fb.explanation||''),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "hasPracticePrompt:!!practicePrompt, practicePrompt:practicePrompt,",
            "hasPracticePrompt:!!practicePrompt, practicePrompt:this.withFurigana(practicePrompt),",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "topicLabel=t.label;",
            "practicePrompt=this.questionRich(q,'activityPrompt',practicePrompt);topicLabel=t.label;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "for(const row of measureRows)for(const cell of (row.cells||[]))if(cell.text===undefined)cell.text='';",
            "for(const row of measureRows){row.label=this.withFurigana(row.label);for(const cell of (row.cells||[])){if(cell.text===undefined)cell.text='';else cell.text=this.withFurigana(cell.text);}}",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildQuestionFuriganaScript()
    {
        return """
  furiganaEntries(){const curriculum=this.kanjiCurriculumEntries().map(entry=>[entry.k,entry.stem||entry.r]);return curriculum.concat([
    ['何本','なんぼん'],['何日','なんにち'],
    ['一ねんせい','いちねんせい'],['校てい','こうてい'],['花だん','はなだん'],['国語じてん','こくごじてん'],['ちょう点','ちょうてん'],['テープ図','テープず'],['気もち','きもち'],
    ['外国語','がいこくご'],['外国','がいこく'],['学級','がっきゅう'],['課題','かだい'],['必要','ひつよう'],
    ['共通','きょうつう'],['目標','もくひょう'],['判断','はんだん'],['基準','きじゅん'],['共有','きょうゆう'],['一人','ひとり'],['担当','たんとう'],
    ['往復','おうふく'],['質問','しつもん'],['記録','きろく'],['種類','しゅるい'],
    ['家ぞく','かぞく'],['まい日','まいにち'],['三つ','みっつ'],['会を','かいを'],['問題','もんだい'],['祭り','まつり'],
    ['1人分','ひとりぶん'],['1人','ひとり'],['2人','ふたり'],
    ['直角三角形','ちょっかくさんかくけい'],['二等辺三角形','にとうへんさんかくけい'],['正三角形','せいさんかくけい'],
    ['五十音','ごじゅうおん'],['五十回','ごじゅっかい'],['何時間','なんじかん'],['何人','なんにん'],['何秒','なんびょう'],['何分','なんぷん'],
    ['図書館','としょかん'],['日曜日','にちようび'],['日本語','にほんご'],['長方形','ちょうほうけい'],['正方形','せいほうけい'],
    ['四角形','しかくけい'],['三角形','さんかくけい'],
    ['円周','えんしゅう'],['円玉','えんだま'],['円札','えんさつ'],['家族','かぞく'],['学校','がっこう'],['漢字','かんじ'],
    ['研究','けんきゅう'],['空気','くうき'],['元気','げんき'],['行事','ぎょうじ'],['国語','こくご'],['三角','さんかく'],
    ['三年','さんねん'],['写真','しゃしん'],['宿題','しゅくだい'],['出会','であ'],['出歩','である'],['小数','しょうすう'],
    ['神社','じんじゃ'],['人数','にんずう'],['人分','にんぶん'],['世界','せかい'],['太陽','たいよう'],['中心','ちゅうしん'],
    ['昼休','ひるやす'],['直角','ちょっかく'],['直径','ちょっけい'],['直線','ちょくせん'],['電車','でんしゃ'],['土地','とち'],
    ['動物','どうぶつ'],['日中','にっちゅう'],['年下','としした'],['年上','としうえ'],['半円','はんえん'],['半径','はんけい'],
    ['半分','はんぶん'],['病院','びょういん'],['分後','ふんご'],['文字','もじ'],['勉強','べんきょう'],['毎週','まいしゅう'],
    ['名前','なまえ'],['野原','のはら'],['洋服','ようふく'],['旅行','りょこう'],['練習','れんしゅう'],['一日','いちにち'],
    ['一万','いちまん'],['九時','くじ'],['六時','ろくじ'],['大人','おとな'],['子供','こども'],['全部','ぜんぶ'],
    ['切手','きって'],['封筒','ふうとう'],
    ['医者','いしゃ'],['運動','うんどう'],['英語','えいご'],['荷物','にもつ'],['角','かく'],['時間','じかん'],
    ['一','いち'],['雨','あめ'],['雲','くも'],['泳','およ'],['駅','えき'],['円','えん'],['遠','とお'],['億','おく'],['音','おと'],
    ['下','した'],['何','なん'],['夏','なつ'],['家','いえ'],['花','はな'],['会','あ'],['回','かい'],['海','うみ'],['貝','かい'],
    ['学','まな'],['楽','たの'],['間','あいだ'],['岩','いわ'],['顔','かお'],['帰','かえ'],['気','き'],['休','やす'],['急','いそ'],
    ['球','きゅう'],['魚','さかな'],['強','つよ'],['教','おし'],['橋','はし'],['近','ちか'],['空','そら'],['兄','あに'],['形','かたち'],
    ['月','つき'],['犬','いぬ'],['見','み'],['湖','みずうみ'],['口','くち'],['校','こう'],['港','みなと'],['考','かんが'],['行','い'],
    ['高','たか'],['合','あ'],['国','くに'],['祭','まつり'],['坂','さか'],['作','つく'],['三','さん'],['山','やま'],['算','ざん'],
    ['始','はじ'],['姉','あね'],['思','おも'],['糸','いと'],['紙','かみ'],['字','じ'],['時','じ'],['耳','みみ'],['車','くるま'],
    ['手','て'],['秋','あき'],['終','お'],['十','じゅう'],['重','おも'],['出','で'],['春','はる'],['書','か'],['女','おんな'],
    ['小','ちい'],['少','すく'],['上','うえ'],['色','いろ'],['食','た'],['新','あたら'],['森','もり'],['深','ふか'],['人','ひと'],
    ['図','ず'],['水','みず'],['数','かず'],['星','ほし'],['生','せい'],['声','こえ'],['昔','むかし'],['石','いし'],['先生','せんせい'],
    ['千','せん'],['川','かわ'],['前','まえ'],['早','はや'],['草','くさ'],['走','はし'],['足','あし'],['速','はや'],['多','おお'],
    ['体','からだ'],['大','おお'],['短','みじか'],['男','おとこ'],['知','し'],['池','いけ'],['竹','たけ'],['中','なか'],['昼','ひる'],
    ['柱','はしら'],['虫','むし'],['朝','あさ'],['町','まち'],['長','なが'],['鳥','とり'],['庭','にわ'],['弟','おとうと'],['点','てん'],
    ['土','つち'],['冬','ふゆ'],['島','しま'],['当','あ'],['頭','あたま'],['同','おな'],['読','よ'],['二','に'],['日','ひ'],
    ['馬','うま'],['白','しろ'],['悲','かな'],['百','ひゃく'],['表','ひょう'],['秒','びょう'],['風','かぜ'],['分','ぶん'],['聞','き'],
    ['歩','ある'],['母','かあ'],['方','かた'],['本','ほん'],['妹','いもうと'],['万','まん'],['名','な'],['明','あか'],['面','めん'],
    ['木','き'],['目','め'],['夜','よる'],['役','やく'],['薬','くすり'],['友','とも'],['葉','は'],['落','お'],['立','た'],
    ['力','ちから'],['緑','みどり'],['話','はなし'],['枚','まい'],['左','ひだり'],['順','じゅん'],['答','こた'],
    ['七','なな'],['丸','まる'],['交','まじ'],['京','きょう'],['今','いま'],['仕','し'],['使','つか'],['係','かかり'],['光','ひかり'],['入','はい'],
    ['八','やっ'],['公','こう'],['具','ぐ'],['冷','ひ'],['区','く'],['午','ご'],['去','さ'],['取','と'],['古','ふる'],['右','みぎ'],
    ['号','ごう'],['君','きみ'],['味','み'],['品','ひん'],['員','いん'],['園','えん'],['場','ば'],['夕','ゆう'],['外','そと'],['天','てん'],
    ['央','おう'],['委','い'],['安','あん'],['客','きゃく'],['宮','みや'],['寒','さむ'],['寺','てら'],['屋','や'],['岸','きし'],['工','こう'],
    ['市','し'],['幸','しあわ'],['広','ひろ'],['庫','こ'],['引','ひ'],['意','い'],['感','かん'],['戸','と'],['所','ところ'],['支','ささ'],
    ['暗','くら'],['曲','きょく'],['期','き'],['村','むら'],['来','く'],['東','とう'],['林','はやし'],['根','ね'],['植','しょく'],['業','ぎょう'],
    ['横','よこ'],['歌','うた'],['止','と'],['段','だん'],['決','き'],['波','なみ'],['温','あたた'],['火','ひ'],['牛','うし'],['王','おう'],
    ['田','た'],['番','ばん'],['的','てき'],['県','けん'],['科','か'],['級','きゅう'],['細','ほそ'],['絵','え'],['羽','はね'],['育','そだ'],
    ['自','じ'],['苦','くる'],['菜','さい'],['血','ち'],['言','い'],['計','はか'],['記','しる'],['谷','たに'],['赤','あか'],['起','お'],
    ['転','てん'],['軽','かる'],['農','のう'],['道','みち'],['金','かね'],['銀','ぎん'],['開','ひら'],['階','かい'],['青','あお'],['飲','の'],
    ['養','よう'],['黄','き'],['黒','くろ']
  ]);}
  furiganaTrie(){if(this._furiganaTrie)return this._furiganaTrie;const root=Object.create(null);for(const entry of this.furiganaEntries()){let node=root;for(const ch of entry[0]){if(!node[ch])node[ch]=Object.create(null);node=node[ch];}node.$=entry;}this._furiganaTrie=root;return root;}
  contextualFurigana(surface,reading,text,index){const before=text.slice(0,index),after=text.slice(index+surface.length),number=(before.match(/(\d+)$/)||[])[1],interrogative=before.endsWith('なん');if(surface==='何')return /^[をがに]/.test(after)?'なに':'なん';if(surface==='本'&&interrogative)return 'ぼん';if(surface==='分'&&interrogative)return 'ぷん';if(surface==='分後'&&interrogative)return 'ぷんご';if(surface==='人'&&interrogative)return 'にん';if(surface==='日'&&interrogative)return 'にち';if(surface==='人'){if(number==='1')return 'ひとり';if(number==='2')return 'ふたり';return number?'にん':'ひと';}if(surface==='人分'&&number==='1')return 'ひとりぶん';if(surface==='日')return number?'にち':'ひ';if(surface==='数'&&after.startsWith('え'))return 'かぞ';if(surface==='話'&&after.startsWith('す'))return 'はな';if(surface==='残'&&after.startsWith('さ'))return 'のこ';if(surface==='生'){if(after.startsWith('ま'))return 'う';if(after.startsWith('き'))return 'い';}if(surface==='分'&&after.startsWith('け'))return 'わ';if((surface==='分'||surface==='分後')&&number){const last=Number(number.slice(-1)),pun=last===0||last===1||last===3||last===4||last===6||last===8;return (pun?'ぷん':'ふん')+(surface==='分後'?'ご':'');}if(surface==='本'&&number){const last=Number(number.slice(-1));return last===3?'ぼん':(last===0||last===1||last===6||last===8?'ぽん':'ほん');}return reading;}
  questionRich(q,field,fallback){const display=q&&q.display;const value=display&&typeof display==='object'&&!Array.isArray(display)?display[field]:(field==='prompt'?display:undefined);return typeof value==='string'&&value.trim()?this.withRichText(value):this.withFurigana(fallback);}
  // A drill that asks which kanji writes a reading is answered by its own furigana, so the
  // choices of those subtypes stay bare. The skip has to reach the single withFurigana call
  // below: annotating an already-annotated fallback is a no-op only because withFurigana
  // returns arrays untouched, and a deliberately bare string is not an array.
  kanjiTargetChoices(q){return !!q&&q.topic==='kokugo'&&(q.subtype==='kanji-choice'||q.subtype==='kanji-picture');}
  questionChoiceRich(q,index,fallback,skipFurigana){const display=q&&q.display;const choices=display&&typeof display==='object'&&!Array.isArray(display)?display.choices:undefined;const value=Array.isArray(choices)?choices[index]:undefined;return typeof value==='string'&&value.trim()?this.withRichInline(value):this.withFurigana(fallback,skipFurigana);}
  withFurigana(value,skip){if(value===null||value===undefined)return '';if(skip||Array.isArray(value)||React.isValidElement(value))return value;const text=String(value);if(!/[一-龯々]/.test(text))return this.withLearningNotation(text);const trie=this.furiganaTrie(),out=[];let plain='',i=0;const flush=()=>{if(plain){out.push(this.withLearningNotation(plain));plain='';}};while(i<text.length){let node=trie,j=i,best=null;while(j<text.length&&node[text[j]]){node=node[text[j]];j++;if(node.$)best=node.$;}if(!best){plain+=text[i];i++;continue;}flush();const surface=best[0],reading=this.contextualFurigana(surface,best[1],text,i),key='ruby-'+i+'-'+out.length,ruby=React.createElement('ruby',{key:key,style:{rubyPosition:'over'}},surface,React.createElement('rt',{'aria-hidden':true,style:{fontSize:'.46em',fontWeight:700,lineHeight:1}},reading));out.push(ruby,React.createElement('wbr',{key:'break-'+i+'-'+out.length}));i+=surface.length;}flush();return out;}
""";
    }
}
