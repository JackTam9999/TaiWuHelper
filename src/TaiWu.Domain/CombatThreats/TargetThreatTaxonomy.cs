namespace TaiWu.Domain.CombatThreats;

public static class TargetThreatTaxonomy
{
    public const string UnrecognizedMechanicWarningCode =
        "UNRECOGNIZED_TARGET_MECHANIC";

    public static TargetThreatSet Normalize(
        IEnumerable<TargetThreat> recognizedThreats,
        IEnumerable<UnknownTargetMechanic> unknownMechanics)
    {
        ArgumentNullException.ThrowIfNull(recognizedThreats);
        ArgumentNullException.ThrowIfNull(unknownMechanics);

        var threats = recognizedThreats.ToArray();
        var unknowns = unknownMechanics.ToArray();
        if (threats.Any(threat => threat is null))
        {
            throw new ArgumentException(
                "Recognized threats cannot contain null entries.",
                nameof(recognizedThreats));
        }

        if (unknowns.Any(mechanic => mechanic is null))
        {
            throw new ArgumentException(
                "Unknown mechanics cannot contain null entries.",
                nameof(unknownMechanics));
        }

        var duplicateCode = threats
            .GroupBy(threat => threat.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCode is not null)
        {
            throw new ArgumentException(
                $"Duplicate target-threat code "
                + $"'{duplicateCode.Key}'.",
                nameof(recognizedThreats));
        }

        var warnings = unknowns.Select(
            mechanic => new TargetThreatWarning(
                UnrecognizedMechanicWarningCode,
                $"Unrecognized target mechanic: "
                + $"{mechanic.Description}",
                mechanic));

        return new TargetThreatSet(threats, warnings);
    }
}
