# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step and the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The rule in Core

- [x] 1.1 `NullFreeModelsShould.ExemptTheHouseShapes`: move `TryFind(int, [NotNullWhen(true)] out string?)` from `IHouseShapes` to a probe of its own and assert it is reported as `TryFind(found)`. Red, because `IsOutUntilParsed` still accepts `NotNullWhen`. Then drop the `NotNullWhen` branch from `IsOutUntilParsed`: the fact goes green and `HoldNoNullableMember` goes red, reporting `ClaudeVersion.TryParse`, `ConfigView.TryPathOf` and `TabKeys.TryFor`. Verify both outcomes and that `IHouseShapes` still reports nothing.
  Verified: the probe `INotNullWhenTry` failed red with `["TryFind(found)"]` unreported, then passed; `HoldNoNullableMember` went red with `ConfigView.TryPathOf(path)`, `TabKeys.TryFor(effect)`, `ClaudeVersion.TryParse(version)` and also `ClaudeVersion.TryParse(text)` (no longer a house-shape TryParse once its `out` is not exempt); `IHouseShapes` reports nothing.
- [x] 1.2 Green for `HoldNoNullableMember`: `ClaudeVersion.TryParse`, `ConfigView.TryPathOf` and `TabKeys.TryFor` declare `[MaybeNullWhen(false)] out T` with `T` non-nullable, bodies and callers unchanged. Verify the Core suite passes, including `ClaudeVersionShould`, `TabKeysShould` and the Config view facts with their expected answers unchanged, and `KeepNoStaleEntry` still passes.
  Verified: build 0 warnings; Core suite 1956 passed, 0 failed, no test or caller changed.

## 2. Tui and the comment

- [x] 2.1 `KeyMapping.TryFrom` declares `[MaybeNullWhen(false)] out Key mapped`; `TableBody.OnKey` keeps `out Key? mapped`. No test exists for the attribute in Tui (design.md), so verify the solution builds with no new warning and `git grep NotNullWhen -- src` answers nothing.
  Verified: build 0 warnings, 0 errors; `git grep NotNullWhen -- src` finds nothing.
- [ ] 2.2 `ITextValue<TSelf>.TryParse` gains `<param name="value">` saying the value is null exactly when the method returns false. Verify the build passes and the comment reads correctly in the interface.

## 3. The rule in words and the version

- [ ] 3.1 `CLAUDE.md`: the house-shape sentence in "Values, sessions and null" names a Try method's `out` marked `[MaybeNullWhen(false)]` only, and "C# idioms" gains one line: a Try method's `out` parameter is a non-nullable `T` marked `[MaybeNullWhen(false)]`, never `[NotNullWhen(true)] out T?`, as `ITextValue` and the base class library's `IParsable` declare it, and callers declare `out T? x`. Verify by reading both against design.md and checking no em dash was introduced.
- [ ] 3.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` by one preview step above main at the time. Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 3.3 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate standardise-maybe-null-when --strict` all pass. Record the counts here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
