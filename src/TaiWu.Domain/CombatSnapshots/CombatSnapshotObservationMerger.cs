namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSnapshotObservationMerger
{
    public const string PlayerEquippedSkillsField =
        "player.equippedSkills";

    public const string PlayerGenericSlotAllocationField =
        "player.genericSlotAllocation";

    public const string PlayerSlotBudgetsField =
        "player.slotBudgets";

    public const string PlayerLegendaryBookCostSlotsField =
        "player.legendaryBookCostSlots";

    public const string PlayerLegendaryBookCostAssignmentsField =
        "player.legendaryBookCostAssignments";

    public static CombatSnapshot Merge(
        CombatSnapshot snapshot,
        PlayerLoadoutObservation observation)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(observation);

        if (snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable
            && observation.ObservedAtUtc
                <= snapshot.Metadata.SaveLastWriteTimeUtc.Value)
        {
            return CopyWithWarning(
                snapshot,
                new SnapshotWarning(
                    "CURRENT_SCREEN_OBSERVATION_NOT_NEWER",
                    "The current-screen observation was not applied because "
                    + "it is not newer than the disk save."));
        }

        ValidateObservedSkills(snapshot.Player, observation.EquippedSkills);

        var player = new PlayerCombatSnapshot(
            snapshot.Player.CharacterId,
            snapshot.Player.DisplayName,
            snapshot.Player.LearnedSkills,
            observation.EquippedSkills,
            snapshot.Player.Equipment,
            observation.DisplayedSlotBudgets
                ?? snapshot.Player.SlotBudgets,
            observation.GenericSlotAllocation,
            observation.LegendaryBookCostSlots
                ?? snapshot.Player.LegendaryBookCostSlots,
            observation.LegendaryBookCostAssignments
                ?? snapshot.Player.LegendaryBookCostAssignments);

        var observedFields = new List<string>
        {
            PlayerEquippedSkillsField,
            PlayerGenericSlotAllocationField
        };
        if (observation.DisplayedSlotBudgets is not null)
        {
            observedFields.Add(PlayerSlotBudgetsField);
        }

        if (observation.LegendaryBookCostSlots is not null)
        {
            observedFields.Add(PlayerLegendaryBookCostSlotsField);
            observedFields.Add(PlayerLegendaryBookCostAssignmentsField);
        }

        var retainedSources = snapshot.FieldSources
            .Where(source => !observedFields.Contains(source.FieldPath));
        var observationSources = observedFields.Select(
            fieldPath => new SnapshotFieldSource(
                fieldPath,
                SnapshotDataSource.CurrentScreenObservation,
                observation.ObservedAtUtc,
                observation.EvidenceReference));

        var warnings = observation.DisplayedSlotBudgets is null
            ? snapshot.Warnings
            : snapshot.Warnings.RemoveAll(warning =>
                warning.Code
                == "RUNTIME_SLOT_CAPACITY_MODIFIERS_NOT_EVALUATED");
        if (!snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable)
        {
            warnings = warnings.Add(
                new SnapshotWarning(
                    "SAVE_TIMESTAMP_UNAVAILABLE",
                    "The current-screen observation was applied using source "
                    + "precedence because the save timestamp is unavailable."));
        }

        return new CombatSnapshot(
            snapshot.Metadata,
            player,
            snapshot.Target,
            warnings,
            retainedSources.Concat(observationSources));
    }

    private static void ValidateObservedSkills(
        PlayerCombatSnapshot player,
        CombatLoadoutSnapshot observedLoadout)
    {
        var learnedById =
            player.LearnedSkills.ToDictionary(skill => skill.SkillId);

        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            foreach (var skillId in observedLoadout.Get(category))
            {
                if (!learnedById.TryGetValue(skillId, out var skill))
                {
                    throw new ArgumentException(
                        $"Observed skill {skillId} is not learned by the player.",
                        nameof(observedLoadout));
                }

                if (skill.Category != category)
                {
                    throw new ArgumentException(
                        $"Observed skill {skillId} belongs to "
                        + $"{skill.Category}, not {category}.",
                        nameof(observedLoadout));
                }
            }
        }
    }

    private static CombatSnapshot CopyWithWarning(
        CombatSnapshot snapshot,
        SnapshotWarning warning)
    {
        return new CombatSnapshot(
            snapshot.Metadata,
            snapshot.Player,
            snapshot.Target,
            snapshot.Warnings.Add(warning),
            snapshot.FieldSources);
    }
}
