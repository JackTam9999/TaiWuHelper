namespace TaiWu.Application.CombatSkills;

public interface IResolveTargetSkillSelection
{
    Task<TargetSkillSelectionResult> ExecuteAsync(
        TargetSkillSelectionRequest request,
        CancellationToken cancellationToken = default);
}
