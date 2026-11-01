# Tasks

Documentation only, so there is no red-green cycle; each task says how it is verified.

## 1. The split

- [ ] 1.1 Write `.claude\rules\console-screen.md`, `terminal-gui.md`, `windows.md` and `checks.md`, each with `paths` frontmatter, from the matching text in `CLAUDE.md`. Verify each file's frontmatter parses (a YAML list of quoted globs between `---` markers).
- [ ] 1.2 Rewrite `CLAUDE.md`: pointers to the four rule files, the build and test section with the checks reduced to their commands and when to run them, the working rules, the code rules under headings, the Claude Code CLI facts and the open items. Verify the file is under about 25,000 characters and has no em dash.
- [ ] 1.3 Check nothing was lost: every sentence of the old `CLAUDE.md` that holds "never", "always", "must", "only" or "do not" appears in the new `CLAUDE.md` or a rule file, and every type name in the old file appears in one of them. Verify by a script over the old and new texts, recording what it reports here.

## 2. The references

- [ ] 2.1 `CONTRIBUTING.md` ("The documents" check) and `openspec\config.yaml` (the verified facts line) name the rule files. Verify the workflow comments that cite `CLAUDE.md` sections still find them.
- [ ] 2.2 `openspec validate slim-claude-md --strict` passes and `.github\scripts\Test-DocumentationOnly.ps1 -Base origin/main -Head HEAD` answers true.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
