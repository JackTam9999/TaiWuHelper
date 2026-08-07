namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSnapshotWarningCodes
{
    public const string TargetLoadoutNotPersisted =
        "TARGET_LOADOUT_NOT_PERSISTED";

    public const string TargetObservationNotNewer =
        "TARGET_OBSERVATION_NOT_NEWER";

    public const string TargetObservationSaveTimeConfirmationRequired =
        "TARGET_OBSERVATION_SAVE_TIME_CONFIRMATION_REQUIRED";

    public const string TargetObservationSaveTimeUnavailable =
        "TARGET_OBSERVATION_SAVE_TIME_UNAVAILABLE";

    public const string TargetObservationUnsupportedVersion =
        "TARGET_OBSERVATION_UNSUPPORTED_VERSION";

    public const string TargetObservationPartial =
        "TARGET_OBSERVATION_PARTIAL";

    public const string TargetObservationSaveConflict =
        "TARGET_OBSERVATION_SAVE_CONFLICT";
}
