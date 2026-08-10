using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed record TargetCombatProfileAnalysis
{
    public TargetCombatProfileAnalysis(
        TargetThreatAnalysis threatAnalysis,
        TargetCombatProfile profile,
        TargetArchetypeMatchSet archetypeMatches)
    {
        ThreatAnalysis = threatAnalysis
            ?? throw new ArgumentNullException(nameof(threatAnalysis));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        ArchetypeMatches = archetypeMatches
            ?? throw new ArgumentNullException(nameof(archetypeMatches));
        if (!string.Equals(
                profile.Fingerprint,
                archetypeMatches.ProfileFingerprint,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Archetype matches must reference the extracted profile.",
                nameof(archetypeMatches));
        }
    }

    public TargetThreatAnalysis ThreatAnalysis { get; }

    public TargetCombatProfile Profile { get; }

    public TargetArchetypeMatchSet ArchetypeMatches { get; }
}
