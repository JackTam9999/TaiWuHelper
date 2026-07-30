namespace TaiWu.Application.Targets;

public sealed record TargetLookupWarning
{
    public TargetLookupWarning(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "A target lookup warning requires a code.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A target lookup warning requires a message.",
                nameof(message));
        }

        Code = code.Trim();
        Message = message.Trim();
    }

    public string Code { get; }

    public string Message { get; }
}
