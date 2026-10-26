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
import { auditThinkingQuestions } from './ThinkingQuestionAudit.mjs';
import { auditQuestionQuality } from './QuestionQualityAudit.mjs';
import { createFuriganaCorpusAudit } from './FuriganaCorpusAudit.mjs';

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
assertFurigana('受賞', 'じゅしょう');
assertFurigana('仲間', 'なかま');
assertFurigana('落選', 'らくせん');

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
for (const [id, stages] of [['math.g4.number-calculation', [2, 3]], ['math.g5.number-calculation', [3, 4]]]) {
  const unit = app.curriculumUnit(id);
  for (const item of unit.questions.filter(item => stages.includes(item.stage))) {
    const question = app.pickCurriculumBank(unit, item.stage, item);
    observe('upper calculation banks consistently select written arithmetic');
    if (question.writtenArithmetic !== true || !app.writtenArithmeticPlan(question)) {
      violated('upper calculation banks consistently select written arithmetic', `${id}: ${item.prompt}`, JSON.stringify(question));
    }
  }
}
{
  const prompts = new Set();
  for (const grade of [7, 8, 9]) for (const slug of ['language-classics', 'communication-reading-writing']) {
    const unit = app.curriculumUnit(`japanese.g${grade}.${slug}`);
    observe('middle Japanese assesses distinct contextual skills at every stage');
    if (JSON.stringify(unit.questions.map(item => item.stage)) !== '[1,2,3,4,5]') violated('middle Japanese assesses distinct contextual skills at every stage', 'missing stage coverage', unit.id);
    for (const item of unit.questions) {
      if (prompts.has(item.prompt) || !/[「『【]/.test(item.prompt)) violated('middle Japanese assesses distinct contextual skills at every stage', 'duplicated or context-free question', item.prompt);
      prompts.add(item.prompt);
    }
  }
}
{
  const flatten = value => Array.isArray(value) ? value.flatMap(flatten) : value && typeof value === 'object' ? [value, ...flatten(value.children || [])] : [];
  const calculations = {
    'math.g4.relations-data': [f=>f.values[2]-f.values[0],f=>f.table[0][1]+f.table[1][1]],
    'math.g5.rate-statistics': [f=>40*f.values[0]/100,f=>f.values[1]+f.values[2]],
    'math.g6.ratio-data': [f=>f.values[2]+f.values[3],f=>100*f.values[1]/f.values.reduce((a,b)=>a+b,0)],
    'math.g7.proportion-functions': [f=>f.values[4],f=>f.values[4]/Number(f.labels[4])],
    'math.g8.linear-functions': [f=>f.values[0],f=>f.values[3]-f.values[1]],
    'math.g8.probability-distribution': [f=>f.values[3]-f.values[1],f=>f.values[4]-f.values[0]],
    'math.g9.quadratic-functions': [f=>f.values[1],f=>(f.values[6]-f.values[4])/(Number(f.labels[6])-Number(f.labels[4]))],
  };
  for(const [id, calculate] of Object.entries(calculations)) {
    const unit=app.curriculumUnit(id),items=unit.questions.filter(item=>item.figure);
    for(const [index,item] of items.entries()){
      observe('data figure answers agree with independent readings and accessible tables');
      const q=app.pickCurriculumBank(unit,item.stage,item),f=q.figure,nodes=flatten(app.dataFigureView(f));
      const expected=calculate[index](f),numbers=nodes.filter(node=>node.type==='td').flatMap(node=>node.children).map(Number);
      if(parseFloat(item.answer)!==expected || JSON.stringify(numbers)!==JSON.stringify(f.kind==='table'?f.table.flat():f.values) || !nodes.some(node=>node.type==='table') || (f.kind!=='table'&&!nodes.some(node=>node.type==='svg'))) violated('data figure answers agree with independent readings and accessible tables','answer or accessible values differ',JSON.stringify(item));
    }
    if(items.length!==calculate.length) violated('data figure answers agree with independent readings and accessible tables','missing authored diagram',id);
  }
}
const writtenCases = [
  { name: 'multi-digit addition', question: { topic: 'hissan', difficulty: 5, prompt: '1234 + 111', answer: '1345' }, kind: 'addition', expects: ['5', '4', '3', '1'] },
  { name: 'three-addend addition', question: { topic: 'hissan', difficulty: 4, writtenArithmetic: true, prompt: '38 + 47 + 29', answer: '114' }, kind: 'addition', expects: ['24', '11'] },
  { name: 'multi-digit subtraction', question: { topic: 'hissan', difficulty: 5, prompt: '9000 - 111', answer: '8889' }, kind: 'subtraction', expects: ['9', '8', '8', '8'] },
  { name: 'three-by-one multiplication', question: { topic: 'hissan', difficulty: 5, prompt: '123 × 7', answer: '861' }, kind: 'multiplication', expects: ['21', '16', '8'] },
  { name: 'two-by-two multiplication', question: { topic: 'hissan', difficulty: 5, prompt: '12 × 34', answer: '408' }, kind: 'multiplication', expects: ['8', '4', '6', '3', '8', '10', '4'] },
  { name: '864 long division', question: { topic: 'kazu', difficulty: 2, prompt: '864÷24は？', answer: '36' }, kind: 'division', expects: ['3', '72', '14', '144', '6', '144', '0'] },
  { name: 'one-place decimal multiplication', question: { topic: 'kazu', difficulty: 3, prompt: '3.6×4は？', answer: '14.4' }, kind: 'multiplication', expects: ['36', '24', '14', '1', '14.4'] },
  { name: 'two-place decimal multiplication', question: { topic: 'kazu', difficulty: 3, prompt: '2.4×0.5は？', answer: '1.2' }, kind: 'multiplication', expects: ['24', '5', '20', '12', '2', '1.2'] },
  { name: 'decimal long division', question: { topic: 'kazu', difficulty: 4, prompt: '3.6÷0.9は？', answer: '4' }, kind: 'division', expects: ['36', '9', '4', '36', '0'] },
  { name: 'remainder long division', question: { topic: 'div', difficulty: 5, prompt: '157 ÷ 9', answer: '17 あまり 4' }, kind: 'division', expects: ['1', '9', '6', '67', '7', '63', '4'] },
];
const CALIB_WRITTEN_CHECK = 'placement check uses a blank written-arithmetic board for written questions';
const calibBoardMarkup = page.includes('class="kt-written-step-board kt-calibration-written-board"') &&
  page.includes('list="{{ calibWrittenLines }}"') && page.includes('value="{{ calibIsWritten }}"');
observe(CALIB_WRITTEN_CHECK);
if (!calibBoardMarkup) violated(CALIB_WRITTEN_CHECK, 'generated placement check has no written board', runtimePagePath);
for (const question of [
  { topic: 'hissan', difficulty: 2, prompt: '52 + 18', answer: '70' },
  { topic: 'hissan', difficulty: 2, prompt: '52 - 18', answer: '34' },
  ...writtenCases.map(sample => sample.question),
  { topic: 'mul', difficulty: 2, prompt: '9 × 8', answer: '72' },
  { topic: 'div', difficulty: 2, prompt: '72 ÷ 8', answer: '9' },
]) {
  observe(CALIB_WRITTEN_CHECK);
  const calibApp = new app.constructor();
  calibApp.props = calibApp.props || {};
  calibApp.state.screen = 'calib';
  calibApp.state.setupGrade = 2;
  calibApp.state.calib = { items: [{ q: question, choices: [question.answer, '0', '1', '2'] }], idx: 0 };
  try {
    const view = calibApp.renderVals();
    const written = question.topic === 'hissan' || writtenCases.some(sample => sample.question === question);
    const lines = view.calibWrittenLines || [];
    if (view.calibIsWritten !== written || view.calibIsPlain === written ||
        (written && (!lines.some(line => line.tone === 'number') || !lines.some(line => line.tone === 'rule') ||
          lines.some(line => line.tone === 'result' && line.text.trim()) || !view.calibWrittenAria)) ||
        view.calibChoices.length !== 4) {
      violated(CALIB_WRITTEN_CHECK, `${question.prompt} rendered incorrectly`, JSON.stringify({written:view.calibIsWritten,plain:view.calibIsPlain,lines,choices:view.calibChoices.length}));
    }
  } catch (error) {
    violated(CALIB_WRITTEN_CHECK, `${question.prompt} threw ${error && error.message}`, String(error && error.stack));
  }
}
for (const grade of [2, 3, 4, 5, 6]) {
  const calibApp = new app.constructor();
  calibApp.props = calibApp.props || {};
  calibApp.state.setupGrade = grade;
  const items = calibApp.buildCalib().filter(item => item.q.topic === 'hissan');
  observe(CALIB_WRITTEN_CHECK, items.length);
  for (const item of items) {
    calibApp.state.screen = 'calib';
    calibApp.state.calib = { items: [item], idx: 0 };
    const view = calibApp.renderVals();
    if (!view.calibIsWritten || view.calibIsPlain || !view.calibWrittenLines.some(line => line.tone === 'rule')) {
      violated(CALIB_WRITTEN_CHECK, `grade ${grade} generated ${item.q.prompt} without a written board`, JSON.stringify(view.calibWrittenLines));
    }
  }
}

// Calibration draws each topic three times; a repeated draw must be rerolled so the
// first-run check never shows the same question twice. Stub two identical 'add' draws,
// then a second canned value; the retry path must swap the duplicate out.
{
  const probe = new app.constructor();
  probe.state.setupGrade = 2;
  const realAdd = probe.buildCalib().find((item) => item.q.topic === 'add').q;
  const dedupeApp = new app.constructor();
  dedupeApp.state.setupGrade = 2;
  const original = dedupeApp.genFor.bind(dedupeApp);
  const canned = { ...realAdd, prompt: '監査用の おきかえ もんだい1' };
  const cannedNext = { ...realAdd, prompt: '監査用の おきかえ もんだい2' };
  let addCalls = 0;
  dedupeApp.genFor = (kind, profile) => (kind === 'add' ? { ...(addCalls++ < 2 ? canned : cannedNext) } : original(kind, profile));
  observe('first-run calibration retries a repeated question', 1);
  const adds = dedupeApp.buildCalib().filter((item) => item.q.topic === 'add');
  const distinct = new Set(adds.map((item) => app.questionIdentity(item.q))).size;
  if (adds.length !== 3 || distinct !== 2) {
    violated('first-run calibration retries a repeated question', `expected canned + rerolled, got ${distinct} distinct`, JSON.stringify(adds.map((item) => item.q.prompt)));
  }
}
for (const [left, right, answer] of [[700,286,414],[802,467,335],[1000,1,999],[654,321,333],[52,18,34]]) {
  const plan=app.writtenArithmeticPlan({topic:'hissan',writtenArithmetic:true,prompt:`${left} - ${right}`});
  observe('subtraction regroups across zero without negative intermediate digits');
  const result=Number(plan.steps.map(step=>step.writeDigit).reverse().join(''));
  if(result!==answer) violated('subtraction regroups across zero without negative intermediate digits', 'wrong final subtraction', `${left}-${right}`);
  for(const [index,step] of plan.steps.entries()) {
    const recomposed=step.regrouped.reduce((sum,value,column)=>sum+value*10**column,0);
    const view=app.writtenArithmeticView(plan,index);
    if(recomposed!==left || step.regrouped.some(value=>value<0) || /-1から|−1から/.test(JSON.stringify(view))) violated('subtraction regroups across zero without negative intermediate digits','regrouping changed the number or exposed negative digits',JSON.stringify(view));
    const resultLine=view.lines.find(line=>line.tone==='result').text.replace(/\s/g,'');
    const expectedVisible=plan.steps.slice(0,index).map(item=>item.writeDigit).reverse().join('');
    if(resultLine!==expectedVisible) violated('subtraction regroups across zero without negative intermediate digits','unfinished subtraction digit exposed',JSON.stringify(view));
  }
  if(left===700 && JSON.stringify(plan.steps[0].regrouped)!=='[10,9,6]') violated('subtraction regroups across zero without negative intermediate digits','700 was not decomposed into 6 hundreds, 9 tens and 10 ones',JSON.stringify(plan));
}
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

for (const sample of writtenCases.filter(sample => sample.name.includes('decimal multiplication'))) {
  const originals = { finish: app.finishScoredQuestion, sfx: app.sfx };
  let finished = 0;
  app.sfx = () => {};
  app.finishScoredQuestion = () => { finished++; app._terminalQuestionToken = app.currentQuestionToken(); };
  app.state = { screen: 'quiz', session: { attempt: 1, idx: 0, questions: [sample.question] }, input: '', waStep: 0, waMistakes: 0 };
  app._terminalQuestionToken = ''; app._answerBusy = false;
  const plan = app.writtenArithmeticPlan(sample.question);
  for (const step of plan.steps.slice(0, -1)) app.submitWrittenStep(step.expect);
  observe('decimal multiplication requires the placed decimal result before scoring');
  if (finished || app.state.waStep !== plan.steps.length - 1) violated('decimal multiplication requires the placed decimal result before scoring', 'finished after counting decimal places', sample.name);
  app.submitWrittenStep(String(Number(sample.question.answer) * 10));
  if (finished || app.state.waMistakes !== 1) violated('decimal multiplication requires the placed decimal result before scoring', 'misplaced decimal accepted', sample.name);
  app.submitWrittenStep(sample.question.answer);
  app.submitWrittenStep(sample.question.answer);
  const view = app.writtenArithmeticView(plan, plan.steps.length);
  if (finished !== 1 || !view.lines.some(line => line.text === '答え　' + sample.question.answer)) violated('decimal multiplication requires the placed decimal result before scoring', 'final result absent or duplicate scoring', sample.name);
  Object.assign(app, { finishScoredQuestion: originals.finish, sfx: originals.sfx });
}

