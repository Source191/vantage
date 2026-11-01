# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, or the Infrastructure tests where the task says so.

## 1. Remote shapes

- [ ] 1.1 `RemoteRepositoryShould.ParseGitHubHttpsAndSshRemotes` gains `[InlineData("https://northwind@github.com/northwind/Northwind.Orders.git")]`, expecting `GitHubRepository("northwind", "Northwind.Orders")`. Red (today `TryParse` answers false), then green: the GitHub pattern at `src\Source191.Vantage.Core\Domain\RemoteRepository.cs:54` takes `(?:[^@/\s]+@)?` before `github\.com/` in its https alternative. Verify the theory and `AnswerFalseForOtherHostsBlankTextAndUnsafeAzureNames` pass.

## 2. Versions

- [ ] 2.1 `ReleaseVersionShould.AnswerFalseForTextThatIsNotAVersion` gains `[InlineData("01.0.0")]`, `[InlineData("1.0.0-01")]` and `[InlineData("1.0.0-a..b")]`; `ReadAVersion` gains `("1.0.0-0", 1, 0, 0, "0")` and `("1.0.0-01a", 1, 0, 0, "01a")`. Red, then green: `TryParse` in `ReleaseVersion.cs` checks each `.`-separated prerelease identifier (non-empty; a leading zero refused when all digits) and `Number` refuses a leading zero on a multi-digit number. Refactor: the identifier rule is one private method both `Number` and the prerelease check call. Verify `CompareBySemanticVersionPrecedence` still passes with its `1.0.0-alpha` over `1.0.0-1` case.
- [ ] 2.2 `ParsingShould.HideTheConstructorBehindEveryTryParse`: every type `FindEveryParser` lists has `GetConstructors()` empty. Red (it names `ReleaseVersion`), then green: `ReleaseVersion` becomes a non-positional sealed record with get-only `Major`, `Minor`, `Patch`, `Prerelease`, a private constructor and `From(string text)` (TryParse or `ArgumentException`), with `ReleaseVersionShould.BuildFromTrustedTextAndThrowForTheRest`; `ClaudeCodeSupport.cs:11` reads `ReleaseVersion.From("2.1.309")`. Every `new ReleaseVersion(…)` in `ReleaseVersionShould`, `NewestReleaseShould`, `UpdateCheckRuleShould` and `ClaudeCodeSupportShould` goes through `ReleaseVersions.Version`, and `JsonFileStoreShould.cs:491,520` through `ReleaseVersion.From`; `ReadAVersion` asserts the four properties. Refactor: `TextValuesShould.HaveNoPublicConstructor` is removed, its fact now in `ParsingShould`. Verify the Core and Infrastructure test projects pass and `DocumentedMinimumShould` still reads 2.1.309.

## 3. The WHERE label

- [ ] 3.1 `WhereLabelShould.NeverAnswerMoreThanTheWidth`, a theory over widths 0 to 4 for `Inside("Northwind.Orders", "fix-123")`, `Inside("Northwind.Orders", "")` and `Outside(@"~\scratch\notes")`, asserting `Fit(width).Length <= width`, and `KeepTheSeparatorForAPlaceOnly`, asserting `Inside("Northwind.Orders", "").Fit(width)` holds no `›` at those widths and `Inside(…, "fix-123").Fit(5)` is `… › …`. Red (today `Fit(2)` with an empty place is `… › ` and `Fit(0)` throws or answers `…`), then green as the design's Fit decision: `LeastElided` and `LeastPlaced` named, the empty-place and small-width paths eliding the repository alone, `ElideMiddle` cutting below `LeastElided`, `CutEnd` empty at 0. Verify `ElideTheRepositoryFirst` and `CutTheLabelOutsideARepositoryAtItsEnd` still pass.

## 4. The batch-file rule

- [ ] 4.1 `BatchFileArgumentShould.RefuseAnythingCmdExeReadsSpecially` gains `[InlineData("abc\n")]` and `[InlineData("abc\r\n")]`. Red (the `\n` case passes `IsSafe` today), then green: `BatchFileArgument.cs` holds a `static readonly SearchValues<char>` of the allowed characters and `IsSafe` is `text.Length > 0 && !text.AsSpan().ContainsAnyExcept(…)`; the class is no longer `partial` and loses its `System.Text.RegularExpressions` using. Verify both theories and `RemoteRepositoryShould` pass, and `.claude\rules\security.md` still describes the rule correctly (it names the characters, not the regex).

## 5. The Claude CLI adapter

- [ ] 5.1 `ClaudeCliShould.RefuseAStartAnswerWhoseIdIsNotEightHexCharacters`: standard output `backgrounded · 81cdb0f9x · cc-spike` answers `StartResult.Failed`; and `AnswerNotReadWhenTheVersionWordIsNotAVersion`: output `2.1.310abc (Claude Code)` answers `NotRead`. Red (the regexes accept both), then green: `InterpretStart` splits on whitespace, skips to `backgrounded`, skips two words and hands the next to `JobId.TryParse`; `VersionAsync` takes the first word and answers `Printed` only when `ClaudeVersion.TryParse` reads it. `LeadingVersion`, `BackgroundedLine`, the comment about the two rules agreeing and `partial` go. Verify with `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` that every existing `ClaudeCliShould` fact (`StartABackgroundSessionNamedInTheChosenDirectory`, `ReadTheVersionFromVersionOutput`, `AnswerNotReadWhenNoVersionIsPrinted`, the resume facts) passes unchanged.

## 6. Version and whole-solution check

- [ ] 6.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above the one on `main` when implemented; patch, minor or major as CONTRIBUTING's Versions says (patch here: fixes, no new key, and the one new remote shape keeps every existing one). Verify `Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 6.2 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate domain-parsing-edges --strict` all pass; `Regex` is then referenced under `src` only in `RemoteRepository.cs` and `WorktreeSlug.cs` (verify with a search).

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\pull-requests` describes the new behaviour.
