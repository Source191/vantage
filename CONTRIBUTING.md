# Contributing to Vantage

Thank you for wanting to make Vantage better. This guide holds what you need to develop it: how to propose a change, build and test it, how the code is organised, how every change goes through OpenSpec, and how to open a pull request. The [README](README.md) is the user guide; [CLAUDE.md](CLAUDE.md) and `.claude\rules` are the full rulebook and the verified facts about Claude Code, Terminal.Gui, Windows Terminal and Windows that Vantage depends on; [MAINTAINING.md](MAINTAINING.md) is the owner's guide to releasing.

## Contents

- [How to contribute](#how-to-contribute)
- [Setting up](#setting-up)
- [The build and test loop](#the-build-and-test-loop)
- [Trying your build](#trying-your-build)
- [How the code is organised](#how-the-code-is-organised)
- [The code rules in brief](#the-code-rules-in-brief)
- [OpenSpec: how every change is made](#openspec-how-every-change-is-made)
- [Opening a pull request](#opening-a-pull-request)
- [Manual checks for tab and attach changes](#manual-checks-for-tab-and-attach-changes)
- [Continuous integration](#continuous-integration)
- [Versions](#versions)

## How to contribute

- **Small fixes:** a typo, a broken link or an obvious one-line bug can go straight to a pull request.
- **Anything larger:** open an issue first and describe the problem or the idea, so we can agree the change before you spend time on a proposal and code. A bug report is best made from Vantage itself with `!` (see the README's [Reporting a problem](README.md#reporting-a-problem)), which fills in the diagnostics.
- **Security problems:** never in a public issue; follow [SECURITY.md](SECURITY.md).
- **Review:** every pull request needs the owner's approval before it can merge.
- **Licence:** Vantage is under the [Apache License 2.0](LICENSE). By opening a pull request you agree your contribution is under the same licence (its section 5); there is no contributor licence agreement to sign.
- **Fixtures:** tests use made-up `Northwind.*` repositories and `C:\Users\me` paths, never real repositories, people or your own machine's paths.

## Setting up

Vantage builds and runs on Windows 11 only. Install what the README's [Prerequisites](README.md#prerequisites) list (Windows Terminal, PowerShell 7, the .NET 10 SDK, git and Claude Code), then:

```powershell
npm install -g @fission-ai/openspec   # the OpenSpec CLI
dotnet tool install -g dotnet-stryker # mutation testing, optional
git clone https://github.com/Source191/vantage.git
cd vantage
dotnet build -c Debug
```

- **SDK:** `global.json` chooses the SDK (10.0.301, rolling forward to the latest minor) and Microsoft.Testing.Platform as the test runner.
- **Claude Code:** the `/opsx:*` commands and the OpenSpec skills under `.claude\` work in any Claude Code session opened in the repository; the `openspec` CLI does the same work by hand. Trust the clone in Claude Code once (run `claude` in it and accept the prompt) if you will run the contract tests or the hand-off check.
- **Your environment:** never start `vantage`, or anything that opens tabs, from inside a Claude Code session without clearing `NO_COLOR`, `CLAUDECODE`, `CLAUDE_PID` and `CLAUDE_CODE_*`. Windows Terminal hands the caller's environment to new tabs, and everything in them then runs monochrome.

## The build and test loop

```powershell
dotnet build -c Debug                                              # warnings are errors
dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build  # the fast in-memory suite, a few seconds
dotnet test -c Debug --no-build                                    # every suite, including real git and processes
dotnet format --verify-no-changes
```

- **Warnings are errors:** style and analyser rules fail the build, so a clean build is most of the style review.
- **The hand-off check:** after any change to attach, `vantage here` or the pseudo-console code, build Release and run it from `tools\HeadlessTerminal`. It starts and removes its own throwaway session, makes no model call, and exits 0 when it passes, 1 when `here` had to be killed after the detach, 2 when it could not start, and 3 when Claude's own screen never appeared.

  ```powershell
  dotnet build src\Source191.Vantage -c Release
  cd tools\HeadlessTerminal; dotnet run -- check
  ```

- **The smoke check:** `dotnet run -- smoke <exe> <worktree-folder>` from the same folder opens and cancels every dialog and view of an installed tool, then quits; exit 0 passes. The worktree must be an idle linked worktree under a repository root holding two solutions. The tool's [README](tools/HeadlessTerminal/README.md) also covers `drive`, `grid` and `count`, for scripted keystrokes and reading a capture as a screen.
- **Claude Code contract tests:** they start real sessions, so an ordinary `dotnet test` skips them. Run them from a trusted repository with `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"`.
- **Mutation testing:** from `test\Source191.Vantage.Core.Tests`, run `dotnet stryker --test-runner mtp --project Source191.Vantage.Core.csproj --mutate "**/Domain/*.cs"`. Use `--test-runner mtp` (xunit.v3 scores 0 % under the VSTest bridge) and globs like that one, never solution-relative paths.

## Trying your build

Pack and install the tool from your working copy:

```powershell
.\tools\install-local.ps1
```

The script stops a running `vantage.exe`, packs the tool as `<Version>-local.<timestamp>` and installs or updates the global tool. The suffix matters: the tool cache keeps the first package it saw for a version, so two plain builds of one version would run the first. `-Clean` uninstalls first.

To test the package a GitHub Actions run built, download the run's `Source191.Vantage-<version>-ci.<run number>` artefact and install from the folder:

```powershell
gh run download <run id> --name Source191.Vantage-0.20.0-ci.42 --dir .\downloaded
.\tools\install-local.ps1 -FromArtifact .\downloaded
```

Go back to the published tool with `dotnet tool update -g Source191.Vantage` (add `--prerelease` to stay on previews).

## How the code is organised

| Project | What it holds |
| --- | --- |
| [src\Source191.Vantage.Core](src/Source191.Vantage.Core/README.md) | Domain (rules and value objects, depends on nothing), Ports (the boundary interfaces and their records), Application (use cases in Actions and Launch, effect routing in Effects, the Views reducers, the detail pane in Detail, Diagnostics, Reporting, TerminalSetup, ScreenLoop, CommandLine, Wording) |
| [src\Source191.Vantage.Infrastructure](src/Source191.Vantage.Infrastructure/README.md) | The adapters: git, the Claude CLI, Windows Terminal, processes, files, state, pull requests, package versions, diagnostics |
| [src\Source191.Vantage.Tui](src/Source191.Vantage.Tui/README.md) | Terminal.Gui rendering and dialogs, nothing else |
| [src\Source191.Vantage](src/Source191.Vantage/README.md) | Program.cs, the only composition root, packed as the tool with the other three inside |
| [test\Source191.Vantage.Core.Tests](test/Source191.Vantage.Core.Tests/README.md) | The fast in-memory suite, including the architecture tests |
| [test\Source191.Vantage.Infrastructure.Tests](test/Source191.Vantage.Infrastructure.Tests/README.md) | The adapters against real git, processes and files, and the Claude Code contract tests |
| [test\Source191.Vantage.TestDoubles](test/Source191.Vantage.TestDoubles/README.md) | The shared hand-written fakes, one per port, no test framework |
| [tools\HeadlessTerminal](tools/HeadlessTerminal/README.md) and [its tests](tools/HeadlessTerminal.Tests/README.md) | Drives the built exe in a pseudo-console: the hand-off and smoke checks |

Each project's README holds what is worth knowing about it; a `.csproj` carries no comments (the root `Directory.Build.props` keeps its own beside its settings). `openspec\` holds the specifications and the changes made against them, `.github\` the workflows and their scripts, `assets\` the icon and artwork.

## The code rules in brief

CLAUDE.md's "Code rules" are the authority; this is their shape.

- **Layers:** Domain, Ports, Application, Adapters, Tui, one namespace each, with dependencies pointing one way; `Architecture\LayeringShould` fails otherwise. Nothing outside the adapters touches files or processes, and nothing outside `Tui` touches Terminal.Gui.
- **Test-driven:** red, green, refactor, a commit per step. No untested code except `Program.cs` and wiring.
- **Sociable tests:** real collaborators inside the code; hand-written fakes from `TestDoubles` only at the boundaries, never a mocking library.
- **C# style:** concrete types rather than `var`, primary constructors, sealed records and classes, collection expressions, file-scoped namespaces, target-typed `new`, `TimeProvider`, braces always, one top-level type per file, the throw helpers for guard clauses, and switch expressions that name every member of a closed enum. `.editorconfig` and the architecture tests hold these, so the build reports a lapse.
- **Reducers:** each view (`Application\Views\*View.cs`) is a pure function of state and input returning effects; the `Tui` layer only renders and runs effects. A new key is the effect record, the reducer case and its test, a handler (`IEffectHandler<TEffect>`, one class per effect, with its outcome record and a `...HandlerShould` test) and its line in `Composition.Handlers`.
- **Values are types:** folders are `FolderPath`, ids are `SessionId`, `JobId` and `BranchName`, and so on. Each text value builds with `From`, parses outside text with `TryParse` and, where a person typed it, explains a problem with `Validate`. Vantage's own models never use null for meaning.
- **Words live in `Wording`:** all user-facing text is in `Application\Wording`, in British English, with a fact in its tests. User text never comes from an exception message.
- **Privacy:** diagnostics record numbers, durations, versions, enums and type names only, never a path, a name, program output or anything typed; `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` enforce it.
- **Programs Vantage starts:** each is found once on the PATH as an `.exe` and run by its full path; no batch file is ever given text from a person, a repository or a listing; user text never reaches PowerShell source unquoted.

## OpenSpec: how every change is made

Vantage uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven development. Every piece of work, including refactors, tooling and documentation, starts as an OpenSpec change and is archived in the pull request that completes it.

- **`openspec\specs`:** what Vantage does today, one capability per folder (`session-listing`, `session-launch`, `worktree-management`, `settings` and so on), as `SHALL` requirements with `WHEN`/`THEN` scenarios. Read it to learn current behaviour.
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
- **The loop:** `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and the hand-off check exits 0 if you touched attach, `vantage here` or the pseudo-console.
- **The version:** a pull request that changes the packaged tool raises `Version`; see [Versions](#versions).
- **The documents:** the README describes any behaviour a user will notice; any newly verified fact about Claude Code, Terminal.Gui, Windows Terminal or Windows is recorded in its `.claude\rules` file with the date and version; a project's README holds any fact worth knowing about it.
- **Manual checks:** a change to how Vantage drives `wt.exe`, tabs or attach has been through the [manual checks](#manual-checks-for-tab-and-attach-changes).

## Manual checks for tab and attach changes

CI drives Vantage inside a pseudo-console, so rendering, keys and the attach through `vantage here` are covered; anything that goes out through `wt.exe` is not, because a runner has no desktop. Before merging a pull request that changes how Vantage opens, titles or colours tabs, or attaches to a session, install its package (`tools\install-local.ps1`) and check in a real Windows Terminal:

1. Enter on a session opens a coloured tab titled with the session name, and Enter again switches to that tab while it is open.
2. `n`, choose a repository, name the session, leave `New worktree` chosen, F5: a new tab opens attached to the new session, its REPOSITORY cell reads the repository, and the Worktrees tab lists the new worktree.
3. Ctrl+Z in that tab closes it, and the TAB block clears within one refresh.
4. `d` on a session shown in its tab ends it and the tab closes by itself.
5. A background session that pauses for input turns its row amber and rings the bell in the Vantage tab while another tab is in front.
6. Vantage's own tab is titled `Vantage` and green, reads `Vantage (Keys)` while `?` is open and `Vantage` after Esc, and goes back to the profile's title and colour on `q`.
7. `vantage here` in a trusted repository prints "Ctrl+Z detaches; the session keeps running.", attaches in the same tab, and Ctrl+Z returns to the shell with the line gone.

After a Terminal.Gui update, check the dialog titles without a terminal: from `tools\HeadlessTerminal`, `dotnet run -- drive <vantage.exe> '8000:?,2000:\u001b,2000:q'`, then the `ESC ] 0 ;` titles in `drive-raw.txt` must read `Vantage`, `Vantage (Keys)`, `Vantage`. Terminal.Gui writes every window's title to the terminal unless told not to, so a new dialog that skips `DialogTabTitle.Apply` shows up here.

## Continuous integration

`.github\workflows\check.yml` runs on every pull request and push to `main` on a Windows runner, and its `check` job must pass, on a branch up to date with `main`, before a pull request can merge.

- **What `check` does:** checks the version bump, builds in Release, runs every test, checks whitespace, packs the tool as `<Version>-ci.<run number>` and uploads it as an artefact (see [Trying your build](#trying-your-build)), then installs that package and runs the smoke check against it.
- **Documentation-only pull requests:** one that changes only files under `openspec\` or `.md` files outside `.github\` passes after the version check without building. Run the tests yourself if you touched the README, because `DocumentedMinimumShould` reads it.
- **The hand-off job:** a second job, `handoff`, runs the hand-off check against a real throwaway session when the repository's Claude Code token is available; it never blocks a merge.
- **CodeQL:** `.github\workflows\codeql.yml` scans the C# and the workflows; it is not a required check.

## Versions

Every pull request that changes the packaged tool (anything under `src\`, or `Directory.Build.props`, `global.json` or `nuget.config`) raises `Version` in `src\Source191.Vantage\Source191.Vantage.csproj`, because the tool cache keeps a version for ever and each merge of one is a release. Versions follow [semantic versioning](https://semver.org/):

- **Patch:** fixes, refactors with no behaviour change, and changes to text.
- **Minor:** a new key or a new behaviour that keeps existing keys working.
- **Major:** a change that breaks how existing keys, state files or the `vantage here` command line behave. While the tool is `0.y.z`, a minor bump may break.

```powershell
.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD   # the check, locally
.github\scripts\Step-Version.ps1                                 # raise Version one step; -WhatIf only prints it
```

`Step-Version.ps1` raises the prerelease number when there is one, otherwise the patch. The check compares semantic versions, so 0.10.0 is above 0.9.0. Because branches must be up to date before merging, two pull requests that bump to the same version cannot both merge: the second fails until it bumps again.

Releasing and publishing are the owner's; see [MAINTAINING.md](MAINTAINING.md).
