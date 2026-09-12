// Solve the shipped Japanese prompts independently of the C# authoring formulas.
// Exhaustive small state spaces also check cases whose correct answer is unknown.
const permutations = values => values.length === 0 ? [[]] : values.flatMap((value, index) =>
  permutations(values.filter((_, at) => at !== index)).map(tail => [value, ...tail]));
const capture = (text, pattern) => {
  const match = text.match(pattern);
  if (!match) throw new Error(`Unparsed prompt: ${pattern}`);
  return match.slice(1);
};
const digits = text => [...text.matchAll(/\d+/g)].map(match => Number(match[0]));

export function solveThinkingPrompt(prompt) {
  const family = capture(prompt, /^【(.+?)】/)[0];
  const p = prompt.replace(/^【.+?】/, '');
  if (family === 'ならびの推理') {
    const cards = capture(p, /^(.+?)の カード/)[0].split('・');
    const clues = [...p.matchAll(/([^。]+?)は (.+?)より 左。/g)].map(match => [match[1], match[2]]);
    const position = Number(capture(p, /左から (\d+)ばんめは/)[0]) - 1;
    const possibilities = permutations(cards).filter(order => clues.every(([a, b]) =>
      cards.includes(a) && cards.includes(b) && order.indexOf(a) < order.indexOf(b)));
    if (!possibilities.length) throw new Error('Contradictory ordering clues');
    const outcomes = new Set(possibilities.map(order => order[position]));
    return outcomes.size === 1 ? [...outcomes][0] : 'きめられない';
  }
  if (family === 'わりあての推理') {
    const [people, fruit] = capture(p, /^(.+?)が、(.+?)を 1こずつ/);
    const names = people.split('・'), items = fruit.split('・');
    const exclusions = [...p.matchAll(/([^。]+?)は ([^。]+?)を えらびません。/g)]
      .map(match => [match[1], match[2].split('・')]);
    const target = capture(p, /。([^。]+?)が えらぶのは/)[0];
    const solutions = permutations(items).filter(assignment => exclusions.every(([name, forbidden]) =>
      names.includes(name) && !forbidden.includes(assignment[names.indexOf(name)])));
    if (solutions.length !== 1) throw new Error(`Assignment has ${solutions.length} solutions`);
    return solutions[0][names.indexOf(target)];
  }
  if (family === 'うしろから考える') {
    const [chain, result] = capture(p, /「(.+?)」.+?、(\d+)になりました/);
    const operations = chain.split(' → ').map(step => capture(step, /^(\d+)(たす|ひく|倍する)$/));
    // Search starting values by applying the stated operations forwards.
    const starts = Array.from({ length: 501 }, (_, value) => value).filter(start => {
      let value = start;
      for (const [raw, operation] of operations) {
        const amount = Number(raw);
        value = operation === 'たす' ? value + amount : operation === 'ひく' ? value - amount : value * amount;
      }
      return value === Number(result);
    });
    if (starts.length !== 1) throw new Error(`Reverse puzzle has ${starts.length} solutions`);
    return String(starts[0]);
  }
  if (family === '道すじをたどる') {
    if (p.startsWith('1から')) {
      const [start, right, left] = capture(p, /(\d+)の ますから、右へ (\d+)ます、左へ (\d+)ます/).map(Number);
      const end = Number(capture(p, /1から (\d+)まで/)[0]);
      if (start < 1 || start + right > end || start + right - left < 1) throw new Error('Route leaves the board');
      let position = start;
      for (let step = 0; step < right; step++) position++;
      for (let step = 0; step < left; step++) position--;
      return String(position);
    }
    if (p.startsWith('4人で')) {
      const durations = Object.fromEntries([...p.matchAll(/([ABCD])は (\d+)分/g)].map(m => [m[1], Number(m[2])]));
      const dependencies = Object.fromEntries([...p.matchAll(/([CD])は ([ABC])(?:と([ABC]))?が (?:両方 )?終わったら/g)]
        .map(m => [m[1], [m[2], m[3]].filter(Boolean)]));
      // Discrete event simulation, independent of the author's critical-path formula.
      const started = new Map(), ended = new Set();
      for (let minute = 0; minute <= 100; minute++) {
        for (const [task, time] of started) if (time + durations[task] === minute) ended.add(task);
        if (ended.size === 4) return String(minute);
        for (const task of ['A', 'B', 'C', 'D']) if (!started.has(task) &&
          (dependencies[task] || []).every(parent => ended.has(parent))) started.set(task, minute);
      }
      throw new Error('Task schedule did not terminate');
    }
    const routes = ['直通', '公園を通る道', '図書館を通る道'].map(label =>
      digits(capture(p, new RegExp(`${label}：([^。]+)`))[0]).reduce((a, b) => a + b, 0));
    return String(Math.min(...routes));
  }
  if (family === '組合せをしぼる') {
    if (p.startsWith('えんぴつ')) {
      const [cheap, expensive, count] = capture(p, /1本(\d+)円、ペンは 1本(\d+)円.+?合わせて (\d+)本/).map(Number);
      const exact = p.includes('ちょうど');
      const budget = Number(capture(p, exact ? /ちょうど (\d+)円/ : /予算は (\d+)円/)[0]);
      const feasible = Array.from({ length: count + 1 }, (_, pens) => pens).filter(pens => {
        const cost = (count - pens) * cheap + pens * expensive;
        return exact ? cost === budget : cost <= budget;
      });
      if (!feasible.length || (exact && feasible.length !== 1)) throw new Error('Invalid budget puzzle');
      return String(Math.max(...feasible));
    }
    const cards = capture(p, /^([\d・]+)の 数カード/)[0].split('・').map(Number);
    const target = Number(capture(p, /合計を (\d+)に/)[0]);
    const pairs = [];
    for (let a = 0; a < cards.length; a++) for (let b = a + 1; b < cards.length; b++)
      if (cards[a] + cards[b] === target) pairs.push(`${cards[a]}と${cards[b]}`);
    if (pairs.length !== 1) throw new Error(`Card puzzle has ${pairs.length} solutions`);
    return pairs[0];
  }
  if (family === 'かならずを考える') {
    if (p.startsWith('ふくろ')) {
      const [red, blue] = capture(p, /あかい玉が (\d+)こ、あおい玉が (\d+)こ/).map(Number);
      const target = capture(p, /どんな じゅんでも (あか|あお)を/)[0];
      // Enumerate every attainable prefix count, including the worst ordering.
      for (let draws = 1; draws <= red + blue; draws++) {
        let guaranteed = true;
        for (let reds = 0; reds <= red; reds++) {
          const blues = draws - reds;
          if (blues >= 0 && blues <= blue && (target === 'あか' ? reds === 0 : blues === 0)) guaranteed = false;
        }
        if (guaranteed) return String(draws);
      }
    } else {
      const [stones, maximum] = capture(p, /石が (\d+)こ.+?1〜(\d+)こ/).map(Number);
      // Minimax: a state wins iff some legal move leaves a losing state.
      const wins = [false];
      for (let left = 1; left <= stones; left++) wins[left] = Array.from(
        { length: Math.min(maximum, left) }, (_, i) => i + 1).some(take => !wins[left - take]);
      const moves = Array.from({ length: maximum }, (_, i) => i + 1).filter(take => !wins[stones - take]);
      if (moves.length !== 1) throw new Error(`Game has ${moves.length} winning moves`);
      return String(moves[0]);
    }
  }
  throw new Error(`Unsolved family: ${family}`);
}

