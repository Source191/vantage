# Proposal

## Why

After `rename-to-torni` the name still appears in about 700 places that would read as well, or better, without it: identifiers that only mean "this program", comments, sentences on the screen that are read inside the program, the unreleased changelog, temporary folders and the word the tab files use for the console's own tab. Each is one more thing a later rename rewrites, and one, the problem-report label, is already broken: the issue form asks for `from-torni` while the repository only has `from-vantage`.

## What Changes

- The problem-report issue form applies a `problem-report` label, and the repository's `from-vantage` label is renamed to it, so reports keep their label whatever the program is called.
- **BREAKING:** the tab files record the console's own tab as `console` instead of `torni`. Nothing is migrated: no one has installed this name from winget.
- Identifiers that only meant "this program" say what they mean: `CreatedHere` and `NotCreatedHere`, `createdHere`, `AlreadyInBackground`, `FocusOwnTab`, `TabOwner.Console`, `Started.Tool`, the "Torni" profile's `ConsoleProfileName`, `ConsoleExecutable`, `ConsoleSound` and `ConsoleDefaultSound`, and the locals and test names that carried the name.
- Sentences on the screen drop the name where the program is the obvious subject (key help, status lines, setup hints, dialog text); titles, the settings row's label, profile names, commands and any sentence that sets the program beside Claude Code, Windows Terminal or winget keep it.
- Code comments, workflow comments and `openspec\config.yaml` drop the name by the same rule as the docs.
- The unreleased changelog entries drop it where the program is the obvious subject; released sections stay as history.
- Temporary and internal names go neutral: CI's extracted-zip folder, the headless check's and the tests' temporary folder prefixes, `Get-TorniVersion`, `.gitignore` comments.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `detail-pane`: the Prompted line's scenario quotes "not seen since the console started". No other requirement or scenario quotes a sentence that changes, and the tab file's own-tab word is not specified.

## Impact

- **Code:** `Wording` and its partial files, `TabOwner` and `TabOwnerJsonConverter`, `WorktreeOrigin`, `StatusMessage`, `Started`, `TerminalProfiles`, the effects and handlers that use them, and their tests.
- **Repository:** the issue form and the GitHub label, `pipeline.yml`, `tools\HeadlessTerminal`, the test temporary folders, `.gitignore`, `CHANGELOG.md`.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Names a person types or sees outside the program: the command, `%LOCALAPPDATA%\Torni`, the variables, the profile names, the winget identifier, the zip and CI artefact names, URLs, the issue form's text and the project and namespace names.
- The specs, whose requirements need a subject.
