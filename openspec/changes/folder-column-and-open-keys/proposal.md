# Proposal

## Why

The point of a session's folder is to use it with other tools: open it in VS Code to read the diffs and commits, open its solution, paste its path into a shell. Today the sessions table shows only the repository name and a `yes` in WT, the full path is nowhere on screen, and getting from a row to an editor means typing the path by hand. One key per job, and a folder column that says where the session is without the whole path, removes that step.

## What Changes

- **Sessions table:** the WT column is replaced by FOLDER, the session's working directory relative to the configured root that holds it (a repository root or the worktree root), else relative to the home folder as `~\...`, else the full path. Columns become NAME, STATE, REPOSITORY, FOLDER, BRANCH, AGE, TAB. A worktree is still recognisable: its folder name differs from its repository's.
- **Worktrees table:** FOLDER uses the same rule instead of the bare folder name.
- **`c` in both views:** copies the selected row's full folder path to the clipboard and says so in the footer.
- **`e` in both views:** opens the selected row's folder in the editor chosen in settings (VS Code by default).
- **`v` in both views:** opens the folder's solution with Windows' default application for it. Vantage looks for `*.sln` and `*.slnx` in the folder, then in its `src` folder; one found opens; several found opens the one named after the repository, else asks which; none found says so in the footer.
- The three keys work on every row that has a folder, including terminal and stopped sessions; a worktrees row whose folder is gone is refused with the existing leftover footer.
- The header's key hints gain a third column for the new keys; `?` help lists them.
- `SessionRow.IsWorktree`, used only by the WT column, goes.

Out of scope: a pull request column or key (change `pull-request-column`), showing the full path on screen, filtering by folder, opening a file or a diff view inside the editor, editors on other platforms, multiple Claude accounts.

Depends on `remove-session-descriptions` (the DESCRIPTION column has gone, making room) and `settings-and-worktree-root` (the editor setting and the worktree root that FOLDER is relative to).

## Capabilities

### New Capabilities

- `folder-actions`: what both table views let the user do with a row's folder: show it relative to its root, copy its path, open it in the chosen editor, open its solution.

### Modified Capabilities

- `session-listing`: "Rows show repository, branch and worktree" changes, because the row now shows the folder instead of a worktree marker.

## Impact

- `Core`: new `Domain\FolderLabel` (the relative-path rule), `Application\SolutionFinder`, `Application\FolderLabels` (roots from the current config), effects `CopyFolderPath`, `OpenFolderInEditor`, `FindSolution` (a dialog effect) and `OpenSolution`, handlers `CopyFolderPathHandler`, `OpenInEditorHandler`, `OpenSolutionHandler` with their outcome records, new ports `IClipboard`, `IDesktop` and `IEditorLocator`, `IFolders.FilesIn`, reducer cases in `SessionsView` and `WorktreesView`, new `Footer` cases and `Wording`/`WordingOfOutcomes` entries.
- `Infrastructure`: `WindowsClipboard` (Win32 clipboard), `Desktop` (ShellExecute and detached starts with `InheritedEnvironment` applied), `EditorLocator` (finds each preset's real executable without cmd.exe), `Folders.FilesIn`.
- `Tui`: both screen definitions (columns, cells, key hints), `DialogEffects` (the solution chooser flow), `TableScreen` (a third key-hint column).
- Tests: reducer, handler, wording, dispatcher and dialog facts as CLAUDE.md requires; adapter tests in `Infrastructure.Tests`; fakes in `TestDoubles`.
- README (columns and keys), CLAUDE.md (the launching and clipboard ports), a minor version bump.
