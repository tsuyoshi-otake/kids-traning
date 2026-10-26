namespace KidsTraining.RuntimeExport;

/// <summary>
/// Narrows a fully patched learning runtime page down to the drill (とっくんモード)
/// courses for the Android WebView wrapper. The generated markup is produced by the
/// same LearningPageBuilder + LearningMarkupPatcher pipeline the Windows app uses;
/// this transform only hides the non-drill start-screen elements and injects the
/// mobile stylesheet. Every anchor is required, so a template change fails the
/// export instead of silently shipping a broken drill-only page.
/// </summary>
internal static class MobileDrillOnlyPatch
{
    public static string Apply(string markup)
    {
        // Start-screen mascot, greeting, mission/weakness cards, the session start button,
        // and the level/star/parent/unlock chips are hidden with a shared class so the
        // drill grid is the only visible content. The DC runtime renders class attributes
        // verbatim, so a marker class is more reliable than matching onclick bindings.
        markup = AddClassAttribute(markup,
            "<div style=\"position:relative; width:200px; height:200px; flex:none; animation:floaty 2.8s ease-in-out infinite;\"");
        markup = AddClassAttribute(markup,
            "<div style=\"font-size:46px; font-weight:900;\"");
        markup = AddClassAttribute(markup,
            "<div style=\"font-size:24px; color:#6b5e45; margin-top:4px;\"");
        markup = ExtendClassAttribute(markup,
            "<div class=\"kt-hero-card\" style=\"background:#fff; border-color:#f0e2c8;\"");
        markup = ExtendClassAttribute(markup,
            "<div class=\"kt-hero-card kt-hero-weak\" style=\"background:#ffe6e0; border-color:#ff8a8a;\"");
        markup = AddClassAttribute(markup,
            "onclick=\"{{ goStart }}\" style=\"background:#ff8a3d; color:#fff; border:5px solid #e07d2a; border-radius:28px; padding:20px; text-align:center; font-size:38px; font-weight:900; cursor:pointer; box-shadow:0 8px 0 #d96a26;\"");
        markup = AddClassAttribute(markup,
            "<div style=\"margin-top:18px; background:#fff; border:4px solid #f0e2c8; border-radius:20px; padding:12px 16px; display:grid; grid-template-columns:auto 1fr auto; gap:12px; align-items:center;\"");
        markup = AddClassAttribute(markup,
            "<span style=\"background:#fff; border:3px solid #f0e2c8; border-radius:24px; padding:7px 16px; font-size:18px; font-weight:700;\"");
        markup = AddClassAttribute(markup,
            "<span style=\"background:#ffcb45; border:3px solid #3a3326; border-radius:24px; padding:7px 16px; font-size:18px; font-weight:700;\"");
        markup = AddClassAttribute(markup,
            "<span role=\"button\" tabindex=\"0\" onclick=\"{{ goEmergency }}\"");

        // The sound toggle already sits at the top right of the start, answer-mode and drill
        // screens (the shared SoundTogglePlacementPatch, #109 and #111); BuildMobileStyle only
        // moves the drill title below it on a narrow phone.

        // The parent dashboard's "pause to desktop" action only talks to the WebView2
        // host (kidsTraining.pause), which does not exist on Android.
        markup = AddClassAttribute(markup,
            "<span role=\"button\" tabindex=\"0\" onclick=\"{{ pauseLearning }}\"");

        // Marker class so the Android back key can step out of the PIN gate and the
        // parent dashboard just like it leaves the drill screens.
        markup = ReplaceRequired(markup,
            "<span role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\"",
            "<span role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\" class=\"kt-mobile-back\"");
        markup = ReplaceRequired(markup,
            "<div role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\"",
            "<div role=\"button\" tabindex=\"0\" onclick=\"{{ backStart }}\" class=\"kt-mobile-back\"");

        // The panel that holds the greeting and the hero row keeps only the drill grid.
        markup = ReplaceRequired(markup,
            "<div style=\"max-width:1000px;\">",
            "<div class=\"kt-mobile-panel\" style=\"max-width:1000px;\">");

        // App methods bridging to the native KtAndroid JavascriptInterface. They no-op
        // when the bridge is absent so the same runtime still opens in a plain browser.
        markup = ReplaceRequired(markup,
            "  drillStorageKey(){return 'kt_drill_v1';}",
            "  drillStorageKey(){return 'kt_drill_v1';}\n"
            + "  ktAndroid(){try{return typeof KtAndroid!=='undefined'?KtAndroid:null;}catch(e){return null;}}\n"
            + "  ktAutoLaunch(){const a=this.ktAndroid();try{return a?a.getAutoLaunch():false;}catch(e){return false;}}\n"
            + "  ktAutoLaunchHelp(){const a=this.ktAndroid();if(!a)return 'この きのうは Android 版のアプリでのみ つかえます。';try{return a.canDrawOverlays()?'かいじょした ときに アプリが ひらきます。':'権限が ないので かいじょ時は 通知で お知らせします。「まえに出す権限」を おすと 自動で ひらきます。';}catch(e){return '';}}\n"
            + "  ktToggleAutoLaunch(){const a=this.ktAndroid();if(!a)return;try{a.setAutoLaunch(!this.ktAutoLaunch());}catch(e){}this.sfx('select');this.setState({});}\n"
            + "  ktOpenOverlaySettings(){const a=this.ktAndroid();if(!a)return;try{a.openOverlaySettings();}catch(e){}}\n"
            + "  ktOpenNotifSettings(){const a=this.ktAndroid();if(!a)return;try{a.openNotificationSettings();}catch(e){}}\n"
            + "  ktOpenBatterySettings(){const a=this.ktAndroid();if(!a)return;try{a.openBatterySettings();}catch(e){}}");

        // View bindings resolved by the parent dashboard template.
        markup = ReplaceRequired(markup,
            "goParent:()=>this.goParent(),",
            "goParent:()=>this.goParent(), "
            + "ktAutoLaunch:this.ktAutoLaunch(), ktAutoLaunchLabel:this.ktAutoLaunch()?'ON':'OFF', "
            + "ktAutoLaunchHelp:this.ktAutoLaunchHelp(), ktToggleAutoLaunch:()=>this.ktToggleAutoLaunch(), "
            + "ktOpenOverlaySettings:()=>this.ktOpenOverlaySettings(), "
            + "ktOpenNotifSettings:()=>this.ktOpenNotifSettings(), "
            + "ktOpenBatterySettings:()=>this.ktOpenBatterySettings(),");

        // Android settings card at the top of the dashboard's bottom grid.
        markup = ReplaceRequired(markup,
            "<div style=\"margin-top:20px; display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:16px;\">",
            "<div style=\"margin-top:20px; display:grid; grid-template-columns:repeat(auto-fit,minmax(280px,1fr)); gap:16px;\">\n"
            + "        <div style=\"background:#eef4ff; border:4px solid #a8c3f0; border-radius:24px; padding:18px 22px; display:flex; flex-direction:column; gap:12px;\">\n"
            + "          <div style=\"font-size:20px; font-weight:900; color:#294d8f;\">Android せってい</div>\n"
            + "          <div style=\"font-size:15px; color:#4a5b7a; line-height:1.6;\">まいにち はじめて ロックを かいじょした ときに、このアプリを ひらきます。</div>\n"
            + "          <div style=\"display:flex; align-items:center; justify-content:space-between; gap:10px;\">\n"
            + "            <div style=\"font-size:15px; font-weight:800; color:#294d8f;\">まいにちの じどう起動</div>\n"
            + "            <button type=\"button\" role=\"button\" tabindex=\"0\" onclick=\"{{ ktToggleAutoLaunch }}\" aria-pressed=\"{{ ktAutoLaunch }}\" style=\"min-width:72px; background:#fff; color:#294d8f; border:3px solid #6f94d4; border-radius:16px; padding:8px 14px; font-size:17px; font-weight:900; cursor:pointer;\">{{ ktAutoLaunchLabel }}</button>\n"
            + "          </div>\n"
            + "          <div style=\"font-size:13px; color:#4a5b7a; line-height:1.5;\">{{ ktAutoLaunchHelp }}</div>\n"
            + "          <div style=\"display:flex; gap:8px; flex-wrap:wrap; margin-top:auto;\">\n"
            + "            <button type=\"button\" role=\"button\" tabindex=\"0\" onclick=\"{{ ktOpenOverlaySettings }}\" style=\"background:#fff; color:#294d8f; border:3px solid #a8c3f0; border-radius:14px; padding:8px 14px; font-size:14px; font-weight:900; cursor:pointer;\">まえに出す権限</button>\n"
            + "            <button type=\"button\" role=\"button\" tabindex=\"0\" onclick=\"{{ ktOpenNotifSettings }}\" style=\"background:#fff; color:#294d8f; border:3px solid #a8c3f0; border-radius:14px; padding:8px 14px; font-size:14px; font-weight:900; cursor:pointer;\">通知の権限</button>\n"
            + "            <button type=\"button\" role=\"button\" tabindex=\"0\" onclick=\"{{ ktOpenBatterySettings }}\" style=\"background:#fff; color:#294d8f; border:3px solid #a8c3f0; border-radius:14px; padding:8px 14px; font-size:14px; font-weight:900; cursor:pointer;\">電池の最適化</button>\n"
            + "          </div>\n"
            + "        </div>");

        return ReplaceRequired(markup, "</head>", BuildMobileStyle() + "\n</head>");
    }

