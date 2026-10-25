# Design

## Context

See proposal.md, Why. After `header-and-help`, `Program.cs` reads the running version once (`ToolVersion.FromInformational`) and the header is built by `Wording.HeaderValues`; after `settings-and-worktree-root`, settings come from `CurrentSettings` and the header context is a function the screen calls on each redraw, so a value that arrives after start-up shows on the next redraw. `TabScripts` builds every tab Vantage opens and is the only holder of the vantage executable path (`Environment.ProcessPath` in `Program.cs`, which for an installed tool is the shim in `~\.dotnet\tools`). nuget.org's flat-container index, `https://api.nuget.org/v3-flatcontainer/source191.vantage/index.json` (the id lowercased), answers `{"versions": [...]}` with every listed version and needs no key. A running `vantage.exe` locks its files, so `dotnet tool update` fails while any Vantage runs.

## Goals / Non-Goals

**Goals:** the rule for when to check is a pure, tested Domain function; the network is one port with one adapter; the update never runs inside the process it replaces.

**Non-Goals:** checking more than once a day or while Vantage runs; showing release notes; updating any other Vantage that is open in another tab (the update fails and the tab says so).

## Decisions

- **`ReleaseVersion` in Domain** parses `major.minor.patch[-prerelease]` (build metadata after `+` dropped) and compares by semantic version precedence; `IsPrerelease` is true for any `-` suffix. Alternative: `NuGet.Versioning`. Rejected: a package dependency in Core for one comparison, and Core's only package is Logging.Abstractions.
- **`UpdateCheckRule` in Domain** decides from the running version, the setting, the recorded check and the clock: never for a prerelease or when off; otherwise only when the last check is absent or 24 hours old. `Newest(IEnumerable<string>)` keeps the highest non-prerelease version, skipping text that does not parse. Tested with `FakeTimeProvider` values, mutation tested.
- **`IPackageVersions.ListAsync(string packageId, CancellationToken)`** returns `IReadOnlyList<string>?`, null on any failure, as `IGit`'s nullable queries do. `NuGetPackageVersions` uses one `HttpClient` with a 3 s timeout and a `User-Agent` of `Vantage/<version>`; tested in Infrastructure.Tests with a stub `HttpMessageHandler` (an answer, a 404, a timeout, malformed JSON). Alternative: running `dotnet package search`. Rejected: it starts the SDK, takes seconds and its output is not a stable surface.
- **The record** `UpdateCheckRecord(DateTimeOffset CheckedAt, string? Newest)` lives behind a state port and in `update-check.json` in the state folder, one file like the other records. A failed request records nothing, so the next start tries again.
- **`UpdateCheck` in Application** holds the newest known release (from the record at start, replaced when a check lands) and runs the check on a pool thread started by `Program.cs` after the first screen is up; the header function and the `U` reducer case read it. Nothing awaits it on the UI thread (the Terminal.Gui deadlock rule).
- **`U` is a dialog effect.** The reducer raises `UpdateVantage(current, newest)` when an update is known, else `Footer.UpToDate` or `Footer.UpdateCheckOff`. Its flow in `DialogEffects` asks "Quit Vantage and update to 0.10.0? Background sessions keep running."; on yes it dispatches `StartUpdate` to `StartUpdateHandler`, which opens the tab through `IWindowsTerminal` with `TabScripts.Update(processId)` and returns `UpdateStarted(newest)`, and the screen then leaves with `Quit`. A capital letter, as for the other keys that act beyond the selected row.
- **`TabScripts.Update(int processId)`** script: `Wait-Process -Id <pid> -ErrorAction SilentlyContinue`, `dotnet tool update -g Source191.Vantage`, then the vantage executable only when `$LASTEXITCODE` is 0; otherwise `Write-Host` a one-line hint and leave the tab open. Every value goes through `PowerShellScript.Quote`. Alternative: a detached hidden process. Rejected: a failure would be invisible, and a tab is how Vantage already shows work.
- **Setting.** `Settings.CheckForUpdates` (bool, true when the file has no property) joins the file record, the settings state and the dialog as a check box.

## Risks / Trade-offs

- [The shim or the store copy is still locked when `Wait-Process` returns] → the script waits for the process id Vantage passes, which is the process that holds both files; a task verifies the update at the terminal and, if it fails, adds a retry of `dotnet tool update` after a short wait.
- [Another Vantage tab is open] → `dotnet tool update` fails with a locked-file error, the tab stays open with dotnet's message, and the hint says to quit every Vantage and press `U` again.
- [A request to nuget.org from a tool people run at work] → once a day, anonymous, no identifiers beyond the user agent; documented in the README with the setting that turns it off.
- [The first release with this change has nothing newer to find] → the scenario tests use recorded answers; the update itself is verified in `public-release`'s final test from a local folder source holding a higher version, and the real request once after the first publish.

## Migration Plan

Nothing to migrate: a missing `checkForUpdates` reads as true and a missing `update-check.json` means "never checked". Rolling back to an earlier version ignores both.
