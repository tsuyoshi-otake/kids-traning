// Issue #81: expected Japanese readings and assessment semantics, independently of
// the production vocabulary. Run against the assembled runtime via GeneratedQuestionAudit.
import { furiganaContextFixtures } from './FuriganaContextFixtures.mjs';
import { furiganaGradeSixFixtures } from './FuriganaGradeSixFixtures.mjs';
export function auditQuestionQuality(app, units, profileFor, observe, violated) {
  const check = (name, ok, detail) => {
    observe(name);
    if (!ok) violated(name, detail, detail);
  };
  const nodes = (value) => Array.isArray(value) ? value.flatMap(nodes)
    : value && typeof value === 'object' ? [value, ...(value.children || []).flatMap(nodes)] : [];
  const visible = (value) => Array.isArray(value) ? value.map(visible).join('')
    : value && typeof value === 'object' ? value.type === 'rt' ? '' : visible(value.children || []) : String(value ?? '');
  const spoken = (value) => Array.isArray(value) ? value.map(spoken).join('')
    : value && typeof value === 'object' ? value.type === 'ruby'
      ? spoken(value.children.find(child => child?.type === 'rt')) : spoken(value.children || []) : String(value ?? '');
  const readings = (value) => nodes(value).filter(node => node.type === 'ruby');
  // Explicitly reviewed vocabulary has priority over mechanically expanded stems.
  // This is a precedence contract, separate from independent reading fixtures.
  const finalVocabulary = new Map(app.furiganaEntries());
  for (const [surface, reading] of app.furiganaVocabulary()) {
    check('reviewed vocabulary overrides inflection expansion', finalVocabulary.get(surface) === reading, surface);
  }
  const expected = [
    ['算数', 'さんすう'], ['今日', 'きょう'], ['自分', 'じぶん'], ['図工', 'ずこう'], ['図書室', 'としょしつ'],
    ['時計', 'とけい'], ['教室', 'きょうしつ'], ['毎日', 'まいにち'], ['公園', 'こうえん'], ['色紙', 'いろがみ'],
    ['二人', 'ふたり'], ['三人', 'さんにん'], ['四人', 'よにん'], ['一週間', 'いっしゅうかん'],
    ['三十分', 'さんじゅっぷん'], ['十分ほど', 'じゅっぷんほど'], ['十分に', 'じゅうぶんに'],
    ['四時', 'よじ'], ['七時', 'しちじ'], ['九時', 'くじ'], ['4月', 'しがつ'], ['7月', 'しちがつ'], ['9月', 'くがつ'],
    ['一本', 'いっぽん'], ['三本', 'さんぼん'], ['六本', 'ろっぽん'], ['8本', 'はっぽん'], ['10本', 'じゅっぽん'],
    ['100本', 'ひゃっぽん'], ['300本', 'さんびゃっぽん'], ['1000本', 'せんぼん'], ['100匹', 'ひゃっぴき'], ['1000匹', 'せんびき'],
    ['一ぴき', 'いっぴき'], ['三びき', 'さんびき'], ['六ぴき', 'ろっぴき'], ['100分', 'ひゃっぷん'], ['1000分', 'せんぷん'],
    ['4年生', 'よねんせい'], ['2年生', 'にねんせい'], ['四年間', 'よねんかん'], ['14人', 'じゅうよにん'],
    ['14時', 'じゅうよじ'], ['17時', 'じゅうしちじ'], ['19時', 'じゅうくじ'], ['24時間', 'にじゅうよじかん'],
    ['三時半', 'さんじはん'], ['5分後', 'ごふんご'], ['10分前', 'じゅっぷんまえ'], ['三日後', 'みっかご'], ['三日間', 'みっかかん'],
    ['一週間後', 'いっしゅうかんご'], ['四年後', 'よねんご'], ['4月1日', 'しがつついたち'],
    ['組合せ', 'くみあわせ'], ['労働組合', 'ろうどうくみあい'], ['初期微動', 'しょきびどう'], ['柔毛', 'じゅうもう'],
    ['1分', 'いっぷん'], ['2分', 'にふん'], ['3分', 'さんぷん'], ['4分', 'よんぷん'], ['6分', 'ろっぷん'], ['8分', 'はっぷん'],
    ['2日', 'ふつか'], ['4日', 'よっか'], ['20日', 'はつか'], ['24日', 'にじゅうよっか'],
    ['話した', 'はなした'], ['分かる', 'わかる'], ['分けた', 'わけた'], ['数える', 'かぞえる'],
    ['選んだ', 'えらんだ'], ['変える', 'かえる'], ['表した', 'あらわした'], ['少し', 'すこし'], ['残り', 'のこり'],
    ['今日は二人で話した。', 'きょうはふたりではなした。'], ['お母さんと母', 'おかあさんとはは'],
    ['学校へ行った', 'がっこうへいった'], ['実験を行った', 'じっけんをおこなった'],
    ['文章', 'ぶんしょう'], ['漢字', 'かんじ'], ['気持ち', 'きもち'], ['植物', 'しょくぶつ'], ['観察', 'かんさつ'],
    ['二酸化炭素', 'にさんかたんそ'], ['水溶液', 'すいようえき'], ['光合成', 'こうごうせい'], ['電流', 'でんりゅう'],
    ['直方体', 'ちょくほうたい'], ['表面積', 'ひょうめんせき'], ['比例', 'ひれい'], ['方程式', 'ほうていしき'],
    ['平方根', 'へいほうこん'], ['確率', 'かくりつ'], ['中央交差点', 'ちゅうおうこうさてん'],
    ['日本国憲法', 'にほんこくけんぽう'], ['鎌倉幕府', 'かまくらばくふ'], ['産業革命', 'さんぎょうかくめい'],
    ['個人情報', 'こじんじょうほう'], ['情報源', 'じょうほうげん'], ['著作権', 'ちょさくけん'], ['条件分岐', 'じょうけんぶんき'],
    ['招待', 'しょうたい'], ['分析', 'ぶんせき'], ['矛盾', 'むじゅん'], ['指示語', 'しじご'], ['比喩', 'ひゆ'],
  ];
  for (const [text, reading] of [...expected, ...furiganaContextFixtures]) {
    const rendered = app.withFurigana(text);
    check('word and contextual furigana are correct', spoken(rendered) === reading, `${text}: ${spoken(rendered)} != ${reading}`);
    check('furigana preserves the original text', visible(rendered) === text, text);
    check('furigana is idempotent', app.withFurigana(rendered) === rendered && readings(rendered).every(ruby => readings(ruby.children).length === 0), text);
    const rich = app.questionRich({ display: { explanation: `**${text}**` } }, 'explanation', text);
    check('rich feedback retains contextual readings', spoken(rich) === reading && visible(rich) === text, `${text}: ${spoken(rich)} != ${reading}`);
  }
  const unknown = '学龘校';
  const gradeSixFields = new Map();
  for (const unit of units.filter(u => u.grade === 6)) for (const item of unit.questions) {
    const q = app.pickCurriculumBank(unit, item.stage, item);
    for (const field of ['prompt', 'answer', 'explanation', 'activityPrompt']) {
      if (q[field]) gradeSixFields.set(q[field], () => app.questionRich(q, field, q[field]));
    }
    q.choices.forEach((text, index) => gradeSixFields.set(text,
      () => app.questionChoiceRich(q, index, text, app.kanjiTargetChoices(q))));
  }
  for (const [text, expectedReading] of furiganaGradeSixFixtures) {
    const render = gradeSixFields.get(text);
    check('reviewed grade-6 phrase exists in the real bank', !!render, text);
    if (!render) continue;
    const result = render();
    check('grade-6 field readings match reviewed sentences', spoken(result) === expectedReading, `${text}: ${spoken(result)} != ${expectedReading}`);
    check('grade-6 field rendering preserves text', visible(result) === text, text);
  }
  // The single-letter answer can deliberately teach a different reading from
  // ordinary prose. Render the actual canonical assessment data, not dictionary
  // expectations, and ensure both plain and rich feedback retain that reading.
  for (const entry of app.kanjiCurriculumEntries()) for (const subtype of ['reading', 'kanji-choice']) {
    const surface = entry.word, reading = entry.r;
    const q = { topic: 'kokugo', isKokugo: true, subtype,
      word: subtype === 'reading' ? surface : reading,
      answer: subtype === 'reading' ? reading : surface,
      prompt: subtype === 'reading' ? surface : reading,
      explanation: `「${surface}」は「${reading}」と読む。` };
    for (const rich of [false, true]) {
      const question = rich ? { ...q, display: { prompt: `**${q.prompt}**`, answer: `**${q.answer}**`, explanation: `**${q.explanation}**` } } : q;
      const explanation = app.questionRich(question, 'explanation', q.explanation);
      check('kanji feedback uses the assessed reading', spoken(explanation) === `「${reading}」は「${reading}」とよむ。`, `${surface}/${reading}: ${spoken(explanation)}`);
      check('kanji feedback preserves the written answer', visible(explanation) === q.explanation, surface);
      const prompt = app.questionRich(question, 'prompt', q.prompt);
      check('kanji prompt never reveals the assessment reading', readings(prompt).length === 0 && visible(prompt) === q.prompt, surface);
      if (subtype === 'kanji-choice') {
        const answer = app.questionRich(question, 'answer', q.answer);
        check('kanji feedback answer uses the assessed reading', spoken(answer) === reading && visible(answer) === surface, `${surface}/${reading}: ${spoken(answer)}`);
      }
    }
  }
  const contextQuestion = { topic: 'kokugo', subtype: 'reading', word: '私', answer: 'し', explanation: '「私」は「し」と読む。私が使う物。' };
  check('assessment reading does not override surrounding prose', spoken(app.questionRich(contextQuestion, 'explanation', contextQuestion.explanation)) === '「し」は「し」とよむ。わたしがつかうもの。', contextQuestion.explanation);
  check('assessment rendering does not mutate the shared vocabulary', spoken(app.withFurigana('私が使う紙')) === 'わたしがつかうかみ', '私が使う紙');
  check('unknown compounds never synthesize character readings', readings(app.withFurigana(unknown)).length === 0 && visible(app.withFurigana(unknown)) === unknown, unknown);
  const protectedText = '招待と招待';
  const protectedRendered = app.withFurigana(protectedText, false, ['待']);
  check('reading targets are protected even inside a longer word', readings(protectedRendered).length === 0 && visible(protectedRendered) === protectedText, protectedText);
  for (const grade of [7, 8, 9]) {
    const unit = units.find(unit => unit.grade === grade && unit.questions.some(q => q.readingTarget));
    check('each middle grade protects its reading assessment', !!unit, String(grade));
    if (!unit) continue;
    const item = unit.questions.find(q => q.readingTarget), q = app.pickCurriculumBank(unit, item.stage, item);
    for (const rich of [false, true]) {
      const question = rich ? { ...q, display: { prompt: `**${q.prompt}**` } } : q;
      const prompt = app.questionRich(question, 'prompt', question.prompt);
      check('reading targets stay bare in plain and rich prompts', !readings(prompt).some(ruby => visible(ruby).includes(item.readingTarget)), `${grade}: ${visible(prompt)}`);
      check('reading-target suppression keeps the full prompt', visible(prompt) === q.prompt, q.prompt);
      check('reading feedback still teaches the correct pronunciation', readings(app.questionRich(q, 'explanation', q.explanation)).some(ruby => visible(ruby) === item.readingTarget && spoken(ruby) === item.answer), item.readingTarget);
    }
  }
  for (const subtype of ['kanji-choice', 'kanji-picture']) {
    const q = { topic: 'kokugo', subtype, display: { choices: ['**算数**'] } };
    const choice = app.questionChoiceRich(q, 0, '算数', app.kanjiTargetChoices(q));
    check('rich kanji answer choices never reveal their reading', readings(choice).length === 0 && visible(choice) === '算数' && nodes(choice).some(n => n.type === 'strong'), subtype);
  }
  const gradeTwo = profileFor(2);
  for (const topic of ['frac', 'kokugo', 'dokkai']) {
    const unit = units.find(u => u.grade === 2 && u.generatorKey === topic);
    for (let stage = 1; stage <= 5; stage++) {
      const passages = new Set();
      for (let sample = 0; sample < 200; sample++) {
        const q = app.genFor(unit.id, gradeTwo, stage);
        if (topic === 'frac') {
          check('grade-2 fractions are concrete equal unit parts', q.isFracViz && q.fn === 1 && [2, 3, 4, 8].includes(q.fd), JSON.stringify(q));
          check('grade-2 fractions avoid abstract computation', !/いくつ分|どちらが 大きい|\d+\.\d+|\s[+＋−-]\s/.test(q.prompt), q.prompt);
          if (stage <= 3) check('grade-2 fraction label matches the picture', q.answer === `1/${q.fd}` && q.choices.every(c => /^1\/(2|3|4|8)$/.test(c)), JSON.stringify(q));
          if (stage === 4) check('grade-2 fraction explanation identifies equal parts', q.answer === `同じ 大きさの ${q.fd}つに 分けた 1つ分だから`, JSON.stringify(q));
          if (stage === 5) check('grade-2 fractional reconstruction matches the diagram', Number(q.answer) === q.fd, JSON.stringify(q));
        } else if (topic === 'kokugo') {
          check('grade-2 kanji targets belong to the current grade', q.kanjiGrade === 2, JSON.stringify(q));
        } else {
          check('grade-2 reading advances within its own grade', q.passageGrade === 2 && q.readingStage === stage, JSON.stringify(q));
          passages.add(q.prompt);
        }
      }
      if (topic === 'dokkai') check('each grade-2 reading stage has multiple passages', passages.size >= 3, `stage ${stage}: ${passages.size}`);
    }
  }
  const multiplication = new app.constructor();
  multiplication.topicStage = () => 5;
  multiplication.multiplicationFactProgress = () => ({ secure: 1, total: 1 });
  multiplication.pick4 = (answer, distractors) => [answer, ...distractors.slice(0, 3)];
  const random = Math.random;
  try {
    for (let a = 2; a <= 9; a++) for (let b = 2; b <= 9; b++) {
      let call = 0, digit = 0;
      Math.random = () => call++ === 0 ? .9 : .1;
      multiplication.rand = () => digit++ === 0 ? a : b;
      const q = multiplication.pickMul(gradeTwo);
      const solve = text => { const [x, op, y] = text.split(' '); return op === '×' ? Number(x) * Number(y) : Number(x) + Number(y); };
      check('multiplication equivalence has exactly one correct choice', q.choices?.filter(c => solve(c) === a * b).length === 1, JSON.stringify(q));
    }
  } finally { Math.random = random; }
  const shapeUnit = units.find(u => u.grade === 2 && u.generatorKey === 'shape');
  let squares = 0;
  for (let stage = 1; stage <= 5; stage++) for (let sample = 0; sample < 100; sample++) {
    const q = app.genFor(shapeUnit.id, gradeTwo, stage);
    if (q.answer !== '正方形') continue;
    squares++;
    check('square explanations require both equal sides and right angles', q.explanation.includes('直角') && q.explanation.includes('4つ'), q.explanation);
  }
  check('square definition check observes actual questions', squares > 0, String(squares));
}
