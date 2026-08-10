using System.Globalization;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookCompositionDiagnostic
{
    internal TargetPlaybookCompositionDiagnostic(
        string code,
        TargetArchetypeIdentity archetype,
        TargetArchetypeMatchState? matchState = null,
        TargetCounterPlaybookResolutionStatus? resolutionStatus = null)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        Archetype = archetype
            ?? throw new ArgumentNullException(nameof(archetype));
        if (matchState.HasValue && !Enum.IsDefined(matchState.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(matchState));
        }

        if (resolutionStatus.HasValue
            && !Enum.IsDefined(resolutionStatus.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(resolutionStatus));
        }

        if (matchState.HasValue == resolutionStatus.HasValue)
        {
            throw new ArgumentException(
                "A composition diagnostic must describe either a match "
                + "state or a catalogue resolution state.");
        }

        MatchState = matchState;
        ResolutionStatus = resolutionStatus;
    }

    public string Code { get; }

    public TargetArchetypeIdentity Archetype { get; }

    public TargetArchetypeMatchState? MatchState { get; }

    public TargetCounterPlaybookResolutionStatus? ResolutionStatus { get; }

    internal string StableKey => TargetProfileText.Stable(
        Code,
        Archetype.StableKey,
        MatchState.HasValue
            ? ((int)MatchState.Value).ToString(CultureInfo.InvariantCulture)
            : string.Empty,
        ResolutionStatus.HasValue
            ? ((int)ResolutionStatus.Value).ToString(
                CultureInfo.InvariantCulture)
            : string.Empty);
}
