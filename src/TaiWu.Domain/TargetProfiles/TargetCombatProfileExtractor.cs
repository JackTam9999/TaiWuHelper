using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.TargetProfiles;

public static class TargetCombatProfileExtractor
{
    public const string GameDataVersionUnavailableCode =
        "PROFILE_GAMEDATA_VERSION_UNAVAILABLE";

    public const string GameDataVersionUnsupportedCode =
        "PROFILE_GAMEDATA_VERSION_UNSUPPORTED";

    public const string ActiveSkillStaticFactsMissingCode =
        "ACTIVE_SKILL_STATIC_FACTS_MISSING";

    public const string LearnedThreatNotActiveCode =
        "LEARNED_THREAT_NOT_ACTIVE";

    public static TargetCombatProfile Extract(
        CombatSnapshot snapshot,
        TargetThreatAnalysis threatAnalysis,
        TargetProfileExtractionRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(threatAnalysis);
        ArgumentNullException.ThrowIfNull(rules);

        if (!snapshot.Metadata.GameDataVersion.IsAvailable)
        {
            return EmptyUnsupported(
                snapshot,
                rules,
                GameDataVersionUnavailableCode);
        }

        if (!string.Equals(
                snapshot.Metadata.GameDataVersion.Value,
                rules.GameDataVersion.Value,
                StringComparison.Ordinal))
        {
            return EmptyUnsupported(
                snapshot,
                rules,
                GameDataVersionUnsupportedCode);
        }

        var sourceVersion = new TargetProfileVersion(
            snapshot.Metadata.GameDataVersion.Value);
        List<TargetProfileFacet> facets = [];
        List<TargetProfileDiagnostic> diagnostics = [];
        AddWeaponFacets(snapshot.Target, rules, sourceVersion, facets);

        var bindings = GetActiveSkillBindings(snapshot.Target, sourceVersion);
        AddSkillMechanicFacet(
            snapshot.Target,
            rules.OuterDamageFacet,
            bindings,
            skill => skill.HasConfiguredOuterDamage,
            sourceVersion,
            facets,
            diagnostics);
        AddSkillMechanicFacet(
            snapshot.Target,
            rules.PoisonApplicationFacet,
            bindings,
            skill => skill.HasConfiguredPoisonApplication,
            sourceVersion,
            facets,
            diagnostics);
        AddResistanceFacet(
            snapshot.Target,
            rules.ChannelResistanceFacet,
            sourceVersion,
            facets);
        AddThreatFacets(
            threatAnalysis,
            rules,
            sourceVersion,
            facets,
            diagnostics);
        AddSnapshotDiagnostics(snapshot, diagnostics);

        return new TargetCombatProfile(
            snapshot.Target.CharacterId,
            rules.RuleVersion,
            facets,
            diagnostics
                .DistinctBy(diagnostic => diagnostic.StableKey,
                    StringComparer.Ordinal));
    }

    private static TargetCombatProfile EmptyUnsupported(
        CombatSnapshot snapshot,
        TargetProfileExtractionRuleSet rules,
        string diagnosticCode) => new(
            snapshot.Target.CharacterId,
            rules.RuleVersion,
            [],
            [
                new TargetProfileDiagnostic(
                    diagnosticCode,
                    TargetProfileDiagnosticSeverity.Error,
                    facet: null)
            ]);

    private static void AddWeaponFacets(
        TargetCombatSnapshot target,
        TargetProfileExtractionRuleSet rules,
        TargetProfileVersion sourceVersion,
        ICollection<TargetProfileFacet> facets)
    {
        foreach (var group in target.Equipment
                     .Where(item => item.Kind.IsAvailable
                         && item.Kind.Value == EquipmentKind.Weapon
                         && item.ItemSubtype.IsAvailable)
                     .GroupBy(item => item.ItemSubtype.Value)
                     .OrderBy(group => group.Key))
        {
            var identity = rules.WeaponSubtypeFacet(group.Key);
            List<TargetProfileEvidence> evidence = [];
            foreach (var item in group.OrderBy(value => value.SlotIndex))
            {
                evidence.Add(new TargetProfileEvidence(
                    $"SAVE:WEAPON_SLOT:{item.SlotIndex}",
                    TargetProfileEvidenceSourceKind.SavedEquippedMembership,
                    $"TARGET:{target.CharacterId}:EQUIPMENT_SLOT:"
                    + item.SlotIndex,
                    sourceVersion));
                evidence.Add(new TargetProfileEvidence(
                    item.TemplateId.IsAvailable
                        ? $"CONFIG:WEAPON:{item.TemplateId.Value}"
                        : $"CONFIG:WEAPON_SUBTYPE:{group.Key}",
                    TargetProfileEvidenceSourceKind.InstalledConfiguration,
                    item.TemplateId.IsAvailable
                        ? $"WEAPON_TEMPLATE:{item.TemplateId.Value}"
                        : $"WEAPON_SUBTYPE:{group.Key}",
                    sourceVersion));
            }

            facets.Add(TargetProfileFacet.Confirmed(
                identity,
                TargetProfileFacetValue.Presence(
                    identity.Dimension,
                    identity.Code),
                evidence.DistinctBy(
                    item => item.StableKey,
                    StringComparer.Ordinal)));
        }
    }

