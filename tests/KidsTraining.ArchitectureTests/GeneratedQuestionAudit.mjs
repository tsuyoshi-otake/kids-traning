// Executable audit of the generated question bank (issue #41).
//
// Every question generator lives in the single JS class that the generated runtime page
// carries, so the audit lifts that class out of the page and runs the real generators on
// Node's V8 -- the same engine family WebView2 renders the app with. Nothing here
// re-implements a generator: a copy would drift and stop protecting the child.
//
// Usage: node GeneratedQuestionAudit.mjs <generated-runtime-html> [samples] [seed]
// Exit code 0 = every invariant holds, 1 = at least one violation, 2 = the audit could
// not run at all (which is also a failure, never a silent skip).
//
// The default sample count keeps the suite quick; pass a larger one to sweep for rare
// combinations, and a different seed to confirm a clean sweep was not luck.

import { readFileSync } from 'node:fs';

const runtimePagePath = process.argv[2];
if (!runtimePagePath) {
  console.error('Usage: node GeneratedQuestionAudit.mjs <generated-runtime-html>');
  process.exit(2);
}

// --- lift the app class out of the generated page ---------------------------------------

const page = readFileSync(runtimePagePath, 'utf8');
const classMarker = 'class Component extends DCLogic';
const classAt = page.indexOf(classMarker);
if (classAt < 0) {
  console.error(`the generated page does not contain "${classMarker}": ${runtimePagePath}`);
  process.exit(2);
}
const scriptOpen = page.lastIndexOf('<script', classAt);
const sourceStart = page.indexOf('>', scriptOpen) + 1;
const sourceEnd = page.indexOf('</script>', classAt);
if (scriptOpen < 0 || sourceStart <= 0 || sourceEnd < 0) {
  console.error(`the app script block is not delimited as expected: ${runtimePagePath}`);
  process.exit(2);
}
const appSource = page.slice(sourceStart, sourceEnd);

// The audit only calls pure generator methods, so the framework base class needs no more
// than a state sink. Anything that reaches for the DOM would throw and be reported.
class DCLogic {
  setState(patch, callback) {
    Object.assign(this.state, patch);
    if (typeof callback === 'function') callback();
  }
}
const React = {
  Fragment: 'fragment',
  isValidElement(value) {
    return !!value && typeof value === 'object' && typeof value.type === 'string';
  },
  createElement(type, props, ...children) {
    return { type, props: props || {}, children };
  },
};

// Deterministic randomness: a failure reported by the audit must reproduce verbatim.
let randomState = (Number(process.argv[4]) || 0x2f6e2b1) | 0;
Math.random = () => {
  randomState = (randomState + 0x6d2b79f5) | 0;
  let t = randomState;
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
};

let app;
try {
  const Component = new Function('DCLogic', 'React', `${appSource}\nreturn Component;`)(DCLogic, React);
  app = new Component();
  app.props = app.props || {};
} catch (error) {
  console.error(`the app class could not be instantiated: ${error && error.stack}`);
  process.exit(2);
}

// --- failure ledger ---------------------------------------------------------------------

const violations = new Map();
const violated = (check, detail, sample) => {
  const key = `${check}: ${detail}`;
  if (!violations.has(key)) violations.set(key, { count: 0, samples: [] });
  const entry = violations.get(key);
  entry.count += 1;
  if (entry.samples.length < 3) entry.samples.push(sample);
};

// A check that never observed a matching question protects nothing, so every check
// declares what it counted and the audit fails when the count is zero.
const observed = new Map();
const observe = (check, amount = 1) => observed.set(check, (observed.get(check) || 0) + amount);

observe('the generated application reaches an explicit initial render state');
try {
  const initialView = app.renderVals();
  if (!initialView || typeof initialView !== 'object') {
    violated('the generated application reaches an explicit initial render state', 'renderVals returned no view model', JSON.stringify(initialView));
  }
} catch (error) {
  violated('the generated application reaches an explicit initial render state', String(error && error.message), String(error && error.stack));
}

const assertFurigana = (surface, reading) => {
  const rendered = app.withFurigana(surface);
  const ruby = Array.isArray(rendered) ? rendered.find((entry) => entry && entry.type === 'ruby') : null;
  const annotation = ruby && ruby.children.find((entry) => entry && entry.type === 'rt');
  observe('compound furigana overrides single-character readings');
  if (!ruby || ruby.children[0] !== surface || !annotation || annotation.children[0] !== reading) {
    violated('compound furigana overrides single-character readings', `${surface} did not render as ${reading}`, JSON.stringify(rendered));
  }
};
assertFurigana('外国', 'がいこく');
assertFurigana('外国語', 'がいこくご');
assertFurigana('学級', 'がっきゅう');
assertFurigana('課題', 'かだい');
assertFurigana('必要', 'ひつよう');
assertFurigana('共通', 'きょうつう');
assertFurigana('目標', 'もくひょう');
assertFurigana('判断', 'はんだん');
assertFurigana('基準', 'きじゅん');
assertFurigana('共有', 'きょうゆう');
assertFurigana('一人', 'ひとり');
assertFurigana('担当', 'たんとう');
assertFurigana('学校', 'がっこう');
assertFurigana('記録', 'きろく');

for (const [weekday, reading] of [
  ['月曜日', 'げつようび'],
  ['火曜日', 'かようび'],
  ['水曜日', 'すいようび'],
  ['木曜日', 'もくようび'],
  ['金曜日', 'きんようび'],
  ['土曜日', 'どようび'],
  ['日曜日', 'にちようび'],
]) {
  assertFurigana(weekday, reading);
}

