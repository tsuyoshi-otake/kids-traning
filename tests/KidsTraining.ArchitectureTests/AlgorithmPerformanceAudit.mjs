// Operation-count audit for the generated learning-runtime hot paths (issue #52).
//
// Usage: node AlgorithmPerformanceAudit.mjs <generated-runtime-html> [--verify]
// Timing is reported for diagnostics, but verification uses deterministic operation counts
// and structural sharing so slower CI machines cannot create false failures.

import { readFileSync } from 'node:fs';
import { performance } from 'node:perf_hooks';

const runtimePagePath = process.argv[2];
const verify = process.argv.includes('--verify');
if (!runtimePagePath) {
  console.error('Usage: node AlgorithmPerformanceAudit.mjs <generated-runtime-html> [--verify]');
  process.exit(2);
}

const page = readFileSync(runtimePagePath, 'utf8');
const classMarker = 'class Component extends DCLogic';
const classAt = page.indexOf(classMarker);
const scriptOpen = page.lastIndexOf('<script', classAt);
const sourceStart = page.indexOf('>', scriptOpen) + 1;
const sourceEnd = page.indexOf('</script>', classAt);
if (classAt < 0 || scriptOpen < 0 || sourceStart <= 0 || sourceEnd < 0) {
  console.error(`the generated app script could not be located: ${runtimePagePath}`);
  process.exit(2);
}

class DCLogic {
  setState(patch) {
    Object.assign(this.state, patch);
  }
}

let randomState = 0x52a11ce;
Math.random = () => {
  randomState = (randomState + 0x6d2b79f5) | 0;
  let value = randomState;
  value = Math.imul(value ^ (value >>> 15), value | 1);
  value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
  return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
};

let app;
try {
  const source = page.slice(sourceStart, sourceEnd);
  const Component = new Function('DCLogic', `${source}\nreturn Component;`)(DCLogic);
  app = new Component();
  app.props = app.props || {};
} catch (error) {
  console.error(`the generated app class could not be instantiated: ${error && error.stack}`);
  process.exit(2);
}

const profile = {
  name: 'algorithm-audit',
  grade: 1,
  color: '#ff8a3d',
  streak: 0,
  stars: 0,
  xp: 0,
  mastery: {},
  skillStats: {},
  unitStats: {},
  cleared: {},
};
app.state = {
  ...(app.state || {}),
  profiles: [profile],
  profileIdx: 0,
  settings: typeof app.defaultSettings === 'function' ? app.defaultSettings() : {},
  session: null,
};
app.ensureLearningProfile(profile);

const unit = app.curriculumCatalog().find((candidate) => {
  if (candidate.generatorKey !== 'curriculum-bank') return false;
  const stageOne = (candidate.questions || []).filter((question) => Number(question.stage) <= 1);
  return stageOne.length === 1;
});
if (!unit) {
  console.error('the curriculum contains no deterministic stage-one bank for the audit');
  process.exit(2);
}

const stageViewStarted = performance.now();
let stageView;
for (let index = 0; index < 2000; index += 1) {
  stageView = app.profileAtStage(profile, unit.id, 1 + (index % 5));
}
const stageViewElapsedMs = performance.now() - stageViewStarted;
const stageViewSharesMaps =
  !Object.hasOwn(stageView, 'mastery') && stageView.mastery === profile.mastery &&
  !Object.hasOwn(stageView, 'skillStats') && stageView.skillStats === profile.skillStats &&
  !Object.hasOwn(stageView, 'unitStats') && stageView.unitStats === profile.unitStats &&
  !Object.hasOwn(stageView, 'cleared') && stageView.cleared === profile.cleared;
const stageViewIsOverlay = Object.getPrototypeOf(stageView) === profile;
const stageViewStage = app.topicStage(stageView, unit.id);

let generatedCandidates = 0;
let allowedTopicBuilds = 0;
const originalGenFor = app.genFor.bind(app);
const originalAllowedTopics = app.allowedTopics.bind(app);
app.genFor = (...args) => {
  generatedCandidates += 1;
  return originalGenFor(...args);
};
app.allowedTopics = (...args) => {
  allowedTopicBuilds += 1;
  return originalAllowedTopics(...args);
};

const session = {
  questions: [],
  rolePlan: [],
  idx: 0,
  correct: 0,
  activeTargetTopic: unit.id,
  targetTopics: [unit.id],
  targetAsked: 0,
  targetIndependent: 0,
  reviewTopics: [],
  supportTopics: {},
  questionCounts: {},
  lastQuestionKey: '',
  attempt: 1,
  startStars: 0,
  startXp: 0,
};
const questionCount = 12;
const generationStarted = performance.now();
for (let index = 0; index < questionCount; index += 1) {
  app.generateSessionQuestion(profile, session, 'target');
}
const generationElapsedMs = performance.now() - generationStarted;
const sessionGeneratedCandidates = generatedCandidates;

