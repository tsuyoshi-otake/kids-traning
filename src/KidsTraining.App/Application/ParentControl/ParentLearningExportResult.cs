namespace KidsTraining.App.Application.ParentControl;

internal sealed record ParentLearningExportResult(
    bool Success,
    string Message,
    string JsonPayload)
{
    public static ParentLearningExportResult Failed(string message) =>
        new(false, message, string.Empty);

    public static ParentLearningExportResult Succeeded(string jsonPayload) =>
        new(true, "Learning history JSON is ready.", jsonPayload);
}
