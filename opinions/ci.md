---
targets: [net10.0]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [meziantou]
---

# CI & automation

Supply-chain hygiene is not optional in the agentic era.

## Opinions

- **Pin GitHub Actions to commit SHAs, not mutable tags** — tags move silently; SHAs don't. Automate the sweep across repositories. ([Meziantou — SHA pinning](https://www.meziantou.net/enable-sha-pinning-for-github-actions-across-personal-repositories.htm))
- **Never interpolate user-provided input into workflow scripts** — pass it via environment variables and parse deliberately (script-injection is the top Actions vulnerability). ([Meziantou — Safely passing extra arguments](https://www.meziantou.net/safely-passing-extra-arguments-in-github-actions-workflows-using-powershell.htm))
- **CI builds are pinned and reproducible:** `global.json` decides the SDK, lock files or CPM decide packages — a CI run must not float versions the repo didn't choose.

<!-- TODO: full treatment — build/test/publish pipeline shape, artifact signing, dependabot/renovate stance -->
