# Design

## Context

See proposal.md for why. Every file is already file-scoped (IDE0161 at warning, none block-scoped), files have no BOM and `.gitattributes` sets `* text=auto`. Warnings are errors, `GenerateDocumentationFile` is on (so a public type that loses its `/// <summary>` breaks the build with CS1591) and IDE0005 (unused using) is a warning, so a new file that copies its origin's whole using block fails the build. Two branches with unmerged code commits change files this change splits: `v-locale-safety` (14 commits; `IDetailSource.cs`, `ChildCommand.cs`, `WordingOfConfig.cs`, `WordingOfDetail.cs`, `EditorLocator.cs`) and `v-null-free-follow-ups` (24 commits, 57 files). `Infrastructure.Tests` already finds the repository through `RepositoryFolder.Root()` (the folder holding `global.json`) and is the suite that reads real files; Core.Tests is the in-memory suite and stays that way.

## Goals / Non-Goals

**Goals:**
- A fact that states the rule exactly and lists every offender, so it is the inventory as well as the guard.
- A split that is mechanical and repeatable, so it can be rerun on whatever `main` is when the other branches have merged.

**Non-Goals:**
- Tidying the code the split moves: member order, comments and formatting are kept byte for byte.
- Checking files outside `src`, `test` and `tools` (the openspec folder's script included).

## Decisions

**The fact parses with Roslyn rather than matching text.** `CSharpSyntaxTree.ParseText` with `LanguageVersion.Preview`, then the members whose parent is the `CompilationUnitSyntax` or a `BaseNamespaceDeclarationSyntax`: `BaseTypeDeclarationSyntax` (class, record, struct, interface, enum) and `DelegateDeclarationSyntax`. That ignores nested types, comments, strings and raw string literals for free, where a column-0 regex would need care and two regexes during the review already disagreed. The fact does not assert the tree has no diagnostics: an older Roslyn than the SDK's still produces the declaration nodes. Alternatives: StyleCop's SA1402 and SA1649 (a new analyser package across every project for two rules, and its own file-name rule differs for partials); reflection (types know their assembly, not their file, without reading PDBs). The package is `Microsoft.CodeAnalysis.CSharp` at the newest stable version, in `Infrastructure.Tests` only.

**The file name is the identifier without arity; partials are judged by their prefix.** A non-partial type must sit in `<Name>.cs`. A partial type's file must start with `<Name>` (`Wording.cs`, `WordingOfConsole.cs`), and still holds only that type. The mismatched single-type files (three once `resume-without-dialogue` deleted `Outcomes.cs`) are renamed rather than excused, so the fact has no allow-list.

**The fact reports, it does not count.** It collects every offending file with its type names, relative to the repository root, and fails with the whole list in the message (`ShouldBeEmpty` over the strings), so one run shows everything left to do. Two facts: `HoldOneTypePerFile` and `NameEachFileAfterItsType`.

**The split is a file-based app over the same syntax rules.** `openspec\changes\one-type-per-file\split-types.cs`, run with `dotnet run split-types.cs -- <repository root>` (a `#:package Microsoft.CodeAnalysis.CSharp` directive, .NET 10's file-based apps). For each file with more than one top-level type:
- The type named after the file stays in it; if none is, the file is deleted after its types are written out.
- Each other type is written to `<Name>.cs` beside it: the origin's using directives (and any `#nullable` or `#pragma` above the namespace), a blank line, the namespace line, a blank line, then the member's full text with its leading trivia (the documentation comment and attributes live there), trimmed of the blank lines between it and the previous member.
- Line endings follow the origin (LF in the working tree under `text=auto`, CRLF on a CRLF checkout), no BOM, and a final newline only where the origin had one (the repository's files end without one, `insert_final_newline = false`).
- It refuses to overwrite a file that exists, and stops with the list of such clashes before writing anything.
- It also applies the renames with `git mv`, so history follows them.
- Partial types: moved out like any other type when their file is named for another one, which never happens today.

Its selection logic is the fact's, copied rather than shared, because a throwaway script should not be a project the solution builds. The script is kept in the change folder so the archive records how the split was made.

**Unused usings are removed by the formatter, not by the script.** After the split, `dotnet format style --diagnostics IDE0005 --severity warn` removes the using directives each new file does not need, then `dotnet format whitespace` and the build confirm the result. Working out per file which usings a type needs would mean a semantic model; the formatter already has one.

**Sequencing: prepare now, split after the branches merge.** The fact (red), its package and the script are committed now. The split runs on a branch from `main` once `v-locale-safety` and `v-null-free-follow-ups` have merged, so neither has to resolve a split by hand; the script makes the rerun cheap if anything else lands first. The red fact is not pushed on its own, because CI would fail; the pull request carries red fact, split and green together. In the event the split ran at the top of the stack of open changes (on `v-harden`, 1 November 2026): 87 files, 221 types moved, ten files deleted, three renames, and the counts of `///` lines (2509), attribute lines (2356) and `[Fact]`/`[Theory]` lines (1580) the same before and after.

**Commits follow the layers.** After the red fact: one commit per project area (Core's `Domain`, `Ports`, `Application`; `Infrastructure`; `Tui`; the tests and tools), each building on its own, then the version, the docs and the archive. Each commit is green for the projects it touches; the fact goes green with the last split commit.

## Risks / Trade-offs

- [A type loses its documentation comment or an attribute in the move] → the member's full leading trivia moves with it; `Directory.Build.props` suppresses CS1591, so the build does not catch a lost summary: the check is that the counts of `///` lines, attribute lines and `[Fact]`/`[Theory]` lines under `src`, `test` and `tools` are the same before and after the split (they were, 2429, 2037 and 1510, on a scratch run at main c2b6d0f9), plus `git diff -M --stat` and a spot check of the `[JsonConverter]` bearers.
- [A `#pragma` or `#if` region spans types] → none exists today above or between top-level types (checked: no `#if` or `#region` at the start of a line); the script stops on a directive inside the moved range rather than guessing.
- [`dotnet format` removes a using that a `cref` in a comment needs] → `GenerateDocumentationFile` makes the analyser count `cref` uses, and an unresolved `cref` (CS1574) fails the build.
- [Other branches opened after the split meet the new files] → the split is one pull request merged quickly; a branch that edits a moved type rebases onto the new file by hand, and git's rename detection follows the files that keep their name.
- [The fact slows the adapter suite] → parsing about 650 files takes well under a second.
- [A new file name clashes with an existing file] → the script lists clashes before writing anything; none are expected, since each type name is unique in its namespace.

## Migration Plan

Nothing to migrate: no state file, key or command line changes. Rollback is reverting the pull request.
