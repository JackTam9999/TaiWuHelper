using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public enum TacticalPreparationCheckKind
{
    RemoveSkill,
    CompleteBreakthrough,
    AddSkill,
    ChangeDirection,
    Capacity,
    UniversalSlotAllocation,
    LegendaryCostAssignment,
    Equipment,
    Weapon,
    ExecutionContext,
    BeforeCombatRole
}

public enum TacticalFinishDisposition
{
    Supported,
    FallbackOnly,
    Unsupported
}

public sealed record TacticalPreparationCheck
{
    internal TacticalPreparationCheck(
        string identity,
        TacticalPreparationCheckKind kind,
        string manualActionIdentity,
        SkillCategory? category = null,
        int? skillId = null,
        PracticeDirection? direction = null)
    {
        Identity = TacticalCombatText.Code(identity, nameof(identity));
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        ManualActionIdentity = TacticalCombatText.Code(
            manualActionIdentity,
            nameof(manualActionIdentity));
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skillId));
        }

        if (category.HasValue && !Enum.IsDefined(category.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        if (direction.HasValue
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        var skillSpecific = Kind is TacticalPreparationCheckKind.RemoveSkill
            or TacticalPreparationCheckKind.CompleteBreakthrough
            or TacticalPreparationCheckKind.AddSkill
            or TacticalPreparationCheckKind.ChangeDirection
            or TacticalPreparationCheckKind.BeforeCombatRole;
        if (skillSpecific != (category.HasValue && skillId.HasValue)
            || (Kind is TacticalPreparationCheckKind.CompleteBreakthrough
                    or TacticalPreparationCheckKind.ChangeDirection
                    or TacticalPreparationCheckKind.BeforeCombatRole)
                != direction.HasValue)
        {
            throw new ArgumentException(
                "Skill preparation checks require category and skill, and direction changes require an exact direction.");
        }

        Category = category;
        SkillId = skillId;
        Direction = direction;
    }

    public string Identity { get; }

    public TacticalPreparationCheckKind Kind { get; }

    public string ManualActionIdentity { get; }

    public SkillCategory? Category { get; }

    public int? SkillId { get; }

    public PracticeDirection? Direction { get; }

    internal string StableKey => Identity;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Kind),
        ManualActionIdentity,
        Category?.ToString().ToUpperInvariant() ?? "NONE",
        SkillId?.ToString(CultureInfo.InvariantCulture) ?? "NONE",
        Direction?.ToString().ToUpperInvariant() ?? "NONE");
}

