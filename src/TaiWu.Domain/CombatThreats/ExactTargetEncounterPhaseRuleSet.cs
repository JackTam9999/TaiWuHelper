using System.Collections.Immutable;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public sealed class ExactTargetEncounterPhaseRuleSet
{
    public ExactTargetEncounterPhaseRuleSet(
        string gameDataVersion,
        int targetTemplateId,
        string phaseCode,
        IEnumerable<TargetThreatSkillSignature> expectedEquippedSkills,
        IEnumerable<int> directMagicSoundSkillIds,
        IEnumerable<TargetEncounterFact> facts,
        IEnumerable<TargetEncounterTransition> transitions)
    {
        GameDataVersion = TargetEncounterText.Stable(
            gameDataVersion,
            nameof(gameDataVersion));
        if (targetTemplateId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetTemplateId),
                targetTemplateId,
                "An exact target template ID must be positive.");
        }

        TargetTemplateId = targetTemplateId;
        PhaseCode = TargetEncounterText.Code(phaseCode, nameof(phaseCode));
        ArgumentNullException.ThrowIfNull(expectedEquippedSkills);
        var signatures = expectedEquippedSkills
            .OrderBy(item => item.SkillId)
            .ToImmutableArray();
        if (signatures.IsEmpty || signatures.Any(item => item is null)
            || signatures.GroupBy(item => item.SkillId).Any(group =>
                group.Count() > 1))
        {
            throw new ArgumentException(
                "Expected exact-target skills must be non-empty and unique.",
                nameof(expectedEquippedSkills));
        }

        ExpectedEquippedSkills = signatures;
        ArgumentNullException.ThrowIfNull(directMagicSoundSkillIds);
        var magic = directMagicSoundSkillIds.Order().ToImmutableArray();
        if (magic.IsEmpty || magic.Any(id => id < 0)
            || magic.Distinct().Count() != magic.Length
            || magic.Any(id => signatures.All(item => item.SkillId != id))
            || magic.Any(id => signatures.Single(item =>
                    item.SkillId == id).Direction
                != PracticeDirection.Direct))
        {
            throw new ArgumentException(
                "Direct magic-sound skills must be a non-empty equipped subset.",
                nameof(directMagicSoundSkillIds));
        }

        DirectMagicSoundSkillIds = magic;
        Facts = TargetEncounterText.CopyUnique(
            facts,
            item => item.Code,
            nameof(facts));
        Transitions = TargetEncounterText.CopyUnique(
            transitions,
            item => item.Code,
            nameof(transitions));
        if (Facts.IsEmpty || Transitions.IsEmpty)
        {
            throw new ArgumentException(
                "An exact-target phase requires facts and transitions.");
        }

        if (Facts.SelectMany(item => item.Evidence)
            .Concat(Transitions.SelectMany(item => item.Evidence))
            .Any(item => !string.Equals(
                item.GameDataVersion,
                GameDataVersion,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Exact-target rule evidence must match the rule version.");
        }

        var factCodes = Facts.Select(item => item.Code)
            .ToHashSet(StringComparer.Ordinal);
        if (Transitions.SelectMany(item => item.TriggerFactCodes
                .Concat(item.ResultFactCodes))
            .Any(code => !factCodes.Contains(code)))
        {
            throw new ArgumentException(
                "Every transition must reference facts from this exact phase.",
                nameof(transitions));
        }

        Fingerprint = CreateFingerprint();
    }

    public string GameDataVersion { get; }

    public int TargetTemplateId { get; }

    public string PhaseCode { get; }

    public ImmutableArray<TargetThreatSkillSignature> ExpectedEquippedSkills
    { get; }

    public ImmutableArray<int> DirectMagicSoundSkillIds { get; }

    public ImmutableArray<TargetEncounterFact> Facts { get; }

    public ImmutableArray<TargetEncounterTransition> Transitions { get; }

    public bool Reverse604CoversEveryEquippedSkill =>
        ExpectedEquippedSkills.All(item =>
            item.Direction == PracticeDirection.Direct);

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("EXACT_TARGET_PHASE_V1\n")
            .Append(GameDataVersion).Append('\n')
            .Append(TargetTemplateId).Append('\n')
            .Append(PhaseCode).Append('\n');
        foreach (var signature in ExpectedEquippedSkills)
        {
            canonical.Append("SKILL|")
                .Append(signature.SkillId).Append('|')
                .Append((int)signature.Direction).Append('|')
                .Append(signature.RawEffectId).Append('\n');
        }

        canonical.Append("MAGIC|")
            .AppendJoin(',', DirectMagicSoundSkillIds)
            .Append('\n');
        foreach (var fact in Facts)
        {
            canonical.Append("FACT|").Append(fact.StableKey).Append('\n');
        }

        foreach (var transition in Transitions)
        {
            canonical.Append("TRANSITION|")
                .Append(transition.StableKey)
                .Append('\n');
        }

        return TargetEncounterText.Fingerprint(canonical.ToString());
    }
}

