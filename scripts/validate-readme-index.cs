#!/usr/bin/env dotnet
#:include Opinions.cs
#:property Nullable=enable
#:property TreatWarningsAsErrors=true

// Verifies that README.md indexes the repository in both directions: every file
// under opinions/ has a linked Scope bullet and a Repository layout entry, every
// directory under skills/ has a row in the Maintenance via skills table, and none
// of those three places points at something that no longer exists.
//
// A .NET 10 file-based app: no project, no build step, sharing its helpers with the
// other checks via #:include. Run it from the repository root:
//
//     dotnet run scripts/validate-readme-index.cs
//     ./scripts/validate-readme-index.cs      (the shebang above)
//
// Exit codes: 0 clean, 1 findings printed.

using System.Text.RegularExpressions;

const string ReadmePath = "README.md";
const string SkillsDirectory = "skills";

if (!File.Exists(ReadmePath))
{
    Console.WriteLine("README.md not found");
    return 1;
}

string readme = File.ReadAllText(ReadmePath).ReplaceLineEndings("\n");
List<string> errors = [];

string? scope = Section(readme, "Scope");
string? layout = Section(readme, "Repository layout");
string? skillsTable = Section(readme, "Maintenance via skills");

(string Name, string? Body)[] required =
[
    ("Scope", scope),
    ("Repository layout", layout),
    ("Maintenance via skills", skillsTable),
];

foreach ((string name, string? body) in required)
{
    if (body is null)
    {
        errors.Add($"README.md: '## {name}' section not found — the index checks below cannot run");
    }
}

string[] opinions = Opinions.Names();

if (opinions.Length == 0)
{
    errors.Add($"no markdown files found under {Opinions.DirectoryName}/");
}

HashSet<string> names = [.. opinions];
HashSet<string> paths = [.. opinions.Select(Opinions.PathOf)];

if (scope is not null)
{
    // Opinion paths the Scope section links to, in any Markdown link style.
    HashSet<string> linked =
    [
        .. Patterns.InlineOpinionLink().Matches(scope).Select(match => match.Groups[1].Value),
        .. Patterns.ReferenceOpinionLink().Matches(scope).Select(match => match.Groups[1].Value),
    ];

    foreach (string path in paths)
    {
        if (!linked.Contains(path))
        {
            errors.Add($"README.md: Scope has no bullet linking to {path} — add one when adding an opinion");
        }
    }

    foreach (string stale in linked.Except(paths).Order(StringComparer.Ordinal))
    {
        errors.Add($"README.md: Scope links to {stale}, which no longer exists — drop or repoint the bullet");
    }
}

if (layout is not null)
{
    HashSet<string> listed = TreeOpinions(layout);

    if (listed.Count == 0)
    {
        errors.Add(
            $"README.md: Repository layout tree has no `{Opinions.DirectoryName}/` node — the tree check cannot run");
    }

    foreach (string missing in names.Except(listed).Order(StringComparer.Ordinal))
    {
        errors.Add($"README.md: Repository layout tree does not list {Opinions.PathOf(missing)}");
    }

    foreach (string stale in listed.Except(names).Order(StringComparer.Ordinal))
    {
        errors.Add(
            $"README.md: Repository layout tree lists {Opinions.PathOf(stale)}, which no longer exists");
    }
}

HashSet<string> skills = [];
if (Directory.Exists(SkillsDirectory))
{
    skills = [.. new DirectoryInfo(SkillsDirectory).EnumerateDirectories().Select(directory => directory.Name)];
}

if (skills.Count == 0)
{
    errors.Add($"no skill directories found under {SkillsDirectory}/");
}

if (skillsTable is not null)
{
    HashSet<string> rows = TableIds(skillsTable);

    if (rows.Count == 0)
    {
        errors.Add("README.md: no skill rows parsed from the 'Maintenance via skills' table");
    }

    foreach (string missing in skills.Except(rows).Order(StringComparer.Ordinal))
    {
        errors.Add($"README.md: 'Maintenance via skills' table has no row for `{missing}`");
    }

    foreach (string stale in rows.Except(skills).Order(StringComparer.Ordinal))
    {
        errors.Add(
            $"README.md: 'Maintenance via skills' table has a row for `{stale}`, which is not a skill directory");
    }
}

if (errors.Count > 0)
{
    Console.WriteLine("README index validation failed:");
    foreach (string error in errors)
    {
        Console.WriteLine($"  - {error}");
    }

    return 1;
}

Console.WriteLine($"README indexes all {opinions.Length} opinion files and {skills.Count} skills, with no stale entries.");
return 0;

// The body of a level-2 section, up to the next level-2 heading.
static string? Section(string text, string heading)
{
    Match match = Regex.Match(text, $@"^## {Regex.Escape(heading)}\s*$", RegexOptions.Multiline);
    if (!match.Success)
    {
        return null;
    }

    string rest = text[(match.Index + match.Length)..];
    Match next = Regex.Match(rest, "^## ", RegexOptions.Multiline);
    return next.Success ? rest[..next.Index] : rest;
}

// Files listed under the `opinions/` node of the repository layout tree.
static HashSet<string> TreeOpinions(string layout)
{
    HashSet<string> listed = [];
    bool inside = false;

    foreach (string line in layout.Split('\n'))
    {
        if (Patterns.TreeOpinionsNode().IsMatch(line))
        {
            inside = true;
            continue;
        }

        if (!inside)
        {
            continue;
        }

        if (Patterns.TreeNode().IsMatch(line) || line.StartsWith("```", StringComparison.Ordinal))
        {
            break;
        }

        Match match = Patterns.MarkdownFileName().Match(line);
        if (match.Success)
        {
            listed.Add(match.Groups[1].Value);
        }
    }

    return listed;
}

// First-cell `code` ids from the Markdown table rows in a section.
static HashSet<string> TableIds(string body)
{
    HashSet<string> ids = [];

    foreach (string line in body.Split('\n'))
    {
        if (!line.StartsWith('|'))
        {
            continue;
        }

        string first = line.Trim().Trim('|').Split('|')[0].Trim();
        Match match = Patterns.TableId().Match(first);
        if (match.Success)
        {
            ids.Add(match.Groups[1].Value);
        }
    }

    return ids;
}

internal static partial class Patterns
{
    // Inline link to an opinion file, with an optional #anchor and optional "title".
    [GeneratedRegex(@"]\(\s*(opinions/[\w.-]+\.md)(?:#[^)\s]*)?(?:\s+""[^""]*"")?\s*\)")]
    internal static partial Regex InlineOpinionLink();

    // Reference-style link definition: [label]: opinions/x.md
    [GeneratedRegex(@"^\s*\[[^\]]+]:\s*(opinions/[\w.-]+\.md)", RegexOptions.Multiline)]
    internal static partial Regex ReferenceOpinionLink();

    [GeneratedRegex(@"^[├└]── opinions/")]
    internal static partial Regex TreeOpinionsNode();

    [GeneratedRegex(@"^[├└]── ")]
    internal static partial Regex TreeNode();

    [GeneratedRegex(@"([\w.-]+\.md)")]
    internal static partial Regex MarkdownFileName();

    // A first cell is a bare id — `vet-source` — or a linked one:
    // [`vet-source`](skills/vet-source/SKILL.md).
    [GeneratedRegex(@"\A(?:\[\s*)?`([\w.-]+)`(?:\s*\]\([^)]*\))?\z")]
    internal static partial Regex TableId();
}
