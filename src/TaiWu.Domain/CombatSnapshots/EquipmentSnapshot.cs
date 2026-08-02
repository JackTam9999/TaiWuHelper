namespace TaiWu.Domain.CombatSnapshots;

public sealed record EquipmentSnapshot
{
    public EquipmentSnapshot(
        int slotIndex,
        SnapshotValue<long> instanceId,
        SnapshotValue<int> templateId,
        SnapshotValue<string> displayName,
        SnapshotValue<EquipmentKind> kind)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                "Equipment slot index cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(templateId);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(kind);

        if (instanceId.IsAvailable && instanceId.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(instanceId),
                "An available equipment instance ID cannot be negative.");
        }

        if (templateId.IsAvailable && templateId.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(templateId),
                "An available equipment template ID cannot be negative.");
        }

        if (kind.IsAvailable && !Enum.IsDefined(kind.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "Unknown equipment kind.");
        }

        SlotIndex = slotIndex;
        InstanceId = instanceId;
        TemplateId = templateId;
        DisplayName = displayName;
        Kind = kind;
    }

    public int SlotIndex { get; }

    public SnapshotValue<long> InstanceId { get; }

    public SnapshotValue<int> TemplateId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SnapshotValue<EquipmentKind> Kind { get; }
}
