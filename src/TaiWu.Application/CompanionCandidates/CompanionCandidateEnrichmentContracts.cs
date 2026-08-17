using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Application.CompanionCandidates;

public enum CompanionCandidateEnrichmentStatus
{
    Complete = 0,
    Partial = 1,
    CatalogueMissing = 2,
    CatalogueStale = 3,
    CatalogueRebuilding = 4,
    CatalogueUnsupported = 5,
    CatalogueFailed = 6
}

public enum CompanionCandidateEnrichmentState
{
    Complete = 0,
    Partial = 1,
    CatalogueMissing = 2,
    CatalogueStale = 3,
    CatalogueRebuilding = 4,
    CatalogueUnsupported = 5,
    CatalogueFailed = 6
}

public enum CompanionMembershipEvidenceState
{
    Available = 0,
    Incomplete = 1,
    Unsupported = 2,
    Stale = 3,
    Conflicting = 4
}

public enum CompanionSkillDefinitionState
{
    Available = 0,
    Missing = 1,
    CatalogueUnavailable = 2
}

public enum CompanionDetailedProgressState
{
    NotRequestedByApprovedRole = 0
}

public sealed class CompanionSkillMembershipFact
{
    internal CompanionSkillMembershipFact(
        CompanionMembershipEvidenceState state,
        bool? value,
        CandidateProfileFact? sourceFact)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown membership evidence state.");
        }

        if ((state == CompanionMembershipEvidenceState.Available) != value.HasValue)
        {
            throw new ArgumentException("Only available membership evidence has a Boolean value.", nameof(value));
        }

        State = state;
        Value = value;
        if (state == CompanionMembershipEvidenceState.Available && sourceFact is null)
        {
            throw new ArgumentNullException(nameof(sourceFact));
        }

        SourceFact = sourceFact;
    }

    public CompanionMembershipEvidenceState State { get; }

    public bool? Value { get; }

    public CandidateProfileFact? SourceFact { get; }

    internal string StableKey => $"{(int)State}|{(Value.HasValue ? (Value.Value ? 1 : 0) : -1)}|{SourceFact?.Identity.Field.ToString() ?? "NONE"}";
}

public sealed class CompanionCombatSkillEnrichment
{
    internal CompanionCombatSkillEnrichment(
        int skillId,
        CompanionSkillMembershipFact learned,
        CompanionSkillMembershipFact equipped,
        CompanionSkillDefinitionState definitionState,
        CombatSkillDefinition? definition)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillId), skillId, "A combat-skill identity cannot be negative.");
        }

        if (!Enum.IsDefined(definitionState))
        {
            throw new ArgumentOutOfRangeException(nameof(definitionState), definitionState, "Unknown skill-definition state.");
        }

        if ((definitionState == CompanionSkillDefinitionState.Available) != (definition is not null)
            || definition is not null && definition.SkillId != skillId)
        {
            throw new ArgumentException("Skill-definition state, identity, and value are incompatible.", nameof(definition));
        }

        SkillId = skillId;
        Learned = learned ?? throw new ArgumentNullException(nameof(learned));
        Equipped = equipped ?? throw new ArgumentNullException(nameof(equipped));
        DefinitionState = definitionState;
        Definition = definition;
    }

    public int SkillId { get; }

    public CompanionSkillMembershipFact Learned { get; }

    public CompanionSkillMembershipFact Equipped { get; }

    public CompanionSkillDefinitionState DefinitionState { get; }

    public CombatSkillDefinition? Definition { get; }

    public CompanionDetailedProgressState DetailedProgressState =>
        CompanionDetailedProgressState.NotRequestedByApprovedRole;

    internal string StableKey => string.Join('|',
        SkillId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Learned.StableKey,
        Equipped.StableKey,
        ((int)DefinitionState).ToString(System.Globalization.CultureInfo.InvariantCulture));
}

