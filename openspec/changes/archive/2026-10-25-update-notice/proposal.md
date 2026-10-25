# Proposal

## Why

Once Vantage is on nuget.org, people who install it have no way to learn that a newer version exists: a dotnet global tool never updates itself, and nothing on screen says one is out. Updating by hand is also awkward, because a running `vantage.exe` locks its own files, so `dotnet tool update` fails until every Vantage has quit.

## What Changes

- **Vantage checks nuget.org for a newer release** at most once a day, off the UI thread, with a short timeout. A failed or slow check shows nothing and delays nothing. Local and CI builds (a prerelease version) never check.
- **The header says when an update is available:** the `Vantage:` line from `header-and-help` reads `0.9.0 (0.10.0 available, U to update)`.
- **`U` in both views updates Vantage.** After a confirmation, Vantage opens a new Windows Terminal tab that waits for this Vantage to exit, runs `dotnet tool update -g Source191.Vantage`, then starts Vantage again; this Vantage quits. Background sessions run under Claude's supervisor and keep running. When no update is known, the footer says so and nothing happens.
- **A setting turns the check off:** `checkForUpdates` in `config.json` (default true, so a file written by an earlier version keeps checking), editable in the settings dialog from `settings-and-worktree-root`.
- **README:** the `U` key, the header notice, the setting, and what the check sends (one anonymous request for the package's version list to `api.nuget.org`).
- **Version:** a minor bump.

Out of scope: updating silently without asking; updating while Vantage keeps running; prerelease channels; checking for Claude Code updates; winget or other installers; multiple Claude accounts, which is a later change.

Depends on `header-and-help` (the `Vantage:` header line and `ToolVersion`) and `settings-and-worktree-root` (the settings holder, the dialog and the header as a function read on each redraw), and on `public-release` having published the package so there is something to find.

## Capabilities

### New Capabilities

- `tool-updates`: learning that a newer Vantage release exists, saying so, and updating to it from Vantage.

### Modified Capabilities

None. The header line and the settings dialog gain content owned by `tool-updates`; no existing requirement changes.

## Impact

- **Core:** Domain `ReleaseVersion` (semantic version comparison, prerelease detection) and `UpdateCheckRule` (once a day, never for a prerelease, off when disabled); Ports `IPackageVersions` and an update-check record in `StatePorts`; Application `UpdateCheck` (holds the latest known version, refreshed in the background), `Settings.CheckForUpdates`, the `U` reducer cases, an `UpdateVantage` dialog effect, a `StartUpdate` effect with `StartUpdateHandler` and its outcome, `TabScripts.Update`, `Footer` cases and `Wording` entries.
- **Infrastructure:** `NuGetPackageVersions` (`HttpClient` against the flat-container index), the update-check record in `JsonFileStore`.
- **Tui:** the confirmation flow in `DialogEffects`, the `U` key hint, the setting in `SettingsControls`.
- **Program.cs:** starts the check after the first screen is up and wires the adapter.
- **Tests:** Domain facts, adapter tests with a stub `HttpMessageHandler`, `UpdateCheckShould` with `FakeTimeProvider`, reducer, handler, wording, dispatcher and dialog facts; `FakePackageVersions` in TestDoubles.
- **Docs:** README, CLAUDE.md (the update tab and the locked exe), `Version`.
