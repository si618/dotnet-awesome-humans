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

// Verifies that every file under opinions/ carries the four frontmatter fields
// AGENTS.md requires: targets, last-reviewed, last-used, sources.
//
// A .NET 10 file-based app: no project, no build step, sharing its helpers with the
// other checks via #:include. Run it from the repository root:
//
//     dotnet run scripts/validate-opinion-frontmatter.cs
//     ./scripts/validate-opinion-frontmatter.cs      (the shebang above)
//
// Exit codes: 0 clean, 1 findings printed, 2 not run from the repository root.

using System.Collections;
using YamlDotNet.Serialization;

if (!Opinions.DirectoryExists)
{
    Console.Error.WriteLine($"No {Opinions.DirectoryName}/ directory here — run this script from the repository root.");
    return 2;
}

string[] required = ["targets", "last-reviewed", "last-used", "sources"];

List<string> errors = [];
string[] names = Opinions.Names();

if (names.Length == 0)
{
    errors.Add($"no markdown files found under {Opinions.DirectoryName}/");
}

IDeserializer deserializer = new DeserializerBuilder().Build();

foreach (string name in names)
{
    string path = Opinions.PathOf(name);

    if (Frontmatter.Read(deserializer, path, out string? error) is not IDictionary frontmatter)
    {
        errors.Add($"{path}: {error}");
        continue;
    }

    foreach (string field in required)
    {
        if (!frontmatter.Contains(field) || IsEmpty(frontmatter[field]))
        {
            errors.Add($"{path}: missing or empty frontmatter field '{field}'");
        }
    }
}

if (errors.Count > 0)
{
    Console.WriteLine("Opinion frontmatter validation failed:");
    foreach (string error in errors)
    {
        Console.WriteLine($"  - {error}");
    }

    return 1;
}

Console.WriteLine($"All {names.Length} opinion files have valid frontmatter.");
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