public sealed record TacticalPlanCompilationRequest
{
    public TacticalPlanCompilationRequest(
        TacticalCombatScoringRequest scoringRequest,
        TacticalCombatScoringResult scoringResult,
        string selectedLoadoutStableKey)
    {
        ScoringRequest = scoringRequest
            ?? throw new ArgumentNullException(nameof(scoringRequest));
        ScoringResult = scoringResult
            ?? throw new ArgumentNullException(nameof(scoringResult));
        SelectedLoadoutStableKey = TacticalCombatText.Stable(
            selectedLoadoutStableKey,
            nameof(selectedLoadoutStableKey));
        if (!string.Equals(
                ScoringRequest.SearchResult.SemanticFingerprint,
                ScoringResult.SearchSemanticFingerprint,
                StringComparison.Ordinal)
            || ScoringRequest.Policy != ScoringResult.Weights.Policy)
        {
            throw new ArgumentException(
                "Plan compilation requires the exact scoring request and result.",
                nameof(scoringResult));
        }

        if (ScoringResult.RankedCandidates.All(item => !string.Equals(
                item.Candidate.StableKey,
                SelectedLoadoutStableKey,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "The selected loadout must be an exact scored feasible result.",
                nameof(selectedLoadoutStableKey));
        }
    }

    public TacticalCombatScoringRequest ScoringRequest { get; }

    public TacticalCombatScoringResult ScoringResult { get; }

    public string SelectedLoadoutStableKey { get; }
}

public sealed class TacticalCompiledCombatPlan
{
    internal TacticalCompiledCombatPlan(
        TacticalScoredLoadout selectedLoadout,
        TacticalCombatPlan plan,
        TacticalFinishDisposition finishDisposition,
        IEnumerable<TacticalPreparationCheck> preparationChecks,
        string contextSemanticFingerprint,
        string searchSemanticFingerprint,
        string scoringSemanticFingerprint,
        string observationRevisionFingerprint)
    {
        SelectedLoadout = selectedLoadout
            ?? throw new ArgumentNullException(nameof(selectedLoadout));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        FinishDisposition = TacticalCombatText.Defined(
            finishDisposition,
            nameof(finishDisposition));
        PreparationChecks = TacticalCombatText.CopyUnique(
            preparationChecks,
            item => item.StableKey,
            "preparation check",
            nameof(preparationChecks));
        ContextSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            contextSemanticFingerprint,
            nameof(contextSemanticFingerprint));
        SearchSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            searchSemanticFingerprint,
            nameof(searchSemanticFingerprint));
        ScoringSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            scoringSemanticFingerprint,
            nameof(scoringSemanticFingerprint));
        ObservationRevisionFingerprint = TacticalCombatText.ValidateFingerprint(
            observationRevisionFingerprint,
            nameof(observationRevisionFingerprint));
        if (PreparationChecks.IsEmpty
            || Plan.Stages.Single(item => item.Stage == TacticalPlanStage.Preparation)
                .Steps.Length != PreparationChecks.Length)
        {
            throw new ArgumentException(
                "Every preparation check must compile to exactly one preparation step.",
                nameof(preparationChecks));
        }

        ValidateFinishDisposition();
        SelectedLoadoutFingerprint = CreateSelectedLoadoutFingerprint();
        SemanticFingerprint = CreateFingerprint();
    }

    public TacticalScoredLoadout SelectedLoadout { get; }

    public TacticalCombatPlan Plan { get; }

    public TacticalFinishDisposition FinishDisposition { get; }

    public ImmutableArray<TacticalPreparationCheck> PreparationChecks { get; }

    public string ContextSemanticFingerprint { get; }

    public string SearchSemanticFingerprint { get; }

    public string ScoringSemanticFingerprint { get; }

    public string ObservationRevisionFingerprint { get; }

    public string SelectedLoadoutFingerprint { get; }

    public string SemanticFingerprint { get; }

    private void ValidateFinishDisposition()
    {
        var finish = Plan.Stages.Single(item =>
            item.Stage == TacticalPlanStage.Finish);
        var fallback = Plan.Stages.Single(item =>
            item.Stage == TacticalPlanStage.Fallback);
        var valid = FinishDisposition switch
        {
            TacticalFinishDisposition.Supported =>
                finish.State == TacticalPlanStageState.Supported,
            TacticalFinishDisposition.FallbackOnly =>
                finish.State == TacticalPlanStageState.Unsupported
                && fallback.State == TacticalPlanStageState.Supported,
            TacticalFinishDisposition.Unsupported =>
                finish.State == TacticalPlanStageState.Unsupported
                && fallback.State == TacticalPlanStageState.Unsupported,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException(
                "Finish disposition must match the explicit finish and fallback stages.",
                nameof(FinishDisposition));
        }
    }

    private string CreateSelectedLoadoutFingerprint()
    {
        var candidate = SelectedLoadout.Candidate;
        var proposal = candidate.Loadout.Proposal;
        var canonical = new StringBuilder()
            .Append("TACTICAL_SELECTED_LOADOUT_V1\n")
            .Append(candidate.StableKey).Append('\n');
        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            canonical.Append(category.ToString().ToUpperInvariant())
                .Append('|')
                .AppendJoin(',', proposal.Skills.Get(category))
                .Append('\n');
        }

        canonical.Append("UNIVERSAL|")
            .Append(proposal.GenericSlotAllocation.TotalSlots).Append('|')
            .Append(proposal.GenericSlotAllocation.Attack).Append('|')
            .Append(proposal.GenericSlotAllocation.Agility).Append('|')
            .Append(proposal.GenericSlotAllocation.Defense).Append('|')
            .Append(proposal.GenericSlotAllocation.Assistance).Append('\n');
        foreach (var value in proposal.SkillCandidates.OrderBy(item => item.SkillId))
        {
            canonical.Append("CANDIDATE|")
                .Append(value.SkillId).Append('|')
                .Append(value.RequiredDirection?.ToString().ToUpperInvariant()
                    ?? "NONE")
                .Append('|').Append(value.AllowDirectionChange)
                .Append('|').Append(value.AllowBreakthrough).Append('\n');
        }

        foreach (var value in proposal.LegendaryCostAssignments)
        {
            canonical.Append("LEGENDARY|")
                .Append(value.SkillId).Append('|')
                .Append(value.Slot.SlotReference).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_COMPILED_PLAN_V1\n")
            .Append(ContextSemanticFingerprint).Append('\n')
            .Append(ObservationRevisionFingerprint).Append('\n')
            .Append(SearchSemanticFingerprint).Append('\n')
            .Append(ScoringSemanticFingerprint).Append('\n')
            .Append(SelectedLoadoutFingerprint).Append('\n')
            .Append(Plan.Fingerprint).Append('\n')
            .Append(TacticalCombatText.EnumKey(FinishDisposition)).Append('\n');
        foreach (var check in PreparationChecks)
        {
            canonical.Append("PREPARATION|")
                .Append(check.ContentKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }
}
