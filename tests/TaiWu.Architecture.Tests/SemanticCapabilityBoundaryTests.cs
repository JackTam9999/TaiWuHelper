using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class SemanticCapabilityBoundaryTests
{
    private static readonly HashSet<string> FileWriteMethods =
    [
        "AppendAllLines",
        "AppendAllLinesAsync",
        "AppendAllText",
        "AppendAllTextAsync",
        "OpenWrite",
        "WriteAllBytes",
        "WriteAllBytesAsync",
        "WriteAllLines",
        "WriteAllLinesAsync",
        "WriteAllText",
        "WriteAllTextAsync"
    ];

    private static readonly HashSet<string> DestructiveFileMethods =
    [
        "Copy",
        "Delete",
        "Move",
        "Replace"
    ];

    private static readonly HashSet<string> NativeGameControlMethods =
    [
        "CallNextHookEx",
        "CreateRemoteThread",
        "keybd_event",
        "mouse_event",
        "NtCreateThreadEx",
        "OpenProcess",
        "QueueUserAPC",
        "ReadProcessMemory",
        "SendInput",
        "SetWindowsHookEx",
        "UnhookWindowsHookEx",
        "VirtualAllocEx",
        "VirtualProtectEx",
        "WriteProcessMemory"
    ];

    private static readonly HashSet<string> NetworkTypes =
    [
        "System.Net.Http.HttpClient",
        "System.Net.Http.HttpMessageInvoker",
        "System.Net.Sockets.Socket",
        "System.Net.Sockets.TcpClient",
        "System.Net.Sockets.UdpClient",
        "System.Net.WebClient",
        "System.Net.WebRequest",
        "System.Net.WebSockets.ClientWebSocket"
    ];

    [Fact]
    public void Production_source_has_no_semantically_resolved_high_risk_calls()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sources = ProductionSources(repositoryRoot)
            .ToDictionary(
                file => file,
                File.ReadAllText,
                StringComparer.OrdinalIgnoreCase);

        var violations = FindViolations(sources, repositoryRoot);

        Assert.True(
            violations.Count == 0,
            "Semantically resolved high-risk capabilities were found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Semantic_scan_detects_aliases_and_fully_qualified_calls()
    {
        const string source = """
            using IO = System.IO.File;
            using Net = System.Net.Http.HttpClient;

            internal sealed class CapabilityProbe
            {
                internal async System.Threading.Tasks.Task RunAsync()
                {
                    IO.Delete("save.tw");
                    System.Diagnostics.Process.Start("game.exe");
                    using var client = new Net();
                    await client.GetAsync("https://example.invalid");
                }
            }
            """;
        var root = Path.GetFullPath(Path.DirectorySeparatorChar.ToString());
        var file = Path.Combine(root, "TaiWuAPI", "CapabilityProbe.cs");

        var violations = FindViolations(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [file] = source
            },
            root);

        Assert.Contains(violations, value => value.Contains("file delete"));
        Assert.Contains(violations, value => value.Contains("process control"));
        Assert.Contains(violations, value => value.Contains("network access"));
    }

    private static IReadOnlyList<string> FindViolations(
        IReadOnlyDictionary<string, string> sources,
        string repositoryRoot)
    {
        var trees = sources
            .Select(pair => CSharpSyntaxTree.ParseText(
                pair.Value,
                new CSharpParseOptions(LanguageVersion.Preview),
                path: pair.Key))
            .ToArray();
        var parseErrors = trees
            .SelectMany(tree => tree.GetDiagnostics())
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Take(10)
            .ToArray();
        if (parseErrors.Length > 0)
        {
            throw new InvalidOperationException(
                "The semantic capability scan could not parse production "
                + "source:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, parseErrors));
        }

        var compilation = CSharpCompilation.Create(
            "TaiWu.Architecture.SemanticScan",
            trees,
            PlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var violations = new List<string>();

        foreach (var tree in trees)
        {
            var relativePath = Path.GetRelativePath(
                repositoryRoot,
                tree.FilePath);
            var model = compilation.GetSemanticModel(
                tree,
                ignoreAccessibility: true);
            foreach (var invocation in tree.GetRoot()
                         .DescendantNodes()
                         .OfType<InvocationExpressionSyntax>())
            {
                var symbol = ResolveMethod(model, invocation);
                if (symbol is null
                    || DescribeViolation(relativePath, symbol) is not
                        { } description)
                {
                    continue;
                }

                var line = tree.GetLineSpan(invocation.Span)
                    .StartLinePosition.Line + 1;
                violations.Add(
                    $"{relativePath}:{line}: {description} "
                    + $"({symbol.ContainingType.ToDisplayString()}."
                    + $"{symbol.Name})");
            }
        }

        return violations;
    }

    private static IMethodSymbol? ResolveMethod(
        SemanticModel model,
        InvocationExpressionSyntax invocation)
    {
        var symbolInfo = model.GetSymbolInfo(invocation);
        return symbolInfo.Symbol as IMethodSymbol
               ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>()
                   .FirstOrDefault();
    }

    private static string? DescribeViolation(
        string relativePath,
        IMethodSymbol method)
    {
        var typeName = method.ContainingType.ToDisplayString();
        var isPresentation = relativePath.StartsWith(
            "TaiWuAPI" + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
        var isSaveAdapter = relativePath.StartsWith(
            Path.Combine("src", "TaiWu.Infrastructure", "SaveGames")
            + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

        if (isPresentation
            && typeName == "System.IO.File"
            && FileWriteMethods.Contains(method.Name))
        {
            return "file write";
        }

        if ((isPresentation || isSaveAdapter)
            && typeName == "System.IO.File"
            && DestructiveFileMethods.Contains(method.Name))
        {
            return $"file {method.Name.ToLowerInvariant()}";
        }

        if (isPresentation
            && typeName == "System.IO.Directory"
            && method.Name == "CreateDirectory")
        {
            return "directory write";
        }

        if ((isPresentation || isSaveAdapter)
            && typeName == "System.IO.Directory"
            && method.Name is "Delete" or "Move")
        {
            return $"directory {method.Name.ToLowerInvariant()}";
        }

        if (typeName == "System.Diagnostics.Process"
            && (method.Name is "Start" or "Kill"
                || method.Name.StartsWith(
                    "GetProcess",
                    StringComparison.Ordinal)))
        {
            return "process control";
        }

        if (NativeGameControlMethods.Contains(method.Name))
        {
            return "native game control";
        }

        if (typeName == "HarmonyLib.Harmony"
            && method.Name is "Patch" or "PatchAll")
        {
            return "runtime patching";
        }

        return NetworkTypes.Contains(typeName)
            ? "network access"
            : null;
    }

    private static IEnumerable<string> ProductionSources(string repositoryRoot)
    {
        foreach (var root in new[]
                 {
                     Path.Combine(repositoryRoot, "src"),
                     Path.Combine(repositoryRoot, "TaiWuAPI")
                 })
        {
            foreach (var file in Directory.EnumerateFiles(
                         root,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                var segments = file.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                if (!segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
                    && !segments.Contains(
                        "obj",
                        StringComparer.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        var trustedPlatformAssemblies =
            (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException(
                "Trusted platform assemblies are unavailable.");
        return trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TaiWu.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root containing TaiWu.slnx.");
    }
}
