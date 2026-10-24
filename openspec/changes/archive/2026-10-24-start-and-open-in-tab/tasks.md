# Tasks

## 1. Launch outcome carries the listed session

- [x] 1.1 LaunchServiceShould: ReportTheListedSessionAfterAStart (no description given; outcome.Session.SessionId is "sess-1"); add `Session? Session` to LaunchOutcome and resolve it after every successful start; verify LaunchServiceShould passes
- [x] 1.2 LaunchServiceShould: StillReportStartedWhenTheSessionIsNotListedYet (listing empty; Kind Started, JobId set, Session null, description skipped); verify LaunchServiceShould passes

## 2. Tab for a session that is not yet a row

- [x] 2.1 TabOpenerShould: OpenATabForAJustStartedSession (`OpenAsync(Session)` opens the titled tab, records the pending registration against the session id); make the row overload delegate after its "already open" check; verify TabOpenerShould passes
- [x] 2.2 LaunchWorkflow gains the TabOpener and `OpenTabAsync(Session)`; Program.cs builds the tab opener before the workflow; verify `dotnet build -c Debug` passes (wiring, no test)

## 3. Wizard and screens

- [x] 3.1 WizardResult(Plan, OpenInTab); F6 and the button become "Start and open in _tab (F6)"; both hint strings and the doc comment say "F6 start and open in tab"; verify `dotnet build -c Debug` passes
- [x] 3.2 SessionsScreen.RunWizard and WorktreesScreen.RunWizard: after Started with OpenInTab, open the tab and append its footer ("Started name (id). Opened name in a blue tab."); when the session is null say "not listed yet: press o on it to open a tab"; no attach exit from the wizard; verify `dotnet build -c Debug`, `dotnet test`, `dotnet format --verify-no-changes`
- [x] 3.3 README: F6 line in the wizard section and the "start work on a story" scenario; verify `grep "open here"` finds nothing in README or src

## 4. Integration check

- [x] 4.1 Hand-off check `dotnet run --project tools\HeadlessTerminal -- check` from a shell without the Claude Code variables exits 0

## Workflow follow-up

- Archive the change (`openspec archive start-and-open-in-tab -y`) so the session-launch main spec carries the new scenario.
