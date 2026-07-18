namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string PatchLayoutAndTypography(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "<html><head>",
            "<html lang=\"ja\"><head>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "</head>",
            BuildLayoutTypographyStyles() + "\n</head>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "isPlainEq=modeNumeric;",
            "isPlainEq=modeNumeric&&q.topic!=='story';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:72px; font-weight:900;\">{{ prompt }} = ?</div>",
            "<div class=\"kt-question-prompt kt-numeric-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}<sc-if value=\"{{ isPlainEq }}\" hint-placeholder-val=\"{{ true }}\"> = ?</sc-if></div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"flex:1; display:flex; gap:30px; margin-top:18px; align-items:stretch;\">",
            "<div class=\"kt-numeric-layout\" style=\"flex:1; display:flex; gap:30px; margin-top:18px; align-items:stretch;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"width:300px; flex:none; display:grid; grid-template-columns:repeat(3,1fr); gap:12px; align-content:start;\">",
            "<div class=\"kt-keypad\" style=\"width:300px; flex:none; display:grid; grid-template-columns:repeat(3,1fr); gap:12px; align-content:start;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"{{ promptStyle }}\">{{ prompt }}</div>",
            "<div class=\"kt-question-prompt\" style=\"{{ promptStyle }}\">{{ prompt }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:60px; font-weight:900; text-align:center; white-space:nowrap;\">{{ calibPrompt }}{{ calibEq }}</div>",
            "<div class=\"kt-question-prompt kt-calibration-prompt\">{{ calibPrompt }}{{ calibEq }}</div>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "<div style=\"font-size:46px; font-weight:900; text-align:center; margin-bottom:10px;\">{{ calibKokuPre }}",
            "<div class=\"kt-question-prompt kt-kokugo-prompt\">{{ calibKokuPre }}",
            StringComparison.Ordinal);
        markup = markup.Replace(
            "<div style=\"font-size:46px; font-weight:900; text-align:center; margin-bottom:10px;\">{{ kokuPre }}",
            "<div class=\"kt-question-prompt kt-kokugo-prompt\">{{ kokuPre }}",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "<div onclick=\"{{ c.onClick }}\" style=\"{{ c.style }}\">{{ c.text }}</div>",
            "<div class=\"kt-choice\" onclick=\"{{ c.onClick }}\" style=\"{{ c.style }}\">{{ c.text }}</div>",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "style=\"display:grid; grid-template-columns:1fr 1fr; gap:18px; margin-top:22px; width:660px; max-width:90%;\"",
            "class=\"kt-choice-grid\" style=\"display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-top:24px; width:880px; max-width:92%;\"",
            StringComparison.Ordinal);
        markup = markup.Replace(
            "style=\"display:grid; grid-template-columns:1fr 1fr; gap:18px; margin-top:18px; width:660px; max-width:90%;\"",
            "class=\"kt-choice-grid\" style=\"display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-top:24px; width:880px; max-width:92%;\"",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            "<div class=\"kt-feedback-answer\"><div class=\"kt-feedback-row\"><span class=\"kt-feedback-label\">もんだい</span><span>{{ fbPrompt }}</span></div><div class=\"kt-feedback-row\"><span class=\"kt-feedback-label kt-answer-label\">こたえ</span><b>{{ fbAnswer }}</b></div></div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "const plen=String(q.prompt||'').length;promptStyle='font-size:'+(plen>16?30:(plen>11?40:54))+'px; font-weight:900; text-align:center; margin-bottom:6px; white-space:'+(plen>16?'normal':'nowrap')+'; max-width:880px; line-height:1.35;';",
            "const plen=String(q.prompt||'').length,promptSize=plen>48?28:(plen>32?32:(plen>20?36:(plen>12?44:56)));promptStyle='font-size:'+promptSize+'px; font-weight:900; text-align:center; margin-bottom:8px; white-space:pre-line; max-width:min(880px,92vw); line-height:1.65; padding:.5em 16px .15em; overflow-wrap:break-word; word-break:normal;';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "let calibIdx=0,calibTotal=0,calibProgStyle='',calibTopicLabel='',calibTopicChipStyle='',calibChoices=[],calibIsKokugo=false,calibIsPlain=true,calibPrompt='',calibKokuPre='',calibKokuWord='',calibKokuPost='';",
            "let calibIdx=0,calibTotal=0,calibProgStyle='',calibTopicLabel='',calibTopicChipStyle='',calibChoices=[],calibIsKokugo=false,calibIsPlain=true,calibPrompt='',calibEq='',calibKokuPre='',calibKokuWord='',calibKokuPost='';",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "else{calibPrompt=cq.prompt;}}",
            "else{calibPrompt=cq.prompt;}calibEq=cq.mode==='num'?' = ?':'';}",
            StringComparison.Ordinal);
        markup = ReplaceBlock(
            markup,
            "calibEq:",
            ", calibKokuPre:",
            "calibEq:calibEq");

        markup = ReplaceRequired(
            markup,
            "it.t+'　もんだい： '+it.q",
            "it.t+'\\nもんだい： '+it.q",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildLayoutTypographyStyles()
    {
        return """
<style id="kt-layout-typography">
  :root {
    --kt-space-1: 4px;
    --kt-space-2: 8px;
    --kt-space-3: 12px;
    --kt-space-4: 16px;
    --kt-space-6: 24px;
    --kt-space-8: 32px;
    --kt-focus: #2563eb;
    --kt-ink: #3a3326;
    --kt-muted: #765f3d;
    --kt-paper: #fffdf8;
    --kt-border: #e7d6b6;
  }

  body {
    text-rendering: optimizeLegibility;
    -webkit-font-smoothing: antialiased;
  }

  ruby {
    ruby-position: over;
    ruby-align: center;
  }

  rt {
    font-size: .46em;
    font-weight: 700;
    line-height: 1;
    letter-spacing: 0;
  }

  .kt-question-prompt {
    max-width: min(880px, 92vw) !important;
    padding: .5em var(--kt-space-4) .15em !important;
    margin: 0 auto var(--kt-space-2) !important;
    color: var(--kt-ink);
    font-weight: 900 !important;
    line-height: 1.65 !important;
    letter-spacing: .01em;
    text-align: center !important;
    white-space: pre-line !important;
    overflow-wrap: break-word;
    word-break: normal;
    line-break: strict;
    text-wrap: pretty;
  }

  .kt-numeric-prompt {
    max-width: min(1120px, 96%) !important;
    font-variant-numeric: tabular-nums;
  }

  .kt-calibration-prompt,
  .kt-kokugo-prompt {
    font-size: clamp(30px, 3.5vw, 48px) !important;
  }

  .kt-choice-grid {
    width: min(880px, 92vw) !important;
    gap: var(--kt-space-4) !important;
    margin-top: var(--kt-space-6) !important;
  }

  .kt-choice {
    min-height: 96px !important;
    padding: var(--kt-space-3) var(--kt-space-4) !important;
    font-size: clamp(24px, 2.2vw, 36px) !important;
    line-height: 1.5 !important;
    text-align: center;
    white-space: normal !important;
    overflow-wrap: break-word;
    word-break: normal;
    line-break: strict;
    text-wrap: pretty;
  }

  .kt-feedback-answer {
    display: grid;
    gap: var(--kt-space-3);
    width: min(900px, 92vw);
    margin-top: var(--kt-space-4);
    padding: var(--kt-space-4) var(--kt-space-6);
    border: 3px solid var(--kt-border);
    border-radius: 20px;
    background: var(--kt-paper);
    color: #5b5040;
    font-size: clamp(20px, 2vw, 28px);
    line-height: 1.65;
  }

  .kt-feedback-row {
    display: grid;
    grid-template-columns: 6em minmax(0, 1fr);
    gap: var(--kt-space-4);
    align-items: start;
  }

  .kt-feedback-label {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 36px;
    padding: var(--kt-space-1) var(--kt-space-3);
    border-radius: 999px;
    background: #f3e8d4;
    color: var(--kt-muted);
    font-size: .72em;
    font-weight: 900;
    white-space: nowrap;
  }

  .kt-answer-label {
    background: #eafaef;
    color: #2f7d44;
  }

  [role="button"],
  button {
    transition: transform .18s ease, filter .18s ease;
  }

  [role="button"]:hover {
    filter: brightness(.98) saturate(1.04);
    transform: translateY(-1px);
  }

  [role="button"]:active {
    transform: translateY(1px) scale(.985);
  }

  [role="button"]:focus-visible,
  button:focus-visible,
  input:focus-visible,
  select:focus-visible,
  textarea:focus-visible {
    outline: 4px solid var(--kt-focus) !important;
    outline-offset: 4px !important;
  }

  @media (max-width: 1100px), (max-height: 760px) {
    .kt-question-prompt {
      line-height: 1.55 !important;
    }

    .kt-choice-grid {
      gap: var(--kt-space-3) !important;
      margin-top: var(--kt-space-3) !important;
    }

    .kt-choice {
      min-height: 72px !important;
      font-size: clamp(20px, 2.7vw, 30px) !important;
    }

    .kt-feedback-answer {
      gap: var(--kt-space-2);
      padding: var(--kt-space-3) var(--kt-space-4);
    }
  }

  @media (max-width: 800px) {
    .kt-numeric-layout {
      flex-direction: column;
      gap: 12px !important;
    }

    .kt-numeric-layout > .kt-keypad {
      width: 100% !important;
      grid-template-columns: repeat(6, minmax(0, 1fr)) !important;
      gap: 8px !important;
      align-self: center;
    }
  }

  @media (max-width: 720px) {
    .kt-choice-grid {
      grid-template-columns: 1fr !important;
    }

    .kt-feedback-row {
      grid-template-columns: 1fr;
    }

    .kt-feedback-label {
      justify-self: start;
    }
  }

  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      scroll-behavior: auto !important;
      animation-duration: .01ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: .01ms !important;
    }
  }
</style>
""";
    }
}
