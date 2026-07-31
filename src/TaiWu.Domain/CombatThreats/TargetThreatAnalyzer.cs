using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public static class TargetThreatAnalyzer
{
    public const string GameDataVersionUnavailableWarningCode =
        "TARGET_GAMEDATA_VERSION_UNAVAILABLE";

    public const string UnsupportedGameDataVersionWarningCode =
        "TARGET_GAMEDATA_VERSION_UNSUPPORTED";

    public const string EquippedSkillsUnavailableWarningCode =
        "TARGET_EQUIPPED_SKILLS_UNAVAILABLE";

    public const string EquippedSkillNotLearnedWarningCode =
        "TARGET_EQUIPPED_SKILL_NOT_LEARNED";

    public const string SkillDirectionUnavailableWarningCode =
        "TARGET_SKILL_DIRECTION_UNAVAILABLE";

    public const string EffectIdUnavailableWarningCode =
        "TARGET_EFFECT_ID_UNAVAILABLE";

    public const string UnrecognizedEffectWarningCode =
        "UNRECOGNIZED_TARGET_EFFECT";

    public static TargetThreatAnalysis Analyze(
        CombatSnapshot snapshot,
        TargetThreatRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(ruleSet);

        List<TargetThreatWarning> warnings = [];
        AddUnresolvedRuleWarnings(ruleSet, warnings);
        if (!ValidateVersion(snapshot.Metadata, ruleSet, warnings))
        {
            return new TargetThreatAnalysis([], warnings);
        }

        var loadoutAbsenceAlreadyReported = snapshot.Warnings.Any(
            warning => warning.Code.Equals(
                CombatSnapshotWarningCodes.TargetLoadoutNotPersisted,
                StringComparison.Ordinal));
        var candidates = GetCandidates(
            snapshot.Target,
            warnings,
            reportUnavailableLoadout: !loadoutAbsenceAlreadyReported);
        Dictionary<
            string,
            (TargetThreat Threat, List<TargetThreatSource> Sources)> findings =
            new(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            AnalyzeCandidate(candidate, ruleSet, findings, warnings);
        }

        var ordered = findings.Values
            .Select(finding => new AnalyzedTargetThreat(
                finding.Threat,
                finding.Sources))
            .OrderByDescending(finding => finding.Threat.Severity)
            .ThenBy(finding => finding.Sources[0].Scope)
            .ThenBy(
                finding => finding.Threat.Code,
                StringComparer.Ordinal)
            .ToArray();

        return new TargetThreatAnalysis(ordered, warnings);
    }

    private static bool ValidateVersion(
        CombatSnapshotMetadata metadata,
        TargetThreatRuleSet ruleSet,
        List<TargetThreatWarning> warnings)
    {
        if (!metadata.GameDataVersion.IsAvailable)
        {
            warnings.Add(
                Warning(
                    GameDataVersionUnavailableWarningCode,
                    "Target threats were not analyzed because the GameData "
                    + "version is unavailable: "
                    + metadata.GameDataVersion.UnavailableReason,
                    "snapshot:metadata:game-data-version"));
            return false;
        }

        if (!string.Equals(
                metadata.GameDataVersion.Value,
                ruleSet.GameDataVersion,
                StringComparison.Ordinal))
        {
            warnings.Add(
                Warning(
                    UnsupportedGameDataVersionWarningCode,
                    $"Target threats were not analyzed for unsupported "
                    + $"GameData version "
                    + $"'{metadata.GameDataVersion.Value}'.",
                    "snapshot:metadata:game-data-version"));
            return false;
        }

        return true;
    }

    private static List<Candidate> GetCandidates(
        TargetCombatSnapshot target,
        List<TargetThreatWarning> warnings,
        bool reportUnavailableLoadout)
    {
        var learnedById = target.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        HashSet<int> equippedIds = [];
        List<Candidate> candidates = [];
        if (target.EquippedSkills.IsAvailable)
        {
            foreach (var category in Enum.GetValues<SkillCategory>())
            {
                foreach (var skillId in
                         target.EquippedSkills.Value.Get(category))
                {
                    equippedIds.Add(skillId);
                    if (learnedById.TryGetValue(skillId, out var skill))
                    {
                        candidates.Add(
                            new Candidate(
                                skill,
                                TargetThreatSourceScope.Equipped));
                    }
                    else
                    {
                        warnings.Add(
                            Warning(
                                EquippedSkillNotLearnedWarningCode,
                                $"Target equipped skill {skillId} is absent "
                                + "from learned-skill evidence.",
                                $"snapshot:target:{target.CharacterId}"
                                + $":skill:{skillId}",
                                sourceSkillId: skillId));
                    }
                }
            }
        }
        else if (reportUnavailableLoadout)
        {
            warnings.Add(
                Warning(
                    EquippedSkillsUnavailableWarningCode,
                    "Target equipped skills are unavailable: "
                    + target.EquippedSkills.UnavailableReason,
                    $"snapshot:target:{target.CharacterId}"
                    + ":equipped-skills"));
        }

        candidates.AddRange(
            target.LearnedSkills
                .Where(skill => !equippedIds.Contains(skill.SkillId))
                .OrderBy(skill => skill.SkillId)
                .Select(skill => new Candidate(
                    skill,
                    TargetThreatSourceScope.LearnedUnequipped)));
        return candidates;
    }

    private static void AnalyzeCandidate(
        Candidate candidate,
        TargetThreatRuleSet ruleSet,
        Dictionary<
            string,
            (TargetThreat Threat, List<TargetThreatSource> Sources)> findings,
        List<TargetThreatWarning> warnings)
    {
        var skill = candidate.Skill;
        if (!ruleSet.RelevantSkillIds.Contains(skill.SkillId))
        {
            return;
        }

        if (!skill.Direction.IsAvailable)
        {
            warnings.Add(
                Warning(
                    SkillDirectionUnavailableWarningCode,
                    $"Target skill {skill.SkillId} has unavailable practice "
                    + $"direction: {skill.Direction.UnavailableReason}",
                    SourceReference(skill.SkillId),
                    skill.SkillId));
            return;
        }

        var direction = skill.Direction.Value;
        if (direction == PracticeDirection.Neutral)
        {
            return;
        }

        var effectId = direction == PracticeDirection.Direct
            ? skill.DirectEffectId
            : skill.ReverseEffectId;
        if (!effectId.IsAvailable)
        {
            warnings.Add(
                Warning(
                    EffectIdUnavailableWarningCode,
                    $"Target skill {skill.SkillId} has unavailable "
                    + $"{direction} effect ID: "
                    + effectId.UnavailableReason,
                    SourceReference(skill.SkillId),
                    skill.SkillId));
            return;
        }

        var matches = ruleSet.Rules
            .Where(rule => rule.Matches(
                skill.SkillId,
                direction,
                effectId.Value))
            .ToArray();
        if (matches.Length == 0)
        {
            warnings.Add(
                Warning(
                    UnrecognizedEffectWarningCode,
                    $"Target skill {skill.SkillId} has unrecognized "
                    + $"{direction} effect {effectId.Value}.",
                    SourceReference(skill.SkillId),
                    skill.SkillId,
                    effectId.Value));
            return;
        }

        var source = new TargetThreatSource(
            skill.SkillId,
            direction,
            effectId.Value,
            candidate.Scope);
        foreach (var rule in matches)
        {
            if (!findings.TryGetValue(
                    rule.Threat.Code,
                    out var finding))
            {
                finding = (rule.Threat, []);
                findings.Add(rule.Threat.Code, finding);
            }

            finding.Sources.Add(source);
        }
    }

    private static void AddUnresolvedRuleWarnings(
        TargetThreatRuleSet ruleSet,
        List<TargetThreatWarning> warnings)
    {
        warnings.AddRange(
            ruleSet.UnresolvedMechanics.Select(
                mechanic => new TargetThreatWarning(
                    TargetThreatTaxonomy.UnrecognizedMechanicWarningCode,
                    $"Unrecognized target mechanic: "
                    + mechanic.Description,
                    mechanic)));
    }

    private static TargetThreatWarning Warning(
        string code,
        string message,
        string evidenceReference,
        int? sourceSkillId = null,
        int? rawEffectId = null)
    {
        return new TargetThreatWarning(
            code,
            message,
            new UnknownTargetMechanic(
                message,
                evidenceReference,
                sourceSkillId,
                rawEffectId));
    }

    private static string SourceReference(int skillId) =>
        $"snapshot:target:skill:{skillId}";

    private sealed record Candidate(
        CombatSkillSnapshot Skill,
        TargetThreatSourceScope Scope);
}