// The production formatter returns React nodes, so inspect the whole stub tree rather than
// relying on a string snapshot. This protects the semantic class, accessible label, and
// visible glyph independently.
const notationNodes = (value, nodes = []) => {
  if (Array.isArray(value)) {
    for (const child of value) notationNodes(child, nodes);
    return nodes;
  }
  if (!value || typeof value !== 'object' || !value.type) return nodes;
  nodes.push(value);
  for (const child of value.children || []) notationNodes(child, nodes);
  return nodes;
};
const notationText = (value) => {
  if (Array.isArray(value)) return value.map(notationText).join('');
  if (value === null || value === undefined) return '';
  if (typeof value !== 'object') return String(value);
  return (value.children || []).map(notationText).join('');
};
const notationClass = (node, name) => String(node.props?.className || '').split(/\s+/).includes(name);
const notationByClass = (rendered, name) => notationNodes(rendered).filter((node) => notationClass(node, name));
const notationLabels = (rendered) => notationNodes(rendered).map((node) => node.props?.['aria-label']).filter(Boolean);
const assertNotation = (name, source, expectation) => {
  const gradingData = { prompt: source, answer: 'unchanged-answer', choices: ['unchanged-choice'] };
  const canonical = JSON.stringify(gradingData);
  const rendered = app.withLearningNotation(gradingData.prompt);
  observe('learning notation keeps canonical grading data and exposes semantic React nodes');
  if (JSON.stringify(gradingData) !== canonical) {
    violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} mutated a canonical question field`, canonical);
  }
  const text = notationText(rendered);
  const labels = notationLabels(rendered);
  const count = (className) => notationByClass(rendered, className).length;
  if (expectation.unchanged) {
    if (rendered !== source || notationNodes(rendered).length !== 0 || text !== source) {
      violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} was transformed although it is not mathematics`, JSON.stringify(rendered));
    }
    return;
  }
  if (count('kt-fraction') !== (expectation.fractions || 0)) {
    violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} rendered ${count('kt-fraction')} fractions, expected ${expectation.fractions || 0}`, JSON.stringify(rendered));
  }
  for (const className of [expectation.className, ...(expectation.classNames || [])].filter(Boolean)) {
    if (count(className) === 0) {
      violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} lacks ${className}`, JSON.stringify(rendered));
    }
  }
  for (const label of expectation.labels || []) {
    if (!labels.includes(label)) {
      violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} lacks accessible operator label ${label}`, JSON.stringify(rendered));
    }
  }
  if (expectation.text && !text.includes(expectation.text)) {
    violated('learning notation keeps canonical grading data and exposes semantic React nodes', `${name} does not expose visible text ${expectation.text}`, JSON.stringify(rendered));
  }
};
assertNotation('compact fraction multiplication', '2/3×3/5', { fractions: 2, className: 'kt-math-operator', labels: ['かける'] });
assertNotation('ASCII fraction multiplication', '2/3*3/5', { fractions: 2, className: 'kt-math-operator', labels: ['かける'], text: '×' });
assertNotation('spaced fraction multiplication', '2/3 × 3/5', { fractions: 2, className: 'kt-math-operator', labels: ['かける'] });
assertNotation('spaced subtraction', '16 - 6', { className: 'kt-math-operator', labels: ['ひく'], text: '−' });
assertNotation('algebraic fraction equation', 'x/3+2=6', { fractions: 1, className: 'kt-math-operator', labels: ['たす', 'イコール'] });
assertNotation('radical', '√48', { className: 'kt-radical' });
assertNotation('superscript', 'x²', { className: 'kt-power' });
assertNotation('signed algebraic solution', 'x=±3', { className: 'kt-math-operator', labels: ['イコール', 'プラスマイナス'], text: '±' });
assertNotation('unary negative literal', '-11', { className: 'kt-math-operator', labels: ['マイナス'], text: '−' });
assertNotation('negative solution list', 'x=-2, -3', { className: 'kt-math-operator', labels: ['イコール', 'マイナス'], text: '−' });
assertNotation('exponent subtraction', 'x²-9', { classNames: ['kt-power', 'kt-math-operator'], labels: ['ひく'], text: '−' });
assertNotation('exponent addition', 'x²+2x', { classNames: ['kt-power', 'kt-math-operator'], labels: ['たす'] });
assertNotation('ASCII ratio', '12:18', { className: 'kt-ratio', labels: ['12対18'] });
assertNotation('full-width ratio', '2：3', { className: 'kt-ratio', labels: ['2対3'] });
assertNotation('speed unit', '80km/h', { unchanged: true });
assertNotation('English ordering separators', 'I / play / tennis', { unchanged: true });
assertNotation('English grammar notation', 'have+過去分詞', { unchanged: true });
assertNotation('English colon label', 'note: value', { unchanged: true });
assertNotation('English hyphenated word', 'e-mail', { unchanged: true });

// Rich display content is an additive presentation layer: authored Markdown and constrained
// TeX become React nodes, while canonical prompt/answer/choice values remain grading data.
const richRendered = app.withRichText('**学校**で \\(\\frac{2}{3}\\times\\frac{3}{5}\\) を考える。');
observe('rich text renders safe Markdown and constrained TeX as semantic React nodes');
if (
  notationByClass(richRendered, 'kt-rich-strong').length !== 1 ||
  notationByClass(richRendered, 'kt-fraction').length !== 2 ||
  notationByClass(richRendered, 'kt-rich-math').length !== 1 ||
  !notationNodes(richRendered).some((node) => node.type === 'ruby') ||
  !notationLabels(richRendered).some((label) => label.includes('3分の2') && label.includes('5分の3'))
) {
  violated('rich text renders safe Markdown and constrained TeX as semantic React nodes', 'strong text, furigana, fractions, or the math label is missing', JSON.stringify(richRendered));
}

const richBlocks = app.withRichText('# 手順\n\n1. `x^2` はコード\n2. $$\\sqrt{x^2}\\ge 0$$');
observe('rich text supports bounded block structure without interpreting code spans');
if (
  !notationNodes(richBlocks).some((node) => node.type === 'h1') ||
  !notationNodes(richBlocks).some((node) => node.type === 'ol') ||
  notationByClass(richBlocks, 'kt-rich-code').length !== 1 ||
  notationByClass(richBlocks, 'kt-rich-math-block').length !== 1 ||
  notationByClass(richBlocks, 'kt-tex-radical').length !== 1 ||
  notationText(notationByClass(richBlocks, 'kt-rich-code')[0]) !== 'x^2'
) {
  violated('rich text supports bounded block structure without interpreting code spans', 'a heading, list, code span, or block formula was not rendered correctly', JSON.stringify(richBlocks));
}

const inertMarkup = app.withRichText('<img src=x onerror=alert(1)> [bad](javascript:alert(1))');
const rejectedTex = app.withRichInline('$\\href{javascript:alert(1)}{x}$ and $\\frac{1}$');
observe('rich text keeps HTML, links, unsafe TeX, and malformed TeX inert and visible');
if (
  notationNodes(inertMarkup).some((node) => ['img', 'script', 'a'].includes(node.type)) ||
  notationByClass(rejectedTex, 'kt-rich-math').length !== 0 ||
  !notationText(inertMarkup).includes('<img src=x onerror=alert(1)>') ||
  !notationText(rejectedTex).includes('\\href') ||
  !notationText(rejectedTex).includes('\\frac{1}')
) {
  violated('rich text keeps HTML, links, unsafe TeX, and malformed TeX inert and visible', 'unsafe or malformed source became active markup or disappeared', JSON.stringify({ inertMarkup, rejectedTex }));
}

const fractionUnit = app.curriculumCatalog().find((unit) => unit.grade === 6 && unit.topicId === 'frac');
const fractionQuestion = fractionUnit && app.pickCurriculumBank(fractionUnit, 1);
const fractionDisplayByCanonical = new Map([
  ['2/5', '\\(\\frac{2}{5}\\)'],
  ['5/8', '\\(\\frac{5}{8}\\)'],
  ['6/8', '\\(\\frac{6}{8}\\)'],
  ['1/5', '\\(\\frac{1}{5}\\)'],
]);
observe('rich choice display follows shuffled canonical choices without changing grading');
if (!fractionQuestion || fractionQuestion.prompt !== '2/3×3/5は？' || fractionQuestion.answer !== '2/5') {
  violated('rich choice display follows shuffled canonical choices without changing grading', 'the grade-6 fraction question lost its canonical prompt or answer', JSON.stringify(fractionQuestion));
} else {
  const canonicalBefore = JSON.stringify({
    prompt: fractionQuestion.prompt,
    answer: fractionQuestion.answer,
    choices: fractionQuestion.choices,
    explanation: fractionQuestion.explanation,
  });
  const promptNodes = app.questionRich(fractionQuestion, 'prompt', fractionQuestion.prompt);
  for (const [index, canonicalChoice] of fractionQuestion.choices.entries()) {
    const expectedDisplay = fractionDisplayByCanonical.get(String(canonicalChoice));
    const actualDisplay = fractionQuestion.display?.choices?.[index];
    const choiceNodes = app.questionChoiceRich(fractionQuestion, index, canonicalChoice);
    if (actualDisplay !== expectedDisplay || notationByClass(choiceNodes, 'kt-fraction').length !== 1) {
      violated('rich choice display follows shuffled canonical choices without changing grading', `${canonicalChoice} rendered from ${actualDisplay}`, JSON.stringify(choiceNodes));
    }
  }
  const identityBefore = app.questionIdentity({ ...fractionQuestion, display: { prompt: 'first presentation' } });
  const identityAfter = app.questionIdentity({ ...fractionQuestion, display: { prompt: 'second presentation' } });
  if (
    JSON.stringify({
      prompt: fractionQuestion.prompt,
      answer: fractionQuestion.answer,
      choices: fractionQuestion.choices,
      explanation: fractionQuestion.explanation,
    }) !== canonicalBefore ||
    notationByClass(promptNodes, 'kt-fraction').length !== 2 ||
    identityBefore !== identityAfter
  ) {
    violated('rich choice display follows shuffled canonical choices without changing grading', 'rendering mutated canonical fields, lost the prompt fractions, or changed question identity', JSON.stringify(fractionQuestion));
  }
}

// Written arithmetic is an assessed, bounded state machine. It must teach every intermediate
// operation while keeping unfinished values out of the rendered and accessible view.
const writtenCases = [
  { name: 'multi-digit addition', question: { topic: 'hissan', difficulty: 5, prompt: '1234 + 111', answer: '1345' }, kind: 'addition', expects: ['5', '4', '3', '1'] },
  { name: 'multi-digit subtraction', question: { topic: 'hissan', difficulty: 5, prompt: '9000 - 111', answer: '8889' }, kind: 'subtraction', expects: ['9', '8', '8', '8'] },
  { name: 'three-by-one multiplication', question: { topic: 'hissan', difficulty: 5, prompt: '123 × 7', answer: '861' }, kind: 'multiplication', expects: ['21', '16', '8'] },
  { name: 'two-by-two multiplication', question: { topic: 'hissan', difficulty: 5, prompt: '12 × 34', answer: '408' }, kind: 'multiplication', expects: ['8', '4', '6', '3', '8', '10', '4'] },
  { name: '864 long division', question: { topic: 'kazu', difficulty: 2, prompt: '864÷24は？', answer: '36' }, kind: 'division', expects: ['3', '72', '14', '144', '6', '144', '0'] },
  { name: 'one-place decimal multiplication', question: { topic: 'kazu', difficulty: 3, prompt: '3.6×4は？', answer: '14.4' }, kind: 'multiplication', expects: ['36', '24', '14', '1'] },
  { name: 'two-place decimal multiplication', question: { topic: 'kazu', difficulty: 3, prompt: '2.4×0.5は？', answer: '1.2' }, kind: 'multiplication', expects: ['24', '5', '20', '12', '2'] },
  { name: 'decimal long division', question: { topic: 'kazu', difficulty: 4, prompt: '3.6÷0.9は？', answer: '4' }, kind: 'division', expects: ['36', '9', '4', '36', '0'] },
  { name: 'remainder long division', question: { topic: 'div', difficulty: 5, prompt: '157 ÷ 9', answer: '17 あまり 4' }, kind: 'division', expects: ['1', '9', '6', '67', '7', '63', '4'] },
];
for (const sample of writtenCases) {
  observe('written arithmetic plans every required intermediate operation');
  const plan = app.writtenArithmeticPlan(sample.question);
  if (!plan) {
    violated('written arithmetic plans every required intermediate operation', `${sample.name} produced no plan`, JSON.stringify(sample.question));
    continue;
  }
  const expects = plan.steps.map((step) => String(step.expect));
  if (plan.kind !== sample.kind || JSON.stringify(expects) !== JSON.stringify(sample.expects)) {
    violated('written arithmetic plans every required intermediate operation', `${sample.name} returned ${plan.kind} ${JSON.stringify(expects)}`, JSON.stringify(plan));
  }
  for (let completed = 0; completed <= plan.steps.length; completed += 1) {
    observe('written arithmetic views are bounded and never expose unfinished answers');
    const view = app.writtenArithmeticView(plan, completed);
    const rendered = JSON.stringify(view);
    if (/undefined|NaN|\[object/.test(rendered) || !Array.isArray(view.lines) || !view.aria) {
      violated('written arithmetic views are bounded and never expose unfinished answers', `${sample.name} rendered invalid state at ${completed}`, rendered);
    }
    if (completed === 0 && rendered.includes(String(sample.question.answer))) {
      violated('written arithmetic views are bounded and never expose unfinished answers', `${sample.name} exposed the final answer before work began`, rendered);
    }
    const active = plan.steps[completed];
    if (active && view.stepPrompt !== active.prompt) {
      violated('written arithmetic views are bounded and never expose unfinished answers', `${sample.name} skipped active step ${completed}`, rendered);
    }
  }
}

{
  const plan = app.writtenArithmeticPlan(writtenCases.find((sample) => sample.name === '864 long division').question);
  const view = app.writtenArithmeticView(plan, plan.steps.length);
  const arithmeticLines = view.lines.filter((line) => line.tone !== 'caption');
  const widths = new Set(arithmeticLines.map((line) => line.text.length));
  observe('long division uses one clean textbook column grid');
  if (widths.size !== 1 || arithmeticLines.some((line) => /←|を下ろす|段目|□/.test(line.text))) {
    violated('long division uses one clean textbook column grid', 'rows were not equally aligned or contained inline teaching annotations', JSON.stringify(view.lines));
  }
  const dividendLine = arithmeticLines.find((line) => line.tone === 'number' && line.text.includes('864'));
  const firstProduct = arithmeticLines.find((line) => line.tone === 'partial' && line.text.includes('72'));
  const secondProducts = arithmeticLines.filter((line) => line.tone === 'partial' && line.text.includes('144'));
  const finalRemainder = [...arithmeticLines].reverse().find((line) => line.tone === 'result' && line.text.trim() === '0');
  const dividendStart = dividendLine?.text.indexOf('864') ?? -1;
  if (
    dividendStart < 0 ||
    firstProduct?.text.indexOf('72') !== dividendStart ||
    secondProducts.length !== 1 ||
    secondProducts[0].text.indexOf('144') !== dividendStart ||
    finalRemainder?.text.indexOf('0') !== dividendStart + 2
  ) {
    violated('long division uses one clean textbook column grid', 'products or remainder did not align with the dividend digits', JSON.stringify(view.lines));
  }
}

{
  const plan = app.writtenArithmeticPlan(writtenCases.find((sample) => sample.name === 'two-by-two multiplication').question);
  const view = app.writtenArithmeticView(plan, 4);
  const partials = view.lines.filter((line) => line.tone === 'partial');
  observe('long multiplication shifts partial products by place without printing placeholder zeroes');
  if (
    partials.length !== 2 ||
    partials[0].text.trim() !== '48' ||
    partials[1].text.trim() !== '36' ||
    partials[1].text.indexOf('36') !== partials[0].text.indexOf('48') - 1 ||
    partials.some((line) => /←|段目|□/.test(line.text))
  ) {
    violated('long multiplication shifts partial products by place without printing placeholder zeroes', 'partial products were not placed on the textbook column grid', JSON.stringify(view.lines));
  }
}

for (const question of [
  { topic: 'mul', difficulty: 5, prompt: '9 × 8', answer: '72' },
  { topic: 'div', difficulty: 4, prompt: '72 ÷ 8', answer: '9' },
  { topic: 'frac', difficulty: 5, prompt: '2/3×3/5', answer: '2/5' },
  { topic: 'frac', difficulty: 5, prompt: '3/4÷2/5', answer: '15/8' },
]) {
  observe('basic facts and fractions stay out of written-arithmetic steps');
  if (app.writtenArithmeticPlan(question) !== null) {
    violated('basic facts and fractions stay out of written-arithmetic steps', `${question.prompt} was incorrectly converted`, JSON.stringify(question));
  }
}

// Exercise the actual controller, including assisted work, success, terminal failure, and
// duplicate-submit protection. All branches must release the in-flight guard.
{
  const question = writtenCases.find((sample) => sample.name === '864 long division').question;
  const plan = app.writtenArithmeticPlan(question);
  const originals = { finish: app.finishScoredQuestion, exhaust: app.exhaustQuestion, reveal: app.revealAnswer, sfx: app.sfx };
  let finished = 0;
  let exhausted = 0;
  let revealed = 0;
  app.sfx = () => {};
  app.finishScoredQuestion = () => { finished += 1; app._terminalQuestionToken = app.currentQuestionToken(); };
  app.exhaustQuestion = () => { exhausted += 1; app._terminalQuestionToken = app.currentQuestionToken(); };
  app.revealAnswer = () => { revealed += 1; app._terminalQuestionToken = app.currentQuestionToken(); };
  app.state = { session: { attempt: 1, idx: 0, questions: [question], rolePlan: ['kazu'] }, input: '', waStep: 0, waMistakes: 0, waStepMiss: 0, waStepChoices: null, combo: 0 };
  app._terminalQuestionToken = '';
  app._answerBusy = false;
  app.submitWrittenStep('99');
  observe('written arithmetic controller reaches exactly one explicit terminal state');
  if (app.state.waStep !== 0 || app.state.waMistakes !== 1 || app._answerBusy) {
    violated('written arithmetic controller reaches exactly one explicit terminal state', 'a recoverable error skipped a step or retained the busy lock', JSON.stringify(app.state));
  }
  for (const step of plan.steps) app.submitWrittenStep(step.expect);
  app.submitWrittenStep(plan.steps.at(-1).expect);
  if (finished !== 1 || exhausted !== 0 || revealed !== 0 || app._answerBusy) {
    violated('written arithmetic controller reaches exactly one explicit terminal state', `success terminals: finish=${finished}, exhaust=${exhausted}, reveal=${revealed}, busy=${app._answerBusy}`, JSON.stringify(app.state));
  }
  app.state = { session: { attempt: 2, idx: 0, questions: [question], rolePlan: ['kazu'] }, input: '', waStep: 0, waMistakes: 0, waStepMiss: 0, waStepChoices: null, combo: 0 };
  app._terminalQuestionToken = '';
  app._answerBusy = false;
  app.submitWrittenStep('99');
  app.submitWrittenStep('99');
  app.submitWrittenStep('99');
  if (finished !== 1 || exhausted !== 1 || revealed !== 0 || app._answerBusy) {
    violated('written arithmetic controller reaches exactly one explicit terminal state', `failure terminals: finish=${finished}, exhaust=${exhausted}, reveal=${revealed}, busy=${app._answerBusy}`, JSON.stringify(app.state));
  }
  Object.assign(app, { finishScoredQuestion: originals.finish, exhaustQuestion: originals.exhaust, revealAnswer: originals.reveal, sfx: originals.sfx });
}

// A paused exercise must reopen on the same intermediate operation, with the same assisted
// choices and error accounting. The checkpoint stores state, never a second copy of the plan.
{
  const question = writtenCases.find((sample) => sample.name === 'two-by-two multiplication').question;
  const originals = { curP: app.curP, valid: app.validLearningCheckpoint };
  const priorStorage = globalThis.localStorage;
  const values = new Map();
  globalThis.localStorage = {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
  };
  app.curP = () => ({ name: 'audit' });
  app.state = {
    screen: 'quiz',
    session: { attempt: 3, idx: 0, questions: [question], rolePlan: ['hissan'] },
    combo: 0,
    input: '1',
    waStep: 3,
    waMistakes: 2,
    waStepMiss: 1,
    waStepChoices: null,
    waHint: '途中のヒント',
    waError: '',
  };
  const checkpoint = app.checkpointState();
  observe('written arithmetic checkpoint restores the exact active step');
  if (!checkpoint || checkpoint.waStep !== 3 || checkpoint.waMistakes !== 2 || checkpoint.waHint !== '途中のヒント') {
    violated('written arithmetic checkpoint restores the exact active step', 'checkpoint serialization dropped written-work state', JSON.stringify(checkpoint));
  }
  values.set(app.learningCheckpointKey(), JSON.stringify(checkpoint));
  app.validLearningCheckpoint = () => true;
  app.state = {};
  if (!app.restoreLearningCheckpoint() || app.state.waStep !== 3 || app.state.waMistakes !== 2 || app.state.input !== '1' || app.state.waHint !== '途中のヒント') {
    violated('written arithmetic checkpoint restores the exact active step', 'checkpoint restore changed the active written-work state', JSON.stringify(app.state));
  }
  app.curP = originals.curP;
  app.validLearningCheckpoint = originals.valid;
  if (priorStorage === undefined) delete globalThis.localStorage;
  else globalThis.localStorage = priorStorage;
}
const shouldAppendNumericQuestionMark = (question) => question.mode === 'num' && question.topic !== 'story' && !/[=?]/.test(String(question.prompt || ''));
observe('numeric prompts add exactly one answer marker only when needed', 2);
if (shouldAppendNumericQuestionMark({ mode: 'num', topic: 'add', prompt: '□ + 5 = 12' })) {
  violated('numeric prompts add exactly one answer marker only when needed', 'a missing-operand equation would receive a duplicate = ?', '□ + 5 = 12');
}
if (!shouldAppendNumericQuestionMark({ mode: 'num', topic: 'add', prompt: '12 + 3' })) {
  violated('numeric prompts add exactly one answer marker only when needed', 'a bare numeric expression lost its = ?', '12 + 3');
}

// --- fixtures ---------------------------------------------------------------------------

const TOPICS = [
  'add', 'sub', 'hissan', 'mul', 'clock', 'kokugo', 'moji', 'measure', 'kazu', 'shape',
  'div', 'frac', 'chart', 'story', 'bun', 'goi', 'dokkai', 'eigo', 'money', 'groups',
  'order', 'soroban', 'seikatsu', 'shakai', 'rika', 'kateika', 'gijutsu', 'doutoku', 'jouhou', 'sougou',
  'tokubetsu', 'keyboard', 'thinking',
];
const STAGES = [1, 2, 3, 4, 5];
const SAMPLES_PER_COMBINATION = Math.max(1, Number(process.argv[3]) || 300);

const appTopics = Object.keys(app.topics || {});
const unaudited = appTopics.filter((topic) => !TOPICS.includes(topic));
const stale = TOPICS.filter((topic) => !appTopics.includes(topic));
if (unaudited.length) violated('topic coverage', `the app teaches topics the audit never generates: ${unaudited.join(', ')}`, '');
if (stale.length) violated('topic coverage', `the audit generates topics the app no longer teaches: ${stale.join(', ')}`, '');

const profileFor = (grade) => ({
  name: 'audit',
  grade,
  color: '#ff8a3d',
  streak: 0,
  stars: 0,
  mastery: Object.fromEntries(TOPICS.map((topic) => [topic, 0.85])),
  // masteredAt unlocks the lanes that gate on a prerequisite topic being complete.
  skillStats: Object.fromEntries(TOPICS.map((topic) => [topic, { level: 5, attempts: 9, correct: 9, masteredAt: 1 }])),
});

// --- reference data taken from the app itself, never re-typed ----------------------------

const curriculum = app.kanjiCurriculumEntries();
const kanjiCounts = [1, 2, 3, 4, 5, 6].map((grade) => curriculum.filter((entry) => entry.g === grade).length);
observe('kanji allocation is complete', curriculum.length);
if (kanjiCounts.join(',') !== '80,160,200,202,193,191' || curriculum.length !== 1026 || new Set(curriculum.map((entry) => entry.k)).size !== 1026) {
  violated('kanji allocation is complete', `counts were ${kanjiCounts.join(',')} with ${curriculum.length} total entries`, 'MEXT grade allocation');
}
const readingsByWord = new Map();
for (const entry of curriculum) {
  const word = entry.word || entry.k;
  if (!readingsByWord.has(word)) readingsByWord.set(word, new Set());
  readingsByWord.get(word).add(entry.r);
}

// Readings that inflect are only unambiguous when the okurigana is on screen. Verb and
// adjective tails all end in one of these kana.
// Derived nouns and na-adjectives (同じ, 幸せ, 平ら) are written the same way, so their
// tails count as well.
const INFLECTION_ENDINGS = ['う', 'く', 'ぐ', 'す', 'つ', 'ぬ', 'ふ', 'ぶ', 'む', 'る', 'い', 'じ', 'せ', 'ら'];
// A 拗音 marks an on reading (じゅう, ひゃく, きょく), which never takes okurigana.
const YOUON = ['ゃ', 'ゅ', 'ょ'];
// Kun readings that end in one of those kana yet are plain nouns, so there is nothing to
// inflect and nothing to write outside the kanji.
const UNINFLECTED_READINGS = new Set([
  'やまい', 'まつり', 'みやこ', 'いのち', 'かたち', 'ひかり', 'ちから', 'こころ', 'あいだ',
  'うしろ', 'おとうと', 'いもうと', 'ともだち', 'くるま', 'あたま', 'はしら', 'あぶら', 'さくら',
  'ひつじ', 'にじ', 'すじ',
]);

const isPotentiallyInflected = (reading) =>
  reading.length >= 3 &&
  INFLECTION_ENDINGS.includes(reading[reading.length - 1]) &&
  !YOUON.some((small) => reading.includes(small)) &&
  !UNINFLECTED_READINGS.has(reading);

for (const entry of curriculum) {
  observe('kanji entries show okurigana');
  const word = entry.word || entry.k;
  if (!isPotentiallyInflected(entry.r)) continue;
  if (word.length > 1) continue;
  violated(
    'kanji entries show okurigana',
    'an inflected reading is asked for on a bare kanji, so more than one reading fits',
    `${word} -> ${entry.r} (grade ${entry.g})`,
  );
}

// The minute reading rule the app itself encodes: 0/1/3/4/6/8 in the ones place take ぷん.
const expectedMinuteWord = (minute) => ([0, 1, 3, 4, 6, 8].includes(minute % 10) ? 'ぷん' : 'ふん');

const GOJUON = 'あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわん';
// を is deliberately outside the drill: it shares "o" with お and would make two choices correct.
const STANDARD_ROMAJI = { し: 'shi', ち: 'chi', つ: 'tsu', ふ: 'fu' };
const LEGACY_INPUT_ROMAJI = new Set(['si', 'ti', 'tu', 'hu']);
const romajiKanaSeen = new Set();
const romajiSpellingsSeen = new Map();

const COMPARISON_SYMBOLS = new Set(['＞', '＜', '＝']);
// Grade-1 comparison has exactly three possible answers, so three choices is the whole
// answer space rather than a thin distractor pool.
const isComparisonSymbolSet = (choices) =>
  choices.length === 3 && choices.every((choice) => COMPARISON_SYMBOLS.has(choice));

const numbersIn = (text) => (String(text).match(/\d+/g) || []).map(Number);
const TWO_TERM_ADDITION = /^\s*(\d+)\s*[+＋]\s*(\d+)\s*$/;

// --- generate and audit -----------------------------------------------------------------

let generated = 0;
const UNITS = app.curriculumCatalog();
observe('common IME romaji spellings are equivalent', 10);
for (const [standard, ime] of [['shi', 'si'], ['chi', 'ti'], ['tsu', 'tu'], ['fu', 'hu'], ['sha', 'sya'], ['cha', 'tya'], ['ja', 'zya']]) {
  if (!app.romajiInputEquivalent(standard, ime)) {
    violated('common IME romaji spellings are equivalent', `${standard} and ${ime} were treated differently`, `${standard}/${ime}`);
  }
}

// Exercise the real schema migration and lane selection code before auditing generators.
// These checks protect the early-learning contract independently of the registered grade.
app.state = {
  // The real app clamps a saved session length into 10-50 before it is ever used, so a
  // numeric count is part of the settings contract; buildSession sizes its plan from it.
  settings: { count: 10, topics: Object.fromEntries(TOPICS.map((topic) => [topic, true])) },
  session: null,
};
const legacyProfiles = [
  { name: 'complete', grade: 2, stars: 42, xp: 91, mastery: { add: 0.95 }, skillStats: { add: { attempts: 8, independent: 7, retentionStartedAt: 100, masteredAt: 200 } }, cleared: { add: true } },
  { name: 'partial', grade: 2, stars: 7, xp: 13, mastery: { add: 0.6 }, skillStats: { add: { attempts: 5, independent: 3, confidence: 0.6, lastAttemptAt: 300 } }, cleared: {} },
  { name: 'untouched', grade: 1, stars: 0, xp: 0, mastery: {}, skillStats: {}, cleared: {} },
];
const migratedOnce = app.migrateProfiles(legacyProfiles);
const migratedTwice = app.migrateProfiles(migratedOnce);
observe('schema migration is idempotent', legacyProfiles.length);
if (JSON.stringify(migratedOnce) !== JSON.stringify(migratedTwice)) {
  violated('schema migration is idempotent', 'a second migration changed profile data', 'legacy -> v7 -> v7');
}
for (let index = 0; index < legacyProfiles.length; index += 1) {
  const before = legacyProfiles[index];
  const after = migratedOnce[index];
  if (after.learningSchema !== 7 || Object.keys(after.unitStats || {}).length !== UNITS.length) {
    violated('schema migration is idempotent', `${before.name} did not receive one stat per unit`, before.name);
  }
  if (after.stars !== before.stars || after.xp !== before.xp || !after.legacyTopicStats) {
    violated('schema migration preserves evidence', `${before.name} lost stars, XP, or legacy evidence`, before.name);
  }
}
observe('schema migration preserves evidence', legacyProfiles.length);

const revisionUnit = UNITS.find((unit) => unit.generatorKey === 'curriculum-bank') || UNITS[0];
const revisionSourceStat = {
  ...app.blankUnitStat(),
  attempts: 77,
  independent: 61,
  confidence: 0.91,
  retentionStep: 2,
  retentionStartedAt: 1000,
  nextReviewAt: 2000,
  level: 5,
  evidenceWindow: [1, 0, 1],
  recentQuestionFingerprints: ['oldest', 'newest'],
  multiplicationFacts: {
    '7x8': { attempts: 3, independent: 2, errors: 1, strength: 1, lastAttemptAt: 1500, lastOutcome: 'independent' },
    invalid: { attempts: 99, strength: 2 },
  },
};
const revisionProfile = app.migrateProfiles([{
  name: 'revision-change',
  grade: revisionUnit.grade,
  stars: 123,
  xp: 456,
  learningSchema: 7,
  learningCatalogRevision: 'stale-catalog-revision',
  mastery: { [revisionUnit.id]: 0.91 },
  skillStats: {},
  unitStats: { [revisionUnit.id]: revisionSourceStat },
  cleared: {},
}])[0];
const revisionStat = revisionProfile.unitStats[revisionUnit.id];
observe('catalog revision migration preserves unit progression and recent-question state', 1);
if (
  revisionProfile.stars !== 123 ||
  revisionProfile.xp !== 456 ||
  revisionStat.attempts !== 77 ||
  revisionStat.independent !== 61 ||
  revisionStat.retentionStep !== 2 ||
  revisionStat.retentionStartedAt !== 1000 ||
  revisionStat.nextReviewAt !== 2000 ||
  revisionStat.level !== 5 ||
  revisionStat.evidenceWindow.join(',') !== '1,0,1' ||
  revisionStat.recentQuestionFingerprints.join(',') !== 'oldest,newest' ||
  revisionStat.multiplicationFacts['7x8']?.strength !== 1 ||
  Object.hasOwn(revisionStat.multiplicationFacts, 'invalid')
) {
  violated(
    'catalog revision migration preserves unit progression and recent-question state',
    'a catalog update changed saved progression or bounded recent-question state',
    JSON.stringify(revisionStat),
  );
}

const beginnerAtGrade = (grade) => app.ensureLearningProfile({
  name: `grade-${grade}`,
  grade,
  stars: 0,
  xp: 0,
  mastery: {},
  skillStats: {},
  cleared: {},
});

const multiplicationUnit = UNITS.find((unit) => unit.topicId === 'mul' && unit.grade === 2);
observe('multiplication memory curriculum covers every ordered fact', 81);
if (!multiplicationUnit) {
  violated('multiplication memory curriculum covers every ordered fact', 'grade 2 multiplication unit is missing', 'catalog');
} else {
  const expectedStageTables = [[2, 5], [3, 4], [6, 7], [8, 9, 1]];
  const allFacts = new Set(app.multiplicationStageFactKeys(5));
  if (allFacts.size !== 81) {
    violated('multiplication memory curriculum covers every ordered fact', `expected 81 facts, received ${allFacts.size}`, 'stage 5');
  }
  for (let stage = 1; stage <= 4; stage += 1) {
    const keys = app.multiplicationStageFactKeys(stage);
    const expected = expectedStageTables[stage - 1];
    if (keys.length !== expected.length * 9 || keys.some((key) => !expected.includes(Number(key.split('x')[0])))) {
      violated('multiplication memory curriculum covers every ordered fact', `stage ${stage} has the wrong tables`, keys.join(','));
    }
  }

  const multiplicationProfile = beginnerAtGrade(2);
  const multiplicationStat = multiplicationProfile.unitStats[multiplicationUnit.id];
  const firstStageFacts = app.multiplicationStageFactKeys(1);
  const factQuestion = (key, difficulty, outcome = 'independent') => {
    const [a, b] = key.split('x').map(Number);
    app.recordEvidence(multiplicationProfile, {
      topic: 'mul', unitId: multiplicationUnit.id, difficulty, sessionRole: 'check',
      mode: 'num', answer: String(a * b), multiplicationFactKey: key, memoryAssessment: true,
    }, outcome, outcome === 'independent' ? 1 : 0, { userAnswer: String(a * b) });
  };
  for (const key of firstStageFacts.slice(0, -1)) factQuestion(key, 1);
  observe('multiplication stages require fact coverage', firstStageFacts.length);
  if (multiplicationStat.level !== 1) {
    violated('multiplication stages require fact coverage', 'stage advanced before every target fact was recalled', String(multiplicationStat.level));
  }
  factQuestion(firstStageFacts.at(-1), 1);
  if (multiplicationStat.level !== 2) {
    violated('multiplication stages require fact coverage', 'stage did not advance after evidence and full coverage', String(multiplicationStat.level));
  }

  factQuestion('3x7', 2);
  factQuestion('3x7', 2, 'assisted');
  if (multiplicationStat.multiplicationFacts['3x7']?.strength !== 0) {
    violated('missed multiplication facts return to practice', 'an assisted answer did not reset fact strength', JSON.stringify(multiplicationStat.multiplicationFacts['3x7']));
  }
  let weakFactCount = 0;
  for (let sample = 0; sample < 120; sample += 1) {
    const question = app.genFor(multiplicationUnit.id, multiplicationProfile, 2);
    if (question.mode !== 'num' || !question.memoryAssessment || !question.multiplicationFactKey) {
      violated('multiplication recall uses unaided numeric input', 'a stage recall question was not a numeric memory assessment', JSON.stringify(question));
      break;
    }
    if (question.multiplicationFactKey === '3x7') weakFactCount += 1;
  }
  observe('missed multiplication facts return to practice', 120);
  observe('multiplication recall uses unaided numeric input', 120);
  if (weakFactCount < 20) {
    violated('missed multiplication facts return to practice', `weak fact appeared only ${weakFactCount}/120 times`, '3x7');
  }
}

const carryCount = (left, right) => {
  let count = 0;
  let incoming = 0;
  while (left || right || incoming) {
    const total = left % 10 + right % 10 + incoming;
    if (total >= 10) count += 1;
    incoming = total >= 10 ? 1 : 0;
    left = Math.floor(left / 10);
    right = Math.floor(right / 10);
  }
  return count;
};
const borrowCount = (left, right) => {
  let count = 0;
  let borrow = 0;
  while (left || right) {
    const top = left % 10 - borrow;
    const bottom = right % 10;
    borrow = top < bottom ? 1 : 0;
    if (borrow) count += 1;
    left = Math.floor(left / 10);
    right = Math.floor(right / 10);
  }
  return count;
};
const writtenUnits = [2, 3].map((grade) => UNITS.find((unit) => unit.topicId === 'hissan' && unit.grade === grade));
observe('written arithmetic expands by grade and stage', 10);
for (const unit of writtenUnits) {
  if (!unit) {
    violated('written arithmetic expands by grade and stage', 'a grade 2 or 3 written-arithmetic unit is missing', 'catalog');
    continue;
  }
  const profile = beginnerAtGrade(unit.grade);
  for (let stage = 1; stage <= 5; stage += 1) {
    for (let sample = 0; sample < 80; sample += 1) {
      const q = app.genFor(unit.id, profile, stage);
      let valid = q.writtenArithmetic === true && q.difficulty === stage;
      if (unit.grade === 2 && stage === 1) valid &&= q.a >= 10 && q.a < 100 && q.b >= 10 && q.b < 100 && (q.op === '＋' ? carryCount(q.a, q.b) === 0 : borrowCount(q.a, q.b) === 0);
      if (unit.grade === 2 && stage === 2) valid &&= q.op === '＋' && carryCount(q.a, q.b) >= 1;
      if (unit.grade === 2 && stage === 3) valid &&= q.op === '−' && borrowCount(q.a, q.b) >= 1;
      if (unit.grade === 2 && stage >= 4) valid &&= q.a >= 100 && q.b >= 100 && (stage < 5 || (q.op === '＋' ? carryCount(q.a, q.b) >= 2 : borrowCount(q.a, q.b) >= 2));
      if (unit.grade === 3 && stage === 1) valid &&= q.a >= 100 && q.b >= 100 && (q.op === '＋' || q.op === '−');
      if (unit.grade === 3 && stage === 2) valid &&= q.op === 'mul' && q.a >= 10 && q.a < 100 && q.b < 10;
      if (unit.grade === 3 && stage === 3) valid &&= q.op === 'mul' && q.a >= 100 && q.b < 10;
      if (unit.grade === 3 && stage === 4) valid &&= q.op === 'mul' && q.a >= 10 && q.a < 100 && q.b >= 10 && q.b < 100;
      if (!valid) {
        violated('written arithmetic expands by grade and stage', `grade ${unit.grade} stage ${stage} generated the wrong shape`, JSON.stringify(q));
        break;
      }
    }
  }
}
const originalSettings = app.state.settings;
app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: false };
const normalProfiles = [1, 3, 6].map((grade) => beginnerAtGrade(grade));
observe('mode-aware curriculum starts at the correct grade', normalProfiles.length);
for (const profile of normalProfiles) {
  const activeGrade = app.activeCurriculumGrade(profile);
  const frontierGrades = new Set(app.frontierTopics(profile).map((id) => app.curriculumUnit(id).grade));
  if (activeGrade !== 1 || frontierGrades.size !== 1 || !frontierGrades.has(1)) {
    violated('mode-aware curriculum starts at the correct grade', `normal mode registered grade ${profile.grade} did not start at grade 1`, `${activeGrade}: ${[...frontierGrades]}`);
  }
}

const preferenceProfile = beginnerAtGrade(4);
app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: false };
const preferenceOffFrontier = app.frontierTopics(preferenceProfile);
const preferenceStatsBefore = JSON.stringify(preferenceProfile.unitStats);
app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: true };
const preferenceOnFrontier = app.frontierTopics(preferenceProfile);
observe('school-grade preference is optional and reversible', 4);
const belowPreferredGrade = preferenceOnFrontier.filter((id) => app.curriculumUnit(id).grade < 4);
if (belowPreferredGrade.length) {
  violated('school-grade preference is optional and reversible', 'enabled preference still selected units below grade 4', belowPreferredGrade.join(','));
}
const preferredMath = preferenceOnFrontier.find((id) => id.startsWith('math.'));
if (!preferredMath || app.curriculumUnit(preferredMath).grade !== 4) {
  violated('school-grade preference is optional and reversible', 'grade 4 did not begin at the first grade-4 mathematics unit', String(preferredMath));
}
if (app.activeCurriculumGrade(preferenceProfile) !== 4 || preferenceOnFrontier.some((id) => app.curriculumUnit(id).grade !== 4)) {
  violated('mode-aware curriculum starts at the correct grade', 'registered-grade preference did not restrict the first frontier to grade 4', preferenceOnFrontier.join(','));
}
if (JSON.stringify(preferenceProfile.unitStats) !== preferenceStatsBefore) {
  violated('school-grade preference is optional and reversible', 'enabling the preference changed unit progress', preferenceProfile.name);
}
app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: false };
const preferenceRestoredFrontier = app.frontierTopics(preferenceProfile);
if (JSON.stringify(preferenceRestoredFrontier) !== JSON.stringify(preferenceOffFrontier)) {
  violated('school-grade preference is optional and reversible', 'turning the preference off did not restore the original frontier', `${preferenceRestoredFrontier} vs ${preferenceOffFrontier}`);
}
app.state.settings = originalSettings;
const supportProfile = beginnerAtGrade(1);
const supportUnit = UNITS[0];
supportProfile.unitStats[supportUnit.id] = { ...app.blankUnitStat(), level: 5, confidence: 0.85 };
supportProfile.mastery[supportUnit.id] = 0.85;
app.state.session = { supportTopics: {} };
const supportQuestion = { unitId: supportUnit.id, topic: supportUnit.topicId, difficulty: 5, sessionRole: 'target', answer: 'x' };
app.recordEvidence(supportProfile, supportQuestion, 'incorrect', 0, { userAnswer: 'wrong' });
const supportStageAfterFirstBlock = app.sessionStage(supportProfile, app.state.session, supportUnit.id, 'target');
app.recordEvidence(supportProfile, { ...supportQuestion, difficulty: 4 }, 'incorrect', 0, { userAnswer: 'wrong-again' });
const supportStageAfterSecondBlock = app.sessionStage(supportProfile, app.state.session, supportUnit.id, 'target');
app.recordEvidence(supportProfile, { ...supportQuestion, difficulty: 3 }, 'independent', 1, { userAnswer: 'x' });
const supportStageAfterIndependent = app.sessionStage(supportProfile, app.state.session, supportUnit.id, 'target');
observe('support ladder is bounded and clears after independent success', 3);
if (supportStageAfterFirstBlock !== 4 || supportStageAfterSecondBlock !== 3 || supportStageAfterIndependent !== 5) {
  violated('support ladder is bounded and clears after independent success', 'repeated blocks did not lower two stages or an independent success did not restore the target stage', `${supportStageAfterFirstBlock}/${supportStageAfterSecondBlock}/${supportStageAfterIndependent}`);
}
app.state.session = null;

// Progression is stateful, so exercise the generated runtime itself rather than asserting
// markers in the C# source. The catalog intentionally includes both full and sparse banks.
const bankUnits = UNITS.filter((unit) => unit.generatorKey === 'curriculum-bank');
const sparseBank = bankUnits.find((unit) => app.unitStages(unit).join(',') === '1,3,5');
const fullBank = bankUnits.find((unit) => app.unitStages(unit).join(',') === '1,2,3,4,5');
observe('curriculum banks expose stable authored stage selections', sparseBank && fullBank ? 2 : 0);
if (!sparseBank || !fullBank) {
  violated(
    'curriculum banks expose stable authored stage selections',
    'the audit needs both a sparse 1/3/5 bank and a full 1/2/3/4/5 bank',
    bankUnits.map((unit) => unit.id + ':' + app.unitStages(unit).join('/')).join(', '),
  );
}

if (sparseBank && fullBank) {
  const freshUnitProfile = (unit, level = 1) => {
    const profile = beginnerAtGrade(unit.grade);
    profile.unitStats[unit.id] = { ...app.blankUnitStat(), level, confidence: 0.85 };
    profile.mastery[unit.id] = 0.85;
    return profile;
  };
  const evidenceSession = () => ({ correct: 0, targetAsked: 0, targetIndependent: 0, supportTopics: {} });
  const evidenceQuestion = (unit, difficulty, sessionRole) => ({
    unitId: unit.id,
    topic: unit.topicId,
    difficulty,
    sessionRole,
    answer: 'audit',
  });
  const record = (profile, unit, difficulty, role, outcome = 'independent') =>
    app.recordEvidence(profile, evidenceQuestion(unit, difficulty, role), outcome, outcome === 'independent' ? 1 : 0, { userAnswer: 'audit' });
  const authoredAt = (unit, stage, question) =>
    (unit.questions || []).some(
      (item) => Number(item.stage) === stage && item.prompt === question.prompt && item.answer === question.answer,
    );
  const qualifyCurrentStage = (profile, unit, role = 'check') => {
    const stage = app.topicStage(profile, unit.id);
    const requirement = app.stageEvidenceRequired(stage);
    for (let index = 0; index < requirement.attempts; index += 1) record(profile, unit, stage, role);
    return stage;
  };

  const sparseGenerationProfile = freshUnitProfile(sparseBank);
  const sparseRequests = [1, 2, 3, 4, 5];
  const sparseExpected = [1, 1, 3, 3, 5];
  observe('sparse curriculum-bank generation retains authored stages', sparseRequests.length);
  for (let index = 0; index < sparseRequests.length; index += 1) {
    const requested = sparseRequests[index];
    const expected = sparseExpected[index];
    const question = app.genFor(sparseBank.id, sparseGenerationProfile, requested);
    if (question.difficulty !== expected || !authoredAt(sparseBank, expected, question)) {
      violated(
        'sparse curriculum-bank generation retains authored stages',
        'request ' + requested + ' produced difficulty ' + question.difficulty + ' instead of authored stage ' + expected,
        sparseBank.id + ': ' + JSON.stringify(question),
      );
    }
  }

  const supportGenerationProfile = freshUnitProfile(sparseBank, 3);
  const supportGenerationSession = evidenceSession();
  supportGenerationProfile.unitStats[sparseBank.id].supportDepth = 1;
  const supportStage = app.sessionStage(supportGenerationProfile, supportGenerationSession, sparseBank.id, 'target');
  const supportQuestionAtStage = app.genFor(sparseBank.id, supportGenerationProfile, supportStage);
  supportGenerationProfile.unitStats[sparseBank.id].nextReviewAt = Date.now() - 1;
  const reviewStage = app.sessionStage(supportGenerationProfile, supportGenerationSession, sparseBank.id, 'review');
  const reviewQuestionAtStage = app.genFor(sparseBank.id, supportGenerationProfile, reviewStage);
  observe('support and review generation use canonical authored stages', 2);
  if (
    supportStage !== 1 ||
    supportQuestionAtStage.difficulty !== 1 ||
    !authoredAt(sparseBank, 1, supportQuestionAtStage) ||
    reviewStage !== 1 ||
    reviewQuestionAtStage.difficulty !== 1 ||
    !authoredAt(sparseBank, 1, reviewQuestionAtStage)
  ) {
    violated(
      'support and review generation use canonical authored stages',
      'support/review were ' + supportStage + '/' + reviewStage + ' with question difficulties ' + supportQuestionAtStage.difficulty + '/' + reviewQuestionAtStage.difficulty,
      sparseBank.id,
    );
  }

  const savedStageCases = [
    { saved: 2, current: 1, next: 3 },
    { saved: 4, current: 3, next: 5 },
  ];
  observe('saved sparse-bank levels canonicalize without migration', savedStageCases.length);
  for (const stageCase of savedStageCases) {
    const profile = freshUnitProfile(sparseBank, stageCase.saved);
    const stat = profile.unitStats[sparseBank.id];
    const beforeLevel = stat.level;
    const beforeSchema = profile.learningSchema;
    const question = app.genFor(sparseBank.id, profile);
    app.state.session = evidenceSession();
    const qualified = qualifyCurrentStage(profile, sparseBank);
    if (
      beforeSchema !== 7 ||
      beforeLevel !== stageCase.saved ||
      qualified !== stageCase.current ||
      question.difficulty !== stageCase.current ||
      stat.level !== stageCase.next
    ) {
      violated(
        'saved sparse-bank levels canonicalize without migration',
        'saved ' + stageCase.saved + ' became current ' + qualified + ', question ' + question.difficulty + ', stored ' + stat.level + ', schema ' + beforeSchema,
        sparseBank.id,
      );
    }
  }

  const fullProgressProfile = freshUnitProfile(fullBank);
  app.state.session = evidenceSession();
  const fullProgression = [1, 2, 3, 4, 5].map(() => qualifyCurrentStage(fullProgressProfile, fullBank));
  observe('full curriculum banks progress through every authored stage', fullProgression.length);
  if (
    fullProgression.join(',') !== '1,2,3,4,5' ||
    !fullProgressProfile.unitStats[fullBank.id].retentionStartedAt
  ) {
    violated(
      'full curriculum banks progress through every authored stage',
      'progression ' + fullProgression.join(',') + ' retention=' + fullProgressProfile.unitStats[fullBank.id].retentionStartedAt,
      fullBank.id,
    );
  }

  const sparseProgressProfile = freshUnitProfile(sparseBank);
  app.state.session = evidenceSession();
  const sparseProgression = [1, 3, 5].map(() => qualifyCurrentStage(sparseProgressProfile, sparseBank));
  const sparseProgressStat = sparseProgressProfile.unitStats[sparseBank.id];
  observe('sparse curriculum banks progress only through authored stages', sparseProgression.length);
  if (sparseProgression.join(',') !== '1,3,5' || !sparseProgressStat.retentionStartedAt) {
    violated(
      'sparse curriculum banks progress only through authored stages',
      'progression ' + sparseProgression.join(',') + ' retention=' + sparseProgressStat.retentionStartedAt,
      sparseBank.id,
    );
  }

  const mixedEvidenceProfile = freshUnitProfile(sparseBank);
  app.state.session = evidenceSession();
  record(mixedEvidenceProfile, sparseBank, 1, 'target');
  record(mixedEvidenceProfile, sparseBank, 1, 'mixed');
  record(mixedEvidenceProfile, sparseBank, 1, 'check', 'assisted');
  record(mixedEvidenceProfile, sparseBank, 1, 'exit');
  const mixedEvidenceStat = mixedEvidenceProfile.unitStats[sparseBank.id];
  const mixedEvidenceSession = app.state.session;
  observe('aligned mixed questions supply evidence but not target quota', 4);
  if (
    mixedEvidenceStat.level !== 3 ||
    mixedEvidenceSession.targetAsked !== 2 ||
    mixedEvidenceSession.targetIndependent !== 2
  ) {
    violated(
      'aligned mixed questions supply evidence but not target quota',
      'level=' + mixedEvidenceStat.level + ', targetAsked=' + mixedEvidenceSession.targetAsked + ', targetIndependent=' + mixedEvidenceSession.targetIndependent,
      sparseBank.id,
    );
  }

  const finalIndependentProfile = freshUnitProfile(sparseBank);
  app.state.session = evidenceSession();
  record(finalIndependentProfile, sparseBank, 1, 'check');
  record(finalIndependentProfile, sparseBank, 1, 'check');
  record(finalIndependentProfile, sparseBank, 1, 'check');
  record(finalIndependentProfile, sparseBank, 1, 'mixed', 'assisted');
  const finalIndependentStat = finalIndependentProfile.unitStats[sparseBank.id];
  observe('only an independent final item can promote a stage', 4);
  if (
    finalIndependentStat.level !== 1 ||
    finalIndependentStat.evidenceWindow.join(',') !== '1,1,1,0' ||
    app.stageEvidenceReady(finalIndependentStat, 1)
  ) {
    violated(
      'only an independent final item can promote a stage',
      'level=' + finalIndependentStat.level + ', evidence=' + finalIndependentStat.evidenceWindow.join(','),
      sparseBank.id,
    );
  }

  const terminalOutcomeProfile = freshUnitProfile(sparseBank);
  app.state.session = evidenceSession();
  record(terminalOutcomeProfile, sparseBank, 1, 'target');
  record(terminalOutcomeProfile, sparseBank, 1, 'mixed', 'assisted');
  record(terminalOutcomeProfile, sparseBank, 1, 'check', 'incorrect');
  record(terminalOutcomeProfile, sparseBank, 1, 'exit', 'revealed');
  const terminalOutcomeStat = terminalOutcomeProfile.unitStats[sparseBank.id];
  const terminalOutcomeSession = app.state.session;
  observe('non-independent terminal outcomes append zero evidence', 4);
  if (
    terminalOutcomeStat.level !== 1 ||
    terminalOutcomeStat.stageAttempts !== 4 ||
    terminalOutcomeStat.stageIndependent !== 1 ||
    terminalOutcomeStat.evidenceWindow.join(',') !== '1,0,0,0' ||
    terminalOutcomeSession.targetAsked !== 2 ||
    terminalOutcomeSession.targetIndependent !== 1
  ) {
    violated(
      'non-independent terminal outcomes append zero evidence',
      'level=' + terminalOutcomeStat.level + ', attempts=' + terminalOutcomeStat.stageAttempts + ', independent=' + terminalOutcomeStat.stageIndependent + ', evidence=' + terminalOutcomeStat.evidenceWindow.join(',') + ', quota=' + terminalOutcomeSession.targetAsked + '/' + terminalOutcomeSession.targetIndependent,
      sparseBank.id,
    );
  }

  for (const invalidRole of [undefined, 'unexpected']) {
    const profile = freshUnitProfile(sparseBank, 3);
    const stat = profile.unitStats[sparseBank.id];
    stat.supportDepth = 1;
    stat.consecutiveBlocks = 1;
    app.state.session = evidenceSession();
    app.state.session.supportTopics[sparseBank.id] = 1;
    record(profile, sparseBank, 3, invalidRole, 'incorrect');
    record(profile, sparseBank, 3, invalidRole);
    observe('missing and unknown roles fail closed for evidence support and quota', 2);
    if (
      stat.stageAttempts !== 0 ||
      stat.evidenceWindow.length !== 0 ||
      stat.supportDepth !== 1 ||
      stat.consecutiveBlocks !== 1 ||
      app.state.session.targetAsked !== 0 ||
      app.state.session.targetIndependent !== 0 ||
      app.state.session.supportTopics[sparseBank.id] !== 1
    ) {
      violated(
        'missing and unknown roles fail closed for evidence support and quota',
        String(invalidRole) + ' changed evidence=' + stat.evidenceWindow.join(',') + ' support=' + stat.supportDepth + '/' + stat.consecutiveBlocks + ' quota=' + app.state.session.targetAsked + '/' + app.state.session.targetIndependent,
        sparseBank.id,
      );
    }
  }

  const reviewEvidenceProfile = freshUnitProfile(sparseBank, 3);
  const reviewEvidenceStat = reviewEvidenceProfile.unitStats[sparseBank.id];
  reviewEvidenceStat.supportDepth = 1;
  reviewEvidenceStat.consecutiveBlocks = 1;
  app.state.session = evidenceSession();
  app.state.session.supportTopics[sparseBank.id] = 1;
  record(reviewEvidenceProfile, sparseBank, 3, 'review');
  observe('review never mutates ordinary evidence or support', 1);
  if (
    reviewEvidenceStat.stageAttempts !== 0 ||
    reviewEvidenceStat.evidenceWindow.length !== 0 ||
    reviewEvidenceStat.supportDepth !== 1 ||
    reviewEvidenceStat.consecutiveBlocks !== 1 ||
    app.state.session.targetAsked !== 0 ||
    app.state.session.supportTopics[sparseBank.id] !== 1
  ) {
    violated(
      'review never mutates ordinary evidence or support',
      'evidence=' + reviewEvidenceStat.evidenceWindow.join(',') + ' support=' + reviewEvidenceStat.supportDepth + '/' + reviewEvidenceStat.consecutiveBlocks + ' quota=' + app.state.session.targetAsked,
      sparseBank.id,
    );
  }

  const bridgeProfile = freshUnitProfile(sparseBank, 3);
  const bridgeStat = bridgeProfile.unitStats[sparseBank.id];
  bridgeStat.supportDepth = 1;
  app.state.session = evidenceSession();
  app.state.session.supportTopics[sparseBank.id] = 1;
  record(bridgeProfile, sparseBank, 1, 'target');
  const bridgeAfterIndependent = bridgeStat.supportDepth;
  record(bridgeProfile, sparseBank, 1, 'target', 'incorrect');
  const bridgeAfterFailure = bridgeStat.supportDepth;
  record(bridgeProfile, sparseBank, 1, 'unexpected', 'incorrect');
  observe('lower-stage bridges stay outside current-stage evidence', 3);
  if (
    bridgeStat.stageAttempts !== 0 ||
    bridgeStat.evidenceWindow.length !== 0 ||
    bridgeAfterIndependent !== 0 ||
    bridgeAfterFailure !== 1 ||
    bridgeStat.supportDepth !== 1 ||
    app.state.session.supportTopics[sparseBank.id] !== 1
  ) {
    violated(
      'lower-stage bridges stay outside current-stage evidence',
      'evidence=' + bridgeStat.evidenceWindow.join(',') + ' support=' + bridgeAfterIndependent + '/' + bridgeAfterFailure + '/' + bridgeStat.supportDepth,
      sparseBank.id,
    );
  }

  sparseProgressStat.nextReviewAt = Date.now() - 1;
  app.state.session = evidenceSession();
  record(sparseProgressProfile, sparseBank, 5, 'review');
  observe('retention review remains separate from ordinary stage evidence', 1);
  if (
    sparseProgressStat.retentionStep !== 1 ||
    sparseProgressStat.stageAttempts !== 0 ||
    sparseProgressStat.evidenceWindow.length !== 0
  ) {
    violated(
      'retention review remains separate from ordinary stage evidence',
      'retention=' + sparseProgressStat.retentionStep + ', stageAttempts=' + sparseProgressStat.stageAttempts + ', evidence=' + sparseProgressStat.evidenceWindow.join(','),
      sparseBank.id,
    );
  }

  const retainedStepBeforeOrdinaryQuestion = sparseProgressStat.retentionStep;
  const retainedReviewBeforeOrdinaryQuestion = sparseProgressStat.nextReviewAt;
  record(sparseProgressProfile, sparseBank, 5, 'mixed', 'assisted');
  observe('only a scheduled due review can mutate retention progress', 1);
  if (
    sparseProgressStat.retentionStep !== retainedStepBeforeOrdinaryQuestion ||
    sparseProgressStat.nextReviewAt !== retainedReviewBeforeOrdinaryQuestion
  ) {
    violated(
      'only a scheduled due review can mutate retention progress',
      'an ordinary assisted question reset retention progress or its schedule',
      'retention=' + sparseProgressStat.retentionStep + ', next=' + sparseProgressStat.nextReviewAt,
    );
  }

  // Passing the last stage only starts retention. The star is earned by the scheduled
  // reviews, so the audit drives that chain to its end -- a unit that could never be
  // cleared would leave the child stuck on a finished topic forever.
  const CLEAR_CHECK = 'scheduled reviews carry a finished unit all the way to cleared';
  const clearSteps = [];
  for (let round = 0; round < 6 && !sparseProgressStat.masteredAt; round += 1) {
    sparseProgressStat.nextReviewAt = Date.now() - 1;
    app.state.session = evidenceSession();
    record(sparseProgressProfile, sparseBank, app.terminalUnitStage(sparseBank.id), 'review');
    clearSteps.push(sparseProgressStat.retentionStep);
  }
  observe(CLEAR_CHECK, clearSteps.length);
  if (
    !sparseProgressStat.masteredAt ||
    !(sparseProgressProfile.cleared || {})[sparseBank.id] ||
    !app.topicReady(sparseProgressProfile, sparseBank.id)
  ) {
    violated(
      CLEAR_CHECK,
      'confirmations ' + clearSteps.join(',') + ' left mastered=' + !!sparseProgressStat.masteredAt +
        ', cleared=' + !!(sparseProgressProfile.cleared || {})[sparseBank.id] +
        ', ready=' + app.topicReady(sparseProgressProfile, sparseBank.id),
      sparseBank.id,
    );
  }

  const RELAPSE_CHECK = 'a missed retention review restarts the confirmations without taking the star back';
  sparseProgressStat.nextReviewAt = Date.now() - 1;
  app.state.session = evidenceSession();
  record(sparseProgressProfile, sparseBank, app.terminalUnitStage(sparseBank.id), 'review', 'incorrect');
  observe(RELAPSE_CHECK, 1);
  if (
    sparseProgressStat.retentionStep !== 0 ||
    !sparseProgressStat.masteredAt ||
    !(sparseProgressProfile.cleared || {})[sparseBank.id] ||
    app.topicReady(sparseProgressProfile, sparseBank.id)
  ) {
    violated(
      RELAPSE_CHECK,
      'retention=' + sparseProgressStat.retentionStep + ', cleared=' + !!(sparseProgressProfile.cleared || {})[sparseBank.id] +
        ', ready=' + app.topicReady(sparseProgressProfile, sparseBank.id),
      sparseBank.id,
    );
  }

  // The confirmations can only arrive as due review questions, so the session planner has
  // to keep offering the retained unit at its terminal stage.
  const REVIEW_REACH_CHECK = 'a unit in retention is still offered as a due review at its terminal stage';
  const fullRetentionStat = fullProgressProfile.unitStats[fullBank.id];
  fullRetentionStat.nextReviewAt = Date.now() - 1;
  const retentionSession = {
    reviewTopics: app.dueTopics(fullProgressProfile).slice(),
    targetTopics: [],
    activeTargetTopic: null,
    supportTopics: {},
    questionCounts: {},
    lastQuestionKey: '',
  };
  // "When do I get the star?" is an acceptance question, not a detail. The answer has to be "after
  // three more sessions", never "tomorrow": no confirmation may sit behind a clock.
  const ONE_DAY_CHECK = 'the confirmations that earn a star are open immediately, not on a timer';
  const oneDayProfile = freshUnitProfile(fullBank);
  const oneDayStat = oneDayProfile.unitStats[fullBank.id];
  app.state.session = evidenceSession();
  for (let stage = 0; stage < 5; stage += 1) qualifyCurrentStage(oneDayProfile, fullBank);
  const clearWaits = [];
  for (let round = 0; round < 6 && !oneDayStat.masteredAt; round += 1) {
    clearWaits.push(Math.max(0, Number(oneDayStat.nextReviewAt) - Date.now()));
    app.state.session = evidenceSession();
    record(oneDayProfile, fullBank, app.terminalUnitStage(fullBank.id), 'review');
  }
  const totalWait = clearWaits.reduce((sum, wait) => sum + wait, 0);
  observe(ONE_DAY_CHECK, clearWaits.length);
  if (!oneDayStat.masteredAt || totalWait !== 0) {
    violated(
      ONE_DAY_CHECK,
      oneDayStat.masteredAt
        ? 'the star sat behind ' + Math.round(totalWait / 60000) + ' minutes of waiting over gaps ' + clearWaits.join('/') + ' ms'
        : 'the star never arrived in ' + clearWaits.length + ' straight confirmations',
      fullBank.id,
    );
  }

  const offeredTopic = app.sessionTopic(fullProgressProfile, retentionSession, 'review');
  const offeredStage = app.sessionStage(fullProgressProfile, retentionSession, fullBank.id, 'review');
  observe(REVIEW_REACH_CHECK, 2);
  if (offeredTopic !== fullBank.id || offeredStage !== app.terminalUnitStage(fullBank.id)) {
    violated(
      REVIEW_REACH_CHECK,
      'the review slot offered ' + offeredTopic + ' at stage ' + offeredStage + ' instead of ' + fullBank.id + ' at stage ' + app.terminalUnitStage(fullBank.id),
      fullBank.id,
    );
  }

  // With the clock out of the way, the one-review-per-session splice is the entire spacing left
  // between confirmations. If a session ever handed out two, the star would collapse into one sitting.
  const SPACING_CHECK = 'one session confirms a retained unit at most once';
  const repeatOffer = app.sessionTopic(fullProgressProfile, retentionSession, 'review');
  observe(SPACING_CHECK, 1);
  if (repeatOffer === fullBank.id) {
    violated(SPACING_CHECK, 'the same session offered ' + fullBank.id + ' a second confirmation', fullBank.id);
  }
}
app.state.session = null;

// ——— Session pacing. Switching task types every item drains attention, so scatter is a defect. ———

// Reviews, then mixed work, then a contiguous target block, then the exit check.
// The session must not open on the new unit when mixed work exists.
const CLUSTER_CHECK = 'target practice is a contiguous block after mixed work, not spread through the session';
const paceProfile = () => app.ensureLearningProfile({ name: 'pace', grade: 1, stars: 0, xp: 0, mastery: {}, skillStats: {}, cleared: {} });
const clusterPlan = app.buildSession(paceProfile(), 1).rolePlan;
const targetPositions = clusterPlan.map((role, index) => (role === 'target' ? index : -1)).filter((index) => index >= 0);
const expectedTargets = Math.max(4, Math.floor(clusterPlan.length * 0.25)) - 1;
const firstTarget = targetPositions[0];
const lastTarget = targetPositions[targetPositions.length - 1];
const contiguousTargets = targetPositions.length > 0 && lastTarget - firstTarget + 1 === targetPositions.length;
const mixedCount = clusterPlan.filter((role) => role === 'mixed').length;
const prefixBeforeTargets = targetPositions.length ? clusterPlan.slice(0, firstTarget) : clusterPlan.slice(0, -1);
const prefixIsWarmup = prefixBeforeTargets.every((role) => role === 'review' || role === 'mixed');
observe(CLUSTER_CHECK, 1);
if (
  clusterPlan[clusterPlan.length - 1] !== 'exit' ||
  (mixedCount > 0 && clusterPlan[0] === 'target') ||
  !contiguousTargets ||
  !prefixIsWarmup ||
  targetPositions.length !== expectedTargets
) {
  violated(CLUSTER_CHECK, 'role plan came out as ' + clusterPlan.join(','), 'buildSession');
}

const simulateSession = (profile) => {
  const session = app.buildSession(profile, 1);
  app.state.session = session;
  for (let index = 0; index < session.rolePlan.length; index += 1) {
    app.recordEvidence(profile, session.questions[index], 'independent', 1, { userAnswer: 'audit' });
    session.idx = index;
    if (index + 1 < session.rolePlan.length) session.questions.push(app.generateSessionQuestion(profile, session, session.rolePlan[index + 1]));
  }
  app.state.session = null;
  return session;
};
const mixedRunCount = (questions) => {
  const units = questions.filter((question) => question.sessionRole === 'mixed').map((question) => question.unitId || question.topic);
  if (!units.length) return { length: 0, runs: 0 };
  let runs = 1;
  for (let index = 1; index < units.length; index += 1) if (units[index] !== units[index - 1]) runs += 1;
  return { length: units.length, runs };
};

const MIXED_CLUSTER_CHECK = 'mixed practice keeps the same unit together instead of switching every question';
const clusterRounds = 12;
let scatteredSessions = 0;
for (let round = 0; round < clusterRounds; round += 1) {
  const mixed = mixedRunCount(simulateSession(paceProfile()).questions);
  if (mixed.length >= 6 && mixed.runs > Math.ceil(mixed.length / 3)) scatteredSessions += 1;
}
observe(MIXED_CLUSTER_CHECK, clusterRounds);
if (scatteredSessions > 0) {
  violated(MIXED_CLUSTER_CHECK, scatteredSessions + ' of ' + clusterRounds + ' sessions still scattered mixed units', 'generateSessionQuestion');
}

const ENDGAME_CHECK = 'a two-unit endgame masses each unit instead of alternating every question';
const endgameProfile = app.ensureLearningProfile({ name: 'endgame', grade: 1, stars: 0, xp: 0, mastery: {}, skillStats: {}, cleared: {} });
const gradeOneUnits = app.curriculumCatalog().filter((unit) => unit.grade === 1);
for (const [index, unit] of gradeOneUnits.entries()) {
  const keepFresh = index >= gradeOneUnits.length - 2;
  endgameProfile.unitStats[unit.id] = keepFresh
    ? { ...app.blankUnitStat(), level: 2, confidence: 0.5, attempts: 4, independent: 3 }
    : { ...app.blankUnitStat(), level: app.terminalUnitStage(unit.id), confidence: 0.9, attempts: 12, independent: 11, retentionStartedAt: Date.now(), retentionStep: 0, nextReviewAt: Date.now() + 86400000 };
  endgameProfile.mastery[unit.id] = keepFresh ? 0.5 : 0.9;
}
const endgameSession = simulateSession(endgameProfile);
const endgameUnits = endgameSession.questions.map((question) => question.unitId || question.topic);
let endgameRun = 1;
let endgameMaxRun = 1;
for (let index = 1; index < endgameUnits.length; index += 1) {
  endgameRun = endgameUnits[index] === endgameUnits[index - 1] ? endgameRun + 1 : 1;
  endgameMaxRun = Math.max(endgameMaxRun, endgameRun);
}
const endgameMixed = mixedRunCount(endgameSession.questions);
observe(ENDGAME_CHECK, 1);
if (endgameMaxRun <= 2 || new Set(endgameUnits).size < 2 || (endgameMixed.length >= 4 && endgameMixed.runs > 2)) {
  violated(ENDGAME_CHECK, 'unit order was ' + endgameUnits.map((id) => id.split('.').pop()).join(',') + ' (longest run ' + endgameMaxRun + ')', 'buildSession');
}

const mathLane = app.curriculumLaneIds().find((lane) => lane.some((id) => id.startsWith('math.')));
const gradeOneBeginner = normalProfiles[0];
const gradeSixBeginner = normalProfiles[2];
observe('question generation uses unit grade, not school grade', 2);
if (mathLane) {
  const firstMathGrade = app.curriculumUnit(mathLane[0]).grade;
  const lastMathGrade = app.curriculumUnit(mathLane[mathLane.length - 1]).grade;
  if (app.profileAtStage(gradeSixBeginner, mathLane[0], 1).grade !== firstMathGrade) {
    violated('question generation uses unit grade, not school grade', 'a grade-6 registration changed the first mathematics unit generator grade', mathLane[0]);
  }
  if (app.profileAtStage(gradeOneBeginner, mathLane[mathLane.length - 1], 1).grade !== lastMathGrade) {
    violated('question generation uses unit grade, not school grade', 'a grade-1 registration changed the last mathematics unit generator grade', mathLane[mathLane.length - 1]);
  }
}

app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: false };
const cohortProfile = beginnerAtGrade(3);
const completeUnits = (profile, ids) => {
  for (const id of ids) profile.unitStats[id].retentionStartedAt = 1;
};
const topology = app.curriculumTopology(cohortProfile);
const gradeIds = (grade) => topology.unitIdsByGrade.get(grade) || [];
const mathIdsAtGrade = (grade) => gradeIds(grade).filter((id) => id.startsWith('math.'));
observe('grade cohorts unlock sequentially', 6);
completeUnits(cohortProfile, mathIdsAtGrade(1));
if (app.activeCurriculumGrade(cohortProfile) !== 1) {
  violated('grade cohorts unlock sequentially', 'finishing grade-1 mathematics unlocked grade 2 before the other subjects', app.activeCurriculumGrade(cohortProfile));
}
completeUnits(cohortProfile, gradeIds(7));
if (app.activeCurriculumGrade(cohortProfile) !== 1) {
  violated('grade cohorts unlock sequentially', 'historical grade-7 evidence bypassed the active grade', app.activeCurriculumGrade(cohortProfile));
}
completeUnits(cohortProfile, gradeIds(1));
if (app.activeCurriculumGrade(cohortProfile) !== 2 || app.frontierTopics(cohortProfile).some((id) => app.curriculumUnit(id).grade !== 2)) {
  violated('grade cohorts unlock sequentially', 'completing grade 1 did not unlock only grade 2', app.frontierTopics(cohortProfile).join(','));
}
completeUnits(cohortProfile, gradeIds(2));
if (app.activeCurriculumGrade(cohortProfile) !== 3) {
  violated('grade cohorts unlock sequentially', 'completing grade 2 did not unlock grade 3', app.activeCurriculumGrade(cohortProfile));
}
completeUnits(cohortProfile, mathIdsAtGrade(3));
if (app.activeCurriculumGrade(cohortProfile) !== 3) {
  violated('grade cohorts unlock sequentially', 'finishing grade-3 mathematics unlocked grade 4 before the other subjects', app.activeCurriculumGrade(cohortProfile));
}
completeUnits(cohortProfile, gradeIds(3));
if (app.activeCurriculumGrade(cohortProfile) !== 4) {
  violated('grade cohorts unlock sequentially', 'completing grade 3 did not unlock grade 4', app.activeCurriculumGrade(cohortProfile));
}

const disabledProfile = beginnerAtGrade(1);
const disabledTopic = app.curriculumCatalog().find((unit) => unit.grade === 1).topicId;
app.state.settings = {
  ...app.defaultSettings(),
  preferSchoolGrade: false,
  topics: { ...app.defaultSettings().topics, [disabledTopic]: false },
};
const disabledTopology = app.curriculumTopology(disabledProfile);
completeUnits(disabledProfile, disabledTopology.unitIdsByGrade.get(1) || []);
observe('disabled units do not block grade completion');
if (app.activeCurriculumGrade(disabledProfile) !== 2) {
  violated('disabled units do not block grade completion', `disabling ${disabledTopic} still blocked grade 1`, app.activeCurriculumGrade(disabledProfile));
}
app.state.settings = originalSettings;

let prerequisitePair = null;
for (const lane of app.curriculumLaneIds()) {
  for (let index = 1; index < lane.length; index += 1) {
    const prerequisite = app.curriculumUnit(lane[index - 1]);
    const dependent = app.curriculumUnit(lane[index]);
    if (prerequisite.grade === 1 && dependent.grade === 1) {
      prerequisitePair = { lane, index, prerequisite, dependent };
      break;
    }
  }
  if (prerequisitePair) break;
}
observe('retention completion advances the frontier without hijacking review selection', prerequisitePair ? 2 : 0);
if (!prerequisitePair) {
  violated(
    'retention completion advances the frontier without hijacking review selection',
    'the audit could not find adjacent grade-one prerequisite units',
    app.curriculumLaneIds().map((lane) => lane.join('>')).join(' | '),
  );
} else {
  const { lane, index, prerequisite, dependent } = prerequisitePair;
  const reviewProfile = beginnerAtGrade(1);
  const reviewDependentStat = reviewProfile.unitStats[dependent.id];
  reviewDependentStat.attempts = 2;
  reviewDependentStat.confidence = 0.2;
  reviewDependentStat.nextReviewAt = Date.now() - 1;
  const reviewSession = {
    activeTargetTopic: dependent.id,
    targetTopics: [dependent.id],
    reviewTopics: [dependent.id],
    supportTopics: {},
    questionCounts: {},
    lastQuestionKey: '',
  };
  const selectedReview = app.sessionTopic(reviewProfile, reviewSession, 'review');
  if (selectedReview !== dependent.id || reviewSession.reviewTopics.length !== 0) {
    violated(
      'retention completion advances the frontier without hijacking review selection',
      'a due review slot was substituted with a prerequisite or was not consumed',
      selectedReview + ' / remaining=' + reviewSession.reviewTopics.join(','),
    );
  }

  const frontierProfile = beginnerAtGrade(1);
  for (let priorIndex = 0; priorIndex < index; priorIndex += 1) {
    const priorStat = frontierProfile.unitStats[lane[priorIndex]];
    priorStat.level = 5;
    priorStat.retentionStartedAt = 1000 + priorIndex;
    priorStat.retentionStep = 0;
    priorStat.nextReviewAt = Date.now() + 86400000;
  }
  const frontierDependentStat = frontierProfile.unitStats[dependent.id];
  frontierDependentStat.attempts = 2;
  frontierDependentStat.confidence = 0.2;
  const remediation = app.remediationTopics(frontierProfile, dependent.id);
  const frontier = app.frontierTopics(frontierProfile);
  if (
    !app.topicComplete(frontierProfile, prerequisite.id) ||
    remediation.includes(prerequisite.id) ||
    !frontier.includes(dependent.id)
  ) {
    violated(
      'retention completion advances the frontier without hijacking review selection',
      'a prerequisite in retention still blocked or replaced its dependent frontier unit',
      'remediation=' + remediation.join(',') + ' frontier=' + frontier.join(','),
    );
  }
}

const thinkingGradeOne = UNITS.find((unit) => unit.topicId === 'thinking' && unit.grade === 1);
observe('reasoning questions rotate across sessions and stay balanced at the 30-question limit', thinkingGradeOne ? 36 : 0);
if (!thinkingGradeOne) {
  violated(
    'reasoning questions rotate across sessions and stay balanced at the 30-question limit',
    'the grade-one reasoning unit is missing',
    'thinking grade 1',
  );
} else {
  const reasoningSession = () => ({
    questions: [],
    rolePlan: [],
    idx: 0,
    correct: 0,
    activeTargetTopic: thinkingGradeOne.id,
    targetTopics: [thinkingGradeOne.id],
    targetAsked: 0,
    targetIndependent: 0,
    reviewTopics: [],
    supportTopics: {},
    questionCounts: {},
    lastQuestionKey: '',
    attempt: 1,
    startStars: 0,
    startXp: 0,
  });

  const crossSessionProfile = beginnerAtGrade(1);
  const crossSessionPrompts = [];
  for (let index = 0; index < 6; index += 1) {
    crossSessionPrompts.push(app.generateSessionQuestion(crossSessionProfile, reasoningSession(), 'target').prompt);
  }
  const recycledPrompt = app.generateSessionQuestion(crossSessionProfile, reasoningSession(), 'target').prompt;
  const recentFingerprints = crossSessionProfile.unitStats[thinkingGradeOne.id].recentQuestionFingerprints;
  if (
    new Set(crossSessionPrompts).size !== 6 ||
    recycledPrompt !== crossSessionPrompts[0] ||
    recentFingerprints.length !== 6
  ) {
    violated(
      'reasoning questions rotate across sessions and stay balanced at the 30-question limit',
      'cross-session selection repeated early or did not recycle the oldest exhausted candidate',
      JSON.stringify({ crossSessionPrompts, recycledPrompt, recentFingerprints }),
    );
  }

  const maxSessionProfile = beginnerAtGrade(1);
  const maxSession = reasoningSession();
  const maxSessionPrompts = [];
  for (let index = 0; index < 30; index += 1) {
    maxSessionPrompts.push(app.generateSessionQuestion(maxSessionProfile, maxSession, 'target').prompt);
  }
  const promptCounts = new Map();
  for (const prompt of maxSessionPrompts) promptCounts.set(prompt, (promptCounts.get(prompt) || 0) + 1);
  const adjacentDuplicate = maxSessionPrompts.some((prompt, index) => index > 0 && prompt === maxSessionPrompts[index - 1]);
  const counts = [...promptCounts.values()];
  if (
    promptCounts.size !== 6 ||
    adjacentDuplicate ||
    Math.max(...counts) - Math.min(...counts) > 1
  ) {
    violated(
      'reasoning questions rotate across sessions and stay balanced at the 30-question limit',
      'the bounded fallback repeated immediately or distributed an exhausted bank unevenly',
      JSON.stringify({ maxSessionPrompts, counts }),
    );
  }
}

// The drills that ask the child to produce a kanji are rendered further down, so keep one
// real generated sample of each instead of hand-writing a question the app never emits.
const kanjiTargetSamples = new Map();

for (const unit of UNITS) {
  const topic = unit.topicId;
  const grade = unit.grade;
    for (const stage of STAGES) {
      const profile = profileFor(grade);
      for (let i = 0; i < SAMPLES_PER_COMBINATION; i += 1) {
        const where = `${unit.id} (${topic}) grade${grade} stage${stage}`;
        let question;
        try {
          question = app.genFor(unit.id, profile, stage);
        } catch (error) {
          violated('generators complete', `${topic} threw ${error && error.message}`, where);
          continue;
        }
        generated += 1;
        const prompt = String(question.prompt);
        const answer = String(question.answer);
        const explanation = String(question.explanation || '');
        const context = `${where} ${JSON.stringify(prompt)} -> ${JSON.stringify(answer)}`;

        observe('questions are complete');
        if (question.topic !== topic) {
          violated('questions are complete', `the question is labelled ${question.topic} but was asked for ${topic}`, context);
        }
        if (question.unitId !== unit.id || question.grade !== unit.grade) {
          violated('questions are complete', `unit metadata is ${question.unitId}/grade${question.grade}, expected ${unit.id}/grade${unit.grade}`, context);
        }
        if (answer === '' || answer === 'undefined' || answer === 'null') {
          violated('questions are complete', 'the question has no answer', context);
        }
        if (/undefined|NaN|\[object/.test(prompt + answer + explanation)) {
          violated('questions are complete', 'unrendered value in the text shown to the child', `${context} | ${explanation}`);
        }
        if (explanation === '') {
          violated('questions are complete', 'the question teaches nothing after it is answered', context);
        }

        if (question.mode === 'choices') {
          const choices = (question.choices || []).map(String);
          observe('choices are a fair test');
          if (!choices.includes(answer)) {
            violated('choices are a fair test', 'the answer is not among the choices', `${context} choices=${JSON.stringify(choices)}`);
          }
          if (new Set(choices).size !== choices.length) {
            violated('choices are a fair test', 'the same choice is offered twice', `${context} choices=${JSON.stringify(choices)}`);
          }
          if (choices.length < 4 && !isComparisonSymbolSet(choices)) {
            violated('choices are a fair test', `only ${choices.length} choices, so guessing pays`, `${context} choices=${JSON.stringify(choices)}`);
          }
          if (prompt.trim() === answer.trim()) {
            violated('choices are a fair test', 'the prompt is its own answer', context);
          }
        }

        if (question.mode === 'num') {
          observe('typed answers are numeric');
          if (!/^-?\d+(\.\d+)?$/.test(answer)) {
            violated('typed answers are numeric', 'a number pad question expects something that is not a number', context);
          }
        }

        if (question.mode === 'hissan-steps') {
          observe('column walkthrough reaches the stated answer');
          const steps = question.steps || [];
          const ones = steps.find((step) => step.place === 'ones');
          const tens = steps.find((step) => step.place === 'tens');
          const written = `${tens && tens.writeTens}${ones && ones.writeOnes}`;
          // A leading zero in the tens column is ordinary 筆算 notation, so compare values.
          if (!/^\d+$/.test(written) || Number(written) !== Number(answer)) {
            violated(
              'column walkthrough reaches the stated answer',
              `the walkthrough writes ${written} while the answer is ${answer}`,
              `${context} | ${explanation}`,
            );
          }
        }

        const writtenPlan = app.writtenArithmeticPlan(question);
        if (writtenPlan) {
          observe('generated written arithmetic has a complete finite step plan');
          if (!Array.isArray(writtenPlan.steps) || writtenPlan.steps.length < 1 || writtenPlan.steps.length > 40) {
            violated('generated written arithmetic has a complete finite step plan', `step count was ${writtenPlan.steps?.length}`, context);
          }
          for (const [stepIndex, step] of (writtenPlan.steps || []).entries()) {
            if (!step || !/^\d+$/.test(String(step.expect)) || !step.prompt || !step.explain || !step.completeText) {
              violated('generated written arithmetic has a complete finite step plan', `step ${stepIndex} is incomplete`, `${context} | ${JSON.stringify(step)}`);
            }
          }
          const arithmetic = String(question.prompt).replace(/\s+/g, '').replace(/は？$/, '').replace(/[？?]$/, '').match(/^(\d+(?:\.\d+)?)([+\-−×÷])(\d+(?:\.\d+)?)$/);
          if (!arithmetic) {
            violated('generated written arithmetic has a complete finite step plan', 'a non-arithmetic prompt entered the written-work gate', context);
          } else {
            const left = Number(arithmetic[1]);
            const right = Number(arithmetic[3]);
            const operator = arithmetic[2] === '−' ? '-' : arithmetic[2];
            const expected = operator === '+' ? String(left + right) : operator === '-' ? String(left - right) : operator === '×' ? String(left * right) : null;
            if (expected !== null && Number(question.answer) !== Number(expected)) {
              violated('generated written arithmetic has a complete finite step plan', `canonical answer ${question.answer} disagrees with ${expected}`, context);
            }
            if (operator === '÷') {
              const quotient = Math.floor(left / right);
              const remainder = Math.round((left - quotient * right) * 1e9) / 1e9;
              const canonical = remainder === 0 ? String(quotient) : `${quotient} あまり ${remainder}`;
              if (String(question.answer) !== canonical) {
                violated('generated written arithmetic has a complete finite step plan', `division answer ${question.answer} disagrees with ${canonical}`, context);
              }
            }
          }
        }

        // Self-answering commutativity item: "5 × 5 と こたえが おなじ しきは？" -> "5 × 5".
        const commutativity = prompt.match(/^(.+?)\s*と\s*こたえが おなじ/);
        if (commutativity) {
          observe('commutativity items do not answer themselves');
          if (commutativity[1].trim() === answer.trim()) {
            violated('commutativity items do not answer themselves', 'the multiplication in the prompt is the answer', context);
          }
        }

        if (topic === 'add' && stage === 3) {
          const twoTerm = prompt.match(TWO_TERM_ADDITION);
          if (twoTerm) {
            observe('addition stage 3 never regroups');
            const [a, b] = [Number(twoTerm[1]), Number(twoTerm[2])];
            if ((a % 10) + (b % 10) >= 10) {
              violated('addition stage 3 never regroups', `${a} + ${b} carries although the stage teaches no carrying`, context);
            }
          }
        }

        if (topic === 'sub' && grade === 1) {
          const values = numbersIn(prompt).concat(numbersIn(answer));
          if (values.length) {
            observe('grade-1 subtraction stays within 20');
            if (Math.max(...values) > 20) {
              violated('grade-1 subtraction stays within 20', `${Math.max(...values)} is outside the grade-1 range`, context);
            }
            const terms = numbersIn(prompt);
            if (terms.length === 3 && terms[1] > 9) {
              violated('grade-1 subtraction stays within 20', `the middle term ${terms[1]} is not a single digit`, context);
            }
          }
        }

        if (topic === 'frac' && grade === 2) {
          observe('grade-2 fractions stay within simple fractional meaning');
          if (/\d+\.\d+/.test(prompt + explanation) || /\s[+＋−-]\s/.test(prompt)) {
            violated(
              'grade-2 fractions stay within simple fractional meaning',
              'decimal or fraction arithmetic leaked into the grade-2 question bank',
              `${context} | ${explanation}`,
            );
          }
        }

        if (topic === 'chart') {
          if (question.isTable) {
            observe('chart table fill-ins include a countable pictograph');
            if (!question.isChart || !Array.isArray(question.rows) || question.rows.length === 0) {
              violated(
                'chart table fill-ins include a countable pictograph',
                'isTable question shipped without isChart rows',
                context,
              );
            }
          }
          if (question.isChart) {
            observe('chart pictograph questions carry bar rows');
            if (!Array.isArray(question.rows) || question.rows.length === 0) {
              violated('chart pictograph questions carry bar rows', 'isChart question has no rows', context);
            }
          }
        }

        if (question.isTape) {
          observe('tape diagrams include labeled parts');
          if (!Array.isArray(question.tapeParts) || question.tapeParts.length === 0) {
            violated('tape diagrams include labeled parts', 'isTape question has no tapeParts', context);
          }
        }
        if (question.isMoney) {
          observe('money visuals include coin or bill pieces');
          if (!Array.isArray(question.moneyPieces) || question.moneyPieces.length === 0) {
            violated('money visuals include coin or bill pieces', 'isMoney question has no moneyPieces', context);
          }
        }
        if (question.isCount) {
          observe('counting visuals include a positive count');
          if (!(Number(question.count) > 0)) {
            violated('counting visuals include a positive count', 'isCount question has no count', context);
          }
        }
        if (question.isFracViz) {
          observe('fraction visuals include numerator and denominator');
          if (!(Number(question.fd) > 0) || question.fn == null) {
            violated('fraction visuals include numerator and denominator', 'isFracViz missing fd/fn', context);
          }
        }
        if (question.isOrder) {
          observe('order visuals include length, position, and direction');
          if (!(Number(question.oc) > 0 && Number(question.op) > 0 && question.od)) {
            violated('order visuals include length, position, and direction', 'isOrder incomplete', context);
          }
        }
        if (question.isClock) {
          observe('clock visuals include hour and minute');
          if (question.h == null || question.m == null) {
            violated('clock visuals include hour and minute', 'isClock missing h/m', context);
          }
        }
        if (question.isShape) {
          observe('shape visuals include a drawable style');
          if (!question.shapeStyle) {
            violated('shape visuals include a drawable style', 'isShape missing shapeStyle', context);
          }
        }
        if (question.isGroups) {
          observe('group visuals include group count and size');
          if (!(Number(question.groupCount) > 0 && Number(question.groupSize) > 0)) {
            violated('group visuals include group count and size', 'isGroups incomplete', context);
          }
        }
        if (question.isMeasure) {
          observe('measure comparison visuals include at least one amount');
          if (!(Number(question.m1) || Number(question.m2) || Number(question.m3))) {
            violated('measure comparison visuals include at least one amount', 'isMeasure missing m1/m2/m3', context);
          }
        }

        const figurePrompt = String(prompt || '');
        if (
          /^(この かたちの|とけいを よもう|まるは いくつ|オレンジの ます|いろの ついた|おかねは ぜんぶ|テープ図の|グラフを みて)/.test(figurePrompt) ||
          /表の □/.test(figurePrompt)
        ) {
          observe('figure-required prompts ship a matching visual flag');
          const hasVisual =
            question.isChart ||
            question.isTable ||
            question.isTape ||
            question.isMeasure ||
            question.isFracViz ||
            question.isOrder ||
            question.isMoney ||
            question.isCount ||
            question.isClock ||
            question.isShape ||
            question.isGroups;
          if (!hasVisual) {
            violated(
              'figure-required prompts ship a matching visual flag',
              'prompt asks the learner to look at a figure that was never attached',
              context,
            );
          }
        }

        for (const text of [answer, explanation, ...((question.choices || []).map(String))]) {
          for (const match of text.matchAll(/(\d+)\s*(ふん|ぷん)/g)) {
            observe('minute readings follow the ふん / ぷん rule');
            const minute = Number(match[1]);
            const wanted = expectedMinuteWord(minute);
            if (match[2] !== wanted) {
              violated(
                'minute readings follow the ふん / ぷん rule',
                `${minute} is read ${minute}${wanted}, not ${match[0]}`,
                `${context} | ${text}`,
              );
            }
          }
        }

        if (topic === 'moji') {
          const romaji = prompt.match(/ローマ字「([a-z]+)」/);
          if (romaji) {
            observe('the romaji drill covers the gojuon in the current standard spelling');
            romajiKanaSeen.add(answer);
            if (!romajiSpellingsSeen.has(answer)) romajiSpellingsSeen.set(answer, new Set());
            romajiSpellingsSeen.get(answer).add(romaji[1]);
            if (LEGACY_INPUT_ROMAJI.has(romaji[1])) {
              violated(
                'the romaji drill uses the current standard spelling',
                `${romaji[1]} is an input hint, not the primary spelling shown for ${answer}`,
                context,
              );
            }
          }
        }

        if (topic === 'kokugo' && (question.subtype === 'kanji-choice' || question.subtype === 'kanji-picture')) {
          if (!kanjiTargetSamples.has(question.subtype)) kanjiTargetSamples.set(question.subtype, question);
        }

        if (topic === 'kokugo' && question.subtype === 'kanji-choice') {
          observe('kanji choices have exactly one right reading');
          const askedReading = String(question.word);
          const alsoCorrect = (question.choices || [])
            .map(String)
            .filter((choice) => choice !== answer)
            .filter((choice) => (readingsByWord.get(choice) || new Set()).has(askedReading));
          if (alsoCorrect.length) {
            violated(
              'kanji choices have exactly one right reading',
              `${alsoCorrect.join('、')} is also read ${askedReading}`,
              `${context} choices=${JSON.stringify(question.choices)}`,
            );
          }
        }

        if (topic === 'kokugo' && question.subtype === 'kanji-picture') {
          observe('SVG picture-to-kanji questions expose safe SVG metadata');
          const svg = app.kanjiPictureSvg(question.pictureId, question.pictureLabel);
          const svgProps = svg && svg.props ? svg.props : {};
          const choices = (question.choices || []).map(String);
          if (
            question.pictureKind !== 'svg' ||
            !question.pictureId ||
            !question.pictureLabel ||
            !choices.includes(String(question.answer)) ||
            !svg ||
            svg.type !== 'svg' ||
            svgProps.role !== 'img' ||
            !svgProps.viewBox ||
            !svgProps['aria-label']
          ) {
            violated(
              'SVG picture-to-kanji questions expose safe SVG metadata',
              'picture question is missing safe SVG metadata or a fair answer choice',
              context + `${JSON.stringify(question)}`,
            );
          }
        }

        if (topic === 'kokugo' && question.subtype === 'reading') {
          observe('kanji readings are asked with their okurigana');
          const shown = String(question.word);
          if (shown.length === 1 && isPotentiallyInflected(answer)) {
            violated(
              'kanji readings are asked with their okurigana',
              `${shown} alone does not fix the reading ${answer}`,
              context,
            );
          }
        }
      }
    }
}

// The picture provider is random by design, so sweep the real low-grade generator until
// the new format is observed at least once instead of allowing an unobserved check to pass.
const pictureProfile = app.ensureLearningProfile(profileFor(1));
let pictureSample = null;
for (let i = 0; i < 1024 && !pictureSample; i += 1) {
  const candidate = app.pickKokugo(pictureProfile);
  if (candidate && candidate.subtype === 'kanji-picture') pictureSample = candidate;
}
observe('SVG picture-to-kanji questions are generated for grades 1-3', pictureSample ? 1 : 0);
if (!pictureSample) {
  violated(
    'SVG picture-to-kanji questions are generated for grades 1-3',
    'pickKokugo never produced the SVG picture format for a grade-1 profile',
    'grade1',
  );
}
if (pictureSample) {
  const svg = app.kanjiPictureSvg(pictureSample.pictureId, pictureSample.pictureLabel);
  if (pictureSample.pictureKind !== 'svg' || !svg || svg.type !== 'svg' || svg.props?.role !== 'img') {
    violated(
      'SVG picture-to-kanji questions are generated for grades 1-3',
      'the sampled picture question did not expose a safe SVG image node',
      JSON.stringify(pictureSample),
    );
  }
}

const upperGradePictureProfile = app.ensureLearningProfile(profileFor(4));
let upperGradePictureSeen = false;
for (let i = 0; i < 512; i += 1) {
  const candidate = app.pickKokugo(upperGradePictureProfile);
  if (candidate && candidate.subtype === 'kanji-picture') {
    upperGradePictureSeen = true;
    break;
  }
}
observe('SVG picture-to-kanji questions stay within grades 1-3', upperGradePictureSeen ? 0 : 1);
if (upperGradePictureSeen) {
  violated(
    'SVG picture-to-kanji questions stay within grades 1-3',
    'a grade-4 profile received a grade-1-3 SVG picture question',
    'grade4',
  );
}

// A drill that asks the child to write a reading as a kanji answers itself when the tiles
// carry that reading as furigana, and the same is true of the picture drill. Asserting on
// the source text of the exclusion is what let the leak ship, so render the real view model
// through the real choice pipeline and require the tiles to come out bare.
const RUBY_CHECK = 'kanji-target choices are rendered without furigana';
const rubyReadingsIn = (value) => notationNodes(value)
  .filter((node) => node && node.type === 'rt')
  .map((node) => (node.children || []).map(String).join(''));

// A pristine instance: the audit above rewrote app.state for the migration checks, and the
// view model is only meaningful on the state shape the constructor establishes.
const renderApp = new app.constructor();
renderApp.props = renderApp.props || {};

for (const subtype of ['kanji-choice', 'kanji-picture']) {
  const question = kanjiTargetSamples.get(subtype);
  observe(RUBY_CHECK, question ? 1 : 0);
  if (!question) {
    violated(RUBY_CHECK, `the sweep never generated a ${subtype} question, so the tiles were never checked`, subtype);
    continue;
  }
  // Control: the answer kanji must be annotatable, otherwise bare tiles prove nothing.
  if (!rubyReadingsIn(renderApp.withFurigana(String(question.answer))).length) {
    violated(RUBY_CHECK, `${question.answer} carries no furigana even when asked for, so the check cannot detect a leak`, subtype);
    continue;
  }
  let choices;
  try {
    renderApp.state.screen = 'quiz';
    renderApp.state.profile = renderApp.ensureLearningProfile(profileFor(question.grade || 1));
    renderApp.state.session = { questions: [question], idx: 0, total: 1, correct: 0 };
    renderApp.state.lastResult = null;
    const view = renderApp.renderVals();
    choices = (view.choices || []).map((choice) => choice && choice.text);
  } catch (error) {
    violated(RUBY_CHECK, `rendering a ${subtype} question threw ${error && error.message}`, String(error && error.stack));
    continue;
  }
  if (choices.length !== (question.choices || []).length) {
    violated(RUBY_CHECK, `the ${subtype} view model rendered ${choices.length} tiles for ${(question.choices || []).length} choices`, JSON.stringify(question.choices));
    continue;
  }
  for (let index = 0; index < choices.length; index += 1) {
    const readings = rubyReadingsIn(choices[index]);
    if (readings.length) {
      violated(
        RUBY_CHECK,
        `the ${subtype} tile ${question.choices[index]} is annotated with ${readings.join('/')}, which is the answer the child is asked for`,
        `${question.prompt} -> ${question.answer} choices=${JSON.stringify(question.choices)}`,
      );
    }
  }
}

// --- arithmetic drill courses (issues #62 and #64) ----------------------------------------

// The drill is a fixed course, so every question can be checked instead of sampled. Each
// prompt is solved from its own text: an answer that disagrees with the arithmetic in the
// prompt would teach the child a wrong fact, which no amount of sampling may miss.
const DRILL_OPERATORS = { '＋': (x, y) => x + y, '−': (x, y) => x - y, '×': (x, y) => x * y };
const solveDrillPrompt = (text) => {
  const parts = String(text).split(' ');
  const apply = DRILL_OPERATORS[parts[1]];
  if (!apply) return undefined;
  if (parts.length === 3) {
    if (parts[0] === '□' || parts[2] === '□') return undefined;
    return apply(Number(parts[0]), Number(parts[2]));
  }
  if (parts.length !== 5 || parts[3] !== '＝') return undefined;
  const blankLeft = parts[0] === '□';
  const blankRight = parts[2] === '□';
  if (blankLeft === blankRight) return undefined;
  const expected = Number(parts[4]);
  for (let candidate = 0; candidate <= 100; candidate += 1) {
    const left = blankLeft ? candidate : Number(parts[0]);
    const right = blankRight ? candidate : Number(parts[2]);
    if (apply(left, right) === expected) return candidate;
  }
  return undefined;
};

// The drill persists progress through localStorage, which Node does not have. The stub is a
// storage sink only; every rule about what is stored still comes from the generated app.
const drillStorage = new Map();
globalThis.localStorage = {
  getItem: (key) => (drillStorage.has(key) ? drillStorage.get(key) : null),
  setItem: (key, value) => drillStorage.set(key, String(value)),
  removeItem: (key) => drillStorage.delete(key),
};

const DRILL_COURSE_CHECK = 'the drill courses are fixed, complete, and self-consistent';
const DRILL_EMPHASIS_CHECK = 'the grade-1 drill emphasizes complements of ten and subtraction from ten';
const DRILL_FACT_CHECK = 'the grade-2 drill covers every ordered multiplication fact';
const DRILL_CHOICE_CHECK = 'every arithmetic drill question offers one plausible wrong answer and one right answer';
const DRILL_KANJI_CHECK = 'the kanji drills cover their grade and distinguish on-yomi from kun-yomi';
const DRILL_KANJI_WORD_CHECK = 'kanji word questions use only characters learned by the course grade';
const DRILL_KANJI_WRITING_CHECK = 'every kanji writing question offers one right spelling and three same-grade distractors with different readings';
const DRILL_FLOW_CHECK = 'a drill run advances, requeues a revealed question, and resumes where it stopped';
const DRILL_PAIR_CHECK = 'a correct drill answer shows the question and its answer as one pair before advancing';

const drillCourses = app.drillCourses();
observe(DRILL_COURSE_CHECK, drillCourses.length);
if (drillCourses.length !== 4) {
  violated(DRILL_COURSE_CHECK, `expected an arithmetic and a kanji course for grade 1 and grade 2, found ${drillCourses.length}`, JSON.stringify(drillCourses.map((c) => c.id)));
}
const DRILL_FIRST_PROMPT = { g1: '1 ＋ 1', g2: '2 × 1' };
for (const course of drillCourses) {
  const bank = app.drillBank(course.id);
  if (bank.length !== course.total) {
    violated(DRILL_COURSE_CHECK, `${course.id} advertises ${course.total} questions but builds ${bank.length}`, course.title);
  }
  const expectedFirst = DRILL_FIRST_PROMPT[course.id];
  if (expectedFirst && (!bank[0] || bank[0].text !== expectedFirst)) {
    violated(DRILL_COURSE_CHECK, `${course.id} does not start at ${expectedFirst}`, bank[0] ? bank[0].text : '(empty)');
  }
  bank.forEach((question, index) => {
    if (question.no !== index + 1) {
      violated(DRILL_COURSE_CHECK, `${course.id} question numbering breaks at index ${index}`, JSON.stringify(question));
    }
    if (!question.sec || !question.hint) {
      violated(DRILL_COURSE_CHECK, `${course.id} question ${question.no} has no section or no hint`, JSON.stringify(question));
    }
    // A reading is chosen, not calculated, so the arithmetic solver only judges keypad questions.
    if (question.kind === 'pick') return;
    const solved = solveDrillPrompt(question.text);
    if (solved === undefined) {
      violated(DRILL_COURSE_CHECK, `${course.id} question ${question.no} is not a readable prompt`, question.text);
      return;
    }
    if (solved !== question.ans) {
      violated(DRILL_COURSE_CHECK, `${course.id} question ${question.no} answers ${question.ans} but ${question.text} is ${solved}`, question.text);
    }
    if (!Number.isInteger(question.ans) || question.ans < 0) {
      violated(DRILL_COURSE_CHECK, `${course.id} question ${question.no} does not answer with a whole number`, JSON.stringify(question));
    }
  });
}

// The optional recognition mode must be safe across the entire fixed bank, not only a sample.
// A session keeps its shuffled layout stable while avoiding a learnable question-number pattern.
observe(DRILL_CHOICE_CHECK, 400);
for (const courseId of ['g1', 'g2']) {
  const bank = app.drillBank(courseId);
  const choiceOrder = app.drillChoiceOrder(bank.length, courseId === 'g1' ? 123456789 : 987654321);
  const secondOrder = app.drillChoiceOrder(bank.length, courseId === 'g1' ? 987654321 : 123456789);
  if (choiceOrder.length !== bank.length || choiceOrder.every((position, index) => position === index % 2) || choiceOrder.every((position, index) => position === 1 - (index % 2))) {
    violated(DRILL_CHOICE_CHECK, `${courseId} uses a predictable alternating answer layout`, JSON.stringify(choiceOrder));
  }
  if (choiceOrder.every((position, index) => position === secondOrder[index])) {
    violated(DRILL_CHOICE_CHECK, `${courseId} answer layout does not change with the session seed`, JSON.stringify(choiceOrder));
  }
  const drill = { id: courseId, answerMode: 'choice', choiceOrder };
  const correctPositions = [0, 0];
  for (const question of bank) {
    const choices = app.drillNumericChoices(drill, question);
    const repeatedChoices = app.drillNumericChoices(drill, question);
    if (choices.length !== 2 || new Set(choices).size !== 2 || !choices.includes(question.ans)) {
      violated(DRILL_CHOICE_CHECK, `${courseId} question ${question.no} offers ${JSON.stringify(choices)}`, JSON.stringify(question));
      continue;
    }
    if (JSON.stringify(choices) !== JSON.stringify(repeatedChoices)) {
      violated(DRILL_CHOICE_CHECK, `${courseId} question ${question.no} changes position during the same session`, JSON.stringify([choices, repeatedChoices]));
    }
    const correctAt = choices.indexOf(question.ans);
    correctPositions[correctAt] += 1;
    const wrong = choices[1 - correctAt];
    if (!Number.isInteger(wrong) || wrong < 0) {
      violated(DRILL_CHOICE_CHECK, `${courseId} question ${question.no} has an invalid distractor ${wrong}`, question.text);
      continue;
    }
    const fact = /^(\d+) × (\d+)$/.exec(question.text);
    if (!fact && Math.abs(wrong - question.ans) !== 1) {
      violated(DRILL_CHOICE_CHECK, `${courseId} question ${question.no} does not use an adjacent-number distractor ${wrong}`, question.text);
    }
    if (fact) {
      const left = Number(fact[1]);
      const wrongFactor = wrong / left;
      if (!Number.isInteger(wrongFactor) || wrongFactor < 1 || wrongFactor > 9 || Math.abs(wrongFactor - Number(fact[2])) !== 1) {
        violated(DRILL_CHOICE_CHECK, `${courseId} question ${question.no} does not use an adjacent table fact as distractor`, JSON.stringify(choices));
      }
    }
  }
  if (correctPositions[0] !== bank.length / 2 || correctPositions[1] !== bank.length / 2) {
    violated(DRILL_CHOICE_CHECK, `${courseId} correct positions are ${correctPositions.join(' / ')}`, 'expected an even split');
  }
}

// Complements of ten and subtraction from ten are the facts the course exists to drill, so
// each of the nine pairs has to appear more than once and in a missing-number form as well.
const gradeOneBank = app.drillBank('g1');
observe(DRILL_EMPHASIS_CHECK, 9);
for (let addend = 1; addend <= 9; addend += 1) {
  const complements = gradeOneBank.filter((q) => q.text === `${addend} ＋ ${10 - addend}`).length;
  const complementBlanks = gradeOneBank.filter((q) => q.text.includes('＝ 10') && q.ans === 10 - addend).length;
  const fromTen = gradeOneBank.filter((q) => q.text === `10 − ${addend}`).length;
  const fromTenBlanks = gradeOneBank.filter((q) => q.text === `10 − □ ＝ ${10 - addend}`).length;
  if (complements < 2) {
    violated(DRILL_EMPHASIS_CHECK, `${addend} ＋ ${10 - addend} is drilled only ${complements} time(s)`, 'complement of ten');
  }
  if (complementBlanks < 1) {
    violated(DRILL_EMPHASIS_CHECK, `no missing-number question asks for ${10 - addend} as a complement of ten`, 'complement of ten');
  }
  if (fromTen < 2) {
    violated(DRILL_EMPHASIS_CHECK, `10 − ${addend} is drilled only ${fromTen} time(s)`, 'subtraction from ten');
  }
  if (fromTenBlanks < 1) {
    violated(DRILL_EMPHASIS_CHECK, `10 − □ ＝ ${10 - addend} is never asked`, 'subtraction from ten');
  }
}

const gradeTwoBank = app.drillBank('g2');
const drilledFacts = new Set();
for (const question of gradeTwoBank) {
  const match = /^(\d) × (\d)$/.exec(question.text);
  if (match) drilledFacts.add(`${match[1]}x${match[2]}`);
}
observe(DRILL_FACT_CHECK, drilledFacts.size);
for (let left = 1; left <= 9; left += 1) {
  for (let right = 1; right <= 9; right += 1) {
    if (!drilledFacts.has(`${left}x${right}`)) {
      violated(DRILL_FACT_CHECK, `${left} × ${right} is never drilled`, 'multiplication fact coverage');
    }
  }
}

// The kanji courses still introduce every character in their own grade, then apply those
// characters in real words. Grade 1 may use only grade-1 kanji; grade 2 is cumulative and
// may combine grade-1 and grade-2 kanji, but never a later-grade character.
const KANJI_DRILL_GRADES = { k1: 1, k2: 2 };
const gradeByKanji = new Map(curriculum.map((entry) => [entry.k, entry.g]));
observe(DRILL_KANJI_CHECK, Object.keys(KANJI_DRILL_GRADES).length);
observe(DRILL_KANJI_WORD_CHECK, 160);
for (const [courseId, grade] of Object.entries(KANJI_DRILL_GRADES)) {
  const bank = app.drillBank(courseId);
  const gradeEntries = curriculum.filter((entry) => entry.g === grade);
  const readingOf = new Map(gradeEntries.map((entry) => [entry.k, entry]));
  const asked = new Set();
  const askedWords = new Set();
  const readingTypes = new Set();
  for (const question of bank) {
    if (question.kind !== 'pick') {
      violated(DRILL_KANJI_CHECK, `${courseId} question ${question.no} is not answered by choosing a reading`, JSON.stringify(question));
      continue;
    }
    const choices = question.choices || [];
    if (choices.length !== 4 || new Set(choices).size !== 4 || choices.filter((choice) => choice === question.ans).length !== 1) {
      violated(DRILL_KANJI_CHECK, `${courseId} question ${question.no} offers ${JSON.stringify(choices)}`, question.text);
      continue;
    }
    if (question.readingType === 'word') {
      readingTypes.add('word');
      askedWords.add(question.text);
      const expectedWord = app.drillKanjiWords(grade).find((entry) => entry.word === question.text);
      if (!question.kanjiWord || !expectedWord || expectedWord.reading !== question.ans) {
        violated(DRILL_KANJI_WORD_CHECK, `${courseId} question ${question.no} is not a canonical word-reading pair`, JSON.stringify(question));
      }
      const wordKanji = Array.from(question.text).filter((character) => /\p{Script=Han}/u.test(character));
      if (wordKanji.length < 2) {
        violated(DRILL_KANJI_WORD_CHECK, `${courseId} question ${question.no} does not combine at least two kanji`, question.text);
      }
      for (const character of wordKanji) {
        const learnedGrade = gradeByKanji.get(character);
        if (!learnedGrade || learnedGrade > grade) {
          violated(DRILL_KANJI_WORD_CHECK, `${courseId} question ${question.no} uses ${character} from grade ${learnedGrade || 'outside the curriculum'}`, question.text);
        }
      }
      if (grade === 2 && !wordKanji.some((character) => gradeByKanji.get(character) === 2)) {
        violated(DRILL_KANJI_WORD_CHECK, `${courseId} question ${question.no} does not apply a grade-2 kanji`, question.text);
      }
      continue;
    }
    const entry = readingOf.get(question.kanji);
    if (!entry) {
      violated(DRILL_KANJI_CHECK, `${courseId} question ${question.no} asks about ${question.kanji}, which is not a grade-${grade} kanji`, question.text);
      continue;
    }
    if (question.readingType !== 'on' && question.readingType !== 'kun') {
      violated(DRILL_KANJI_CHECK, `${courseId} question ${question.no} does not identify on-yomi or kun-yomi`, JSON.stringify(question));
      continue;
    }
    const expected = question.readingType === 'on' ? entry.on : entry.kun;
    const expectedText = question.readingType === 'on' ? entry.k : entry.kunWord;
    if (!expected) {
      violated(DRILL_KANJI_CHECK, `${courseId} asks for a missing ${question.readingType} reading of ${entry.k}`, JSON.stringify(question));
      continue;
    }
    asked.add(entry.k);
    readingTypes.add(question.readingType);
    if (question.ans !== expected) {
      violated(DRILL_KANJI_CHECK, `${courseId} reads ${entry.k} as ${question.ans} instead of ${expected}`, question.text);
    }
    if (question.text !== expectedText) {
      violated(DRILL_KANJI_CHECK, `${courseId} uses ${question.text} for ${question.readingType} of ${entry.k}; expected ${expectedText}`, question.text);
    }
    if (!choices.includes(expected)) {
      violated(DRILL_KANJI_CHECK, `${courseId} question ${question.no} never offers the right reading ${expected}`, JSON.stringify(choices));
    }
  }
  if (asked.size !== gradeEntries.length) {
    const missing = gradeEntries.filter((entry) => !asked.has(entry.k)).map((entry) => entry.k);
    violated(DRILL_KANJI_CHECK, `${courseId} drills ${asked.size} of the ${gradeEntries.length} grade-${grade} kanji`, missing.join(''));
  }
  if (!readingTypes.has('on') || !readingTypes.has('kun') || !readingTypes.has('word')) {
    violated(DRILL_KANJI_CHECK, `${courseId} does not include on-yomi, kun-yomi, and word questions`, JSON.stringify([...readingTypes]));
  }
  const expectedWords = app.drillKanjiWords(grade);
  const expectedWordQuestions = 200 - gradeEntries.length;
  const wordQuestions = bank.filter((question) => question.readingType === 'word');
  if (wordQuestions.length !== expectedWordQuestions || askedWords.size !== expectedWords.length) {
    violated(DRILL_KANJI_WORD_CHECK, `${courseId} has ${wordQuestions.length} word questions using ${askedWords.size} unique words`, `expected ${expectedWordQuestions} questions using ${expectedWords.length} words`);
  }
}

// Writing mode reverses each prompt: the reading is shown and the child chooses the canonical
// spelling (including okurigana). Distractors stay inside the same grade and never share the
// target reading, so an alternative valid spelling cannot create an ambiguous question.
observe(DRILL_KANJI_WRITING_CHECK, 400);
for (const [courseId, grade] of Object.entries(KANJI_DRILL_GRADES)) {
  const bank = app.drillBank(courseId);
  const gradeSpellings = new Set(bank.map((question) => question.text));
  const correctPositions = [0, 0, 0, 0];
  for (const question of bank) {
    const drill = { id: courseId, answerMode: 'writing' };
    const presented = app.drillPresentedQuestion(drill, question);
    const choices = app.drillChoices(drill, presented);
    if (presented.text !== question.ans || presented.ans !== question.text) {
      violated(DRILL_KANJI_WRITING_CHECK, `${courseId} question ${question.no} was not reversed for writing`, JSON.stringify(presented));
      continue;
    }
    if (choices.length !== 4 || new Set(choices).size !== 4 || choices.filter((choice) => choice === question.text).length !== 1) {
      violated(DRILL_KANJI_WRITING_CHECK, `${courseId} question ${question.no} offers ${JSON.stringify(choices)}`, JSON.stringify(question));
      continue;
    }
    correctPositions[choices.indexOf(question.text)] += 1;
    for (const distractor of choices.filter((choice) => choice !== question.text)) {
      if (!gradeSpellings.has(distractor)) {
        violated(DRILL_KANJI_WRITING_CHECK, `${courseId} question ${question.no} uses an out-of-grade spelling ${distractor}`, JSON.stringify(choices));
      }
      const distractorQuestions = bank.filter((candidate) => candidate.text === distractor);
      if (distractorQuestions.some((candidate) => candidate.ans === question.ans)) {
        violated(DRILL_KANJI_WRITING_CHECK, `${courseId} question ${question.no} uses same-reading distractor ${distractor}`, question.ans);
      }
    }
  }
  if (correctPositions.some((count) => count !== bank.length / 4)) {
    violated(DRILL_KANJI_WRITING_CHECK, `${courseId} correct positions are ${correctPositions.join(' / ')}`, 'expected an even four-way split');
  }
}

// Drive a whole run the way the child does, so advancing, the two-mistake reveal, the
// requeue, and resuming after quitting are proven rather than assumed.
observe(DRILL_FLOW_CHECK, 4);
drillStorage.clear();
app.state.screen = 'start';
app.selectDrillCourse('g1');
if (app.state.screen !== 'drill-mode' || app.state.drillCourseChoice !== 'g1') {
  violated(DRILL_FLOW_CHECK, 'selecting an arithmetic course did not ask how to answer', JSON.stringify(app.state));
}

// A correct answer holds the completed pair on screen before the next question replaces it, so
// the run has to flush that echo exactly the way the app's timer does (issue #68).
const submitDrillAnswer = (value) => {
  app.setState({ input: String(value) });
  app.drillSubmit();
  app.drillFlushEcho();
};
const chooseDrillAnswer = (value) => {
  app.drillChoose(value);
  app.drillFlushEcho();
};

observe(DRILL_PAIR_CHECK, 2);
app.startDrill('g1', true);
const echoQuestion = app.drillQuestion();
app.setState({ input: String(echoQuestion.ans) });
app.drillSubmit();
if (!app.state.drill.echo || app.state.drill.idx !== 0) {
  violated(DRILL_PAIR_CHECK, 'a correct answer advanced without showing the completed pair', JSON.stringify(app.state.drill));
} else if (app.state.drill.echo.main !== app.drillAnswerLine(echoQuestion) || app.state.drill.echo.sub !== '') {
  violated(DRILL_PAIR_CHECK, `the arithmetic pair read ${JSON.stringify(app.state.drill.echo)}`, app.drillAnswerLine(echoQuestion));
}
app.drillFlushEcho();
if (app.state.drill.idx !== 1 || app.state.drill.echo) {
  violated(DRILL_PAIR_CHECK, 'flushing the pair did not move on to the next question', JSON.stringify(app.state.drill));
}
// Both kanji directions have to leave the same picture behind: the spelling above its reading.
for (const answerMode of ['reading', 'writing']) {
  app.startDrill('k1', true, answerMode);
  const pairQuestion = app.drillPresentedQuestion(app.state.drill, app.drillQuestion());
  const pair = app.drillPair(app.state.drill, pairQuestion);
  const spelling = answerMode === 'writing' ? pairQuestion.ans : pairQuestion.text;
  const reading = answerMode === 'writing' ? pairQuestion.text : pairQuestion.ans;
  if (pair.main !== spelling || pair.sub !== reading) {
    violated(DRILL_PAIR_CHECK, `the ${answerMode} pair read ${JSON.stringify(pair)}`, `${spelling} / ${reading}`);
  }
}

app.startDrill('g1', true);
let drillAsked = 0;
while (!app.state.drill.done && drillAsked <= gradeOneBank.length) {
  drillAsked += 1;
  submitDrillAnswer(app.drillQuestion().ans);
}
if (drillAsked !== gradeOneBank.length || app.state.drill.perfect !== gradeOneBank.length) {
  violated(DRILL_FLOW_CHECK, `a clean run asked ${drillAsked} questions and scored ${app.state.drill.perfect} of ${gradeOneBank.length}`, 'clean run');
}
if (JSON.parse(drillStorage.get('kt_drill_v1')).g1.runs !== 1) {
  violated(DRILL_FLOW_CHECK, 'finishing the course did not record exactly one completed run', drillStorage.get('kt_drill_v1'));
}

app.startDrill('g1', true);
const revealedQuestion = app.drillQuestion();
app.setState({ input: String(revealedQuestion.ans + 1) });
app.drillSubmit();
if (app.state.drill.mark !== 'wrong' || app.state.drill.hint !== revealedQuestion.hint) {
  violated(DRILL_FLOW_CHECK, 'the first mistake did not offer the hint and a retry', JSON.stringify(app.state.drill));
}
app.setState({ input: String(revealedQuestion.ans + 2) });
app.drillSubmit();
if (!app.state.drill.revealed) {
  violated(DRILL_FLOW_CHECK, 'the second mistake did not reveal the answer', JSON.stringify(app.state.drill));
}
app.drillNext();
let requeueAsked = 0;
let lastPrompt = '';
while (!app.state.drill.done && requeueAsked <= gradeOneBank.length) {
  requeueAsked += 1;
  lastPrompt = app.drillQuestion().text;
  submitDrillAnswer(app.drillQuestion().ans);
}
if (lastPrompt !== revealedQuestion.text) {
  violated(DRILL_FLOW_CHECK, `a revealed question was not asked again at the end (last prompt was ${lastPrompt})`, revealedQuestion.text);
}
if (app.state.drill.perfect !== gradeOneBank.length - 1) {
  violated(DRILL_FLOW_CHECK, `a requeued question inflated the first-try score to ${app.state.drill.perfect}`, 'requeued run');
}

app.startDrill('g2', true);
for (let step = 0; step < 7; step += 1) {
  submitDrillAnswer(app.drillQuestion().ans);
}
app.exitDrill();
if (app.state.screen !== 'start' || app.state.drill !== null) {
  violated(DRILL_FLOW_CHECK, 'quitting the drill did not return to the start screen', JSON.stringify(app.state.screen));
}
app.startDrill('g2', false, 'choice');
if (app.state.drill.idx !== 7 || app.state.drill.answerMode !== 'choice' || app.drillQuestion().text !== gradeTwoBank[7].text) {
  violated(DRILL_FLOW_CHECK, `resuming restarted at ${app.state.drill.idx} instead of 7`, JSON.stringify(app.state.drill));
}
// Arithmetic choice answers use the same miss/retry/scoring flow as keypad answers.
app.startDrill('g1', true, 'choice');
const arithmeticChoiceQuestion = app.drillQuestion();
const arithmeticChoices = app.drillChoices(app.state.drill, arithmeticChoiceQuestion);
chooseDrillAnswer(arithmeticChoices.find((choice) => choice !== arithmeticChoiceQuestion.ans));
if (app.state.drill.mark !== 'wrong' || app.state.drill.idx !== 0) {
  violated(DRILL_FLOW_CHECK, 'a wrong arithmetic choice did not keep the question on screen', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(arithmeticChoiceQuestion.ans);
if (app.state.drill.idx !== 1 || app.state.drill.perfect !== 0) {
  violated(DRILL_FLOW_CHECK, 'the corrected arithmetic choice did not advance without first-try credit', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(app.drillQuestion().ans);
if (app.state.drill.idx !== 2 || app.state.drill.perfect !== 1) {
  violated(DRILL_FLOW_CHECK, 'a clean arithmetic choice was not credited', JSON.stringify(app.state.drill));
}
// Both kanji modes answer by choosing, so the same run has to work without the keypad.
app.selectDrillCourse('k1');
if (app.state.screen !== 'drill-mode' || app.state.drillCourseChoice !== 'k1') {
  violated(DRILL_FLOW_CHECK, 'selecting a kanji course did not ask which answer direction to use', JSON.stringify(app.state));
}
app.startDrill('k1', true, 'reading');
const kanjiQuestion = app.drillQuestion();
chooseDrillAnswer(kanjiQuestion.choices.find((choice) => choice !== kanjiQuestion.ans));
if (app.state.drill.mark !== 'wrong' || app.state.drill.idx !== 0) {
  violated(DRILL_FLOW_CHECK, 'a wrong reading did not keep the kanji question on screen', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(kanjiQuestion.ans);
if (app.state.drill.idx !== 1 || app.state.drill.perfect !== 0) {
  violated(DRILL_FLOW_CHECK, 'choosing the right reading did not advance without crediting a first-try answer', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(app.drillQuestion().ans);
if (app.state.drill.idx !== 2 || app.state.drill.perfect !== 1) {
  violated(DRILL_FLOW_CHECK, 'a clean kanji answer was not credited', JSON.stringify(app.state.drill));
}

app.startDrill('k1', true, 'writing');
const writingBaseQuestion = app.drillQuestion();
const writingQuestion = app.drillPresentedQuestion(app.state.drill, writingBaseQuestion);
const writingChoices = app.drillChoices(app.state.drill, writingQuestion);
chooseDrillAnswer(writingChoices.find((choice) => choice !== writingQuestion.ans));
if (app.state.drill.mark !== 'wrong' || app.state.drill.idx !== 0) {
  violated(DRILL_FLOW_CHECK, 'a wrong kanji spelling did not keep the question on screen', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(writingQuestion.ans);
if (app.state.drill.idx !== 1 || app.state.drill.perfect !== 0) {
  violated(DRILL_FLOW_CHECK, 'choosing the right kanji spelling did not advance without first-try credit', JSON.stringify(app.state.drill));
}

if ([...drillStorage.keys()].join(',') !== 'kt_drill_v1') {
  violated(DRILL_FLOW_CHECK, `the drill wrote outside its own storage key: ${[...drillStorage.keys()].join(',')}`, 'storage isolation');
}
app.exitDrill();

// --- aggregate checks -------------------------------------------------------------------

const missingKana = [...GOJUON].filter((kana) => !romajiKanaSeen.has(kana));
if (missingKana.length) {
  violated('the romaji drill covers the gojuon in the current standard spelling', `the drill never teaches ${missingKana.join('')}`, '');
}
for (const [kana, standard] of Object.entries(STANDARD_ROMAJI)) {
  const spellings = romajiSpellingsSeen.get(kana);
  if (!spellings) continue;
  if (!spellings.has(standard)) {
    violated(
      'the romaji drill covers the gojuon in the current standard spelling',
      `${kana} is never spelled the standard way (${standard}); seen ${[...spellings].join('/')}`,
      '',
    );
  }
}

for (const [check, count] of observed) {
  if (count === 0) violated(check, 'the check never saw a matching question, so it protects nothing', '');
}
const emptyChecks = [...observed.entries()].filter(([, count]) => count === 0);

// --- report -----------------------------------------------------------------------------

const summary = [...observed.entries()]
  .sort(([left], [right]) => left.localeCompare(right))
  .map(([check, count]) => `  ${check}: ${count} observed`)
  .join('\n');

if (violations.size === 0 && emptyChecks.length === 0) {
  console.log(`generated-question audit passed: ${generated} questions across ${UNITS.length} units x ${STAGES.length} stages`);
  console.log(summary);
  process.exit(0);
}

console.error(`generated-question audit failed on ${generated} generated questions`);
for (const [key, entry] of [...violations.entries()].sort(([, a], [, b]) => b.count - a.count)) {
  console.error(`\n[${entry.count}] ${key}`);
  for (const sample of entry.samples) {
    if (sample) console.error(`    ${sample}`);
  }
}
console.error(`\n${summary}`);
process.exit(1);
