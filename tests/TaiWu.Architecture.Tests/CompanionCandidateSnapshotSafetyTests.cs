using System.Reflection;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.GameData;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWu.Infrastructure;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class CompanionCandidateSnapshotSafetyTests
{
    private static readonly string[] ForbiddenAdapterTokens =
    [
        "File.Write",
        "File.OpenWrite",
        "File.Create",
        "File.Delete",
        "Directory.CreateDirectory",
        "FileStream",
        "SqliteConnection",
        "HttpClient",
        "Socket",
        "Process.Start",
        "Process.GetProcess",
        "DllImport",
        "SendInput",
        "Harmony",
        ".Save("
    ];

    [Fact]
    public void Candidate_snapshot_port_is_path_free_immutable_and_read_only()
    {
        var port = typeof(ICompanionCandidateSnapshotReader);
        Assert.True(typeof(IReadOnlyGameDataSource).IsAssignableFrom(port));
        var method = Assert.Single(port.GetMethods());
        Assert.Equal("ReadAsync", method.Name);
        Assert.Contains(
            method.GetParameters(),
            parameter => parameter.ParameterType == typeof(CancellationToken));
        Assert.DoesNotContain(
            method.GetParameters(),
            parameter => parameter.ParameterType == typeof(string));

        var contractTypes = typeof(CompanionCandidateSnapshot).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "TaiWu.Application.CompanionCandidates")
            .ToArray();
        Assert.NotEmpty(contractTypes);
        Assert.All(
            contractTypes.SelectMany(type => type.GetProperties()),
            property => Assert.False(property.CanWrite));
        Assert.DoesNotContain(
            contractTypes.SelectMany(type => type.GetProperties()),
            property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            contractTypes.SelectMany(PublicSignatureTypes),
            type => type.Namespace?.StartsWith("GameData", StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith("TaiWu.Infrastructure", StringComparison.Ordinal) == true
                || type == typeof(FileInfo)
                || type == typeof(DirectoryInfo)
                || type == typeof(Stream)
                || type == typeof(System.Diagnostics.Process));
    }

    [Fact]
    public void Candidate_snapshot_adapter_has_one_archive_call_and_no_mutation_path()
    {
        var root = FindRepositoryRoot();
        var readerPath = Path.Combine(
            root,
            "src",
            "TaiWu.Infrastructure",
            "SaveGames",
            "TaiwuCompanionCandidateSnapshotReader.cs");
        var mappingPath = Path.Combine(
            root,
            "src",
            "TaiWu.Infrastructure",
            "SaveGames",
            "CompanionCandidateSnapshotMapping.cs");
        var reader = File.ReadAllText(readerPath);
        var combined = reader + Environment.NewLine + File.ReadAllText(mappingPath);

        Assert.Equal(
            1,
            CountOccurrences(reader, "readSession.ReadAsync("));
        Assert.Contains("saveFilePathProvider.Resolve()", reader);
        Assert.DoesNotContain("request.Save", reader);
        Assert.DoesNotContain("request.Path", reader);
        Assert.DoesNotContain(
            ForbiddenAdapterTokens,
            token => combined.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public void Candidate_snapshot_public_contract_exposes_no_archive_or_game_types()
    {
        var publicTypes = typeof(CompanionCandidateSnapshot).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "TaiWu.Application.CompanionCandidates")
            .SelectMany(PublicSignatureTypes)
            .Distinct()
            .ToArray();

        Assert.DoesNotContain(
            publicTypes,
            type => type.Namespace?.StartsWith("GameData", StringComparison.Ordinal) == true
                || type.Name.Contains("Archive", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeof(CandidateProfile), publicTypes);
        Assert.Contains(typeof(CandidateProfileSourceVersions), publicTypes);
    }

    [Fact]
    public void Candidate_enrichment_cannot_open_character_progress_in_an_n_plus_one_loop()
    {
        var parameters = Assert.Single(
                typeof(EnrichCompanionCandidateProfiles).GetConstructors())
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        Assert.Equal(
            [typeof(ICombatSkillDefinitionSource),
                typeof(ICombatSkillCatalogueRepository)],
            parameters);
        Assert.DoesNotContain(
            typeof(ICharacterCombatSkillProgressReader),
            parameters);

        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "TaiWu.Application",
            "CompanionCandidates",
            "EnrichCompanionCandidateProfiles.cs"));
        Assert.DoesNotContain("ICharacterCombatSkillProgressReader", source);
        Assert.DoesNotContain("ReadCharacterCombatSkillAtlas", source);
        Assert.Equal(1, CountOccurrences(source, "catalogueRepository.QueryAsync("));
    }

    [Fact]
    public void Candidate_enrichment_is_a_view_over_immutable_profiles()
    {
        var enrichmentTypes = typeof(CompanionCandidateEnrichmentResult)
            .Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "TaiWu.Application.CompanionCandidates"
                && type.Name.Contains("Enrichment", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(enrichmentTypes);
        Assert.All(
            enrichmentTypes.SelectMany(type => type.GetProperties()),
            property => Assert.False(property.CanWrite));
        Assert.DoesNotContain(
            enrichmentTypes.SelectMany(type => type.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)),
            method => method.Name.StartsWith("Set", StringComparison.Ordinal)
                || method.Name.StartsWith("Update", StringComparison.Ordinal)
                || method.Name.StartsWith("Replace", StringComparison.Ordinal));
        Assert.Equal(
            typeof(CandidateProfile),
            typeof(CompanionCandidateEnrichment)
                .GetProperty(nameof(CompanionCandidateEnrichment.Profile))!
                .PropertyType);
    }

    [Fact]
    public void Companion_finder_request_is_bounded_path_free_and_information_only()
    {
        var properties = typeof(CompanionFinderRequest).GetProperties();
        Assert.All(properties, property => Assert.False(property.CanWrite));
        Assert.DoesNotContain(
            properties,
            property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Definition", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Expression", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Command", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            [
                typeof(string),
                typeof(string),
                typeof(CandidateDisciplineDomain),
                typeof(short),
                typeof(CompanionRoleShortlistFilter),
                typeof(int?),
                typeof(int?)
            ],
            Assert.Single(typeof(CompanionFinderRequest).GetConstructors())
                .GetParameters()
                .Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void Companion_finder_has_one_snapshot_path_and_no_second_evaluation_path()
    {
        var constructorTypes = Assert.Single(typeof(FindCompanionCandidates).GetConstructors())
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        Assert.Equal(
            [
                typeof(ICompanionCandidateSnapshotReader),
                typeof(ICombatSkillDefinitionSource),
                typeof(ICombatSkillCatalogueRepository)
            ],
            constructorTypes);

        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "TaiWu.Application",
            "CompanionCandidates",
            "FindCompanionCandidates.cs"));
        Assert.Equal(1, CountOccurrences(source, "snapshotReader.ReadAsync("));
        Assert.Equal(1, CountOccurrences(source, "CompanionRoleShortlistBuilder.EvaluateAndRank("));
        Assert.Equal(1, CountOccurrences(source, "CompanionRoleShortlistFactory.Create("));
        Assert.Equal(1, CountOccurrences(source, "CompanionRoleComparisonBuilder.Compare("));
        Assert.Contains("cancellationToken", source);
        Assert.DoesNotContain("CompanionRoleEvaluator.Evaluate(", source);
        Assert.DoesNotContain("File.", source);
        Assert.DoesNotContain("Process.", source);
    }

    private static IEnumerable<Type> PublicSignatureTypes(Type type)
    {
        yield return type;
        foreach (var property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.Public))
        {
            yield return Unwrap(property.PropertyType);
        }

        foreach (var constructor in type.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return Unwrap(parameter.ParameterType);
            }
        }

        foreach (var method in type.GetMethods(
                     BindingFlags.Instance
                     | BindingFlags.Static
                     | BindingFlags.Public
                     | BindingFlags.DeclaredOnly))
        {
            yield return Unwrap(method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                yield return Unwrap(parameter.ParameterType);
            }
        }
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Last();
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
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
