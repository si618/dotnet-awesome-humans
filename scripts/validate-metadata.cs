#!/usr/bin/env dotnet
#:package YamlDotNet@18.1.0
#:include Opinions.cs
#:include Frontmatter.cs
#:include CommentHeader.cs
// PublishAot is on by default for file-based apps, and YamlDotNet's reflection-based
// builder trips its analyzer (IL3050). These scripts are run, never published, so turn
// AOT off rather than suppress the diagnostic that is telling the truth.
#:property PublishAot=false
#:property Nullable=enable
#:property TreatWarningsAsErrors=true

// Verifies that every resource carries the four metadata fields AGENTS.md requires:
// targets, last-reviewed, last-used, sources — and that the two dates are ISO 8601.
//
// Three resource kinds carry them two ways. opinions/ and research/ use YAML
// frontmatter; templates/ use a first-line comment header, because an XML or INI file
// cannot open with a `---` block and stay valid for the tools that read it. Both are
// checked against the same required set here, so the two syntaxes cannot drift apart.
//
// A .NET 10 file-based app: no project, no build step, sharing its helpers with the
// other checks via #:include. Run it from the repository root:
//
//     dotnet run scripts/validate-metadata.cs
//     ./scripts/validate-metadata.cs      (the shebang above)
//
// Exit codes: 0 clean, 1 findings printed, 2 not run from the repository root.

using System.Collections;
using System.Globalization;
using YamlDotNet.Serialization;

if (!Opinions.DirectoryExists)
{
    Console.Error.WriteLine($"No {Opinions.DirectoryName}/ directory here — run this script from the repository root.");
    return 2;
}

const string TemplatesDirectory = "templates";

string[] required = ["targets", "last-reviewed", "last-used", "sources"];
string[] dateFields = ["last-reviewed", "last-used"];

List<string> errors = [];
IDeserializer deserializer = new DeserializerBuilder().Build();

List<(string Path, IDictionary? Fields, string? Error)> resources = [];

string[] opinions = Opinions.Names();
if (opinions.Length == 0)
{
    errors.Add($"no markdown files found under {Opinions.DirectoryName}/");
}

foreach (string name in opinions)
{
    string path = Opinions.PathOf(name);
    resources.Add((path, Frontmatter.Read(deserializer, path, out string? error), error));
}

// research/ is staging, and legitimately empty once every topic has been resolved —
// resolve-research deletes the file it finishes with. So an empty or absent directory
// is not a finding; a topic that is there carries the same four fields as an opinion.
string[] research = MarkdownIn("research");

foreach (string path in research)
{
    resources.Add((path, Frontmatter.Read(deserializer, path, out string? error), error));
}

string[] templates = TemplatesWithHeaders(TemplatesDirectory);
if (templates.Length == 0)
{
    errors.Add($"no header-carrying files found under {TemplatesDirectory}/");
}

foreach (string path in templates)
{
    resources.Add((path, CommentHeader.Read(path, out string? error), error));
}

foreach ((string path, IDictionary? fields, string? readError) in resources)
{
    if (fields is null)
    {
        errors.Add($"{path}: {readError}");
        continue;
    }

    foreach (string field in required)
    {
        if (!fields.Contains(field) || IsEmpty(fields[field]))
        {
            errors.Add($"{path}: missing or empty metadata field '{field}'");
        }
    }

    foreach (string field in dateFields)
    {
        // YamlDotNet may hand back an already-parsed date for an unquoted YAML scalar;
        // a comment header always arrives as text. Only text needs the format check —
        // a value YAML read as a date is one that was written as one.
        if (fields.Contains(field) && fields[field] is string text && !IsIsoDate(text))
        {
            errors.Add($"{path}: '{field}' is '{text}', not an ISO 8601 date (YYYY-MM-DD)");
        }
    }

}

if (errors.Count > 0)
{
    Console.WriteLine("Resource metadata validation failed:");
    foreach (string error in errors)
    {
        Console.WriteLine($"  - {error}");
    }

    return 1;
}

Console.WriteLine(
    $"All {resources.Count} resources carry valid metadata "
    + $"({opinions.Length} opinions, {research.Length} research, {templates.Length} templates).");
return 0;

// A key that is present but carries nothing — null, "", [] — is as much a gap as
// a missing key, so both fail the same way.
static bool IsEmpty(object? value) => value switch
{
    null => true,
    string text => text.Length == 0,
    ICollection collection => collection.Count == 0,
    _ => false,
};

// AGENTS.md: dates are ISO 8601, always absolute, never relative. An exact parse is
// what rejects "yesterday" and "August 2026" as well as 20-08-2026.
static bool IsIsoDate(string text) =>
    DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

static string[] MarkdownIn(string directory)
{
    if (!Directory.Exists(directory))
    {
        return [];
    }

    return
    [
        .. new DirectoryInfo(directory)
            .EnumerateFiles("*.md")
            .Select(file => $"{directory}/{file.Name}")
            .Order(StringComparer.Ordinal),
    ];
}

// JSON carries no comment syntax, so templates/global.json and the .slnf filter cannot
// hold a header at all. They are exempt here rather than permanently failing; what they
// pin is audited against the latest releases instead (skills/audit-freshness/SKILL.md).
static string[] TemplatesWithHeaders(string directory)
{
    if (!Directory.Exists(directory))
    {
        return [];
    }

    return
    [
        .. new DirectoryInfo(directory)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(file => file.Extension is not (".json" or ".slnf"))
            // Reported as repository-relative paths with forward slashes, so a finding
            // reads the same whichever platform ran the check.
            .Select(file => $"{directory}/{Path.GetRelativePath(directory, file.FullName).Replace('\\', '/')}")
            .Order(StringComparer.Ordinal),
    ];
}
