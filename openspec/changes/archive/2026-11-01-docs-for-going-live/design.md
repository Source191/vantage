# Design

## Context

See proposal.md for why. The review's findings are in the owner's plan, `Documents\Vantage\Vantage-docs-go-live-plan.md`, outside the repository; the tasks below repeat each finding they act on, so the change stands on its own.

- **The build checks crefs:** `GenerateDocumentationFile` is on and warnings are errors, so a cref that names nothing breaks it; CS1591 (a missing comment) is suppressed.
- **The README is the package page:** it doubles as the nuget.org readme (`PackageReadmeFile`), and `DocumentedMinimumShould` asserts it contains "2.1.309 or later".
- **CI and documentation:** a pull request of only `.md` files and `openspec\` skips the build, but this one changes `src\`, so the full check and the version check run.
- **Path-scoped rules:** files in `.claude\rules` with a `paths:` list load into a Claude Code session only when a matching file is read or edited; `CLAUDE.md` loads always.

## Goals / Non-Goals

**Goals:**
- Every statement in the README, CONTRIBUTING, CLAUDE.md, the rules files and the project READMEs is true of the code on this branch.
- A user can install Vantage and use it daily from the README without reading internals.
- A contributor finds in CONTRIBUTING everything needed to build, test, propose and open a pull request, and nothing addressed only to the owner.
- `CLAUDE.md` holds only what applies anywhere in the code, at about half its size; nothing is lost, only moved.

**Non-Goals:**
- Any behaviour change beyond the three corrected messages.
- Rewording the specs, or adding comments for their own sake: a comment is added only where its absence misleads.

## Decisions

**One change and one pull request.** The owner chose one pull request over four stacked ones, so the review reads the whole picture once. Commits are grouped by area (wording, comments by layer, README, CONTRIBUTING and MAINTAINING, CLAUDE.md and rules, the rest), so the history still reads by subject.

**The three messages are corrected test-first.** Each message has a fact in `WordingShould` or `WordingOfOutcomesShould`. The fact is changed to the new words first (red), then the string (green).

**The README keeps its reference tables and loses its internals.** The in-app help (`?`) is generated from `Wording`, so it is always exact; the README has to get someone from install to daily use and answer "what does this key or column mean".

- **Kept:** the column and key tables for all three tabs, the wizard, session tabs, `vantage here`, setup-terminal, scenarios, troubleshooting, where files live, updates and reporting.
- **Dropped:**
  - Version history ("before 0.8.0", "before 0.11.0", "before 0.20.0", `recent.json`, `descriptions\`, `sessions.json`, Cursor).
  - Lookup timings and limits.
  - The `cmd.exe` metacharacter list.
  - The `=`/`:`/`@` parsing detail.
  - Shade and frame colours.
  - The memory-measurement method.
  - The report-trimming order.
  - Test class names and `tools\HeadlessTerminal`.
- **Merged:** "What Vantage trusts" and "What Vantage records" become "Security and privacy", which keeps every promise (what runs, what is deleted, what is never recorded or sent) in fewer words and links `SECURITY.md`.
- **Target size:** about 30 KB.

**The owner's sections leave CONTRIBUTING.** Releases, publishing, reviews, Dependabot's token, supply chain, the hand-off job and the nightly contract check move whole to `MAINTAINING.md`, with a two-line pointer left in CONTRIBUTING. The references that name "CONTRIBUTING.md, Dependabot and the version bump token" (the `check.yml` message, `dependabot.yml`) move with them. `Assert-VersionBumped.ps1`'s pointer to "CONTRIBUTING.md, Versions" stays, because Versions stays.

**CLAUDE.md keeps rules that apply anywhere; facts for one area move to path-scoped rules.**

- **`claude-code.md` (new):** the whole "Claude Code CLI facts" section with its verification date and the re-verification instruction. Paths:
  - `**/*Claude*.cs`
  - `**/*Session*Handler*.cs`
  - `**/AutomatedRun*.cs`
  - `**/ITranscripts.cs`
  - `**/TabScripts*.cs`
  - `src/Source191.Vantage.Infrastructure/Claude/**`
  - `src/Source191.Vantage.Core/Application/Launch/**`
  - `test/Source191.Vantage.Infrastructure.Tests/Contract/**`
  - `openspec/changes/**` and `openspec/specs/session-*/**`, so a proposal about sessions loads it.

  CLAUDE.md keeps three must-knows inline: never run `claude --bg --resume --help`; resuming a listed background job with flags starts a copy, so `rm` it first; never script edits to the trust records.
- **`security.md` (new):** the "Security" and "Diagnostics" detail. CLAUDE.md keeps one line per invariant:
  - programs found by `ProgramLocator` and run by full path;
  - no user text to a batch file;
  - PowerShell values only through `PowerShellScript.Quote`;
  - nothing deleted except by `WorktreeRemover`;
  - no command line, message or typed text kept or logged.

  Paths: the Processes, PullRequests and Terminal adapter folders, `Application\Diagnostics`, and the named files (`TabScripts`, `PowerShellScript`, `BatchFileArgument`, `RemoteRepository`, `Editor*`, `WorktreeRemover`, `LeftoverFiles`, `RemoveWorktreeHandler`, `DeleteSessionHandler`, `SessionId`, `ProcessIdentity`, `ProgramLabel`, `PathFolders`, `ProgramLocator`, `*Executable*`, `SettingsProblem`, `LaunchPlanBuilder`, `HereCommand*`, `GitPointer`, `WorktreeHealth*`, `WorktreeInventory`, `TerminalTab`, `TabRegistration*`, `*Privacy*`).
- **`console-screen.md`:** gains the wizard and settings-dialog paragraphs, because its paths already cover `Application\Views` and `Tui`.
- **`checks.md`:** gains the contract-test command detail, the mutation-testing command and the smoke exit codes.
- **Pointer index:** CLAUDE.md's index of rules files gains the two new files.
- **Trimmed in place:**
  - The C# idioms keep each rule but drop the analyser ID lists and their rationale, because a build error names its own ID.
  - The release paragraph keeps the bump rule and points to CONTRIBUTING's Versions.
  - "Open items" is dropped: the README's roadmap has them.
- **Your global CLAUDE.md:** the C# idioms that repeat the owner's global CLAUDE.md stay, because a contributor's Claude Code does not have that file.

**Pointers follow the facts.** `openspec\config.yaml`'s context names `.claude\rules\claude-code.md` for the CLI facts. `ClaudeHelpSurfaceShould`'s summary and `claude-contract.yml`'s header name the same file. `check.yml`'s comments name `.claude\rules\checks.md`, where the headings they cite already live.

**Immutable releases are described as off.** `gh api repos/Source191/vantage/immutable-releases` answered `enabled: false` on 1 November 2026. MAINTAINING.md and `checks.md` say they are off and that a fix is a new version either way. Turning them on is the owner's separate decision.

**`docs\architecture-review.md` is deleted.** Git keeps it, and the nine proposals it produced are archived.

## Risks / Trade-offs

- **[A planning session that reads only the specs misses the CLI facts]:** `claude-code.md`'s paths include `openspec/changes/**` and the session specs, and `openspec\config.yaml` names the file, so a proposal loads it.
- **[A rule moved out of CLAUDE.md is missed in an area its globs do not cover]:** CLAUDE.md keeps every security invariant as one line, and only the detail behind each moves.
- **[The README loses something a user needed]:** each cut is internals or history. The keys, columns, files, settings, exit codes and privacy promises all stay, and the in-app help remains the full reference.
- **[The README's minimum version is reworded]:** `DocumentedMinimumShould` fails the full build, which this pull request runs.
- **[One large pull request is hard to review]:** commits are grouped by area, and the three behaviour changes (the messages) are isolated in their own red and green commits.
