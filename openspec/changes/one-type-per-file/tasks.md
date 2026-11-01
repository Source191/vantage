# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the test is named before the code. Groups 1 and 2 run now on `v-one`; group 3 onwards runs only after `v-locale-safety` and `v-null-free-follow-ups` have merged, on `v-one` rebased onto that `main`. Nothing is pushed until group 4 is green. Verify with `dotnet build -c Debug` (warnings are errors) and `dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build -- --filter-class "*OneTypePerFileShould"` unless a task says otherwise.

## 1. The fact, red

- [x] 1.1 Add `Microsoft.CodeAnalysis.CSharp` (newest stable) to `test\Source191.Vantage.Infrastructure.Tests` and record why in that project's `README.md` (no `.csproj` comment). Verify the build passes.
- [x] 1.2 `OneTypePerFileShould.HoldOneTypePerFile`: parses every `.cs` file under `src`, `test` and `tools` (not `bin` or `obj`) from `RepositoryFolder.Root()` with `LanguageVersion.Preview`, takes the class, record, struct, interface, enum and delegate declarations whose parent is the compilation unit or a namespace, and fails listing each file (relative path) that holds more than one, with its type names. Verify it fails red for the right reason: the message lists `src\Source191.Vantage.Core\Application\Views\SessionsView.cs` with 28 types, and 87 files in all, and does not list a file whose only extra types are nested (`LaunchProblem`'s cases). Commit red. (Measured at main c2b6d0f9: 87 files, 299 types, 212 beyond the first; SessionsView.cs holds 27.)
- [x] 1.3 `OneTypePerFileShould.NameEachFileAfterItsType`: a file holding one non-partial type is named `<Name>.cs` without arity; a partial type's file starts with `<Name>`; a file of top-level statements passes. Verify it fails red listing exactly the four mismatched files in proposal.md (`Outcomes.cs`, `PullRequestEffects.cs`, `IdentifierJsonConverters.cs`, `ScriptedScreens.cs`) and not the `WordingOf*` partials or `Program.cs`. Commit red.

## 2. The split script

- [x] 2.1 Write `openspec\changes\one-type-per-file\split-types.cs` as design.md describes (file-based app over Roslyn; keeps the type named after its file, writes each other type with its using directives, namespace line and full leading trivia to `<Name>.cs` beside it, deletes a file left with no type of its own, keeps the origin's line endings without a BOM, refuses and lists clashes before writing, `git mv`s the four renames, stops on a preprocessor directive inside a moved range). Add a `--dry-run` that prints the plan and writes nothing. Verify `dotnet run openspec\changes\one-type-per-file\split-types.cs -- . --dry-run` lists the same 87 files and four renames as the fact and no clashes, and that a real run on a scratch copy of the repository (in the job's temporary folder, never the working tree) followed by `dotnet format style --diagnostics IDE0005 --severity warn` and `dotnet build -c Debug` succeeds with `OneTypePerFileShould` green there. Commit.

## 3. The split, after the branches merge

- [ ] 3.1 Rebase `v-one` onto `main` once `v-locale-safety` and `v-null-free-follow-ups` have merged; rerun the fact and update the counts in proposal.md if they moved. Verify the fact still fails red, and only for files the script plans to split.
- [ ] 3.2 Run the script for real, then `dotnet format style --diagnostics IDE0005 --severity warn` and `dotnet format whitespace`. Verify `dotnet build -c Debug` passes and both facts are green; check with `git diff -M --stat main` that every moved type kept its summary and attributes (spot-check `[JsonConverter]`, `[Flags]` and `[Fact]` bearers).
- [ ] 3.3 Commit the split per area as design.md describes (Core `Domain`, `Ports`, `Application`; `Infrastructure`; `Tui`; tests and tools), each commit building on its own. Verify `dotnet build -c Debug` at each commit (`git rebase --exec "dotnet build -c Debug" main` or by hand).

## 4. The rule in words, the version and the whole build

- [ ] 4.1 `CLAUDE.md`'s code rules gain the rule (one top-level type per file, named after it without arity; nested types stay nested; a partial type's files start with its name) and name `OneTypePerFileShould` beside `LayeringShould`, `TextValuesShould`, `DiagnosticEventsShould` and `NullFreeModelsShould`; the sentence about a handler's outcome record in its own file stays. Verify by reading it against design.md and checking no em dash was introduced.
- [ ] 4.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview above `main`. Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`.
- [ ] 4.3 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate one-type-per-file --strict` all pass. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- Push and open the pull request only when the owner asks.
