using System.Collections.Immutable;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonSkillCell
{
    public LoadoutComparisonSkillCell(
        LoadoutComparisonSkillIdentity identity,
        LoadoutComparisonValue<LoadoutComparisonMembership> membership,
        LoadoutComparisonValue<int> effectiveCost,
        IEnumerable<LoadoutComparisonSkillAction> actions)
    {
        Identity = identity
            ?? throw new ArgumentNullException(nameof(identity));
        Membership = membership
            ?? throw new ArgumentNullException(nameof(membership));
        EffectiveCost = effectiveCost
            ?? throw new ArgumentNullException(nameof(effectiveCost));
        ArgumentNullException.ThrowIfNull(actions);

        if (Membership.IsAvailable
            && !Enum.IsDefined(Membership.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(membership),
                Membership.Value,
                "Unknown comparison membership.");
        }

        if (EffectiveCost.IsAvailable && EffectiveCost.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveCost),
                EffectiveCost.Value,
                "An available effective cost must be positive.");
        }

        Actions = [.. actions];
        if (Actions.Any(action => action is null))
        {
            throw new ArgumentException(
                "Comparison actions cannot contain null entries.",
                nameof(actions));
        }

        if (!Membership.IsAvailable && !Actions.IsEmpty)
        {
            throw new ArgumentException(
                "An unavailable membership cannot claim manual actions.",
                nameof(actions));
        }

        var duplicate = Actions
            .GroupBy(action => action.Kind)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate comparison action {duplicate.Key}.",
                nameof(actions));
        }

        if (!Actions
                .Select(action => action.Kind)
                .SequenceEqual(
                    Actions.Select(action => action.Kind).Order()))
        {
            throw new ArgumentException(
                "Comparison actions must use canonical kind order.",
                nameof(actions));
        }
    }

    public LoadoutComparisonSkillIdentity Identity { get; }

    public LoadoutComparisonValue<LoadoutComparisonMembership> Membership
    {
        get;
    }

    public LoadoutComparisonValue<int> EffectiveCost { get; }

    public ImmutableArray<LoadoutComparisonSkillAction> Actions { get; }

    public bool HasRequiredChange => Membership.IsAvailable
        && Membership.Value is LoadoutComparisonMembership.Added
            or LoadoutComparisonMembership.Removed
        || !Actions.IsEmpty;
}
