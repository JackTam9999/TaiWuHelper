using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed class TargetArchetypeMatchSet
{
    internal TargetArchetypeMatchSet(
        string profileFingerprint,
        IEnumerable<TargetArchetypeMatch> matches)
    {
        ProfileFingerprint = TargetProfileText.Fingerprint(
            profileFingerprint,
            nameof(profileFingerprint));
        ArgumentNullException.ThrowIfNull(matches);
        var values = matches.ToImmutableArray();
        if (values.Any(match => match is null))
        {
            throw new ArgumentException(
                "An archetype match set cannot contain null entries.",
                nameof(matches));
        }

        if (values.Any(match => !string.Equals(
                match.ProfileFingerprint,
                ProfileFingerprint,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Every archetype match must reference the same profile.",
                nameof(matches));
        }

        var duplicate = values
            .GroupBy(match => match.Definition.StableKey,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Archetype match {duplicate.Key} is duplicated.",
                nameof(matches));
        }

        Matches = [.. values
            .OrderBy(match => match.Definition.Identity.Code,
                StringComparer.Ordinal)
            .ThenBy(match => match.Definition.Identity.Version.Value,
                StringComparer.Ordinal)];
        StableKey = CreateStableKey();
    }

    public string ProfileFingerprint { get; }

    public ImmutableArray<TargetArchetypeMatch> Matches { get; }

    public string StableKey { get; }

    public ImmutableArray<TargetArchetypeMatch> Matched =>
        [.. Matches.Where(match =>
            match.State == TargetArchetypeMatchState.Matched)];

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_ARCHETYPE_MATCH_SET_V1",
            ProfileFingerprint,
            TargetProfileText.StableCollection(
                Matches.Select(match => match.StableKey)));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
