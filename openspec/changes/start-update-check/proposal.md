# Proposal

## Why

Nothing starts Vantage's update check. `UpdateCheck.RunAsync` is the only code that reads `update-check.json` and asks nuget.org, and nothing calls it: `Program.cs` builds the check and reads its `Status`, but never runs it. So no release since the check was added has ever shown `(x.y.z available, U to update)`, and `U` always says Vantage is up to date. Section 13 of the 0.15.0 test plan found it: with a recorded 0.99.0 release, the header of an installed 0.16.0 showed only `0.16.0`.

## What Changes

- The console starts the update check as it opens, without waiting for it, so the first screen is never delayed. A recorded newer release appears in the header on its next redraw, and the nuget.org answer appears when it arrives.
- An unexpected failure inside the check (a state file that cannot be written, an adapter that throws) is logged and goes no further: the console keeps running, shows no notice and says nothing about it, as the spec already says for an unreachable nuget.org.
- The patch version goes to 0.16.1.

Out of scope: any change to the rules of the check (once a day, prerelease builds never check, the setting and `VANTAGE_NO_UPDATE_CHECK`), the update key and its tab, a running Vantage checking again during the day, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `tool-updates`: the once-a-day requirement gains scenarios saying that a recorded newer release is shown once the console opens, and that a failure inside the check never reaches the screen. The rest of the requirement is unchanged.

## Impact

- `src\Source191.Vantage.Core\Application\ScreenLoop.cs` starts the check; `UpdateCheck`'s summary stops claiming that `Program.cs` runs it.
- `src\Source191.Vantage\Program.cs` passes the `UpdateCheck` to the `ScreenLoop`.
- `test\Source191.Vantage.Core.Tests\Application\ScreenLoopShould.cs` gets the regression facts.
- `src\Source191.Vantage\Source191.Vantage.csproj`: `Version` 0.16.0 to 0.16.1.
- The hand-off harness is unaffected: it sets `VANTAGE_NO_UPDATE_CHECK`, so the check still makes no request there.
