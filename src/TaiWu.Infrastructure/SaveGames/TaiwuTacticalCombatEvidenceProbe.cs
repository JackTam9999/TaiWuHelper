using GameData.ArchiveData;
using GameData.Domains;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.SaveGames;

internal static class TaiwuTacticalCombatEvidenceProbe
{
    private static readonly TacticalSkillExpectation[] CandidateExpectations =
    [
        new(134, PracticeDirection.Reverse, 247, 973),
        new(267, PracticeDirection.Direct, 165, 891),
        new(291, PracticeDirection.Reverse, 189, 915),
        new(604, PracticeDirection.Reverse, 338, 1064),
        new(611, PracticeDirection.Reverse, 439, 1165),
        new(624, PracticeDirection.Reverse, 508, 1234),
        new(686, PracticeDirection.Reverse, 696, 1422)
    ];

    private static readonly TacticalSkillExpectation[]
        DirectMagicSoundExpectations =
    [
        new(718, PracticeDirection.Direct, 668, 1394),
        new(719, PracticeDirection.Direct, 669, 1395),
        new(720, PracticeDirection.Direct, 670, 1396),
        new(721, PracticeDirection.Direct, 671, 1397),
        new(722, PracticeDirection.Direct, 672, 1398),
        new(723, PracticeDirection.Direct, 673, 1399),
        new(724, PracticeDirection.Direct, 674, 1400),
        new(725, PracticeDirection.Direct, 349, 1075),
        new(726, PracticeDirection.Direct, 350, 1076),
        new(727, PracticeDirection.Direct, 351, 1077),
        new(728, PracticeDirection.Direct, 352, 1078),
        new(729, PracticeDirection.Direct, 353, 1079),
        new(730, PracticeDirection.Direct, 354, 1080),
        new(731, PracticeDirection.Direct, 355, 1081),
        new(732, PracticeDirection.Direct, 356, 1082),
        new(733, PracticeDirection.Direct, 357, 1083)
    ];

