# CLAUDE.md

See [AGENTS.md](AGENTS.md) for all guidance on working in this repository.

This repository is LLM- and agent-agnostic — nothing outside this file may assume a specific agent product. Skills follow the [Agent Skills specification](https://agentskills.io/specification) in `skills/`, and every resource under `opinions/`, `research/` and `templates/` carries `last-reviewed`/`last-used` metadata that must be kept current — as frontmatter, or as a first-line comment header where the file format has no room for frontmatter. Read AGENTS.md before making changes.

Skills live at the specification's canonical path, `skills/<name>/SKILL.md`. Read and follow them from there. If your harness only discovers skills under `.claude/skills/`, create that as a local symlink (`ln -s ../skills .claude/skills`; `.claude/` is gitignored) — do not restructure the repository to suit it.
