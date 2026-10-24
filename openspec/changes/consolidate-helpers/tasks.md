# Tasks

## 1. Claude vocabulary into the adapter

- [ ] 1.1 `ClaudeStatesShould`: move every case from `DisplayStateShould` (busy, waiting, idle and blocked, idle and done, no pid, no pid and failed, unknown); implement `ClaudeStates.From` in `Adapters\Claude` and reduce `DisplayState` to kinds, labels and `Unknown(label)`
- [ ] 1.2 `ClaudeAgentsListing` calls `ClaudeStates`; delete `SessionListingEntry` from `Domain`; verify build, tests and the architecture test pass

## 2. One worktree inspector

- [ ] 2.1 `WorktreeInspectorShould.GatherFactsForARegisteredWorktree`, `ReportBrokenWhenGitNoLongerListsIt`, `ReportLockedWithTheReason`, `SkipMergeStateWhenNotAsked`: against `FakeProcessRunner` rules copied from `SessionActionsShould`; implement `WorktreeInspector`
- [ ] 2.2 `DeleteSessionHandler` and `RemoveWorktreeHandler` call the inspector; delete `FactsAsync` and `IsMergedAsync`; verify their tests pass unchanged
- [ ] 2.3 `WorktreeInventory.InspectedAsync` calls the inspector with merge state off; verify `WorktreeInventoryShould` passes and the number of git requests per row recorded by `FakeProcessRunner` is unchanged

## 3. One tab script factory

- [ ] 3.1 `TabScriptsShould.BuildTheAttachScript`, `BuildTheTrustScript`, `BuildTheShellScript`, `QuoteUserTextSafely`: the script assertions from `TabOpenerShould`, `TrustRecoveryShould` and `PowerShellScriptShould` verbatim; implement `TabScripts`
- [ ] 3.2 `OpenTabHandler`, `OpenShellHandler` and the trust flow take `TabScripts`; remove `claudeExecutable` and `vantageExecutable` from every other constructor; delete `TrustRecovery`; verify `grep claudeExecutable` finds it only in `Program.cs` and `TabScripts`
- [ ] 3.3 Delete `LaunchWorkflow`; the wizard dialog takes `RepositoryCatalogue`, `IRecentRepositories`, `LaunchService` and `TabScripts`; verify `dotnet run --project tools\HeadlessTerminal -- check` passes and a manual wizard run (start, start and open in tab, untrusted folder) behaves as the README describes

## 4. IGit failure convention

- [ ] 4.1 `GitCliShould`: one failure-path fact per `IGit` method that lacks one, asserting throw or null per the convention in design.md; make `GitCli` consistent and document each method on `IGit`
- [ ] 4.2 Update CLAUDE.md Code rules (facts come from `WorktreeInspector`, scripts from `TabScripts`, CLI words stay in the Claude adapter, the `IGit` convention); verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 17: task groups 1 and 2 (Claude vocabulary into the adapter, `WorktreeInspector`).
- PR 18: task groups 3 and 4 (`TabScripts`, `LaunchWorkflow` deleted, `IGit` convention). Touches different files from PR 17, so the two can be developed side by side. Archive the change in whichever merges second.
