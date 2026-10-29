# Tasks

## 1. Domain rules

- [x] 1.1 `BatchFileArgumentShould` (new, Core.Tests\Domain): letters, digits, space, dot, underscore, hyphen and slash are safe; `&`, `|`, `%`, `"`, `<`, `>`, `^`, `!` and an empty text are not. Then `Domain\BatchFileArgument.IsSafe` and `RemoteRepository` using it in place of its own regex; `RemoteRepositoryShould` still passes.
- [x] 1.2 `ProcessIdentityShould` (new): a process started before or at the session's listed start is still the session's, one started six seconds after is not, an unknown start time is not. Then `Domain\ProcessIdentity.StillTheSession`.
- [ ] 1.3 `LaunchPlanShould.RefuseAnOpeningPromptThatStartsWithADash` (`- fix` and `--dangerously-skip-permissions` refused as `LaunchProblem.PromptLooksLikeAnOption`, `Fix - all` planned) and `HereCommandShould.RefuseADashLedPromptAsAFact`. Then the record and the check in `LaunchPlanBuilder`.
- [ ] 1.4 `SettingsShould.RefuseACustomEditorThatIsNotAFullPath` (`subl` refused as `SettingsProblem.ExecutableNotFullPath`, a batch file still refused as a batch file) and `EditorChoiceShould.ReadACustomEditorByBareNameAsVsCode`. Then the record and the check in `EditorChoice.Problem`.
- [ ] 1.5 `WorktreeHealthShould.BeElsewhereForAWorktreeOfARepositoryOutsideTheRoots` (unregistered, `.git` target exists, owner exists): kind `Elsewhere`, reason `RepositoryOutsideRoots` naming the owner; an owner that is gone stays broken. Then `OwnerRepositoryExists` on `WorktreeObservation`, the kind, the reason and the arm in `WorktreeHealth.From`; every switch over `WorktreeHealthKind` names the new member (`RemovalDecision`, `WorktreesScreenDefinition.RowColour`).
- [ ] 1.6 `RemovalDecisionShould.RefuseAnElsewhereWorktreeEvenWhenForced` (refused, reason `RemovalReason.RepositoryOutsideRoots`, cannot be forced). Then the arm in `RemovalDecision.For`.
- [ ] 1.7 `Ports\ClaudeLocation`: `Found(Path)`, `BatchFileOnly(Path)`, `Missing` (used by 3.2 and 5.3; built by that test).

## 2. Wording

- [ ] 2.1 `WordingShould`: the prompt problem ("An opening prompt cannot start with a dash: Claude Code would read it as options. Start it with a word."), the editor problem, the elsewhere health reason naming the repository, the removal reason, the `BranchNotGivenToAz` footer, `GitNotFound`, `ClaudeNotFound` and `ClaudeIsABatchFile(path)`; `WordingOfOutcomesShould`: the close-empty `Gone` list and `AdoptSessionOutcome.ProcessGone`. Then the words in `Wording`, `WordingOfPullRequests` and `WordingOfOutcomes`, and the `PullRequestGap` member. Verify with the two wording suites and the reflection facts.

## 3. Adapters

