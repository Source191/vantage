# Design

## Context

See proposal.md, Why. This change builds on `privacy-safe-diagnostics`: `IDiagnostics.Snapshot()` gives the recorded events, `ProcessCounts` the child program counts, `DiagnosticLine` renders both, and `Ports\ICrashFile` writes `%LOCALAPPDATA%\Vantage\last-crash.log`. `IDesktop.OpenAsync(string)` already hands a URL to Windows (the `p` key opens a pull request that way) through a short-lived `pwsh -EncodedCommand` child, with the target as Base64 inside the script. `IClipboard` copies text (`c`). Dialogs follow the binder pattern (`SettingsView.Reduce` over `SettingsState`, bound by `SettingsDialog` and `SettingsControls`) and every dialog names itself with a `DialogName` for its tab title. GitHub's new-issue page accepts `template`, `title` and, for an issue form, one query parameter per field id, which fills that field; an address over the server's limit answers 414, and the documented practical limit is about 8 KB. Labels named in an issue form are applied for every submitter.

## Goals / Non-Goals

**Goals:**
- One key, one dialog, one browser page: the user reviews the text in Vantage, writes the description in GitHub, and submits from their own account.
- The preview is the text: what the dialog shows is the value the handler encodes, not a second rendering.
- Nothing new can carry user data: the only user text is the title, which travels in the effect and the address and never reaches the diagnostic log.

