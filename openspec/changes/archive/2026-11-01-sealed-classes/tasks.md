# Tasks

Each task is one red-green-refactor cycle; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The probe

- [x] 1.1 `SealedTypesShould.FindOnlyTheOpenLeaf`: over nested probe types in the test file (an open class, a sealed class, a static class, an abstract record with a sealed case, an open base with a sealed subclass), the finder reports only the open class. Red, then the finder green. Verify the fact passes and fails when the sealed class's `sealed` is removed by hand.

## 2. The rule over the solution

- [x] 2.1 `SealedTypesShould.LeaveNoClassOpen`: over the Core, Infrastructure, Tui and TestDoubles assemblies the finder reports nothing. Seal anything it reports (a change under `src\` raises `Version`). Verify the fact passes.
- [x] 2.2 `.editorconfig` gains `dotnet_code_quality.CA1852.ignore_internalsvisibleto = true`. Verify `dotnet build -c Debug` stays clean, and that an internal class left open in Infrastructure fails it with CA1852 (tried by hand, then reverted).

## 3. The rule in words, and the whole build

- [x] 3.1 `CLAUDE.md`'s C# idioms gain the sealing bullet citing CA1852 and `SealedTypesShould`, and the layering sentence names `SealedTypesShould` beside the other facts. Verify by reading it against proposal.md and checking no em dash was introduced.
- [x] 3.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate sealed-classes --strict` all pass; `git diff --stat origin/main` shows whether anything under `src\` changed (and so whether `Version` was raised).
  - 1 November 2026, after fast-forwarding to main at 3bb04844 (PR #153): build clean, solution 2199 total, 2191 passed, 8 skipped, format clean, validate strict valid. Nothing was open, so nothing under `src\` changed and `Version` stays. Hand checks: unsealing `FakeTranscripts` fails `LeaveNoClassOpen` naming it; unsealing `NuGetPackageVersions.VersionIndex` fails the build with CA1852 (and Sonar's S3260); both reverted.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
