# Proposal

## Why

A review of `src\Source191.Vantage\Program.cs` on 1 November 2026 found it hard to follow: 218 lines, one 75-line `OpenConsoleAsync` building about 35 objects (five lines of 200 to 300 characters), and behaviour that the rule "no untested code except `Program.cs` and wiring" was never meant to excuse. `LoadAsync`, `ScanAsync` and `ReadConfigAsync` decide how a refresh combines the listing with the enrichment and how a scan reuses the listing; `RecordStart` parses Claude Code's version; two lambdas hold the update-check switch and the settings row's null handling; `LocateClaudeAsync` and `LocateGitAsync` choose the sentence and the exit code and answer `string?`. The console and `vantage here` also repeat the same locate-and-build preamble. The owner asked whether dependency injection would simplify it; the review recommended staying with hand-written construction and giving it structure instead (see design.md), and the owner agreed, with `RequiredPrograms` included.

## What Changes

- **`ConsoleLoads`:** a new `Application` class holds the console's three loads (a Sessions refresh, a Worktrees scan, a Config read) and the Config rows shown while the setup facts are read, so `ConsoleScreen` is handed its method groups instead of local functions in `Program.cs`. Tested in `ConsoleLoadsShould`.
- **`Started.From`:** the start event is built from the versions, the Claude version report and the terminal's size by a tested factory on `Application\Diagnostics\Started`; the composition only records it.
- **`UpdateCheckSwitch`:** the update check's on-or-off question (the `checkForUpdates` setting, read per call, and `VANTAGE_NO_UPDATE_CHECK`) becomes a tested `Application` class whose `IsOn` is handed to `UpdateCheck`.
- **`HeaderSettingsRow`:** the console's settings row above the tabs (Vantage's version, update status and own memory, zero when the process cannot be measured, then Claude Code's version) becomes a tested `Application` class whose `Rows` is handed to `ScreenContext`.
- **`RequiredPrograms`:** a closed two-case result in `Application`, `Found(Claude, Git)` with both full paths or `Missing(Sentence)` with `Wording`'s sentence, built by `RequiredPrograms.Check(ClaudeLocation, ProgramLocation)`. It replaces `LocateClaudeAsync` and `LocateGitAsync` and their `string?`, which the `null-at-the-boundary` change kept on purpose; Claude Code's sentence still wins when both are missing, and the exit code stays 1.
- **One preamble:** the console and `vantage here` share one step that checks the required programs, prints the sentence and exits 1 on `Missing`, and otherwise builds `ClaudeCli`, `GitCli` and `LaunchService` once.
- **Wiring split:** `Program.cs` keeps the process-wide start (state folder, log, crash hooks, the command line) and shrinks to about 40 lines; the rest moves to a new `Composition` folder in the tool project: a `SharedParts` record of what every command uses, a `LaunchParts` record for the preamble's three objects, and one static class per command that needs more than a line (`ConsoleComposition`, `HereComposition`, `TerminalSetupComposition`). They stay untested wiring.
- **Docs:** `CLAUDE.md`, `CONTRIBUTING.md`, `src\Source191.Vantage\README.md`, `openspec\config.yaml` and `LayeringShould`'s summary say the tool project (`Program.cs` and its `Composition` folder) is the only composition root.
- **Version:** the next preview above `main` (0.20.0-preview.8 if nothing lands first), because files under `src\` change; a refactor, so patch-level under semantic versioning.
- `skip_specs: true`: no key, screen, message, exit code, state file or command line behaves differently.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage\Program.cs` and new files under `src\Source191.Vantage\Composition\`; new `Application\ConsoleLoads.cs`, `Application\UpdateCheckSwitch.cs`, `Application\HeaderSettingsRow.cs` and `Application\RequiredPrograms.cs` in Core; `Application\Diagnostics\Started.cs` gains `From`. No port, adapter, Tui type or constructor signature changes.
- **Tests:** new `ConsoleLoadsShould`, `UpdateCheckSwitchShould`, `HeaderSettingsRowShould`, `RequiredProgramsShould` and `StartedShould` in `test\Source191.Vantage.Core.Tests`, over the existing fakes; no new fake is expected.
- **Docs:** as listed above; `src\Source191.Vantage\Source191.Vantage.csproj` for the version.
- **Other open changes:** `performance-diagnostics` wraps `LoadAsync` and `ScanAsync` in its timing and starts a writer in `Program.cs`, `show-release-notes` wires a dialog in `Program.cs`, and `standardise-maybe-null-when` touches `ClaudeVersion.TryParse`, which `RecordStart` calls. Whichever lands second rebases; after this change those edits land in `ConsoleLoads` (where they become testable), `ConsoleComposition` and `Started.From`.
- **Users:** none.

## Out of scope

- A dependency injection container (Microsoft.Extensions.DependencyInjection or another): design.md records why.
- Narrowing wide constructors (`ConsoleScreen`'s 14 parameters, `EffectPorts`' 12, `DialogEffects`' 10), or changing `ConsoleScreen` to take `ConsoleLoads` instead of its delegates.
- Changing `Composition.Handlers` or the handlers it registers.
- Tests for the tool project itself: its classes stay wiring, checked by the build, the hand-off check and CI's smoke check.
- Multiple Claude accounts, which is a later change.