public sealed record TargetEncounterPhaseResolution
{
    internal TargetEncounterPhaseResolution(
        TargetEncounterBindingStatus status,
        ExactTargetEncounterPhaseRuleSet ruleSet,
        IEnumerable<string> diagnostics)
    {
        Status = status;
        RuleSet = ruleSet;
        Diagnostics = TargetEncounterText.Codes(
            diagnostics,
            nameof(diagnostics));
    }

    public TargetEncounterBindingStatus Status { get; }

    public ExactTargetEncounterPhaseRuleSet RuleSet { get; }

    public ImmutableArray<string> Diagnostics { get; }
}

public static class ExactTargetEncounterPhaseResolver
{
    public static TargetEncounterPhaseResolution Resolve(
        ExactTargetEncounterPhaseRuleSet ruleSet,
        TargetEncounterPhaseObservation observation)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(observation);
        if (!string.Equals(
                ruleSet.GameDataVersion,
                observation.DetectedGameDataVersion,
                StringComparison.Ordinal))
        {
            return Result(
                TargetEncounterBindingStatus.UnsupportedVersion,
                ruleSet,
                "UNSUPPORTED_GAME_DATA_VERSION");
        }

        if (observation.PhaseEvidence
                .Select(item => item.Evidence)
                .Append(observation.LoadoutEvidence)
                .Where(item => item is not null)
                .Any(item => !string.Equals(
                    item!.GameDataVersion,
                    observation.DetectedGameDataVersion,
                    StringComparison.Ordinal)))
        {
            return Result(
                TargetEncounterBindingStatus.Conflicting,
                ruleSet,
                "TARGET_EVIDENCE_VERSION_CONFLICT");
        }

        var phases = observation.PhaseEvidence
            .Select(item => item.TargetTemplateId)
            .Distinct()
            .ToArray();
        if (phases.Length == 0)
        {
            return Result(
                TargetEncounterBindingStatus.Partial,
                ruleSet,
                "TARGET_PHASE_EVIDENCE_MISSING");
        }

        if (phases.Length > 1)
        {
            return Result(
                TargetEncounterBindingStatus.Conflicting,
                ruleSet,
                "TARGET_PHASE_EVIDENCE_CONFLICT");
        }

        if (phases[0] != ruleSet.TargetTemplateId)
        {
            return Result(
                TargetEncounterBindingStatus.WrongPhase,
                ruleSet,
                "TARGET_PHASE_DOES_NOT_MATCH_RULE");
        }

        if (!observation.LoadoutCoverage.HasValue)
        {
            return Result(
                TargetEncounterBindingStatus.Partial,
                ruleSet,
                "TARGET_LOADOUT_EVIDENCE_MISSING");
        }

        if (observation.LoadoutCoverage.Value
            == TargetLoadoutCoverageKind.PartialLoadout)
        {
            return Result(
                TargetEncounterBindingStatus.Partial,
                ruleSet,
                "TARGET_LOADOUT_COVERAGE_PARTIAL");
        }

        if (!SignaturesEqual(
                ruleSet.ExpectedEquippedSkills,
                observation.EquippedSkillSignatures))
        {
            return Result(
                TargetEncounterBindingStatus.WrongPhase,
                ruleSet,
                "TARGET_LOADOUT_DOES_NOT_MATCH_PHASE");
        }

        return Result(
            TargetEncounterBindingStatus.Complete,
            ruleSet,
            "EXACT_TARGET_PHASE_COMPLETE");
    }

    private static bool SignaturesEqual(
        IEnumerable<TargetThreatSkillSignature> expected,
        IEnumerable<TargetThreatSkillSignature> actual) => expected
        .Select(SignatureKey)
        .SequenceEqual(actual.Select(SignatureKey), StringComparer.Ordinal);

    private static string SignatureKey(TargetThreatSkillSignature item) =>
        $"{item.SkillId}:{(int)item.Direction}:{item.RawEffectId}";

    private static TargetEncounterPhaseResolution Result(
        TargetEncounterBindingStatus status,
        ExactTargetEncounterPhaseRuleSet ruleSet,
        params string[] diagnostics) => new(status, ruleSet, diagnostics);
}
