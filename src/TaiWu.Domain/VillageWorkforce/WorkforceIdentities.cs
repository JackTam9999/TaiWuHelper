namespace TaiWu.Domain.VillageWorkforce;

public sealed record SettlementIdentity
{
    public SettlementIdentity(short settlementId)
    {
        if (settlementId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settlementId),
                settlementId,
                "A settlement identity cannot be negative.");
        }

        SettlementId = settlementId;
    }

    public short SettlementId { get; }

    internal string StableKey => WorkforceText.Number(SettlementId);
}

public sealed record VillageWorkerIdentity
{
    public VillageWorkerIdentity(int characterId)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "A worker identity must be positive.");
        }

        CharacterId = characterId;
    }

    public int CharacterId { get; }

    internal string StableKey => WorkforceText.Number(CharacterId);
}

public sealed record ShopBuildingIdentity
{
    public ShopBuildingIdentity(
        short areaId,
        short blockId,
        short buildingBlockIndex)
    {
        if (areaId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaId));
        }

        if (blockId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockId));
        }

        if (buildingBlockIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingBlockIndex));
        }

        AreaId = areaId;
        BlockId = blockId;
        BuildingBlockIndex = buildingBlockIndex;
    }

    public short AreaId { get; }

    public short BlockId { get; }

    public short BuildingBlockIndex { get; }

    internal string StableKey => string.Join(':',
        WorkforceText.Number(AreaId),
        WorkforceText.Number(BlockId),
        WorkforceText.Number(BuildingBlockIndex));
}

public sealed record ShopManagerTargetIdentity
{
    public ShopManagerTargetIdentity(
        ShopBuildingIdentity building,
        int managerSlotIndex)
    {
        Building = building ?? throw new ArgumentNullException(nameof(building));
        if (managerSlotIndex is < 0 or > sbyte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(managerSlotIndex),
                managerSlotIndex,
                "A manager slot must fit the installed signed-byte position range.");
        }

        ManagerSlotIndex = managerSlotIndex;
    }

    public ShopBuildingIdentity Building { get; }

    public int ManagerSlotIndex { get; }

    public WorkforceTargetKind Kind => WorkforceTargetKind.ShopManagerSlot;

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Kind)}:{Building.StableKey}:{WorkforceText.Number(ManagerSlotIndex)}";
}

public sealed record LifeSkillDisciplineIdentity
{
    public const sbyte MaximumSupportedType = 15;

    public LifeSkillDisciplineIdentity(sbyte type)
    {
        if (type is < 0 or > MaximumSupportedType)
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                $"A supported life-skill discipline must be between 0 and {MaximumSupportedType}.");
        }

        Type = type;
    }

    public sbyte Type { get; }

    internal string StableKey => WorkforceText.Number(Type);
}

public sealed record WorkforceObjectiveIdentity
{
    public WorkforceObjectiveIdentity(
        WorkforceObjectiveKind kind,
        string version)
    {
        WorkforceText.Defined(kind, nameof(kind));
        Kind = kind;
        Version = WorkforceText.Version(version, nameof(version));
    }

    public WorkforceObjectiveKind Kind { get; }

    public string Version { get; }

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Kind)}:{Version}";
}

public sealed record WorkforceRuleVersion
{
    public WorkforceRuleVersion(string value)
    {
        Value = WorkforceText.SemanticVersion(value, nameof(value));
    }

    public string Value { get; }

    internal string StableKey => Value;
}

public sealed record WorkforceRuleIdentity
{
    public WorkforceRuleIdentity(string value)
    {
        Value = WorkforceText.Stable(value, nameof(value));
    }

    public string Value { get; }

    internal string StableKey => Value;
}

public sealed record WorkforceRuleLimitation
{
    public WorkforceRuleLimitation(string identity)
    {
        Identity = WorkforceText.Stable(identity, nameof(identity));
    }

    public string Identity { get; }

    internal string StableKey => Identity;
}

public sealed record WorkforceSourceVersions
{
    public WorkforceSourceVersions(
        string saveSha256,
        string gameDataVersion,
        string mappingVersion,
        string candidateUniverseVersion,
        string fingerprintSchemaVersion)
    {
        SaveSha256 = WorkforceText.Sha256(
            saveSha256,
            nameof(saveSha256));
        GameDataVersion = WorkforceText.Version(
            gameDataVersion,
            nameof(gameDataVersion));
        MappingVersion = WorkforceText.Version(
            mappingVersion,
            nameof(mappingVersion));
        CandidateUniverseVersion = WorkforceText.Version(
            candidateUniverseVersion,
            nameof(candidateUniverseVersion));
        FingerprintSchemaVersion = WorkforceText.Version(
            fingerprintSchemaVersion,
            nameof(fingerprintSchemaVersion));
    }

    public string SaveSha256 { get; }

    public string GameDataVersion { get; }

    public string MappingVersion { get; }

    public string CandidateUniverseVersion { get; }

    public string FingerprintSchemaVersion { get; }

    internal string StableKey => string.Join('|',
        SaveSha256,
        GameDataVersion,
        MappingVersion,
        CandidateUniverseVersion,
        FingerprintSchemaVersion);
}

public sealed record WorkforceResultIdentity
{
    public WorkforceResultIdentity(
        string snapshotFingerprint,
        WorkforceObjectiveIdentity objective,
        WorkforceRuleVersion ruleVersion,
        ShopManagerTargetIdentity target)
    {
        SnapshotFingerprint = WorkforceText.Sha256(
            snapshotFingerprint,
            nameof(snapshotFingerprint));
        Objective = objective ?? throw new ArgumentNullException(nameof(objective));
        RuleVersion = ruleVersion
            ?? throw new ArgumentNullException(nameof(ruleVersion));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public string SnapshotFingerprint { get; }

    public WorkforceObjectiveIdentity Objective { get; }

    public WorkforceRuleVersion RuleVersion { get; }

    public ShopManagerTargetIdentity Target { get; }

    internal string StableKey => string.Join('|',
        SnapshotFingerprint,
        Objective.StableKey,
        RuleVersion.StableKey,
        Target.StableKey);
}
