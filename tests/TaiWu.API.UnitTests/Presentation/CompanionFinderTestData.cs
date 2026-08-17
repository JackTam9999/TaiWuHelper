using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

internal static class CompanionFinderTestData
{
    internal const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    internal static async Task<CompanionFinderResult> ResultAsync(
        bool partialSnapshot = false,
        bool comprehensiveObjective = false)
    {
        var snapshot = Snapshot();
        var reader = Substitute.For<ICompanionCandidateSnapshotReader>();
        reader.ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(partialSnapshot
                ? CompanionCandidateSnapshotReadResult.Partial(snapshot)
                : CompanionCandidateSnapshotReadResult.Complete(snapshot));
        var identity = CatalogueIdentity();
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(identity, []));
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                identity,
                0,
                DateTimeOffset.Parse("2026-08-17T12:00:00Z")));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        return await new FindCompanionCandidates(reader, source, repository)
            .ExecuteAsync(
                new CompanionFinderRequest(
                    comprehensiveObjective
                        ? "COMPREHENSIVE_BASE_CAPABILITY"
                        : "MARTIAL_DISCIPLINE_APTITUDE",
                    "1",
                    comprehensiveObjective
                        ? CandidateDisciplineDomain.Capability
                        : CandidateDisciplineDomain.Martial,
                    0),
                TestContext.Current.CancellationToken);
    }

    internal static CompanionDisciplineDisplayResult Disciplines(
        CompanionDisciplineDisplayStatus status =
            CompanionDisciplineDisplayStatus.Complete)
    {
        var values = new List<CompanionDisciplineDisplayName>();
        AddDisciplines(
            values,
            CandidateDisciplineDomain.Martial,
            14,
            "武學類別",
            "Martial discipline");
        AddDisciplines(
            values,
            CandidateDisciplineDomain.LifeSkill,
            16,
            "技藝類別",
            "Life-skill discipline");
        return new CompanionDisciplineDisplayResult(status, values);
    }

    private static CompanionCandidateSnapshot Snapshot()
    {
        var profiles = new[]
        {
            Profile(31001, CandidateUniverseState.Eligible, Score(90)),
            Profile(31002, CandidateUniverseState.Eligible, Score(75)),
            Profile(31003, CandidateUniverseState.Eligible, Score(75)),
            Profile(31004, CandidateUniverseState.Ineligible, Score(60)),
            Profile(31005, CandidateUniverseState.Eligible),
            Profile(
                31006,
                CandidateUniverseState.Eligible,
                CandidateProfileFact.Incomplete(
                    ScoreField(),
                    new CandidateUnavailableReason(
                        "SCORE_MISSING",
                        "The saved base value is missing."),
                    [])),
            Profile(
                31007,
                CandidateUniverseState.Eligible,
                CandidateProfileFact.Unsupported(
                    ScoreField(),
                    new CandidateUnavailableReason(
                        "SCORE_UNSUPPORTED",
                        "The source cannot provide this value."),
                    [])),
            Profile(
                31008,
                CandidateUniverseState.Eligible,
                CandidateProfileFact.Stale(
                    ScoreField(),
                    CandidateFactValue.Int16(71),
                    Provenance(Sha),
                    new CandidateUnavailableReason(
                        "SCORE_STALE",
                        "The saved value is no longer current."),
                    [])),
            Profile(
                31009,
                CandidateUniverseState.Eligible,
                ConflictingScore())
        };
        var displays = profiles.Select((profile, index) =>
            new CompanionCandidateDisplay(
                profile.Identity,
                $"範例人物{ChineseOrdinal(index)}",
                $"Synthetic Person {Convert.ToChar('A' + index)}",
                $"範例地點{ChineseOrdinal(index)}",
                $"Synthetic Place {Convert.ToChar('A' + index)}"));
        return new CompanionCandidateSnapshot(
            DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            Versions(),
            profiles,
            omissions: [],
            warnings: [],
            diagnostics: [],
            displays);
    }

    private static CandidateProfile Profile(
        int characterId,
        CandidateUniverseState universeState,
        params CandidateProfileFact[] scoreFacts) => new(
        new CandidateIdentity(characterId),
        universeState,
        Versions(),
        scoreFacts
            .Concat(MembershipFacts())
            .Concat(CapabilityFacts(characterId)),
        diagnostics: []);

    private static CandidateProfileFact Score(short value) =>
        CandidateProfileFact.Confirmed(
            ScoreField(),
            CandidateFactValue.Int16(value),
            Provenance(Sha),
            [new CandidateEvidenceReference(
                "E6-SYNTHETIC-SCORE",
                Provenance(Sha))]);

    private static CandidateProfileFact ConflictingScore()
    {
        var first = Provenance(Sha);
        var secondRevision = new string('B', 64);
        var second = Provenance(secondRevision);
        return CandidateProfileFact.Conflicting(
            ScoreField(),
            [
                new CandidateConflictValue(
                    CandidateFactValue.Int16(70),
                    first,
                    [new CandidateEvidenceReference(
                        "E6-SYNTHETIC-CONFLICT-A",
                        first)]),
                new CandidateConflictValue(
                    CandidateFactValue.Int16(80),
                    second,
                    [new CandidateEvidenceReference(
                        "E6-SYNTHETIC-CONFLICT-B",
                        second)])
            ],
            new CandidateConflictDecision(
                CandidateConflictDecisionKind.Unresolved,
                "NO_SAFE_PRECEDENCE"),
            evidence: []);
    }

    private static IEnumerable<CandidateProfileFact> MembershipFacts() =>
    [
        SetFact(CandidateProfileField.LearnedMartialSkillIdentities),
        SetFact(CandidateProfileField.EquippedMartialSkillIdentities),
        SetFact(CandidateProfileField.LearnedLifeSkillIdentities)
    ];

    private static IEnumerable<CandidateProfileFact> CapabilityFacts(
        int characterId)
    {
        var offset = characterId % 10;
        foreach (var attribute in Enum.GetValues<CandidateMainAttribute>())
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute),
                checked((short)(50 + (int)attribute + offset)));
        }

        for (short type = 1; type < 14; type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMartialQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.Martial,
                        type)),
                checked((short)(40 + type + offset)));
        }

        for (short type = 0; type < 16; type++)
        {
            yield return ScalarFact(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseLifeSkillQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.LifeSkill,
                        type)),
                checked((short)(30 + type + offset)));
        }
    }

    private static CandidateProfileFact ScalarFact(
        CandidateProfileFieldIdentity field,
        short value) => CandidateProfileFact.Confirmed(
        field,
        CandidateFactValue.Int16(value),
        Provenance(Sha),
        evidence: []);

    private static CandidateProfileFact SetFact(
        CandidateProfileField field) => CandidateProfileFact.Confirmed(
        new CandidateProfileFieldIdentity(field),
        CandidateFactValue.Int32Set([]),
        Provenance(Sha),
        evidence: []);

    private static CandidateProfileFieldIdentity ScoreField() => new(
        CandidateProfileField.BaseMartialQualification,
        new CandidateDisciplineIdentity(
            CandidateDisciplineDomain.Martial,
            0));

    private static CandidateFactProvenance Provenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        revision);

    private static CandidateProfileSourceVersions Versions() => new(
        Sha,
        VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        "1",
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion);

    private static CombatSkillCatalogueSourceIdentity CatalogueIdentity() =>
        new(
            VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
            3,
            Sha,
            Sha,
            Sha);

    private static void AddDisciplines(
        ICollection<CompanionDisciplineDisplayName> destination,
        CandidateDisciplineDomain domain,
        short count,
        string chinesePrefix,
        string englishPrefix)
    {
        for (short type = 0; type < count; type++)
        {
            destination.Add(new CompanionDisciplineDisplayName(
                new CandidateDisciplineIdentity(domain, type),
                $"{chinesePrefix}{type + 1}",
                $"{englishPrefix} {type + 1}"));
        }
    }

    private static string ChineseOrdinal(int index) => index switch
    {
        0 => "甲",
        1 => "乙",
        2 => "丙",
        3 => "丁",
        4 => "戊",
        5 => "己",
        6 => "庚",
        7 => "辛",
        8 => "壬",
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