for(const [expected,answer,accepted] of [['27','27.0',true],['14.4','14.40',true],['1.2','1.20',true],['1.2','12',false],['1.2','1.2.0',false],['0.6','',false]]){
  observe('written decimal products accept equivalent trailing zeroes only');
  if(app.writtenStepAnswerMatches({phase:'decimal-result',expect:expected},answer)!==accepted) violated('written decimal products accept equivalent trailing zeroes only','decimal equivalence mismatch',`${expected}: ${answer}`);
}

{
  const flatten=value=>Array.isArray(value)?value.flatMap(flatten):value&&typeof value==='object'?[value,...flatten(value.children||[])]:[];
  for(const value of [0,5,7,10,23,99,105,999]){
    observe('soroban represents place value with one five-bead and four one-beads');
    const digits=app.sorobanDigits(value);
    if(app.sorobanNumber(digits)!==value) violated('soroban represents place value with one five-bead and four one-beads','round trip changed value',String(value));
    const q={mode:'num',soroban:{kind:'read',initial:value,steps:[]}};
    const buttons=flatten(app.sorobanView(q)).filter(node=>node.props?.className==='kt-soroban-bead');
    if(buttons.length!==15 || buttons.filter(node=>node.props['aria-label'].includes('五珠')).length!==3 || buttons.some(node=>!node.props.disabled)) violated('soroban represents place value with one five-bead and four one-beads','wrong bead geometry or readable board was editable',String(value));
    for(let column=0;column<3;column++){
      const rod=buttons.slice(column*5,column*5+5),shown=(rod[0].props['aria-pressed']?5:0)+rod.slice(1).filter(node=>node.props['aria-pressed']).length;
      if(shown!==digits[column]) violated('soroban represents place value with one five-bead and four one-beads','rendered beads changed digit',`${value} column${column}`);
    }
  }
  const originals={finish:app.finishScoredQuestion,exhaust:app.exhaustQuestion,sfx:app.sfx};
  for(let index=0;index<15;index++){
    const q=app.sorobanQuestion(index),s=q.soroban,addition=q.prompt.match(/で (\d+)に(\d+)を/),subtraction=q.prompt.match(/で (\d+)から(\d+)を/);
    const expected=addition?Number(addition[1])+Number(addition[2]):subtraction?Number(subtraction[1])-Number(subtraction[2]):s.kind==='read'?s.initial:Number(q.prompt.match(/^\d+/)[0]);
    observe('soroban authored operations reach their arithmetic result');
    if(expected!==Number(q.answer)||(s.steps.length&&s.steps.at(-1).target!==expected)) violated('soroban authored operations reach their arithmetic result','plan and arithmetic differ',JSON.stringify(q));
  }
  let finished=0,exhausted=0;
  app.sfx=()=>{};
  app.finishScoredQuestion=()=>{finished++;app._terminalQuestionToken=app.currentQuestionToken();};
  app.exhaustQuestion=()=>{exhausted++;app._terminalQuestionToken=app.currentQuestionToken();};
  const begin=index=>{const q=app.sorobanQuestion(index);app.state={...app.freshQ(),screen:'quiz',session:{attempt:index,idx:0,questions:[q],rolePlan:['soroban']}};return q;};
  begin(11);app.submitSoroban();
  observe('soroban operations require bead movement and finalize exactly once');
  if(finished||app.state.sbMiss||app._answerBusy) violated('soroban operations require bead movement and finalize exactly once','untouched board was scored',JSON.stringify(app.state));
  app.moveSorobanBead(2,4);app.submitSoroban();app.submitSoroban();
  if(finished!==1||app.sorobanNumber(app.state.sbBoard)!==9) violated('soroban operations require bead movement and finalize exactly once','6+3 failed or scored twice',JSON.stringify(app.state));
  const subtraction=begin(12);
  app.moveSorobanBead(2,0);app.moveSorobanBead(2,3);
  const originalSetState=app.setState;let renderedDisabled=false;
  app.setState=function(patch,callback){originalSetState.call(this,patch,()=>{renderedDisabled=flatten(this.sorobanView(subtraction)).filter(node=>node.props?.className==='kt-soroban-bead').some(node=>node.props.disabled);callback?.();});};
  app.submitSoroban();app.setState=originalSetState;
  if(renderedDisabled) violated('soroban operations require bead movement and finalize exactly once','intermediate render disabled all beads before callback released the guard',JSON.stringify(app.state));
  if(finished!==1||app.state.sbStep!==1||app.sorobanNumber(app.state.sbBoard)!==18) violated('soroban operations require bead movement and finalize exactly once','complement step did not preserve 18',JSON.stringify(app.state));
  const snapshot={...app.state};
  if(!app.validSorobanCheckpoint(subtraction,snapshot)||app.validSorobanCheckpoint(subtraction,{...snapshot,sbBoard:[0,1,10]})||app.validSorobanCheckpoint(subtraction,{...snapshot,sbStep:2})||app.validSorobanCheckpoint(subtraction,{...snapshot,sbMiss:'bad'})) violated('soroban operations require bead movement and finalize exactly once','checkpoint accepts invalid board or step',JSON.stringify(snapshot));
  app.moveSorobanBead(1,1);app.submitSoroban();app.submitSoroban();
  if(finished!==2||app.sorobanNumber(app.state.sbBoard)!==8||app._answerBusy) violated('soroban operations require bead movement and finalize exactly once','12-4 failed or scored twice',JSON.stringify(app.state));
  const terminalBoard=JSON.stringify(app.state.sbBoard);app.moveSorobanBead(2,0);
  if(JSON.stringify(app.state.sbBoard)!==terminalBoard) violated('soroban operations require bead movement and finalize exactly once','terminal board remained editable',JSON.stringify(app.state));
  begin(11);app.moveSorobanBead(2,1);app.submitSoroban();app.submitSoroban();app.submitSoroban();
  if(exhausted!==1||app._answerBusy) violated('soroban operations require bead movement and finalize exactly once','third miss did not finalize once',JSON.stringify(app.state));
  begin(12);app.moveSorobanBead(2,0);app.submitSoroban();app.resetSoroban();
  if(app.sorobanNumber(app.state.sbBoard)!==12||app.state.sbStep!==0||app.state.sbMiss!==1||app.state.sbMoved) violated('soroban operations require bead movement and finalize exactly once','reset lost mistakes or failed to reset work',JSON.stringify(app.state));
  Object.assign(app,{finishScoredQuestion:originals.finish,exhaustQuestion:originals.exhaust,sfx:originals.sfx});
  const priorStorage=globalThis.localStorage,originalProfile=app.curP,originalValid=app.validLearningCheckpoint,storage=new Map();
  globalThis.localStorage={getItem:key=>storage.get(key)??null,setItem:(key,value)=>storage.set(key,String(value)),removeItem:key=>storage.delete(key)};
  app.curP=()=>({name:'soroban-audit'});app.state=snapshot;
  const checkpoint=app.checkpointState();storage.set(app.learningCheckpointKey(),JSON.stringify(checkpoint));app.state={};app.validLearningCheckpoint=value=>app.validSorobanCheckpoint(subtraction,value);
  observe('soroban checkpoint resumes the exact complement operation');
  if(!app.restoreLearningCheckpoint()||app.sorobanNumber(app.state.sbBoard)!==18||app.state.sbStep!==1||app.state.sbMoved!==false) violated('soroban checkpoint resumes the exact complement operation','saved operation changed on restore',JSON.stringify(app.state));
  app.curP=originalProfile;app.validLearningCheckpoint=originalValid;
  if(priorStorage===undefined)delete globalThis.localStorage;else globalThis.localStorage=priorStorage;
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
  'order', 'soroban', 'shakai', 'rika', 'gijutsu', 'sougou',
  'keyboard', 'thinking',
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
// Valid readings of an asked word: a bare kanji admits its on/kun alternates (姉→あね|し),
// while an inflected word reads only one way (広い→ひろい, never こう).
const readingsByAskedWord = new Map();
for (const entry of curriculum) {
  const word = entry.word || entry.k;
  if (!readingsByWord.has(word)) readingsByWord.set(word, new Set());
  readingsByWord.get(word).add(entry.r);
  const addReadings = (key, values) => {
    if (!readingsByAskedWord.has(key)) readingsByAskedWord.set(key, new Set());
    for (const reading of values) if (reading) readingsByAskedWord.get(key).add(reading);
  };
  addReadings(entry.k, [entry.r, entry.on, entry.kun]);
  addReadings(word, [entry.r]);
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
  'ひつじ', 'にじ', 'すじ', 'いばら',
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
auditQuestionQuality(app, UNITS, profileFor, observe, violated);
const furiganaCorpus = createFuriganaCorpusAudit(app, UNITS);
auditThinkingQuestions(app, observe, violated);
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
      let valid = (q.writtenArithmetic === true || q.subtype === 'check') && q.difficulty === stage;
      if (q.subtype === 'check') {
        // No.14/20: たしかめ算 — the shown result must be reproduced by the check expression.
        const m = String(q.prompt).match(/^(\d+) ([+\-]) (\d+) = (\d+) の たしかめ　(\d+) \+ (\d+) は？$/);
        valid &&= !!m && (m[2] === '+' ? Number(m[1]) + Number(m[3]) : Number(m[1]) - Number(m[3])) === Number(m[4])
          && Number(m[5]) + Number(m[6]) === Number(q.answer)
          && Number(q.answer) === Number(m[2] === '+' ? m[4] : m[1])
          && (q.op === '＋' ? stage === 2 : stage === 3);
      } else if (q.subtype === 'three-add') {
        // Issue #95: 3つの 数の たし算 — two-digit addends written on one board, stage 4 only.
        valid &&= unit.grade === 2 && stage === 4 && q.op === '＋' && [q.a, q.b, q.c].every((n) => n >= 10 && n < 100)
          && q.prompt === `${q.a} + ${q.b} + ${q.c}` && q.answer === String(q.a + q.b + q.c) && !!app.writtenArithmeticPlan(q);
      } else {
      if (unit.grade === 2 && stage === 1) valid &&= q.a >= 10 && q.a < 100 && q.b >= 10 && q.b < 100 && (q.op === '＋' ? carryCount(q.a, q.b) === 0 : borrowCount(q.a, q.b) === 0);
      if (unit.grade === 2 && stage === 2) valid &&= q.op === '＋' && carryCount(q.a, q.b) >= 1;
      if (unit.grade === 2 && stage === 3) valid &&= q.op === '−' && borrowCount(q.a, q.b) >= 1;
      if (unit.grade === 2 && stage >= 4 && q.b >= 100) valid &&= q.a >= 100 && (stage < 5 || (q.op === '＋' ? carryCount(q.a, q.b) >= 2 : borrowCount(q.a, q.b) >= 2));
      // No.34-36/40: 答えが 三桁に なる 加算（二桁＋二桁、三桁＋一・二桁）。
      if (unit.grade === 2 && stage === 4 && q.b < 100) valid &&= q.op === '＋' && q.a + q.b >= 100 && q.a + q.b <= 999;
      // No.37-39: 百のくらいを ふくむ 減算（三桁−一・二桁、100 - 8 型）。
      if (unit.grade === 2 && stage === 5 && q.b < 100) valid &&= q.op === '−' && q.a >= 100 && q.b < q.a && borrowCount(q.a, q.b) >= 1;
      }
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
// Issue #85 — 2年上 workbook (No.2-44): every listed problem type must be reachable
// through the real grade-2 generators at the stage where it is taught.
const CHECK_G2_WORKBOOK = 'grade 2 generators cover the 2年上 workbook problem types';
{
  const g2profile = beginnerAtGrade(2);
  const toCmMm = (t) => (t % 10 === 0 ? `${t / 10}cm` : `${Math.floor(t / 10)}cm${t % 10}mm`);
  const parseCmMm = (text) => { const m = String(text).match(/^(\d+)cm(?:(\d+)mm)?$/); return m ? Number(m[1]) * 10 + Number(m[2] || 0) : NaN; };
  const parseLDl = (text) => { const m = String(text).match(/^(?:(\d+)L)?(?:(\d+)dL)?$/); return m && (m[1] || m[2]) ? Number(m[1] || 0) * 10 + Number(m[2] || 0) : NaN; };
  const cases = [
    ['math.g2.add', 4, 'round-ten', 240, (q) => (q.n1 + q.n2) % 10 === 0 && q.n1 >= 10 && q.n1 < 100 && q.n2 >= 1 && q.n2 <= 9 && q.answer === String(q.n1 + q.n2), '16 + 4'],
    ['math.g2.add', 4, 'tens-add-100', 240, (q) => q.n1 % 10 === 0 && q.n2 % 10 === 0 && q.n1 + q.n2 > 100 && q.answer === String(q.n1 + q.n2), '70 + 60'],
    ['math.g2.add', 5, 'hundreds-add', 240, (q) => q.n1 % 100 === 0 && q.n2 % 100 === 0 && q.n1 + q.n2 <= 1000 && q.answer === String(q.n1 + q.n2), '500 + 200'],
    ['math.g2.add', 5, 'paren-add', 240, (q) => { const m = String(q.prompt).match(/^(\d+) \+ \((\d+) \+ (\d+)\)$/); return !!m && Number(q.answer) === Number(m[1]) + Number(m[2]) + Number(m[3]) && (Number(m[2]) + Number(m[3])) % 10 === 0; }, '16 + (8 + 2)'],
    ['math.g2.sub', 3, 'tens-sub-one', 240, (q) => q.a % 10 === 0 && q.b >= 1 && q.b <= 9 && q.answer === String(q.a - q.b), '40 - 7'],
    ['math.g2.sub', 4, 'tens-sub-100', 240, (q) => q.a % 10 === 0 && q.a > 100 && q.b % 10 === 0 && q.b < q.a && q.answer === String(q.a - q.b), '130 - 50'],
    ['math.g2.sub', 5, 'hundreds-sub', 240, (q) => q.a % 100 === 0 && q.b % 100 === 0 && q.b < q.a && q.answer === String(q.a - q.b), '1000 - 600'],
    ['math.g2.hissan', 2, 'check', 120, (q) => q.op === '＋' && /の たしかめ/.test(String(q.prompt)), '48 + 27 の たしかめ'],
    ['math.g2.hissan', 3, 'check', 120, (q) => q.op === '−' && /の たしかめ/.test(String(q.prompt)), '72 - 48 の たしかめ'],
    ['math.g2.clock', 5, 'minutes-before', 800, (q) => { const m = String(q.prompt).match(/^(\d+)時(\d+)分 の (\d+)分前は？$/); return !!m && q.answer === `${m[1]}時${Number(m[2]) - Number(m[3])}分` && Number(m[2]) > Number(m[3]); }, '3時40分 の 10分前は？'],
    ['math.g2.clock', 5, 'elapsed-hours', 800, (q) => { const m = String(q.prompt).match(/^午前(\d+)時 から 午後(\d+)時 まで 何時間？$/); return !!m && q.answer === `${Number(m[2]) + 12 - Number(m[1])}時間`; }, '午前8時 から 午後3時'],
    ['math.g2.clock', 5, 'elapsed-minutes', 800, (q) => { const m = String(q.prompt).match(/^(\d+)時(\d+)分 から (\d+)時(\d+)分 まで 何分？$/); return !!m && q.answer === `${Number(m[3]) * 60 + Number(m[4]) - Number(m[1]) * 60 - Number(m[2])}分`; }, '9時10分 から 9時40分'],
    ['math.g2.measure', 5, 'mm-cmmm', 500, (q) => { const m = String(q.prompt).match(/^(\d+)mm は 何cm何mm？$/); return !!m && q.answer === toCmMm(Number(m[1])) && Number(m[1]) % 10 !== 0; }, '46mm は 4cm6mm'],
    ['math.g2.measure', 5, 'cmmm-add', 500, (q) => { const m = String(q.prompt).match(/^(\d+cm(?:\d+mm)?) \+ (\d+cm(?:\d+mm)?) は？$/); return !!m && q.answer === toCmMm(parseCmMm(m[1]) + parseCmMm(m[2])); }, '2cm3mm + 4cm5mm'],
    ['math.g2.measure', 5, 'cmmm-sub', 500, (q) => { const m = String(q.prompt).match(/^(\d+cm(?:\d+mm)?) - (\d+cm(?:\d+mm)?) は？$/); return !!m && parseCmMm(m[1]) > parseCmMm(m[2]) && q.answer === toCmMm(parseCmMm(m[1]) - parseCmMm(m[2])); }, '4cm5mm - 2cm8mm'],
    ['math.g2.measure', 5, 'dl-ldl', 500, (q) => { const m = String(q.prompt).match(/^(\d+)dL は 何L何dL？$/); return !!m && q.answer === `${Math.floor(Number(m[1]) / 10)}L${Number(m[1]) % 10}dL`; }, '15dL は 1L5dL'],
    ['math.g2.measure', 5, 'dl-add', 500, (q) => { const m = String(q.prompt).match(/^(\d+)dL \+ (\d+)dL は？$/); return !!m && q.answer === `1L${Number(m[1]) + Number(m[2]) - 10}dL` && parseLDl(q.answer) === Number(m[1]) + Number(m[2]); }, '3dL + 8dL'],
    ['math.g2.measure', 5, 'dl-sub', 500, (q) => { const m = String(q.prompt).match(/^1L - (\d+)dL は？$/); return !!m && q.answer === `${10 - Number(m[1])}dL`; }, '1L - 4dL'],
    ['math.g2.measure', 5, 'ldl-mixed', 500, (q) => { const m = String(q.prompt).match(/^(\d+L\d+dL) ([+\-]) (\d+L\d+dL|\d+L|\d+dL) は？$/); if (!m) return false; const t = m[2] === '+' ? parseLDl(m[1]) + parseLDl(m[3]) : parseLDl(m[1]) - parseLDl(m[3]); return t >= 10 && q.answer === (t % 10 === 0 ? `${t / 10}L` : `${Math.floor(t / 10)}L${t % 10}dL`); }, '4L2dL + 1L3dL / 1L3dL - 3dL'],
    ['math.g2.measure', 5, 'l-ml', 500, (q) => { const m = String(q.prompt).match(/^(\d+)L は 何mL？$/); return !!m && q.answer === `${Number(m[1]) * 1000}mL`; }, '2L は 2000mL'],
    ['math.g2.measure', 5, 'dl-ml', 500, (q) => { const m = String(q.prompt).match(/^(\d+)dL は 何mL？$/); return !!m && q.answer === `${Number(m[1]) * 100}mL`; }, '3dL は 300mL'],
    ['math.g2.measure', 5, 'ml-ldl', 500, (q) => { const m = String(q.prompt).match(/^(\d+)mL は 何L何dL？$/); return !!m && q.answer === `${Math.floor(Number(m[1]) / 1000)}L${(Number(m[1]) % 1000) / 100}dL`; }, '1500mL は 1L5dL'],
    ['math.g2.kazu', 1, 'kanji-num', 240, (q) => /^「[一二三四五六七八九十百]+」を すうじで かくと？$/.test(String(q.prompt)) && q.readingTarget && Number(q.answer) >= 100 && Number(q.answer) <= 999, '「三百五十二」を すうじで'],
    ['math.g2.kazu', 2, 'number-line', 240, (q) => { const m = String(q.prompt).match(/^数直線の ならび (\d+)、(\d+)、□、(\d+)　□は？$/); if (!m) return false; const step = Number(m[2]) - Number(m[1]); return step > 0 && Number(m[3]) - Number(m[2]) === 2 * step && Number(q.answer) === Number(m[2]) + step; }, '240、250、□、270'],
    ['math.g2.kazu', 3, 'make-1000', 240, (q) => q.answer === '1000', '100を 10こ / 999の つぎ'],
    ['math.g2.story', 2, 'story-tens', 300, (q) => Number(q.answer) > 0 && /なん(?:こ|本|まい|さつ)？$/.test(String(q.prompt)), '34本 + 30本'],
    ['math.g2.story', 2, 'story-combine', 300, (q) => { const m = String(q.prompt).match(/^1くみが (\d+)人、2くみが (\d+)人。あわせて 何人？$/); return !!m && Number(q.answer) === Number(m[1]) + Number(m[2]); }, '24人 + 35人'],
    ['math.g2.story', 2, 'story-group', 300, (q) => { const m = String(q.prompt).match(/^バスに (\d+)人 のっていました。つぎの バスていで (\d+)人と (\d+)人が のりました。ぜんぶで 何人？$/); return !!m && Number(q.answer) === Number(m[1]) + Number(m[2]) + Number(m[3]); }, '10人に 4人と 2人'],
    ['math.g2.story', 3, 'story-reverse', 300, (q) => q.isTape === true && Array.isArray(q.tapeParts) && Number(q.answer) === q.tapeParts.reduce((s, p) => s + Number(p), 0) && /ぜんぶは なん/.test(String(q.prompt)), 'つかった+のこり=ぜんぶ'],
    ['math.g2.chart', 3, 'chart-total', 600, (q) => q.isChart === true && q.isTable === true && Number(q.answer) === (q.rows || []).reduce((s, r) => s + r.count, 0) * (q.chartScale || 1), '表の 数の 合計'],
    ['math.g2.order', 2, 'paren-add', 200, (q) => { const m = String(q.prompt).match(/^\(?(\d+)\)? \+ \(\)?(\d+)\)? \+ \(\)?(\d+)\)? は？$/) || String(q.prompt).match(/^(\d+) \+ \((\d+) \+ (\d+)\) は？$/) || String(q.prompt).match(/^\((\d+) \+ (\d+)\) \+ (\d+) は？$/); return !!m && Number(q.answer) === Number(m[1]) + Number(m[2]) + Number(m[3]); }, '(4 + 5) + 7'],
    ['math.g2.order', 4, 'expr-cmp', 240, (q) => { const m = String(q.prompt).match(/^(\d+) ([+\-]) (\d+) □ (\d+)　□に はいる しるしは？$/); if (!m) return false; const lv = m[2] === '+' ? Number(m[1]) + Number(m[3]) : Number(m[1]) - Number(m[3]); const rv = Number(m[4]); return q.answer === (lv === rv ? '＝' : lv > rv ? '＞' : '＜'); }, '60 + 70 □ 120'],
  ];
  observe(CHECK_G2_WORKBOOK, cases.length);
  for (const [unitId, stage, subtype, draws, validate, example] of cases) {
    const seen = [];
    for (let i = 0; i < draws; i += 1) {
      const q = app.genFor(unitId, g2profile, stage);
      if (q.subtype === subtype) seen.push(q);
    }
    if (!seen.length) {
      violated(CHECK_G2_WORKBOOK, `${unitId} stage ${stage} never produced ${subtype}`, example);
      continue;
    }
    const bad = seen.find((q) => !validate(q));
    if (bad) violated(CHECK_G2_WORKBOOK, `${unitId} stage ${stage} produced an invalid ${subtype}`, JSON.stringify(bad));
  }
}
// Issue #95 — 小学2年算数 topic list (study.jitenon.jp): every added problem type is
// reachable at its stage, and each answer is recomputed here from the generated numbers.
const CHECK_G2_TOPICS = 'grade 2 generators cover the 小学2年算数 topic list';
{
  const g2profile = beginnerAtGrade(2);
  const mulProfile = beginnerAtGrade(2);
  mulProfile.unitStats['math.g2.mul'].multiplicationFacts = Object.fromEntries(
    app.multiplicationStageFactKeys(5).map((key) => [key, { strength: 1, attempts: 1, errors: 0, lastAttemptAt: 1 }]));
  const toCmMm = (t) => (t % 10 === 0 ? `${t / 10}cm` : `${Math.floor(t / 10)}cm${t % 10}mm`);
  const parseCmMm = (text) => { const m = String(text).match(/^(\d+)cm(?:(\d+)mm)?$/); return m ? Number(m[1]) * 10 + Number(m[2] || 0) : NaN; };
  const parseLDl = (text) => { const m = String(text).match(/^(?:(\d+)L)?(?:(\d+)dL)?$/); return m && (m[1] || m[2]) ? Number(m[1] || 0) * 10 + Number(m[2] || 0) : NaN; };
  const toMCm = (t) => (t % 100 === 0 ? `${t / 100}m` : `${t >= 100 ? `${Math.floor(t / 100)}m` : ''}${t % 100}cm`);
  const parseMCm = (text) => { const m = String(text).match(/^(?:(\d+)m)?(?:(\d+)cm)?$/); return m && (m[1] || m[2]) ? Number(m[1] || 0) * 100 + Number(m[2] || 0) : NaN; };
  const kanjiValue = (word) => {
    let total = 0;
    let digit = 0;
    for (const ch of word) {
      const d = '一二三四五六七八九'.indexOf(ch);
      if (d >= 0) { digit = d + 1; continue; }
      const unit = { 十: 10, 百: 100, 千: 1000 }[ch];
      if (!unit) return NaN;
      total += (digit || 1) * unit;
      digit = 0;
    }
    return total + digit;
  };
  const product = (text) => { const m = String(text).match(/^(\d) × (\d)$/); return m ? Number(m[1]) * Number(m[2]) : NaN; };
  const shapeTruth = {
    '3本の 直線で かこまれた 形を 何と いう？': '三角形',
    '4本の 直線で かこまれた 形を 何と いう？': '四角形',
    '長方形には 直角の かどが いくつ ある？': '4つ',
    '直角三角形には 直角の かどが いくつ ある？': '1つ',
    '正方形の 4つの へんの 長さは どう なって いる？': '4つとも 同じ',
    '長方形の むかいあう へんの 長さは どう なって いる？': '同じ',
    'はこの形で、同じ 形の 面は いくつずつ ある？': '2つずつ',
    'さいころの形の 面は、どんな 形？': '正方形',
    'ひごと ねんどだまで はこの形を つくる。ねんどだまは 何こ いる？': '8こ',
    'ひごと ねんどだまで はこの形を つくる。ひごは 何本 いる？': '12本',
  };
  const cases = [
    ['math.g2.kazu', 4, 'place-4digit', 400, (q) => { const m = String(q.prompt).match(/^1000が ([1-9])こ、100が (\d)こ、10が (\d)こ、1が (\d)こで？$/); return !!m && q.answer === String(Number(m[1]) * 1000 + Number(m[2]) * 100 + Number(m[3]) * 10 + Number(m[4])); }, '1000が 3こ、100が 0こ…'],
    ['math.g2.kazu', 4, 'digit-place', 400, (q) => { const m = String(q.prompt).match(/^([1-9]\d{3}) の (千|百|十|一)の くらいの 数字は？$/); return !!m && q.answer === m[1]['千百十一'.indexOf(m[2])] && q.choices.length === 4 && new Set(q.choices).size === 4 && q.choices.every((c) => /^\d$/.test(c)); }, '4618 の 百の くらい'],
    ['math.g2.kazu', 4, 'kanji-num-4', 400, (q) => { const m = String(q.prompt).match(/^「([一二三四五六七八九十百千]+)」を すうじで かくと？$/); return !!m && q.readingTarget === m[1] && Number(q.answer) === kanjiValue(m[1]) && Number(q.answer) >= 1000 && Number(q.answer) <= 9999; }, '「三千五十二」'],
    ['math.g2.kazu', 4, 'hundreds-count', 400, (q) => { const p = String(q.prompt); const a = p.match(/^100を (\d+)こ あつめた 数は？$/); const b = p.match(/^(\d+) は 100を 何こ あつめた 数？$/); return a ? q.answer === String(Number(a[1]) * 100) : !!b && Number(q.answer) * 100 === Number(b[1]); }, '100を 23こ / 3600は 100を 何こ'],
    ['math.g2.kazu', 5, 'make-10000', 400, (q) => { const p = String(q.prompt); const rest = p.match(/^(\d+) と あと いくつで 10000？$/); if (rest) return Number(rest[1]) + Number(q.answer) === 10000; return ({ '1000を 10こ あつめた 数は？': '10000', '10000より 1 小さい 数は？': '9999', '10000は 100を 何こ あつめた 数？': '100' })[p] === q.answer; }, '1000を 10こ / あと いくつで 10000'],
    ['math.g2.kazu', 5, 'number-line-big', 400, (q) => { const m = String(q.prompt).match(/^数直線の ならび (\d+)、(\d+)、□、(\d+)　□は？$/); if (!m) return false; const step = Number(m[2]) - Number(m[1]); return (step === 100 || step === 1000) && Number(m[1]) >= 1000 && Number(m[3]) - Number(m[2]) === 2 * step && Number(q.answer) === Number(m[2]) + step; }, '7400、7500、□、7700'],
    ['math.g2.kazu', 5, 'compare-4digit', 400, (q) => /^いちばん 大きい かずは？ /.test(String(q.prompt)) && q.choices.length === 4 && new Set(q.choices).size === 4 && q.answer === String(Math.max(...q.choices.map(Number))) && q.choices.every((c) => /^[1-9]\d{3}$/.test(c) && c[0] === q.choices[0][0]), '4けたの 大小'],
    ['math.g2.add', 5, 'thousands-add', 400, (q) => q.prompt === `${q.n1} + ${q.n2}` && q.answer === String(q.n1 + q.n2) && ((q.n1 % 1000 === 0 && q.n2 % 1000 === 0) || (q.n1 % 100 === 0 && q.n2 % 100 === 0 && q.n1 + q.n2 > 1000)), '700 + 500 / 3000 + 4000'],
    ['math.g2.add', 5, 'hundreds-tens-add', 400, (q) => q.prompt === `${q.n1} + ${q.n2}` && q.answer === String(q.n1 + q.n2) && q.n1 > 100 && q.n1 % 10 === 0 && q.n1 % 100 !== 0 && q.n2 % 10 === 0 && q.n2 < 100 && q.n1 + q.n2 < 1000, '480 + 70'],
    ['math.g2.sub', 5, 'thousands-sub', 400, (q) => q.prompt === `${q.a} - ${q.b}` && q.answer === String(q.a - q.b) && q.a - q.b > 0 && q.a >= 1000 && ((q.a % 1000 === 0 && q.b % 1000 === 0) || (q.a % 100 === 0 && q.b % 100 === 0 && q.b < 1000)), '1300 - 500 / 8000 - 3000'],
    ['math.g2.sub', 5, 'hundreds-tens-sub', 400, (q) => q.prompt === `${q.a} - ${q.b}` && q.answer === String(q.a - q.b) && q.a > 100 && q.a % 10 === 0 && q.a % 100 !== 0 && q.b % 10 === 0 && q.b < 100, '530 - 40'],
    ['math.g2.hissan', 4, 'three-add', 300, (q) => {
      const m = String(q.prompt).match(/^(\d{2}) \+ (\d{2}) \+ (\d{2})$/);
      if (!m || q.writtenArithmetic !== true || q.answer !== String(Number(m[1]) + Number(m[2]) + Number(m[3]))) return false;
      const plan = app.writtenArithmeticPlan(q);
      const ones = [m[1], m[2], m[3]].reduce((sum, n) => sum + Number(n[1]), 0);
      const tens = [m[1], m[2], m[3]].reduce((sum, n) => sum + Number(n[0]), 0) + Math.floor(ones / 10);
      const numberLines = plan ? app.writtenArithmeticView(plan, 0).lines.filter((line) => line.tone === 'number').length : 0;
      return !!plan && plan.third === m[3] && JSON.stringify(plan.steps.map((step) => step.expect)) === JSON.stringify([String(ones), String(tens)]) && numberLines === 3;
    }, '23 + 45 + 18'],
    ['math.g2.measure', 3, 'length-story', 600, (q) => { const p = String(q.prompt); const join = p.match(/^長さ (\S+) の テープと (\S+) の テープを つなぐと、ぜんぶで 何cm何mm？$/); const cut = p.match(/^長さ (\S+) の テープから (\S+) 切りとると、のこりは 何cm何mm？$/); const t = join ? parseCmMm(join[1]) + parseCmMm(join[2]) : cut ? parseCmMm(cut[1]) - parseCmMm(cut[2]) : NaN; return t > 10 && t % 10 !== 0 && q.answer === toCmMm(t); }, '12cm + 8cm5mm'],
    ['math.g2.measure', 5, 'dl-cups', 800, (q) => { const m = String(q.prompt).match(/^1dLますで (\d+)かい はかった 水の かさは 何L何dL？$/); return !!m && Number(m[1]) % 10 !== 0 && /^\d+L\d+dL$/.test(q.answer) && parseLDl(q.answer) === Number(m[1]); }, '13かい → 1L3dL'],
    ['math.g2.measure', 5, 'cm-mcm', 800, (q) => { const m = String(q.prompt).match(/^(\d+)cm は 何m(何cm)?？$/); return !!m && Number(m[1]) >= 100 && !!m[2] === (Number(m[1]) % 100 !== 0) && q.answer === toMCm(Number(m[1])); }, '135cm → 1m35cm'],
    ['math.g2.measure', 5, 'mcm-add', 800, (q) => { const m = String(q.prompt).match(/^(\S+) \+ (\S+) は？$/); return !!m && q.answer === toMCm(parseMCm(m[1]) + parseMCm(m[2])); }, '1m70cm + 40cm'],
    ['math.g2.measure', 5, 'mcm-sub', 800, (q) => { const m = String(q.prompt).match(/^(\S+) - (\S+) は？$/); return !!m && parseMCm(m[1]) > parseMCm(m[2]) && q.answer === toMCm(parseMCm(m[1]) - parseMCm(m[2])); }, '2m50cm - 80cm'],
    ['math.g2.clock', 5, 'elapsed-cross-hour', 1000, (q) => { const m = String(q.prompt).match(/^(\d+)時(\d+)分 から (\d+)時(\d+)分 まで 何分？$/); return !!m && Number(m[3]) === Number(m[1]) + 1 && q.answer === `${Number(m[3]) * 60 + Number(m[4]) - Number(m[1]) * 60 - Number(m[2])}分`; }, '9時40分 から 10時20分'],
    ['math.g2.clock', 5, 'hour-minute-convert', 1000, (q) => { const p = String(q.prompt); const toMin = p.match(/^(\d+)時間(\d+)分 は 何分？$/); const toHour = p.match(/^(\d+)分 は 何時間何分？$/); if (toMin) return q.answer === `${Number(toMin[1]) * 60 + Number(toMin[2])}分`; return !!toHour && Number(toHour[1]) > 60 && q.answer === `${Math.floor(Number(toHour[1]) / 60)}時間${Number(toHour[1]) % 60}分`; }, '1時間20分 = 80分'],
    ['math.g2.clock', 5, 'hour-shift', 1000, (q) => { const m = String(q.prompt).match(/^(\d+)時(\d+)分 の 1時間(あと|まえ)の 時こくは？$/); return !!m && q.answer === `${Number(m[1]) + (m[3] === 'あと' ? 1 : -1)}時${m[2]}分`; }, '3時20分 の 1時間あと'],
    ['math.g2.mul', 5, 'kuku-rule-step', 1200, (q) => { const p = String(q.prompt); const blank = p.match(/^(\d) × (\d) = (\d) × (\d) \+ □　□に はいる 数は？$/); const diff = p.match(/^(\d) × (\d) の こたえは、(\d) × (\d) の こたえより いくつ 大きい？$/); const m = blank || diff; return !!m && m[1] === m[3] && Number(m[2]) === Number(m[4]) + 1 && Number(q.answer) === Number(m[1]) * Number(m[2]) - Number(m[3]) * Number(m[4]); }, '7 × 6 = 7 × 5 + □', mulProfile],
    ['math.g2.mul', 5, 'kuku-same-product', 1200, (q) => { const m = String(q.prompt).match(/^こたえが (\d+) に なる 九九は どれ？$/); return !!m && q.choices.length === 4 && q.choices.includes(q.answer) && product(q.answer) === Number(m[1]) && q.choices.filter((c) => product(c) === Number(m[1])).length === 1; }, 'こたえが 24 に なる 九九', mulProfile],
    ['math.g2.mul', 5, 'kuku-swap-blank', 1200, (q) => { const m = String(q.prompt).match(/^(\d) × (\d) = (\d) × □　□に はいる 数は？$/); return !!m && m[2] === m[3] && Number(m[1]) * Number(m[2]) === Number(m[3]) * Number(q.answer); }, '4 × 6 = 6 × □', mulProfile],
    ['math.g2.groups', 5, 'times-of', 400, (q) => { const p = String(q.prompt); const len = p.match(/^(\d+)cmの テープの (\d+)ばいの 長さは？$/); const times = p.match(/^(\d+)cmの テープは、(\d+)cmの テープの 何ばい？$/); if (len) return q.answer === `${Number(len[1]) * Number(len[2])}cm`; return !!times && Number(times[1]) % Number(times[2]) === 0 && q.answer === `${Number(times[1]) / Number(times[2])}ばい`; }, '4cmの 3ばい / 12cmは 4cmの 何ばい'],
    ['math.g2.shape', 3, 'shape-define', 800, (q) => shapeTruth[q.prompt] === q.answer && q.choices.includes(q.answer), '3本の 直線で かこまれた 形'],
    ['math.g2.shape', 3, 'shape-cut', 800, (q) => q.answer === '直角三角形' && q.isShape === true && /を、むかいあう ちょう点を むすぶ 直線で 切ると/.test(String(q.prompt)), '正方形を 切ると 直角三角形'],
    ['math.g2.shape', 3, 'rect-perimeter', 800, (q) => { const p = String(q.prompt); const sq = p.match(/^1つの へんが (\d+)cmの 正方形。まわりの 長さは？$/); const rect = p.match(/^たて (\d+)cm、よこ (\d+)cmの 長方形。まわりの 長さは？$/); if (sq) return q.answer === `${Number(sq[1]) * 4}cm`; return !!rect && rect[1] !== rect[2] && q.answer === `${2 * (Number(rect[1]) + Number(rect[2]))}cm`; }, 'たて 3cm、よこ 5cm'],
    ['math.g2.shape', 4, 'box-build', 800, (q) => shapeTruth[q.prompt] === q.answer && q.choices.includes(q.answer), 'ねんどだまは 8こ'],
    ['math.g2.shape', 4, 'box-edges', 800, (q) => { const m = String(q.prompt).match(/^たて (\d+)cm、よこ (\d+)cm、高さ (\d+)cmの はこの形を ひごで つくる。(\d+)cmの ひごは 何本 いる？$/); return !!m && new Set([m[1], m[2], m[3]]).size === 3 && [m[1], m[2], m[3]].includes(m[4]) && q.answer === '4本'; }, '4cmの ひごは 4本'],
    ['math.g2.frac', 4, 'frac-quantity', 600, (q) => { const m = String(q.prompt).match(/^(\d+)この 1\/(\d) は 何こ？$/); return !!m && Number(m[1]) % Number(m[2]) === 0 && q.answer === `${Number(m[1]) / Number(m[2])}こ` && q.isGroups === true && q.groupCount === Number(m[2]) && q.groupSize === Number(m[1]) / Number(m[2]); }, '12この 1/3'],
    ['math.g2.story', 3, 'story-find-change', 600, (q) => { const m = String(q.prompt).match(/^.+?が (\d+)(\D+?) ありました。いくつか もらったので (\d+)\2に なりました。もらったのは なん\2？$/); return !!m && q.isTape === true && Number(q.answer) === Number(m[3]) - Number(m[1]) && Number(q.answer) > 0; }, '25こ → 41こ'],
    ['math.g2.story', 3, 'story-find-used', 600, (q) => { const m = String(q.prompt).match(/^.+?が (\d+)(\D+?) ありました。いくつか つかったので のこりが (\d+)\2に なりました。つかったのは なん\2？$/); return !!m && q.isTape === true && Number(q.answer) === Number(m[1]) - Number(m[3]) && Number(q.answer) > 0; }, '43こ → のこり 18こ'],
    ['math.g2.story', 3, 'story-find-start', 600, (q) => { const m = String(q.prompt).match(/^.+?を いくつか もっていました。(\d+)(\D+?) もらったので (\d+)\2に なりました。はじめは なん\2？$/); return !!m && q.isTape === true && Number(q.answer) === Number(m[3]) - Number(m[1]) && Number(q.answer) > 0; }, 'いくつか + 12こ = 35こ'],
    ['math.g2.story', 3, 'story-compare', 600, (q) => { const m = String(q.prompt).match(/^あおいさんは .+?を (\d+)(\D+?) もっています。けんさんは あおいさんより (\d+)\2 (多く|少なく) もっています。けんさんは なん\2？$/); return !!m && Number(q.answer) === Number(m[1]) + (m[4] === '多く' ? 1 : -1) * Number(m[3]); }, 'あおいさんより 9こ 少ない'],
    ['math.g2.money', 3, 'price-sum', 400, (q) => { const m = String(q.prompt).match(/^(.+?)が (\d+)円、(.+?)が (\d+)円。2つ かうと いくら？$/); return !!m && m[1] !== m[3] && q.answer === `${Number(m[2]) + Number(m[4])}円`; }, '120円 + 80円'],
    ['math.g2.money', 5, 'price-sum', 400, (q) => { const m = String(q.prompt).match(/^(.+?)が (\d+)円、(.+?)が (\d+)円。2つ かうと いくら？$/); return !!m && m[1] !== m[3] && q.answer === `${Number(m[2]) + Number(m[4])}円`; }, '650円 + 480円'],
    ['math.g2.order', 5, 'paren-story', 400, (q) => { const m = String(q.prompt).match(/^ノート (\d+)円と、えんぴつ (\d+)円・けしゴム (\d+)円を かいます。/); return !!m && Number(m[2]) + Number(m[3]) === 100 && q.answer === `${m[1]} + (${m[2]} + ${m[3]})` && q.choices.includes(q.answer); }, '120 + (60 + 40)'],
  ];
  observe(CHECK_G2_TOPICS, cases.length);
  for (const [unitId, stage, subtype, draws, validate, example, profile = g2profile] of cases) {
    const seen = [];
    for (let i = 0; i < draws; i += 1) {
      const q = app.genFor(unitId, profile, stage);
      if (q.subtype === subtype) seen.push(q);
    }
    if (!seen.length) {
      violated(CHECK_G2_TOPICS, `${unitId} stage ${stage} never produced ${subtype}`, example);
      continue;
    }
    const bad = seen.find((q) => !validate(q));
    if (bad) violated(CHECK_G2_TOPICS, `${unitId} stage ${stage} produced an invalid ${subtype}`, JSON.stringify(bad));
  }
  // ばい starts in grade 2, so the grade-1 groups unit that shares pickGroups must never ask it.
  const CHECK_G1_TIMES = 'grade 1 groups never ask ばい questions';
  observe(CHECK_G1_TIMES, 400);
  const g1profile = beginnerAtGrade(1);
  for (let i = 0; i < 400; i += 1) {
    const q = app.genFor('math.g1.groups', g1profile, 5);
    if (q.subtype === 'times-of' || /ばい/.test(String(q.prompt))) { violated(CHECK_G1_TIMES, 'math.g1.groups stage 5 asked a ばい question', JSON.stringify(q)); break; }
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
  const reasoningPoolSize = app.curriculumBankPool(thinkingGradeOne, 1).length;
  for (let index = 0; index < reasoningPoolSize; index += 1) {
    crossSessionPrompts.push(app.generateSessionQuestion(crossSessionProfile, reasoningSession(), 'target').prompt);
  }
  const recycledPrompt = app.generateSessionQuestion(crossSessionProfile, reasoningSession(), 'target').prompt;
  const recentFingerprints = crossSessionProfile.unitStats[thinkingGradeOne.id].recentQuestionFingerprints;
  if (
    new Set(crossSessionPrompts).size !== reasoningPoolSize ||
    recycledPrompt !== crossSessionPrompts[0] ||
    recentFingerprints.length !== reasoningPoolSize
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
    promptCounts.size !== reasoningPoolSize ||
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
        furiganaCorpus.record(question, { grade: unit.grade, unit: unit.id, stage, kind: 'generated' });
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
            if (!step || !/^\d+(?:\.\d+)?$/.test(String(step.expect)) || !step.prompt || !step.explain || !step.completeText) {
              violated('generated written arithmetic has a complete finite step plan', `step ${stepIndex} is incomplete`, `${context} | ${JSON.stringify(step)}`);
            }
          }
          const compact = String(question.prompt).replace(/\s+/g, '').replace(/は？$/, '').replace(/[？?]$/, '');
          // 3つの 数の たし算は 2つめと 3つめを まとめた a + (b + c) として 答えを たしかめる。
          const triple = compact.match(/^(\d+)\+(\d+)\+(\d+)$/);
          const arithmetic = triple ? [compact, triple[1], '+', String(Number(triple[2]) + Number(triple[3]))] : compact.match(/^(\d+(?:\.\d+)?)([+\-−×÷])(\d+(?:\.\d+)?)$/);
          if (!arithmetic) {
            violated('generated written arithmetic has a complete finite step plan', 'a non-arithmetic prompt entered the written-work gate', context);
          } else {
            const left = Number(arithmetic[1]);
            const right = Number(arithmetic[3]);
            const operator = arithmetic[2] === '−' ? '-' : arithmetic[2];
            const expected = operator === '+' ? String(left + right) : operator === '-' ? String(left - right) : operator === '×' ? String(left * right) : null;
            if (expected !== null && Math.abs(Number(question.answer) - Number(expected)) > 1e-10) {
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

        if (topic === 'clock' && grade === 2) {
          observe('grade-2 clock stays within the 12-hour clock');
          if (/^(1[3-9]|2[0-3])時/.test(answer) || /時刻で あらわす|午前・午後で いうと|24時間制/.test(prompt)) {
            violated('grade-2 clock stays within the 12-hour clock', `${prompt} -> ${answer}`, context);
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
            observe('pictographs disclose scale before the learner answers');
            const expectedScale = grade >= 3 && stage >= 4 ? 5 : grade >= 3 && stage >= 3 ? 2 : 1;
            const expectedUnit = expectedScale > 1 ? '人' : 'こ';
            if (question.chartScale !== expectedScale || question.chartUnit !== expectedUnit ||
                !prompt.includes(`1ますは ${expectedScale}${expectedUnit}。`)) {
              violated('pictographs disclose scale before the learner answers', 'scale or unit is absent or inconsistent', context);
            }
            if (prompt.includes('より いくつ 多い？')) {
              const counts = question.rows.map(row => row.count);
              const expectedDifference = (Math.max(...counts) - Math.min(...counts)) * expectedScale;
              if (Number(answer) !== expectedDifference) {
                violated('pictographs disclose scale before the learner answers', 'the answer differs from the displayed scaled difference', context);
              }
            }
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
            .filter((choice) => (readingsByAskedWord.get(choice) || new Set()).has(askedReading));
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
          observe('reading questions never offer another valid reading of the asked word');
          const targetReadings = readingsByAskedWord.get(shown) || new Set();
          const alternateReading = (question.choices || [])
            .map(String)
            .filter((choice) => choice !== answer)
            .filter((choice) => targetReadings.has(choice));
          if (alternateReading.length) {
            violated(
              'reading questions never offer another valid reading of the asked word',
              `${alternateReading.join('、')} also reads ${shown}`,
              `${context} choices=${JSON.stringify(question.choices)}`,
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
const DRILL_OPERATORS = { '＋': (x, y) => x + y, '−': (x, y) => x - y, '×': (x, y) => x * y, '÷': (x, y) => x / y };
const solveDrillPrompt = (text) => {
  // Issue #95: （ ）の ある けいさん — the bracket is worked first, from either side.
  const frontGroup = /^\((\d+) (＋|−) (\d+)\) (＋|−) (\d+)$/.exec(String(text));
  if (frontGroup) return DRILL_OPERATORS[frontGroup[4]](DRILL_OPERATORS[frontGroup[2]](Number(frontGroup[1]), Number(frontGroup[3])), Number(frontGroup[5]));
  const backGroup = /^(\d+) (＋|−) \((\d+) (＋|−) (\d+)\)$/.exec(String(text));
  if (backGroup) return DRILL_OPERATORS[backGroup[2]](Number(backGroup[1]), DRILL_OPERATORS[backGroup[4]](Number(backGroup[3]), Number(backGroup[5])));
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

// Issue #103: a missed learning question waits on the feedback screen until the child enters the
// shown answer by the same means the question used. The copy is practice: it never scores.
const ANSWER_COPY_CHECK = 'a missed learning question moves on only after the shown answer is entered again';
{
  const priorState = app.state, priorSfx = app.sfx, priorSetTimeout = globalThis.setTimeout, timers = [];
  const missed = (q, attempt) => {
    app.state = { screen: 'feedback', input: '', answerCopy: null, lastResult: { correct: false, q }, session: { attempt, idx: 0, questions: [q], rolePlan: ['kazu'] } };
  };
  app.sfx = () => {};
  globalThis.setTimeout = (fn, ms) => { timers.push({ fn, ms }); return timers.length; };
  try {
    observe(ANSWER_COPY_CHECK, 7);
    if (!page.includes('class="kt-answer-copy"') || !page.includes('{{ answerCopy.show }}') || !page.includes('{{ answerCopy.showNext }}')) {
      violated(ANSWER_COPY_CHECK, 'the feedback screen has no answer-copy panel', runtimePagePath);
    }
    missed({ mode: 'num', prompt: '9 × 8', answer: '72' }, 1);
    const sessionBefore = JSON.stringify(app.state.session);
    app.next();
    if (app.state.screen !== 'feedback' || !app.answerCopyPending()) {
      violated(ANSWER_COPY_CHECK, 'つぎへ left a missed question before its answer was entered', JSON.stringify(app.state));
    }
    app.answerCopyInput('8');
    if (!app.state.answerCopy || app.state.answerCopy.input !== '' || app.state.answerCopy.miss !== 1 || !app.answerCopyPending()) {
      violated(ANSWER_COPY_CHECK, 'a digit that does not match the shown answer was kept', JSON.stringify(app.state.answerCopy));
    }
    app.answerCopyInput('7');
    app.answerCopyInput('2');
    if (!app.state.lastResult.copied || app.answerCopyPending() || timers.length !== 1 || timers[0].ms !== app.answerCopyAdvanceMs() ||
        JSON.stringify(app.state.session) !== sessionBefore) {
      violated(ANSWER_COPY_CHECK, 'entering the shown answer did not move on by itself, or it changed the session', JSON.stringify(app.state));
    }
    let advanced = 0;
    app.next = () => { advanced += 1; };
    app.state.session.idx = 1;
    timers[0].fn();
    app.state.session.idx = 0;
    timers[0].fn();
    delete app.next;
    if (advanced !== 1) violated(ANSWER_COPY_CHECK, `the copy timer advanced ${advanced} time(s)`, 'expected once, and never for another question');
    missed({ mode: 'choices', prompt: 'どれ？', answer: 'いぬ', choices: ['ねこ', 'いぬ', 'とり', 'うま'] }, 2);
    app.answerCopyInput('1');
    app.answerCopyChoose('ねこ');
    if (!app.state.answerCopy || app.state.answerCopy.miss !== 1 || !app.answerCopyPending()) {
      violated(ANSWER_COPY_CHECK, 'a different choice was accepted as the copy', JSON.stringify(app.state));
    }
    app.answerCopyChoose('いぬ');
    if (!app.state.lastResult.copied) violated(ANSWER_COPY_CHECK, 'choosing the shown answer did not count', JSON.stringify(app.state));
    missed({ mode: 'type', prompt: 'ねこ', answer: 'Cat' }, 3);
    for (const key of ['c', 'A', 't']) app.answerCopyInput(key);
    if (!app.state.lastResult.copied) violated(ANSWER_COPY_CHECK, 'typing the shown word did not count regardless of case', JSON.stringify(app.state));
    missed({ mode: 'num', prompt: '7 ＋ 8', answer: '15' }, 4);
    app.answerCopyInput('1');
    app.state.session.idx = 1;
    if (app.answerCopyState().input !== '') violated(ANSWER_COPY_CHECK, 'keys typed for one question carried over to the next', JSON.stringify(app.state.answerCopy));
    missed({ mode: 'num', prompt: '3 ÷ 4', answer: '3/4' }, 5);
    if (app.answerCopyPending()) violated(ANSWER_COPY_CHECK, 'an answer with no entry method blocked つぎへ', JSON.stringify(app.state.lastResult));
    app.state.lastResult = { correct: true, q: { mode: 'num', answer: '12' } };
    if (app.answerCopyPending()) violated(ANSWER_COPY_CHECK, 'a correct answer asked to be entered again', '');
  } catch (error) {
    violated(ANSWER_COPY_CHECK, 'the answer copy threw', String(error && error.stack));
  } finally {
    globalThis.setTimeout = priorSetTimeout;
    if (Object.prototype.hasOwnProperty.call(app, 'next')) delete app.next;
    app.sfx = priorSfx;
    app.state = priorState;
  }
}

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
const DRILL_COPY_CHECK = 'a revealed drill answer has to be entered or chosen again before the run moves on';
const DRILL_RETIRE_CHECK = 'a drill question leaves later runs after three first-try answers in a row at six options';
const DRILL_SPEECH_CHECK = 'a correct drill answer is read aloud as one sentence and the drill waits for the reading';
const DRILL_COLUMN_CHECK = 'the written-arithmetic drill takes its answer one digit at a time from the ones place';

const drillCourses = app.drillCourses();
observe(DRILL_COURSE_CHECK, drillCourses.length);
if (drillCourses.length !== 8) {
  violated(DRILL_COURSE_CHECK, `expected eight 100-question courses, found ${drillCourses.length}`, JSON.stringify(drillCourses.map((c) => c.id)));
}
const DRILL_FIRST_PROMPT = { g1: '1 ＋ 1', g2: '2 × 1', d3: '2 ÷ 1', h2n: '21 ＋ 11', h2: '24 ＋ 16', p2: '(2 ＋ 1) ＋ 1' };
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
const ARITHMETIC_DRILLS = ['g1', 'g2', 'd3', 'h2n', 'h2', 'p2'];
observe(DRILL_CHOICE_CHECK, ARITHMETIC_DRILLS.length * 100);
for (const courseId of ARITHMETIC_DRILLS) {
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

// Issues #87 and #89: each arithmetic question carries its own option count (2-6),
// persisted inside the course record. A first-attempt correct answer raises the count by
// one and a first-attempt miss lowers it by one, so a mastered fact already asks for
// sharper recognition on the next run while a shaky one stays friendly.
const DRILL_STEP_CHECK = 'each drill question steps its own option count between 2 and 6 with first-attempt results';
const DRILL_SIX_CHECK = 'every arithmetic drill question offers six valid unique options at count 6';
const DRILL_POSITION_CHECK = 'the drill answer position spreads evenly across every option count';
const DRILL_STABLE_CHECK = 'a wrong first try does not change the options of the question on screen';
const DRILL_RECORD_CHECK = 'drill progress persists per-question option counts and drops invalid data';

observe(DRILL_SIX_CHECK, ARITHMETIC_DRILLS.length * 100);
observe(DRILL_POSITION_CHECK, ARITHMETIC_DRILLS.length * 5);
for (const courseId of ARITHMETIC_DRILLS) {
  const bank = app.drillBank(courseId);
  for (const count of [2, 3, 4, 5, 6]) {
    const seed = courseId.charCodeAt(0) * 7919 + count;
    const drill = { id: courseId, answerMode: 'choice', choiceSeed: seed, choiceOrder: app.drillChoiceOrder(bank.length, seed), count: count };
    const positions = new Array(count).fill(0);
    for (const question of bank) {
      const choices = app.drillNumericChoices(drill, question);
      const correctAt = choices.indexOf(question.ans);
      if (choices.length !== count || correctAt < 0) {
        violated(DRILL_POSITION_CHECK, `${courseId} question ${question.no} offers ${JSON.stringify(choices)} at count ${count}`, question.text);
        continue;
      }
      positions[correctAt] += 1;
      if (count !== 6) continue;
      if (new Set(choices).size !== 6 || choices.filter((choice) => choice === question.ans).length !== 1 ||
          choices.some((choice) => !Number.isInteger(choice) || choice < 0)) {
        violated(DRILL_SIX_CHECK, `${courseId} question ${question.no} offers ${JSON.stringify(choices)}`, question.text);
        continue;
      }
      const wrong = choices.filter((choice) => choice !== question.ans);
      const fact = /^(\d+) × (\d+)$/.exec(question.text);
      if (courseId === 'g2' && fact) {
        const left = Number(fact[1]), right = Number(fact[2]);
        for (const distractor of wrong) {
          const row = distractor / left;
          if (!Number.isInteger(row) || row < 1 || row > 9 || row === right) {
            violated(DRILL_SIX_CHECK, `${courseId} question ${question.no} distractor ${distractor} is not a same-row fact`, question.text);
          }
        }
      } else {
        const allowed = new Set(courseId === 'd3' ? [1, 2, 3, 4, 5] : [1, 2, 3, 4, 5, 10, 20]);
        for (const distractor of wrong) {
          if (!allowed.has(Math.abs(distractor - question.ans))) {
            violated(DRILL_SIX_CHECK, `${courseId} question ${question.no} distractor ${distractor} is not a plausible near miss`, question.text);
          }
        }
      }
    }
    if (Math.max(...positions) - Math.min(...positions) > 1) {
      violated(DRILL_POSITION_CHECK, `${courseId} count ${count} puts the answer at ${positions.join(' / ')}`, 'expected a near-even spread');
    }
  }
}

// Drive the real run the way a child would, restarting the course between attempts: the
// first question's dial must climb on its own while every other question stays at two.
observe(DRILL_STEP_CHECK, 10);
drillStorage.clear();
app.state.screen = 'start';
const stepOpt = (no) => (app.readDrillProgress().g1.opts[no] || { c: 2 });
const stepCorrect = () => {
  app.startDrill('g1', true, 'choice');
  const question = app.drillQuestion();
  app.drillChoose(app.drillChoices(app.state.drill, question).find((choice) => choice === question.ans));
  app.drillFlushEcho();
};
stepCorrect();
if (stepOpt(1).c !== 3) {
  violated(DRILL_STEP_CHECK, `one correct answer recorded ${JSON.stringify(stepOpt(1))}`, 'expected count 3');
}
app.startDrill('g1', true, 'choice');
if (app.drillChoices(app.state.drill, app.drillQuestion()).length !== 3) {
  violated(DRILL_STEP_CHECK, 'a question at count 3 did not offer three options on the next run', '');
}
for (let round = 0; round < 3; round += 1) stepCorrect();
if (stepOpt(1).c !== 6) {
  violated(DRILL_STEP_CHECK, `four consecutive correct answers reached ${JSON.stringify(stepOpt(1))}`, 'expected count 6');
}
app.startDrill('g1', true, 'choice');
if (app.drillChoices(app.state.drill, app.drillQuestion()).length !== 6) {
  violated(DRILL_STEP_CHECK, 'a question at count 6 did not offer six options', '');
}
stepCorrect();
stepCorrect();
// Issue #104: at six the dial cannot rise, so the first-try answers count toward retirement.
if (stepOpt(1).c !== 6 || JSON.stringify(JSON.parse(drillStorage.get('kt_drill_v1')).g1.opts['1']) !== JSON.stringify({ c: 6, t: 2 })) {
  violated(DRILL_STEP_CHECK, `the dial did not stay persisted at six with two answers toward retirement: ${JSON.stringify(stepOpt(1))}`, drillStorage.get('kt_drill_v1'));
}
if (stepOpt(2).c !== 2 || stepOpt(3).c !== 2) {
  violated(DRILL_STEP_CHECK, 'questions that were never answered left their dial', JSON.stringify(app.readDrillProgress().g1.opts));
}

// A first-attempt miss steps the same question down, persists at once, and never lets the
// options on screen change while the question is still up.
observe(DRILL_STABLE_CHECK, 4);
app.startDrill('g1', true, 'choice');
const stableQuestion = app.drillQuestion(), stableBefore = app.drillChoices(app.state.drill, stableQuestion);
app.drillChoose(stableBefore.find((choice) => choice !== stableQuestion.ans));
const stableAfter = app.drillChoices(app.state.drill, app.drillQuestion());
if (stableBefore.length !== 6 || stableAfter.length !== 6 || JSON.stringify(stableAfter) !== JSON.stringify(stableBefore)) {
  violated(DRILL_STABLE_CHECK, 'a wrong first try changed the on-screen options', JSON.stringify([stableBefore, stableAfter]));
}
if (stepOpt(1).c !== 5) {
  violated(DRILL_STEP_CHECK, `a first-attempt miss left ${JSON.stringify(stepOpt(1))}`, 'expected count 5');
}
app.drillChoose(stableAfter.find((choice) => choice === stableQuestion.ans));
app.drillFlushEcho();
if (stepOpt(1).c !== 5) {
  violated(DRILL_STEP_CHECK, 'a retry moved the dial a second time', JSON.stringify(stepOpt(1)));
}

// The re-queued "again" pass must not touch the record: a revealed question that is later
// answered correctly in the same run keeps only its first-pass miss, while a question seen
// for the first time does record its result.
app.startDrill('g1', true, 'choice');
const requeueChoices = app.drillChoices(app.state.drill, app.drillQuestion());
if (requeueChoices.length !== 5) {
  violated(DRILL_STEP_CHECK, `a restarted question at count 5 offered ${requeueChoices.length} options`, 'expected the lowered count to persist');
}
app.drillChoose(requeueChoices.find((choice) => choice !== app.drillQuestion().ans));
app.drillChoose(requeueChoices.find((choice) => choice !== app.drillQuestion().ans));
// Copying the revealed answer (issue #103) is the only way on, and it leaves the dial alone.
app.drillChoose(app.drillQuestion().ans);
app.drillFlushEcho();
let requeueGuard = 0;
while (!app.state.drill.done && requeueGuard < 200) {
  requeueGuard += 1;
  app.drillChoose(app.drillQuestion().ans);
  app.drillFlushEcho();
}
if (stepOpt(1).c !== 4) {
  violated(DRILL_STEP_CHECK, `the again pass moved the dial to ${JSON.stringify(stepOpt(1))}`, 'expected count 4');
}
if (stepOpt(2).c !== 3) {
  violated(DRILL_STEP_CHECK, 'a first-pass correct answer was not recorded', JSON.stringify(stepOpt(2)));
}
// Missing the same question at count 2 must stay at 2; it takes three more misses to get
// there from count 4.
for (let round = 0; round < 3; round += 1) {
  app.startDrill('g1', true, 'choice');
  const lowQuestion = app.drillQuestion();
  app.drillChoose(app.drillChoices(app.state.drill, lowQuestion).find((choice) => choice !== lowQuestion.ans));
}
if (stepOpt(1).c !== 2) {
  violated(DRILL_STEP_CHECK, `a miss at count 2 left ${JSON.stringify(stepOpt(1))}`, 'expected the count to stay at 2');
}

// Records saved before the dial existed, and records holding malformed dials, behave as an
// empty dial: invalid entries are dropped, valid neighbours survive, and leftover streak
// fields from the #87 format are stripped.
observe(DRILL_RECORD_CHECK, 4);
drillStorage.clear();
drillStorage.set('kt_drill_v1', JSON.stringify({ g1: { idx: 0, perfect: 0, mistakes: 0, runs: 0, best: 0 } }));
if (Object.keys(app.readDrillProgress().g1.opts).length !== 0) {
  violated(DRILL_RECORD_CHECK, 'a legacy record without dials was not treated as empty', '');
}
drillStorage.set('kt_drill_v1', JSON.stringify({ g1: {
  idx: 0, perfect: 0, mistakes: 0, runs: 0, best: 0,
  opts: { '1': { c: 9, s: 0 }, '2': { c: 4, s: 7 }, '3': 'junk', '4': { c: 4, s: 1 }, '5': [4, 0], '101': { c: 3, s: 0 }, '0': { c: 3, s: 0 }, '-2': { c: 3, s: 0 }, 'abc': { c: 3, s: 0 } },
} }));
const cleanedOpts = app.readDrillProgress().g1.opts;
if (JSON.stringify(cleanedOpts) !== JSON.stringify({ '2': { c: 4 }, '4': { c: 4 } })) {
  violated(DRILL_RECORD_CHECK, `invalid stored dials survived as ${JSON.stringify(cleanedOpts)}`, 'expected the two valid entries without streak fields');
}
app.startDrill('g1', false, 'choice');
if (app.drillChoices(app.state.drill, app.drillQuestion()).length !== 2) {
  violated(DRILL_RECORD_CHECK, 'a dropped dial still changed the options shown', '');
}
app.exitDrill();
drillStorage.clear();

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

const DRILL_HISSAN_CHECK = 'the grade-2 written-arithmetic drill follows the two-digit then three-digit regrouping sequence';
// Issue #95: the course is split by difficulty. h2n never carries or borrows in any column;
// h2 (the original course id) regroups in every question.
const hissanCarries = (left, right) => { let count = 0, carry = 0; while (left || right || carry) { const total = (left % 10) + (right % 10) + carry; carry = total >= 10 ? 1 : 0; count += carry; left = Math.floor(left / 10); right = Math.floor(right / 10); } return count; };
const hissanBorrows = (left, right) => { let count = 0, lend = 0; while (left || right) { const digit = (left % 10) - lend; lend = digit < right % 10 ? 1 : 0; count += lend; left = Math.floor(left / 10); right = Math.floor(right / 10); } return count; };
const hissanBank = app.drillBank('h2');
const plainHissanBank = app.drillBank('h2n');
observe(DRILL_HISSAN_CHECK, hissanBank.length + plainHissanBank.length);
const hissanDigits = (value) => String(value).length;
for (const [courseId, bank] of [['h2n', plainHissanBank], ['h2', hissanBank]]) {
  let sawThreeDigit = false;
  let carryAdds = 0;
  let borrowSubs = 0;
  let plainAdds = 0;
  let plainSubs = 0;
  let zeroHeavy = 0;
  for (const question of bank) {
    const match = /^(\d+) (＋|−) (\d+)$/.exec(question.text);
    if (!match) {
      violated(DRILL_HISSAN_CHECK, `${courseId} question ${question.no} is not a written-arithmetic prompt`, question.text);
      continue;
    }
    const left = Number(match[1]);
    const right = Number(match[3]);
    const widest = Math.max(hissanDigits(left), hissanDigits(right));
    if (widest > 3) {
      violated(DRILL_HISSAN_CHECK, `${courseId} question ${question.no} leaves the grade-2 range`, question.text);
    }
    if (widest === 3) sawThreeDigit = true;
    if (!sawThreeDigit && widest !== 2) {
      violated(DRILL_HISSAN_CHECK, `${courseId} question ${question.no} leaves two-digit work before three-digit work starts`, question.text);
    }
    const regroups = match[2] === '＋' ? hissanCarries(left, right) : hissanBorrows(left, right);
    if (courseId === 'h2n' && regroups !== 0) {
      violated(DRILL_HISSAN_CHECK, `h2n question ${question.no} regroups although the course has none`, question.text);
    }
    if (courseId === 'h2' && regroups === 0) {
      violated(DRILL_HISSAN_CHECK, `h2 question ${question.no} needs no regrouping`, question.text);
    }
    if (match[2] === '＋' && ((left % 10) + (right % 10) >= 10)) carryAdds += 1;
    if (match[2] === '−' && ((left % 10) < (right % 10))) borrowSubs += 1;
    if (match[2] === '＋') plainAdds += 1; else plainSubs += 1;
    if (question.text === '700 − 286') zeroHeavy += 1;
  }
  if (courseId === 'h2' && (carryAdds < 20 || borrowSubs < 20 || !sawThreeDigit || zeroHeavy < 1)) {
    violated(DRILL_HISSAN_CHECK, `carry ${carryAdds}, borrow ${borrowSubs}, zero-heavy ${zeroHeavy}`, 'grade-2 written arithmetic coverage');
  }
  if (courseId === 'h2n' && (plainAdds < 40 || plainSubs < 40 || !sawThreeDigit)) {
    violated(DRILL_HISSAN_CHECK, `h2n has ${plainAdds} additions and ${plainSubs} subtractions`, 'expected both operations in two and three digits');
  }
}

const DRILL_HISSAN_VIEW_CHECK = 'the 100-question written-arithmetic course stacks aligned operands and puts the input below the rule';
observe(DRILL_HISSAN_VIEW_CHECK, 6);
const hissanMarkup = ['kt-drill-hissan-row', '{{ drillView.hissanLeft }}', '{{ drillView.hissanRight }}',
  'kt-drill-hissan-rule', 'kt-drill-hissan-cell'].every(fragment => page.includes(fragment));
if (!hissanMarkup) violated(DRILL_HISSAN_VIEW_CHECK, 'the generated page is missing the stacked operands, rule, or answer row', runtimePagePath);
const hissanViewApp = new app.constructor();
hissanViewApp.props = hissanViewApp.props || {};
for (const [courseId, index, mode] of [['h2', 0, 'input'], ['h2', 90, 'input'], ['h2', 0, 'choice'], ['h2n', 0, 'input'], ['h2n', 60, 'input']]) {
  try {
    hissanViewApp.startDrill(courseId, true, mode);
    hissanViewApp.state.drill.idx = index;
    const question = app.drillBank(courseId)[index];
    const parts = /^(\d+) (＋|−) (\d+)$/.exec(question.text);
    const view = hissanViewApp.renderVals().drillView;
    if (!view.showHissan || view.showPlainPrompt || view.showAns || view.hissanLeft !== parts[1] ||
        view.hissanOp !== parts[2] || view.hissanRight !== parts[3] || view.showHissanInput !== (mode === 'input')) {
      violated(DRILL_HISSAN_VIEW_CHECK, `question ${question.no} in ${mode} mode has an incorrect written layout`, JSON.stringify(view));
    }
  } catch (error) {
    violated(DRILL_HISSAN_VIEW_CHECK, `question ${index + 1} in ${mode} mode did not render`, String(error && error.stack));
  }
}

// Issue #95: （ ）の ある けいさん. Every question keeps a bracket, stays inside the
// grade-2 range at every step, and some subtractions change value without the bracket,
// so the course actually teaches working the bracket first.
const DRILL_PAREN_CHECK = 'the grade-2 parentheses drill works the bracket first inside the grade-2 range';
const parenBank = app.drillBank('p2');
observe(DRILL_PAREN_CHECK, parenBank.length);
let bracketMatters = 0;
for (const question of parenBank) {
  const front = /^\((\d+) ＋ (\d+)\) ＋ (\d+)$/.exec(question.text);
  const back = /^(\d+) ＋ \((\d+) ＋ (\d+)\)$/.exec(question.text);
  const minus = /^(\d+) − \((\d+) ＋ (\d+)\)$/.exec(question.text);
  const match = front || back || minus;
  if (!match) {
    violated(DRILL_PAREN_CHECK, `p2 question ${question.no} has no bracketed grade-2 form`, question.text);
    continue;
  }
  const [a, b, c] = match.slice(1).map(Number);
  const inside = front ? a + b : b + c;
  const answer = minus ? a - inside : a + b + c;
  if (question.ans !== answer || inside <= 0 || answer < 1 || answer > 99) {
    violated(DRILL_PAREN_CHECK, `p2 question ${question.no} leaves the grade-2 range or answers ${question.ans}`, question.text);
  }
  if (minus && a - b + c !== answer) bracketMatters += 1;
}
if (bracketMatters < 10) {
  violated(DRILL_PAREN_CHECK, `only ${bracketMatters} subtractions change value without the bracket`, 'expected at least 10');
}

const DRILL_UNIQUE_CHECK = 'the grade-2 written-arithmetic and parentheses drills never repeat a question';
observe(DRILL_UNIQUE_CHECK, 3);
for (const courseId of ['h2n', 'h2', 'p2']) {
  const texts = app.drillBank(courseId).map((question) => question.text);
  const repeated = texts.filter((text, index) => texts.indexOf(text) !== index);
  if (repeated.length) violated(DRILL_UNIQUE_CHECK, `${courseId} repeats ${repeated.length} questions`, repeated.join(', '));
}

const DRILL_DIV_CHECK = 'the division drill covers every ordered inverse of a multiplication fact';
const divisionBank = app.drillBank('d3');
const drilledDivisions = new Set();
for (const question of divisionBank) {
  const match = /^(\d+) ÷ (\d+)$/.exec(question.text);
  if (match) drilledDivisions.add(`${question.ans}x${match[2]}`);
}
observe(DRILL_DIV_CHECK, drilledDivisions.size);
for (let left = 1; left <= 9; left += 1) {
  for (let right = 1; right <= 9; right += 1) {
    if (!drilledDivisions.has(`${left}x${right}`)) {
      violated(DRILL_DIV_CHECK, `${left * right} ÷ ${right} is never drilled`, 'division fact coverage');
    }
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

const DRILL_STANDALONE_CHECK = 'isolated kanji drills ask only readings that are words on their own';
const KANJI_DRILL_GRADES = { k1: 1, k2: 2 };
const freeOnKeys = app.drillStandaloneOnKeys();
observe(DRILL_STANDALONE_CHECK, 2);
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
    if (question.readingType === 'kun' && (question.ans !== entry.kun || question.text !== (entry.kunWord || entry.k))) {
      violated(DRILL_STANDALONE_CHECK, `${courseId} question ${question.no} is not a standalone kun word`, JSON.stringify(question));
    }
    if (question.readingType === 'on' && (!freeOnKeys.has(`${question.kanji}:${question.ans}`) || question.text !== question.kanji)) {
      violated(DRILL_STANDALONE_CHECK, `${courseId} asks bound on-yomi ${question.ans} in isolation`, JSON.stringify(question));
    }
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
  const isolatedCount = bank.filter((question) => question.readingType === 'on' || question.readingType === 'kun').length;
  const standaloneTargets = app.drillStandaloneTargets(gradeEntries);
  const standaloneKanji = new Set(standaloneTargets.map((target) => target.e.k));
  const missingStandalone = [...standaloneKanji].filter((kanji) => !asked.has(kanji));
  if (standaloneTargets.length <= isolatedCount && missingStandalone.length) {
    violated(DRILL_KANJI_CHECK, `${courseId} skipped standalone ${missingStandalone.join('')}`, courseId);
  }
  if (isolatedCount >= 20 && asked.size < 20) {
    violated(DRILL_KANJI_CHECK, `${courseId} drills only ${asked.size} kanji in a 100-question course`, courseId);
  }
  if (!readingTypes.has('on') || !readingTypes.has('kun') || !readingTypes.has('word')) {
    violated(DRILL_KANJI_CHECK, `${courseId} does not include on-yomi, kun-yomi, and word questions`, JSON.stringify([...readingTypes]));
  }
  const expectedWords = app.drillKanjiWords(grade);
  const wordQuestions = bank.filter((question) => question.readingType === 'word');
  const expectedWordQuestions = 100 - isolatedCount;
  if (wordQuestions.length !== expectedWordQuestions || askedWords.size < Math.min(expectedWords.length, expectedWordQuestions)) {
    violated(DRILL_KANJI_WORD_CHECK, `${courseId} has ${wordQuestions.length} word questions using ${askedWords.size} unique words`, `expected ${expectedWordQuestions} questions`);
  }
}

// Writing mode reverses each prompt: the reading is shown and the child chooses the canonical
// spelling (including okurigana). Distractors stay inside the same grade and never share the
// target reading, so an alternative valid spelling cannot create an ambiguous question.
observe(DRILL_KANJI_WRITING_CHECK, 200);
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
// Issue #103: the revealed answer has to be entered again before the run moves on. A wrong copy
// only clears the entry; the right copy shows the pair, scores nothing, and requeues the question.
observe(DRILL_COPY_CHECK, 3);
const copyMistakes = app.state.drill.mistakes;
app.setState({ input: String(revealedQuestion.ans + 3) });
app.drillSubmit();
if (!app.state.drill.revealed || app.state.drill.copyMiss !== 1 || app.state.drill.echo || app.state.drill.idx !== 0 ||
    app.state.input !== '' || app.state.drill.mistakes !== copyMistakes) {
  violated(DRILL_COPY_CHECK, 'a wrong copy of the revealed answer moved on, kept the entry, or counted as a mistake', JSON.stringify(app.state.drill));
}
app.setState({ input: String(revealedQuestion.ans) });
app.drillSubmit();
if (!app.state.drill.echo || app.state.drill.idx !== 0 || app.state.drill.echo.main !== app.drillAnswerLine(revealedQuestion)) {
  violated(DRILL_COPY_CHECK, 'copying the revealed answer did not show the completed pair', JSON.stringify(app.state.drill));
}
app.drillFlushEcho();
if (app.state.drill.idx !== 1 || !app.state.drill.again.includes(0) || app.state.drill.revealed || app.state.drill.copyMiss !== 0 ||
    app.state.drill.perfect !== 0 || app.state.drill.mistakes !== copyMistakes) {
  violated(DRILL_COPY_CHECK, 'the copied question did not move on to the again queue without credit', JSON.stringify(app.state.drill));
}
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
if (page.includes('drillNext(') || page.includes('drillView.onNext')) {
  violated(DRILL_COPY_CHECK, 'the drill still offers a つぎへ button that skips copying the revealed answer', runtimePagePath);
}
// A chosen answer is copied by choosing it again, with the same miss handling.
app.startDrill('k1', true, 'reading');
const copyKanji = app.drillQuestion();
const copyWrongReading = copyKanji.choices.find((choice) => choice !== copyKanji.ans);
chooseDrillAnswer(copyWrongReading);
chooseDrillAnswer(copyWrongReading);
chooseDrillAnswer(copyWrongReading);
if (!app.state.drill.revealed || app.state.drill.copyMiss !== 1 || app.state.drill.idx !== 0) {
  violated(DRILL_COPY_CHECK, 'a wrong reading chosen after the reveal moved on or was not counted as a copy miss', JSON.stringify(app.state.drill));
}
chooseDrillAnswer(copyKanji.ans);
if (app.state.drill.idx !== 1 || !app.state.drill.again.includes(0) || app.state.drill.perfect !== 0) {
  violated(DRILL_COPY_CHECK, 'choosing the revealed reading did not requeue the question without credit', JSON.stringify(app.state.drill));
}
app.exitDrill();

// Issue #104: at six options, three first-try answers in a row retire a question. The run that
// retired it still finishes with it; every later run leaves it out, and the counts follow.
observe(DRILL_RETIRE_CHECK, 9);
const writeDrillRecord = (id, record) => drillStorage.set('kt_drill_v1', JSON.stringify({ [id]: Object.assign({ idx: 0, perfect: 0, mistakes: 0, runs: 0, best: 0, opts: {} }, record) }));
const storedDrillRecord = (id) => JSON.parse(drillStorage.get('kt_drill_v1'))[id];
const drillViewOf = (id, restart, mode) => { hissanViewApp.startDrill(id, restart, mode); return hissanViewApp.renderVals().drillView; };
drillStorage.clear();
writeDrillRecord('g1', { runs: 4, opts: { '1': { c: 6, t: 2 } } });
app.startDrill('g1', true, 'choice');
chooseDrillAnswer(app.drillQuestion().ans);
if (JSON.stringify(storedDrillRecord('g1').opts['1']) !== JSON.stringify({ c: 6, t: 3, r: 4 })) {
  violated(DRILL_RETIRE_CHECK, `the third first-try answer at six recorded ${JSON.stringify(storedDrillRecord('g1').opts['1'])}`, 'expected {c:6,t:3,r:4}');
}
app.exitDrill();
app.startDrill('g1', true, 'choice');
if (app.state.drill.idx !== 0 || app.drillQuestion().no !== 1 || app.state.drill.skip.length !== 0) {
  violated(DRILL_RETIRE_CHECK, 'a question retired in this run left the run before it finished', JSON.stringify(app.state.drill));
}
app.exitDrill();
writeDrillRecord('g1', { runs: 5, opts: { '1': { c: 6, t: 3, r: 4 } } });
const retiredView = drillViewOf('g1', true, 'choice');
if (hissanViewApp.drillQuestion().no !== 2 || retiredView.countText !== '0 / 99') {
  violated(DRILL_RETIRE_CHECK, `the next run started at question ${hissanViewApp.drillQuestion().no} counting ${retiredView.countText}`, 'expected question 2 and 0 / 99');
}
hissanViewApp.exitDrill();
writeDrillRecord('g1', { runs: 5, opts: { '3': { c: 6, t: 3, r: 0 }, '4': { c: 6, t: 3, r: 1 } } });
app.startDrill('g1', true, 'choice');
chooseDrillAnswer(app.drillQuestion().ans);
chooseDrillAnswer(app.drillQuestion().ans);
if (app.drillQuestion().no !== 5) {
  violated(DRILL_RETIRE_CHECK, `advancing past two retired questions landed on question ${app.drillQuestion().no}`, 'expected question 5');
}
let retireAsked = 2;
while (!app.state.drill.done && retireAsked <= 100) {
  retireAsked += 1;
  chooseDrillAnswer(app.drillQuestion().ans);
}
if (retireAsked !== 98 || app.state.drill.perfect !== 98 || storedDrillRecord('g1').runs !== 6) {
  violated(DRILL_RETIRE_CHECK, `a run without two retired questions asked ${retireAsked}, scored ${app.state.drill.perfect}, runs ${storedDrillRecord('g1').runs}`, 'expected 98 questions and one more run');
}
app.exitDrill();
writeDrillRecord('g1', { runs: 2, opts: { '1': { c: 6, t: 2 } } });
app.startDrill('g1', true, 'choice');
const retireMiss = app.drillQuestion();
chooseDrillAnswer(app.drillChoices(app.state.drill, retireMiss).find((choice) => choice !== retireMiss.ans));
if (JSON.stringify(storedDrillRecord('g1').opts['1']) !== JSON.stringify({ c: 5 })) {
  violated(DRILL_RETIRE_CHECK, `a miss at six recorded ${JSON.stringify(storedDrillRecord('g1').opts['1'])}`, 'expected the run toward retirement to drop with the count');
}
app.exitDrill();
writeDrillRecord('g1', { runs: 3, opts: { '1': { c: 6, t: 3 }, '2': { c: 6, t: 7, r: 1 }, '3': { c: 5, t: 2 }, '4': { c: 6, t: 3, r: -1 }, '5': { c: 6, t: 0 }, '6': { c: 6, t: 3, r: 1.5 } } });
const sanitizedRetire = app.readDrillProgress().g1.opts;
const expectedRetire = { '1': { c: 6, t: 2 }, '2': { c: 6, t: 3, r: 1 }, '3': { c: 5 }, '4': { c: 6, t: 2 }, '5': { c: 6 }, '6': { c: 6, t: 2 } };
if (JSON.stringify(sanitizedRetire) !== JSON.stringify(expectedRetire)) {
  violated(DRILL_RETIRE_CHECK, `stored retirement fields were read as ${JSON.stringify(sanitizedRetire)}`, JSON.stringify(expectedRetire));
}
const everyRetired = {};
for (let no = 1; no <= 100; no += 1) everyRetired[no] = { c: 6, t: 3, r: 0 };
writeDrillRecord('g1', { runs: 1, opts: everyRetired });
const allRetiredView = drillViewOf('g1', true, 'choice');
if (!hissanViewApp.state.drill.done || !hissanViewApp.state.drill.allRetired || allRetiredView.finishTitle !== 'ぜんぶ おぼえたよ！' || allRetiredView.countText !== '') {
  violated(DRILL_RETIRE_CHECK, 'a course with every question retired did not open on its finish screen', JSON.stringify(allRetiredView));
}
hissanViewApp.relearnDrill();
if (hissanViewApp.state.drill.done || hissanViewApp.state.drill.idx !== 0 || Object.keys(storedDrillRecord('g1').opts).length !== 0 || storedDrillRecord('g1').runs !== 1) {
  violated(DRILL_RETIRE_CHECK, 'starting a fully retired course over did not bring every question back', drillStorage.get('kt_drill_v1'));
}
hissanViewApp.exitDrill();
for (const [opts, expected] of [[{ '1': { c: 6, t: 3, r: 4 } }, '99もん'], [everyRetired, 'ぜんぶ おぼえた']]) {
  writeDrillRecord('g1', { runs: 5, opts });
  hissanViewApp.state.screen = 'start';
  try {
    const card = hissanViewApp.renderVals().drillCards.find((row) => row.id === 'g1');
    if (!card || card.progressText !== expected) {
      violated(DRILL_RETIRE_CHECK, `the start card read ${card && card.progressText}`, expected);
    }
  } catch (error) {
    violated(DRILL_RETIRE_CHECK, 'the start screen did not render the drill cards', String(error && error.stack));
  }
}
drillStorage.clear();

// Issue #105: a correct answer, first try or copied, is read aloud as one sentence, and the
// pair stays up until the reading ends (capped so a stuck engine never holds the drill).
observe(DRILL_SPEECH_CHECK, 10);
for (const [courseId, prompt, expected] of [['g2', '6 × 5', '6かける5は30'], ['g1', '3 ＋ 7', '3たす7は10'], ['g1', '10 − □ ＝ 3', '10ひく7は3'],
  ['h2n', '21 ＋ 11', '21たす11は32'], ['p2', '(2 ＋ 1) ＋ 1', 'かっこ2たす1かっことじ、たす1は4']]) {
  const question = app.drillBank(courseId).find((row) => row.text === prompt);
  const spoken = question ? app.drillSpeechText({ id: courseId }, question) : '(missing)';
  if (spoken !== expected) violated(DRILL_SPEECH_CHECK, `${courseId} ${prompt} is read as ${spoken}`, expected);
}
for (const course of drillCourses) {
  for (const question of app.drillBank(course.id)) {
    const spoken = app.drillSpeechText({ id: course.id }, question);
    const readsAnswer = question.text.includes('□') ? spoken.includes(String(question.ans)) : spoken.endsWith('は' + question.ans);
    if (!spoken || (question.kind !== 'pick' && (/[()＋−×÷＝□\s-]/.test(spoken) || !readsAnswer))) {
      violated(DRILL_SPEECH_CHECK, `${course.id} question ${question.no} is read as ${spoken}`, question.text);
    }
  }
}
{
  const priorGlobals = { setTimeout: globalThis.setTimeout, clearTimeout: globalThis.clearTimeout, window: globalThis.window, SpeechSynthesisUtterance: globalThis.SpeechSynthesisUtterance, KtAndroid: globalThis.KtAndroid };
  const timers = [], utterances = [], androidCalls = [];
  let voices = [{ lang: 'ja-JP' }], cancelled = 0, androidStops = 0, androidAccepts = true;
  globalThis.setTimeout = (fn, ms) => { timers.push({ fn, ms, cleared: false }); return timers.length; };
  globalThis.clearTimeout = (id) => { if (timers[id - 1]) timers[id - 1].cleared = true; };
  globalThis.window = { speechSynthesis: { getVoices: () => voices, speak: (utterance) => utterances.push(utterance), cancel: () => { cancelled += 1; } } };
  globalThis.SpeechSynthesisUtterance = class { constructor(text) { this.text = text; } };
  const pending = () => timers.filter((timer) => !timer.cleared).at(-1);
  const answerG2 = (value) => { app.setState({ input: String(value) }); app.drillSubmit(); };
  try {
    app.state.muted = false;
    app.startDrill('g2', true, 'input');
    const spokenQuestion = app.drillQuestion();
    answerG2(spokenQuestion.ans);
    const reading = utterances.at(-1);
    if (utterances.length !== 1 || reading.text !== app.drillSpeechText(app.state.drill, spokenQuestion) || reading.lang !== 'ja-JP' ||
        !pending() || pending().ms !== app.drillSpeechMaxMs()) {
      violated(DRILL_SPEECH_CHECK, 'a correct answer was not read aloud in Japanese while the pair waited', JSON.stringify({ utterances, timers: timers.map((t) => t.ms) }));
    }
    reading.onend();
    const afterReading = pending();
    if (!afterReading || afterReading.ms > app.drillEchoMs() || app.state.drill.idx !== 0) {
      violated(DRILL_SPEECH_CHECK, 'the end of the reading did not release the pair', JSON.stringify(timers.map((t) => t.ms)));
    }
    const timersBefore = timers.length;
    reading.onend();
    if (timers.length !== timersBefore) violated(DRILL_SPEECH_CHECK, 'a second end of the same reading moved the drill again', '');
    afterReading.fn();
    if (app.state.drill.idx !== 1) violated(DRILL_SPEECH_CHECK, 'the drill did not move on after the reading', JSON.stringify(app.state.drill));
    // The copy of a revealed answer is read aloud too.
    const copiedQuestion = app.drillQuestion();
    answerG2(copiedQuestion.ans + 1);
    answerG2(copiedQuestion.ans + 2);
    answerG2(copiedQuestion.ans);
    if (utterances.length !== 2 || utterances[1].text !== app.drillSpeechText(app.state.drill, copiedQuestion)) {
      violated(DRILL_SPEECH_CHECK, 'copying the revealed answer was not read aloud', JSON.stringify(utterances.map((u) => u.text)));
    }
    app.toggleMute();
    const mutedTimers = timers.length;
    utterances[1].onend();
    if (cancelled < 1 || !app.state.muted || timers.length !== mutedTimers || pending().ms > app.drillEchoMs()) {
      violated(DRILL_SPEECH_CHECK, 'muting did not stop the reading or left the pair waiting for it', JSON.stringify({ cancelled, timers: timers.map((t) => t.ms) }));
    }
    drillStorage.delete('kt_muted_v1');
    pending().fn();
    answerG2(app.drillQuestion().ans);
    if (utterances.length !== 2 || pending().ms !== app.drillEchoMs()) {
      violated(DRILL_SPEECH_CHECK, 'a muted drill read aloud or waited for a reading', JSON.stringify(timers.map((t) => t.ms)));
    }
    pending().fn();
    app.state.muted = false;
    voices = [{ lang: 'en-US' }];
    answerG2(app.drillQuestion().ans);
    if (utterances.length !== 2 || pending().ms !== app.drillEchoMs()) {
      violated(DRILL_SPEECH_CHECK, 'the drill read numbers with a non-Japanese voice', JSON.stringify(utterances.map((u) => u.text)));
    }
    pending().fn();
    voices = [{ lang: 'ja-JP' }];
    answerG2(app.drillQuestion().ans);
    const cancelledBefore = cancelled;
    app.exitDrill();
    if (cancelled !== cancelledBefore + 1) violated(DRILL_SPEECH_CHECK, 'leaving the drill did not stop the reading', String(cancelled));
    const webReadings = utterances.length;
    globalThis.KtAndroid = { speak: (text, id) => { androidCalls.push({ text, id }); return androidAccepts; }, stopSpeaking: () => { androidStops += 1; } };
    app.startDrill('g2', true, 'input');
    const androidQuestion = app.drillQuestion();
    answerG2(androidQuestion.ans);
    const androidCall = androidCalls.at(-1);
    if (androidCalls.length !== 1 || utterances.length !== webReadings || !/^\d+$/.test(androidCall.id) ||
        androidCall.text !== app.drillSpeechText(app.state.drill, androidQuestion) || pending().ms !== app.drillSpeechMaxMs()) {
      violated(DRILL_SPEECH_CHECK, 'the Android app did not read the answer through its own speech bridge', JSON.stringify(androidCalls));
    }
    const androidTimers = timers.length;
    globalThis.window.__ktDrillSpeechEnd(String(Number(androidCall.id) + 1));
    if (timers.length !== androidTimers) violated(DRILL_SPEECH_CHECK, 'an end notice for another reading released the pair', '');
    globalThis.window.__ktDrillSpeechEnd(androidCall.id);
    if (pending().ms > app.drillEchoMs()) violated(DRILL_SPEECH_CHECK, 'the Android end notice did not release the pair', JSON.stringify(timers.map((t) => t.ms)));
    pending().fn();
    androidAccepts = false;
    answerG2(app.drillQuestion().ans);
    if (pending().ms !== app.drillEchoMs()) violated(DRILL_SPEECH_CHECK, 'a refused Android reading still held the pair', JSON.stringify(timers.map((t) => t.ms)));
    androidAccepts = true;
    pending().fn();
    answerG2(app.drillQuestion().ans);
    app.exitDrill();
    if (androidStops < 1) violated(DRILL_SPEECH_CHECK, 'leaving the drill did not stop the Android reading', String(androidStops));
  } catch (error) {
    violated(DRILL_SPEECH_CHECK, 'reading aloud threw', String(error && error.stack));
  } finally {
    for (const [name, value] of Object.entries(priorGlobals)) {
      if (value === undefined) delete globalThis[name]; else globalThis[name] = value;
    }
    app.state.muted = false;
    if (app.state.drill) app.exitDrill();
  }
}

// Issue #106: the written-arithmetic drill takes the answer like the written method, one digit
// at a time from the ones place, so 21 ＋ 11 is entered 2 then 3.
observe(DRILL_COLUMN_CHECK, 6);
drillStorage.clear();
app.startDrill('h2n', true, 'input');
const columnQuestion = app.drillQuestion();
app.drillPress('2');
app.drillPress('3');
app.drillPress('9');
if (columnQuestion.text !== '21 ＋ 11' || app.drillHissanColumns(app.state.drill, columnQuestion) !== 2 || app.state.input !== '23' || app.drillTypedValue() !== '32') {
  violated(DRILL_COLUMN_CHECK, `21 ＋ 11 took ${app.state.input} as ${app.drillTypedValue()}`, 'expected two cells read as 32');
}
app.drillSubmit();
if (!app.state.drill.echo || app.state.drill.mark) violated(DRILL_COLUMN_CHECK, 'entering 2 then 3 for 21 ＋ 11 was not accepted', JSON.stringify(app.state.drill));
app.drillFlushEcho();
app.startDrill('h2n', true, 'input');
app.drillPress('3');
app.drillPress('2');
app.drillSubmit();
if (app.state.drill.mark !== 'wrong') violated(DRILL_COLUMN_CHECK, 'entering 32 left to right was accepted although the ones place comes first', JSON.stringify(app.state.drill));
app.exitDrill();
const widerAnswer = hissanBank.find((row) => String(row.ans).length > Math.max(...row.text.split(/ [＋−] /).map((part) => part.length)));
const shorterAnswer = hissanBank.find((row) => String(row.ans).length < Math.max(...row.text.split(/ [＋−] /).map((part) => part.length)));
for (const row of [widerAnswer, shorterAnswer]) {
  if (!row) { violated(DRILL_COLUMN_CHECK, 'h2 has no question whose answer width differs from its numbers', ''); continue; }
  app.startDrill('h2', true, 'input');
  app.state.drill.idx = row.no - 1;
  const width = Math.max(String(row.ans).length, ...row.text.split(/ [＋−] /).map((part) => part.length));
  for (const digit of String(row.ans).split('').reverse()) app.drillPress(digit);
  if (app.drillHissanColumns(app.state.drill, row) !== width || app.drillTypedValue() !== String(row.ans)) {
    violated(DRILL_COLUMN_CHECK, `${row.text} offers ${app.drillHissanColumns(app.state.drill, row)} cells and reads ${app.drillTypedValue()}`, `expected ${width} cells`);
  }
  app.drillSubmit();
  if (!app.state.drill.echo) violated(DRILL_COLUMN_CHECK, `${row.text} rejected its answer entered from the ones place`, JSON.stringify(app.state.drill));
  app.exitDrill();
}
for (const [courseId, mode] of [['h2n', 'choice'], ['g1', 'input']]) {
  app.startDrill(courseId, true, mode);
  app.setState({ input: '12' });
  if (app.drillHissanColumns(app.state.drill, app.drillQuestion()) !== 0 || app.drillTypedValue() !== '12') {
    violated(DRILL_COLUMN_CHECK, `${courseId} in ${mode} mode reads its entry backwards`, app.drillTypedValue());
  }
  app.exitDrill();
}
try {
  hissanViewApp.startDrill('h2n', true, 'input');
  hissanViewApp.drillPress('2');
  const columnView = hissanViewApp.renderVals().drillView;
  const cells = columnView.hissanCells || [];
  if (cells.length !== 2 || cells[1].text !== '2' || cells[0].text !== '' || !cells[0].style || cells[1].style ||
      columnView.hissanCellsAria !== 'こたえ 2' || !columnView.showColumnAsk) {
    violated(DRILL_COLUMN_CHECK, 'the column cells do not show the ones digit and point at the tens place next', JSON.stringify(columnView));
  }
  hissanViewApp.exitDrill();
} catch (error) {
  violated(DRILL_COLUMN_CHECK, 'the column cells did not render', String(error && error.stack));
}
if (!page.includes('this.drillPress(e.key)')) violated(DRILL_COLUMN_CHECK, 'the keyboard bypasses the column limit', runtimePagePath);
drillStorage.clear();

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

const furiganaCorpusSummary = furiganaCorpus.finish(observe, violated);
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
  console.log(furiganaCorpusSummary);
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
