# Proposal

## Why

The 0.15.0 test found the header and tables misbehaving at the owner's two screen sizes (about 120 and 126 columns): a key's tooltip is drawn in the header's own colours and runs straight into the next hint, says only the help's terse one-liner, the PR column's tooltip runs off the right edge below the table, the Sessions line reads "6 ()" when every session needs input, the right-hand columns of both tables (PR, AGE and TAB; SESSION and HEALTH) vanish behind one long repository or folder name, and the third column of key hints sits beyond column 120, so `v` and `p` show bare. The WHERE column and the key bar redesign replace the layout later; this change is the stopgap that makes the pull-request and tab columns reachable now and fixes the tooltips and the summary for good. Issue #89, found on 29 October, belongs here too: the Keys help has grown to 26 lines while the dialog is clipped at about 31 rows on a 40-row terminal, so its last three lines, the TAB column's line and the Claude Code path, are never shown, and a 36-row terminal loses more.

## What Changes

- **The Sessions line.** "6 ()" becomes "6": the brackets appear only when some session is in a state other than needs input. The sentence moves into Wording, where user text belongs, with a fact.
- **Key tooltips.** Each key gets a tooltip sentence saying what it does and when to use it (the help keeps its one-liner), shown on the key and on its words, drawn in its own colours (the crumb's black on amber) with a margin, so it reads as a tooltip and not as more header text.
- **Column tooltips.** The column tooltip keeps the help's line breaks, so the PR column's two lines no longer run off the screen; the PR meaning is shortened to fit two lines.
- **Long names and the tables.** NAME, REPOSITORY, FOLDER and BRANCH are capped and end in an ellipsis when cut, and Left and Right scroll the table sideways, so the right-hand columns can always be reached. At 120 columns the sessions table still needs a scroll to show TAB: eight columns do not fit, which is what the WHERE column fixes.
- **Key hints within the owner's screens.** The third hint column (`c`, `e`, `v`, `p`) moves under the logo and the columns tighten, so every key ends by column 112 and every word by column 123: the monitor window (about 126 columns) shows them all, and at 120 columns the keys show with their words cut, which waits for the key bar. The alternative, if the laptop matters more now: demote `/`, `?` and `q` to help-only and fold the third column into the second, which fits 120.
- **The help fits the screen.** The help dialog scrolls: its text sits in a read-only box that takes the dialog's height, with the arrow keys and PageUp and PageDown moving through it, so every line, the Claude Code path included, is reachable on a 36-row terminal and the whole text is visible at 40 rows; the worktrees help the same. Closes #89.
- **Version:** a patch raise (fixes and text).

Out of scope: the WHERE column, the key bar and the Sessions and Worktrees tabs (the redesign changes that follow), a tooltip on a row, the e key's wording (dialog-fixes), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: "Key hints explain themselves on hover" shows a sentence in its own colours on the key and its words; "Column headers explain themselves on hover" keeps line breaks; "Help explains the columns" is reachable in full on a 36-row terminal; two added requirements, "The header summarises sessions by state" and "Long names never hide columns".
- `worktree-management`: "The worktrees header and help match the sessions view" follows the same tooltip and help rules.

## Impact

- **Core:** `Application\KeyHelp.cs` (a Tip per key), `Application\WordingOfScreens.cs` (the tips, the sessions summary, the PR column meaning).
- **Tui:** `TableScreen.cs` (the tooltip view and its colours, the column tooltip's lines, the column caps and the sideways scroll, a column's first row), `KeyHintColumns.cs` (the layout), `SessionsScreenDefinition.cs` (the summary from Wording), `KeyMapping.cs` (Left and Right).
- **Tui, the help:** a new `HelpDialog.cs` in place of the generic question dialog for `ShowHelp`, wired in `DialogEffects.cs`.
- **Tests:** WordingOfScreensShould, WordingShould (every key has a tip; every column meaning fits), KeyHintColumnsShould, the headless captures at 120 x 36 and 120 x 40 (the help's last line counted).
- **Docs:** README's sentences on tooltips and the help, and the keys table (Left and Right); CLAUDE.md's Terminal.Gui fact that caps the help's length; the two specs; issue #89 closes with the pull request.
- **Release:** a patch version raise.

## Order

Pull request 2 of six for the 0.15.0 test findings, which merge one after another in this order, each raising the version one preview step: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump. The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
