using System.Collections.Immutable;
using System.Diagnostics;
using TaiWu.Application.CombatSkills;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed record CombatSkillRequirementSourceValue(
    int PropertyId,
    int RequiredValue,
    int SourceIndex);

internal sealed record CombatSkillSourceRecord(
    int SkillId,
    string? NameKey,
    string? DescriptionKey,
    int Category,
    int Grade,
    int Faction,
    int Element,
    int EquipmentType,
    int BaseGridCost,
    ImmutableArray<int> SpecificGrids,
    int GenericGrid,
    int PreparationProgress,
    int BreathStanceCost,
    int CastSpeed,
    int DirectEffectId,
    int ReverseEffectId,
    ImmutableArray<CombatSkillRequirementSourceValue> Requirements);

internal sealed record CombatSkillConfigurationReadResult(
    ImmutableArray<CombatSkillSourceRecord> Records,
    ImmutableArray<CombatSkillImportDiagnostic> Diagnostics);

internal interface ICombatSkillConfigurationReader
{
    string CompatibleGameDataVersion { get; }

    string LoadedConfigurationAssemblyPath { get; }

    CombatSkillConfigurationReadResult ReadAll(
        CancellationToken cancellationToken = default);
}

internal sealed class CombatSkillConfigurationReader
    : ICombatSkillConfigurationReader
{
    private static readonly object ConfigurationGate = new();

    public string CompatibleGameDataVersion =>
        FileVersionInfo.GetVersionInfo(LoadedConfigurationAssemblyPath)
            .ProductVersion
        ?? "unknown";

    public string LoadedConfigurationAssemblyPath =>
        typeof(Config.CombatSkill).Assembly.Location;

    public CombatSkillConfigurationReadResult ReadAll(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (ConfigurationGate)
        {
            var configuration = Config.CombatSkill.Instance;
            if (configuration.Count == 0)
            {
                configuration.Init();
            }

            List<CombatSkillSourceRecord> records = [];
            List<CombatSkillImportDiagnostic> diagnostics = [];
            foreach (var skillId in configuration.GetAllKeys().Order())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var item = configuration.GetItem(skillId)
                        ?? throw new InvalidDataException(
                            "The configuration key has no record.");
                    records.Add(Map(skillId, item));
                    if (item.TemplateId != skillId)
                    {
                        diagnostics.Add(new CombatSkillImportDiagnostic(
                            CombatSkillImportDiagnosticSeverity.Warning,
                            "CONFIGURATION_ID_MISMATCH",
                            $"combat-skill:{skillId}",
                            $"The record TemplateId {item.TemplateId} does "
                            + "not match its configuration key; the key is "
                            + "authoritative."));
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new CombatSkillImportDiagnostic(
                        CombatSkillImportDiagnosticSeverity.Error,
                        "CONFIGURATION_RECORD_READ_FAILED",
                        $"combat-skill:{skillId}",
                        exception.Message));
                }
            }

            return new CombatSkillConfigurationReadResult(
                records.OrderBy(record => record.SkillId).ToImmutableArray(),
                diagnostics
                    .OrderBy(
                        diagnostic => diagnostic.SourceRecordIdentity,
                        StringComparer.Ordinal)
                    .ToImmutableArray());
        }
    }

    private static CombatSkillSourceRecord Map(
        short skillId,
        Config.CombatSkillItem item) => new(
            skillId,
            item.Name,
            item.Desc,
            item.Type,
            item.Grade,
            item.SectId,
            item.FiveElements,
            item.EquipType,
            item.GridCost,
            (item.SpecificGrids ?? []).Select(value => (int)value)
                .ToImmutableArray(),
            item.GenericGrid,
            item.PrepareTotalProgress,
            item.BreathStanceTotalCost,
            item.CastSpeed,
            item.DirectEffectID,
            item.ReverseEffectID,
            (item.UsingRequirement ?? [])
                .Select((value, index) =>
                    new CombatSkillRequirementSourceValue(
                        value.PropertyId,
                        value.Value,
                        index))
                .ToImmutableArray());
}