public sealed class CompanionCandidateEnrichment
{
    internal CompanionCandidateEnrichment(
        CandidateProfile profile,
        CompanionCandidateEnrichmentState state,
        CompanionMembershipEvidenceState learnedMartialState,
        CompanionMembershipEvidenceState equippedMartialState,
        CompanionMembershipEvidenceState learnedLifeSkillState,
        IEnumerable<CompanionCombatSkillEnrichment> combatSkills,
        IEnumerable<CompanionCandidateSnapshotDiagnostic> diagnostics)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!Enum.IsDefined(state)
            || !Enum.IsDefined(learnedMartialState)
            || !Enum.IsDefined(equippedMartialState)
            || !Enum.IsDefined(learnedLifeSkillState))
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Unknown candidate enrichment state.");
        }

        ArgumentNullException.ThrowIfNull(combatSkills);
        var skillValues = combatSkills.ToImmutableArray();
        if (skillValues.Any(item => item is null)
            || skillValues.GroupBy(item => item.SkillId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Candidate enrichment cannot contain null or duplicate combat skills.", nameof(combatSkills));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var diagnosticValues = diagnostics.ToImmutableArray();
        if (diagnosticValues.Any(item => item is null)
            || diagnosticValues.GroupBy(item => item.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Candidate enrichment cannot contain null or duplicate diagnostics.", nameof(diagnostics));
        }

        State = state;
        LearnedMartialState = learnedMartialState;
        EquippedMartialState = equippedMartialState;
        LearnedLifeSkillState = learnedLifeSkillState;
        CombatSkills = [.. skillValues.OrderBy(item => item.SkillId)];
        Diagnostics = [.. diagnosticValues.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
    }

    public CandidateProfile Profile { get; }

    public CompanionCandidateEnrichmentState State { get; }

    public CompanionMembershipEvidenceState LearnedMartialState { get; }

    public CompanionMembershipEvidenceState EquippedMartialState { get; }

    public CompanionMembershipEvidenceState LearnedLifeSkillState { get; }

    public ImmutableArray<CompanionCombatSkillEnrichment> CombatSkills { get; }

    public ImmutableArray<CompanionCandidateSnapshotDiagnostic> Diagnostics { get; }

    internal string StableKey => string.Join('|',
        Profile.Fingerprint,
        ((int)State).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)LearnedMartialState).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)EquippedMartialState).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)LearnedLifeSkillState).ToString(System.Globalization.CultureInfo.InvariantCulture),
        string.Join("||", CombatSkills.Select(item => item.StableKey)),
        string.Join("||", Diagnostics.Select(item => item.StableKey)));
}

public sealed class CompanionCandidateEnrichmentResult
{
    internal CompanionCandidateEnrichmentResult(
        CompanionCandidateSnapshot snapshot,
        CompanionCandidateEnrichmentStatus status,
        CombatSkillCatalogueStatus catalogueStatus,
        CombatSkillCatalogueSourceIdentity? catalogueSource,
        IEnumerable<CompanionCandidateEnrichment> candidates,
        IEnumerable<CompanionCandidateSnapshotDiagnostic> diagnostics)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        if (!Enum.IsDefined(status) || !Enum.IsDefined(catalogueStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Unknown enrichment or catalogue state.");
        }

        ArgumentNullException.ThrowIfNull(candidates);
        var candidateValues = candidates.ToImmutableArray();
        if (candidateValues.Any(item => item is null)
            || candidateValues.GroupBy(item => item.Profile.Identity.CharacterId)
                .Any(group => group.Count() > 1)
            || candidateValues.Any(item => !snapshot.Profiles.Contains(item.Profile)))
        {
            throw new ArgumentException("Enrichment candidates must uniquely reference snapshot profiles.", nameof(candidates));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var diagnosticValues = diagnostics.ToImmutableArray();
        if (diagnosticValues.Any(item => item is null)
            || diagnosticValues.GroupBy(item => item.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Enrichment result diagnostics must be unique and non-null.", nameof(diagnostics));
        }

        Status = status;
        CatalogueStatus = catalogueStatus;
        CatalogueSource = catalogueSource;
        Candidates = [.. candidateValues.OrderBy(item => item.Profile.Identity.CharacterId)];
        Diagnostics = [.. diagnosticValues.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
        Fingerprint = CreateFingerprint();
    }

    public CompanionCandidateSnapshot Snapshot { get; }

    public CompanionCandidateEnrichmentStatus Status { get; }

    public CombatSkillCatalogueStatus CatalogueStatus { get; }

    public CombatSkillCatalogueSourceIdentity? CatalogueSource { get; }

    public ImmutableArray<CompanionCandidateEnrichment> Candidates { get; }

    public ImmutableArray<CompanionCandidateSnapshotDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_CANDIDATE_ENRICHMENT_V1\n")
            .Append(Snapshot.SourceVersions.SaveSha256).Append('|')
            .Append(Snapshot.SourceVersions.GameDataVersion).Append('|')
            .Append(Snapshot.SourceVersions.ProfileMappingVersion).Append('\n')
            .Append((int)Status).Append('|').Append((int)CatalogueStatus).Append('\n');
        if (CatalogueSource is not null)
        {
            canonical.Append("CATALOGUE|")
                .Append(CatalogueSource.GameDataVersion).Append('|')
                .Append(CatalogueSource.ImporterVersion).Append('|')
                .Append(CatalogueSource.GameDataFingerprint).Append('|')
                .Append(CatalogueSource.TraditionalChineseFingerprint).Append('|')
                .Append(CatalogueSource.EnglishFingerprint).Append('\n');
        }

        foreach (var candidate in Candidates)
        {
            canonical.Append("CANDIDATE|").Append(candidate.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical.Append("DIAGNOSTIC|").Append(diagnostic.StableKey).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
