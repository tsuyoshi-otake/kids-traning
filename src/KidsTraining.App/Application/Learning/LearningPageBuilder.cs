using KidsTraining.App.Application.Learning.Markup;
using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.Learning;

internal sealed class LearningPageBuilder
{
    private const string AppPlaceholder = "<!--__KIDS_TRAINING_APP__-->";

    public string Build(
        string htmlTemplate,
        string appDefinition,
        string profileName,
        ParentPin parentPin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(appDefinition);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        var placeholderIndex = htmlTemplate.IndexOf(AppPlaceholder, StringComparison.Ordinal);
        if (placeholderIndex < 0)
        {
            throw new InvalidOperationException($"Required learning app placeholder was not found: {AppPlaceholder}");
        }

        if (htmlTemplate.IndexOf(AppPlaceholder, placeholderIndex + AppPlaceholder.Length, StringComparison.Ordinal) >= 0)
        {
            throw new InvalidOperationException($"Learning app placeholder must occur exactly once: {AppPlaceholder}");
        }

        var assembledHtml = htmlTemplate[..placeholderIndex] +
            appDefinition +
            htmlTemplate[(placeholderIndex + AppPlaceholder.Length)..];
        return LearningMarkupPatcher.Apply(assembledHtml, profileName.Trim(), parentPin.Value);
    }
}
