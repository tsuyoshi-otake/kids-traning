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
  settings: { topics: Object.fromEntries(TOPICS.map((topic) => [topic, true])) },
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
  violated('schema migration is idempotent', 'a second migration changed profile data', 'legacy -> v6 -> v6');
}
for (let index = 0; index < legacyProfiles.length; index += 1) {
  const before = legacyProfiles[index];
  const after = migratedOnce[index];
  if (after.learningSchema !== 6 || Object.keys(after.unitStats || {}).length !== UNITS.length) {
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
};
const revisionProfile = app.migrateProfiles([{
  name: 'revision-change',
  grade: revisionUnit.grade,
  stars: 123,
  xp: 456,
  learningSchema: 6,
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
  revisionStat.recentQuestionFingerprints.join(',') !== 'oldest,newest'
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
      beforeSchema !== 6 ||
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
}
app.state.session = null;

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
