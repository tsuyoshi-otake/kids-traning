// Collect the questions and visible presentation data exposed by the generated app.
// Authored curriculum-bank rows are exhaustive; other generator output is explicitly
// sampled because those generators draw from random domains rather than stored rows.
//
// Usage: node DisplayedQuestionInventory.mjs <generated-runtime-html> <output-dir> [samples-per-stage]

import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export function createAppFromRuntime(runtimePath) {
  const page = readFileSync(runtimePath, 'utf8');
  const classMarker = 'class Component extends DCLogic';
  const classAt = page.indexOf(classMarker);
  if (classAt < 0) throw new Error(`runtime page lacks ${JSON.stringify(classMarker)}: ${runtimePath}`);
  const scriptOpen = page.lastIndexOf('<script', classAt);
  const sourceStart = page.indexOf('>', scriptOpen) + 1;
  const sourceEnd = page.indexOf('</script>', classAt);
  if (scriptOpen < 0 || sourceStart <= 0 || sourceEnd < 0) throw new Error(`cannot locate app script in ${runtimePath}`);

  class DCLogic {
    setState(patch, callback) {
      Object.assign(this.state || (this.state = {}), patch);
      if (typeof callback === 'function') callback();
    }
  }
  const React = {
    Fragment: 'fragment',
    isValidElement(value) { return !!value && typeof value === 'object' && typeof value.type === 'string'; },
    createElement(type, props, ...children) { return { type, props: props || {}, children }; },
  };
  const source = page.slice(sourceStart, sourceEnd);
  const Component = new Function('DCLogic', 'React', `${source}\nreturn Component;`)(DCLogic, React);
  const app = new Component();
  app.props = app.props || {};
  return app;
}

function richSummary(value) {
  const text = [];
  const readings = [];
  const visit = node => {
    if (Array.isArray(node)) { for (const child of node) visit(child); return; }
    if (node === null || node === undefined) return;
    if (typeof node !== 'object') { text.push(String(node)); return; }
    if (node.type === 'rt') {
      readings.push((node.children || []).map(child => richSummaryText(child)).join(''));
      return;
    }
    for (const child of node.children || []) visit(child);
  };
  visit(value);
  return { text: text.join(''), rubyReadings: readings };
}

function richSummaryText(value) {
  if (Array.isArray(value)) return value.map(richSummaryText).join('');
  if (value === null || value === undefined) return '';
  if (typeof value !== 'object') return String(value);
  return (value.children || []).map(richSummaryText).join('');
}

function renderQuestion(app, q) {
  const rendered = {};
  for (const field of ['prompt', 'activityPrompt', 'answer', 'explanation']) {
    if (q[field] !== undefined && q[field] !== null && q[field] !== '') {
      rendered[field] = richSummary(app.questionRich(q, field, q[field]));
    }
  }
  if (Array.isArray(q.choices)) {
    const skipFurigana = app.kanjiTargetChoices(q);
    rendered.choices = q.choices.map((choice, index) => richSummary(app.questionChoiceRich(q, index, choice, skipFurigana)));
  }
  return rendered;
}

const canonical = value => JSON.stringify(value);
const profileFor = grade => ({ grade, name: 'inventory', stars: 0, xp: 0, mastery: {}, skillStats: {}, unitStats: {} });

function inventoryQuestion(app, q, origin) {
  return { ...origin, canonicalQuestion: q, rendered: renderQuestion(app, q) };
}

