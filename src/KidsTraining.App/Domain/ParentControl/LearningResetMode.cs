namespace KidsTraining.App.Domain.ParentControl;

internal enum LearningResetMode
{
    None = 0,
    HistoryOnly = 1,
    Full = 2
}

internal static class LearningResetModeValues
{
    public const string HistoryOnly = "history";
    public const string Full = "full";

    public static string ToWireValue(this LearningResetMode mode) => mode switch
    {
        LearningResetMode.HistoryOnly => HistoryOnly,
        LearningResetMode.Full => Full,
        _ => string.Empty
    };

    public static bool TryParse(string? value, out LearningResetMode mode)
    {
        mode = value?.Trim().ToLowerInvariant() switch
        {
            HistoryOnly => LearningResetMode.HistoryOnly,
            Full => LearningResetMode.Full,
            _ => LearningResetMode.None
        };
        return mode != LearningResetMode.None;
    }
}
