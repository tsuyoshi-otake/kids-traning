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

        markup = ReplaceRequiredOccurrences(
            markup,
            "<div style=\"width:300px; flex:none; display:grid; grid-template-columns:repeat(3,1fr); gap:12px; align-content:start;\">",
            "<div class=\"kt-keypad\" style=\"width:300px; flex:none; display:grid; grid-template-columns:repeat(3,1fr); gap:12px; align-content:start;\">",
            StringComparison.Ordinal,
            expectedOccurrences: 2);

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

        markup = ReplaceRequired(
            markup,
            "<div style=\"flex:1; display:flex; flex-direction:column; align-items:center; justify-content:center; margin-top:10px;\">",
            "<div class=\"kt-choice-stage\" style=\"flex:1; display:flex; flex-direction:column; align-items:center; justify-content:center; margin-top:10px;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div role=\"img\" aria-label=\"時計。{{ clockAskLabel }}\" style=\"position:relative;",
            "<div class=\"kt-clock-visual\"><div class=\"kt-clock-face\" role=\"img\" aria-label=\"時計。{{ clockAskLabel }}\" style=\"position:relative;",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:16px; color:#7a6db5; font-weight:700; margin-bottom:12px;\">{{ clockAskLabel }}</div>",
            "<div class=\"kt-clock-label\" style=\"font-size:16px; color:#7a6db5; font-weight:700; margin-bottom:12px;\">{{ clockAskLabel }}</div></div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:20px; color:#9a8662;\">こたえを えらんでね</div>",
            "<div class=\"kt-choice-instruction\" style=\"font-size:20px; color:#9a8662;\">こたえを えらんでね</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"display:flex; flex-direction:column; gap:10px; align-items:flex-start; margin:8px auto 10px; width:100%; max-width:720px; min-width:0; box-sizing:border-box; overflow-x:auto; overscroll-behavior-inline:contain; padding:0 2px;\">",
            "<div class=\"kt-measure-visual\" style=\"display:flex; flex-direction:column; gap:10px; align-items:flex-start; margin:8px auto 10px; width:100%; max-width:720px; min-width:0; box-sizing:border-box; overflow-x:auto; overscroll-behavior-inline:contain; padding:0 2px;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div role=\"img\" aria-label=\"{{ mrow.ariaLabel }}\" style=\"display:flex; align-items:center; gap:10px; width:max-content; max-width:none;\">",
            "<div class=\"kt-measure-row\" role=\"img\" aria-label=\"{{ mrow.ariaLabel }}\" style=\"display:flex; align-items:center; gap:10px; width:max-content; max-width:none;\">",
            StringComparison.Ordinal);

        markup = markup.Replace(
            "style=\"display:grid; grid-template-columns:1fr 1fr; gap:18px; margin-top:22px; width:660px; max-width:90%;\"",
            "class=\"kt-choice-grid\" style=\"display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-top:24px; width:1120px; max-width:100%;\"",
            StringComparison.Ordinal);
        markup = markup.Replace(
            "style=\"display:grid; grid-template-columns:1fr 1fr; gap:18px; margin-top:18px; width:660px; max-width:90%;\"",
            "class=\"kt-choice-grid\" style=\"display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-top:24px; width:1120px; max-width:100%;\"",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:26px; color:#5b5040; margin-top:10px;\">{{ fbPrompt }} = <b>{{ fbAnswer }}</b></div>",
            "<div class=\"kt-feedback-answer\"><div class=\"kt-feedback-row\"><span class=\"kt-feedback-label\">もんだい</span><span class=\"kt-feedback-prompt\">{{ fbPrompt }}</span></div><div class=\"kt-feedback-row\"><span class=\"kt-feedback-label kt-answer-label\">こたえ</span><b class=\"kt-feedback-answer-text\">{{ fbAnswer }}</b></div></div>",
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
            "else{calibPrompt=this.questionRich(cq,'prompt',cq.prompt);}}",
            "else{calibPrompt=this.questionRich(cq,'prompt',cq.prompt);}calibEq=cq.mode==='num'&&!/[=?]/.test(String(cq.prompt||''))?' = ?':'';}",
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
    --kt-feedback-measure: min(760px, 92vw);
    --kt-choice-max: 1120px;
  }

  body {
    text-rendering: optimizeLegibility;
    -webkit-font-smoothing: antialiased;
  }

  [data-screen-label] {
    -webkit-user-select: none;
    user-select: none;
  }

  input,
  textarea,
  [contenteditable="true"] {
    -webkit-user-select: text;
    user-select: text;
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
    width: min(var(--kt-choice-max), 100%) !important;
    max-width: none !important;
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    grid-auto-rows: minmax(80px, auto);
    gap: var(--kt-space-4) !important;
    margin-top: var(--kt-space-6) !important;
  }

  .kt-choice-stage {
    box-sizing: border-box;
    width: 100%;
    min-width: 0;
  }

  .kt-choice,
  .kt-choice-button {
    box-sizing: border-box;
    width: 100%;
    min-width: 0;
    max-width: 100%;
    min-height: 80px !important;
    padding: var(--kt-space-3) var(--kt-space-4) !important;
    font-size: clamp(21px, 1.8vw, 26px) !important;
    line-height: 1.5 !important;
    text-align: center;
    white-space: normal !important;
    overflow-wrap: break-word;
    word-break: normal;
    line-break: strict;
    text-wrap: pretty;
    overflow: visible;
  }

  .kt-choice ruby,
  .kt-choice-button ruby {
    white-space: normal;
  }

  .kt-feedback-answer {
    display: grid;
    gap: var(--kt-space-3);
    width: fit-content;
    min-width: min(460px, 92vw);
    max-width: var(--kt-feedback-measure);
    margin-top: var(--kt-space-4);
    padding: var(--kt-space-4) var(--kt-space-6);
    border: 3px solid var(--kt-border);
    border-radius: 20px;
    background: var(--kt-paper);
    color: #5b5040;
    font-size: clamp(20px, 2vw, 28px);
    line-height: 1.65;
  }

  .kt-feedback-screen {
    width: 100%;
    min-height: 100dvh !important;
    padding: clamp(20px, 3vh, 32px) clamp(20px, 4vw, 48px) !important;
    justify-content: safe center !important;
  }

  .kt-feedback-hero {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: var(--kt-space-6);
    animation: rise .4s ease-out both;
  }

  .kt-feedback-mark {
    display: block;
    flex: none;
    width: 152px;
    height: 152px;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
  }

  .kt-mark-hanamaru {
    stroke-width: 6;
  }

  .kt-mark-batsu {
    stroke-width: 10;
  }

  /* Every mark path declares pathLength="100", so one dash length draws them all. */
  .kt-mark-petals,
  .kt-mark-swirl,
  .kt-mark-stroke {
    stroke-dasharray: 100;
    stroke-dashoffset: 100;
    animation: kt-draw-mark .42s ease-out both;
  }

  .kt-mark-swirl {
    animation-duration: .62s;
    animation-delay: .3s;
  }

  .kt-mark-stroke-b {
    animation-delay: .16s;
  }

  @keyframes kt-draw-mark {
    to {
      stroke-dashoffset: 0;
    }
  }

  .kt-feedback-title {
    font-size: clamp(44px, 4vw, 58px);
    font-weight: 900;
    line-height: 1.15;
  }

  .kt-feedback-hero-correct {
    color: #2f7d44;
  }

  .kt-feedback-hero-wrong {
    color: #c0453d;
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

  .kt-feedback-prompt,
  .kt-feedback-answer-text,
  .kt-feedback-explanation-body {
    overflow-wrap: break-word;
    word-break: normal;
    line-break: strict;
    text-wrap: pretty;
  }

  .kt-feedback-note {
    margin-top: var(--kt-space-3);
    padding: var(--kt-space-2) var(--kt-space-4);
    border: 2px solid;
    border-radius: 18px;
    background: #fff;
    font-size: 18px;
    font-weight: 700;
  }

  .kt-feedback-note-success {
    border-color: #cfe9d4;
    color: #2f7d44;
  }

  .kt-feedback-note-helped {
    border-color: #ffe0a8;
    color: #8a5b0b;
  }

  .kt-feedback-xp {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    gap: var(--kt-space-2);
    margin-top: var(--kt-space-3);
    padding: var(--kt-space-2) var(--kt-space-4);
    border: 3px solid #d5def5;
    border-radius: 18px;
    background: #fff;
    color: #4f7edb;
  }

  .kt-feedback-xp-label {
    font-size: 15px;
    font-weight: 900;
  }

  .kt-feedback-xp-amount {
    font-size: 26px;
    line-height: 1.2;
  }

  .kt-feedback-level-up {
    color: #a96808;
    font-size: 18px;
    font-weight: 900;
    animation: popIn .45s ease-out;
  }

  .kt-feedback-score {
    margin-top: var(--kt-space-3);
    padding: var(--kt-space-2) var(--kt-space-4);
    border: 3px solid #e8d8b9;
    border-radius: 18px;
    background: #fff;
    color: #5b5040;
    font-size: 20px;
    font-weight: 900;
  }

  .kt-feedback-auto-advance {
    margin-top: var(--kt-space-2);
    color: #835a21;
    font-size: 17px;
    font-weight: 800;
  }

  .kt-feedback-rewards {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: var(--kt-space-3);
    margin-top: var(--kt-space-4);
    animation: pop .4s ease-out .2s both;
  }

  .kt-feedback-reward {
    padding: var(--kt-space-2) var(--kt-space-6);
    border: 3px solid var(--kt-ink);
    border-radius: 24px;
    font-size: 24px;
    font-weight: 900;
    line-height: 1.4;
  }

  .kt-feedback-points {
    background: #ffcb45;
  }

  .kt-feedback-combo {
    background: #ff6b3d;
    color: #fff;
  }

  .kt-feedback-explanation {
    width: fit-content;
    min-width: min(460px, 92vw);
    max-width: var(--kt-feedback-measure);
    margin-top: var(--kt-space-4);
    padding: var(--kt-space-4) var(--kt-space-6);
    border: 3px solid #f0d8c0;
    border-radius: 20px;
    background: #fff;
    color: var(--kt-ink);
    animation: rise .4s ease-out .2s both;
  }

  .kt-feedback-explanation-label {
    margin-bottom: var(--kt-space-2);
    color: var(--kt-muted);
    font-size: 16px;
    font-weight: 700;
  }

  .kt-feedback-explanation-body {
    font-size: clamp(20px, 2vw, 24px);
    line-height: 1.5;
  }

  .kt-feedback-topic-row {
    margin-top: var(--kt-space-3);
  }

  .kt-feedback-topic {
    display: inline-block;
    padding: var(--kt-space-1) var(--kt-space-3);
    border: 2px solid #ff8a8a;
    border-radius: 18px;
    background: #ffe6e0;
    color: #b83f31;
    font-size: 16px;
    font-weight: 700;
  }

  .kt-feedback-next {
    min-width: 220px;
    min-height: 64px;
    margin-top: var(--kt-space-6);
    padding: var(--kt-space-3) var(--kt-space-8);
    border: 4px solid #e07d2a;
    border-radius: 24px;
    background: #ff8a3d;
    box-shadow: 0 7px 0 #d96a26;
    color: #fff;
    cursor: pointer;
    font-size: 30px;
    font-weight: 900;
    line-height: 1.2;
    text-align: center;
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

    .kt-choice,
    .kt-choice-button {
      min-height: 72px !important;
      font-size: clamp(18px, 2vw, 24px) !important;
    }

    .kt-feedback-answer {
      gap: var(--kt-space-2);
      padding: var(--kt-space-3) var(--kt-space-4);
    }
  }

  @media (min-width: 1000px) and (max-height: 820px) {
    [data-screen-label]:has(.kt-choice-stage) {
      padding: var(--kt-space-4) var(--kt-space-8) var(--kt-space-6) !important;
    }

    .kt-choice-stage {
      justify-content: flex-start !important;
      margin-top: var(--kt-space-1) !important;
    }

    .kt-choice-stage:has(.kt-clock-face) {
      display: grid !important;
      grid-template-columns: 240px minmax(0, 1fr);
      grid-template-rows: auto auto minmax(0, auto) auto;
      column-gap: var(--kt-space-6);
      align-content: center;
      align-items: center;
    }

    .kt-choice-stage:has(.kt-clock-face) > .kt-clock-visual {
      grid-column: 1;
      grid-row: 1 / 5;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-width: 0;
    }

    .kt-choice-stage:has(.kt-clock-face) .kt-clock-face {
      width: 216px !important;
      height: 216px !important;
      margin: 0 auto !important;
    }

    .kt-choice-stage:has(.kt-clock-face) .kt-clock-label {
      margin: var(--kt-space-2) 0 0 !important;
      text-align: center;
    }

    .kt-choice-stage:has(.kt-clock-face) > .kt-question-prompt {
      grid-column: 2;
      grid-row: 1;
    }

    .kt-choice-stage:has(.kt-clock-face) > .kt-choice-instruction {
      grid-column: 2;
      grid-row: 2;
    }

    .kt-choice-stage:has(.kt-clock-face) > .kt-choice-grid {
      grid-column: 2;
      grid-row: 3 / 5;
      width: 100% !important;
      margin-top: var(--kt-space-3) !important;
    }

    .kt-measure-visual:has(.kt-measure-row:nth-child(3)) {
      display: grid !important;
      grid-template-columns: repeat(2, max-content) !important;
      justify-content: center;
      gap: var(--kt-space-2) var(--kt-space-4) !important;
      max-width: 1000px !important;
      margin: var(--kt-space-1) auto var(--kt-space-2) !important;
      overflow-x: visible !important;
    }

    .kt-measure-visual:has(.kt-measure-row:nth-child(3)) > .kt-measure-row {
      max-width: 100% !important;
    }

    .kt-choice-grid {
      margin-top: var(--kt-space-3) !important;
    }
  }

  @media (max-height: 900px) {
    .kt-feedback-screen {
      padding: var(--kt-space-4) var(--kt-space-6) !important;
    }

    .kt-feedback-hero {
      gap: var(--kt-space-4);
    }

    .kt-feedback-mark {
      width: 120px;
      height: 120px;
    }

    .kt-feedback-title {
      font-size: 44px;
    }

    .kt-feedback-answer {
      gap: var(--kt-space-2);
      margin-top: var(--kt-space-3);
      padding: var(--kt-space-3) var(--kt-space-4);
      font-size: 21px;
      line-height: 1.45;
    }

    .kt-feedback-label {
      min-height: 30px;
      padding: var(--kt-space-1) var(--kt-space-2);
    }

    .kt-feedback-note {
      margin-top: var(--kt-space-2);
      padding: var(--kt-space-1) var(--kt-space-3);
      font-size: 16px;
    }

    .kt-feedback-xp {
      margin-top: var(--kt-space-2);
      padding: var(--kt-space-1) var(--kt-space-3);
    }

    .kt-feedback-xp-label {
      font-size: 14px;
    }

    .kt-feedback-xp-amount {
      font-size: 22px;
    }

    .kt-feedback-level-up {
      font-size: 16px;
    }

    .kt-feedback-score {
      margin-top: var(--kt-space-2);
      padding: var(--kt-space-1) var(--kt-space-3);
      font-size: 17px;
    }

    .kt-feedback-auto-advance {
      margin-top: var(--kt-space-1);
      font-size: 15px;
    }

    .kt-feedback-rewards {
      gap: var(--kt-space-2);
      margin-top: var(--kt-space-3);
    }

    .kt-feedback-reward {
      padding: var(--kt-space-1) var(--kt-space-4);
      font-size: 20px;
    }

    .kt-feedback-explanation {
      margin-top: var(--kt-space-3);
      padding: var(--kt-space-3) var(--kt-space-4);
    }

    .kt-feedback-explanation-label {
      margin-bottom: var(--kt-space-1);
      font-size: 14px;
    }

    .kt-feedback-explanation-body {
      font-size: 21px;
      line-height: 1.45;
    }

    .kt-feedback-topic-row {
      margin-top: var(--kt-space-2);
    }

    .kt-feedback-topic {
      padding: 2px var(--kt-space-2);
      font-size: 14px;
    }

    .kt-feedback-next {
      min-height: 56px;
      margin-top: var(--kt-space-4);
      padding: var(--kt-space-2) var(--kt-space-8);
      font-size: 26px;
    }
  }

  @media (max-height: 760px) {
    .kt-feedback-mark {
      width: 104px;
      height: 104px;
    }

    .kt-feedback-title {
      font-size: 40px;
    }

    .kt-feedback-answer,
    .kt-feedback-explanation-body {
      font-size: 19px;
      line-height: 1.4;
    }

    .kt-feedback-answer {
      padding: var(--kt-space-2) var(--kt-space-4);
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

  @media (max-width: 900px) {
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