    private static Dictionary<int, ActiveSkillBinding>
        GetActiveSkillBindings(
            TargetCombatSnapshot target,
            TargetProfileVersion sourceVersion)
    {
        Dictionary<int, ActiveSkillBinding> bindings = [];
        var observation = target.LoadoutObservation;
        if (observation is not null)
        {
            var reference = ObservationReference(
                observation.EvidenceReference);
            foreach (var observed in observation.ObservedSkills
                         .OrderBy(skill => skill.SkillId))
            {
                bindings[observed.SkillId] = new ActiveSkillBinding(
                    observed.SkillId,
                    new TargetProfileEvidence(
                        reference,
                        TargetProfileEvidenceSourceKind
                            .CurrentScreenObservation,
                        $"TARGET:{target.CharacterId}:OBSERVED_SKILL:"
                        + observed.SkillId,
                        sourceVersion));
            }
        }

        if (target.EquippedSkills.IsAvailable
            && observation?.Coverage.CanEstablishAbsence != true)
        {
            foreach (var category in Enum.GetValues<SkillCategory>())
            {
                foreach (var skillId in target.EquippedSkills.Value
                             .Get(category)
                             .Order())
                {
                    bindings.TryAdd(
                        skillId,
                        new ActiveSkillBinding(
                            skillId,
                            new TargetProfileEvidence(
                                "SAVE:EQUIPPED_SKILL",
                                TargetProfileEvidenceSourceKind
                                    .SavedEquippedMembership,
                                $"TARGET:{target.CharacterId}:EQUIPPED_SKILL:"
                                + skillId,
                                sourceVersion)));
                }
            }
        }

        return bindings;
    }

    private static void AddSkillMechanicFacet(
        TargetCombatSnapshot target,
        TargetProfileFacetIdentity identity,
        IReadOnlyDictionary<int, ActiveSkillBinding> bindings,
        Func<CombatSkillSnapshot, SnapshotValue<bool>> selector,
        TargetProfileVersion sourceVersion,
        ICollection<TargetProfileFacet> facets,
        ICollection<TargetProfileDiagnostic> diagnostics)
    {
        var learned = target.LearnedSkills.ToDictionary(skill => skill.SkillId);
        List<TargetProfileEvidence> confirming = [];
        List<TargetProfileEvidence> unresolved = [];
        foreach (var binding in bindings.Values.OrderBy(value => value.SkillId))
        {
            if (!learned.TryGetValue(binding.SkillId, out var skill))
            {
                unresolved.Add(binding.Evidence);
                continue;
            }

            if (skill.Category != SkillCategory.Attack)
            {
                continue;
            }

            var configured = selector(skill);
            if (!configured.IsAvailable)
            {
                unresolved.Add(binding.Evidence);
                continue;
            }

            if (!configured.Value)
            {
                continue;
            }

            confirming.Add(binding.Evidence);
            confirming.Add(new TargetProfileEvidence(
                $"CONFIG:SKILL:{skill.SkillId}",
                TargetProfileEvidenceSourceKind.InstalledConfiguration,
                $"SKILL:{skill.SkillId}:MECHANICS",
                sourceVersion));
        }

        if (confirming.Count > 0)
        {
            facets.Add(TargetProfileFacet.Confirmed(
                identity,
                TargetProfileFacetValue.Presence(
                    identity.Dimension,
                    identity.Code),
                confirming.DistinctBy(
                    evidence => evidence.StableKey,
                    StringComparer.Ordinal)));
            return;
        }

        if (unresolved.Count > 0)
        {
            facets.Add(TargetProfileFacet.Incomplete(
                identity,
                unresolved.DistinctBy(
                    evidence => evidence.StableKey,
                    StringComparer.Ordinal),
                new TargetProfileUnavailableReason(
                    "STATIC_MECHANIC_UNAVAILABLE")));
            diagnostics.Add(new TargetProfileDiagnostic(
                ActiveSkillStaticFactsMissingCode,
                TargetProfileDiagnosticSeverity.Warning,
                identity,
                unresolved.Select(evidence => evidence.Reference)
                    .Distinct(StringComparer.Ordinal)));
            return;
        }

        if (bindings.Count == 0 && !target.EquippedSkills.IsAvailable)
        {
            var evidence = new TargetProfileEvidence(
                "SAVE:TARGET_LOADOUT",
                TargetProfileEvidenceSourceKind.SavedLoadoutSource,
                $"TARGET:{target.CharacterId}:EQUIPPED_SKILLS",
                sourceVersion);
            facets.Add(TargetProfileFacet.Incomplete(
                identity,
                [evidence],
                new TargetProfileUnavailableReason(
                    "ACTIVE_SKILL_BINDING_UNAVAILABLE")));
        }
    }

