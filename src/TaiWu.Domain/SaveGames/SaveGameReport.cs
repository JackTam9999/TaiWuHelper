namespace TaiWu.Domain.SaveGames;

public sealed record SaveGameReport(IReadOnlyList<string> Lines)
{
    public string ToLegacyText() => string.Join(Environment.NewLine, Lines);
}
