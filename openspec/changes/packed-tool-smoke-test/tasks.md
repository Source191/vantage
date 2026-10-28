# Tasks

## 1. Confirm the assumptions by hand

- [x] 1.1 With NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared and `CLAUDE_CONFIG_DIR` pointing at an empty temporary folder (a signed-out CLI), `drive` the installed tool with `9000:?,3000:\u001b,2000:q` and confirm the sessions view draws its column header and the capture holds `ESC ] 0 ; Vantage (Keys)` and then `ESC ] 0 ; Vantage`. Verify with `count` and a title search of `drive-raw.txt`; record the result here, and revise design.md's first risk if the view does not draw.
  - Result (2026-10-28, CLI 2.1.310): signed out, `claude agents --json` prints `[]`; the header `NAME STATE REPOSITORY FOLDER BRANCH PR AGE TAB` is drawn, the titles run `Vantage`, `Vantage (Keys)`, `Vantage`, and Vantage exits 0.
- [x] 1.2 In a temporary folder used as the home for this check, or against one of your own linked worktrees, `drive` the keys `w`, `/`, the worktree folder name, Enter, `D`, Esc, `w`, `q` and confirm the force-remove dialog opens on that worktree (title `Vantage (Force remove)`) and that Esc leaves it with "Cancelled; nothing removed.". Verify with `grid` and `count`; record the keys that work here, and update decision 4 in design.md if they differ.
  - Result (2026-10-28, Vantage 0.16.1, against the product owner's own worktrees; Vantage's home cannot be redirected because it reads the known folder): the keys work as listed once the worktree rows have loaded, which took up to about 25 s with five worktrees. Typed while the scan was running, the filter matched nothing and the keys after it were lost, so the script waits for the worktree's folder name in the table before `/`. `D` on a worktree with a live session is refused ("Not removed: session v-nuget is using it.") with no dialog; on an idle one the titles run `Vantage`, `Vantage (Force remove)`, `Vantage` and the footer says "Cancelled; nothing removed.". `,` (Settings) and `n` (wizard) open and close with Esc. In a `drive` script a comma must be written `\u002c`.

## 2. Waiting for markers

- [x] 2.1 Test first: `CaptureMarkerShould` in `tools\HeadlessTerminal.Tests` (a title marker matches only an exact OSC 0 or OSC 2 title terminated by BEL or ST, `Vantage` does not match `Vantage (Keys)`; a text marker matches after escape sequences are stripped; both search from a given position and return the position after the match, or none). Then `CaptureMarker` in `tools\HeadlessTerminal`. Verify the tests pass.
- [x] 2.2 Refactor: move the pseudo-console start, the reader, the query replies and the capture out of `Driver.RunAsync` into one run session that both run modes share, with no change to `drive` or `check`. Verify the existing `tools\HeadlessTerminal.Tests` pass and `dotnet run -- check` from `tools\HeadlessTerminal` still exits 0.
- [x] 2.3 Test first: `StepRunnerShould` (a step waits for its marker, then settles and sends its keys; a marker that never appears ends the run with exit code 4 and the step's marker in the output; a program that does not exit within six seconds of the last step is killed and gives 1; one that exits by itself gives 0), driven against a short `pwsh -NoProfile -Command` child that prints the markers and reads a key. Then the step runner over the shared session. Verify the tests pass.

## 3. The smoke script from Wording

- [x] 3.1 Add a project reference from `tools\HeadlessTerminal` to `src\Source191.Vantage.Core`. Verify `dotnet build -c Debug` passes with warnings as errors.
- [x] 3.2 Test first: `SmokeScriptShould` (each dialog marker equals `Wording.TabTitleDuring` of its `DialogName` and the closed marker `Wording.TabTitle`; the view markers are the first column each view has that the other lacks; each key is the one `SessionsKeys` or `WorktreesKeys` gives for its hint, `esc` and `enter` translated; the filter step types the worktree folder name passed in; a hint missing from the list fails with a message naming it; the steps run in the order of design.md's decision 4). Then `SmokeScript`. Verify the tests pass.

## 4. The smoke command

- [x] 4.1 Test first: `SmokeCheckShould` (exit 2 and no capture when the exe does not exist; the driver options pretend to be Windows Terminal and answer its queries; `CommandLineShould` covers argument quoting, not `Program.Main`, whose `smoke <exe> <worktree folder>` arm and usage line are wiring). Then `SmokeCheck`, which starts the exe with the step runner over `SmokeScript.Build(worktreeFolder)`, writes `drive-raw.txt`, prints each step as it passes and returns the runner's exit code (2 when the exe does not exist). Verify the tests pass.
- [x] 4.2 Pack with `dotnet pack src\Source191.Vantage -c Release -p:Version=<Version>-smoke.1`, install it with `dotnet tool install Source191.Vantage --tool-path <temporary folder> --add-source artifacts\packages --version <that version>`, and run `smoke` against `<temporary folder>\vantage.exe` and one of your linked worktrees, with the Claude variables cleared. Verify exit code 0 and that each step was printed.
- [x] 4.3 Document `smoke` in `tools\HeadlessTerminal\README.md` (what it opens, its exit codes 0, 1, 2 and 4, that it only ever cancels, that it needs the same environment clean-up as `check`) and correct the README's pointer to the unit tests to `tools\HeadlessTerminal.Tests`; add one line on `smoke` to CLAUDE.md's build section. Verify the README's commands run as written.

## 5. The smoke step in check

- [x] 5.1 In `.github\workflows\check.yml`, after `build-test-pack`: install Claude Code (not signed in, `DISABLE_AUTOUPDATER=1`), seed `~\source\repos\Northwind.Smoke` with one commit and its linked worktree `~\source\worktrees\Northwind.Smoke-smoke`, install the packed version into `$env:RUNNER_TEMP\vantage-tool`, run `smoke` against it with `Northwind.Smoke-smoke`, and upload `drive-raw.txt` as `smoke-capture` on failure; each step commented in the file's style. Verify the pull request's `check` run is green and its log lists every smoke step.
  - Result (2026-10-28): https://github.com/Source191/vantage/actions/runs/40646421424 is green on its second attempt, all 16 steps in about 11 s; the first attempt failed before the smoke step on the existing `DesktopShould.ReturnWithoutWaitingForTheOpenedProgramToEnd` timing (a flake this change does not touch). Force remove opened on the seeded worktree, whose repository has no remote.
- [x] 5.2 Prove it can fail: on a throwaway commit in the pull request, delete `Terminal.Gui.dll` from the tool path before the smoke step and confirm `check` goes red with the smoke step named; then drop that commit. Verify by the two run results, linked in the pull request's description.
  - Result (2026-10-28): run as draft pull request #76, stacked on this branch and closed unmerged, rather than a commit pushed and dropped here, so this branch's history never held it. https://github.com/Source191/vantage/actions/runs/40646524327 went red at the smoke step: Vantage crashed at start-up (exit 0xE0434352) and step 1 never saw the sessions view.

## Workflow follow-up

- No `Version` bump: nothing under `src\` changes, so `Assert-VersionBumped.ps1` does not require one.
- Evidence for the later licence decision (2026-10-28, draft pull request #77, closed unmerged): with `TextMateSharp.Grammars.dll` deleted from the tool path, the smoke check passed all 16 steps (https://github.com/Source191/vantage/actions/runs/40646534424). This covers the screens a clean machine reaches; this change does not drop the DLL.
- One pull request; archive the change in it with `/opsx:archive`.