export function auditThinkingQuestions(app, observe, violated) {
  const units = app.curriculumCatalog().filter(unit => unit.topicId === 'thinking');
  const firstGrade = units.find(unit => unit.grade === 1), secondGrade = units.find(unit => unit.grade === 2);
  observe('grade two adds constraints and reverse steps beyond grade one');
  for (const stage of [1, 2, 3, 4, 5]) {
    const first = firstGrade.questions.find(item => item.stage === stage && item.prompt.startsWith('【うしろから考える】'));
    const second = secondGrade.questions.find(item => item.stage === stage && item.prompt.startsWith('【うしろから考える】'));
    if (second.prompt.split(' → ').length <= first.prompt.split(' → ').length)
      violated('grade two adds constraints and reverse steps beyond grade one', `stage ${stage}`, second.prompt);
  }
  for (const item of secondGrade.questions.filter(item => item.prompt.startsWith('【ならびの推理】'))) {
    if (capture(item.prompt, /】(.+?)の カード/)[0].split('・').length !== 4)
      violated('grade two adds constraints and reverse steps beyond grade one', 'missing fourth card', item.prompt);
  }
  for (const unit of units) {
    for (const stage of [1, 2, 3, 4, 5]) {
      const items = unit.questions.filter(item => item.stage === stage);
      const families = new Set(items.map(item => capture(item.prompt, /^【(.+?)】/)[0]));
      observe('reasoning stages offer six families with distinct puzzles');
      if (items.length !== 24 || families.size !== 6 || new Set(items.map(item => item.prompt)).size !== 24)
        violated('reasoning stages offer six families with distinct puzzles', `${unit.id} stage ${stage}`, JSON.stringify(items));
    }
    for (const item of unit.questions) {
      const check = 'every reasoning answer is independently solved from its displayed clues';
      observe(check);
      try {
        const expected = solveThinkingPrompt(item.prompt);
        const generated = app.pickCurriculumBank(unit, item.stage, item);
        if (String(item.answer) !== expected || String(generated.answer) !== expected ||
          generated.choices.length !== 4 || new Set(generated.choices).size !== 4 ||
          generated.choices.filter(value => String(value) === expected).length !== 1 || !item.explanation)
          violated(check, `expected ${expected}, got ${item.answer}`, item.prompt);
        if (unit.grade <= 2 && /90|180|倍する|で わる|相手が x/.test(item.prompt + item.explanation))
          violated(check, 'later-grade prerequisites in a lower-grade puzzle', item.prompt);
      } catch (error) { violated(check, error.message, item.prompt); }
    }
  }
  const originalState = app.state;
  try {
    const retired = ['kateika', 'doutoku', 'tokubetsu'];
    const check = 'retired subjects stay absent and old progress survives catalog migration';
    observe(check);
    const legacy = { name: 'retired-audit', grade: 2, learningSchema: 7, stars: 17, xp: 81,
      mastery: {}, skillStats: {}, cleared: { doutoku: true },
      unitStats: { 'moral.g2.doutoku': { attempts: 3, independent: 2, level: 2 } } };
    const profile = app.migrateProfiles([legacy])[0];
    app.state = { ...originalState, settings: app.defaultSettings(), profiles: [profile], profileIdx: 0 };
    if (retired.some(key => key in app.topics || key in app.defaultSettings().topics ||
      app.curriculumCatalog().some(unit => unit.topicId === key)) || profile.stars !== 17 || profile.xp !== 81 ||
      !profile.cleared.doutoku || profile.unitStats['moral.g2.doutoku'].attempts !== 3)
      violated(check, 'removed topic exposed or historical data changed', JSON.stringify(profile));
    const current = app.curriculumCatalog().find(unit => unit.id === app.unlockedGradeTopics(profile)[0]);
    const question = app.genFor(current.id, profile, 1);
    const checkpoint = { version: 1, profileName: profile.name, screen: 'quiz',
      session: { idx: 0, rolePlan: ['target'], questions: [question] } };
    if (!app.validLearningCheckpoint(checkpoint)) violated(check, 'a valid current session was rejected', JSON.stringify(checkpoint));
    for (const topic of retired) {
      for (const session of [
        { ...checkpoint.session, questions: [{ ...question, topic, unitId: undefined }] },
        { ...checkpoint.session, activeTargetTopic: topic },
        { ...checkpoint.session, reviewTopics: [topic] },
      ]) if (app.validLearningCheckpoint({ ...checkpoint, session }))
        violated(check, 'retired subject can reappear through a checkpoint', JSON.stringify(session));
    }
  } finally { app.state = originalState; }
}
