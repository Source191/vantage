# Proposal

## Why

A review on 1 November 2026 found 87 of the 528 C# files under `src`, `test` and `tools` declaring more than one top-level type, about 212 types beyond the first. The worst is `Application\Views\SessionsView.cs` with 28 types, then `Diagnostics\DiagnosticEvent.cs` with 13 and `WorktreesView.cs` with 12, and eleven handler files break the existing rule in `CLAUDE.md` that a handler's outcome record is declared in its own file. A type that hides in another type's file is hard to find by name, and nothing in the build stops more of them: no analyser checks it and no architecture fact covers it. The owner has asked for one type per file, enforced.

## What Changes

- **The rule:** every C# file under `src`, `test` and `tools` declares exactly one top-level type (class, record, record struct, struct, interface, enum or delegate), and the file is named after it without its generic arity (`TableBody<TState, TRow>` in `TableBody.cs`). Nested types stay nested: a closed hierarchy's cases (`LaunchProblem`'s, `SettingsProblem`'s) keep living inside their base. A `partial` type may be split across files named for its parts, as the `Wording` partials are (`WordingOfConsole.cs`), but each such file still holds that one type only. `Program.cs` and other files of top-level statements declare no type and pass.
- **The fact:** `OneTypePerFileShould` in `test\Source191.Vantage.Infrastructure.Tests` parses every `.cs` file under the three folders (skipping `bin` and `obj`) with Roslyn's syntax API and fails, naming each file and the types it holds, for a second top-level type or a file name that does not match. It is written red first, against today's tree.
- **The split:** about 212 types move out of 87 files into new files in the same folder and namespace, each keeping its XML documentation comment and attributes. A file whose own type has gone (`FolderEffects.cs`, `Identifiers.cs`, `StatePorts.cs`, `Detail.cs`, `Screens.cs` and the like) is deleted once its types are out. Four single-type files are renamed after their type: `Actions\Outcomes.cs` to `ResumePlan.cs`, `Views\PullRequestEffects.cs` to `OpenPullRequest.cs`, `State\IdentifierJsonConverters.cs` to `TabOwnerJsonConverter.cs`, and `TestDoubles\ScriptedScreens.cs` to `ScriptedConsoleScreen.cs`. The three test classes in `IdentifiersShould.cs` split the same way.
- **A re-runnable split script** (a .NET file-based app over Roslyn, kept in this change's folder) does the moves, so the split can be run again on a fresh `main` rather than merged by hand.
- **`CLAUDE.md`:** the code rules gain the one-type-per-file sentence and name `OneTypePerFileShould` beside the other architecture facts; `Infrastructure.Tests`' README records why it references `Microsoft.CodeAnalysis.CSharp`.
- `skip_specs: true`: no behaviour changes. The tool's version rises to the next preview in the pull request, because files under `src\` change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor, a test and a documentation rule, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** 87 files under `src`, `test` and `tools` split or renamed; no namespace, folder, member or behaviour changes, so every reflection fact (`LayeringShould`, `NullFreeModelsShould`, `TextValuesShould`, `DiagnosticEventsShould`, `EffectRoutingShould`) is unaffected.
- **Tests:** new `test\Source191.Vantage.Infrastructure.Tests\OneTypePerFileShould.cs`; a new package reference, `Microsoft.CodeAnalysis.CSharp`, in that project only.
- **Docs:** `CLAUDE.md`, `test\Source191.Vantage.Infrastructure.Tests\README.md`.
- **Projects:** `src\Source191.Vantage\Source191.Vantage.csproj` for the version.
- **Other branches:** `v-locale-safety` and `v-null-free-follow-ups` change files the split carves up (`IDetailSource.cs`, `ChildCommand.cs`, the `Wording` partials among them), so the split runs only after both have merged; see design.md.
- **Users:** none.

## Out of scope

- Moving types between folders or namespaces, or regrouping folders: every type stays where its file was.
- Un-nesting nested types, or nesting top-level ones to avoid a file.
- Renaming the `Wording` partial files, or any type.
- Adding StyleCop or another analyser package for the rule.
- Multiple Claude accounts, which is a later change.
