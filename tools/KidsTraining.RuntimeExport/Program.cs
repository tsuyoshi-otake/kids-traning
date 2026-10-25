using System.Text;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.RuntimeExport;

/// <summary>
/// Builds the patched learning runtime page outside the Windows shell so other wrappers
/// (currently the Android drill-only WebView app) can ship the exact same generated
/// markup. Usage:
///   KidsTraining.RuntimeExport &lt;repository-root&gt; &lt;output.html&gt; [--mode full|drill-only]
///       [--profile-name NAME] [--pin ####]
/// </summary>
internal static class Program
{
    private const string DefaultProfileName = "キッズ";

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(
                "Usage: KidsTraining.RuntimeExport <repository-root> <output.html> " +
                "[--mode full|drill-only] [--profile-name NAME] [--pin ####]");
            return 2;
        }

        var repositoryRoot = Path.GetFullPath(args[0]);
        var outputPath = Path.GetFullPath(args[1]);
        var mode = "drill-only";
        // Non-ASCII values arrive corrupted through console argument encoding on Windows
        // (cp932 vs UTF-8), so callers pass them through the process environment instead.
        var profileName = Environment.GetEnvironmentVariable("KT_EXPORT_PROFILE_NAME");
        if (string.IsNullOrWhiteSpace(profileName)) profileName = DefaultProfileName;
        string? pinValue = Environment.GetEnvironmentVariable("KT_EXPORT_PIN");

        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--profile-name" when i + 1 < args.Length:
                    profileName = args[++i];
                    break;
                case "--pin" when i + 1 < args.Length:
                    pinValue = args[++i];
                    break;
                default:
                    Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
                    return 2;
            }
        }

        if (mode is not ("full" or "drill-only"))
        {
            Console.Error.WriteLine($"Unsupported mode: {mode}");
            return 2;
        }

        var templatePath = Path.Combine(repositoryRoot, "kids-training", "index.template.html");
        var appDefinitionPath = Path.Combine(repositoryRoot, "kids-training", "app", "learning-app.dc.html");
        if (!File.Exists(templatePath) || !File.Exists(appDefinitionPath))
        {
            Console.Error.WriteLine($"Learning sources not found under {repositoryRoot}");
            return 1;
        }

        var html = new LearningPageBuilder().Build(
            File.ReadAllText(templatePath, Encoding.UTF8),
            File.ReadAllText(appDefinitionPath, Encoding.UTF8),
            profileName,
            ParentPin.FromOrDefault(pinValue));

        var failures = GeneratedLearningRuntimeContractValidator.Validate(html, profileName);
        if (failures.Count != 0)
        {
            Console.Error.WriteLine(
                "Generated runtime contract failed: " +
                string.Join("; ", failures.Select(f => $"{f.Code}: {f.Message}")));
            return 1;
        }

        if (mode == "drill-only")
        {
            html = MobileDrillOnlyPatch.Apply(html);
            var mobileFailures = GeneratedLearningRuntimeContractValidator.Validate(html, profileName);
            if (mobileFailures.Count != 0)
            {
                Console.Error.WriteLine(
                    "Mobile drill-only transform broke the runtime contract: " +
                    string.Join("; ", mobileFailures.Select(f => $"{f.Code}: {f.Message}")));
                return 1;
            }

            foreach (var marker in new[] { "kt-mobile-hide", "kt-drill-grid", "drillBank(id)", "kt_drill_v1" })
            {
                if (!html.Contains(marker, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine($"Mobile drill-only marker missing after transform: {marker}");
                    return 1;
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, html, new UTF8Encoding(false));
        Console.WriteLine($"Wrote {mode} learning runtime ({html.Length:N0} chars) to {outputPath}");
        return 0;
    }
}
