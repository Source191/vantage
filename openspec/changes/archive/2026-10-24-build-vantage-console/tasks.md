# Tasks

## 0. Spike (no code)

- [x] 0.1 For two working days start sessions with `claude --bg -n <name>` from the repository, use `claude agents` as the dashboard and `claude attach` in tabs; verify by noting in CLAUDE.md whether the idle stop and hook start-up are tolerable (the author: the native agent view is not tolerable; whether the cause is the view or the session model is still to be answered)
- [x] 0.2 (moved to the README roadmap on 2026-10-24; still to observe) Try `claude attach` to the same session from two tabs and record the outcome in CLAUDE.md
- [x] 0.3 (moved to the README roadmap on 2026-10-24; still to observe) Reboot once with sessions running, run `claude --bg --resume <id>` and `claude respawn --all`, and record which brings sessions back in CLAUDE.md
- [x] 0.4 Start a `--bg` session inside a hand-made sibling worktree, let it finish an edit, and record in CLAUDE.md whether it committed or pushed on its own
- [x] 0.5 Record in CLAUDE.md whether permission_prompt and idle_prompt notifications fire from an unattended background session, and whether `claude agents --json --all` distinguishes a deliberate stop from "ended while the background service was off"
- [x] 0.6 Decide Model A or Model B from the notes; if B, update design.md and the session-attach spec before continuing (Model A confirmed: the native view's screen was the problem, not the session model)

## 1. Solution scaffold

- [x] 1.1 Verify the current stable Terminal.Gui v2 and Spectre.Console versions through Context7 and record the choice in design.md
- [x] 1.2 Create Source191.Vantage.slnx with src/Source191.Vantage (packed as a global tool named vantage) and test/Source191.Vantage.UnitTests (xunit.v3, Shouldly, NSubstitute and its analysers, SonarAnalyzer.CSharp); verify `dotnet build` and `dotnet test` succeed with one placeholder test
- [x] 1.3 Copy .editorconfig, Directory.Build.props, global.json and nuget.config conventions from Northwind.Billing; verify `dotnet format --verify-no-changes` passes (nuget.config keeps only nuget.org because Vantage uses no internal packages; global.json adds the Microsoft.Testing.Platform runner, which .NET 10's dotnet test requires for xunit.v3)

## 2. Domain rules

- [x] 2.1 Test and implement SessionName slug derivation (lower-case, runs of non-alphanumerics to one hyphen, trimmed, empty rejected, semicolon and double quote rejected); verify SessionNameShould passes
- [x] 2.2 Test and implement SiblingWorktreePath and branch derivation from a repository path and slug; verify SiblingWorktreePathShould passes
- [x] 2.3 Test and implement DisplayState mapping from status and state pairs including the unknown case; verify DisplayStateShould passes
- [x] 2.4 Test and implement session row ordering by attention and start time; verify SessionOrderingShould passes
- [x] 2.5 Test and implement WorktreeHealth classification (ok, locked, orphan, broken, empty) from listing and folder facts; verify WorktreeHealthShould passes
- [x] 2.6 Test and implement RemovalDecision (refuse reasons, force rules, branch deletion rules); verify RemovalDecisionShould passes
- [x] 2.7 Test and implement TabColourSelection over a palette and a set of live tabs, including exhaustion; verify TabColourSelectionShould passes
- [x] 2.8 Test and implement TabRegistration liveness (pending window, PID plus start time match, stale removal); verify TabRegistrationShould passes
- [x] 2.9 Test and implement the text filter over name, repository and branch; verify RowFilterShould passes

## 3. Boundary adapters

- [x] 3.1 Define IClaudeCli (ListSessions, StartBackground, Attach, Stop, Remove, ResumeBackground, OpenAgentView) and implement ClaudeCliProcess; verify parsing tests against a recorded `claude agents --json --all` sample pass and an integration test runs when claude is on PATH
- [x] 3.2 Define IGit (ListWorktrees, Add, Remove, Prune, Status, Unpushed, LastCommit, DeleteBranch, DefaultBranch) and implement GitProcess; verify porcelain parsing tests pass and an integration test against a temporary `git init` repository passes
- [x] 3.3 Define IWindowsTerminal.OpenTab and implement it with an argument array and an encoded command; verify the argument-building test passes and a manual run opens a titled, coloured tab
- [x] 3.4 Define IProcessRegistry (process exists with start time) and implement it over System.Diagnostics.Process; verify the liveness integration test passes
- [x] 3.5 Implement the state folder store (tabs, recent.json, created.json, sessions.json with descriptions) over IFileSystem with corrupt-file tolerance; verify StateStoreShould passes using a temporary directory
- [x] 3.6 Implement the .worktreeinclude copier; verify WorktreeIncludeShould copies only gitignored matches in a temporary repository

## 4. Sessions view

- [x] 4.1 Test and implement the SessionsView reducer: load, refresh keeping selection, filter, header count, read-only interactive rows; verify SessionsViewShould passes
- [x] 4.2 Implement the sessions table rendering and key dispatch in the TUI layer with the five-second refresh timer; verify a manual run lists the current sessions and `/` filters them
- [x] 4.3 Test and implement StateTransitionNotifier (toast on a row entering needs input, completed or failed between refreshes, no duplicates); verify StateTransitionNotifierShould passes with a fake IToast (2026-10-24: toasts removed at the author's request; the notifier now rings the terminal bell when a background row starts waiting, and Claude Code's preferredNotifChannel terminal_bell covers attached tabs)
- [x] 4.4 Write docs/keys.md for the sessions view and verify every documented key is dispatched by the reducer tests (keys.md was folded into the README's key tables on 2026-10-23 and deleted)

## 5. Launch wizard

- [x] 5.1 Test and implement RepositoryCatalogue (roots scan, recent first, no-repository option, filter); verify RepositoryCatalogueShould passes
- [x] 5.2 Test and implement the LaunchPlan builder (name, optional description, mode, base, opening prompt, existing worktree reuse, empty slug refusal); verify LaunchPlanShould passes
- [x] 5.3 Test and implement LaunchService: create worktree if chosen, copy includes, start background session, record created items and recent repository, then attach, tab or list; verify LaunchServiceShould passes with fake boundaries
- [x] 5.4 Test and implement the untrusted-workspace path (detect the error, offer a plain tab, retry); verify LaunchServiceShould covers it
- [x] 5.5 Implement the wizard dialogs in the TUI layer; verify a manual run starts a named idle session in a chosen repository and it appears in the list

## 6. Attach and tabs

- [x] 6.1 Test and implement the attach effect (suspend, run child, redraw) with the Ctrl+Z footer hint, the rename hint variant and the native agent view variant; verify AttachEffectShould passes
- [x] 6.2 Test and implement OpenInTab (registry pending record, colour choice, refuse when live, terminal call, claim by the tab shell); verify OpenInTabShould passes
- [x] 6.3 Test and implement stop, delete with worktree offer, and resume-all; verify SessionActionsShould passes
- [x] 6.4 Wire the keys in the TUI layer; verify a manual run attaches, returns on Ctrl+Z, opens a coloured tab, and refuses a second tab for the same session

## 7. Worktrees view

- [x] 7.1 Test and implement WorktreeInventory (porcelain across roots plus the four leftover scan rules; broken detection reads the .git file's gitdir line, and prune is skipped when the owning repository no longer exists); verify WorktreeInventoryShould passes against temporary repositories
- [x] 7.2 Test and implement the WorktreesView reducer (columns, remove, force with typed confirmation, prune, start session here, open shell tab); verify WorktreesViewShould passes
- [x] 7.3 Implement the worktrees table and dialogs in the TUI layer; verify a manual run shows today's seven known rows with the expected health values (headless run 2026-10-23 showed 12 rows: the seven known plus the four empties counted singly, and two newer worktrees; the author's visual check pending)

## 8. Notification hook

- [x] 8.1 (removed 2026-10-24 with the toasts; hooks/ deleted and the Notification entry taken out of settings.json) Move Show-ClaudeToast.ps1 into hooks/ in this repository, make it read the session description from sessions.json, and add Pester tests for the title, description line, label and fallback rules by invoking the script with sample JSON; verify `Invoke-Pester` passes (8 tests; the script ran under Windows PowerShell 5.1 on 2026-10-23 with exit 0 and a log line; the author to confirm the toast shows the description as its third line)
- [x] 8.2 (removed 2026-10-24) Add hooks/Install-ClaudeToastHook.ps1 that copies the script to the user hooks folder and writes the Notification entry with async true; verify running it against a copy of settings.json produces the expected entry (5 tests; run against a copy of the author's settings.json the result was textually identical, then installed for real)

## 9. Packaging and profile

- [x] 9.1 Pack the tool and install it with `dotnet tool install -g --add-source`; verify `vantage` starts from a fresh tab (installed 0.1.0 on 2026-10-23 from artifacts\packages; `vantage here --bogus` answers from ~\.dotnet\tools; a fresh-tab start is the author's check. Reinstalling the same version needs `dotnet tool uninstall -g Source191.Vantage` first or a version bump, because the tool cache keeps 0.1.0)
- [x] 9.2 Add the profile alias and a README usage section; verify the documented commands run as written (profile block with `v` and `vh` appended 2026-10-23; README rewritten as the user guide with every key, a screenshot, a text render and the common scenarios; pack, tool install, Pester and `vantage here` commands were run as written, the rest are the author's checks)
- [x] 9.3 Add a `vantage here [name]` verb (or profile function) that starts a supervised session in the current folder and attaches in the same tab, so sessions started from any terminal are Vantage-managed from the outset; verify a session started that way shows a job id and opens with `o` (HereCommandShould; headless run 2026-10-23 printed "Started vantage-here-trial (60746919)", attached, sent the -p prompt and returned on Ctrl+Z; `o` on it is the author's check)
- [x] 9.5 (added 2026-10-23) `b` moves a terminal session under Vantage: end its process, `claude --bg --resume <sessionId> -n <name>`; refused while working; verified SessionActionsShould and SessionsViewShould
- [x] 9.4 (moved to the README roadmap on 2026-10-24; undecided) Decide whether to add a take-over key (`claude stop <id>` then `claude --resume <sessionId>` in a tab) for a normal foreground UI at the cost of leaving supervision; build it only if he wants it

## 10. Integration checks

- [x] 10.1 (moved to the README roadmap on 2026-10-24; the reboot part is still to observe) Run the full flow by hand: launch in a worktree, attach, open a tab, stop, delete with worktree removal, resume after a reboot; record the result in CLAUDE.md
- [x] 10.2 Run `dotnet test` and the mutation testing skill on the domain project; verify no surviving mutants in the rules under section 2 (Stryker 5.0.0 with the mtp runner over `**/Domain/*.cs`: 79.69 % before, 99.22 % after eight strengthened tests on 2026-10-23; the one survivor, First() to FirstOrDefault() in TabColourSelection, is equivalent because the palette is a fixed non-empty list)
- [x] 10.3 Turn the PtyHarness attach-return-wizard script into a repeatable check (exit code 0 within the deadline) so a UI-thread block after a hand-off cannot come back unnoticed (tools\HeadlessTerminal, `dotnet run -- check`: starts and removes its own throwaway session, exit 1 when Vantage had to be killed; passed on 2026-10-23 and again after the C# port on 2026-10-24)


## Workflow follow-up

- Archived on 2026-10-24 with the MVP complete; later work is raised as new changes.