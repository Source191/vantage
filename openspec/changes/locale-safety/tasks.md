# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. The proposal merges on its own (PR #151); implement it in a later pull request, on a branch from main once #151 has merged. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, adding `test\Source191.Vantage.Infrastructure.Tests` where a task names an adapter test.

## 1. Tests run as the tool ships

- [x] 1.1 Move `InvariantGlobalization` out of `src\Source191.Vantage.Core`, `src\Source191.Vantage.Infrastructure` and `src\Source191.Vantage.Tui` and into `test\Source191.Vantage.Core.Tests`, `test\Source191.Vantage.Infrastructure.Tests` and `tools\HeadlessTerminal.Tests`, with a sentence in each of those six projects' `README.md` saying why (design decision 4). Verify `dotnet build -c Debug` is clean with the culture analysers now running on the libraries, and that `EditorLocatorShould.RankRiderBuildsByNumberNotByText` is the one failing test (the red step for group 2).

## 2. Rider by number

- [ ] 2.1 `NumberAwareOrderShould`: `2025.10` after `2025.9`, `253.100` after `253.99`, leading zeros equal by value (`007` and `7` tie on the run, the shorter string first), letters compared without case, digits beside letters (`a9` before `a10`, `a10` before `b1`), and the full Rider paths from the settings spec's two scenarios. Red, then `Domain\NumberAwareOrder` green. Verify the facts pass.
- [ ] 2.2 `EditorLocator.NewestRider` orders by `NumberAwareOrder` in place of the `NumericOrdering` comparer; `RankRiderBuildsByNumberNotByText` goes green and a new `RankToolboxBuildsByNumber` (253.99 and 253.100 in one channel) passes. Verify both facts pass and `EditorLocator.cs` no longer names `CultureInfo`.

## 3. Changed files by name

- [ ] 3.1 `GitCliIntegrationShould.ListAChangedFileByItsOwnName`: a real repository with the untracked `naïve.txt` lists `naïve.txt`. Red, then `-c core.quotePath=false` before `status` in `GitCli.ChangedFilesAsync` green; `GitCliShould`'s argument facts updated to match. Verify both test classes pass in `Source191.Vantage.Infrastructure.Tests`.

## 4. Local times in the detail pane

- [ ] 4.1 `WordingOfDetailShould`: the three scenarios of the detail-pane delta's "The pane's times are local" (a summer start in the UK, before the clocks went back, today in Nairobi) through `Wording.Detail(detail, now, zone)` with `GMT Standard Time` and `E. Africa Standard Time`; the existing facts pass UTC. Red, then the zone parameter and `TimeZoneInfo.ConvertTime` in `DayAndTime` green. Verify the facts pass.
- [ ] 4.2 `SessionDetails` and `WorktreeDetails` pass `clock.LocalTimeZone`; a fact over each with a `FakeTimeProvider` set to the UK zone reads the local Started or Created time. Red, then green. Verify the facts pass.

## 5. Local time on the Config tab

- [ ] 5.1 `WordingOfConfigShould`: the config-tab delta's two scenarios (a check this morning in summer, a check before the clocks went back) through `Wording.ConfigValue(row, now, zone)`. Red, then the zone parameter in `When` and `SetupValue` green. Verify the facts pass.
- [ ] 5.2 `ConfigScreenDefinition` takes the `TimeProvider` and passes `clock.LocalTimeZone` to `ConfigValue`; `ConsoleScreen` hands it `screen.clock`. Wiring only, no test of its own; verify by build and by the Config tab reading the local time in a `dotnet run --project tools\HeadlessTerminal -- smoke` run against an installed local build (`tools\install-local.ps1`).

## 6. Notes for translation

- [ ] 6.1 `CLAUDE.md` gains a "Locale and translation" section (design decision 5): Vantage is English only, and the shipped tool runs invariant; times are shown in the local zone and stored in UTC; translating means turning invariant mode off, which wakes every culture-aware call (the CA rules then guard the libraries, and `Wording`'s formats need an explicit culture), moving `Wording`, `WordingOfOutcomes` and the command line's descriptions to resources (System.CommandLine's own help stays English), and keeping state files, diagnostics, git and az arguments and `ITextValue` parsing invariant; plus the known gaps: text measured by characters not terminal cells (`WhereLabel`, `DetailLayout`, `ConfigScreenDefinition.Middle`), worktree slugs that keep only `a-z0-9`, az writing the ANSI code page when piped and ignoring `PYTHONIOENCODING`, Git for Windows shipping no translations (so `GitCli`'s English matches hold until it does), key letters tied to English words, and German and other languages running longer than the fixed column and dialog widths. Verify by reading it against design.md and checking no em dash was introduced.

## 7. Release and whole build

- [ ] 7.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview after main's version when implementation starts (0.19.0-preview.14 if nothing else has released by then). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against main.
- [ ] 7.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate locale-safety --strict` pass, and the packed tool's `vantage.runtimeconfig.json` (from `dotnet pack`) still sets `System.Globalization.Invariant` to true. Record the counts here.

## Workflow follow-up

- Archive the change in the pull request that completes it, syncing the detail-pane, config-tab and settings deltas into `openspec\specs`.
