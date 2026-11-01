# Tasks

A comment-only change: no test is written, and each area is verified with `dotnet build -c Debug` (malformed XML doc is a warning, so an error).

## 1. Links

- [ ] 1.1 Survey every comment in `src\`, `test\`, `tools\`, `.github\` and `CLAUDE.md`'s facts sections for citations of external APIs, and verify each chosen URL answers 200 (list and status codes kept in the job's `tmp\link-cited-apis\urls.txt`).
- [ ] 1.2 Processes: Win32 and NT process calls and structures, CreateProcess's search order, ShellExecute, ProcessStartInfo, vswhere, cmd.exe's parsing.
- [ ] 1.3 Terminal: console virtual terminal sequences, SetConsoleMode, the pseudo-console, wt.exe's arguments, `-EncodedCommand`, PowerShell's quoting rules, Windows Terminal fragments and profile settings, UI Automation, Sixel and kitty.
- [ ] 1.4 Git, pull requests and packages: git's porcelain formats and settings, gh and az, the NuGet flat container, semantic versioning, System.CommandLine, the workflows.
- [ ] 1.5 `CLAUDE.md` facts sections, leaving the lines beside the `Tab.cpp` link alone.

## 2. Release and checks

- [ ] 2.1 Raise `Version` to `0.19.0-preview.14`; `.github\scripts\Assert-VersionBumped.ps1 -Base v-null-guard-rule -Head HEAD` passes.
- [ ] 2.2 `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate link-cited-apis --strict`, and no em dash in the changed files.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
