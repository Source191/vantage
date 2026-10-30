# Tasks

## 1. The WHERE rule (Domain)

- [x] 1.1 Red: `WhereLabelShould` asserts `WhereLabel.For("Northwind.Orders", "Northwind.Orders-fix-123", "fix-123", inRepository: true).Text` reads `Northwind.Orders › fix-123`; with branch `hotfix/orders` it reads `Northwind.Orders › fix-123 (hotfix/orders)`; the main checkout (label equal to the repository, branch `main`) reads `Northwind.Orders (main)`; the inner folder `Northwind.Orders\src\Api` reads `Northwind.Orders › src\Api (main)`; a hand-made folder `spike` reads `Northwind.Orders › spike`; and `inRepository: false` with label `~\scratch\notes` reads `~\scratch\notes`. Green: the record and `For`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes.
- [x] 1.2 Red: `WhereLabelShould.ElideTheRepositoryFirst` asserts `Fit(57)` of `Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123` is 57 characters, ends with `› fix-123`, holds one "…" inside the repository name and keeps its first and last eight characters; `Fit(64)` returns the text whole; `Fit(20)` keeps `› fix-123` and cuts the repository to the rest. Green: `Fit`. Verify the Core suite passes.

## 2. The rows carry what WHERE and the swatch need (Application)

- [ ] 2.1 Red: `SessionRowEnricherShould.CarryTheCheckoutAndTheTabColour` asserts a session in a worktree gets `Checkout` set to the main checkout and `TabColour` set to the registration's colour when a live tab shows it, and null for both when outside a repository with no tab. Green: `SessionRow.Checkout` and `TabColour`, filled by the enricher from `Location` and the registrations it already reads. Verify the Core suite passes.
- [ ] 2.2 Red: `WordingOfScreensShould.GiveEveryStateAGlyph` asserts `Wording.StateGlyph` answers ◐, ●, ○, ✓, ✗ and ■ for the six kinds and ■ for unknown. Green: the switch over `DisplayStateKind`. Verify the Core suite passes.
- [ ] 2.3 Red: `WordingOfKeysAndColumnsShould` asserts `SessionsColumns` reads NAME, TAB, STATE, WHERE, PR, AGE, MEM in that order and `WorktreesColumns` reads WORKTREE, BRANCH, PR, DIRTY, UNPUSHED, LAST COMMIT, SESSION, HEALTH, each meaning within 100 characters a line, TAB's naming the two blocks and WHERE's the › rule. Green: the lists and meanings. Verify the Core suite passes and the smoke markers (`SmokeScript.ColumnOnlyIn`) resolve to NAME for sessions and WORKTREE for worktrees.

## 3. The tables (Tui)

- [ ] 3.1 `IScreenDefinition` gains `ElasticColumn` and a `width` on `Cell`; `TableSource` passes the width; `TableScreen` computes the elastic width per redraw from the content width, the other columns' longest visible cells and the separators, floor 20, and drops REPOSITORY and FOLDER from `CapLongColumns`. Verify `dotnet build -c Debug` is clean and the Core suite passes.
- [ ] 3.2 `SessionsScreenDefinition` answers the new cells: NAME, the swatch, the glyph and state, `WhereLabel.For(...).Fit(width)`, PR, AGE (right-aligned through the column style), MEM; the TAB column's `ColorGetter` draws the solid block in the tab's colour and the hollow one grey, keeping the highlight on the selected row. `WorktreesScreenDefinition` answers WORKTREE from the same rule with the worktree's folder label and BRANCH as before. Verify with `dotnet run --project tools\HeadlessTerminal -- drive` at 120 x 36 against a worktree of a repository whose name is 51 characters: the capture (`grid`) shows every sessions column within 120 cells, the WHERE cell with "…" inside the repository name and `› ` plus the slug whole; then at 127 x 36 the same cell whole; then the worktrees view at 120 x 36 with WORKTREE, BRANCH and HEALTH all on screen. Record the captures' widths here.
- [ ] 3.3 README: the sessions and worktrees column tables, the sentence on caps (NAME and BRANCH only), the TAB column's blocks, and the filter sentence; CLAUDE.md: "the FOLDER cell is `Domain\FolderLabel`" becomes "the WHERE and WORKTREE cells are `Domain\WhereLabel` over `FolderLabel`". Verify by reading both against `Wording`'s column lists.

## 4. Version and checks

- [ ] 4.1 Raise `Version` one preview step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.
- [ ] 4.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`, and the smoke check against the packed tool (`dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`). Verify both exit 0.
- [ ] 4.3 The product owner installs the build with `tools\install-local.ps1` and, on the laptop screen, opens a session in a tab and checks the swatch takes that tab's colour, that a plain `claude` row shows the hollow block, that the long repository's WHERE keeps its slug, and that `c` copies the full path; on the monitor, that the same WHERE is whole. Record here.

## Workflow follow-up

- Archive the six changes for the 0.15.0 test findings before this one, then archive this change in its own pull request, syncing the three delta specs into `openspec\specs`.
- tabs-and-key-bar bases its MODIFIED blocks for "Long names never hide columns" on this change's text.