    private static string AddClassAttribute(string markup, string openingTagPrefix)
    {
        // The prefix must end at an attribute boundary; otherwise the inserted attribute
        // would land inside a style value and corrupt the tag instead of hiding it.
        if (!openingTagPrefix.EndsWith("\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Anchor must end at an attribute boundary: {openingTagPrefix[..Math.Min(openingTagPrefix.Length, 80)]}");
        }

        return ReplaceRequired(markup, openingTagPrefix, openingTagPrefix + " class=\"kt-mobile-hide\"");
    }

    private static string ExtendClassAttribute(string markup, string openingTagPrefix)
    {
        const string classToken = "class=\"";
        var classIndex = openingTagPrefix.IndexOf(classToken, StringComparison.Ordinal);
        if (classIndex < 0)
        {
            throw new InvalidOperationException(
                $"Anchor has no class attribute to extend: {openingTagPrefix[..Math.Min(openingTagPrefix.Length, 80)]}");
        }

        var insertionPoint = classIndex + classToken.Length;
        var replacement = openingTagPrefix[..insertionPoint] + "kt-mobile-hide " + openingTagPrefix[insertionPoint..];
        return ReplaceRequired(markup, openingTagPrefix, replacement);
    }

    private static string ReplaceRequired(string source, string oldValue, string newValue)
    {
        var index = source.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Required mobile drill-only anchor was not found: {oldValue[..Math.Min(oldValue.Length, 80)]}");
        }

