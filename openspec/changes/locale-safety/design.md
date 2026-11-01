# Design

## Context

See proposal.md for why. What shapes the approach:

- **The clock:** every caller reads `TimeProvider.GetUtcNow()` and hands it to `Wording.Detail(detail, now)` (from `SessionDetails` and `WorktreeDetails` in `Application\Detail\IDetailSource.cs`) and, through `TableBody.Redraw` and `IScreenDefinition.Cell`, to `Wording.ConfigValue(row, now)`. `WordingOfDetail.DayAndTime` and `WordingOfConfig.When` then call `at.ToOffset(now.Offset)`, which is UTC. Stored times stay UTC, as the coding rules require; only the words change.
- **Invariant mode:** the shipped tool runs with `System.Globalization.Invariant` (the tool project's `InvariantGlobalization`). Culture-aware comparisons are then ordinal, and `CompareOptions.NumericOrdering`, which `EditorLocator.NewestRider` relies on, is ignored without an exception (checked 1 November 2026, .NET 10.0.403 SDK: 2025.9 sorts above 2025.10 in invariant mode and below it otherwise). Windows time zones still work in invariant mode, because they come from the registry: `FindSystemTimeZoneById("GMT Standard Time")` converted 08:14 UTC on 23 October 2026 to 09:14 +01:00 in an invariant probe.
- **Analysers:** with `InvariantGlobalization` true a project gets no CA1304, CA1305, CA1309, CA1310 or CA1311, whatever `.editorconfig` says (a probe showed all five firing with it false and none with it true). A whole-solution build with it false raised none of them, so turning them on costs no code change today. Only an executable's `runtimeconfig.json` carries the setting at run time, so on a library project it does nothing but silence the analysers.
- **git:** `git status --porcelain` quotes and octal-escapes a path with bytes above 0x7F while `core.quotePath` is true, git's default (`?? "na\303\257ve.txt"`, git 2.56.0.windows.3). `git worktree list --porcelain` and `git log --format=%s` were not quoted in the same check.

## Goals / Non-Goals

**Goals:**

- Times in words are local, converted with the zone's rule for each moment; nothing stored changes.
- The Rider order is the same in the tests and in the tool, and does not depend on culture data.
- The tests run in the mode the tool ships in, and the culture analysers watch the shipped code.

**Non-Goals:**

- Showing dates in the user's culture's format: the formats stay `HH:mm`, `d MMM HH:mm` and `d MMMM yyyy HH:mm` with the invariant culture, which give English month names.
- Following a time zone change while Vantage runs: .NET caches the local zone, and the next start picks it up.

## Decisions

### 1. Wording takes the time zone beside the time

`Wording.Detail(detail, now, zone)` and `Wording.ConfigValue(row, now, zone)` take a `TimeZoneInfo`. `DayAndTime` and `When` convert both `at` and `now` with `TimeZoneInfo.ConvertTime(value, zone)` and compare the converted dates for today. `SessionDetails` and `WorktreeDetails` pass `clock.LocalTimeZone`. `ConfigScreenDefinition` takes the `TimeProvider` in its constructor (`ConsoleScreen` hands it `screen.clock`) and passes `clock.LocalTimeZone` per cell, so `IScreenDefinition.Cell` keeps its signature.

- **Alternative, a local `now` (`GetLocalNow()`):** rejected. It still converts every past time with today's offset, so a time from before the clocks went back is an hour out.
- **Alternative, `TimeProvider` into `Wording`:** rejected. `Wording` turns facts into words and takes no services.
- **Alternative, a new `IScreenDefinition.Cell` parameter:** rejected. Only the Config tab needs the zone, and the other definitions would carry a parameter they ignore.

Tests use the Windows zones `GMT Standard Time` and `E. Africa Standard Time` by id (Vantage is Windows only, and the ids work in invariant mode), and `FakeTimeProvider.SetLocalTimeZone` where a `TimeProvider` is involved.

### 2. A culture-free number-aware order for Rider

A static `Domain\NumberAwareOrder.Compare(string, string)` compares two strings run by run; callers wrap it with `Comparer<string>.Create`. It is not an `IComparer<string>` itself, because that interface's `Compare(string?, string?)` would be a nullable member of a Domain type, which `NullFreeModelsShould` refuses. A run of ASCII digits compares with a run of ASCII digits by value: leading zeros are dropped, then the shorter run is smaller, then digit by digit. Any other character compares by `char.ToUpperInvariant` ordinally. Where everything compares equal, the shorter string comes first, then an ordinal comparison decides, so only the same text answers zero. `EditorLocator.NewestRider` orders by it descending in place of `StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering | CompareOptions.IgnoreCase)`.

- **Alternative, parse a version from the folder name:** rejected. Standalone folders read `JetBrains Rider 2025.10` and Toolbox ones `253.28294.112`, so it means two parsers for the same answer.
- **Alternative, turn invariant mode off for the tool:** rejected. It loads ICU, makes every culture-aware call live on the user's culture, and is the decision recorded for translation in `CLAUDE.md`, not a fix for one sort.

It compares whole paths, as now, so a Toolbox install (under `%LOCALAPPDATA%`) still ranks above one under Program Files (proposal, out of scope).

### 3. `-c core.quotePath=false` for the changed files

`GitCli.ChangedFilesAsync` runs `git -c core.quotePath=false status --porcelain`. Only that list shows paths. A diagnostic record keeps a child's leading arguments only while each is on its program's fixed word list, so `-c` and `core.quotePath=false` join git's list in `ChildCommandWords`, and the record reads `git -c core.quotePath=false status` rather than a bare `git`; both are fixed words, so nothing personal can enter by them. `CountChangedFilesAsync` counts lines, which quoting does not change, so it stays as it is.

- **Alternative, `-z`:** rejected for now. It also handles a tab, a newline or a double quote in a name (which git still quotes with `core.quotePath` off) and gives renames as two entries, but it needs a new parser for a case nobody has reported.

### 4. Move `InvariantGlobalization` from the libraries to the tests

The setting leaves `Source191.Vantage.Core`, `Source191.Vantage.Infrastructure` and `Source191.Vantage.Tui` and joins `Source191.Vantage.Core.Tests`, `Source191.Vantage.Infrastructure.Tests` and `HeadlessTerminal.Tests`. All three test projects are executables under Microsoft.Testing.Platform, so their `runtimeconfig.json` puts the test run in invariant mode. The tool project and `tools\HeadlessTerminal` keep it. Each affected project's `README.md` says why (no comments in `.csproj` files). `TestDoubles` is a library and gets nothing.

Moving the test projects first makes `EditorLocatorShould.RankRiderBuildsByNumberNotByText` fail, the only one of the 2,048 tests in `Core.Tests` and `Infrastructure.Tests` that does under `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (checked 1 November 2026). That failure is the red step for decision 2.

- **Alternative, keep the libraries invariant and set the CA rules in `.editorconfig`:** not possible. The analysers check the build property and stop before severity applies.
- **Trade-off:** test code loses the culture analysers. Tests that run in the shipped mode are worth more than analysers on test code.

### 5. A section in `CLAUDE.md` for translation and other cultures

It records what supporting another language or culture will need: turning invariant mode off and what that wakes up, `Wording` becoming resources, culture-formatted dates and numbers, cell widths, slugs for other scripts, the az code page, git's translations, and the parts that must stay invariant (state files, diagnostics, arguments to child programs). Its wording is drafted in task 6.1.

## Risks / Trade-offs

- **[A time zone id missing on a runner]:** both ids ship with every Windows 11 and Server image. → The tests fail loudly with `TimeZoneNotFoundException` rather than passing wrongly.
- **[The packed tool loses invariant mode]:** only the tool project's setting reaches the packed tool's runtimeconfig, which this change keeps. → A task reads `System.Globalization.Invariant` in the packed tool's `runtimeconfig.json`.
- **[A future library call on the current culture]:** the analysers now catch it at build time.
- **[`core.quotePath=false` leaves some names quoted]:** a name with a double quote, a backslash or a control character is still quoted. → Accepted; it is rare in a worktree, and decision 3 records the `-z` route.