export function collectInventory({ runtimePath, outputDirectory, samplesPerStage = 10, seed = 0x2f6e2b1 }) {
  if (!runtimePath || !outputDirectory) throw new Error('runtimePath and outputDirectory are required');
  const previousRandom = Math.random;
  let randomState = Number(seed) | 0;
  Math.random = () => {
    randomState = (randomState + 0x6d2b79f5) | 0;
    let value = randomState;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };

  const grades = new Map(Array.from({ length: 9 }, (_, i) => [i + 1, {
    grade: i + 1,
    completeness: { catalogQuestions: 'exhaustive', authoredQuestions: 'exhaustive', generatedQuestions: 'sampled', calibrationQuestions: 'sampled', kanji: 'exhaustive', drillBanks: 'exhaustive' },
    catalogQuestions: [], authoredQuestions: [], generatedSamples: [], calibrationQuestions: [], kanji: [], drillBanks: [],
    coverage: { authoredSourceItems: 0, authoredCollected: 0, generatedRequested: 0, generatedCollectedUnique: 0, generatedDuplicates: 0, generatorErrors: 0 },
  }]));
  const errors = [];
  try {
    const app = createAppFromRuntime(runtimePath);
    const units = app.curriculumCatalog();
    for (const unit of units) {
      const grade = grades.get(Number(unit.grade));
      if (!grade) continue;
      for (let sourceIndex = 0; sourceIndex < (unit.questions || []).length; sourceIndex += 1) {
        grade.catalogQuestions.push({ grade: grade.grade, unitId: unit.id, unitLabel: unit.label, topicId: unit.topicId, sourceIndex, question: unit.questions[sourceIndex] });
      }
      if (unit.generatorKey === 'curriculum-bank') {
        for (let sourceIndex = 0; sourceIndex < (unit.questions || []).length; sourceIndex += 1) {
          const item = unit.questions[sourceIndex];
          grade.coverage.authoredSourceItems += 1;
          const origin = { grade: grade.grade, unitId: unit.id, unitLabel: unit.label, topicId: unit.topicId, sourceIndex, stage: Number(item.stage) || 1, source: 'authored' };
          try {
            const q = app.genFor(unit.id, profileFor(grade.grade), item.stage, item);
            grade.authoredQuestions.push(inventoryQuestion(app, q, origin));
            grade.coverage.authoredCollected += 1;
          } catch (error) {
            grade.coverage.generatorErrors += 1;
            errors.push({ ...origin, error: String(error?.stack || error) });
          }
        }
      } else {
        for (const stage of app.unitStages(unit)) {
          const seen = new Set();
          for (let sampleIndex = 0; sampleIndex < samplesPerStage; sampleIndex += 1) {
            grade.coverage.generatedRequested += 1;
            const origin = { grade: grade.grade, unitId: unit.id, unitLabel: unit.label, topicId: unit.topicId, stage, sampleIndex, source: 'generated-sample' };
            try {
              const q = app.genFor(unit.id, profileFor(grade.grade), stage);
              const key = canonical(q);
              if (seen.has(key)) { grade.coverage.generatedDuplicates += 1; continue; }
              seen.add(key);
              grade.generatedSamples.push(inventoryQuestion(app, q, origin));
              grade.coverage.generatedCollectedUnique += 1;
            } catch (error) {
              grade.coverage.generatorErrors += 1;
              errors.push({ ...origin, error: String(error?.stack || error) });
            }
          }
        }
      }
    }

    // Capture the exact 18-question first-run placement sequence for every grade,
    // including the placement-specific answer choices and question mode.
    for (const grade of grades.values()) {
      app.state = { ...(app.state || {}), setupGrade: grade.grade };
      for (const [index, item] of app.buildCalib().entries()) {
        const q = item.q;
        grade.calibrationQuestions.push({ grade: grade.grade, index, source: 'first-run-calibration', canonicalQuestion: q,
          displayMode: q.mode || (q.choices ? 'choices' : 'numeric'), choices: (item.choices || []).map((choice, choiceIndex) => richSummary(app.questionChoiceRich(q, choiceIndex, choice, app.kanjiTargetChoices(q)))), rendered: renderQuestion(app, q) });
      }
    }

    for (const entry of app.kanjiCurriculumEntries()) {
      const grade = grades.get(Number(entry.g));
      if (grade) grade.kanji.push(entry);
    }
    for (const course of app.drillCourses()) {
      const grade = grades.get(Number(course.grade));
      if (!grade) continue;
      try {
        const bank = app.drillBank(course.id);
        const kanji = course.id === 'k1' || course.id === 'k2';
        const questions = bank.map((q, index) => {
          const modes = {};
          for (const answerMode of (kanji ? ['reading', 'writing'] : ['input', 'choice'])) {
            const d = { id: course.id, answerMode, choiceOrder: app.drillChoiceOrder(2, (Number(q.no) || 1) * 2654435761) };
            const presented = app.drillPresentedQuestion(d, q);
            const choices = app.drillChoices(d, presented);
            modes[answerMode] = { question: presented, choices, displayMode: choices.length ? 'choices' : 'input' };
          }
          return { index, canonicalQuestion: q, modes };
        });
        grade.drillBanks.push({ course, questions });
      } catch (error) {
        errors.push({ grade: grade.grade, courseId: course.id, source: 'drill-bank', error: String(error?.stack || error) });
      }
    }

    mkdirSync(outputDirectory, { recursive: true });
    for (const [gradeNumber, inventory] of grades) {
      writeFileSync(resolve(outputDirectory, `grade-${gradeNumber}.json`), `${JSON.stringify(inventory, null, 2)}\n`);
    }
    const summary = {
      runtimePath: resolve(runtimePath), outputDirectory: resolve(outputDirectory), seed: Number(seed) | 0,
      samplesPerStage, curriculumUnitCount: units.length,
      grades: [...grades.values()].map(({ grade, completeness, coverage, catalogQuestions, authoredQuestions, generatedSamples, calibrationQuestions, kanji, drillBanks }) => ({
        grade, completeness, coverage, catalogQuestionCount: catalogQuestions.length, authoredQuestionCount: authoredQuestions.length,
        calibrationQuestionCount: calibrationQuestions.length,
        generatedSampleCount: generatedSamples.length, kanjiCount: kanji.length,
        drillBanks: drillBanks.map(bank => ({ id: bank.course.id, questionCount: bank.questions.length, displayModes: [...new Set(bank.questions.flatMap(q => Object.keys(q.modes)))] })),
      })),
      generatorErrorCount: errors.length, errors,
    };
    writeFileSync(resolve(outputDirectory, 'summary.json'), `${JSON.stringify(summary, null, 2)}\n`);
    return summary;
  } finally {
    Math.random = previousRandom;
  }
}

const invokedPath = process.argv[1] && resolve(process.argv[1]);
if (invokedPath === resolve(fileURLToPath(import.meta.url))) {
  const [, , runtimePath, outputDirectory, rawSamples] = process.argv;
  if (!runtimePath || !outputDirectory) {
    console.error('Usage: node DisplayedQuestionInventory.mjs <generated-runtime-html> <output-dir> [samples-per-stage]');
    process.exitCode = 2;
  } else {
    try {
      const summary = collectInventory({ runtimePath, outputDirectory, samplesPerStage: Math.max(1, Math.floor(Number(rawSamples) || 10)) });
      console.log(JSON.stringify({ outputDirectory: summary.outputDirectory, grades: summary.grades.map(g => ({ grade: g.grade, catalogQuestionCount: g.catalogQuestionCount, authoredQuestionCount: g.authoredQuestionCount, calibrationQuestionCount: g.calibrationQuestionCount, generatedSampleCount: g.generatedSampleCount, kanjiCount: g.kanjiCount, drillBanks: g.drillBanks })), generatorErrorCount: summary.generatorErrorCount }));
      if (summary.generatorErrorCount) process.exitCode = 1;
    } catch (error) {
      console.error(error?.stack || error);
      process.exitCode = 2;
    }
  }
}
