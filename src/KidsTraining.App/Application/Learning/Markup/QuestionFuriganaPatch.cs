namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchQuestionFurigana(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "\n  renderVals(){",
            BuildFuriganaVocabularyScript() + "\n" + BuildQuestionFuriganaScript() + "\n" + BuildLearningNotationScript() + "\n  renderVals(){",
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
  furiganaEntries(){return [
    ['何本','なんぼん'],['何日','なんにち'],
    ['一ねんせい','いちねんせい'],['校てい','こうてい'],['花だん','はなだん'],['国語じてん','こくごじてん'],['ちょう点','ちょうてん'],['テープ図','テープず'],['気もち','きもち'],
    ['外国語','がいこくご'],['外国','がいこく'],['学級','がっきゅう'],['課題','かだい'],['必要','ひつよう'],
    ['共通','きょうつう'],['目標','もくひょう'],['判断','はんだん'],['基準','きじゅん'],['共有','きょうゆう'],['一人','ひとり'],['担当','たんとう'],
    ['往復','おうふく'],['質問','しつもん'],['記録','きろく'],['種類','しゅるい'],
    ['受賞','じゅしょう'],['仲間','なかま'],['落選','らくせん'],
    ['家ぞく','かぞく'],['まい日','まいにち'],['三つ','みっつ'],['会を','かいを'],['問題','もんだい'],['祭り','まつり'],
    ['1人分','ひとりぶん'],['1人','ひとり'],['2人','ふたり'],
    ['直角三角形','ちょっかくさんかくけい'],['二等辺三角形','にとうへんさんかくけい'],['正三角形','せいさんかくけい'],
    ['五十音','ごじゅうおん'],['五十回','ごじゅっかい'],['何時間','なんじかん'],['何人','なんにん'],['何秒','なんびょう'],['何分','なんぷん'],
    ['図書館','としょかん'],['月曜日','げつようび'],['火曜日','かようび'],['水曜日','すいようび'],['木曜日','もくようび'],['金曜日','きんようび'],['土曜日','どようび'],['日曜日','にちようび'],['日本語','にほんご'],['長方形','ちょうほうけい'],['正方形','せいほうけい'],
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
  ].concat(this.kanjiCurriculumEntries().filter(entry=>entry.word&&entry.word!==entry.k).map(entry=>[entry.word,entry.r]),this.furiganaInflections(),this.furiganaVocabulary());}
  furiganaTrie(){if(this._furiganaTrie)return this._furiganaTrie;const root=Object.create(null);for(const entry of this.furiganaEntries()){let node=root;for(const ch of entry[0]){if(!node[ch])node[ch]=Object.create(null);node=node[ch];}node.$=entry;}this._furiganaTrie=root;return root;}
  // Counters need the whole number (四時 is よじ, 一本 is いっぽん).
  // Limit the match length and cache vocabulary once; annotation work stays linear in text length.
  furiganaNumberAt(text,index){const match=text.slice(index,index+32).match(/^([0-9]+|[一二三四五六七八九十百千]+)(週間|時間|年間|年生|か月|ヶ月|人分|円玉|円札|回転|個分|本分|ひき|びき|ぴき|人|分|本|匹|冊|個|回|枚|台|年|月|日|時|秒|乗|面|度|円|つ)/);if(!match)return null;const raw=match[1],counter=['ひき','びき','ぴき'].includes(match[2])?'匹':({円玉:'円',円札:'円',回転:'回',個分:'個',本分:'本'}[match[2]]||match[2]),digits='一二三四五六七八九',units={十:10,百:100,千:1000};let number=0,pending=0;if(/^[0-9]+$/.test(raw))number=Number(raw);else{for(const ch of raw){if(units[ch]){number+=(pending||1)*units[ch];pending=0;}else{if(pending)return null;pending=digits.indexOf(ch)+1;}}number+=pending;}if(number<1||number>9999)return null;
    const digit=['','いち','に','さん','よん','ご','ろく','なな','はち','きゅう'],say=n=>{let r='';const th=Math.floor(n/1000),h=Math.floor(n%1000/100),t=Math.floor(n%100/10),u=n%10;if(th)r+=th===1?'せん':th===3?'さんぜん':th===8?'はっせん':digit[th]+'せん';if(h)r+=h===1?'ひゃく':h===3?'さんびゃく':h===6?'ろっぴゃく':h===8?'はっぴゃく':digit[h]+'ひゃく';if(t)r+=(t===1?'':digit[t])+'じゅう';return r+digit[u];},after=text.slice(index+match[0].length),last=number%10;
    // A denominator uses ぶん. Exclude time-range bins and numbered people/items.
    if(counter==='分'&&/^の(?:[0-9]+|[一二三四五六七八九十百千]+)(?=$|[\s。、！？）」』]|[をにがはのでと])/.test(after.slice(0,32))&&!/[〜～~－-]\s*$/.test(text.slice(Math.max(0,index-64),index)))return [match[0],say(number)+'ぶん'];
    const countedReading=reading=>{const suffix=['分','秒','時','時間','日','年','年間','か月','ヶ月','週間'].includes(counter)?({後:'ご',前:'まえ',半:'はん'}[after[0]]||(counter==='日'&&after[0]==='間'?'かん':'')):'';return [match[0]+(suffix?after[0]:''),reading+suffix];};
    if(raw==='十'&&counter==='分'&&!/^(ほど|間|後|前|た|歩|かか|待)/.test(after)&&!/[時]\s*$/.test(text.slice(Math.max(0,index-64),index)))return null;
    if(counter==='つ'){const native=['','ひとつ','ふたつ','みっつ','よっつ','いつつ','むっつ','ななつ','やっつ','ここのつ'];return number<10?[match[0],native[number]]:null;}
    if(counter==='月'){if(number>12)return null;return [match[0],(number===4?'し':number===7?'しち':number===9?'く':say(number))+'がつ'];}
    if(counter==='日'){const dates={1:'いちにち',2:'ふつか',3:'みっか',4:'よっか',5:'いつか',6:'むいか',7:'なのか',8:'ようか',9:'ここのか',10:'とおか',14:'じゅうよっか',20:'はつか',24:'にじゅうよっか'};const first=number===1&&/[月]\s*$/.test(text.slice(Math.max(0,index-64),index));return countedReading(first?'ついたち':dates[number]||say(number)+'にち');}
    const finalDigit=overrides=>Object.hasOwn(overrides,last)?say(number-last)+overrides[last]:say(number);
    if(counter==='人'||counter==='人分')return [match[0],(number===1?'ひとり':number===2?'ふたり':finalDigit({4:'よ'})+'にん')+(counter==='人分'?'ぶん':'')];
    if(counter==='時'||counter==='時間')return countedReading(finalDigit({4:'よ',7:'しち',9:'く'})+(counter==='時'?'じ':'じかん'));
    if(['年','年間','年生'].includes(counter))return countedReading(finalDigit({4:'よ'})+{年:'ねん',年間:'ねんかん',年生:'ねんせい'}[counter]);
    let reading=say(number),suffix={週間:'しゅうかん',か月:'かげつ',ヶ月:'かげつ',分:'ふん',本:'ほん',匹:'ひき',冊:'さつ',個:'こ',回:'かい',枚:'まい',台:'だい',秒:'びょう',円:'えん',乗:'じょう',面:'めん',度:'ど'}[counter];
    const changes={分:{0:'ぷん',1:'ぷん',3:'ぷん',4:'ぷん',6:'ぷん',8:'ぷん'},本:{0:'ぽん',1:'ぽん',3:'ぼん',6:'ぽん',8:'ぽん'},匹:{0:'ぴき',1:'ぴき',3:'びき',6:'ぴき',8:'ぴき'}};
    if(changes[counter]&&changes[counter][last])suffix=changes[counter][last];
    if(number%1000===0&&counter==='本')suffix='ぼん';
    if(number%1000===0&&counter==='匹')suffix='びき';
    const contracts=['本','匹','個','回','か月','ヶ月'].includes(counter)?[0,1,6,8]:['分','週間','冊'].includes(counter)?[0,1,8]:[];
    if(counter==='分'&&last===6)reading=reading.slice(0,-2)+'ろっ';
    else if(contracts.includes(last)){const ending={0:'じゅう',1:'いち',6:'ろく',8:'はち'}[last];if(reading.endsWith(ending))reading=reading.slice(0,-ending.length)+{0:'じゅっ',1:'いっ',6:'ろっ',8:'はっ'}[last];}
    if(number%100===0&&number%1000!==0&&['本','匹','個','回','分','か月','ヶ月'].includes(counter))reading=reading.replace(/く$/,'っ');
    return suffix?countedReading(reading+suffix+({円玉:'だま',円札:'さつ',回転:'てん',個分:'ぶん',本分:'ぶん'}[match[2]]||'')):null;
  }
  contextualFurigana(surface,reading,text,index){
    const before=text.slice(Math.max(0,index-64),index),after=text.slice(index+surface.length,index+surface.length+24),interrogative=before.endsWith('なん');
    if(surface==='何')return /^\s*(を|が|に|も|から)/.test(after)?'なに':'なん';
    if(interrogative){const counters={本:'ぼん',分:'ぷん',分後:'ぷんご',人:'にん',日:'にち'};if(counters[surface])return counters[surface];}
    // These readings need context beyond okurigana; the vocabulary supplies the rest.
    if(surface==='表'&&/^が出/.test(after)&&/(?:硬貨|コイン)[^。！？\n]*$/.test(before))return 'おもて';
    if(surface==='分か'&&/^(?:\s*[をにがはも、。！？?]|\s*$)/.test(after)&&/(?:いくつ|[0-9一二三四五六七八九十百千]+(?:つ|こ|さら|まい|さつ)|(?:何|なん)こ)\s*$/.test(before))return 'ぶんか';
    if(surface==='行っ')return /(?:実験|観察|調査|計算|測定|練習|活動|学習|作業|話し合い)を *$/.test(before)?'おこなっ':'いっ';
    if(surface==='下'&&/(?:監督|監督と消火準備|指示|指導|管理)の\s*$/.test(before))return 'もと';
    if(surface==='方'&&/(?:大きい|小さい|長い|短い|多い|少ない|近い|遠い|高い|低い|早い|遅い|速い|重い|軽い|広い|狭い)\s*$/.test(before))return 'ほう';
    if(surface==='間'&&/(?:速さや|話すときの)\s*$/.test(before))return 'ま';
    if(surface==='角'&&/四つの\s*$/.test(before))return 'かど';
    if(surface==='母')return before.endsWith('お')?'かあ':'はは';
    if(surface==='父')return before.endsWith('お')?'とう':'ちち';
    if(surface==='数'&&/(?:ページ|点|画素|文字) *$/.test(before))return 'すう';
    if(/^[月火水木金土日]$/.test(surface)&&(/^→[月火水木金土日]/.test(after)||/[月火水木金土日]→$/.test(before)))return {月:'げつ',火:'か',水:'すい',木:'もく',金:'きん',土:'ど',日:'にち'}[surface];
    return reading;
  }
  // Assessment feedback already has an authoritative reading. Limit it to the
  // quoted target or the answer field; do not change ordinary prose or the trie.
  assessmentFurigana(q){if(!q)return null;const target=q.readingTarget||(q.topic==='kokugo'&&q.subtype==='reading'?q.word:null);if(target&&typeof q.answer==='string')return{surface:target,reading:q.answer};if(q.topic==='kokugo'&&q.subtype==='kanji-choice'&&typeof q.word==='string')return{surface:q.answer,reading:q.word};return null;}
  questionRich(q,field,fallback){const display=q&&q.display,value=display&&typeof display==='object'&&!Array.isArray(display)?display[field]:(field==='prompt'?display:undefined),assessment=this.assessmentFurigana(q),protectedWords=field==='prompt'&&assessment?[assessment.surface]:[],reading=assessment&&(field==='answer'||field==='explanation')?{...assessment,allowBare:field==='answer'}:null;return typeof value==='string'&&value.trim()?this.withRichText(value,false,protectedWords,reading):this.withFurigana(fallback,false,protectedWords,reading);}
  kanjiTargetChoices(q){return !!q&&q.topic==='kokugo'&&(q.subtype==='kanji-choice'||q.subtype==='kanji-picture');}
  questionChoiceRich(q,index,fallback,skipFurigana){const display=q&&q.display,choices=display&&typeof display==='object'&&!Array.isArray(display)?display.choices:undefined,value=Array.isArray(choices)?choices[index]:undefined;return typeof value==='string'&&value.trim()?this.withRichInline(value,skipFurigana):this.withFurigana(fallback,skipFurigana);}
  withFurigana(value,skip,protectedWords=[],assessmentReading=null){if(value===null||value===undefined)return '';if(skip||Array.isArray(value)||React.isValidElement(value))return value;const text=String(value);if(!/[一-龯々]/.test(text))return this.withLearningNotation(text);const protectedAt=new Map();for(const word of protectedWords){if(!word)continue;for(let at=text.indexOf(word);at>=0;at=text.indexOf(word,at+word.length)){if(!protectedAt.has(at)||protectedAt.get(at).length<word.length)protectedAt.set(at,word);}}const trie=this.furiganaTrie(),out=[],kanji=ch=>!!ch&&/[一-龯々]/.test(ch);let plain='',i=0;const flush=()=>{if(plain){out.push(this.withLearningNotation(plain));plain='';}};
    while(i<text.length){const protectedWord=protectedAt.get(i);if(protectedWord){plain+=protectedWord;i+=protectedWord.length;continue;}
      let node=trie,j=i,best=null;while(j<text.length&&node[text[j]]){node=node[text[j]];j++;if(node.$)best=node.$;}
      const counted=this.furiganaNumberAt(text,i);if(counted&&(!best||counted[0].length>=best[0].length))best=counted;
      const assessed=assessmentReading&&assessmentReading.surface&&text.startsWith(assessmentReading.surface,i)&&((assessmentReading.allowBare&&text===assessmentReading.surface)||(text[i-1]==='「'&&text[i+assessmentReading.surface.length]==='」')||(text[i-1]==='『'&&text[i+assessmentReading.surface.length]==='』'));if(assessed)best=[assessmentReading.surface,assessmentReading.reading];
      // A character's school reading is not a reading of an unknown compound.
      // Known words can still be annotated inside longer text, but isolated fallback
      // characters next to another kanji remain plain instead of fabricating a reading.
      if(best&&!assessed&&best[0].length===1&&(kanji(text[i-1])||kanji(text[i+1])))best=null;
      if(best&&protectedAt.size){for(let offset=1;offset<best[0].length;offset++){if(protectedAt.has(i+offset)){best=null;break;}}}
      if(!best){plain+=text[i];i++;continue;}flush();const surface=best[0],reading=assessed?best[1]:this.contextualFurigana(surface,best[1],text,i),key='ruby-'+i+'-'+out.length,ruby=React.createElement('ruby',{key:key,style:{rubyPosition:'over'}},surface,React.createElement('rt',{'aria-hidden':true,style:{fontSize:'.46em',fontWeight:700,lineHeight:1}},reading));out.push(ruby,React.createElement('wbr',{key:'break-'+i+'-'+out.length}));i+=surface.length;
    }flush();return out;}
""";
    }
}
