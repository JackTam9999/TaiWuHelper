using System.Collections.Immutable;
using System.Text;
using TaiWu.Application.CombatSkills;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed record TaiwuLanguageCatalogReadResult(
    TaiwuLanguageCatalog Catalog,
    ImmutableArray<CombatSkillImportDiagnostic> Diagnostics);

internal sealed class TaiwuLanguageCatalog
{
    private readonly IReadOnlyDictionary<string, string> _values;

    internal TaiwuLanguageCatalog(
        IReadOnlyDictionary<string, string>? values = null)
    {
        _values = new Dictionary<string, string>(
            values ?? new Dictionary<string, string>(),
            StringComparer.Ordinal);
    }

    internal string? Find(string? key) =>
        !string.IsNullOrWhiteSpace(key)
        && _values.TryGetValue(key, out var value)
        && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    internal IEnumerable<string> Keys => _values.Keys;

    internal static async Task<TaiwuLanguageCatalogReadResult> ReadAsync(
        string path,
        string sourceIdentity,
        CancellationToken cancellationToken)
    {
        var options = new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.ReadWrite | FileShare.Delete,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        };
        await using var stream = new FileStream(path, options);
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true);

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        List<CombatSkillImportDiagnostic> diagnostics = [];
        while (await reader.ReadLineAsync(cancellationToken)
                   .ConfigureAwait(false) is { } keyLine)
        {
            var key = keyLine.TrimStart('\uFEFF');
            var value = await reader.ReadLineAsync(cancellationToken)
                .ConfigureAwait(false);
            if (value is null)
            {
                diagnostics.Add(new CombatSkillImportDiagnostic(
                    CombatSkillImportDiagnosticSeverity.Warning,
                    "LANGUAGE_VALUE_MISSING",
                    OpaqueKey(sourceIdentity, key),
                    "The language key has no following value line."));
                break;
            }

            if (string.IsNullOrWhiteSpace(key)
                || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!values.TryAdd(key, value))
            {
                diagnostics.Add(new CombatSkillImportDiagnostic(
                    CombatSkillImportDiagnosticSeverity.Warning,
                    "LANGUAGE_KEY_DUPLICATE",
                    OpaqueKey(sourceIdentity, key),
                    "The duplicate language key was ignored; the first "
                    + "value remains authoritative."));
            }
        }

        return new TaiwuLanguageCatalogReadResult(
            new TaiwuLanguageCatalog(values),
            diagnostics.ToImmutableArray());
    }

    private static string OpaqueKey(string sourceIdentity, string key)
    {
        var safeKey = key
            .Replace('\\', '_')
            .Replace('/', '_')
            .Replace("..", "_", StringComparison.Ordinal);
        return $"{sourceIdentity}:key:{safeKey}";
    }
}
