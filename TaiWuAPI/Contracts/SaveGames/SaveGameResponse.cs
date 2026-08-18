using System.Collections.Immutable;

namespace TaiWuAPI.Contracts.SaveGames;

public sealed record SaveGameResponse(
    string SchemaVersion,
    ImmutableArray<string> Lines,
    string LegacyText);