**Non-Goals:**
- Any request made by Vantage, any token, any `gh` dependency.
- Editing the diagnostics inside Vantage (GitHub's form is the editor).

## Decisions

**Pre-filled issue form, not a body.** `.github\ISSUE_TEMPLATE\problem-report.yml` declares the form (name "Problem report", labels `bug` and `from-vantage`) with textareas `what-happened` (required), `expected`, `steps` and `diagnostics` (`render: text`, so GitHub shows it as a code block). The address is `https://github.com/Source191/vantage/issues/new?template=problem-report.yml&title=<title>&diagnostics=<text>`, each value through `Uri.EscapeDataString`. A form keeps the user's description in its own required field and the diagnostics in a field they can read and edit; a plain `body=` would mix the two and lose the required description. Alternative: `gh issue create` (rejected by the product owner: needs a sign-in and posts without a review in GitHub).

**One home for the address.** `Application\Reporting\ProblemReportAddress` holds the new-issue address and the template's file name as its only constants and builds the `Uri` from a `ReportTitle` and a `ProblemReport`; `ProblemReportAddressShould` and a fact reading the template file from the repository (`Architecture\ProblemReportTemplateShould`, asserting the field ids match) keep the two in step.

**The report is a value built once.** `Application\Reporting\ProblemReports(IDiagnostics, ProcessCounts, ICrashFile, ReportEnvironment, CurrentSettings)` has `BuildAsync()` returning `ProblemReport`: the environment lines, the crash text when the file exists, the rendered events split into failures and the rest, and the rendered counts. `ReportEnvironment` is a record of the start facts (`ToolVersion`, the Claude Code `Version`, Windows and .NET `Version`s, terminal columns and rows) handed over by `Program.cs`; the settings part takes only the editor preset (an enum), `checkForUpdates` and the number of roots from `CurrentSettings`. `ProblemReport.Text` is the full text; `ProblemReport.FitWithin(ReportTitle, int limit)` returns the trimmed text in the spec's order (oldest ordinary events, then exit-0 counts, then oldest failures; never the environment or crash) with the closing "n lines left out" line, measuring the escaped address each time it drops a line. The dialog's preview shows the fitted text for the title as typed; the open handler encodes that same fitted text; copy uses the full text.

**The 7,500-character limit.** GitHub's limit is about 8,192, and `Desktop.OpenAsync` inflates its target: the URL becomes Base64 (4/3) inside a script that is then UTF-16 Base64 (8/3), roughly 3.6 times the URL plus about 1,100 characters, against Windows' 32,767-character command line. 7,500 gives about 27,800, inside both with room to spare. `DesktopShould.KeepTheLongestReportAddressUnderTheCommandLineLimit` (Infrastructure, over `FakeProcessRunner`) asserts the request's joined arguments stay under 32,767 for a 7,500-character address, so a later change to `OpenFileScript` cannot break the key silently.

**A reducer for the dialog.** `Application\Reporting\ProblemReportView.Reduce(ProblemReportState, ProblemReportInput)` returns a `ProblemReportEffect`: `Open(ReportTitle)`, `Copy(ReportTitle)`, `Close` or `None`. The rules: a blank title becomes `Wording.DefaultReportTitle(version)`; a title over 200 characters gives `ReportTitleProblem.TooLong`, shown in the dialog, and Open and Copy do nothing until it is shorter. `ReportTitle` is a text value with the three doors (`From`, `TryParse`, `Validate`, the second type after `SessionName` to have `Validate`). `Tui\ProblemReportDialog` and `ProblemReportControls` bind it under a `binding` guard like the settings dialog: an explanation label, the title `TextField`, a read-only `TextView` preview of fixed height, and the buttons "Open in browser" (default), "Copy text", "Cancel"; the preview is refreshed from the state when the title changes, since the fitted text depends on the title's length.

**The key, the effects and the handlers.** `!` on both views returns `ShowProblemReport : ViewEffect, IDialogEffect`. `DialogEffects.ShowProblemReport` builds the report on a pool thread (`ProblemReports.BuildAsync`), runs the dialog and dispatches `OpenProblemReport(ReportTitle, ProblemReport)` or `CopyProblemReport(ReportTitle, ProblemReport)`. `OpenProblemReportHandler(IDesktop, ICrashFile)` opens the address and then removes the crash file when the report held one, returning `ProblemReportOutcome.Opened`; a failure to open propagates as every handler's does, so `Footer.Failed` shows it, and `Wording` adds the copy suggestion for this effect's failure. `CopyProblemReportHandler(IClipboard)` returns `ProblemReportOutcome.Copied`. Both are registered in `Composition.Handlers`; `DialogName.ProblemReport` gives the tab `Vantage (Report a problem)`. `EffectRan` records the effect's type name only, so the title in the effect record never reaches the log; the privacy fact from `privacy-safe-diagnostics` gains a report with a fixture title and asserts it is absent from the log.

**`ICrashFile` gains `ReadAsync` and `RemoveAsync`.** The text it reads was written only by `CrashRecorder` from rendered events, so it is included as it is; `RemoveAsync` ignores a file that has already gone.

**The words.** `Wording` gains the `!` key line ("Report a problem", hint "Report"), the dialog title, the explanation ("Holds no names, paths or code. Opening the page passes this text to GitHub in its address; it is public only once you submit it."), the button captions, the title problem, the default title, and the outcomes ("Opened a problem report in your browser; describe the problem there and submit it.", "Copied the problem report.") with facts in `WordingShould` and `WordingOfOutcomesShould`.

## Risks / Trade-offs

- [Opening the page sends the diagnostics to GitHub before the user decides to submit] → the dialog says so before the page opens, and the text holds nothing personal by construction; the user can copy instead.
- [The user is not signed in to GitHub] → GitHub's sign-in is expected to return to the same address with its query intact (unverified; the product owner checks it in task 7), and copy is the fallback.
- [GitHub's limit is lower than expected] → 7,500 is well under 8,192; a 414 would show in the browser and copy still works.
- [The form's field ids and the address drift apart] → `ProblemReportTemplateShould` reads the template and checks the ids.
- [The help dialog grows past what a 40-row terminal shows] → keys go two to a line; the smoke script and a headless `drive` at 120 x 36 check the last line is visible.
- [The `from-vantage` label does not exist] → what GitHub does with a form naming a missing label is unverified, so the product owner creates the label before the pull request merges.

## Migration Plan

The issue form must be on `main` for the key to work, so the template and the code land in the same pull request; a release built from that merge has both. Rolling back is reverting the change; the template can stay.
