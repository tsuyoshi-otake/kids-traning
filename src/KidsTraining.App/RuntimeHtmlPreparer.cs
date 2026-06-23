using System.Text;
using System.Text.Json;

namespace KidsTraining.App;

internal static class RuntimeHtmlPreparer
{
    public const string EmergencyPin = "1234";
    private const string TemplateOpenTag = "<script type=\"__bundler/template\">";
    private const string TemplateCloseTag = "</script>";

    public static string PrimaryProfileName
    {
        get
        {
            var userName = Environment.UserName;
            return string.IsNullOrWhiteSpace(userName) ? "User" : userName.Trim();
        }
    }

    public static string Prepare()
    {
        if (!File.Exists(AppPaths.HtmlPath))
        {
            throw new FileNotFoundException("Learning HTML was not found.", AppPaths.HtmlPath);
        }

        var html = File.ReadAllText(AppPaths.HtmlPath, Encoding.UTF8);
        html = PatchBundledTemplate(html, PrimaryProfileName);

        File.WriteAllText(AppPaths.RuntimeHtmlPath, html, new UTF8Encoding(false));
        File.SetLastWriteTimeUtc(AppPaths.RuntimeHtmlPath, DateTime.UtcNow);
        return AppPaths.RuntimeHtmlPath;
    }

    public static string? ExtractBundledTemplate(string html)
    {
        if (!TryFindBundledTemplate(html, out var contentStart, out var contentEnd))
        {
            return null;
        }

        var encodedTemplate = html[contentStart..contentEnd].Trim();
        if (string.IsNullOrWhiteSpace(encodedTemplate))
        {
            return null;
        }

        return JsonSerializer.Deserialize<string>(encodedTemplate);
    }

    private static string PatchBundledTemplate(string html, string profileName)
    {
        if (!TryFindBundledTemplate(html, out var contentStart, out var contentEnd))
        {
            return PatchLearningMarkup(html, profileName);
        }

        var template = ExtractBundledTemplate(html)
            ?? throw new InvalidOperationException("Bundled learning template could not be decoded.");
        var patchedTemplate = PatchLearningMarkup(template, profileName);
        var encodedTemplate = JsonSerializer.Serialize(patchedTemplate);

        return html[..contentStart] + Environment.NewLine + encodedTemplate + Environment.NewLine + html[contentEnd..];
    }

    private static bool TryFindBundledTemplate(string html, out int contentStart, out int contentEnd)
    {
        contentStart = -1;
        contentEnd = -1;

        var openStart = html.IndexOf(TemplateOpenTag, StringComparison.Ordinal);
        if (openStart < 0)
        {
            return false;
        }

        contentStart = openStart + TemplateOpenTag.Length;
        contentEnd = html.IndexOf(TemplateCloseTag, contentStart, StringComparison.Ordinal);
        return contentEnd >= 0;
    }

    private static string PatchLearningMarkup(string markup, string profileName)
    {
        markup = markup.Replace("screen:'profile', profileIdx:0,", "screen:'start', profileIdx:0,", StringComparison.Ordinal);
        markup = markup.Replace(
            "unlockPC(){this.sfx('unlock');this.setState({screen:'profile',session:null,combo:0,pin:'',emergencyDone:false});}",
            "unlockPC(){this.sfx('unlock');this.setState({screen:'start',session:null,combo:0,pin:'',emergencyDone:false});}",
            StringComparison.Ordinal);

        return ReplaceBundledProfiles(markup, profileName);
    }

    private static string ReplaceBundledProfiles(string html, string profileName)
    {
        const string startToken = "profiles:[\n";
        const string endToken = "\n    session:null";

        var start = html.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            return html;
        }

        var end = html.IndexOf(endToken, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return html;
        }

        var escapedName = JsonSerializer.Serialize(profileName);
        var replacement =
            "profiles:[\n" +
            $"      {{name:{escapedName},grade:1,color:'#4ad991',streak:0,stars:0,mastery:{{add:.5,sub:.5,mul:.5,clock:.5,kokugo:.5,hissan:.5}}}},\n" +
            "    ],";

        return html[..start] + replacement + html[end..];
    }
}
