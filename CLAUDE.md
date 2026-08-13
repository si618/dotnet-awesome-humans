# CLAUDE.md

See [AGENTS.md](AGENTS.md) for all guidance on working in this repository.

This repository is LLM- and agent-agnostic — nothing outside this file may assume a specific agent product. Skills follow the [Agent Skills specification](https://agentskills.io/specification) in `skills/`, and every resource carries `last-reviewed`/`last-used` frontmatter that must be kept current. Read AGENTS.md before making changes.

Skills live at the specification's canonical path, `skills/<name>/SKILL.md`. Read and follow them from there. If your harness only discovers skills under `.claude/skills/`, create that as a local untracked symlink (`ln -s ../skills .claude/skills`, ignored via `.git/info/exclude`) — do not restructure the repository to suit it.
