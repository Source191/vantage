# Tasks

## 1. Seed the wording tests

- [x] 1.1 `WordingShould`: one fact per sentence currently produced by `SessionsView`, `WorktreesView`, the eleven handlers, `TableScreen`, `DialogEffects` and `HereCommandRunner`, copied verbatim with the inputs that produce it; verify the file compiles against stub `Wording.Describe` methods that return `throw new NotImplementedException()` and every fact is red
- [x] 1.2 Cross-check the seeded sentences against the README's key tables and footer descriptions; verify any mismatch is recorded as a README fix in this change rather than a wording change

## 2. Footers in the reducers

- [x] 2.1 Define the `Footer` hierarchy; `SessionsViewShould` asserts footer values (`Footer.NoSelection`, `Footer.RunsInOwnTerminal(...)`, and so on) instead of strings; `SessionsView` returns them; `Wording.Describe(Footer)` makes the matching `WordingShould` facts green
- [x] 2.2 The same for `WorktreesViewShould` and `WorktreesView`, including the health fallback and the force hint as footer values
- [x] 2.3 `TableScreen.Redraw` renders `Wording.Describe(state.Footer)`; verify `dotnet run --project tools\HeadlessTerminal -- check` passes

## 3. Outcomes from the handlers

- [x] 3.1 `StopSessionHandler`, `PruneRepositoryHandler`, `OpenShellHandler`: return outcome records; their tests assert on the records; `Wording` facts go green
- [x] 3.2 `AdoptSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`: the same, including the failure paths
- [x] 3.3 `CloseEmptyTerminalsHandler`: `CloseEmptyOutcome` replaces `Summary`; the pluralisation moves into `Wording`
- [x] 3.4 `DeleteSessionHandler`, `DeleteBranchHandler`, `RemoveWorktreeHandler`: outcomes keep `BranchToConfirm`; the dialog questions move to `Wording`
- [x] 3.5 `OpenTabHandler` and `StartSessionHandler`: outcomes including the palette-reused warning and the trust explanation
- [ ] 3.6 `IEffectHandler<TEffect>` returns `Task<Outcome>` (a marker base record) and `TableScreen` renders through `Wording`; verify every `WordingShould` fact is green and the hand-off check passes

## 4. Documentation

- [ ] 4.1 Update CLAUDE.md Code rules: user-facing text lives in `Wording` and its test file; handlers and reducers return values; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 12: task groups 1 and 2 (`WordingShould` seeded for the reducers' sentences, the `Footer` hierarchy, reducers return values). The seed facts for handler sentences are written here too but marked `Skip` until PR 13, so CI stays green.
- PR 13: task groups 3 and 4 (outcome records from the handlers, the skipped facts unskipped). Archive the change in this PR.
- PR 12 notes: the handler and `HereCommandRunner` sentences were not seeded as skipped facts, because a skipped fact still has to compile against outcome records that PR 13 designs. Their `...HandlerShould` and `HereCommandRunnerShould` tests already hold every one verbatim, so PR 13's first commit copies them into `WordingShould` before any handler changes. Two fragments still glued to handler text in `DialogEffects.AskAboutBranch` ("{message} Branch kept." and "{message} {deleted}") go with task 3.4, and the three `HereCommandRunner` sentences with 3.5. `Footer.Reported` carries a handler's sentence until then, and `WorkingText` became `Wording.Working`. The README cross-check (1.2) found two rows that disagreed with the code, the sessions view's `d` and the branch rule, and both were fixed in the README. It also found a wording defect for task 3.4: removing a worktree whose branch Vantage created but has not merged says "branch X was not created by Vantage, so it is kept." and asks "was not created by Vantage. Delete it anyway?", which is wrong on both counts. `RemoveOutcome` should carry whether Vantage created the branch so `Wording.DeleteBranchQuestion` picks the right sentence; that is a behaviour fix to list in PR 13's description.
