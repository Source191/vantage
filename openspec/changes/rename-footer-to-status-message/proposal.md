# Proposal

## Why

The status line's message is a type called `Footer`, named for where the line once sat. It now sits under the versions, above the tabs, so the name describes a place it no longer is, and every later move would need another rename. The specs say "the footer" in 42 requirements, which no longer matches the screen either.

## What Changes

- `Footer` becomes `StatusMessage`, a name that says what it is and not where it goes: `StatusMessage.NoSessionSelected`, `StatusMessage.Reported`, `StatusMessage.None` and the rest, unchanged otherwise.
- Each view state's `Footer` property becomes `Message`; `IScreenDefinition.WithFooter` becomes `WithMessage` and `IEffectContext.ShowFooter` becomes `ShowMessage`.
- Tests, comments and `CLAUDE.md` follow, including test names such as `HaveWordsForEveryFooterCase`.
- The specs, the README and `CONTRIBUTING.md` say "the status line" where they said "the footer", and the requirement "The screen is a header, a table and a footer" becomes "The screen is a header, a table and a detail pane". The requirements are reworded only.
- No behaviour changes. `Version` still rises to the next preview, because the rename touches the packaged tool's source under `src\`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

Each of these has requirements that say "the footer", reworded to "the status line" with no change of meaning: `config-tab`, `console-layout`, `diagnostics`, `external-tools`, `folder-actions`, `problem-reports`, `pull-requests`, `session-attach`, `session-launch`, `session-listing` (which also renames one requirement), `tool-updates` and `worktree-management`.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Any change to what a message says, when it shows or clears, or where the status line sits.
- Renaming `StatusTone`, `StatusLayout` or `ITabBody.Status`, which already say what they are.
- Archived changes, which are history and keep the word footer.

## Impact

- About 610 mentions of `Footer` and its compounds in 35 files under `src\`, `test\` and `tools\`; the rename is mechanical and the compiler finds any that are missed.
- 12 spec files, through MODIFIED deltas (and one RENAMED) that only reword.
- `README.md`, `CONTRIBUTING.md` and `CLAUDE.md`.
- `Version` in the tool's csproj.
