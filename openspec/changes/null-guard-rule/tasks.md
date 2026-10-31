# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Start this change only after null-free-sessions has merged, on a branch from main. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The probe

- [ ] 1.1 `NullFreeModelsShould.SeeEveryKindOfNullable`: over a private nested probe type declared in the test file (a nullable and a non-nullable property, parameter and return, a `List<string?>`, an `int?`, and a positional record), the member finder reports exactly the nullable ones and none of the record's compiler-generated members. Red, then the finder (namespace filter aside) green. Verify the fact passes and fails when one probe annotation is flipped by hand.
- [ ] 1.2 `NullFreeModelsShould.ExemptTheHouseShapes`: a probe with `TryParse(string?, out T)` carrying `[MaybeNullWhen(false)]`, `Equals(T?)` and `CompareTo(T?)` reports nothing. Red, then the exemptions green. Verify the fact passes and a `TryParse` without the attribute is reported.

## 2. The rule over Core

- [ ] 2.1 `NullFreeModelsShould.FindTheModelTypes`: the type finder covers `Domain`, `Ports` and `Application` and their sub-namespaces, public and nested-public types only, and finds a known type from each (`SessionName`, `IGit`, `CurrentSettings`, `SessionsView`). Red, then green. Verify the fact passes.
- [ ] 2.2 `NullFreeModelsShould.HoldNoNullableMember`: run over Core with an empty allow-list and record what it reports. Every reported member is either named as kept in a sibling change's design (add it to `Kept` with a one-line reason) or a gap (list it in this task and ask the owner: allow with a reason, or a follow-up change). Green once every report is allowed or the owner has decided. Verify the fact passes, `Kept` has a reason per entry, and the gaps and the owner's decisions are recorded here.
- [ ] 2.3 `NullFreeModelsShould.KeepNoStaleEntry`: every `Kept` entry names an existing member that is still nullable. Red with a deliberately stale entry, then green with it removed. Verify the fact passes.

## 3. The rule in words, and the whole build

- [ ] 3.1 `CLAUDE.md`'s code rules gain the sentence in proposal.md, and the layering sentence names `NullFreeModelsShould` beside `LayeringShould`, `TextValuesShould` and `DiagnosticEventsShould`. Verify by reading both against design.md and checking no em dash was introduced.
- [ ] 3.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate null-guard-rule` all pass; `git diff --stat origin/main` shows nothing under `src\` (no version raise needed).

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