// Persistence work must leave the synchronous button-update stack, but pause still owns a
// durable terminal. Replace timers/storage with deterministic fakes so this is an operation
// count rather than a wall-clock assertion.
const realSetTimeout = globalThis.setTimeout;
const realClearTimeout = globalThis.clearTimeout;
const timerCallbacks = new Map();
let nextTimerId = 1;
globalThis.setTimeout = (callback) => {
  const id = nextTimerId++;
  timerCallbacks.set(id, callback);
  return id;
};
globalThis.clearTimeout = (id) => timerCallbacks.delete(id);

const storageWrites = [];
let failingStorageKey = '';
globalThis.localStorage = {
  getItem: () => null,
  removeItem: () => {},
  setItem: (key, value) => {
    storageWrites.push({ key, length: String(value).length });
    if (key === failingStorageKey) throw new Error(`simulated ${key} quota failure`);
  },
};
let pauseNotifications = 0;
globalThis.window = {
  chrome: { webview: { postMessage: () => { pauseNotifications += 1; } } },
};

const previousState = { ...app.state, lastResult: null };
app.state.lastResult = { outcome: 'independent' };
app.componentDidUpdate(null, previousState);
const writesBeforeDeferredFlush = storageWrites.length;
app.componentDidUpdate(null, { ...app.state, lastResult: null });
const coalescedProfileTimers = timerCallbacks.size;
for (const [id, callback] of [...timerCallbacks]) {
  timerCallbacks.delete(id);
  callback();
}
const profileWritesAfterDeferredFlush = storageWrites.filter((entry) => entry.key === 'kt_profiles_v1').length;

const currentQuestion = app.genFor(unit.id, profile, 1);
const checkpointSession = {
  ...session,
  questions: [currentQuestion],
  rolePlan: ['target'],
  idx: 0,
};
app.state = {
  ...app.state,
  profiles: [profile],
  profileIdx: 0,
  screen: 'quiz',
  session: checkpointSession,
  lastResult: null,
};
app._lastSaved = '';
app._lastCheckpoint = '';
app._profilesSavePending = true;
failingStorageKey = 'kt_profiles_v1';
const failedPauseResult = app.pauseLearning(true);
const checkpointAttemptedOnProfileFailure = storageWrites.some((entry) => entry.key === 'kt_session_checkpoint_v1');
failingStorageKey = '';
app._lastSaved = '';
const successfulPauseResult = app.pauseLearning(true);

globalThis.setTimeout = realSetTimeout;
globalThis.clearTimeout = realClearTimeout;

const metrics = {
  curriculumUnits: app.curriculumCatalog().length,
  questionCount,
  generatedCandidates: sessionGeneratedCandidates,
  allowedTopicBuilds,
  stageViewSharesMaps,
  stageViewIsOverlay,
  stageViewStage,
  stageViewElapsedMs: Number(stageViewElapsedMs.toFixed(3)),
  generationElapsedMs: Number(generationElapsedMs.toFixed(3)),
  writesBeforeDeferredFlush,
  coalescedProfileTimers,
  profileWritesAfterDeferredFlush,
  failedPauseResult,
  checkpointAttemptedOnProfileFailure,
  successfulPauseResult,
  pauseNotifications,
};
console.log(JSON.stringify(metrics));

if (verify) {
  const failures = [];
  const maximumCandidates = 1 + (questionCount - 1) * 2;
  if (sessionGeneratedCandidates > maximumCandidates) {
    failures.push(`duplicate generation used ${sessionGeneratedCandidates} candidates; expected at most ${maximumCandidates}`);
  }
  if (allowedTopicBuilds > questionCount) {
    failures.push(`target generation rebuilt allowed topics ${allowedTopicBuilds} times for ${questionCount} questions`);
  }
  if (!stageViewSharesMaps || !stageViewIsOverlay) {
    failures.push('profileAtStage cloned progress maps instead of using an O(1) overlay');
  }
  if (stageViewStage !== 5 || stageView.grade !== unit.grade) {
    failures.push(`the stage overlay changed semantics (stage ${stageViewStage}, grade ${stageView.grade})`);
  }
  if (writesBeforeDeferredFlush !== 0 || coalescedProfileTimers !== 1 || profileWritesAfterDeferredFlush !== 1) {
    failures.push(
      `profile persistence was not deferred/coalesced (${writesBeforeDeferredFlush} synchronous writes, ` +
      `${coalescedProfileTimers} timers, ${profileWritesAfterDeferredFlush} deferred writes)`,
    );
  }
  if (failedPauseResult || !checkpointAttemptedOnProfileFailure || !successfulPauseResult || pauseNotifications !== 1) {
    failures.push(
      `pause persistence has an invalid terminal (failed=${failedPauseResult}, checkpointAttempted=` +
      `${checkpointAttemptedOnProfileFailure}, successful=${successfulPauseResult}, notifications=${pauseNotifications})`,
    );
  }
  if (failures.length) {
    for (const failure of failures) console.error(failure);
    process.exit(1);
  }
}
