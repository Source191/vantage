# Proposal

## Why

Four tab symptoms from the 0.15.0 test have one cause. A tab Vantage opens runs `pwsh -NoExit` with a script that claims the tab's registration with that shell's process and then runs `claude attach`; the registration is live while the shell lives, and nothing but the close-empty key ever ends a shell. So after a rename or a delete (both `claude rm`), a stop, or a Ctrl+Z detach, the attach ends but the shell sits at a prompt: TAB keeps saying open, Enter is refused with "open in another tab", `o` after a rename searches for a tab under the new name and ends up with two tabs, and the shell's current directory keeps the worktree folder locked, which is why `d` reported "Permission denied" and left an empty folder and a registration behind. Adopt leaves its old tab as a plain shell by design. Two more findings sit beside these: `o` trusted a row flag up to five seconds old and searched every Windows Terminal window by title once the shell had gone, so a tab closed by hand could still answer "Switched"; and `d` on a session whose worktree had lost its `.git` failed with "not a git repository" after the session was already removed.

## What Changes

- **The attach tab closes with the attach.** The tab's script exits when `claude attach` ends by a detach, a stop or a removal (the first task measures the exit codes the script keys on), so Windows Terminal closes the tab, TAB clears and Enter works again; a failed attach keeps the tab open to show its error. The worktrees view's shell tab and the update tab keep their prompt. This and the stop below are the owner's 6.6 preference against leftover shells, applied to every tab Vantage opens for a session.
- **Rename moves the tab.** The old tab is closed after the job is removed and, once the session is resumed under the new name, a tab is opened under that name with the same colour.
- **Delete and stop close the tab first.** Delete ends the tab's shell and waits for it before the worktree is touched; stop closes the tab too.
- **Adopt closes its old tab** once the resume has succeeded; a failed resume keeps the prompt in the session's folder.
- **`o` checks the tab is alive now.** A registration whose shell has gone is dropped and a new tab opened; the window-wide search by title is only for a tab still being claimed. Opening a tab also drops any earlier registration for the same session, which held a palette colour.
- **Delete copes with a broken or in-use worktree.** Files are counted only for a healthy worktree; a worktree whose `.git` has gone has its folder deleted and its registration pruned, as the worktrees view already does, through one remover shared by both views; and when git cannot delete the folder because a tab or program still uses it, the footer says so and names the folder, the session is deleted, and a second remove once the tab has closed finishes the job. The worktrees view's `d` reports the same in-use outcome.
- **Version:** a minor raise: `r`, `x`, `d` and `b` now act on tabs, and adopt's old-tab behaviour reverses.

Out of scope: a take-over key, attaching to one session from two tabs (still an open item), the WHERE column, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: "Open a session in its own Windows Terminal tab" (liveness checked at the key, stale registrations dropped), "Live tabs are tracked by process" (the attach tab closes with the attach), "Stop and delete resolve the session ID" (stop closes the tab), "Delete offers to remove the session's worktree" (tab first, broken and in-use worktrees), "Rename without attaching" (the tab moves), "Terminal sessions can be adopted" (the old tab closes).
- `worktree-management`: "Removal refuses to destroy work" gains the in-use outcome.

## Impact

- **Core:** a new `Application\Actions\SessionTabs.cs` and `WorktreeRemover.cs`; `OpenTabHandler.cs`, `RenameSessionHandler.cs`, `StopSessionHandler.cs`, `DeleteSessionHandler.cs`, `AdoptSessionHandler.cs`, `RemoveWorktreeHandler.cs`; `Application\TabScripts.cs` (the exit); `Application\Effects\Composition.cs`; `Domain\TabRegistration.cs` and `Domain\TerminalTab.cs` (one parent-chain walker); `Ports\GitException.cs` and `Ports\IGit.cs` (the in-use failure); `Application\WordingOfOutcomes.cs`.
- **Infrastructure:** `Git\GitCli.cs` (recognises git's "Permission denied" on a worktree removal).
- **Composition:** `Program.cs` hands `DeleteSessionHandler` the tab ports.
- **Tests:** SessionTabsShould (new), OpenTabHandlerShould, RenameSessionHandlerShould, StopSessionHandlerShould, DeleteSessionHandlerShould, AdoptSessionHandlerShould, RemoveWorktreeHandlerShould, TabScriptsShould, TabRegistrationShould, TerminalTabShould, GitCliShould, WordingOfOutcomesShould; `FakeProcessRegistry` records the order of what it ends.
- **Docs:** README (the TAB column, "Open in its own tab", detach, rename, stop, delete, adopt, the worktrees view's remove), CLAUDE.md's Windows Terminal and CLI facts (attach's exit codes, the registration's end), the two specs.
- **Release:** a minor version raise; the README's manual checks for tabs and attach at the terminal.

## Order

Pull request 4 of six for the 0.15.0 test findings, which merge one after another in this order, each raising the version one preview step: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump. The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
