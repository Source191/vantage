# Tasks

## 1. Seed the wording tests

- [ ] 1.1 `WordingShould`: one fact per sentence currently produced by `SessionsView`, `WorktreesView`, the eleven handlers, `TableScreen`, `DialogEffects` and `HereCommandRunner`, copied verbatim with the inputs that produce it; verify the file compiles against stub `Wording.Describe` methods that return `throw new NotImplementedException()` and every fact is red
- [ ] 1.2 Cross-check the seeded sentences against the README's key tables and footer descriptions; verify any mismatch is recorded as a README fix in this change rather than a wording change

## 2. Footers in the reducers

- [ ] 2.1 Define the `Footer` hierarchy; `SessionsViewShould` asserts footer values (`Footer.NoSelection`, `Footer.RunsInOwnTerminal(...)`, and so on) instead of strings; `SessionsView` returns them; `Wording.Describe(Footer)` makes the matching `WordingShould` facts green
- [ ] 2.2 The same for `WorktreesViewShould` and `WorktreesView`, including the health fallback and the force hint as footer values
- [ ] 2.3 `TableScreen.Redraw` renders `Wording.Describe(state.Footer)`; verify `dotnet run --project tools\HeadlessTerminal -- check` passes

## 3. Outcomes from the handlers

- [ ] 3.1 `StopSessionHandler`, `PruneRepositoryHandler`, `OpenShellHandler`: return outcome records; their tests assert on the records; `Wording` facts go green
- [ ] 3.2 `AdoptSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`: the same, including the failure paths
- [ ] 3.3 `CloseEmptyTerminalsHandler`: `CloseEmptyOutcome` replaces `Summary`; the pluralisation moves into `Wording`
- [ ] 3.4 `DeleteSessionHandler`, `DeleteBranchHandler`, `RemoveWorktreeHandler`: outcomes keep `BranchToConfirm`; the dialog questions move to `Wording`
- [ ] 3.5 `OpenTabHandler` and `StartSessionHandler`: outcomes including the palette-reused warning and the trust explanation
- [ ] 3.6 `IEffectHandler<TEffect>` returns `Task<Outcome>` (a marker base record) and `TableScreen` renders through `Wording`; verify every `WordingShould` fact is green and the hand-off check passes

## 4. Documentation

- [ ] 4.1 Update CLAUDE.md Code rules: user-facing text lives in `Wording` and its test file; handlers and reducers return values; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 12: task groups 1 and 2 (`WordingShould` seeded for the reducers' sentences, the `Footer` hierarchy, reducers return values). The seed facts for handler sentences are written here too but marked `Skip` until PR 13, so CI stays green.
- PR 13: task groups 3 and 4 (outcome records from the handlers, the skipped facts unskipped). Archive the change in this PR.
