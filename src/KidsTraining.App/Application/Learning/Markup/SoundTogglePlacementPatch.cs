namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    // One look for every in-page sound toggle, so the button stays the same pill in the same
    // top-right spot from the start screen through questions, feedback and drills.
    private const string SoundToggle =
        "<span role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" class=\"kt-sound-toggle\" title=\"おと の オン/オフ\" aria-label=\"音声のオン・オフ\">{{ muteIcon }}</span>";

    private const string CornerSoundToggle =
        "<span role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" class=\"kt-sound-toggle kt-sound-toggle-corner\" title=\"おと の オン/オフ\" aria-label=\"音声のオン・オフ\">{{ muteIcon }}</span>";

    // The global sound button is fixed to the bottom-right corner. There it sat on the start
    // button and on the corner of question and drill cards, so every child-facing screen carries
    // its own toggle at the top right and the corner copy is hidden while one is on screen.
    // Screens with a header row take it at the row's right end; the feedback, pass and retry
    // screens have no header, so their toggle is fixed to the same top-right spot. The profile
    // and parent screens keep the corner button (Issue #111, after #101 and #109 for the APK).
    private static string PatchSoundTogglePlacement(string markup)
    {
        markup = ReplaceRequired(markup,
            "<div role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" title=\"おと の オン/オフ\"",
            "<div role=\"button\" tabindex=\"0\" onclick=\"{{ toggleMute }}\" class=\"kt-mute-fab\" title=\"おと の オン/オフ\"",
            StringComparison.Ordinal);

        // Start: the last chip of the top bar, so the toggle is in the same corner as on every
        // other child screen. The APK hides 🔑 解除 and keeps the toggle in that corner.
        markup = ReplaceRequired(markup,
            "🔑 解除</span>\n        </div>",
            "🔑 解除</span>\n          " + SoundToggle + "\n        </div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "{{ calibIdx }} / {{ calibTotal }}</div>\n      </div>",
            "{{ calibIdx }} / {{ calibTotal }}</div>\n        " + SoundToggle + "\n      </div>",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "🔥 {{ comboCount }}れんぞく</span>\n        </sc-if>\n      </div>\n      <!-- question metadata -->",
            "🔥 {{ comboCount }}れんぞく</span>\n        </sc-if>\n        " + SoundToggle + "\n      </div>\n      <!-- question metadata -->",
            StringComparison.Ordinal);

        markup = ReplaceRequired(markup,
            "<button type=\"button\" class=\"kt-drill-mode-back\" aria-label=\"とっくんの いちらんに もどる\" onclick=\"{{ drillModeView.onBack }}\">← もどる</button>",
            "<div class=\"kt-drill-mode-head\">\n"
            + "        <button type=\"button\" class=\"kt-drill-mode-back\" aria-label=\"とっくんの いちらんに もどる\" onclick=\"{{ drillModeView.onBack }}\">← もどる</button>\n"
            + "        " + SoundToggle + "\n"
            + "      </div>",
            StringComparison.Ordinal);

        // The drill header sits outside the playing and finished blocks, so the finish screen keeps it.
        markup = ReplaceRequired(markup,
            "<div class=\"kt-drill-count\">{{ drillView.countText }}</div>",
            "<div class=\"kt-drill-count\">{{ drillView.countText }}</div>\n        " + SoundToggle,
            StringComparison.Ordinal);

        // Placed before the screen element so the toggle stays out of the feedback live region.
        foreach (var screenOpening in new[]
        {
            "<div class=\"kt-feedback-screen\" data-screen-label=\"フィードバック\"",
            "<div data-screen-label=\"合格・解除\"",
            "<div data-screen-label=\"もういちど\"",
        })
        {
            markup = ReplaceRequired(markup, screenOpening, CornerSoundToggle + "\n    " + screenOpening, StringComparison.Ordinal);
        }

        return ReplaceRequired(markup, "</head>", BuildSoundToggleStyle() + "\n</head>", StringComparison.Ordinal);
    }

    private static string BuildSoundToggleStyle()
    {
        return """
<style id="kt-sound-toggle">
  .kt-sound-toggle{flex:none;display:inline-flex;align-items:center;justify-content:center;min-width:46px;min-height:46px;box-sizing:border-box;background:#fff;border:3px solid #f0e2c8;border-radius:24px;padding:7px 14px;font-size:18px;line-height:1;cursor:pointer;user-select:none;}
  .kt-sound-toggle:hover{background:#fff8ee;border-color:#d9bd8c;}
  .kt-sound-toggle:active{transform:translateY(2px);}
  .kt-sound-toggle:focus-visible{outline:4px solid #2f7ccd;outline-offset:3px;}
  .kt-sound-toggle-corner{position:fixed;top:24px;right:44px;z-index:50;}
  .kt-drill-mode-head{display:flex;align-items:center;justify-content:space-between;gap:12px;}
  .kt-drill-mode-head .kt-drill-mode-back{align-self:center;}
  body:has(.kt-sound-toggle) .kt-mute-fab{display:none!important;}
</style>
""".TrimEnd('\r', '\n');
    }
}
