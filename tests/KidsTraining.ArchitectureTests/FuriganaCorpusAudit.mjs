import { writeFileSync } from 'node:fs';

// Capture the text of the real generated questions, not just dictionary keys.
// The optional JSON is a human-review inventory, not a claim that its readings
// have been validated. Context fixtures provide independent pronunciation checks.
export function createFuriganaCorpusAudit(app, units) {
  const texts = new Map();
  const capture = (value, origin) => {
    if (typeof value === 'string' && /[一-龯々]/.test(value)) {
      if (!texts.has(value)) texts.set(value, { id: texts.size, text: value, sources: new Map() });
      texts.get(value).sources.set(JSON.stringify(origin), origin);
    } else if (Array.isArray(value)) value.forEach(item => capture(item, origin));
    else if (value && typeof value === 'object') for (const [key, item] of Object.entries(value)) capture(item, { ...origin, field: `${origin.field}.${key}` });
  };
  const record = (q, origin = {}) => {
    for (const key of ['prompt', 'answer', 'choices', 'distractors', 'explanation', 'activityPrompt', 'pre', 'post', 'mean', 'display'])
      capture(q[key], { ...origin, field: key, assessmentTarget: key === 'prompt' ? q.readingTarget : undefined });
  };
  for (const unit of units) {
    const origin = { grade: unit.grade, unit: unit.id, kind: 'authored' };
    for (const key of ['label', 'title', 'name', 'description']) capture(unit[key], { ...origin, field: key });
    (unit.questions || []).forEach((q, index) => record(q, { ...origin, index }));
  }
  for (const entry of app.kanjiCurriculumEntries()) {
    for (const key of ['word', 'pre', 'post', 'mean']) capture(entry[key], { grade: entry.g, kind: 'kanji', target: entry.k, field: key });
  }
  const plain = value => Array.isArray(value) ? value.map(plain).join('')
    : value && typeof value === 'object' ? value.type === 'rt' ? '' : plain(value.children || []) : String(value ?? '');
  const raw = value => Array.isArray(value) ? value.map(raw).join('')
    : value && typeof value === 'object' ? raw(value.children || []) : String(value ?? '');
  const visible = value => plain(value).replace(/−/g, '-');
  return { record, finish(observe, violated) {
    const readings = new Map(), unannotated = new Map();
    for (const [text, textEntry] of texts) {
      const rendered = app.withFurigana(text);
      observe('corpus furigana preserves visible text');
      // Fractions, minus signs and powers have their own intentional display
      // normalization. Compare against that renderer without ruby.
      // Ruby creates segment boundaries, where a leading negative number can
      // acquire the typographic minus glyph. Both glyphs represent the same sign.
      if (visible(rendered) !== visible(app.withLearningNotation(text))) violated('corpus furigana preserves visible text', plain(rendered), text);
      const collect = value => {
        if (Array.isArray(value)) return value.map(collect).join('');
        if (!value || typeof value !== 'object') return String(value ?? '');
        if (value.type !== 'ruby') return (value.children || []).map(collect).join('');
        const surface = plain(value), reading = raw(value.children.find(child => child?.type === 'rt'));
        const key = `${surface}=${reading}`;
        if (!readings.has(key)) readings.set(key, { surface, reading, count: 0, contexts: [], contextIds: new Set() });
        const entry = readings.get(key);
        entry.count++;
        entry.contextIds.add(textEntry.id);
        if (entry.contexts.length < 3) entry.contexts.push(text);
        return ' ';
      };
      const remaining = collect(rendered);
      // Bare/quoted assessment kanji intentionally need no automatic pronunciation.
      if (text.length > 1 && !/いみ：\d年生で 習う 漢字/.test(text)) {
        let prose = remaining.replace(/「[一-龯々]」/g, '');
        for (const source of textEntry.sources.values()) {
          if (source.field === 'prompt' && source.assessmentTarget)
            prose = prose.replaceAll(`「${source.assessmentTarget}」`, '');
        }
        for (const match of prose.matchAll(/[一-龯々]+/g)) {
          if (!unannotated.has(match[0])) unannotated.set(match[0], { surface: match[0], count: 0, contexts: [] });
          const entry = unannotated.get(match[0]);
          entry.count++;
          if (entry.contexts.length < 3) entry.contexts.push(text);
        }
      }
    }
    const report = { texts: texts.size, uniqueReadings: readings.size,
      textInventory: [...texts.values()].map(entry => ({ ...entry, sources: [...entry.sources.values()] })),
      readings: [...readings.values()].map(entry => ({ ...entry, contextIds: [...entry.contextIds] })), unannotated: [...unannotated.values()] };
    observe('curriculum prose has no unannotated kanji outside assessment targets');
    for (const entry of report.unannotated) violated('curriculum prose has no unannotated kanji outside assessment targets', entry.surface, entry.contexts[0]);
    if (process.env.KIDS_TRAINING_FURIGANA_AUDIT_OUTPUT) writeFileSync(process.env.KIDS_TRAINING_FURIGANA_AUDIT_OUTPUT, JSON.stringify(report, null, 2));
    return `furigana corpus: ${report.texts} distinct texts, ${report.uniqueReadings} reading pairs, ${unannotated.size} unannotated groups for review`;
  } };
}
