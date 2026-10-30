# Contributing to Vantage

Thank you for wanting to make Vantage better. This guide holds everything needed to develop it: how to propose a change, how to build and test it, how the code is organised, how every change goes through OpenSpec, and how a change becomes a release. The [README](README.md) is the user guide; [CLAUDE.md](CLAUDE.md) is the full rulebook and records the verified facts about Claude Code, Terminal.Gui, Windows Terminal and Windows that Vantage depends on.

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
- [Releasing and maintaining](#releasing-and-maintaining)

## How to contribute

- **Small fixes:** a typo, a broken link or an obvious one-line bug can go straight to a pull request.
- **Anything larger:** open an issue first and describe the problem or the idea, so we can agree the change before you spend time on an OpenSpec proposal and code. A bug report is best made from Vantage itself with `!` (see the README's [Reporting a problem](README.md#reporting-a-problem)), which fills in the diagnostics.
- **Security problems:** never in a public issue; follow [SECURITY.md](SECURITY.md).
- **Review:** every pull request needs the owner's approval before it can merge; `.github\CODEOWNERS` and the "Owner approves main" ruleset enforce it.
- **Licence:** Vantage is under the [Apache License 2.0](LICENSE). By opening a pull request you agree your contribution is under the same licence (its section 5); there is no contributor licence agreement to sign.
- **Fixtures:** tests use made-up `Northwind.*` repositories and `C:\Users\me` paths, never real repositories, people or your own machine's paths.

## Setting up

Vantage builds and runs on Windows 11 only. Install what the README's [Prerequisites](README.md#prerequisites) list (Windows Terminal, PowerShell 7, the .NET 10 SDK, git and Claude Code), then:

```powershell
npm install -g @fission-ai/openspec   # the OpenSpec CLI; `openspec install` gives a standalone build without Node.js
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
dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build  # the fast in-memory suite, about two seconds
dotnet test -c Debug --no-build                                    # every suite, including real git and processes
dotnet format --verify-no-changes
dotnet run --project tools\HeadlessTerminal -- check               # the hand-off check, from tools\HeadlessTerminal
```

- **Warnings are errors:** style and analyser rules fail the build, so a clean build is most of the style review.
- **Debug for the loop:** a Release build of `vantage.exe` cannot be rebuilt while it is running; stop the running instance before `dotnet pack` (`tools\install-local.ps1` does both).
- **The hand-off check:** run it after any change to attach, dialogs or Terminal.Gui. It starts and removes its own throwaway session; exit 1 means a blocked UI thread, 3 that Vantage quit but Claude's own screen never appeared. `tools\HeadlessTerminal` also has `drive` (scripted keystrokes into a pseudo-console, captured to `drive-raw.txt`), `grid` (replay a capture as a screen, which is how the screens in the README are drawn) and `count`; keys sent in the first 8 seconds are lost, so scripts start at 8000 ms. See its [README](tools/HeadlessTerminal/README.md).
- **The smoke check:** `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` opens and cancels every dialog and view of a packed tool, then quits; exit 0 passes.
- **Claude Code contract tests:** they start real sessions, so an ordinary `dotnet test` skips them. Run them from a trusted repository with `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"`. A difference in the CLI's help leaves `<name>.received.txt` beside the snapshot; see [the nightly contract check](#the-nightly-claude-code-contract-check).
- **Mutation testing:** from `test\Source191.Vantage.Core.Tests`, run `dotnet stryker --test-runner mtp --project Source191.Vantage.Core.csproj --mutate "**/Domain/*.cs"`. Use `--test-runner mtp` (xunit.v3 scores 0 % under the VSTest bridge) and globs like that one, never solution-relative paths.

## Trying your build

Pack and install the tool from your working copy:

```powershell
.\tools\install-local.ps1
```

The script stops a running `vantage.exe`, packs the tool as `<Version>-local.<timestamp>` and installs or updates the global tool. The suffix matters: the tool cache keeps the first package it saw for a version, so two plain builds of 0.1.18 would run the first one. `-Clean` uninstalls first.

To test the package a GitHub Actions run built instead, download the run's `Source191.Vantage-<version>-ci.<run number>` artefact and install from the folder:

```powershell
gh run download <run id> --name Source191.Vantage-0.1.18-ci.42 --dir .\downloaded
.\tools\install-local.ps1 -FromArtifact .\downloaded
```

Or by hand: `dotnet pack src\Source191.Vantage -c Release` then `dotnet tool install -g Source191.Vantage --add-source .\artifacts\packages`. Go back to the published tool with `dotnet tool update -g Source191.Vantage`.

## How the code is organised

| Project | What it holds |
| --- | --- |
| [src\Source191.Vantage.Core](src/Source191.Vantage.Core/README.md) | `Domain` (rules and value objects, depends on nothing), `Ports` (the boundary interfaces and their records), `Application` (use cases in `Actions` and `Launch`, read models, the `Views` reducers, the detail pane's reading, ordering and layout in `Detail`, `ScreenLoop`, `CommandLine`, `Wording`) |
| [src\Source191.Vantage.Infrastructure](src/Source191.Vantage.Infrastructure/README.md) | The adapters: git, the Claude CLI, Windows Terminal, processes, files, state, pull requests, diagnostics |
| [src\Source191.Vantage.Tui](src/Source191.Vantage.Tui/README.md) | Terminal.Gui rendering and dialogs, nothing else |
| [src\Source191.Vantage](src/Source191.Vantage/README.md) | `Program.cs`, the only composition root, packed as the tool with the other three inside |
| [test\Source191.Vantage.Core.Tests](test/Source191.Vantage.Core.Tests/README.md) | The fast in-memory suite, including the architecture tests |
| [test\Source191.Vantage.Infrastructure.Tests](test/Source191.Vantage.Infrastructure.Tests/README.md) | The adapters against real git, processes and files, and the Claude Code contract tests |
| [test\Source191.Vantage.TestDoubles](test/Source191.Vantage.TestDoubles/README.md) | The shared hand-written fakes, no test framework |
| [tools\HeadlessTerminal](tools/HeadlessTerminal/README.md) and [its tests](tools/HeadlessTerminal.Tests/README.md) | Drives the built exe in a pseudo-console: the hand-off and smoke checks |

Each project's README holds what is worth knowing about it; a `.csproj` carries no comments (the root `Directory.Build.props` keeps its own beside its settings). `openspec\` holds the specifications and the changes made against them, `.github\` the workflows and their scripts, `assets\` the icon and artwork and the scripts in `tools\` that draw them.

## The code rules in brief

CLAUDE.md's "Code rules" are the authority; this is their shape.

- **Layers:** Domain, Ports, Application, Adapters, Tui, one namespace each, with dependencies pointing one way; `Architecture\LayeringShould` fails the build's tests otherwise. Nothing outside the adapters touches files or processes, and nothing outside `Tui` touches Terminal.Gui.
- **Test-driven:** red, green, refactor, a commit per step. No untested code except `Program.cs` and wiring.
- **Sociable tests:** real collaborators inside the code; hand-written fakes from `TestDoubles` only at the boundaries (process runner, process registry, terminal, console, transcripts, folders, state, git roots), never a mocking library.
- **C# style:** concrete types rather than `var`, primary constructors, records, collection expressions, file-scoped namespaces, target-typed `new`, `is null` and `is not` patterns, `System.Threading.Lock`, `[LibraryImport]` for platform calls, `TimeProvider`, braces always, expression bodies for one-line methods, the throw helpers for guard clauses (a primary constructor guards a parameter with `?? throw` in the field initialiser that reads it), and switch expressions that name every member of a closed enum. `.editorconfig` holds the language idioms at `warning`, so the build reports a lapse.
- **Reducers:** each view (`Application\Views\*View.cs`) is a pure function of state and input returning effects; the `Tui` layer only renders and runs effects. A new key is four pieces and no screen change: the effect record, the reducer case and its test, a handler (`IEffectHandler<TEffect>`, one class per effect, with its outcome record and a `...HandlerShould` test) and its line in `Composition.Handlers`.
- **Values are types:** folders are `FolderPath`, ids are `SessionId`, `JobId` and `BranchName`, and so on. Each text value builds with `From`, parses outside text with `TryParse` and, where a person typed it, explains a problem with `Validate`.
- **Words live in `Wording`:** all user-facing text is in `Application\Wording`, in British English, with a fact in its tests. User text never comes from an exception message.
- **Privacy:** diagnostics record numbers, durations, versions, enums and type names only, never a path, a name, program output or anything typed; `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` enforce it.
- **Programs Vantage starts:** each is found once in the PATH folders as an `.exe` and run by its full path; no batch file is ever given text from a person, a repository or a listing; user text never reaches PowerShell source unquoted.

## OpenSpec: how every change is made

Vantage uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven development. Every piece of work, including refactors, tooling and documentation, starts as an OpenSpec change and is archived in the pull request that completes it.

### What lives where

- **`openspec\specs`:** what Vantage does today, one capability per folder (`session-listing`, `session-launch`, `session-attach`, `worktree-management`, `settings` and so on). Each holds requirements written as `SHALL` statements with scenarios (`WHEN` and `THEN`).
- **`openspec\changes\<name>`:** a change in flight: `proposal.md`, `design.md`, `tasks.md` and, under `specs\`, delta specs that add, modify or remove requirements.
- **`openspec\changes\archive\<date>-<name>`:** every finished change, dated.
- **`openspec\config.yaml`:** the project context every change inherits (platform, the Claude Code CLI surface, worktree conventions, layering, coding style) and the rules for each artifact. Update it when a rule changes rather than repeating the rule in each proposal.

### The stages

Each stage has a Claude Code command (from `.claude\commands\opsx`, backed by the skills in `.claude\skills`) and a CLI equivalent. Run `openspec update` after upgrading the CLI to refresh those files.

1. **Explore (optional):** `/opsx:explore` is a thinking partner for an idea that is not yet clear: it reads the specs and the code and asks questions, but writes nothing. Leave it when you can say in two sentences what should change and why.
2. **Propose:** `/opsx:propose <name>`, or `openspec new change <name>` and then `openspec instructions <artifact> --change <name>` for each artifact. It writes, in order:
   - **`proposal.md`:** why the change is needed, what changes, what is out of scope, which capabilities it creates or modifies, and its impact.
   - **Delta specs:** under `specs\<capability>\spec.md`, the `ADDED`, `MODIFIED` or `REMOVED` requirements with their scenarios. Read the current spec first (`openspec show <capability> --type spec`) so a delta changes the existing requirement rather than duplicating it.
   - **`design.md`:** how: the decisions, the alternatives rejected and why, and the risks.
   - **`tasks.md`:** numbered checkboxes, each one red-green-refactor cycle or smaller with the test named before the code, ordered domain first, then adapters, then screens, each saying how it is verified.

   The proposal is done when `openspec validate <name>` passes and the artifacts agree with each other. Commit it before writing code, so the review can start with the plan.
3. **Apply:** `/opsx:apply <name>` works through the tasks test-first: a failing test, the least code that passes, a refactor, a commit per step, and the task's box ticked. `openspec status --change <name>` shows progress. When the code shows the plan was wrong, stop and change the plan rather than quietly building something else.
4. **Update:** `/opsx:update <name>` revises the proposal, design, specs and tasks together when a decision changes in flight, so they stay consistent with one another and with the code.
5. **Record:** a task that needs a person (a check in a real Windows Terminal, the owner trying a local build) is ticked only once done, with a note under it saying what was seen, when and on which build, for example "Recorded 30 October 2026 by the product owner on 0.18.0-preview.4-local.20261030073119: all good on the laptop screen and the monitor". Captures and timings from the headless terminal are recorded the same way.
6. **Sync (rarely):** `/opsx:sync <name>` merges a change's delta specs into `openspec\specs` without archiving, for a long-running change whose finished part others need to build on.
7. **Archive:** `/opsx:archive <name>`, or `openspec archive <name>`, once every task is ticked. It merges the delta specs into `openspec\specs` and moves the change to `openspec\changes\archive\<date>-<name>`. Archive in the same pull request that completes the change, so `openspec\specs` on `main` always describes the code on `main`.

### Changes with no behaviour change

A refactor, a tooling change or a documentation change still gets a proposal and tasks, but has nothing to say to the specs. It declares `skip_specs: true` in its `.openspec.yaml`, writes no delta specs, and `openspec validate` accepts it. The change that added this guide is one.

### Why the specs and the archive are kept for ever

They answer two different questions, and both answers would otherwise be lost.

- **The specs answer "what does Vantage do?"** `openspec\specs` is the one current account of the behaviour on `main`. Because every change's delta specs merge into it as the change is archived, it never drifts from the code the way a hand-kept document does, and it is small enough to read before changing anything. Proposing against it is what stops a new change from quietly contradicting an old promise: the delta has to say which requirement it modifies. It also tells a reviewer, a user or a Claude Code session what behaviour is deliberate rather than accidental, which the code alone cannot.
- **The archive answers "why is it like this?"** Each archived change keeps the problem it solved, the alternatives it rejected and why, the risks it accepted, and what was observed when it was checked by hand. Git keeps the diff, but not the reasoning or the rejected options, and a commit message cannot hold a design. When a choice looks odd, the archive usually explains it, so the next person does not undo a decision that was made on purpose or repeat an experiment that already failed. Dated folders make the history easy to read in order, and keeping it costs only a few kilobytes of text.

Use them that way round. Read `openspec\specs` to learn current behaviour. Search `openspec\changes\archive` only when asking why something was decided, and never read an archived design as a description of the current code: later changes may have replaced it, and only the specs and the code say what is true now.

## Opening a pull request

Before asking for review, check that:

- **The issue:** for anything beyond a small fix, the pull request links the issue where the change was agreed.
- **The change:** its OpenSpec change has every task ticked, manual checks recorded, and is archived in this pull request (`openspec validate` passes).
- **The loop:** `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and the hand-off check exits 0 if you touched attach, dialogs or Terminal.Gui.
- **The version:** a pull request that changes the packaged tool (`src\`, `Directory.Build.props`, `global.json` or `nuget.config`) raises `Version`; see [Versions](#versions).
- **The documents:** the README describes any behaviour a user will notice, CLAUDE.md records any newly verified fact about Claude Code, Terminal.Gui, Windows Terminal or Windows (with the date and version), and a project's README any fact worth knowing about it.
- **Manual checks:** a change to how Vantage drives `wt.exe`, tabs or attach has been through the [manual checks](#manual-checks-for-tab-and-attach-changes) in a real Windows Terminal.

## Manual checks for tab and attach changes

The runner drives Vantage inside a pseudo-console, which is the same plumbing Windows Terminal uses for a tab, so rendering, keys and the attach hand-off are covered. Anything that goes out through `wt.exe` is not: a runner has no desktop for a tab to appear on. Before merging a pull request that changes how Vantage drives `wt.exe`, opens, titles or colours tabs, or attaches to a session, install its package (`tools\install-local.ps1`) and run these in a real Windows Terminal:

1. `o` on a session opens a coloured tab titled with the session name, and `o` again switches to that tab while it is open.
2. `n`, choose a repository, name the session and leave `New worktree` chosen, F6: a new tab opens attached to the new session, and its WHERE cell reads the repository › the worktree's slug.
3. `d` on that session ends claude and the tab closes by itself.
4. A background session that pauses for input turns its row amber and rings the bell in the Vantage tab while another tab is in front.
5. Ctrl+Z from an attached session returns to the table with the keys still working.
6. The tab Vantage runs in is titled `Vantage` and green, reads `Vantage (Keys)` while `?` is open and `Vantage` again after Esc, keeps that title after attaching and pressing Ctrl+Z, and goes back to the profile's title and colour on `q`.

After a Terminal.Gui update, check the dialog title from `tools\HeadlessTerminal` without a terminal: `dotnet run -- drive <vantage.exe> '8000:?,2000:\u001b,2000:q'`, then the `ESC ] 0 ;` titles in `drive-raw.txt` must read `Vantage`, `Vantage (Keys)`, `Vantage`. Terminal.Gui writes every Runnable's title to the terminal unless it is told not to, so a new dialog that skips `DialogTabTitle.Apply` shows up here as its own title.

## Continuous integration

`.github\workflows\check.yml` runs on every pull request and every push to `main`, on a `windows-latest` runner because the code uses P/Invoke, ConPTY and `cmd.exe` in its integration tests. Branch protection on `main` requires its `check` job on an up-to-date branch, so a red pull request cannot merge, and nobody can push to `main` directly.

- **What `check` does:** builds in Release (warnings are errors) with the runner image's .NET SDK, which `global.json` chooses, so no SDK is downloaded; runs the whole test suite against that build; checks whitespace with `dotnet format whitespace --verify-no-changes` (the build already fails on every style and analyser warning, so the full `dotnet format` of the local loop would only repeat it); packs the tool as `<Version>-ci.<run number>`, a version the tool cache has never seen; uploads the `.nupkg` as the artefact `Source191.Vantage-<that version>`, so a working package is downloadable from any commit (see [Trying your build](#trying-your-build)); and runs the smoke check against the installed package, the one test of the bits users install.
- **Documentation-only pull requests:** one that changes only files under `openspec\`, or `.md` files outside `.github\`, as `.github\scripts\Test-DocumentationOnly.ps1` decides, passes `check` after the version check without building. A push to `main` always runs every step.
- **Cancelled runs:** a new commit on a pull request cancels the run for the one before it; runs for pushes to `main` are never cancelled.
- **The hand-off job:** `check.yml` has a second job, `handoff`, that runs the hand-off check against a real throwaway session. It needs the `CLAUDE_CODE_OAUTH_TOKEN` secret and never blocks a merge; see [The hand-off job](#the-hand-off-job).
- **CodeQL:** `.github\workflows\codeql.yml` runs CodeQL over the C# (without a build) and over the workflows on every pull request, every push to `main` and once a week. It is not a required check and skips documentation-only changes.

## Versions

Every pull request that changes the packaged tool raises `Version` in `src\Source191.Vantage\Source191.Vantage.csproj`, because the tool cache keeps a version for ever; each merge of one is therefore a release (see [Releases](#releases)). Versions follow [semantic versioning](https://semver.org/):

- **Patch:** fixes, refactors with no behaviour change, and changes to text.
- **Minor:** a new key or a new behaviour that keeps existing keys working.
- **Major:** a change that breaks how existing keys, state files or the `vantage here` command line behave. While the tool is `0.y.z`, a minor bump may break.

The first step of the `check` job, on pull requests only, runs `.github\scripts\Assert-VersionBumped.ps1` against the two parents of the pull request's merge commit. It fails when the pull request changes anything under `src\`, or `Directory.Build.props`, `global.json` or `nuget.config`, without raising `Version` above `main`'s (compared as semantic versions, so 0.10.0 is above 0.9.0). Pull requests touching only documentation, CI, `openspec\`, `test\` or `tools\` need no bump.

```powershell
.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD   # the check, locally
.github\scripts\Step-Version.ps1                                 # raise Version one step; -WhatIf only prints it
```

`Step-Version.ps1` raises the prerelease number when there is one, otherwise the patch. "Require branches to be up to date before merging" is on for `main`: without it, two open pull requests that both bump to the same version would merge without a conflict and the second would never be released. With it, updating the branch reruns the check against the current `main`, and the second fails until it bumps again.

## Releasing and maintaining

This section is for the owner; contributors can stop here.

### Releases

`.github\workflows\release.yml` runs on every push to `main` and is a no-op unless that push changed `Version`. When it did, it builds, tests and packs again, attaches a signed build provenance attestation to the package (`gh attestation verify <nupkg> --repo Source191/vantage` checks which commit and workflow built it), and publishes a GitHub Release tagged `v<version>` with the `.nupkg` attached. A version with a prerelease label (`0.16.2-preview.1`) is published as a GitHub prerelease, so it never becomes the Latest release. A release is not published to nuget.org: that is a separate, manual step, so a package is tried by hand before it goes where a version can never be replaced. The "Release tags" ruleset stops anyone but the owner moving or deleting a `v*` tag.

### Publishing to nuget.org

`.github\workflows\publish.yml` is run by hand (Actions, "publish", with the release tag, e.g. `v0.5.0`). It downloads that GitHub Release's `.nupkg` and pushes it to nuget.org without building anything, so what is published is the package that was built, tested and released from `main`.

- **No stored API key:** the `NuGet/login` action swaps the run's GitHub identity for a one-hour key through a Trusted Publishing policy on nuget.org (owner `Source191`, repository `vantage`, workflow `publish.yml`, environment `nuget`); the nuget.org profile name is the `NUGET_USER` repository secret.
- **The `nuget` environment:** accepts only runs started from `main` and waits for the owner to approve each one (Actions, the waiting run, Review deployments), so a copy of the workflow on a branch, or a run started by an app, never gets a key.
- **Mistakes:** nuget.org versions cannot be replaced, only unlisted, so a bad publish is unlisted and fixed by a higher patch version.
- **Prereleases:** published the same way; nuget.org leaves them out of search unless prereleases are included, and `dotnet tool install` takes one only with `--prerelease` or `--version`, which is how a version reaches nuget.org before it is offered to everyone.

### Reviews and merging

The "Owner approves main" ruleset requires a code owner's approval, and `.github\CODEOWNERS` names the owner for every path, so a pull request opened by anyone else (an app or Dependabot included) waits for that review. The owner merges their own pull requests through the ruleset's bypass (`gh pr merge --admin`), so auto-merge does not fire for them.

### Dependabot and the version bump token

Dependabot updates the NuGet packages and the actions' pins weekly, and security updates are on, so a dependency with a known vulnerability gets a pull request at once (the build's NuGet audit fails on it anyway). A Dependabot pull request that touches the tool is bumped for you: a step before the version check raises `Version` on the pull request's branch, commits "Raise Version to <version> for the dependency update" and pushes it with `DEPENDABOT_BUMP_TOKEN`, a fine-grained personal access token with Contents read and write on this repository, stored under Settings, Secrets and variables, Dependabot (a Dependabot run can read only those). The run for the pushed commit is the one branch protection reads; you still review and merge. When the token expires the step fails naming it: create a new one and replace the secret.

### Supply chain

Every action the workflows use is pinned to the commit of its release, with the version in a comment, so a moved tag cannot change what runs; Dependabot updates the pins, and the repository setting that requires such pins is turned on once every workflow on `main` carries them. The `CLAUDE_CODE_OAUTH_TOKEN` secret reaches the latest Claude Code CLI that the `handoff` and `claude-contract` jobs install from `claude.ai`, so it belongs to an account that has nothing else and is rotated like a password.

### The hand-off job

`handoff` installs Claude Code on the runner and runs the hand-off check from `tools\HeadlessTerminal` against a real throwaway session. It needs the `CLAUDE_CODE_OAUTH_TOKEN` repository secret (from `claude setup-token`); without one it skips with a notice, as it does for a documentation-only pull request. It builds only the tool (`src\Source191.Vantage`), because `check` builds and runs the tests, and installs Claude Code in the background while it builds. It never blocks a merge, because its keystroke timings were tuned on a laptop and a slow runner can fail it for reasons that are not bugs; read its `handoff-capture` artefact when it does.

The runner has no desktop, so the job seeds workspace trust for the checkout in its own `~\.claude.json`, which, with the nightly contract check, is the only place that record is ever written by a script. Vantage strips `CLAUDE_CODE_*` from the environment of the `claude` processes it starts (`InheritedEnvironment`), so the token reaches the throwaway session the harness starts but not Vantage's own `claude agents` and `claude attach`; the live runs show the attach reaches Claude's screen all the same. The check exits 3 when it does not: Claude's screen must be in the capture, not only Vantage's.

### The nightly Claude Code contract check

Claude Code's native installer updates the CLI in the background, so users soon run a newer CLI than Vantage was verified against. `.github\workflows\claude-contract.yml` runs every night at 03:17 UTC, and by hand from Actions, "claude-contract": it installs the latest CLI, runs the `ClaudeContract` tests (Vantage's own Claude adapters against that CLI: the version, a background session started with no prompt, listed with every field Vantage reads in a state it knows, then stopped and removed; and the commands and options in `claude --help` and `claude agents --help` against the snapshots in `test\Source191.Vantage.Infrastructure.Tests\Contract\Snapshots`), then the hand-off check. It needs the same `CLAUDE_CODE_OAUTH_TOKEN` secret and seeds trust the same way; without the secret it skips with a warning. It never runs on pull requests, so a CLI release never blocks a merge.

- **The claude-cli issue:** when the latest CLI fails to install or a contract test fails, the workflow comments on the open issue labelled `claude-cli`, or opens one when none is open, with the CLI version, the run link and the failing tests' messages. It assigns a new issue to the GitHub login in the `CLAUDE_CLI_WATCHER` repository variable (Settings, Secrets and variables, Actions, Variables); an assignee is subscribed to the issue, so GitHub notifies them of every comment. Close the issue by hand once the fix is merged. The hand-off check marks the run but never raises the issue.
- **Accepting a snapshot:** a difference leaves `<name>.received.txt` in the run's `contract-results` artefact (and in the test output folder locally). Copy it over the snapshot in a change that re-checks the CLI facts in CLAUDE.md, so a new flag is either used, deliberately ignored or recorded.
- **Never** add `--bg --resume --help` to the contract tests: it starts a session.
