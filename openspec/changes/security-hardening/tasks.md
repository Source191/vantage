# Tasks

## 1. Leftover-folder deletion never follows links (adapter)

- [x] 1.1 `FolderDeletionShould.DeleteAFolderWithReadOnlyFiles` (integration, temp folder): a tree with a read-only file in a subfolder is deleted completely; verify it passes today as the baseline for the existing behaviour
- [x] 1.2 `FolderDeletionShould.LeaveTheTargetOfAJunctionUntouched`: a tree holding `node_modules\pkg`, a junction made with `cmd /c mklink /J` through `ProcessRunner`, to an outside folder with a read-only file; after `DeleteTree` the tree is gone, the outside file exists and is still read-only; verify red for the right reason (the outside file loses read-only and `Directory.Delete` throws "Access to the path ... is denied"), commit
- [x] 1.3 Green: `DeleteTree` first checks the folder's own `ReparsePoint` attribute (a linked folder is unlinked and nothing is enumerated), otherwise walks one level at a time, removing reparse points as links, recursing into plain directories and clearing attributes on plain files, then deletes the rest recursively; verify 1.1 and 1.2 pass, commit
- [ ] 1.4 `FolderDeletionShould.RemoveAFolderThatIsItselfAJunction`: `DeleteTree` on a junction removes the link and leaves its target and its read-only file in place; write it before the root check in 1.3 lands if possible so it is red for the right reason, then refactor for readability with `dotnet build -c Debug` and `dotnet format --verify-no-changes` clean, commit

## 2. Worktree include copies stay inside the worktree (adapter tests only)

- [ ] 2.1 `WorktreeIncludeCopierShould.SkipParentRelativePathsFromGit`: `GitCli` over `FakeProcessRunner` answers `ls-files` with `../outside.local.json` and `app.local.json`, `.worktreeinclude` holds `*` and `**/*`, both files exist on disc; only `app.local.json` is copied and no file appears beside the worktree; verify it passes on first run (characterisation), commit
- [ ] 2.2 `WorktreeIncludeCopierShould.SkipRootedAndNetworkPathsFromGit`: the listing adds `C:/Users/me/secrets.local.json` and `//server/share/secrets.local.json`; neither is copied; verify it passes on first run, commit

## 3. Workflow credentials

- [ ] 3.1 Add `persist-credentials: false` to both `actions/checkout` steps in `.github\workflows\check.yml` and the one in `.github\workflows\release.yml`, and `permissions: contents: read` at the top of `check.yml`; verify with `Select-String -Path .github\workflows\*.yml -Pattern 'actions/checkout' -Context 0,3` that every checkout is followed by the setting, commit

## 4. Integration

- [ ] 4.1 Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` all pass, and `openspec validate security-hardening` is clean
- [ ] 4.2 Verify the pull request's `check` run is green, which proves `check.yml` still runs under the read-only token and without persisted credentials

## Workflow follow-up

- Archive the change in the same pull request once 4.2 is green.
- In Aikido after merge: mark `MarkerCount`, `ScreenReplay` and `HandoffCheck` Ignored ("dev-only CLI, path supplied by the local user, no trust boundary"), mark `WorktreeIncludeCopier` line 31 a false positive citing the tests in group 2, and let the next scan close `FolderDeletion` and this repository's two checkout findings.
- The other two checkout findings belong to `klr.orthopaedics.claude` and need the same workflow edit in that repository.
- If the `check` job's `upload-artifact` step fails with a permissions message under `contents: read`, that step is where an extra scope goes.
