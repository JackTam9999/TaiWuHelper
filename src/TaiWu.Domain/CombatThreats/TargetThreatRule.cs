using System.Collections.Immutable;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatRule
{
    public TargetThreatRule(
        TargetThreat threat,
        IEnumerable<TargetThreatSkillSignature> signatures)
    {
        Threat = threat ?? throw new ArgumentNullException(nameof(threat));
        ArgumentNullException.ThrowIfNull(signatures);

        Signatures = [.. signatures];
        if (Signatures.IsEmpty)
        {
            throw new ArgumentException(
                "A target-threat rule requires at least one signature.",
                nameof(signatures));
        }

        if (Signatures.Any(signature => signature is null))
        {
            throw new ArgumentException(
                "Target-threat signatures cannot contain null entries.",
                nameof(signatures));
        }

        var duplicate = Signatures
            .GroupBy(signature => (
                signature.SkillId,
                signature.Direction,
                signature.RawEffectId))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                "Target-threat signatures cannot be duplicated.",
                nameof(signatures));
        }
    }

    public TargetThreat Threat { get; }

    public ImmutableArray<TargetThreatSkillSignature> Signatures { get; }

    public bool Matches(
        int skillId,
        CombatSnapshots.PracticeDirection direction,
        int rawEffectId)
    {
        return Signatures.Any(
            signature => signature.SkillId == skillId
                && signature.Direction == direction
                && signature.RawEffectId == rawEffectId);
    }
}
