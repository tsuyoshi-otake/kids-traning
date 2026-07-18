namespace KidsTraining.App.Domain.ParentControl;

internal readonly record struct ParentPin
{
    private const string DefaultValue = "1234";

    private ParentPin(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ParentPin Default { get; } = new(DefaultValue);

    public static bool TryCreate(string? candidate, out ParentPin pin)
    {
        var normalized = candidate?.Trim();
        if (normalized is { Length: 4 } && normalized.All(static character => character is >= '0' and <= '9'))
        {
            pin = new ParentPin(normalized);
            return true;
        }

        pin = default;
        return false;
    }

    public static ParentPin FromOrDefault(string? candidate) =>
        TryCreate(candidate, out var pin) ? pin : Default;
}
