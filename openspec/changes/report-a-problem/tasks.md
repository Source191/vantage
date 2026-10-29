# Tasks

## 1. The title and the address (Domain, Application\Reporting)

- [x] 1.1 Red: `ReportTitleShould` asserts `Validate` gives no problem for "Attach froze after Ctrl+Z", `TooLong` for 201 characters, and `From` and `TryParse` follow the text-value rules; `TextValuesShould` still passes with the new type. Green: `ReportTitle` and `ReportTitleProblem`. Verify the Core suite passes.
- [x] 1.2 Red: `ProblemReportAddressShould.FillTheFormsTitleAndDiagnostics` asserts the address for a title with `&`, `#` and a newline in the text starts with `https://github.com/Source191/vantage/issues/new?template=problem-report.yml` and carries both values escaped so `HttpUtility.ParseQueryString` gives them back exactly. Green: `ProblemReportAddress`. Verify the fact passes.

## 2. The report value (Application\Reporting)

- [x] 2.1 Red: `ProblemReportsShould.BuildTheEnvironmentEventsCountsAndCrash` over `RecordingDiagnostics`, `ProcessCounts`, `FakeCrashFile` with a crash, `TestSettings` and a `ReportEnvironment` asserts the text's headings and lines in order, and `LeaveOutTheCrashHeadingWithoutACrash` asserts none. Green: `ProblemReport`, `ProblemReports.BuildAsync` (the environment is the `Started` event Vantage already records, so no `ReportEnvironment` type); `ICrashFile` gains `ReadAsync` and `RemoveAsync`. Verify the facts pass.
- [x] 2.2 Red: `ProblemReportShould.FitWithinTheAddressLimit` builds 500 events and a crash, asserts the fitted address is at most 7,500 characters, keeps the environment, the crash and the newest failures, drops the crash file's earlier lines, then ordinary events oldest first, before exit-0 counts and failures (the crash's own line is never dropped), and ends with "n lines left out"; `KeepEverythingWhenItFits` asserts a short report is unchanged with no closing line. Green: `ProblemReport.FitWithin`. Verify the facts pass.

## 3. The issue form

- [x] 3.1 Add `.github\ISSUE_TEMPLATE\problem-report.yml` (name, description, labels `bug` and `from-vantage`, textareas `what-happened` required, `expected`, `steps`, `diagnostics` with `render: text`). Red first: `ProblemReportTemplateShould` (in Infrastructure.Tests, beside the other facts that read repository files, since Core.Tests reads none) reads the file from the repository root and asserts the template file name and the `diagnostics` id match `ProblemReportAddress`. Verify the fact passes.

## 4. Adapters

- [x] 4.1 Red: `CrashFileShould.ReadAndRemoveTheCrash` (temporary folder) asserts `ReadAsync` returns the written text, null when absent, and `RemoveAsync` removes it and ignores a missing file. Green: `CrashFile` and `FakeCrashFile` (the red caught `File.Delete` throwing for a missing state folder). Verify the Infrastructure suite passes.
- [x] 4.2 Red: `DesktopCommandLineShould.KeepTheLongestReportAddressUnderTheCommandLineLimit` (its own class: `DesktopShould` is an integration class that sets environment variables) over `FakeProcessRunner` asserts the recorded request's file name and joined arguments are under 32,767 characters for a 7,500-character address. Green: none expected. Verify the fact passes.

## 5. The key, effects and handlers (Application)

