using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal sealed class ParentLearningSettingsService : IParentLearningSettingsProvider
{
    private readonly IParentLearningSettingsStore store;

    public ParentLearningSettingsService(IParentLearningSettingsStore store)
    {
        this.store = store;
    }

    public LearningSessionSettings GetCurrentSettings() => store.ReadLearningSettings();

    public LearningSessionSettingsUpdateResult Update(int? questionCount, int? passLine)
    {
        var current = GetCurrentSettings();
        if (questionCount is null or < LearningSessionSettings.MinimumQuestionCount or > LearningSessionSettings.MaximumQuestionCount)
        {
            return new LearningSessionSettingsUpdateResult(
                false,
                "1回の出題数は20〜40問にしてください。",
                current);
        }

        if (passLine is null or < LearningSessionSettings.MinimumPassLine || passLine > questionCount)
        {
            return new LearningSessionSettingsUpdateResult(
                false,
                "合格点は1点以上、出題数以下にしてください。",
                current);
        }

        var settings = new LearningSessionSettings(questionCount.Value, passLine.Value);
        try
        {
            store.WriteLearningSettings(settings);
            return new LearningSessionSettingsUpdateResult(true, "学習設定を保存しました。次回の学習から反映されます。", settings);
        }
        catch
        {
            return new LearningSessionSettingsUpdateResult(false, "学習設定を保存できませんでした。", current);
        }
    }
}
