using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal sealed class ParentLearningResetService
{
    private readonly IParentLearningResetStore resetStore;
    private readonly IParentPinStore pinStore;

    public ParentLearningResetService(IParentLearningResetStore resetStore, IParentPinStore pinStore)
    {
        this.resetStore = resetStore;
        this.pinStore = pinStore;
    }

    public LearningResetMode GetPendingReset() => resetStore.ReadPendingLearningReset();

    public LearningResetResult Request(string? currentPassword, string? requestedMode)
    {
        if (!ParentPin.TryCreate(currentPassword, out var current) || current != pinStore.Read())
        {
            return LearningResetResult.Failed("いまのパスワードが違います。");
        }

        if (!LearningResetModeValues.TryParse(requestedMode, out var mode))
        {
            return LearningResetResult.Failed("リセット方法を選び直してください。");
        }

        try
        {
            resetStore.WritePendingLearningReset(mode);
            var description = mode == LearningResetMode.HistoryOnly ? "学習履歴" : "すべての学習データ";
            return new LearningResetResult(true, $"{description}のリセットを受け付けました。", mode, true);
        }
        catch
        {
            return LearningResetResult.Failed("リセットを予約できませんでした。");
        }
    }

    public bool CompleteAppliedReset(LearningResetMode appliedMode)
    {
        try
        {
            var pending = resetStore.ReadPendingLearningReset();
            if (pending != LearningResetMode.None && pending != appliedMode)
            {
                return false;
            }

            resetStore.WritePendingLearningReset(LearningResetMode.None);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
