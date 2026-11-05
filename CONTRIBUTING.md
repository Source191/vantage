# Contributing to Torni

Thank you for wanting to make Torni better. This guide holds what you need to develop it: how to propose a change, build and test it, how the code is organised, how every change goes through OpenSpec, and how to open a pull request. The [README](README.md) is the front page and [docs\user-guide.md](docs/user-guide.md) the user guide; [CLAUDE.md](CLAUDE.md) and `.claude\rules` are the full rulebook and the verified facts about Claude Code, Terminal.Gui, Windows Terminal and Windows that Torni depends on; [MAINTAINING.md](MAINTAINING.md) is the owner's guide to releasing.

## Contents

- [How to contribute](#how-to-contribute)
- [Setting up](#setting-up)
- [The build and test loop](#the-build-and-test-loop)
- [Trying your build](#trying-your-build)
- [The README's images](#the-readmes-images)
- [How the code is organised](#how-the-code-is-organised)
- [The code rules in brief](#the-code-rules-in-brief)
- [OpenSpec: how every change is made](#openspec-how-every-change-is-made)
- [Opening a pull request](#opening-a-pull-request)
- [Manual checks for tab and attach changes](#manual-checks-for-tab-and-attach-changes)
- [Continuous integration](#continuous-integration)
- [Versions](#versions)
- [The changelog](#the-changelog)

## How to contribute

- **Small fixes:** a typo, a broken link or an obvious one-line bug can go straight to a pull request.
- **Anything larger:** open an issue first and describe the problem or the idea, so we can agree the change before you spend time on a proposal and code. A bug report is best made from the console with `!` on any tab, which fills in the diagnostics.
- **Security problems:** never in a public issue; follow [SECURITY.md](SECURITY.md).
- **Review:** every pull request needs the owner's approval before it can merge.
- **Licence:** the code is under the [Apache License 2.0](LICENSE). By opening a pull request you agree your contribution is under the same licence (its section 5); there is no contributor licence agreement to sign.
- **Fixtures:** tests use made-up `Northwind.*` repositories and `C:\Users\me` paths, never real repositories, people or your own machine's paths.

## Setting up

It builds and runs on Windows 11 only. Install Claude Code as the README's [Install](README.md#install) section says, and the tools a build needs that winget would otherwise bring (skip a line for one you already have), then:

```powershell
winget install --id Microsoft.WindowsTerminal --exact --source winget   # already part of Windows 11 22H2 and later
winget install --id Microsoft.PowerShell --exact --source winget
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget
winget install --id Git.Git --exact --source winget
npm install -g @fission-ai/openspec   # the OpenSpec CLI
dotnet tool install -g dotnet-stryker # mutation testing, optional
git clone https://github.com/Source191/torni.git
cd torni
dotnet build -c Debug
```

- **SDK:** `global.json` chooses the SDK (10.0.301, rolling forward to the latest minor) and Microsoft.Testing.Platform as the test runner.
- **Claude Code:** the `/opsx:*` commands and the OpenSpec skills under `.claude\` work in any Claude Code session opened in the repository; the `openspec` CLI does the same work by hand. Trust the clone in Claude Code once (run `claude` in it and accept the prompt) if you will run the contract tests or the hand-off check.
- **Your environment:** never start `torni`, or anything that opens tabs, from inside a Claude Code session without clearing `NO_COLOR`, `CLAUDECODE`, `CLAUDE_PID` and `CLAUDE_CODE_*`. Windows Terminal hands the caller's environment to new tabs, and everything in them then runs monochrome.

## The build and test loop

```powershell
dotnet build -c Debug                                              # warnings are errors
dotnet test test\Source191.Torni.Core.Tests -c Debug --no-build  # the fast in-memory suite, a few seconds
dotnet test -c Debug --no-build                                    # every suite, including real git and processes
dotnet format --verify-no-changes
```

- **Warnings are errors:** style and analyser rules fail the build, so a clean build is most of the style review.
- **The hand-off check:** after any change to attach, `torni here` or the pseudo-console code, build Release and run it from `tools\HeadlessTerminal`. It starts and removes its own throwaway session, makes no model call, and exits 0 when it passes, 1 when `here` had to be killed after the detach, 2 when it could not start, and 3 when Claude's own screen never appeared.

  ```powershell
  dotnet build src\Source191.Torni -c Release
  cd tools\HeadlessTerminal; dotnet run -- check
  ```

- **The smoke check:** `dotnet run -- smoke <exe> <worktree-folder>` from the same folder opens and cancels every dialog and view of an installed tool, then quits; exit 0 passes. The worktree must be an idle linked worktree under a repository root holding two solutions. The tool's [README](tools/HeadlessTerminal/README.md) also covers `drive`, `grid` and `count`, for scripted keystrokes and reading a capture as a screen.
- **Claude Code contract tests:** they start real sessions, so an ordinary `dotnet test` skips them. Run them from a trusted repository with `$env:TORNI_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Torni.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"`.
- **Mutation testing:** from `test\Source191.Torni.Core.Tests`, run `dotnet stryker --test-runner mtp --project Source191.Torni.Core.csproj --mutate "**/Domain/*.cs"`. Use `--test-runner mtp` (xunit.v3 scores 0 % under the VSTest bridge) and globs like that one, never solution-relative paths.
- **Performance:** [docs/performance-monitoring.md](docs/performance-monitoring.md) explains how to watch the console over a working day with `TORNI_LOG`, `dotnet-counters`, `dotnet-trace` and the Windows Performance Recorder, and how to read the results before changing anything for speed.

## Trying your build

Build the self-contained program from your working copy, as winget would install it:

```powershell
.\tools\install-local.ps1
```

The script publishes `<Version>-local.<timestamp>` (a prerelease, so it never offers an update) to `%LOCALAPPDATA%\Torni\local`, replacing the last local build and stopping it first if it runs, and prints the full path of its `torni.exe`. Run it by that path: nothing is added to the PATH, so `torni` still runs your winget install, which the script never touches. Settings and state are shared with the installed Torni, as they live beside it in `%LOCALAPPDATA%\Torni`.

To try what a GitHub Actions run built, download the run's `torni-<version>-ci.<run number>-win-x64` artefact (the zip a release would attach) and extract it to the same place:

```powershell
gh run download <run id> --name torni-0.21.0-ci.42-win-x64 --dir .\downloaded
.\tools\install-local.ps1 -FromArtifact .\downloaded
```

A preview's GitHub prerelease carries the same zip, which `-FromArtifact` takes too. Remove a local build by deleting `%LOCALAPPDATA%\Torni\local`.

## The README's images

The README's screenshot (`assets\readme\sessions.png`) and animation (`assets\readme\new-session.gif`) are the real console over a made-up scene of Northwind.* repositories, made by one script from an ordinary Windows Terminal tab:

```powershell
.\tools\New-ReadmeScreenshot.ps1                    # both images
.\tools\New-ReadmeScreenshot.ps1 -Only Screenshot   # just the PNG
```

- **When to run it:** after a change that alters what the Sessions tab, the new-session wizard or the session tabs look like, so the README still shows the console as it is.
- **What it needs:** Windows Terminal, git, and ffmpeg for the animation (`winget install Gyan.FFmpeg`). The window it opens must be left alone while it runs; the workstation may be locked.
- **What it touches:** only the scene folder (`C:\ReadmeDemo` unless `-SceneRoot` says otherwise), which it deletes afterwards, and the two images. Your own settings and state, `~\.claude` and your sessions are never read or written, and every tab it opens stays in its own window.
- **Changing what they show:** [tools\ReadmeScene](tools/ReadmeScene/README.md) says how: the sessions and pull requests are in `scene.json`, the animation's keys in `new-session.keys`. Only made-up repositories and Torni itself are allowed in the scene.
## How the code is organised

| Project | What it holds |
| --- | --- |
| [src\Source191.Torni.Core](src/Source191.Torni.Core/README.md) | Domain (rules and value objects, depends on nothing), Ports (the boundary interfaces and their records), Application (use cases in Actions and Launch, effect routing in Effects, the Views reducers, the detail pane in Detail, Diagnostics, Reporting, TerminalSetup, ScreenLoop, CommandLine, Wording) |
| [src\Source191.Torni.Infrastructure](src/Source191.Torni.Infrastructure/README.md) | The adapters: git, the Claude CLI, Windows Terminal, processes, files, state, pull requests, package versions, diagnostics |
| [src\Source191.Torni.Tui](src/Source191.Torni.Tui/README.md) | Terminal.Gui rendering and dialogs, nothing else |
| [src\Source191.Torni](src/Source191.Torni/README.md) | Program.cs and its Composition folder, the only composition root (untested wiring), packed as the tool with the other three inside |
| [test\Source191.Torni.Core.Tests](test/Source191.Torni.Core.Tests/README.md) | The fast in-memory suite, including the architecture tests |
| [test\Source191.Torni.Infrastructure.Tests](test/Source191.Torni.Infrastructure.Tests/README.md) | The adapters against real git, processes and files, and the Claude Code contract tests |
| [test\Source191.Torni.TestDoubles](test/Source191.Torni.TestDoubles/README.md) | The shared hand-written fakes, one per port, no test framework |
| [tools\HeadlessTerminal](tools/HeadlessTerminal/README.md) and [its tests](tools/HeadlessTerminal.Tests/README.md) | Drives the built exe in a pseudo-console: the hand-off and smoke checks |
| [tools\ReadmeScene](tools/ReadmeScene/README.md) and [its tests](tools/ReadmeScene.Tests/README.md) | The real console over a made-up scene, for the README's screenshot and animation |

Each project's README holds what is worth knowing about it; a `.csproj` carries no comments (the two `Directory.Build.props` files, the root one and `src\Directory.Build.props`, keep their own beside their settings). `openspec\` holds the specifications and the changes made against them, `.github\` the workflows and their scripts, `assets\` the icon, artwork and the README's images, `docs\` the user guide.

## The code rules in brief

CLAUDE.md's "Code rules" are the authority; this is their shape.

- **Layers:** Domain, Ports, Application, Adapters, Tui, one namespace each, with dependencies pointing one way; `Architecture\LayeringShould` fails otherwise. Nothing outside the adapters touches files or processes, and nothing outside `Tui` touches Terminal.Gui.
- **Test-driven:** red, green, refactor, a commit per step. No untested code except the tool project's wiring (`Program.cs` and its `Composition` folder).
- **Sociable tests:** real collaborators inside the code; hand-written fakes from `TestDoubles` only at the boundaries, never a mocking library.
- **C# style:** concrete types rather than `var`, primary constructors, sealed records and classes, collection expressions, file-scoped namespaces, target-typed `new`, `TimeProvider`, braces always, one top-level type per file, no null guards (nullable reference types carry null, and `ArgumentNullException` is banned in `src`), the throw helpers for range checks, and switch expressions that name every member of a closed enum. `.editorconfig` and the architecture tests hold these, so the build reports a lapse.
- **Reducers:** each view (`Application\Views\*View.cs`) is a pure function of state and input returning effects; the `Tui` layer only renders and runs effects. A new key is the effect record, the reducer case and its test, a handler (`IEffectHandler<TEffect>`, one class per effect, with its outcome record and a `...HandlerShould` test) and its line in `Composition.Handlers`.
- **Values are types:** folders are `FolderPath`, ids are `SessionId`, `JobId` and `BranchName`, and so on. Each text value builds with `From`, parses outside text with `TryParse` and, where a person typed it, explains a problem with `Validate`. Models never use null for meaning.
- **Words live in `Wording`:** all user-facing text is in `Application\Wording`, in British English, with a fact in its tests. User text never comes from an exception message.
- **Privacy:** diagnostics record numbers, durations, versions, enums and type names only, never a path, a name, program output or anything typed; `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` enforce it.
- **Starting programs:** each is found once on the PATH as an `.exe` and run by its full path; no batch file is ever given text from a person, a repository or a listing; user text never reaches PowerShell source unquoted.

## OpenSpec: how every change is made

The project uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven development. Every piece of work, including refactors, tooling and documentation, starts as an OpenSpec change and is archived in the pull request that completes it.

- **`openspec\specs`:** what the code does today, one capability per folder (`session-listing`, `session-launch`, `worktree-management`, `settings` and so on), as `SHALL` requirements with `WHEN`/`THEN` scenarios. Read it to learn current behaviour.
- **`openspec\changes\<name>`:** a change in flight: `proposal.md`, `design.md`, `tasks.md` and delta specs under `specs\`.
- **`openspec\changes\archive`:** every finished change, dated. Search it only to learn why something was decided; never read an archived design as a description of the current code.
- **`openspec\config.yaml`:** the project context every change inherits and the rules for each artifact.

The stages, each with a Claude Code command and a CLI equivalent:

1. **Explore (optional):** `/opsx:explore` thinks an unclear idea through with you, reading the specs and the code, and writes nothing.
2. **Propose:** `/opsx:propose <name>` (or `openspec new change <name>`) writes the proposal (why, what, out of scope, impact), the delta specs (`ADDED`, `MODIFIED` or `REMOVED` requirements; read the current spec first with `openspec show <capability> --type spec`), the design (decisions, rejected alternatives, risks) and the tasks (numbered checkboxes, each one red-green-refactor cycle or smaller, domain first, then adapters, then screens). It is done when `openspec validate <name>` passes; commit it before writing code.
3. **Apply:** `/opsx:apply <name>` works through the tasks test-first, a commit per step, ticking each box. When the code shows the plan was wrong, change the plan (`/opsx:update`) rather than quietly building something else.
4. **Record:** a task that needs a person (a check in a real Windows Terminal) is ticked only once done, with a note saying what was seen, when and on which build.
5. **Archive:** `/opsx:archive <name>` once every task is ticked, in the same pull request, so `openspec\specs` on `main` always describes the code on `main`.

A refactor, tooling or documentation change still gets a proposal and tasks but has nothing to say to the specs: it declares `skip_specs: true` in its `.openspec.yaml`, and `openspec validate` accepts it with no delta specs.

## Opening a pull request

Before asking for review, check that:

- **The issue:** for anything beyond a small fix, the pull request links the issue where the change was agreed.
- **The change:** its OpenSpec change has every task ticked, manual checks recorded, and is archived in this pull request (`openspec validate` passes).
- **The loop:** `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and the hand-off check exits 0 if you touched attach, `torni here` or the pseudo-console.
- **The version:** a pull request that changes the packaged tool raises `Version`; see [Versions](#versions).
- **The changelog:** the same pull request adds its line to `CHANGELOG.md` under Unreleased; see [The changelog](#the-changelog).
- **The documents:** the README stays a short front page with a getting-started guide, `docs\user-guide.md` holds every key, column, setting and command a user meets, and the `?` help, tooltips and `openspec\specs` describe the behaviour a user will notice; any newly verified fact about Claude Code, Terminal.Gui, Windows Terminal or Windows is recorded in its `.claude\rules` file with the date and version; a project's README holds any fact worth knowing about it.
- **Manual checks:** a change to how `wt.exe`, tabs or attach are driven has been through the [manual checks](#manual-checks-for-tab-and-attach-changes).

## Manual checks for tab and attach changes

CI runs the console inside a pseudo-console, so rendering, keys and the attach through `torni here` are covered; anything that goes out through `wt.exe` is not, because a runner has no desktop. Before merging a pull request that changes how tabs are opened, titled or coloured, or how a session is attached, build it with `tools\install-local.ps1`, start it by the path the script prints and check in a real Windows Terminal:

1. Enter on a session opens a coloured tab titled with the session name, and Enter again switches to that tab while it is open.
2. `n`, choose a repository, name the session, leave `New worktree` chosen, F5: a new tab opens attached to the new session, its REPOSITORY cell reads the repository, and the Worktrees tab lists the new worktree.
3. Ctrl+Z in that tab closes it, and the TAB block clears within one refresh.
4. `d` on a session shown in its tab ends it and the tab closes by itself.
5. A background session that pauses for input turns its row amber and rings the bell in the console's tab while another tab is in front.
6. The console's own tab is titled `Torni` and green, reads `Torni (Keys)` while `?` is open and `Torni` after Esc, and goes back to the profile's title and colour on `q`.
7. `torni here` in a trusted repository prints "Ctrl+Z detaches; the session keeps running.", attaches in the same tab, and Ctrl+Z returns to the shell with the line gone.

After a Terminal.Gui update, check the dialog titles without a terminal: from `tools\HeadlessTerminal`, `dotnet run -- drive <torni.exe> '8000:?,2000:\u001b,2000:q'`, then the `ESC ] 0 ;` titles in `drive-raw.txt` must read `Torni`, `Torni (Keys)`, `Torni`. Terminal.Gui writes every window's title to the terminal unless told not to, so a new dialog that skips `DialogTabTitle.Apply` shows up here.

## Continuous integration

`.github\workflows\pipeline.yml` runs on every pull request and push to `main` on a Windows runner, and its `check` job must pass, on a branch up to date with `main`, before a pull request can merge.

- **What `check` does:** checks the version bump and the changelog line, builds in Release, runs every test, checks whitespace, publishes the self-contained winget build and zips it as `torni_<Version>-ci.<run number>_win-x64.zip`, uploads the zip as an artefact (see [Trying your build](#trying-your-build)), then extracts it and runs the smoke check against its `torni.exe`.
- **Documentation-only pull requests:** one that changes only files under `openspec\` or `.md` files outside `.github\` passes after the version and changelog checks without building. Run the tests yourself if you touched the README or the user guide, because `DocumentedMinimumShould` reads both.
- **The hand-off job:** a second job, `handoff`, runs the hand-off check against a real throwaway session when the repository's Claude Code token is available; it never blocks a merge.
- **CodeQL:** `.github\workflows\codeql.yml` scans the C# and the workflows; it is not a required check.
- **Owner-only workflows:** `publish.yml` submits a stable release to winget and `claude-contract.yml` checks each night's Claude Code CLI; [MAINTAINING.md](MAINTAINING.md) describes both.

## Versions

Every pull request that changes the packaged tool (anything under `src\`, or `Directory.Build.props`, `global.json` or `nuget.config`) raises `Version` in `src\Source191.Torni\Source191.Torni.csproj`, because each merge of one is a release. Versions follow [semantic versioning](https://semver.org/):

- **Patch:** fixes, refactors with no behaviour change, and changes to text.
- **Minor:** a new key or a new behaviour that keeps existing keys working.
- **Major:** a change that breaks how existing keys, state files or the `torni here` command line behave. While the tool is `0.y.z`, a minor bump may break.

```powershell
.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD   # the check, locally
.github\scripts\Step-Version.ps1                                 # raise Version one step; -WhatIf only prints it
```

`Step-Version.ps1` raises the prerelease number when there is one, otherwise the patch. The check compares semantic versions, so 0.10.0 is above 0.9.0. Because branches must be up to date before merging, two pull requests that bump to the same version cannot both merge: the second fails until it bumps again.

Releasing and publishing are the owner's; see [MAINTAINING.md](MAINTAINING.md).

## The changelog

`CHANGELOG.md` is the one source of release notes: the zip carries it beside the program, the update check reads a newer release's from the changelog at its tag, the What's new dialog shows it before an update and once after one, and a stable GitHub Release starts with its section. It is written for the people who use it, in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)'s shape, which Torni reads line by line:

- **Where:** every pull request that changes the packaged tool adds one line under `## Unreleased`, below one of the headings `### Added`, `### Changed`, `### Fixed`, `### Removed`, `### Security` or `### Internal`, in that order. `Assert-ChangelogUpdated.ps1` fails the pull request otherwise; a change outside the tool needs no line.
- **How:** one line starting `- `, saying what a user will notice in words they would use, ending with the pull request's number, `(#171)`. Nothing else on the line is read as Markdown, so no links, bold or tables; control characters are dropped.
- **Internal:** a refactor, a test, tooling or anything else nobody using it can see goes under Internal, which the What's new dialog never shows. Dependabot's pull requests get theirs from `pipeline.yml`, which raises `Version` and runs `Add-InternalChangelogEntry.ps1` with the pull request's title.

```powershell
.github\scripts\Assert-ChangelogUpdated.ps1 -Base main -Head HEAD   # the check, locally
```

### Releasing a stable version

The pull request that raises `Version` to a stable version (no `-preview.N`) turns `## Unreleased` into that version's section, `## 0.21.0 - 2026-11-20`, and starts a new, empty `## Unreleased` above it; the check fails while the section is missing or Unreleased still has entries. Preview versions leave the entries under Unreleased, so the stable section collects every preview's changes.
