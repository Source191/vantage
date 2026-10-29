# Proposal

## Why

When Vantage misbehaves, a user has no easy way to tell the maintainer what happened, and the maintainer has no facts to go on: which versions, what the user did, what failed and where. The `privacy-safe-diagnostics` change records those facts in a form that holds nothing personal; this change lets the user hand them over, together with their own description, as a GitHub issue, without Vantage holding a token, running a service or sending anything itself.

## What Changes

- **New key `!` (Report a problem) in both views:** opens a dialog with a title box and a read-only preview of exactly the diagnostics text that will be filled in: the environment (Vantage, Claude Code, Windows and .NET versions, terminal size, the shape of the settings such as the editor preset and the number of roots), the recent diagnostic events, the child program counts and the last crash when there is one.
- **Open in browser:** opens the repository's "Problem report" issue form in the default browser through a pre-filled `issues/new` address carrying the title and the diagnostics. The user describes the problem in GitHub's own form (Markdown and screenshots work there), can edit or delete any of the diagnostics, and submits it from their own GitHub account. Vantage sends nothing itself, holds no token and needs no `gh` sign-in.
- **Copy text:** copies the same diagnostics to the clipboard instead, for someone without a GitHub account or when the browser route fails.
- **Fits GitHub's limit:** the address is kept under 7,500 characters; when the diagnostics are longer the oldest ordinary events go first and a line says how many were left out, while the environment, failures and crash summary are kept.
- **Crash file is used once:** a report that opens successfully includes `last-crash.log` and then removes it, so the next report does not repeat an old crash.
- **The words say what happens:** the dialog states that the text holds no names, paths or code, that opening the page passes it to GitHub in the address, and that it becomes public only when submitted.
- **Issue form:** `.github\ISSUE_TEMPLATE\problem-report.yml` with "What happened?", "What did you expect?", "Steps" and a "Diagnostics from Vantage" field, labelled `bug` and `from-vantage`.
- README (keys table, a "Reporting a problem" section), the smoke script step for the new dialog; `Version` rises (a minor raise, one preview step above `main`'s).

Out of scope: creating the issue from Vantage (through `gh`, the API or a service of Vantage's own), anonymous reports, attachments or screenshots taken by Vantage, reports to anywhere but `Source191/vantage`, a report from the command line (`vantage report`), automatic reports on a crash, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

- `problem-reports`: how a user reports a problem from Vantage, what the report holds, and how it reaches the repository's issue form.

### Modified Capabilities

None.

## Impact

- `src\Source191.Vantage.Core`: the effect, reducer case and wording for `!`; `Application\Reporting` (the report text, its trimming, the issue-form address, the dialog's reducer and state); two handlers (open, copy) and their outcome records; `DialogName.ProblemReport`; `Ports\ICrashFile` gains read and remove.
- `src\Source191.Vantage.Infrastructure\Diagnostics\CrashFile`: read and remove.
- `src\Source191.Vantage.Tui`: the dialog and its controls, and the flow in `DialogEffects`.
- `.github\ISSUE_TEMPLATE\problem-report.yml` (new) and the `from-vantage` label on the repository.
- `tools\HeadlessTerminal`: one `SmokeScript` step.
- README; depends on `privacy-safe-diagnostics` being merged first.
