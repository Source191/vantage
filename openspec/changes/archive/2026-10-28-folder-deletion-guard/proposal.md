# Proposal

## Why

The worktrees view's remove key deletes an orphan, broken or empty folder straight from disc, and that deletion trusts the path it is handed. No caller today passes anything but a worktree folder. A future bug that handed it a relative path, a drive root or a share root would still delete everything beneath it, with no way back. This came up from an Aikido SAST finding at `FolderDeletion.cs:19`. That finding is not exploitable: the line only removes a link, never what it points to. A guard is still worth having in a tool that deletes folders.

## What Changes

- **A guarded deletion:** before touching anything, deleting a leftover folder refuses a path that is not fully qualified, or whose full path is a drive root (`C:\`) or a share root (`\\server\share\`). The refusal names the path, and the footer shows it as a failure, the way any failed background step is reported.
- **Tests:** real-folder tests for a relative path, a drive root and a share root, each proving that nothing was deleted, plus one proving that an ordinary folder written with a trailing separator is still deleted.
- **Version:** a patch bump, above PR #82's `0.16.2-preview.3`.

Out of scope:
- **The three HeadlessTerminal findings** (`MarkerCount.cs:30`, `ScreenReplay.cs:51`, `HandoffCheck.cs:70`) stay as they are. The tool is not shipped, and its paths come from the developer's own command line or fixed file names.
- **Other guards:** refusing other important folders, such as the user profile; nothing passes them.
- **Aikido** stays uninstalled.
- **Multiple Claude accounts,** which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `worktree-management`: a new requirement that deleting a leftover folder never deletes a root or a relative path.

## Impact

- **Code:** `src\Source191.Vantage.Infrastructure\Files\FolderDeletion.cs`, and the `IFolders.DeleteTree` summary in `src\Source191.Vantage.Core\Ports\IFolders.cs`.
- **Tests:** `test\Source191.Vantage.Infrastructure.Tests\FolderDeletionShould.cs`.
- **Version:** `src\Source191.Vantage\Source191.Vantage.csproj`.