    private static void AddResistanceFacet(
        TargetCombatSnapshot target,
        TargetProfileFacetIdentity identity,
        TargetProfileVersion sourceVersion,
        ICollection<TargetProfileFacet> facets)
    {
        var evidence = new TargetProfileEvidence(
            "SAVE:BASE_CHANNEL_RESISTANCE",
            TargetProfileEvidenceSourceKind.SavedBaseCharacter,
            $"TARGET:{target.CharacterId}:BASE_CHANNEL_RESISTANCE",
            sourceVersion);
        if (!target.BaseChannelResistance.IsAvailable)
        {
            facets.Add(TargetProfileFacet.Incomplete(
                identity,
                [evidence],
                new TargetProfileUnavailableReason(
                    "BASE_CHANNEL_RESISTANCE_UNAVAILABLE")));
            return;
        }

        var resistance = target.BaseChannelResistance.Value;
        if (!resistance.IsAsymmetric)
        {
            return;
        }

        facets.Add(TargetProfileFacet.Confirmed(
            identity,
            TargetProfileFacetValue.Measured(
                identity.Dimension,
                identity.Code,
                [
                    new TargetProfileMeasurement(
                        "OUTER",
                        resistance.Outer,
                        "RAW_GAME_UNIT"),
                    new TargetProfileMeasurement(
                        "INNER",
                        resistance.Inner,
                        "RAW_GAME_UNIT")
                ]),
            [evidence]));
    }

    private static void AddThreatFacets(
        TargetThreatAnalysis threatAnalysis,
        TargetProfileExtractionRuleSet rules,
        TargetProfileVersion sourceVersion,
        ICollection<TargetProfileFacet> facets,
        ICollection<TargetProfileDiagnostic> diagnostics)
    {
        foreach (var rule in rules.ThreatFacetRules)
        {
            var threats = threatAnalysis.Threats
                .Where(value => value.Threat.Kind == rule.ThreatKind)
                .ToArray();
            var activeSources = threats
                .SelectMany(value => value.Sources.Select(source => (
                    value.Threat,
                    Source: source)))
                .Where(value => value.Source.Scope
                    != TargetThreatSourceScope.LearnedUnequipped)
                .ToArray();
            if (activeSources.Length > 0)
            {
                var evidence = activeSources.Select(value =>
                    new TargetProfileEvidence(
                        $"THREAT:{value.Threat.Code}",
                        TargetProfileEvidenceSourceKind.VerifiedRule,
                        $"THREAT:{rule.ThreatKind.ToString().ToUpperInvariant()}"
                        + $":SKILL:{value.Source.SkillId}"
                        + $":EFFECT:{value.Source.RawEffectId}"
                        + $":SCOPE:{(int)value.Source.Scope}",
                        sourceVersion));
                facets.Add(TargetProfileFacet.Confirmed(
                    rule.Facet,
                    TargetProfileFacetValue.Presence(
                        rule.Facet.Dimension,
                        rule.Facet.Code),
                    evidence.DistinctBy(
                        value => value.StableKey,
                        StringComparer.Ordinal)));
                continue;
            }

            if (threats.Any(value => value.Sources.Any(source =>
                    source.Scope
                    == TargetThreatSourceScope.LearnedUnequipped)))
            {
                diagnostics.Add(new TargetProfileDiagnostic(
                    LearnedThreatNotActiveCode,
                    TargetProfileDiagnosticSeverity.Information,
                    rule.Facet,
                    threats.Select(value => $"THREAT:{value.Threat.Code}")
                        .Distinct(StringComparer.Ordinal)));
            }
        }
    }

    private static void AddSnapshotDiagnostics(
        CombatSnapshot snapshot,
        ICollection<TargetProfileDiagnostic> diagnostics)
    {
        foreach (var code in snapshot.Warnings
                     .Select(warning => warning.Code)
                     .Where(code => code.StartsWith(
                             "TARGET_OBSERVATION_",
                             StringComparison.Ordinal)
                         || code == CombatSnapshotWarningCodes
                             .TargetLoadoutNotPersisted)
                     .Distinct(StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            diagnostics.Add(new TargetProfileDiagnostic(
                code,
                TargetProfileDiagnosticSeverity.Warning,
                facet: null));
        }
    }

    private static string ObservationReference(string reference)
    {
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(reference)));
        return $"OBSERVATION:{hash}";
    }

    private sealed record ActiveSkillBinding(
        int SkillId,
        TargetProfileEvidence Evidence);
}
