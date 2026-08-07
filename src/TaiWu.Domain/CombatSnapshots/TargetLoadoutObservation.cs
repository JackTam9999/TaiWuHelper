using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed class TargetLoadoutObservation :
    IEquatable<TargetLoadoutObservation>
{
    public TargetLoadoutObservation(
        int targetCharacterId,
        TargetObservationContext observationContext,
        DateTimeOffset observedAt,
        string evidenceReference,
        TargetLoadoutCoverage coverage,
        IEnumerable<ObservedTargetCombatSkill> observedSkills)
    {
        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCharacterId),
                targetCharacterId,
                "Target character ID must be greater than zero.");
        }

        if (!Enum.IsDefined(observationContext))
        {
            throw new ArgumentOutOfRangeException(
                nameof(observationContext),
                observationContext,
                "Unknown target-observation context.");
        }

        if (observationContext != TargetObservationContext.Sparring)
        {
            throw new ArgumentException(
                "The supported UI exposes target loadouts only during "
                + "sparring; hostile and story targets are unavailable.",
                nameof(observationContext));
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A target-loadout observation requires an evidence reference.",
                nameof(evidenceReference));
        }

        ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(observedSkills);

        var skillValues = observedSkills.ToImmutableArray();
        if (skillValues.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Observed target skills cannot contain null entries.",
                nameof(observedSkills));
        }

        var duplicateSkill = skillValues
            .GroupBy(skill => skill.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSkill is not null)
        {
            throw new ArgumentException(
                $"Observed target skill {duplicateSkill.Key} is duplicated.",
                nameof(observedSkills));
        }

        var duplicateSlot = skillValues
            .Where(skill => skill.SlotIndex.HasValue)
            .GroupBy(skill => (skill.Category, skill.SlotIndex))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSlot is not null)
        {
            throw new ArgumentException(
                "Observed target slot "
                + $"{duplicateSlot.Key.Category}:"
                + $"{duplicateSlot.Key.SlotIndex} is duplicated.",
                nameof(observedSkills));
        }

        TargetCharacterId = targetCharacterId;
        ObservationContext = observationContext;
        ObservedAtUtc = observedAt.ToUniversalTime();
        EvidenceReference = SnapshotFieldSource.NormalizeEvidenceReference(
            evidenceReference);
        Coverage = coverage;
        ObservedSkills = skillValues;
    }

    public int TargetCharacterId { get; }

    public TargetObservationContext ObservationContext { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public string EvidenceReference { get; }

    public TargetLoadoutCoverage Coverage { get; }

    public ImmutableArray<ObservedTargetCombatSkill> ObservedSkills { get; }

    public bool EstablishesAbsenceOf(int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        return Coverage.CanEstablishAbsence
            && ObservedSkills.All(skill => skill.SkillId != skillId);
    }

    public bool Equals(TargetLoadoutObservation? other)
    {
        return ReferenceEquals(this, other)
            || (other is not null
                && TargetCharacterId == other.TargetCharacterId
                && ObservationContext == other.ObservationContext
                && ObservedAtUtc == other.ObservedAtUtc
                && string.Equals(
                    EvidenceReference,
                    other.EvidenceReference,
                    StringComparison.Ordinal)
                && Coverage == other.Coverage
                && ObservedSkills.SequenceEqual(other.ObservedSkills));
    }

    public override bool Equals(object? obj) =>
        obj is TargetLoadoutObservation other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TargetCharacterId);
        hash.Add(ObservationContext);
        hash.Add(ObservedAtUtc);
        hash.Add(EvidenceReference, StringComparer.Ordinal);
        hash.Add(Coverage);
        foreach (var skill in ObservedSkills)
        {
            hash.Add(skill);
        }

        return hash.ToHashCode();
    }
}
