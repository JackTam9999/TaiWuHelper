namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotWarning
{
    public SnapshotWarning(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "A snapshot warning requires a code.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "A snapshot warning requires a message.",
                nameof(message));
        }

        Code = code.Trim();
        Message = message.Trim();
    }

    public string Code { get; }

    public string Message { get; }
}
