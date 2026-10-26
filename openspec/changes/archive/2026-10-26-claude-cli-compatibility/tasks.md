# Tasks

## 1. Minimum Claude Code version (Domain)

- [x] 1.1 Test `ClaudeCodeSupportShould.AnswerUnknownWhenNothingWasRead`, then add `ClaudeCodeVersion` (`Unknown`, `Reported`, `BelowMinimum`) and `ClaudeCodeSupport.Check` with `Minimum` 2.1.309 in `Domain`; verify the Core suite passes and `LayeringShould` still holds
- [x] 1.2 Test `AnswerBelowMinimumForAnOlderVersion` (2.1.302 gives `BelowMinimum(2.1.302, 2.1.309)`), then the comparison over `ReleaseVersion`; verify it passes
- [x] 1.3 Test `AnswerReportedAtOrAboveTheMinimum` (2.1.309 and 2.1.310 as a theory) and `AnswerReportedForAVersionItCannotRead` ("2.1" gives `Reported("2.1")`); verify both pass
- [x] 1.4 Run Stryker from `test\Source191.Vantage.Core.Tests` with `--test-runner mtp --project Source191.Vantage.Core.csproj --mutate "**/Domain/ClaudeCodeSupport.cs"`; verify no surviving mutants

## 2. Header warning

- [x] 2.1 Test `WordingOfScreensShould.ShowAClaudeVersionBelowTheMinimumWithTheWarning` ("2.1.302 (below 2.1.309, update Claude Code)"), then change `Wording.HeaderValues` to take `ClaudeCodeVersion`, switching over all three cases with a throwing discard arm; move the existing header facts to `Reported("2.1.310")` and `Unknown`; verify the Core suite passes
- [x] 2.2 Pass `ClaudeCodeSupport.Check(await claudeVersion)` from `Program.cs`; verify `dotnet build -c Debug` and the solution's tests pass, and the Debug build driven by `tools\HeadlessTerminal` (`drive` then `grid`) shows the version without a warning on the current CLI
- [x] 2.3 Test `DocumentedMinimumShould` in `Infrastructure.Tests` (finds the folder holding `global.json` from `AppContext.BaseDirectory`; `README.md` and `openspec\config.yaml` contain `ClaudeCodeSupport.Minimum` followed by "or later"); verify it passes, then fails when the README's number is edited, then passes again
- [x] 2.4 README: the header bullet in The sessions view mentions the below-minimum warning; verify `Select-String -Path README.md -Pattern 'update Claude Code'` finds it
- [x] 2.5 Raise `Version` to 0.13.0 in `src\Source191.Vantage\Source191.Vantage.csproj`; verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes once committed

## 3. Help-surface snapshots

- [x] 3.1 Capture `claude --help` and `claude agents --help` from the installed CLI into a fixture; test `HelpSurfaceShould.ExtractTheCommandNames`, then `HelpSurface.Extract` in `test\Source191.Vantage.Infrastructure.Tests\Contract`; verify it passes. Never run `claude --bg --resume --help`: it starts a session
- [x] 3.2 Test `ExtractLongAndShortOptionsSortedAndDistinct`, then the option extraction; verify it passes
- [x] 3.3 Test `IgnoreDescriptionsAndUsageProse` (a reworded description changes nothing), then tighten the parser; verify all `HelpSurfaceShould` facts pass
- [x] 3.4 Write `Contract\Snapshots\claude-help.txt` and `claude-agents-help.txt` from `HelpSurface.Extract` over the installed CLI, copied to the output folder by the csproj; verify both files exist in `bin\Debug` after a build and list `agents` and `--json`
- [x] 3.5 Test `ClaudeHelpSurfaceShould.MatchTheSnapshot` (a theory over both commands, trait `Category=ClaudeContract`, skipped unless `VANTAGE_CLAUDE_CONTRACT` is 1 and `ClaudeExecutable` finds the CLI; on a difference it writes `<name>.received.txt` and fails listing added and removed lines); verify it passes with the variable set, fails naming the line after a snapshot line is deleted, writes the received file, and passes again once the line is restored

## 4. Contract tests over the real CLI

