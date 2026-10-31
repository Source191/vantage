# Design

## Context

See proposal.md for why. The Core project builds with nullable reference types on and warnings as errors, so the compiler's nullable annotations are in the assembly's metadata, and .NET's `NullabilityInfoContext` reads them back for a property, field, parameter or return value, including generic arguments. `test\Source191.Vantage.Core.Tests\Architecture` already holds reflection facts in the same style: `TextValuesShould` finds its types by namespace and shape, `DiagnosticEventsShould` restricts member types with an allow-list held as data.

## Goals / Non-Goals

**Goals:**
- A failing fact the moment a nullable appears in a public member of `Domain`, `Ports` or `Application`, naming the type and member.
- An allow-list that can only shrink: each kept nullable has a written reason, and an entry whose member is no longer nullable fails.

**Non-Goals:**
- Private members, locals and lambdas: `FirstOrDefault` temporaries and private helpers are a review matter; checking them needs source analysis, not reflection.
- `Infrastructure`, `Tui` and `Program.cs`: Core.Tests does not reference them, and they are where outside nulls arrive.

## Decisions

**What counts as a member.** Public instance and static properties; parameters and return values of public methods and constructors; on every public type (nested ones included) whose namespace is `Source191.Vantage.Domain`, `.Ports` or `.Application` (and their sub-namespaces, such as `Application.Views`). A member is nullable when `NullabilityInfoContext` reports `ReadState` (properties, returns) or `WriteState` (parameters) as `Nullable` for it or for any generic argument, recursively, so `Task<string?>` and `IReadOnlyList<string?>` count. Value-type `Nullable<T>` counts too: `int?` is null in another spelling.

**What is exempt by rule, not by list.** Compiler-generated members (`EqualityContract`, `<Clone>$`, `PrintMembers`, `Deconstruct`), `Equals`, `CompareTo`, the equality operators, and a static `TryParse` whose first parameter is `string?` and whose `out` parameter carries `[MaybeNullWhen(false)]` or `[NotNullWhen(true)]`. These are the language's and the house's own shapes, and listing each would bury the real entries.

**The allow-list is data in the fact.** `private static readonly AllowedNullable[] Kept = [...]`, where `AllowedNullable(Type Type, string Member, string Reason)` and `Member` names the property, method or `Method(parameter)`. The reason is one line ("git answers nothing for a repository with no commits", "the JSON property may be missing"). Starting entries: the nullables the sibling changes record as kept in their designs, which as surveyed are the six `IGit` queries, `IProcessRegistry.StartTimeOf`, `ParentOf` and `UsageOf`, `FolderPath.Parent`, `VantageConfig` and `EditorSetting`, `ListSelection`'s `SelectedKey` and `Selected`, `TableTitle.Selected`, `WizardState.SelectedIndex` (bound directly to Terminal.Gui's `ListView.SelectedItem`, kept by null-free-sessions), and the `DiagnosticEvent` optionals (`Started.ClaudeCode`, `Failed.What`, `TerminalGuiReported.Failure`). The implementing task writes the list from what the fact actually reports on main, not from this paragraph.

**A second fact, `KeepNoStaleEntry`.** For every allow-list entry, the member exists and is still nullable; otherwise it fails naming the entry, so whoever removes a null also removes its excuse.

**When the fact finds something no sibling handled.** It is reported to the owner, not fixed in this change and not silently allowed: the owner chooses between allowing it with a reason and a small follow-up change. If a fix is chosen and it touches `src\`, it goes in its own change with a version raise, keeping this one test-only.

**Why not a source scan for `!`.** A regular expression cannot tell the null-forgiving `!` from negation and generic constraints without parsing C#, and a Roslyn analyser is a project of its own for a handful of sites that null-suppressions removes. The `CLAUDE.md` sentence and review cover it.

## Risks / Trade-offs

- [`NullabilityInfoContext` reads an unannotated or generic member as `Unknown` rather than `NotNull`] → the fact treats only `Nullable` as a failure, and a first task checks it against one known nullable and one known non-nullable member of each kind (property, parameter, return, generic argument, value type) so a blind spot shows as a failing fact.
- [The allow-list grows instead of shrinking] → each entry needs a reason in the code review, and `KeepNoStaleEntry` stops it holding dead entries; the `CLAUDE.md` sentence names the fact, so a new nullable has to be argued for there.
- [Compiler-generated members vary between language versions] → they are exempt by name and by `CompilerGeneratedAttribute`, and the probe task covers a record.
