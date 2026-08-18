using System.Collections.Immutable;

namespace TaiWu.Domain.SaveGames;

public sealed record SaveGameReport
{
    public SaveGameReport(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var copied = lines.ToImmutableArray();
        if (copied.Any(line => line is null))
        {
            throw new ArgumentException(
                "Save report lines cannot contain null.",
                nameof(lines));
        }

        Lines = copied;
    }

    public ImmutableArray<string> Lines { get; }

    public string ToLegacyText() => string.Join(Environment.NewLine, Lines);
}
