# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the test is named before the code. Unless a task says otherwise, verify with `dotnet build -c Debug` (warnings are errors, and crefs are checked) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`. No task adds an em dash.

## 1. The three messages

- [x] 1.1 `WordingOfOutcomesShould.SayWhichEditorCouldNotBeFound` expects "Could not find Rider; choose another under Open folder with on the Config tab (3)." Verify it fails red on the old ", to choose another editor" text, then change `WordingOfOutcomes`'s `EditorNotFound` arm and verify it is green.
- [x] 1.2 `WordingOfOutcomesShould.SayAFailedRenameWasPutBack` expects "...and its tab is closed: Enter opens a new one." Red, then the `Restored { TabClosed: true }` arm, then green.
- [x] 1.3 `WordingShould.ExplainATerminalSessionWithMessages` expects "fix-123 runs in its own terminal. Press b to move it under Vantage (its old tab closes), or type /bg there." Red, then the `RunsInOwnTerminal { HasTranscript: true }` arm in `Wording`, then green.

## 2. XML comments

- [x] 2.1 Domain and Ports:
  - **Wrong:** ClaudeCodeSupport (the minimum is the oldest supported, not the verified one); InheritedEnvironment (also drops GIT_TERMINAL_PROMPT); GitPointer.RegisteredElsewhere (any live gitdir); RemovalDecision.CanBeForced (also false outside the roots); IClaudeCli ("nothing else is touched"); WorktreeSlug (ASCII letters); JobId.TryParse (shorter than eight); IWindowsTerminal (window by process id); Settings.Validate (duplicate roots); IGit's summary (every non-nullable method but IsMerged throws); TerminalProfiles (only when the fragment exists).
  - **Stale:** SessionName ("the constructor" is `From`); EditorChoice (point to IEditorLocator).
  - **Unclear:** the garbled UpdateCheckRule line.
  - **Gaps to fill:** IProcessRegistry.StartTimeOf, HealthReason's undocumented cases, BranchAction, ColourChoice, ResumeResult, WorktreeRemovalFacts.
  - **Verify:** the build passes.
- [x] 2.2 Application:
  - **Wrong:** the "standing message" in SessionsViewState, IListViewState and StatusMessage.Blank; WordingOfConfig.ConfigDescription (the tooltip only); HereCommandRunner (2 for a refused name or prompt); StartSession ("when asked"); Outcome (own files); WorktreeInventory.WorktreeRootCandidates (elsewhere); ConsoleView (also OpenSession).
  - **Stale:** WizardOrigin and WizardState (`n`, not `s`); ResumeSessions and StatusMessage.NothingToResume (no checklist); ConsoleState and ConsoleExit (no hand-off); IConsoleScreen and ISwitchesTab (three tabs); ShowProblemReport and UpdateVantage (any tab); SaveSettingsOutcome (no header); StatusMessage's "attach" cases; TerminalEffects (only `vantage here`); ConfigView's `//` comment on the status line.
  - **Noise:** the empty DetailLoader params, the DeleteSessionOutcome leftover line, the "as before" phrases in Attempt and CrashRecorder.
  - **Gaps to fill:** LaunchPlanBuilder, StartSessionIn, AttachToSession, DeleteSession, StopSessionHandler, ViewUpdate, FailureSite, Started.ClaudeCode.
  - **One name:** "header" and "settings line" become "settings row" in comments.
  - **Verify:** the build passes.
