using TaiWu.Domain.CompanionCandidates;
using Xunit;

namespace TaiWu.Domain.UnitTests.CompanionCandidates;

public sealed class CandidateProfileContractsTests
{
    [Fact]
    public void Candidate_identity_is_stable_numeric_identity_without_display_text()
    {
        var identity = new CandidateIdentity(42);

        Assert.Equal(42, identity.CharacterId);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CandidateIdentity(0));
        Assert.DoesNotContain(
            typeof(CandidateIdentity).GetProperties(),
            property => property.Name.Contains("Name", StringComparison.Ordinal));
    }

    [Fact]
    public void Field_identity_enforces_discipline_domain_and_typed_value()
    {
        var martial = MartialField(type: 3);
        var life = new CandidateDisciplineIdentity(
            CandidateDisciplineDomain.LifeSkill,
            3);

        Assert.Equal(CandidateDisciplineDomain.Martial, martial.Discipline!.Domain);
        Assert.Throws<ArgumentException>(() => new CandidateProfileFieldIdentity(
            CandidateProfileField.BaseMartialQualification,
            life));
        Assert.Throws<ArgumentException>(() => new CandidateProfileFieldIdentity(
            CandidateProfileField.LivingState,
            martial.Discipline));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.Martial, -1));
        Assert.Throws<ArgumentException>(() => CandidateProfileFact.Confirmed(
            martial,
            CandidateFactValue.Boolean(true),
            SaveProvenance(),
            []));
    }

    [Fact]
    public void Confirmed_zero_is_a_value_and_missing_evidence_is_not_zero()
    {
        var confirmed = CandidateProfileFact.Confirmed(
            MartialField(),
            CandidateFactValue.Int16(0),
            SaveProvenance(),
            [Evidence()]);
        var incomplete = CandidateProfileFact.Incomplete(
            MartialField(),
            Reason("MISSING_FIELD"),
            [Evidence()]);

        Assert.Equal(CandidateEvidenceState.Confirmed, confirmed.State);
        Assert.Equal(0, confirmed.Value!.Int16Value);
        Assert.Null(confirmed.UnavailableReason);
        Assert.Equal(CandidateEvidenceState.Incomplete, incomplete.State);
        Assert.Null(incomplete.Value);
        Assert.Equal("MISSING_FIELD", incomplete.UnavailableReason!.Code);
    }

    [Fact]
    public void Unsupported_and_stale_evidence_remain_explicitly_distinct()
    {
        var unsupported = CandidateProfileFact.Unsupported(
            MartialField(),
            Reason("RUNTIME_HOOK_UNAVAILABLE"),
            []);
        var stale = CandidateProfileFact.Stale(
            MartialField(),
            CandidateFactValue.Int16(71),
            SaveProvenance(revision: new string('B', 64)),
            Reason("SAVE_REVISION_CHANGED"),
            [Evidence(revision: new string('B', 64))]);

        Assert.Equal(CandidateEvidenceState.Unsupported, unsupported.State);
        Assert.Null(unsupported.Value);
        Assert.Equal(CandidateEvidenceState.Stale, stale.State);
        Assert.Equal(71, stale.Value!.Int16Value);
        Assert.NotNull(stale.Provenance);
        Assert.NotNull(stale.UnavailableReason);
    }

    [Fact]
    public void Conflict_retains_every_value_and_source_precedence_decision()
    {
        var lower = SaveProvenance("CHARACTER_BUFFER", revision: Sha);
        var higher = SaveProvenance("ROSTER_PROJECTION", revision: Sha);
        var decision = new CandidateConflictDecision(
            CandidateConflictDecisionKind.SelectedBySourcePrecedence,
            "ROSTER_SOURCE_PRECEDENCE",
            higher);
        var fact = CandidateProfileFact.Conflicting(
            new CandidateProfileFieldIdentity(CandidateProfileField.LivingState),
            [
                Conflict(CandidateFactValue.Boolean(false), lower, "E6-SAVE-LOW"),
                Conflict(CandidateFactValue.Boolean(true), higher, "E6-SAVE-HIGH")
            ],
            decision,
            []);

        Assert.Equal(CandidateEvidenceState.Conflicting, fact.State);
        Assert.Equal(2, fact.Conflicts.Length);
        Assert.Contains(fact.Conflicts, item => !item.Value.BooleanValue);
        Assert.Contains(fact.Conflicts, item => item.Value.BooleanValue);
        Assert.Equal(higher, fact.ConflictDecision!.SelectedProvenance);
        Assert.Null(fact.Value);
    }

    [Fact]
    public void Conflict_rejects_too_few_duplicate_or_unretained_candidates()
    {
        var source = SaveProvenance();
        var candidate = Conflict(CandidateFactValue.Boolean(true), source, "E6-SAVE-001");
        var field = new CandidateProfileFieldIdentity(CandidateProfileField.LivingState);
        var unresolved = new CandidateConflictDecision(
            CandidateConflictDecisionKind.Unresolved,
            "NO_SAFE_PRECEDENCE");

        Assert.Throws<ArgumentException>(() => CandidateProfileFact.Conflicting(
            field,
            [candidate],
            unresolved,
            []));
        Assert.Throws<ArgumentException>(() => CandidateProfileFact.Conflicting(
            field,
            [candidate, candidate],
            unresolved,
            []));
        Assert.Throws<ArgumentException>(() => CandidateProfileFact.Conflicting(
            field,
            [
                candidate,
                Conflict(
                    CandidateFactValue.Boolean(false),
                    SaveProvenance("DOMAIN_CHECK"),
                    "E6-SAVE-002")
            ],
            new CandidateConflictDecision(
                CandidateConflictDecisionKind.SelectedBySourcePrecedence,
                "EXTERNAL_SELECTION",
                SaveProvenance("NOT_RETAINED")),
            []));
    }

    [Fact]
    public void Identity_sets_copy_sort_and_reject_duplicates_or_negative_values()
    {
        var mutable = new List<int> { 9, 2, 5 };
        var value = CandidateFactValue.Int32Set(mutable);
        mutable.Clear();

        Assert.Equal([2, 5, 9], value.Identities);
        Assert.Throws<ArgumentException>(() => CandidateFactValue.Int32Set([2, 2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => CandidateFactValue.Int32Set([-1]));
        Assert.Throws<InvalidOperationException>(() => value.Int32Value);
    }

    [Fact]
    public void Profile_allows_empty_facts_and_has_explicit_universe_state()
    {
        var profile = new CandidateProfile(
            new CandidateIdentity(42),
            CandidateUniverseState.Incomplete,
            Versions(),
            [],
            []);

        Assert.Equal(CandidateUniverseState.Incomplete, profile.UniverseState);
        Assert.Empty(profile.Facts);
        Assert.Empty(profile.Diagnostics);
        Assert.Matches("^[0-9A-F]{64}$", profile.Fingerprint);
    }

    [Fact]
    public void Profile_copies_sorts_and_rejects_duplicate_facts()
    {
        var living = Confirmed(
            new CandidateProfileFieldIdentity(CandidateProfileField.LivingState),
            CandidateFactValue.Boolean(true));
        var roster = Confirmed(
            new CandidateProfileFieldIdentity(CandidateProfileField.RosterMembership),
            CandidateFactValue.Boolean(true));
        var facts = new List<CandidateProfileFact> { living, roster };
        var profile = Profile(facts: facts);
        facts.Clear();

        Assert.Equal(
            [CandidateProfileField.RosterMembership, CandidateProfileField.LivingState],
            profile.Facts.Select(item => item.Identity.Field));
        Assert.Same(living, profile.FindFact(living.Identity));
        Assert.Throws<ArgumentException>(() => Profile(facts: [living, living]));
    }

    [Fact]
    public void Profile_rejects_null_entries_invalid_enums_and_blank_stable_ids()
    {
        Assert.Throws<ArgumentException>(() => Profile(
            facts: new CandidateProfileFact[] { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CandidateProfile(
            new CandidateIdentity(42),
            (CandidateUniverseState)99,
            Versions(),
            [],
            []));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CandidateProfileFieldIdentity((CandidateProfileField)99));
        Assert.Throws<ArgumentException>(() => new CandidateFactProvenance(
            CandidateEvidenceSourceKind.ConfiguredSave,
            " ",
            "1",
            Sha));
        Assert.Throws<ArgumentException>(() => new CandidateFactProvenance(
            CandidateEvidenceSourceKind.ConfiguredSave,
            @"C:\local\save.sav",
            "1",
            Sha));
    }

    [Fact]
    public void Evidence_and_diagnostics_are_immutable_sorted_and_unique()
    {
        var first = Evidence("E6-SAVE-001", source: "ROSTER");
        var second = Evidence("E6-SAVE-002", source: "CHARACTER");
        var evidence = new List<CandidateEvidenceReference> { second, first };
        var diagnostic = new CandidateProfileDiagnostic(
            "MEMBERSHIP_CONFIRMED",
            CandidateProfileDiagnosticSeverity.Information,
            "All saved membership checks agree.",
            new CandidateProfileFieldIdentity(CandidateProfileField.RosterMembership),
            evidence);
        evidence.Clear();

        Assert.Equal(
            ["E6-SAVE-001", "E6-SAVE-002"],
            diagnostic.Evidence.Select(item => item.Reference));
        Assert.Throws<ArgumentException>(() => new CandidateProfileDiagnostic(
            "DUPLICATE_EVIDENCE",
            CandidateProfileDiagnosticSeverity.Warning,
            "Duplicate references are invalid.",
            null,
            [first, first]));
        Assert.Throws<ArgumentException>(() => Profile(
            diagnostics: [diagnostic, diagnostic]));
    }

    [Fact]
    public void Fingerprint_is_deterministic_for_reordered_semantic_inputs()
    {
        var living = Confirmed(
            new CandidateProfileFieldIdentity(CandidateProfileField.LivingState),
            CandidateFactValue.Boolean(true),
            [Evidence("E6-SAVE-002"), Evidence("E6-SAVE-001")]);
        var reorderedLiving = Confirmed(
            new CandidateProfileFieldIdentity(CandidateProfileField.LivingState),
            CandidateFactValue.Boolean(true),
            [Evidence("E6-SAVE-001"), Evidence("E6-SAVE-002")]);
        var aptitude = Confirmed(MartialField(), CandidateFactValue.Int16(65));

        var first = Profile(facts: [living, aptitude]);
        var second = Profile(facts: [aptitude, reorderedLiving]);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Fingerprint_changes_with_semantic_facts_identity_state_and_versions()
    {
        var fact = Confirmed(MartialField(), CandidateFactValue.Int16(65));
        var baseline = Profile(facts: [fact]);
        var valueChanged = Profile(facts: [
            Confirmed(MartialField(), CandidateFactValue.Int16(66))
        ]);
        var identityChanged = Profile(characterId: 43, facts: [fact]);
        var stateChanged = Profile(
            state: CandidateUniverseState.Incomplete,
            facts: [fact]);
        var versionChanged = Profile(
            versions: Versions(mappingVersion: "2"),
            facts: [fact]);

        Assert.NotEqual(baseline.Fingerprint, valueChanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, identityChanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, stateChanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, versionChanged.Fingerprint);
    }

    [Fact]
    public void Fingerprint_excludes_diagnostic_detail_and_unavailable_detail()
    {
        var field = MartialField();
        var firstFact = CandidateProfileFact.Incomplete(
            field,
            Reason("MISSING_FIELD", "No value was projected."),
            []);
        var secondFact = CandidateProfileFact.Incomplete(
            field,
            Reason("MISSING_FIELD", @"Diagnostic from C:\local\save.sav."),
            []);
        var firstDiagnostic = Diagnostic("No local detail.");
        var secondDiagnostic = Diagnostic(@"Diagnostic from C:\other\save.sav.");

        var first = Profile(facts: [firstFact], diagnostics: [firstDiagnostic]);
        var second = Profile(facts: [secondFact], diagnostics: [secondDiagnostic]);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Source_versions_validate_hash_and_stable_versions()
    {
        var versions = Versions();

        Assert.Equal(Sha, versions.SaveSha256);
        Assert.Throws<ArgumentException>(() => new CandidateProfileSourceVersions(
            "NOT-A-HASH",
            GameDataVersion,
            "1",
            "1",
            "1"));
        Assert.Throws<ArgumentException>(() => new CandidateProfileSourceVersions(
            Sha,
            " ",
            "1",
            "1",
            "1"));
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string GameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    private static CandidateProfile Profile(
        int characterId = 42,
        CandidateUniverseState state = CandidateUniverseState.Eligible,
        CandidateProfileSourceVersions? versions = null,
        IEnumerable<CandidateProfileFact>? facts = null,
        IEnumerable<CandidateProfileDiagnostic>? diagnostics = null) => new(
            new CandidateIdentity(characterId),
            state,
            versions ?? Versions(),
            facts ?? [],
            diagnostics ?? []);

    private static CandidateProfileSourceVersions Versions(
        string mappingVersion = "1") => new(
            Sha,
            GameDataVersion,
            mappingVersion,
            "1",
            "1");

    private static CandidateProfileFieldIdentity MartialField(short type = 0) =>
        new(
            CandidateProfileField.BaseMartialQualification,
            new CandidateDisciplineIdentity(
                CandidateDisciplineDomain.Martial,
                type));

    private static CandidateProfileFact Confirmed(
        CandidateProfileFieldIdentity field,
        CandidateFactValue value,
        IEnumerable<CandidateEvidenceReference>? evidence = null) =>
        CandidateProfileFact.Confirmed(
            field,
            value,
            SaveProvenance(),
            evidence ?? [Evidence()]);

    private static CandidateFactProvenance SaveProvenance(
        string source = "CONFIGURED_SAVE",
        string revision = Sha) => new(
            CandidateEvidenceSourceKind.ConfiguredSave,
            source,
            "1",
            revision);

    private static CandidateEvidenceReference Evidence(
        string reference = "E6-SAVE-001",
        string source = "CONFIGURED_SAVE",
        string revision = Sha) => new(
            reference,
            SaveProvenance(source, revision));

    private static CandidateConflictValue Conflict(
        CandidateFactValue value,
        CandidateFactProvenance provenance,
        string reference) => new(
            value,
            provenance,
            [new CandidateEvidenceReference(reference, provenance)]);

    private static CandidateUnavailableReason Reason(
        string code,
        string detail = "The required saved fact is unavailable.") =>
        new(code, detail);

    private static CandidateProfileDiagnostic Diagnostic(string detail) => new(
        "PROFILE_INCOMPLETE",
        CandidateProfileDiagnosticSeverity.Warning,
        detail,
        MartialField(),
        []);
}