    internal static TacticalCombatEvidenceProbe Project(
        TaiwuArchiveReadContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var player = DomainManager.Character.GetElement_Objects(taiwuId);
        var learned = DomainManager.CombatSkill.GetCharCombatSkills(taiwuId);
        var equipment = player.GetCombatSkillEquipment();
        HashSet<short> equippedSkillIds = [];
        equipment.GetValidSkills(equippedSkillIds);

        List<string> signatureFacts = [];
        var configuredCandidates = 0;
        var matchingCandidateDefinitions = 0;
        var learnedCandidates = 0;
        var equippedCandidates = 0;
        var requiredDirectionReadyCandidates = 0;
        foreach (var expectation in CandidateExpectations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definition = Config.CombatSkill.Instance.GetItem(
                expectation.SkillId);
            if (definition is not null)
            {
                configuredCandidates++;
                if (definition.DirectEffectID == expectation.DirectEffectId
                    && definition.ReverseEffectID
                        == expectation.ReverseEffectId)
                {
                    matchingCandidateDefinitions++;
                }

                signatureFacts.Add(
                    $"candidate:{expectation.SkillId}:"
                    + $"{definition.DirectEffectID}:"
                    + $"{definition.ReverseEffectID}:"
                    + $"{definition.EquipType}:{definition.Type}:"
                    + $"{definition.GridCost}:{definition.FiveElements}");
            }

            if (!learned.TryGetValue(expectation.SkillId, out var skill))
            {
                continue;
            }

            learnedCandidates++;
            if (equippedSkillIds.Contains(expectation.SkillId))
            {
                equippedCandidates++;
            }

            var direction = CombatSnapshotMapping.MapActivePracticeDirection(
                skill.GetActivationState(),
                expectation.SkillId);
            if (direction.IsAvailable
                && direction.Value == expectation.RequiredDirection)
            {
                requiredDirectionReadyCandidates++;
            }

            signatureFacts.Add(
                $"player-candidate:{expectation.SkillId}:"
                + $"{(direction.IsAvailable ? direction.Value : null)}:"
                + equippedSkillIds.Contains(expectation.SkillId));
        }

        var configuredMagicSoundDefinitions = 0;
        var matchingMagicSoundDefinitions = 0;
        foreach (var expectation in DirectMagicSoundExpectations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definition = Config.CombatSkill.Instance.GetItem(
                expectation.SkillId);
            if (definition is null)
            {
                continue;
            }

            configuredMagicSoundDefinitions++;
            if (definition.DirectEffectID == expectation.DirectEffectId
                && definition.ReverseEffectID == expectation.ReverseEffectId)
            {
                matchingMagicSoundDefinitions++;
            }

            signatureFacts.Add(
                $"magic-sound:{expectation.SkillId}:"
                + $"{definition.DirectEffectID}:"
                + $"{definition.ReverseEffectID}:"
                + $"{definition.EquipType}:{definition.Type}");
        }

        var resetDefinition = Config.CombatSkill.Instance.GetItem(287);
        var resetDefinitionMatches = resetDefinition is
        {
            DirectEffectID: 185,
            ReverseEffectID: 911
        };
        signatureFacts.Add(
            $"reset:287:{resetDefinition?.DirectEffectID}:"
            + resetDefinition?.ReverseEffectID);

        var equippedWeapons = player.GetEquipment()
            .Where(item => item.ItemType == ItemType.Weapon
                && item.HasTemplate)
            .ToArray();
        var availableWeaponSubtypes = equippedWeapons.Count(item =>
            Config.Weapon.Instance.GetItem(item.TemplateId)?.ItemSubType > 0);
        var genericAllocation = DomainManager.Taiwu
            .GetGenericGridAllocation();
        var assignedGenericSlots = genericAllocation.Sum(value => (int)value);
        var legendaryBookAssignments = CountLegendaryBookAssignments(
            cancellationToken);

        signatureFacts.Add($"learned-count:{learned.Count}");
        signatureFacts.Add($"equipped-count:{equippedSkillIds.Count}");
        signatureFacts.Add($"weapons:{equippedWeapons.Length}");
        signatureFacts.Add(
            $"weapon-subtypes:{availableWeaponSubtypes}");
        signatureFacts.Add(
            $"generic-allocation:{genericAllocation.Length}:"
            + assignedGenericSlots);
        signatureFacts.Add(
            $"legendary-assignments:{legendaryBookAssignments}");

        return new TacticalCombatEvidenceProbe(
            GetGameDataVersion(),
            context.SourceFingerprint.Sha256,
            context.LoadWarning is not null,
            learned.Count,
            equippedSkillIds.Count,
            CandidateExpectations.Length,
            configuredCandidates,
            matchingCandidateDefinitions,
            learnedCandidates,
            equippedCandidates,
            requiredDirectionReadyCandidates,
            DirectMagicSoundExpectations.Length,
            configuredMagicSoundDefinitions,
            matchingMagicSoundDefinitions,
            resetDefinitionMatches,
            equippedWeapons.Length,
            availableWeaponSubtypes,
            genericAllocation.Length,
            assignedGenericSlots,
            legendaryBookAssignments,
            Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(string.Join('\n', signatureFacts)))));
    }

    internal static IReadOnlyList<int> ReadMindDamageSteps(
        IEnumerable<int> skillIds)
    {
        ArgumentNullException.ThrowIfNull(skillIds);
        return skillIds.Select(skillId => Config.CombatSkill.Instance
                .GetItem(checked((short)skillId))
                ?.MindDamageStep
            ?? throw new InvalidOperationException(
                $"Combat skill {skillId} is unavailable."))
            .ToArray();
    }

    private static int CountLegendaryBookAssignments(
        CancellationToken cancellationToken)
    {
        var count = 0;
        for (sbyte skillType = 0; skillType < 5; skillType++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DomainManager.Extra.TryGetElement_LegendaryBookSkillSlot(
                    skillType,
                    out var slots)
                && slots.Items is not null)
            {
                count += slots.Items.Count(skillId => skillId >= 0);
            }
        }

        return count;
    }

    private static string GetGameDataVersion()
    {
        var assembly = typeof(LocalArchiveFile).Assembly;
        return assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private sealed record TacticalSkillExpectation(
        short SkillId,
        PracticeDirection RequiredDirection,
        short DirectEffectId,
        short ReverseEffectId);
}

internal sealed record TacticalCombatEvidenceProbe(
    string GameDataVersion,
    string SaveSha256,
    bool HasLoadWarning,
    int LearnedSkillCount,
    int EquippedSkillCount,
    int CandidateExpectationCount,
    int ConfiguredCandidateCount,
    int MatchingCandidateDefinitionCount,
    int LearnedCandidateCount,
    int EquippedCandidateCount,
    int RequiredDirectionReadyCandidateCount,
    int MagicSoundExpectationCount,
    int ConfiguredMagicSoundDefinitionCount,
    int MatchingMagicSoundDefinitionCount,
    bool ResetDefinitionMatches,
    int EquippedWeaponCount,
    int AvailableWeaponSubtypeCount,
    int GenericAllocationValueCount,
    int AssignedGenericSlotCount,
    int LegendaryBookAssignmentCount,
    string Signature);
