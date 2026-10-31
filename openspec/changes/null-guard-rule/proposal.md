# Proposal

## Why

The null programme (owner's decision, 31 October 2026) takes null out of Vantage's own models and results across eight changes: null stands for no state of Vantage's own, and is met only on the line that receives it from outside. Without a mechanical check the gain wears away: the next record that needs "maybe" gets a `T?` because it is the shortest thing to type. The existing architecture facts (`LayeringShould`, `TextValuesShould`, `DiagnosticEventsShould`) show how the project already keeps rules like this; this change adds one for null, plus the sentence in `CLAUDE.md` that states it.

## What Changes

- **`Architecture\NullFreeModelsShould`** in `test\Source191.Vantage.Core.Tests`: reflects over every public type in the `Domain`, `Ports` and `Application` namespaces and fails for a public property, public constructor or method parameter, or method return that is nullable (a reference `T?` or a value `Nullable<T>`, including inside generic arguments such as `IReadOnlyList<string?>`), unless it is a recognised house shape or on a short allow-list.
  - **House shapes, exempt by rule:** `TryParse(string?, out T)` (text from outside, `[MaybeNullWhen(false)]` out), `Equals(T?)` and `CompareTo(T?)`, compiler-generated record members.
  - **Allow-list:** each entry names a type, a member and the reason, from the nullables the programme kept on purpose: the six `IGit` queries (documented per method), `IProcessRegistry`'s `StartTimeOf`, `ParentOf` and `UsageOf`, `FolderPath.Parent`, the JSON data objects (`VantageConfig`, `EditorSetting`), `ListSelection`'s selection and `TableTitle.Selected`, the `DiagnosticEvent` optionals, and whatever the sibling changes record as kept in their designs.
- **A second fact keeps the allow-list honest:** every entry still names a member that exists and is still nullable, so fixing one forces its entry out.
- **`CLAUDE.md`:** one sentence in the code rules: Vantage's own models and results never use null for meaning (absence is a named case or a null object); null is met only on the line that receives it from outside (JSON, Terminal.Gui, System.CommandLine, the base class library, and the nullable ports `NullFreeModelsShould` allows); `!` is never used.
- `skip_specs: true`: a test and a document; no behaviour changes. No `Version` raise, because nothing under `src\` changes, unless the fact finds a nullable no sibling handled (see design.md).

Out of scope: checking `Infrastructure` and `Tui` mechanically (adapters and Terminal.Gui code are where outside nulls arrive, so the rule there is the `CLAUDE.md` sentence and review); detecting `!` by scanning source text; removing guard clauses (the owner kept them); and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: an architecture test and a documentation rule, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Tests:** new `test\Source191.Vantage.Core.Tests\Architecture\NullFreeModelsShould.cs`.
- **Docs:** `CLAUDE.md` (one sentence in the code rules; the layering sentence names the new fact beside `LayeringShould`, `TextValuesShould` and `DiagnosticEventsShould`).

## Order

Change 10, the last of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (this one, no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
