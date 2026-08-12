using System.Globalization;
using System.Text;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    /// <summary>
    /// Clearing a session was visually flatter than answering a single question, which drains the
    /// payoff of a 30-question run. The clear screen now gets a confetti burst plus staggered
    /// entrances, and the feedback rewards pop in instead of appearing instantly.
    /// </summary>
    private static string PatchCelebrationEffects(string markup)
    {
        markup = ReplaceRequired(
            markup,
            "</head>",
            BuildCelebrationStyles() + "\n</head>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div data-screen-label=\"合格・解除\" style=\"position:relative; min-height:100vh; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:40px;\">",
            "<div data-screen-label=\"合格・解除\" style=\"position:relative; min-height:100vh; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:40px;\">\n"
                + BuildConfettiLayerMarkup(),
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:26px; color:#e0a02a; font-weight:900; letter-spacing:6px;\">✦ ✦ ✦</div>\n      <div style=\"font-size:64px; font-weight:900; animation:popIn .5s ease-out;\">ごうかく！</div>",
            "<div class=\"kt-clear-sparkles\" style=\"font-size:26px; color:#e0a02a; font-weight:900; letter-spacing:6px;\">✦ ✦ ✦</div>\n      <div class=\"kt-clear-title\" style=\"font-size:64px; font-weight:900;\">ごうかく！</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequiredOccurrences(
            markup,
            "<div style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            "<div class=\"kt-clear-card\" style=\"width:180px; background:#fff; border:4px solid #f0e2c8; border-radius:22px; padding:16px; text-align:center;\">",
            StringComparison.Ordinal,
            3);

        // RewardMarkupPatch inserts a fourth, blue-bordered XP card between the star cards; it has
        // to join the same staggered entrance or it pops in ahead of its neighbours.
        markup = ReplaceRequired(
            markup,
            "<div style=\"width:180px; background:#fff; border:4px solid #c9d8ff; border-radius:22px; padding:16px; text-align:center;\">",
            "<div class=\"kt-clear-card\" style=\"width:180px; background:#fff; border:4px solid #c9d8ff; border-radius:22px; padding:16px; text-align:center;\">",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"font-size:36px; font-weight:900; color:#e0a02a;\">＋{{ earnedStars }}</div>",
            "<div class=\"kt-clear-earned\" style=\"font-size:36px; font-weight:900; color:#e0a02a;\">＋{{ earnedStars }}</div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(
            markup,
            "<div style=\"display:flex; gap:16px; margin-top:28px; flex-wrap:wrap; justify-content:center;\">",
            "<div class=\"kt-clear-unlock\" style=\"display:flex; gap:16px; margin-top:28px; flex-wrap:wrap; justify-content:center;\">",
            StringComparison.Ordinal);

        return markup;
    }

    private static string BuildConfettiLayerMarkup()
    {
        // Fixed pieces rather than a random generator: the celebration must render identically in
        // the shipped page and in the audit harness, which snapshots the markup.
        var pieces = new (int LeftPercent, int Drift, int DelayMs, int DurationMs, string Color, bool Round)[]
        {
            (6, -46, 0, 2500, "#ff8a3d", false),
            (14, 38, 180, 2800, "#3aa655", true),
            (22, -28, 90, 2350, "#4f7edb", false),
            (30, 54, 320, 2900, "#ffd24a", true),
            (38, -60, 240, 2600, "#d64f8e", false),
            (46, 24, 60, 2750, "#1fa39a", true),
            (54, -34, 400, 2500, "#ff8a3d", false),
            (62, 62, 150, 2850, "#7a6db5", true),
            (70, -18, 300, 2400, "#ffd24a", false),
            (78, 44, 210, 2950, "#3aa655", true),
            (86, -52, 120, 2650, "#d64f8e", false),
            (94, 30, 380, 2550, "#4f7edb", true),
        };

        var builder = new StringBuilder("      <div class=\"kt-confetti\" aria-hidden=\"true\">");
        foreach (var piece in pieces)
        {
            builder.Append(CultureInfo.InvariantCulture, $"<span style=\"left:{piece.LeftPercent}%; background:{piece.Color}; --kt-drift:{piece.Drift}px; animation-delay:{piece.DelayMs}ms; animation-duration:{piece.DurationMs}ms;");
            if (piece.Round)
            {
                builder.Append(" border-radius:50%; height:12px;");
            }

            builder.Append("\"></span>");
        }

        builder.Append("</div>");
        return builder.ToString();
    }

    private static string BuildCelebrationStyles()
    {
        return """
<style id="kt-celebration">
  .kt-confetti {
    position: absolute;
    inset: 0;
    overflow: hidden;
    pointer-events: none;
    z-index: 2;
  }

  .kt-confetti span {
    position: absolute;
    top: -10%;
    width: 12px;
    height: 18px;
    border-radius: 3px;
    opacity: 0;
    animation-name: kt-confetti-fall;
    animation-timing-function: cubic-bezier(.25, .6, .4, 1);
    animation-fill-mode: forwards;
  }

  @keyframes kt-confetti-fall {
    0% { opacity: 0; transform: translate3d(0, 0, 0) rotate(0deg); }
    12% { opacity: 1; }
    85% { opacity: 1; }
    100% { opacity: 0; transform: translate3d(var(--kt-drift, 0px), 118vh, 0) rotate(680deg); }
  }

  .kt-clear-title {
    animation: kt-clear-bounce .72s cubic-bezier(.22, 1.4, .4, 1) both;
  }

  @keyframes kt-clear-bounce {
    0% { transform: scale(.35) translateY(28px); opacity: 0; }
    55% { transform: scale(1.16) translateY(0); opacity: 1; }
    75% { transform: scale(.96); }
    100% { transform: scale(1); opacity: 1; }
  }

  .kt-clear-sparkles {
    animation: kt-sparkle 1.4s ease-in-out .35s 3;
  }

  @keyframes kt-sparkle {
    0%, 100% { opacity: .45; transform: scale(1); }
    50% { opacity: 1; transform: scale(1.12); }
  }

  .kt-clear-card {
    animation: rise .42s ease-out both;
  }

  .kt-clear-card:nth-child(1) { animation-delay: .34s; }
  .kt-clear-card:nth-child(2) { animation-delay: .44s; }
  .kt-clear-card:nth-child(3) { animation-delay: .54s; }
  .kt-clear-card:nth-child(4) { animation-delay: .64s; }

  .kt-clear-earned {
    animation: kt-clear-bounce .6s cubic-bezier(.22, 1.4, .4, 1) .52s both;
  }

  .kt-clear-unlock {
    animation: pop .4s ease-out .8s both;
  }

  .kt-feedback-reward {
    animation: pop .34s ease-out both;
  }

  .kt-feedback-combo {
    animation-delay: .14s;
  }

  @media (prefers-reduced-motion: reduce) {
    .kt-confetti {
      display: none;
    }
  }
</style>
""";
    }
}