        if (source.IndexOf(oldValue, index + 1, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException(
                $"Required mobile drill-only anchor must occur exactly once: {oldValue[..Math.Min(oldValue.Length, 80)]}");
        }

        return source[..index] + newValue + source[(index + oldValue.Length)..];
    }

    private static string BuildMobileStyle()
    {
        return """
<style id="kt-mobile-drill-only">
  .kt-mobile-hide{display:none!important;}
  .kt-mobile-panel{width:100%;margin:0 auto;}
  .kt-mobile-panel .kt-hero-row{margin-top:0;}
  @media (max-width:700px){
    [data-screen-label="開始"]{padding:16px 16px 22px!important;}
    .kt-drill-screen{padding:14px 14px 20px;}
    .kt-drill-head{flex-wrap:wrap;gap:8px;}
    .kt-drill-head .kt-drill-quit{margin-right:auto;}
    .kt-drill-head .kt-drill-titles{order:1;flex-basis:100%;}
    .kt-drill-mode-screen{padding:16px 16px 22px;}
    .kt-drill-prompt{font-size:48px;}
    .kt-drill-hissan{font-size:48px;}
    [data-screen-label="保護者ダッシュボード"],
    [data-screen-label="緊急解除"]{padding:16px 16px 22px;}
  }
</style>
""".TrimEnd('\r', '\n');
    }
}
