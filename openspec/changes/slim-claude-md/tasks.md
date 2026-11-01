# Tasks

Documentation only, so there is no red-green cycle; each task says how it is verified.

## 1. The split

- [x] 1.1 Write `.claude\rules\console-screen.md`, `terminal-gui.md`, `windows.md` and `checks.md`, each with `paths` frontmatter, from the matching text in `CLAUDE.md`. Verify each file's frontmatter parses (a YAML list of quoted globs between `---` markers).
  - Recorded 1 November 2026: all four parse with `ConvertFrom-Yaml` (powershell-yaml), 4, 11, 4 and 24 patterns, and every pattern matches at least one tracked file (`TerminalProfiles` lives in `Ports\TerminalSetupPorts.cs` and `IPackageVersions` needed a leading `*`, both fixed).
- [x] 1.2 Rewrite `CLAUDE.md`: pointers to the four rule files, the build and test section with the checks reduced to their commands and when to run them, the working rules, the code rules under headings, the Claude Code CLI facts and the open items. Verify the file is under about 25,000 characters and has no em dash.
  - Recorded 1 November 2026: 28,825 characters in 114 lines (from 55,685 in 87), a little over the aim because nothing was cut; the rule files are 6,443 (checks), 6,059 (console screen), 7,734 (Terminal.Gui) and 11,100 (Windows) characters. No em dash in any of the five.
- [x] 1.3 Check nothing was lost: every sentence of the old `CLAUDE.md` that holds "never", "always", "must", "only" or "do not" appears in the new `CLAUDE.md` or a rule file, and every type name in the old file appears in one of them. Verify by a script over the old and new texts, recording what it reports here.
  - Recorded 1 November 2026: every code span of the old file appears in the new five. Of 77 sentences holding one of the words, 24 are not verbatim: most are sentences the split cut differently (a bullet now ends where a sentence did), and the rest are moved with a tightened clause (the hand-off and smoke details to `checks.md`, the test-only rules about the Fragments folder, `settings.json` and nuget.org gathered into the working rules, the PowerShell quoting rule into Security). Each was read against the old text; no rule was dropped.

## 2. The references

- [x] 2.1 `CONTRIBUTING.md` ("The documents" check) and `openspec\config.yaml` (the verified facts line) name the rule files. Verify the workflow comments that cite `CLAUDE.md` sections still find them.
  - Recorded 1 November 2026: `check.yml` cites "Smoke check of the packed tool" and "Hand-off check", both still bullets in `CLAUDE.md`; the trust-record comments in `check.yml` and `claude-contract.yml` point at the CLI facts, still in `CLAUDE.md`.
- [x] 2.2 `openspec validate slim-claude-md --strict` passes and `.github\scripts\Test-DocumentationOnly.ps1 -Base origin/main -Head HEAD` answers true.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
