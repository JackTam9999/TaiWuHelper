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
            snapshot,
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
            .ThenBy(finding => SourcePriority(finding.Sources[0].Kind))
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
        CombatSnapshot snapshot,
        List<TargetThreatWarning> warnings,
        bool reportUnavailableLoadout)
    {
        var target = snapshot.Target;
        var learnedById = target.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        HashSet<int> equippedIds = [];
        List<Candidate> candidates = [];
        var observation = target.LoadoutObservation;
        if (observation is not null)
        {
            var battleVisible = observation.ObservationContext
                != TargetObservationContext.Sparring;
            foreach (var observed in observation.ObservedSkills
                         .OrderBy(skill => skill.Category)
                         .ThenBy(skill => skill.SlotIndex ?? int.MaxValue)
                         .ThenBy(skill => skill.SkillId))
            {
                equippedIds.Add(observed.SkillId);
                if (learnedById.TryGetValue(observed.SkillId, out var skill))
                {
                    candidates.Add(new Candidate(
                        skill,
                        battleVisible
                            ? TargetThreatSourceScope
                                .BattleVisibleActiveEffect
                            : TargetThreatSourceScope.Equipped,
                        battleVisible
                            ? TargetThreatSourceKind.ObservedActiveEffect
                            : TargetThreatSourceKind.ObservedEquipped,
                        observation.EvidenceReference));
                }
                else
                {
                    AddEquippedSkillNotLearnedWarning(
                        target.CharacterId,
                        observed.SkillId,
                        warnings);
                }
            }
        }

        var saveEvidenceReference =
            $"save:{snapshot.Metadata.SaveSha256}";
        if (target.EquippedSkills.IsAvailable)
        {
            foreach (var category in Enum.GetValues<SkillCategory>())
            {
                foreach (var skillId in
                         target.EquippedSkills.Value.Get(category))
                {
                    if (!equippedIds.Add(skillId))
                    {
                        continue;
                    }

                    if (learnedById.TryGetValue(skillId, out var skill))
                    {
                        candidates.Add(new Candidate(
                            skill,
                            TargetThreatSourceScope.Equipped,
                            TargetThreatSourceKind.SaveEquipped,
                            saveEvidenceReference));
                    }
                    else
                    {
                        AddEquippedSkillNotLearnedWarning(
                            target.CharacterId,
                            skillId,
                            warnings);
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
                    TargetThreatSourceScope.LearnedUnequipped,
                    TargetThreatSourceKind.LearnedUnconfirmed,
                    saveEvidenceReference)));
        return candidates;
    }

    private static int SourcePriority(TargetThreatSourceKind kind) => kind switch
    {
        TargetThreatSourceKind.ObservedActiveEffect => 0,
        TargetThreatSourceKind.ObservedEquipped => 1,
        TargetThreatSourceKind.SaveEquipped => 2,
        TargetThreatSourceKind.LearnedUnconfirmed => 3,
        _ => int.MaxValue
    };

    private static void AddEquippedSkillNotLearnedWarning(
        int targetCharacterId,
        int skillId,
        List<TargetThreatWarning> warnings)
    {
        warnings.Add(
            Warning(
                EquippedSkillNotLearnedWarningCode,
                $"Target equipped skill {skillId} is absent "
                + "from learned-skill evidence.",
                $"snapshot:target:{targetCharacterId}:skill:{skillId}",
                sourceSkillId: skillId));
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
                    candidate.EvidenceReference,
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
                    candidate.EvidenceReference,
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
                    candidate.EvidenceReference,
                    skill.SkillId,
                    effectId.Value));
            return;
        }

        var source = new TargetThreatSource(
            skill.SkillId,
            direction,
            effectId.Value,
            candidate.Scope,
            candidate.Kind,
            candidate.EvidenceReference);
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

    private sealed record Candidate(
        CombatSkillSnapshot Skill,
        TargetThreatSourceScope Scope,
        TargetThreatSourceKind Kind,
        string EvidenceReference);
}