- [x] 5.1 Red: `SessionsViewShould.ReportAProblemOnBang` and `WorktreesViewShould.ReportAProblemOnBang` assert `!` returns `ShowProblemReport`. Green: the effect record with `IDialogEffect` and the two reducer cases. Verify the facts pass.
- [x] 5.2 Red: `OpenProblemReportHandlerShould.OpenTheFittedAddressAndRemoveTheCrash` over `FakeDesktop` and `FakeCrashFile` asserts the opened address, the crash removed and `ProblemReportOutcome.Opened`; `LeaveTheCrashWhenTheBrowserFails` asserts a failing `FakeDesktop` leaves the crash file and answers `NotOpened`. Green: `OpenProblemReport`, `OpenProblemReportHandler`, `ProblemReportOutcome` (a browser that cannot be started becomes `ProblemReportOutcome.NotOpened` with Windows' reason, so its words can suggest copying); `ICrashFile.RemoveAsync`; `EffectPorts` gains the crash file. Verify the facts pass.
- [x] 5.3 Red: `CopyProblemReportHandlerShould.CopyTheTitleAndTheFullText` over `FakeClipboard` asserts the untrimmed text and `ProblemReportOutcome.Copied`, and the crash file is untouched. Green: `CopyProblemReport`, `CopyProblemReportHandler`. Register both in `Composition.Handlers`. Verify `EffectDispatcherShould` and the handler facts pass.
- [x] 5.4 Red: `WordingShould` asserts the `!` key line and hint in `SessionsKeys` and `WorktreesKeys`, the dialog title, the explanation, the button captions, the title problem and `DefaultReportTitle(0.18.0)` reading "Problem report from Vantage 0.18.0"; `WordingOfOutcomesShould` asserts both outcomes and the open failure's copy suggestion. Green: the words. Verify the facts pass, and the generated help and tooltip facts still pass.

## 6. The dialog (Application, Tui)

- [x] 6.1 Red: `ProblemReportViewShould` asserts typing updates the title, Open with a blank title gives `Open` with the default title, Open with 201 characters gives `None` and `TooLong`, Copy gives `Copy`, Esc gives `Close`. Green: `ProblemReportState`, `ProblemReportInput`, `ProblemReportEffect`, `ProblemReportView.Reduce`. Verify the facts pass.
- [x] 6.2 Red: `DialogEffectsShould` fails until `ShowProblemReport` has a flow. Green: `DialogName.ProblemReport` (CS8509 names every switch that must learn it), `ProblemReportDialog` (its few controls inline, so no separate controls class) bound under a `binding` guard with `DialogTabTitle.Apply`, and `DialogEffects.ShowProblemReport` building the report on a pool thread and dispatching the chosen effect. Verify `DialogEffectsShould` passes and a headless `drive` at 120 x 36 shows the dialog whole, the tab title `Vantage (Report a problem)`, and the preview's text.
- [x] 6.3 Extend `DiagnosticsPrivacyShould` with a report: the fixture-seeded flows, then `!`, a title "Northwind.Orders secret title" and Open; assert the preview and the opened address hold no seeded value except the title, and the log does not hold the title. Verify the fact passes.
- [x] 6.4 Add a `SmokeScript` step that presses `!` and cancels, keyed on the dialog's tab title from `Wording`. Verify `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree>` passes the new steps locally (29 Oct 2026: steps 7 and 8 pass; step 11 times out on this machine's worktree scan, as it does for the installed 0.17.0-preview.3, so CI's seeded run settles the rest).

## 7. Docs, version and checks

- [x] 7.1 README: `!` in both keys tables, a "Reporting a problem" section (what the text holds and never holds, that opening the page passes it to GitHub, that it is public only once submitted, copy as the alternative, the crash file used once), and the Troubleshooting line pointing to it. Verify the section matches the spec's requirements.
- [ ] 7.2 Raise `Version` one preview step above `main`'s (a minor raise). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and `dotnet run --project tools\HeadlessTerminal -- check` exits 0.
- [ ] 7.3 The product owner creates the `from-vantage` label on `Source191/vantage` (`gh label create from-vantage`). Verify `gh label list` shows it.
- [ ] 7.4 The product owner installs the build with `tools\install-local.ps1` and, with the template on the pull request's branch, opens a report signed in and signed out of GitHub (the form loads only from the default branch, so this check runs after merge on a preview release if the branch's template is not picked up). Verify the title and the diagnostics field arrive filled in, signing in keeps them, the labels are applied, copy pastes the full text, and an old `last-crash.log` is gone after opening; record the results here.

## Workflow follow-up

- Merge `privacy-safe-diagnostics` first; this change depends on it.
- Archive the change in the same pull request, syncing the `problem-reports` delta into `openspec\specs`.
