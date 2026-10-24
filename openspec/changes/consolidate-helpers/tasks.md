# Tasks

## 1. Claude vocabulary into the adapter

- [x] 1.1 `ClaudeStatesShould`: move every case from `DisplayStateShould` (busy, waiting, idle and blocked, idle and done, no pid, no pid and failed, unknown); implement `ClaudeStates.From` in `Adapters\Claude` and reduce `DisplayState` to kinds, labels and `Unknown(label)`
- [x] 1.2 `ClaudeAgentsListing` calls `ClaudeStates`; delete `SessionListingEntry` from `Domain`; verify build, tests and the architecture test pass

## 2. One worktree inspector

- [x] 2.1 `WorktreeInspectorShould.GatherFactsForARegisteredWorktree`, `ReportBrokenWhenGitNoLongerListsIt`, `ReportLockedWithTheReason`, `SkipMergeStateWhenNotAsked`: against `FakeProcessRunner` rules copied from `DeleteSessionHandlerShould` and `RemoveWorktreeHandlerShould`; implement `WorktreeInspector`
- [x] 2.2 `DeleteSessionHandler` and `RemoveWorktreeHandler` call the inspector; delete `FactsAsync` and `IsMergedAsync`; verify their tests pass unchanged
- [x] 2.3 `WorktreeInventory.InspectedAsync` calls the inspector with merge state off; verify `WorktreeInventoryShould` passes and the number of git requests per row recorded by `FakeProcessRunner` is unchanged

## 3. One tab script factory

- [ ] 3.1 `TabScriptsShould.BuildTheAttachScript`, `BuildTheTrustScript`, `BuildTheShellScript`, `QuoteUserTextSafely`: the script assertions from `OpenTabHandlerShould`, `TrustRecoveryShould` and `PowerShellScriptShould` verbatim; implement `TabScripts`
- [ ] 3.2 `OpenTabHandler`, `OpenShellHandler` and `StartSessionHandler` take `TabScripts`; remove `claudeExecutable` and `vantageExecutable` from every other constructor; delete `TrustRecovery`; verify `grep claudeExecutable` finds it only in `Program.cs` and `TabScripts`
- [ ] 3.3 Delete `LaunchWorkflow`; the wizard dialog takes `RepositoryCatalogue`, `IRecentRepositories` and `IFolders`; verify `dotnet run --project tools\HeadlessTerminal -- check` passes and a manual wizard run (start, start and open in tab, untrusted folder) behaves as the README describes

## 4. IGit failure convention

- [ ] 4.1 `GitCliShould`: one failure-path fact per `IGit` method that lacks one, asserting throw or null per the convention in design.md; make `GitCli` consistent and document each method on `IGit`
- [ ] 4.2 Update CLAUDE.md Code rules (facts come from `WorktreeInspector`, scripts from `TabScripts`, CLI words stay in the Claude adapter, the `IGit` convention); verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 17: task groups 1 and 2 (Claude vocabulary into the adapter, `WorktreeInspector`).
- PR 18: task groups 3 and 4 (`TabScripts`, `LaunchWorkflow` deleted, `IGit` convention). Touches different files from PR 17, so the two can be developed side by side. Archive the change in whichever merges second.
- PR 17 notes: `WorktreeInspector` has three methods rather than one `InspectAsync`: `CountAsync` (changed files, unpushed commits, last commit), `HealthAsync` (git's listing: broken, locked with its reason, or fine) and `IsMergedAsync` (into the remote default, else HEAD). One method would have made the scan list worktrees again per row and the removal handler re-count a row the scan already counted, against tasks 2.2 and 2.3; with three, the scan asks git the same three things per row (`WorktreeInventoryLeftoversShould.AskGitThreeThingsPerRegisteredWorktreeAndNothingAboutMergeState`), the removal handler only asks about merge state, and the handlers' tests pass unchanged. `SkipMergeStateWhenNotAsked` became `AskWhetherTheBranchIsMergedIntoTheRemoteDefault` and `FallBackToHeadWhenThereIsNoRemoteDefault`. Each caller builds its inspector from the `IGit` it already takes. Found for task 4.1: `GitCli.CountUnpushedCommitsAsync` parses git's output with `int.Parse`, so an empty reply throws `FormatException`, which escapes the scan's `catch (GitException)` and fails the whole worktrees view; the convention should make a parse failure a `GitException`.
