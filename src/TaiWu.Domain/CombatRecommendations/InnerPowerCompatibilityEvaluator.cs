using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public static class InnerPowerCompatibilityEvaluator
{
    public const string EvidenceReference =
        "snapshot:player:inner-power-state";

    public static InnerPowerCompatibilityEvaluation Evaluate(
        PlayerCombatSnapshot player,
        GeneratedCombatLoadout candidate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(candidate);

        var evaluations = candidate.SelectedOptions
            .Select(option => EvaluateActiveUse(player, option))
            .Where(evaluation => evaluation is not null)
            .Cast<InnerPowerSkillCompatibility>()
            .ToArray();
        if (evaluations.Length == 0)
        {
            return new InnerPowerCompatibilityEvaluation(
                SnapshotValue<decimal>.Available(100),
                evaluations: []);
        }

        var known = evaluations
            .Where(evaluation => evaluation.Score.HasValue)
            .ToArray();
        var scoreValue = known.Length == evaluations.Length
            ? SnapshotValue<decimal>.Available(
                known.Average(evaluation =>
                    (decimal)evaluation.Score!.Value))
            : SnapshotValue<decimal>.Unavailable(
                "One or more active skills have no mapped inner-power "
                + "state or element, so compatibility is incomplete.");
        return new InnerPowerCompatibilityEvaluation(
            scoreValue,
            evaluations);
    }

    public static InnerPowerSkillCompatibility? EvaluateActiveUse(
        PlayerCombatSnapshot player,
        CombatLoadoutOption option)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(option);
        if (!IsCast(option.ActivationTiming))
        {
            return null;
        }

        if (!player.InnerPowerState.IsAvailable)
        {
            return InnerPowerSkillCompatibility.Unavailable(
                option.Candidate.SkillId,
                player.InnerPowerState.UnavailableReason!);
        }

        var state = player.InnerPowerState.Value;
        var skill = player.LearnedSkills.SingleOrDefault(
            skill => skill.SkillId == option.Candidate.SkillId);
        if (skill is null)
        {
            return InnerPowerSkillCompatibility.Unavailable(
                option.Candidate.SkillId,
                "The skill is absent from the learned-skill snapshot.");
        }

        if (!skill.Element.IsAvailable)
        {
            return InnerPowerSkillCompatibility.Unavailable(
                skill.SkillId,
                skill.Element.UnavailableReason!);
        }

        var element = skill.Element.Value;
        var causesBacklash = state.BacklashOnUseElement == element;
        var maxPowerChange = state.MaxPowerChanges.Get(element);
        var requirementChange = state.RequirementChanges.Get(element);
        var score = causesBacklash
            ? 0
            : Math.Clamp(
                80 + maxPowerChange - requirementChange,
                0,
                100);
        return new InnerPowerSkillCompatibility(
            skill.SkillId,
            element,
            causesBacklash,
            maxPowerChange,
            requirementChange,
            score,
            UnavailableReason: null);
    }

    private static bool IsCast(
        CombatCounterActivationTiming? timing) => timing is
        CombatCounterActivationTiming.ActiveAttack
        or CombatCounterActivationTiming.ActiveAgility
        or CombatCounterActivationTiming.ActiveDefense;
}

public sealed record InnerPowerCompatibilityEvaluation
{
    public InnerPowerCompatibilityEvaluation(
        SnapshotValue<decimal> score,
        IEnumerable<InnerPowerSkillCompatibility> evaluations)
    {
        Score = score ?? throw new ArgumentNullException(nameof(score));
        ArgumentNullException.ThrowIfNull(evaluations);
        Evaluations = [.. evaluations];
    }

    public SnapshotValue<decimal> Score { get; }

    public ImmutableArray<InnerPowerSkillCompatibility> Evaluations
    { get; }
}

public sealed record InnerPowerSkillCompatibility(
    int SkillId,
    CombatSkillElement? Element,
    bool CausesBacklash,
    int? MaxPowerChange,
    int? RequirementChange,
    int? Score,
    string? UnavailableReason)
{
    internal static InnerPowerSkillCompatibility Unavailable(
        int skillId,
        string reason) => new(
            skillId,
            Element: null,
            CausesBacklash: false,
            MaxPowerChange: null,
            RequirementChange: null,
            Score: null,
            reason);
}
