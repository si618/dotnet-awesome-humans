#!/usr/bin/env dotnet
#:package YamlDotNet@18.1.0
#:include Opinions.cs
#:include Frontmatter.cs
// PublishAot is on by default for file-based apps, and YamlDotNet's reflection-based
// builder trips its analyzer (IL3050). These scripts are run, never published, so turn
// AOT off rather than suppress the diagnostic that is telling the truth.
#:property PublishAot=false
#:property Nullable=enable
#:property TreatWarningsAsErrors=true

// Verifies that every source id in an opinion's frontmatter resolves to the roster
// in AWESOME-HUMANS.md, and that it is allowed to feed an opinion: watch-list
// sources have not been admitted yet, and discovery-only sources are channels that
// lead you to a primary source rather than being one.
//
// A .NET 10 file-based app: no project, no build step, sharing its helpers with the
// other checks via #:include. Run it from the repository root:
//
//     dotnet run scripts/validate-opinion-sources.cs
//     ./scripts/validate-opinion-sources.cs      (the shebang above)
//
// Exit codes: 0 clean, 1 findings printed, 2 not run from the repository root.

using System.Collections;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

const string RosterPath = "AWESOME-HUMANS.md";
const string DiscoveryMarker = "**Discovery-only.**";
const string WatchBucket = "watch";

if (!Opinions.DirectoryExists)
{
    Console.Error.WriteLine($"No {Opinions.DirectoryName}/ directory here — run this script from the repository root.");
    return 2;
}

if (!File.Exists(RosterPath))
{
    Console.WriteLine($"{RosterPath} not found");
    return 1;
}

// The reserved id for house opinions: the repository owner is not on the roster.
HashSet<string> reserved = ["house"];

// Which level-2 headings open which bucket. Anything outside them — the admission
// criteria, the decision log — carries no source rows.
(string Bucket, string[] Prefixes)[] sections =
[
    ("citable", ["## Tier 1", "## Tier 2"]),
    (WatchBucket, ["## Watch list"]),
];

// Parse the roster: source id -> (bucket, the row's Notes/Blocker cell).
Dictionary<string, (string Bucket, string Notes)> roster = new(StringComparer.Ordinal);
string? bucket = null;

foreach (string line in File.ReadAllText(RosterPath).ReplaceLineEndings("\n").Split('\n'))
{
    if (line.StartsWith("## ", StringComparison.Ordinal))
    {
        bucket = null;
        foreach ((string name, string[] prefixes) in sections)
        {
            if (Array.Exists(prefixes, prefix => line.StartsWith(prefix, StringComparison.Ordinal)))
            {
                bucket = name;
            }
        }

        continue;
    }

    if (bucket is null || !line.StartsWith('|'))
    {
        continue;
    }

    string[] cells = [.. line.Trim().Trim('|').Split('|').Select(cell => cell.Trim())];
    if (cells.Length < 5)
    {
        continue;
    }

    Match id = Patterns.SourceId().Match(cells[0]);
    if (!id.Success)
    {
        continue; // header or separator row
    }

    roster[id.Groups[1].Value] = (bucket, cells[^1]);
}

List<string> errors = [];

if (roster.Count == 0)
{
    errors.Add($"{RosterPath}: no source rows parsed — roster tables may have changed shape");
}

HashSet<string> discovery =
[
    .. roster
        .Where(entry => entry.Value.Notes.Contains(DiscoveryMarker, StringComparison.Ordinal))
        .Select(entry => entry.Key),
];

IDeserializer deserializer = new DeserializerBuilder().Build();

foreach (string name in Opinions.Names())
{
    string path = Opinions.PathOf(name);

    // A file with no frontmatter, or unreadable frontmatter, is reported in detail by
    // validate-opinion-frontmatter.cs — this check stays quiet about it.
    if (Frontmatter.Read(deserializer, path, out _) is not IDictionary frontmatter)
    {
        continue;
    }

    object? declared = frontmatter.Contains("sources") ? frontmatter["sources"] : null;
    if (declared is null or "")
    {
        continue;
    }

    if (declared is not IList sources)
    {
        errors.Add($"{path}: 'sources' must be a list");
        continue;
    }

    foreach (object? entry in sources)
    {
        string sourceId = entry?.ToString() ?? string.Empty;

        if (reserved.Contains(sourceId))
        {
            continue;
        }

        if (!roster.TryGetValue(sourceId, out (string Bucket, string Notes) row))
        {
            errors.Add($"{path}: source '{sourceId}' is not on the roster in {RosterPath}");
            continue;
        }

        if (row.Bucket == WatchBucket)
        {
            errors.Add(
                $"{path}: source '{sourceId}' is on the watch list and may not feed opinions "
                    + "— corroborate with a Tier 1/2 source or admit it via vet-source");
        }
        else if (discovery.Contains(sourceId))
        {
            errors.Add(
                $"{path}: source '{sourceId}' is marked discovery-only and never appears in 'sources:' "
                    + "— cite the primary source it led you to");
        }
    }
}

if (errors.Count > 0)
{
    Console.WriteLine("Opinion source validation failed:");
    foreach (string error in errors)
    {
        Console.WriteLine($"  - {error}");
    }

    return 1;
}

Console.WriteLine(
    $"All opinion sources resolve to the roster ({roster.Count} sources, {discovery.Count} discovery-only).");
return 0;

internal static partial class Patterns
{
    // A roster row's first cell is a bare source id in backticks: `andrew-lock`.
    [GeneratedRegex(@"\A`([a-z0-9-]+)`\z")]
    internal static partial Regex SourceId();
}
