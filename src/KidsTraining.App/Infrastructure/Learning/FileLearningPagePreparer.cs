using System.Text;
using KidsTraining.App.Application.Learning;
using KidsTraining.App.Application.ParentControl;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class FileLearningPagePreparer : ILearningPagePreparer
{
    private readonly LearningPageBuilder pageBuilder;
    private readonly IParentPinProvider parentPinProvider;
    private readonly IUserProfileNameProvider profileNameProvider;

    public FileLearningPagePreparer(
        LearningPageBuilder pageBuilder,
        IParentPinProvider parentPinProvider,
        IUserProfileNameProvider profileNameProvider)
    {
        this.pageBuilder = pageBuilder;
        this.parentPinProvider = parentPinProvider;
        this.profileNameProvider = profileNameProvider;
    }

    public LearningPagePreparationResult Prepare()
    {
        try
        {
            if (!File.Exists(AppPaths.HtmlTemplatePath))
            {
                return LearningPagePreparationResult.Failed(
                    $"Learning HTML template was not found: {AppPaths.HtmlTemplatePath}");
            }

            if (!File.Exists(AppPaths.LearningAppDefinitionPath))
            {
                return LearningPagePreparationResult.Failed(
                    $"Learning app definition was not found: {AppPaths.LearningAppDefinitionPath}");
            }

            var htmlTemplate = File.ReadAllText(AppPaths.HtmlTemplatePath, Encoding.UTF8);
            var appDefinition = File.ReadAllText(AppPaths.LearningAppDefinitionPath, Encoding.UTF8);
            var runtimeHtml = pageBuilder.Build(
                htmlTemplate,
                appDefinition,
                profileNameProvider.GetProfileName(),
                parentPinProvider.GetCurrentPin());

            File.WriteAllText(AppPaths.RuntimeHtmlPath, runtimeHtml, new UTF8Encoding(false));
            File.SetLastWriteTimeUtc(AppPaths.RuntimeHtmlPath, DateTime.UtcNow);
            return LearningPagePreparationResult.Prepared(AppPaths.RuntimeHtmlPath);
        }
        catch (Exception exception)
        {
            return LearningPagePreparationResult.Failed(exception.Message);
        }
    }
}
