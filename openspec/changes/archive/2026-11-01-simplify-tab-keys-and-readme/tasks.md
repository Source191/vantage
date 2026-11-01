# Tasks

## 1. The digits stop choosing a tab

- [x] 1.1 Red: `TabKeysShould` asserts `TryFor` answers false for 1, 2 and 3, and `SessionsViewShould`, `WorktreesViewShould` and `ConfigViewShould` assert the digits raise no `SelectTab`; verify they fail on the digit arms
- [x] 1.2 Green: remove the three digit arms from `TabKeys.TryFor` and the digits from its and `SelectTab`'s summaries; verify the Core tests pass
- [x] 1.3 Red then green: a help test asserts `TabKeyHelp` lists ← and → and none of 1, 2 and 3 (`KeyHelpShould` keeps its own example); remove the three entries from `WordingOfScreens.TabKeyHelp`; verify the Core tests pass
- [x] 1.4 Red then green: `SmokeScriptShould` asserts the tab steps are → → from Sessions to Config and ← from Config to Worktrees and from Worktrees to Sessions; rewrite `SmokeScript.TabKey` to step by the help's arrows and teach `Sequence` the arrow sequences; verify `tools\HeadlessTerminal.Tests` passes
- [x] 1.5 Update `.claude\rules\console-screen.md` so the tab keys are Left and Right only; verify by reading it

## 2. A short README

- [x] 2.1 Rewrite `README.md` as a short getting-started guide: pitch, prerequisites (still stating `2.1.309 or later`), install, quick start, everyday keys, notifications (`vantage setup-terminal`), learn more (`?`, `!`, CHANGELOG, SECURITY, CONTRIBUTING) and support; no Ctrl+Z and no digit keys; verify `DocumentedMinimumShould` passes
- [x] 2.2 Repoint `CONTRIBUTING.md`'s link to the README's Reporting a problem section and reword its rule on what the README covers; verify no link in the repository's Markdown names a README anchor that no longer exists

## 3. Release

- [x] 3.1 Raise `Version` to 0.20.0-preview.14 and amend the Unreleased #117 line in `CHANGELOG.md` to drop 1, 2 and 3 and name this pull request; verify the changelog test passes
- [x] 3.2 `dotnet build -c Debug`, the whole solution's `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate simplify-tab-keys-and-readme`; verify all pass

## Workflow follow-up

- Archive the change in the same pull request, so `openspec\specs` describes the code on `main`.
- Owner's manual check after merge: Left and Right still move between tabs and 1, 2 and 3 do nothing.