- [x] 4.1 Test `ClaudeContractShould.ReadTheVersion` (same trait and skip as 3.5) through the real `ProcessRunner` and `ClaudeCli`; verify it passes with the variable set and is skipped without it
- [x] 4.2 Test `ListAStartedBackgroundSessionInAStateVantageKnows`: start `contract-<UTC timestamp>` with no prompt in the repository root, poll `ListSessionsAsync` for up to 30 s, assert a `BackgroundSession` whose state is not `Unknown`, and stop and remove it by job id in `finally`; verify it passes and `claude agents --json --all` lists no `contract-` row afterwards
- [x] 4.3 Test `GiveTheStartedRowEveryFieldVantageReads` (the raw listing's row has `pid`, `id`, `cwd`, `kind`, `startedAt`, `sessionId`, `name` and `status`); verify it passes
- [x] 4.4 Test `RemoveTheSessionByItsJobId` (after `StopAsync` and `RemoveAsync` the row is gone from the listing); verify it passes
- [x] 4.5 Verify the ordinary runs never start a session: `dotnet test -c Debug --no-build` at the solution without the variable reports every `ClaudeContract` test as skipped, and `dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"` with `VANTAGE_CLAUDE_CONTRACT=1` runs only them
- [x] 4.6 Move `ProcessRunnerIntegrationShould.StartStopAndRemoveARealBackgroundSessionWithTheMiddleDotDecodedCorrectly` into `ClaudeContractShould` (as `StartWithAnEightCharacterJobIdAndTheMiddleDotDecodedCorrectly`) behind the same gate, because it starts a real session whenever Claude is installed; verify the plain solution run skips it and the filtered run passes it

## 5. Nightly workflow and its issue

- [x] 5.1 Add `.github\workflows\claude-contract.yml` (design decision 6: schedule 03:17 UTC and `workflow_dispatch`, `contents: read` and `issues: write`, token gate, native install, trust seeding copied from `handoff`, Debug build, contract tests with the variable, hand-off check with `continue-on-error`, artefacts); verify every `run:` block parses with PowerShell's own parser, and the gate, install and trust steps match `handoff`'s apart from the deliberate differences (the install step also records the version; the gate warns rather than notes); GitHub's own parse comes with the first push (Workflow follow-up)
- [x] 5.2 Add the issue step (design decision 7: runs only when the install or the contract tests failed, `gh label create claude-cli --force`, comment on the open `claude-cli` issue or create one, `--assignee` from `vars.CLAUDE_CLI_WATCHER` only when set, body with CLI version, run link and failing messages); verify by a local dry run of the body and assignee logic over a sample failing output (with and without a watcher) and by reading the comment and create branches; the live branches are checked in Workflow follow-up
- [x] 5.3 CLAUDE.md: the build-and-test section names the contract tests, the variable and the filter command; the trust exception names `claude-contract.yml` as well as `handoff`; the CLI facts heading says to re-verify them when a snapshot is accepted; verify `Select-String -Path CLAUDE.md -Pattern 'VANTAGE_CLAUDE_CONTRACT','claude-contract.yml'` finds each
- [x] 5.4 README, Continuous integration: the nightly workflow, what raises the `claude-cli` issue, the `CLAUDE_CLI_WATCHER` variable and that an assignee is notified of every comment, and how to accept a snapshot (copy the received file over it in a change that re-checks the CLI facts); verify `Select-String -Path README.md -Pattern 'claude-contract','CLAUDE_CLI_WATCHER'` finds each
- [x] 5.5 Run the full loop: `dotnet build -c Debug`, the solution's tests, `dotnet format --verify-no-changes` and `openspec validate claude-cli-compatibility`; verify all pass

## Workflow follow-up

- Add the `CLAUDE_CODE_OAUTH_TOKEN` repository secret (from `claude setup-token`): it is not set yet, so `handoff` has never run live and the nightly would skip with a warning.
- After merge, create the `CLAUDE_CLI_WATCHER` repository variable with the watcher's GitHub login.
- On the first live run, check that the contract tests' sessions start without `CLAUDE_CODE_OAUTH_TOKEN` (`ProcessRunner` drops it from its children; design.md, Risks).
- Run `gh workflow run claude-contract.yml` and confirm the run is green; then run it on a throwaway branch with one snapshot line deleted (`gh workflow run claude-contract.yml --ref <branch>`), confirm the `claude-cli` issue is created and assigned, run it again to confirm it comments rather than opening a second issue, and close the issue.
- Archive the change in the pull request that completes it.