- [ ] 3.1 `ProgramLocatorShould` (new, Core.Tests\Application, over `FakeFolders`): finds `git.exe` in the first PATH folder that holds it, appends `.exe` to a bare name, answers null for a `gh.cmd` alone, and never answers a file in a folder that is not on the PATH. Then `Adapters\Processes\ProgramLocator` with a `ForCurrentUser` that reads the PATH.
- [ ] 3.2 `ClaudeExecutableShould` (new): `~\.local\bin\claude.exe` wins over the PATH, `claude.exe` on the PATH is found, `claude.cmd` alone is `BatchFileOnly` naming it, nothing is `Missing`. Then `ClaudeExecutable.Locate(folders, pathFolders, home)` answering `ClaudeLocation`, a parameterless `Locate()` for `Program.cs`; `ProcessRunnerIntegrationShould` and `Contract\ClaudeContract` take the `Found` path.
- [ ] 3.3 `GitCliShould.RunTheGivenExecutable`: the request's file name is the path the adapter was built with. Then `GitCli(processes, executable)`; every test and `Program.cs` pass one.
- [ ] 3.4 `GitHubPullRequestsShould.ReportCliMissingWithoutGh` (null, no process started) and the request's file name is the given path. Then `GitHubPullRequests(processes, string? gh)`.
- [ ] 3.5 `WindowsTerminalShould.ThrowNamingWtWhenItIsNotOnThePath` and the request's file name is the given path. Then `WindowsTerminal(processes, fragment, string? wt)`.
- [ ] 3.6 `DesktopShould.ThrowNamingPwshWhenItIsNotOnThePath` and `OpenAsync` runs the given path (fake runner). Then `Desktop(processes, string? pwsh)`.
- [ ] 3.7 `AzureDevOpsPullRequestsShould.NotStartAzForABranchItCannotBeGiven` (`feature/x&calc`: `Unavailable(BranchNotGivenToAz)`, nothing captured) and `KeepABranchWithASlash` still asks. Then the check in `ListAsync` through `BatchFileArgument.IsSafe`.
- [ ] 3.8 `GitHubPullRequestsShould.LeaveOutAPullRequestWhoseLinkIsNotHttps` (a `file:` url row is dropped, the https rows stay). Then the scheme check in `GitHubPullRequests.Row`; the Azure DevOps page is built by Vantage over https, so it needs no test.
- [ ] 3.9 `WorktreeInventoryLeftoversShould.ReportAWorktreeOfARepositoryOutsideTheRootsAsElsewhere` (a sibling whose `.git` file names an existing registration under `D:\other\Northwind.Ledger\.git\worktrees\fix`, that repository's `.git` present): health `Elsewhere`, repository `Northwind.Ledger`, owner exists; the broken cases unchanged. Then the observation in `WorktreeInventory.BrokenWorktree`.
- [ ] 3.10 `TabScriptsShould.WaitUpdateAndRestartInTheUpdateTab` expects `--source https://api.nuget.org/v3/index.json` after the package id. Then `TabScripts.Update`.

## 4. Handlers

- [ ] 4.1 `CloseEmptyTerminalsHandlerShould.LeaveASessionWhoseProcessIdNowBelongsToAnotherProcess` (process started ten minutes after the session: nothing terminated, named in `Gone`) and the existing facts pass with the fourth list. Then the check in the handler and the `Gone` list on `CloseEmptyTerminalsOutcome`.
- [ ] 4.2 `AdoptSessionHandlerShould.RefuseWhenTheProcessIdNowBelongsToAnotherProcess` (nothing terminated, no resume, `AdoptSessionOutcome.ProcessGone`). Then the check in the handler and the outcome case.
- [ ] 4.3 `RemoveWorktreeHandlerShould.RefuseAnElsewhereRow` (refused with `RepositoryOutsideRoots`, no git command, folder untouched), forced and not. Verify the decision reaches the outcome.

## 5. Screens, wiring and version

- [ ] 5.1 `WorktreesViewShould`: the start-session and open-shell keys on an elsewhere row raise their effects; remove and force-remove on it give the refusal footer without offering force. Then `InRealWorktree` admits `Elsewhere` and `RowColour` paints it amber.
- [ ] 5.2 `SettingsControls` caption reads "Executable (full path, not a .cmd or .bat)"; verify by the smoke check's Settings step and by reading the dialog.
- [ ] 5.3 `Program.cs`: locate git, gh, wt.exe and pwsh.exe through `ProgramLocator.ForCurrentUser()`, Claude through `ClaudeExecutable.Locate()`; print `Wording.GitNotFound`, `ClaudeNotFound` or `ClaudeIsABatchFile` and exit 1; hand the paths to the adapters. Verify: `dotnet run --project src\Source191.Vantage -- --version` and a console start from a folder holding a planted `git.exe` (a copy of cmd.exe) still scans with the real git.
- [ ] 5.4 Raise `Version` to 0.17.0-preview.8 and verify `.github\scripts\Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passes.

## 6. CI and repository

- [ ] 6.1 Pin every `uses:` in `check.yml`, `release.yml`, `claude-contract.yml` and `publish.yml` to its release commit SHA with a `# vX.Y.Z` comment (checkout v7.0.1 3d3c42e5aac5ba805825da76410c181273ba90b1, upload-artifact v7.0.2 cf430e030ddbb5b0abf93d22962f4752f3646cd9, setup-dotnet v6.0.0 a98b56852c35b8e3190ac28c8c2271da59106c68). Verify: a search of `.github` for `uses:` shows only 40-hex pins and local actions.
- [ ] 6.2 Add `.github\workflows\codeql.yml`: csharp (build mode none) and actions, on pull requests and pushes to main with documentation paths ignored, weekly on a schedule, `security-events: write`, codeql-action v4.39.1 (9b3254085494b55a0f7c4c2b3d2d52cc712ce370). Verify the YAML parses (powershell-yaml if present, else by review) and `gh workflow list` shows it after the push.
- [ ] 6.3 Add the attestation step to `release.yml` after the pack (actions/attest-build-provenance v4.2.2 4d101475d8b20a2381f78447822ac1eab6504dd8, `subject-path` the package) with `id-token: write` and `attestations: write`. Verify by review; the first release after the merge shows the attestation on the package.
- [ ] 6.4 Turn on Dependabot security updates: `gh api -X PUT repos/Source191/vantage/automated-security-fixes`; verify `gh api repos/Source191/vantage --jq .security_and_analysis.dependabot_security_updates.status` answers enabled.

## 7. Documentation

- [ ] 7.1 README: a "What Vantage trusts" section (in Contents, after Where Vantage keeps things), the Troubleshooting notes (a claude.cmd shim, VANTAGE_LOG records keystrokes, git not found), the Settings full-path rule, the vantage here prompt rule and exit code, the worktrees HEALTH `elsewhere` word and its refusal, the Pull requests branch rule, the Updates `--source`, and the CI section (SHA pins, CodeQL, attestation, Dependabot security updates, the token on a dedicated account, the post-merge pin setting). Verify by reading each section against the specs.
- [ ] 7.2 CLAUDE.md: the code rule for locators (full paths, never the current directory, never a batch file), the batch-file argument rule, the prompt rule, the CLI fact that `--` is unknown to the CLI (checked 2026-10-29), and the CI line. Verify by reading.

## 8. Verification

- [ ] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` (every project) and `dotnet format --verify-no-changes` all pass.
- [ ] 8.2 `openspec validate harden-for-v1` passes.
- [ ] 8.3 The smoke check of the packed tool passes: `tools\install-local.ps1`, then `dotnet run --project tools\HeadlessTerminal -- smoke <exe> Northwind.Smoke-smoke` (or the CI check job).

## Workflow follow-up

- Open the pull request; after it merges, run `gh api -X PUT repos/Source191/vantage/actions/permissions -f enabled=true -f allowed_actions=all -F sha_pinning_required=true` and confirm `check` still runs on main.
- Archive the change in the pull request that completes it.
