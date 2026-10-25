# Tasks

## 1. Domain and wording

- [ ] 1.1 `ToolVersionShould.CutTheCommitAfterThePlus`, `ShortenALocalPrereleaseToLocal`, `KeepAnOrdinaryPrerelease`, `ReadUnknownWhenBlank`: implement `Domain\ToolVersion.FromInformational`; verify `dotnet test test\Source191.Vantage.Core.Tests` passes
- [ ] 1.2 `WordingShould.TitleTheTabWithTheVersion` (`Vantage (0.5.0)`, `Vantage (0.5.0-local)`) and `ShowVersionsAndRootsInTheHeader` (`Claude: unknown` for a null version, roots joined with `; `): implement `Wording.TabTitle` and `Wording.HeaderValues`
- [ ] 1.3 `ListSelectionShould.CountShownAndTotalRowsAndLabelTheSelection`, `LeaveTheSelectionOutWhenNothingIsSelected`: change `ListSelection.Title` to return the `TableTitle` fact; update `SessionsViewShould` and `WorktreesViewShould` to assert the fact (session name and folder name as labels)
- [ ] 1.4 `WordingShould.DescribeTableTitles` (`7 sessions, selected: fix-123`, `2 of 7 sessions match "templates", selected: templates-sync`, `1 session, selected: fix-123`, `0 sessions`, `12 worktrees, selected: Northwind.Orders-fix-123`): implement `Wording.Describe(TableTitle)`
- [ ] 1.5 `WordingShould.ListEveryKeyOnceWithItsHintAndLine` and `ExplainEveryColumnOfEachView` (every column name of both definitions has a `ColumnHelp`): implement `KeyHelp`, `ColumnHelp`, `Wording.SessionsKeys`, `Wording.WorktreesKeys` and the two glossaries, with the words of today's help and the README's column tables
- [ ] 1.6 `WordingShould.BuildHelpFromKeysColumnsAndClaudePath`: generate `Wording.SessionsHelp(claudePath)` and `Wording.WorktreesHelp` from the lists, replacing the hand-laid strings; verify every key in today's help still appears

## 2. Adapters

- [ ] 2.1 `ClaudeCliShould.ReadTheVersionFromVersionOutput` (`2.1.310 (Claude Code)` gives `2.1.310`), `AnswerNullWhenVersionFails`, `AnswerNullWhenNoVersionIsPrinted`: add `IClaudeCli.VersionAsync` and implement it in `ClaudeCli` with `FakeProcessRunner`; update every other `IClaudeCli` fake to compile
- [ ] 2.2 `TerminalConsoleShould.WriteTheTitleItWasGiven` (and the existing hand-off test with `Vantage (0.5.0)`): give `TerminalConsole` its title through the constructor; verify `dotnet test test\Source191.Vantage.Infrastructure.Tests` passes

## 3. Screen

- [ ] 3.1 Replace `IScreenDefinition`'s four key strings with `IReadOnlyList<KeyHelp> Keys`, drop machine and Claude from `HeaderNames`, take `HelpText` from the generated help, and give both screens a `ScreenContext` (title, header values) in place of `context`; verify `dotnet build -c Debug` and `dotnet format --verify-no-changes` pass
- [ ] 3.2 `TableScreen.Live` lays out one key label and one hint label per `KeyHelp` in columns of six rows at today's x positions, sets `Window.Title` from the context, and after `Init` sets each row's `Line` as the tooltip of both labels through `app.ToolTips`; verify with `dotnet run --project tools\HeadlessTerminal -- drive` from 8000 ms that the header matches today's apart from the context lines, and that `check` exits 0
- [ ] 3.3 `Program.cs`: read the informational version and start `claude.VersionAsync` before config, build `ScreenContext` and `TerminalConsole` from one `Wording.TabTitle` value, and pass the Claude path to the sessions help; verify the tab reads `Vantage (<version>-local)` after `tools\install-local.ps1`, and again after attaching and pressing Ctrl+Z
- [ ] 3.4 Spike: a column-header tooltip on the table through `SetToolTip(table, Func<string>)`, the last mouse position from `table.MouseEvent` and `ScreenToCell(point, out int? header)`; keep it only if a `drive` capture shows the glossary line over a header and nothing over a row and `check` exits 0, otherwise remove it; record the outcome in the workflow notes below

## 4. Documentation and release

- [ ] 4.1 README: Start Vantage's header bullet (versions and roots, no machine or path), the table title, tooltips on key hints, the tab title `Vantage (version)` in the Vantage-tab paragraph, and the `?` help glossary; verify the README's key and column tables match `Wording`'s lists
- [ ] 4.2 CLAUDE.md: the title has one source passed to both writers, help and tooltips come from `Wording`'s key lists, and the table title is a `TableTitle` fact; bump `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s; verify `dotnet test -c Debug --no-build` at the solution and `openspec validate header-and-help --strict` pass

## Workflow follow-up

- One pull request, merged before the other five UI changes; archive the change in it with `/opsx:archive`.
- `settings-and-worktree-root` rebases its header requirement on this change's "The header shows the versions and the roots"; `folder-column-and-open-keys` extends `Wording.SessionsKeys`, `Wording.WorktreesKeys` and the glossaries rather than the header strings.
- `tools\HeadlessTerminal.Tests\TerminalQueriesShould.cs` uses `sessions(all)[4]` only as sample screen text; leave it or change it to the new title, either passes.
