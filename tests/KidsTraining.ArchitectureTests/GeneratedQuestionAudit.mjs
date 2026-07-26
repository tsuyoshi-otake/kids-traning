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
  setState(patch) {
    Object.assign(this.state, patch);
  }
}

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
  const Component = new Function('DCLogic', `${appSource}\nreturn Component;`)(DCLogic);
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

// --- fixtures ---------------------------------------------------------------------------

const TOPICS = [
  'add', 'sub', 'hissan', 'mul', 'clock', 'kokugo', 'moji', 'measure', 'kazu', 'shape',
  'div', 'frac', 'chart', 'story', 'bun', 'goi', 'dokkai', 'eigo', 'money', 'groups',
  'order', 'soroban', 'seikatsu', 'shakai', 'rika', 'kateika', 'doutoku', 'jouhou', 'sougou',
  'tokubetsu', 'keyboard',
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

// Exercise the real schema-v5 migration and lane selection code before auditing generators.
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
observe('schema-v5 migration is idempotent', legacyProfiles.length);
if (JSON.stringify(migratedOnce) !== JSON.stringify(migratedTwice)) {
  violated('schema-v5 migration is idempotent', 'a second migration changed profile data', 'v4 -> v5 -> v5');
}
for (let index = 0; index < legacyProfiles.length; index += 1) {
  const before = legacyProfiles[index];
  const after = migratedOnce[index];
  if (after.learningSchema !== 5 || Object.keys(after.unitStats || {}).length !== UNITS.length) {
    violated('schema-v5 migration is idempotent', `${before.name} did not receive one stat per unit`, before.name);
  }
  if (after.stars !== before.stars || after.xp !== before.xp || !after.legacyTopicStats) {
    violated('schema-v5 migration preserves evidence', `${before.name} lost stars, XP, or legacy evidence`, before.name);
  }
}
observe('schema-v5 migration preserves evidence', legacyProfiles.length);

const beginnerAtGrade = (grade) => app.ensureLearningProfile({
  name: `grade-${grade}`,
  grade,
  stars: 0,
  xp: 0,
  mastery: {},
  skillStats: {},
  cleared: {},
});
const gradeOneBeginner = beginnerAtGrade(1);
const gradeSixBeginner = beginnerAtGrade(6);
const gradeOneFrontier = app.frontierTopics(gradeOneBeginner);
const gradeSixFrontier = app.frontierTopics(gradeSixBeginner);
observe('school grade never caps curriculum', 2);
if (JSON.stringify(gradeOneFrontier) !== JSON.stringify(gradeSixFrontier)) {
  violated('school grade never caps curriculum', 'registered grades 1 and 6 start with different lane frontiers', `${gradeOneFrontier} vs ${gradeSixFrontier}`);
}

const originalSettings = app.state.settings;
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
if (JSON.stringify(preferenceProfile.unitStats) !== preferenceStatsBefore) {
  violated('school-grade preference is optional and reversible', 'enabling the preference changed unit progress', preferenceProfile.name);
}
app.state.settings = { ...app.defaultSettings(), preferSchoolGrade: false };
const preferenceRestoredFrontier = app.frontierTopics(preferenceProfile);
if (JSON.stringify(preferenceRestoredFrontier) !== JSON.stringify(preferenceOffFrontier)) {
  violated('school-grade preference is optional and reversible', 'turning the preference off did not restore the original frontier', `${preferenceRestoredFrontier} vs ${preferenceOffFrontier}`);
}
app.state.settings = originalSettings;

const mathLane = app.curriculumLaneIds().find((lane) => lane.some((id) => id.startsWith('math.')));
const japaneseLane = app.curriculumLaneIds().find((lane) => lane.some((id) => id.startsWith('japanese.')));
observe('subject lanes unlock independently', 2);
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
if (!mathLane || app.frontierTopics(gradeOneBeginner).find((id) => id.startsWith('math.')) !== mathLane[0]) {
  violated('subject lanes unlock independently', 'a beginner does not start at the first mathematics unit', String(mathLane && mathLane[0]));
} else {
  gradeOneBeginner.unitStats[mathLane[0]].retentionStartedAt = 1;
  const nextMath = app.frontierTopics(gradeOneBeginner).find((id) => id.startsWith('math.'));
  if (nextMath !== mathLane[1]) {
    violated('subject lanes unlock independently', 'starting retention did not unlock the next mathematics unit', `${nextMath} vs ${mathLane[1]}`);
  }
  if (japaneseLane && app.frontierTopics(gradeOneBeginner).find((id) => id.startsWith('japanese.')) !== japaneseLane[0]) {
    violated('subject lanes unlock independently', 'advancing mathematics also advanced Japanese', japaneseLane[0]);
  }
  for (const id of mathLane) {
    if ((app.curriculumUnit(id)?.grade || 0) >= 6) break;
    gradeOneBeginner.unitStats[id].retentionStartedAt = 1;
  }
  const earlyLearningUnit = app.frontierTopics(gradeOneBeginner).find((id) => id.startsWith('math.'));
  if ((app.curriculumUnit(earlyLearningUnit)?.grade || 0) !== 6) {
    violated('school grade never caps curriculum', 'a registered grade-1 profile could not reach grade-6 mathematics', String(earlyLearningUnit));
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
