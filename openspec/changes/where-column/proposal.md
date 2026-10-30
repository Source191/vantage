# Proposal

## Why

The sessions table spends three columns (REPOSITORY, FOLDER, BRANCH) on one fact: for a worktree Vantage made, the folder is `<repository>-<slug>` and the branch is `<slug>`, so sixty columns say the same thing three times, and a 51-character repository name still pushes PR, AGE, TAB and MEM off a 120-column terminal. The caps and the sideways scroll of header-tooltips-and-table-fit (#98) made those columns reachable; they did not make them fit, and an ellipsis at 28 cells hides the end of a long name, which is the part that tells MK.Integration.CosmosDb.ChangeFeedMonitoring from its siblings. The UI review of 26 October chose one WHERE column as the fix, and the owner confirmed the revised recommendation on 30 October 2026.

## What Changes

- **One WHERE column on the sessions view** replaces REPOSITORY, FOLDER and BRANCH: `Northwind.Orders › fix-123` for a worktree whose folder is `Northwind.Orders-fix-123` on branch `fix-123`; `Northwind.Orders › fix-123 (hotfix)` when the branch is not the slug; `Northwind.Orders (main)` for the main checkout; `Northwind.Orders › src\Api (main)` for a folder inside the repository; the folder-actions label (`~\scratch\notes`, a full path) for a folder outside any repository or one that has gone. The filter still matches the name, the repository and the branch; `c`, `e`, `v` and the title keep the full folder.
- **WHERE is elastic and elides in the middle.** It takes the width the other columns leave; a repository name too long for it loses its middle (`Northwind.Integrati…ChangeFeedMonitoring › fix-123`), so both ends stay readable and the slug and branch are never cut. The caps on REPOSITORY and FOLDER retire with the columns; NAME keeps its cap and Left and Right keep scrolling, for terminals narrower than 120.
- **TAB becomes a one-cell swatch:** a solid block in the tab's colour while a Vantage tab shows the session, a hollow grey block for a plain `claude` in its own terminal, blank otherwise. The column keeps its name, and the help and tooltip say what the two blocks mean.
- **STATE gains a glyph and AGE is right-aligned:** `◐ needs input`, `● working`, `○ idle`, `✓ completed`, `✗ failed`, `■ stopped`; the words and colours are unchanged.
- **The worktrees view gets one WORKTREE column** in place of REPOSITORY and FOLDER (`Northwind.Billing › invoice-pdf`; `Northwind.Orders › Northwind.Orders-old` for a leftover whose folder does not follow the naming), elastic and elided the same way. BRANCH stays its own column there, as the owner's 27 October mockup shows, because the view's facts (unpushed commits, merge state) are the branch's. Folding it in as the sessions view does is the alternative.
- **MEM stays** until the detail pane carries the figure (owner's decision, 30 October 2026). With MEM's six cells WHERE has about 57 cells at 120 columns, so the 61-character `Northwind.Integration.CosmosDb.ChangeFeedMonitoring › fix-123` is elided in the middle on the laptop and shown whole from about 127 columns; the detail-pane change removes MEM and gives WHERE 64.
- **Version:** one preview step above `main`'s (`0.18.0-preview.N`); the release of 0.18.0 is the owner's call once the series is done.

Out of scope: the slim header, the key bar and the Sessions and Worktrees tabs (tabs-and-key-bar), the wizard (wizard-one-form), the detail pane (detail-pane), hiding columns by priority on terminals narrower than 120 (the scroll covers them), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: "Rows show repository, branch and worktree" becomes the WHERE rule; "Long names never hide columns" (added by header-tooltips-and-table-fit) says WHERE elides in the middle and every column fits at 120; "Interactive sessions are shown with limited actions" marks the kind with the swatch; "States are colour coded" gains the glyphs; an added "The TAB column is a swatch".
- `folder-actions`: "The folder is shown relative to its root" becomes the rule for WHERE's folder part and fallback; the FOLDER column goes.
- `worktree-management`: "Worktrees are listed across all roots" names the WORKTREE column; an added "One WORKTREE column names the worktree".

## Impact

- **Domain:** a new `WhereLabel` (the parts and the text) with its middle elision, beside `FolderLabel`.
- **Application:** `SessionRow` gains the checkout (null outside a repository) and the live tab's colour, which `SessionRowEnricher` already reads from the registrations; `WordingOfScreens` (the column lists, meanings and the state glyphs).
- **Tui:** `SessionsScreenDefinition` and `WorktreesScreenDefinition` (cells, the elastic column), `TableScreen` and `TableSource` (the elastic width, the swatch's colour through the column style, right alignment), `IScreenDefinition` (the elastic column and a width for its cell).
- **Tests:** `WhereLabelShould` (new), `SessionRowEnricherShould`, `WordingOfKeysAndColumnsShould`, `WordingOfScreensShould`; headless captures at 120 x 36 and 127 x 36.
- **Docs:** README (both column tables, the sentences on caps and scrolling, the TAB column), CLAUDE.md (the FOLDER cell fact becomes the WHERE fact), the three specs.
- **Release:** a preview raise; the owner's terminal check of the swatch colours.

## Order

The first of four changes from the UI review (where-column, tabs-and-key-bar, wizard-one-form, detail-pane), each its own pull request raising `Version` one preview step. The six changes for the 0.15.0 test findings (attach-and-command-line-clean-up, header-tooltips-and-table-fit, pull-request-lookups, session-tabs-and-delete, dialog-fixes, dependabot-version-bump) are merged but not yet archived, so this change's MODIFIED blocks are based on their delta text where they overlap (the "Long names never hide columns" requirement), and they archive before it.
