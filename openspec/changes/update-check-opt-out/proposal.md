# Proposal

## Why

`update-notice` checks nuget.org at most once a day and skips prerelease versions. The hand-off check (`tools\HeadlessTerminal`, `check`) runs the Release build, which is not a prerelease, so every run, on a developer machine and in the `handoff` job of `check.yml`, makes a real request to `api.nuget.org` and may write `update-check.json`. A test harness should never contact the registry or touch the user's state.

## What Changes

- A `VANTAGE_NO_UPDATE_CHECK` environment variable: set to any non-empty value, Vantage makes no update request, writes no `update-check.json` and shows no notice, exactly as when `checkForUpdates` is false.
- `Program.cs` combines it with the `checkForUpdates` setting in the `enabled` function `UpdateCheck` already takes; a small `UpdateCheckOptOut` value in `Domain` decides what counts as set, so the rule is tested.
- `DriverOptions.ChildEnvironment` in `tools\HeadlessTerminal` sets it on every Vantage process the harness starts, which covers `check`, `drive` and the CI `handoff` job with no workflow change.
- README documents the variable beside `checkForUpdates`.
- Patch release: `0.11.1`.

Out of scope: a settings-dialog control for it (it is for harnesses and scripts); a command-line flag.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `tool-updates`: adds the requirement that the environment variable turns the check off.

## Impact

- `Core`: new `Domain\UpdateCheckOptOut.cs`; `UpdateCheck` is unchanged.
- `Source191.Vantage\Program.cs`: one line of wiring.
- `tools\HeadlessTerminal`: `DriverOptions.ChildEnvironment`, with a test in `HeadlessTerminal.Tests`.
- `README.md` and `tools\HeadlessTerminal\README.md`.
