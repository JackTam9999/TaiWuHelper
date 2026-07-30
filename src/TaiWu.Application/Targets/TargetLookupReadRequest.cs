namespace TaiWu.Application.Targets;

public sealed record TargetLookupReadRequest
{
    public TargetLookupReadRequest(string saveFilePath)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(saveFilePath));
        }

        SaveFilePath = saveFilePath.Trim();
    }

    public string SaveFilePath { get; }
}
