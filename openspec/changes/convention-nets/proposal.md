# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, section 5) listed every CLAUDE.md rule against what enforces it today. Most hold by analyser or reflection fact; nine hold only by grep and habit: `TimeProvider` over `DateTime.Now`, no blocking waits, no bare `Volatile`, British English, pure reducers, a test class per handler, every `DiagnosticEvent` rendering, a `KeyHelp` entry answered by its reducer, and `!` never used. Three "name both cases, throw from the discard" slips (V-03, I-12, A-07) and one discard that throws the wrong exception (A-04) show that the convention half of the switch rule is held by review alone, and `LayeringShould` still names three namespaces that no longer exist and has no rule between `Ports` and `Application` (T-17). Each net is cheap now and dearer after the next change that drifts.

## What Changes

- **Banned APIs in `src`:** `Microsoft.CodeAnalysis.BannedApiAnalyzers` 5.6.0 with a `src\BannedSymbols.txt`, both added by a new `src\Directory.Build.props` that first imports the root `Directory.Build.props` (`GetPathOfFileAbove`), so the root's analysers and warnings-as-errors still apply and the test projects, which use `TimeProvider.System` and `DateTimeOffset.UtcNow` legitimately, are untouched. Banned: `DateTime.Now`, `UtcNow`, `Today`; `DateTimeOffset.Now`, `UtcNow`, `ToOffset(TimeSpan)`; `Thread.Sleep` (both overloads); `Task.Wait` (six overloads); `Task<T>.Result`; `TaskAwaiter.GetResult` (both); `System.Threading.Volatile`; `Process.Start` (the five `string` overloads); `string.ToLower()` and `ToUpper()`. Two known uses are exempted in place with `#pragma warning disable RS0030` and a reason: `src\Source191.Vantage.Infrastructure\Terminal\ConsoleRecovery.cs:52` and `src\Source191.Vantage.Tui\LaunchWizardDialog.cs:27`. Not banned: `Regex` (two regexes stay by the review's judgement) and `ProcessStartInfo.UseShellExecute` (design.md says why).
- **Layering facts:** `LayeringShould` gains `KeepPortsFreeOfApplication` and `KeepSystemCommandLineInsideCommandLine` (nothing but `Application\CommandLine` depends on `System.CommandLine`), and drops the dead names `Screens`, `Actions`, `Launch` and `Screens.Tui`.
- **New reflection facts in `test\Source191.Vantage.Core.Tests\Architecture`:** `ReducersShould` (every `*View` in `Application.Views` is a static class whose fields are all `static readonly`); `HandlersShould` (every `IEffectHandler<>` implementation has a `<Name>Should` class in Core.Tests, which is red today for `CopyProblemReportHandler`, `OpenProblemReportHandler` and `DeleteBranchHandler`).
- **`DiagnosticLineShould`:** `RenderEveryEvent` constructs every `DiagnosticEvent` with placeholders and renders it; `RefuseAnEventItDoesNotKnow` expects `UnreachableException`, which `DiagnosticLine.cs:45` throws instead of `ArgumentOutOfRangeException`.
- **Key parity:** `KeyHelpParityShould` presses every key in `SessionsKeys`, `WorktreesKeys` and `ConfigKeys` on a loaded state with a selected row and expects an effect, a message or a changed state; `TabKeyHelp`'s keys must answer `TabKeys.TryFor` or the new `Application\Views\ScrollKeys.TryFor`, which takes over `TableBody.ScrollStep` (`<` and `>`), so the scroll keys become a tested rule beside `TabKeys`.
- **Source-text facts in `test\Source191.Vantage.Infrastructure.Tests\SourceConventionsShould`** (beside `OneTypePerFileShould`, over Roslyn syntax trees): no `!` (null-forgiving) expression under `src`; the `volatile` keyword only in `Application\SharedSnapshot.cs`; `catch (ArgumentException` only in `ClaudeSettings.cs` and `ProcessRegistry.cs`, the two files CLAUDE.md names; no word from a short American-spelling list in the string literals of `Application\Wording*.cs` and `Application\CommandLine.cs`.
- **Discard arms that guessed now throw `UnreachableException`:** `SessionsView.cs:139` and `:208`, `WorktreesView.cs:104`, `PullRequestCatalogue.cs:39`, `SetupFactsReader.cs:54` (naming `Missing` and `Unset`), `TabOwnerJsonConverter.cs:25` (naming `ForVantage`); `RowDecision.cs:9` loses its "still loading" sentence.
- **Guard rule (owner to confirm):** CA1062 at `warning` everywhere is proposed as the mechanical option; the alternative (guard nothing inside the solution, remove the eight `?? throw` initialisers) is set out in design.md. Nothing is enabled until the owner chooses.
- **Docs:** `CLAUDE.md` gains one clause per new net where it states each rule; `CONTRIBUTING.md`'s comment rule and `.claude\rules\console-screen.md`'s scroll keys follow; `test\Source191.Vantage.Infrastructure.Tests\README.md` names the second Roslyn reader.
- **Version:** the next preview above `main` (0.20.0-preview.9 if nothing lands first); patch-level, no behaviour changes.
- `skip_specs: true`: no key, screen, message, state file or command line behaves differently.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: analysers, reflection facts, three `UnreachableException` arms and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Build:** new `src\Directory.Build.props` and `src\BannedSymbols.txt`; one analyser package in the four `src` projects only. `Assert-VersionBumped.ps1` already counts any file under `src\`.
- **Code:** the six discard arms above and `DiagnosticLine.cs:45`; `TableBody.cs:177, 389-393` call the new `Application\Views\ScrollKeys`; two `#pragma` pairs. No port, adapter contract, effect or wording changes.
- **Tests:** `LayeringShould` edited; new `ReducersShould`, `HandlersShould`, `KeyHelpParityShould`, `ScrollKeysShould`, `SourceConventionsShould`, `DeleteBranchHandlerShould`; `ProblemReportHandlersShould` split into `OpenProblemReportHandlerShould` and `CopyProblemReportHandlerShould`; `DiagnosticLineShould` gains two facts.
- **Other open changes:** `sessions-row-action` rewrites the Sessions row arms and `table-body-collaborators` splits `TableBody`; whichever lands second rebases, and the discard rule and `ScrollKeys` hold in either shape. `wording-one-home` moves words into `Wording*.cs`, where `SpellBritish` then covers them. `fix-cli-contracts` gives `ProcessRunner` a kill on cancel, which touches nothing banned.
- **Users:** none.

## Out of scope

- Banning `Regex`, `ProcessStartInfo.UseShellExecute` or a bare program name in `ProcessRequest` (I-03): each needs a decision the review left open.
- Words still outside `Wording` (A-06, V-07) and a no-sentence-literal fact over `Views` and `Actions`: `wording-one-home`.
- The `"NAME"`/`"AGE"` column coupling (V-05): `columns-as-data`. `KeyHelp` carrying a `Key` for the key bar's click (V-06 a): a later change, once the parity fact shows which keys are shared.
- Test hygiene (T-04 to T-19 other than T-17), the `tools\HeadlessTerminal` assembly in `SealedTypesShould`, and a `private_fields_camel_case` naming rule: `test-hygiene`.
- Multiple Claude accounts, which is a later change.
