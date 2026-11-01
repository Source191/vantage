# Proposal

## Why

A review on 1 November 2026, against `main` at 13c4d1bd (0.20.0-preview.7), checked every Markdown file outside the archive and every `///` comment in `src`, `test` and `tools` against the code. A month of changes (three tabs, no console hand-off, the status line, keys `f`, `s` and `n`, `R` without a dialogue) has left the documents behind:

- **README:** 15 statements are wrong and 9 behaviours are missing. At 64 KB it also carries internals a user never needs.
- **CONTRIBUTING:** the hand-off check command fails as written, its exit codes are wrong, and two manual checks test keys that no longer exist.
- **CLAUDE.md:** 8 inaccuracies, and at 33 KB (about 8,300 tokens) it loads into every Claude Code session even though half of it matters only in one area.
- **Comments:** about 49 comments are wrong or stale.
- **On-screen text:** three messages users see name a key or behaviour that has gone.

The owner wants the documents ready before the repository goes public.

## What Changes

- **On-screen text:** three outcome and status messages in `Application\Wording` are corrected:
  - the editor-not-found outcome names the Config tab instead of a `,` key;
  - the restored-rename outcome says Enter opens a new tab, not `o`;
  - the terminal-session hint says the old tab closes after `b`, not that it becomes a plain shell.
- **XML comments:** the wrong and stale `///` comments found by the review are corrected (two tabs to three, the settings row, hand-off, the `s` key that became `n`, `R`'s checklist, the standing message), and the most misleading gaps are filled. "Settings row" becomes the one name for the top line in comments.
- **README:** every wrong statement is corrected and the missing behaviours are added. It is cut to about 30 KB by dropping version history, lookup internals, batch-file quoting, parsing detail and test class names. The Config tab moves beside the other tabs. "What Vantage trusts" and "What Vantage records" become one "Security and privacy" section linking `SECURITY.md`. "2.1.309 or later" stays word for word, because `DocumentedMinimumShould` reads it.
- **CONTRIBUTING:**
  - The loop, the exit codes, the code map and the manual checks are corrected.
  - The owner-only "Releasing and maintaining" section moves to a new `MAINTAINING.md`.
  - The OpenSpec and CI sections are shortened.
  - Immutable releases are described as off, as the repository has them (and in `.claude\rules\checks.md`).
- **CLAUDE.md:**
  - Its inaccuracies are corrected and it is slimmed to about 16 KB.
  - The Claude Code CLI facts move to a new path-scoped `.claude\rules\claude-code.md`, and the security details to `.claude\rules\security.md`. CLAUDE.md keeps one-line invariants and the three CLI must-knows.
  - The wizard and settings-dialog detail moves to `console-screen.md`, the contract and mutation detail to `checks.md`, and the "Open items" section is dropped because the README's roadmap holds them.
- **Rules files:** stale names and globs are fixed in `console-screen.md`, `terminal-gui.md`, `windows.md`, `locale.md` and `checks.md`.
- **References:** everything that points at the moved facts follows them: `openspec\config.yaml`, `ClaudeHelpSurfaceShould`, the workflow comments, `dependabot.yml` and `Assert-VersionBumped.ps1`'s message.
- **Project READMEs:** the project READMEs and `tools\HeadlessTerminal\README.md` are corrected and completed.
- **Removed:** `docs\architecture-review.md`, a review of 0.2.0 that names types that no longer exist and that nothing links to.
- **Version:** rises to 0.20.0-preview.8, because files under `src\` change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The three messages are corrected to match what the specs already require (session-attach: adopt closes the old tab; session-listing: Enter opens a session in its tab), so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `Application\Wording.cs` and `WordingOfOutcomes.cs` (three strings) with their facts; comment-only edits across Core, Infrastructure, Tui, tests and tools; `Source191.Vantage.csproj` for the version.
- **Documents:** `README.md`, `CONTRIBUTING.md`, new `MAINTAINING.md`, `CLAUDE.md`, `.claude\rules\*` (two new files), project READMEs, `openspec\config.yaml`; `docs\architecture-review.md` deleted.
- **CI:** comments only in `check.yml`, `claude-contract.yml` and `dependabot.yml`, and one message in `Assert-VersionBumped.ps1`.
- **Users:** three messages read correctly, and the README and package page are accurate.
- **Claude Code sessions:** about 4,000 fewer tokens loaded in each.

## Out of scope

- Moving the dialog captions held in `Tui` (`WizardControls`, `SettingsControls`, `NameDialog`, `TypedConfirmationDialog`, the `DialogEffects` titles, `WaitingText`) into `Wording`, which is a later change.
- The README screenshot, which the owner takes in Windows Terminal; the "To do" line stays until then.
- Turning on immutable releases.
- Renaming `Wording.ReturnHint` or any other member.
- Stale wording inside the specs ("either view", "settings line"), which needs spec deltas of its own.
- Multiple Claude accounts, which is a later change.
