using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TaiwuArchivePerformanceCollection
{
    public const string Name = "Taiwu archive performance";
}