- [x] 2.3 Infrastructure, Tui, tools and tests:
  - **Wrong:** TableBody.ScrollStep (the duplicate summary); Palette's selection overload; GitCliShould's command list; DisplayStateShould and ClaudeStatesShould (seven states); ClaudeAgentsListing's skipped row.
  - **Stale:** ConsoleScreen (three tabs, one settings row, the mark, Show reading the setup); ScreenContext (one row; wrap the long line); Palette (every tab's frame); DialogEffects (every tab); ITabBody (no standing hint); ConfigScreenDefinition.Description (the tooltip); ConsoleRecovery and TerminalQueries (`vantage here`, not the console); LayeringShould (the split is done, nothing is skipped); EditorLocator (Rider and Explorer).
  - **Gaps to fill:** ClaudeCli's summary.
  - **Markup:** backtick code in ConsoleScreen and Palette becomes `<c>`.
  - **CI comment:** `check.yml`'s "v on the worktree" becomes `s`.
  - **Verify:** the build passes.

## 3. The version

- [x] 3.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to 0.20.0-preview.8. Verify `.github\scripts\Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passes.

## 4. README

- [x] 4.1 Correct every wrong statement:
  - the settings row with an update notice (both places);
  - the key bar per row kind and per tab;
  - STATE's `?` words and the missing `● starting`;
  - the PR column (Sessions only; the worktree pane's PR line);
  - lookups for every listed row;
  - the Config sample's "Repository roots";
  - `b` closing the old tab;
  - `d` removing the worktree after one confirmation;
  - the untrusted repository's Claude tab;
  - `U` and `!` on any tab;
  - the report keeping failures last;
  - the obsolete hand-off troubleshooting entry replaced by a `VANTAGE_LOG` tip.

  Add the omissions:
  - the worktrees `?`, `q` and Up/Down;
  - `vantage` and `vantage here` exiting 1 when Claude Code or git is missing;
  - the help's Pane section;
  - Copy name (F2);
  - `.slnx` over `.sln`;
  - the setup rows' values;
  - `none` and Changed in the pane.
- [x] 4.2 Cut and reorder as design.md says: drop the version history, the lookup internals, the `cmd.exe` list, the parsing detail, the shade and frame colours, the memory method, the trimming order, and the test and tool names; move the Config tab beside the other tabs; merge the trust and records sections into "Security and privacy" linking `SECURITY.md`; keep "2.1.309 or later" word for word. Verify the README is about 30 KB, every Contents link resolves to a heading, and `dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-class "*DocumentedMinimumShould"` passes.

## 5. CONTRIBUTING and MAINTAINING

- [x] 5.1 Correct CONTRIBUTING:
  - **Hand-off check:** `dotnet build src\Source191.Vantage -c Release`, then `dotnet run -- check` from `tools\HeadlessTerminal`. Exit codes 1 (killed after the detach), 2 (could not start) and 3 (no Claude screen). It is due after a change to attach, `vantage here` or the pseudo-console.
  - **Installing by hand:** add `--prerelease`.
  - **Code map:** Effects, Diagnostics, Reporting and TerminalSetup in Application; pull requests and package versions in the adapters.
  - **Fakes:** "every port".
  - **Removed:** the archive pointer, and manual-check clauses about `o` and F6.
- [x] 5.2 Move "Releasing and maintaining" whole to a new `MAINTAINING.md`; say immutable releases are off; leave a two-line pointer; shorten "Why the specs and the archive are kept for ever", the OpenSpec stages and the CI section; repoint `check.yml`'s and `dependabot.yml`'s "CONTRIBUTING.md, Dependabot and the version bump token" to MAINTAINING.md. Verify every link and anchor between README, CONTRIBUTING and MAINTAINING resolves.

## 6. CLAUDE.md and the rules files

- [x] 6.1 Create `.claude\rules\claude-code.md` with the CLI facts section and its paths, and `.claude\rules\security.md` with the security and diagnostics detail and its paths, as design.md lists. Move the wizard and settings-dialog paragraphs to `console-screen.md`, and the contract, mutation and smoke detail to `checks.md`.
- [x] 6.2 Rewrite CLAUDE.md:
  - **Kept and trimmed:** what applies anywhere, with the inline must-knows and invariants from design.md; the index gains the two files; "Open items" is dropped.
  - **Corrected:**
    - the hand-off command;
    - every port faked;
    - the Application and Adapters lists, and TerminalSetupRunner;
    - IEffectHandler, Composition and EffectDispatcher in `Application\Effects`;
    - the Wording test files;
    - ClaudeSettings' `catch (ArgumentException`;
    - `ReportTitle.Validate` beside `SessionName`'s.

  Verify it is about 16 KB, and that every fact in the old file is in CLAUDE.md or a rules file (a section-by-section check against `git show HEAD~:CLAUDE.md`).
- [x] 6.3 Fix the rules files: `console-screen.md` ClearsBySelf and SuccessShownFor; `terminal-gui.md` CellPaint.Fill; `windows.md`'s TerminalSetup glob; `locale.md` keys `c`, `f`, `s`; `checks.md` hand-off exit 2, the smoke steps (fixed-folder wizard, solution chooser, two solutions) and immutable releases off. Repoint `openspec\config.yaml`, `ClaudeHelpSurfaceShould`, `claude-contract.yml` and `check.yml`'s comments. Verify every `paths:` glob matches a file (a PowerShell check over the globs) and `openspec validate docs-for-going-live --strict` passes.

## 7. The rest

- [x] 7.1 Project READMEs:
  - `src\Source191.Vantage`: InvariantGlobalization and vantage.ico.
  - Infrastructure: add pull requests and packages.
  - TestDoubles: every port, and why it references Infrastructure.
  - `tools\HeadlessTerminal`:
    - the full class list;
    - AllowUnsafeBlocks and InvariantGlobalization;
    - two solutions in the smoke worktree;
    - `8000:2` in the drive example.
- [x] 7.2 Delete `docs\architecture-review.md`. Verify nothing links to it (`git grep architecture-review`).

## 8. Whole-solution check and archive

- [x] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate docs-for-going-live --strict` all pass; record the totals here. (1 November 2026 on `v-doc`: build 0 warnings; 2326 tests, 2317 passed, 9 skipped (the Claude Code contract tests), 0 failed; format clean after the working tree's line endings were re-checked out; validate passes. Sizes: README 64 KB to 37.6 KB, the two ASCII screens and the reference tables being most of it; CLAUDE.md 33 KB to 19.2 KB, about 4,800 tokens; every rules glob matches a file.)
- [x] 8.2 Archive the change (no spec sync: `skip_specs`) in the same pull request.
