using System.Collections.Immutable;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record ShopManagerTarget
{
    public ShopManagerTarget(
        ShopManagerTargetIdentity identity,
        LifeSkillDisciplineIdentity requiredDiscipline,
        IEnumerable<WorkforceEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        RequiredDiscipline = requiredDiscipline
            ?? throw new ArgumentNullException(nameof(requiredDiscipline));
        Evidence = CopyEvidence(evidence);
        Fingerprint = WorkforceText.Fingerprint(StableKey);
    }

    public ShopManagerTargetIdentity Identity { get; }

    public LifeSkillDisciplineIdentity RequiredDiscipline { get; }

    public ImmutableArray<WorkforceEvidenceReference> Evidence { get; }

    public string Fingerprint { get; }

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        RequiredDiscipline.StableKey,
        string.Join("||", Evidence.Select(item => item.StableKey)));

    private static ImmutableArray<WorkforceEvidenceReference> CopyEvidence(
        IEnumerable<WorkforceEvidenceReference> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var copied = evidence.ToImmutableArray();
        if (copied.IsEmpty || copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "A shop-manager target requires non-null evidence.",
                nameof(evidence));
        }

        if (copied.GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A shop-manager target cannot contain duplicate evidence.",
                nameof(evidence));
        }

        return [.. copied.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
    }
}

public sealed record CurrentShopManagerAssignment
{
    public CurrentShopManagerAssignment(
        ShopManagerTargetIdentity target,
        VillageWorkerIdentity worker,
        WorkforceProvenance provenance)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        Provenance = provenance
            ?? throw new ArgumentNullException(nameof(provenance));
        if (provenance.SourceKind
            != WorkforceEvidenceSourceKind.ConfiguredSave)
        {
            throw new ArgumentException(
                "A current assignment must come from the configured save.",
                nameof(provenance));
        }
    }

    public ShopManagerTargetIdentity Target { get; }

    public VillageWorkerIdentity Worker { get; }

    public WorkforceProvenance Provenance { get; }

    public WorkforceAssignmentOrigin Origin =>
        WorkforceAssignmentOrigin.CurrentSave;

    internal string StableKey => string.Join('|',
        WorkforceText.EnumKey(Origin),
        Target.StableKey,
        Worker.StableKey,
        Provenance.StableKey);
}

public sealed record ProposedShopManagerAssignment
{
    public ProposedShopManagerAssignment(
        WorkforceResultIdentity resultIdentity,
        VillageWorkerIdentity worker)
    {
        ResultIdentity = resultIdentity
            ?? throw new ArgumentNullException(nameof(resultIdentity));
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
    }

    public WorkforceResultIdentity ResultIdentity { get; }

    public ShopManagerTargetIdentity Target => ResultIdentity.Target;

    public VillageWorkerIdentity Worker { get; }

    public WorkforceAssignmentOrigin Origin =>
        WorkforceAssignmentOrigin.ProposedHelper;

    public string Fingerprint => WorkforceText.Fingerprint(StableKey);

    internal string StableKey => string.Join('|',
        WorkforceText.EnumKey(Origin),
        ResultIdentity.StableKey,
        Worker.StableKey);
}
