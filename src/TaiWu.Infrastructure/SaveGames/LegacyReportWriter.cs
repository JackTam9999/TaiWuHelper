using System.Globalization;
using TaiWu.Domain.SaveGames;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class LegacyReportWriter
{
    private readonly List<string> _lines = [];

    public void Write(string value) => _lines.Add(value);

    public void Write(string format, params object?[] values) =>
        _lines.Add(string.Format(CultureInfo.InvariantCulture, format, values));

    public SaveGameReport Build() => new(_lines.AsReadOnly());
}
