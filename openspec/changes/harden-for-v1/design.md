# Design

## Context

See proposal.md for the findings. The facts the design rests on, each checked on 29 October 2026:

- `ProcessRunner` starts every child with `Process.Start` and `UseShellExecute = false`, which is CreateProcess with no application path. For a bare name CreateProcess searches the application's folder, then the parent process's current directory, then the system folders, then the PATH. With a copy of cmd.exe saved as `git.exe` in a folder: a parent whose current directory is that folder ran the planted file, with or without a different working directory for the child; a parent elsewhere with the child's working directory in that folder ran the real git; the `NoDefaultCurrentDirectoryInExePath` variable also stopped it.
- A `.cmd` or `.bat` is run by cmd.exe, which re-reads the whole command line: a double quote toggles quoting, `& | < > ^` outside quotes start a command, `%NAME%` expands everywhere. Through a stand-in `claude.cmd` that echoes `%*`, the argument `a"&echo INJECTED&"b` ran `echo INJECTED`, `see %PATH% here` arrived with the PATH expanded, and `Rename "Foo" to "Bar"` arrived with backslashes before the quotes. `EditorChoice` already refuses batch files for editors for this reason; `ClaudeExecutable` still falls back to `claude.cmd`, and `AzureDevOpsPullRequests` now passes the branch name to `az.cmd`.
- `claude attach -- --help` answers `warning: extra arguments ignored: --help` and `unknown option '--'`: the CLI has no end-of-options marker, so a dash-led prompt cannot be passed safely.
- The Claude Code documentation now says the npm package installs the same native binary as the standalone installer, and recommends the native installer or WinGet on Windows; the README already installs it natively.
- `dotnet tool update --source` replaces every configured source for that run.
- `BranchName` refuses only whitespace; git itself allows `&`, `|`, `%`, `"`, `<`, `>` and `!` in a ref name and refuses `^`, `~`, `:`, `?`, `*`, `[`, `\` and a leading `-`.

## Goals / Non-Goals

**Goals:**
- No child process of Vantage is ever resolved through Vantage's current directory.
- No batch file is ever given text from a person, a repository or a listing unless that text is plain.
- A reused process id can never be killed as a session.
- A healthy worktree is never deleted as a leftover.
- The CI supply chain is pinned, scanned and attested, with no change to what the workflows do.

**Non-Goals:**
- Running the npm shim through node.
- Counting work or removing a worktree through a repository outside the roots.
- Replacing `Interop.UIAutomationClient` or signing the package.
- Changing `pwsh.exe` in the tab command line handed to wt.exe (Windows Terminal resolves it from its own process, whose current directory is not Vantage's) or `dotnet` in the update tab (PowerShell's call operator never searches the current directory).

## Decisions

- **One locator for programs on the PATH.** `Adapters\Processes\ProgramLocator` answers the first `<name>.exe` in the PATH folders through `IFolders`, appends `.exe` when the name has no extension, and never answers a `.cmd` or `.bat`; it is given the PATH folders as a list so a test can point it at a fake tree, as `EditorLocator` is. `Program.cs` locates git, gh, wt.exe and pwsh.exe once and hands the paths to `GitCli`, `GitHubPullRequests` (null reads as the CLI missing, as `AzureDevOpsPullRequests` already does for az), `WindowsTerminal` (null throws `TerminalException` naming wt.exe when a tab is opened) and `Desktop` (null throws when a file is opened). Alternative: set `NoDefaultCurrentDirectoryInExePath` in Vantage's own process. Rejected as the primary fix: children inherit it unless dropped, and Windows Terminal would hand it to every tab's shell, changing how cmd.exe behaves there.
- **Claude Code is an executable or nothing.** `ClaudeExecutable.Locate` answers a `ClaudeLocation` (Ports): `Found(path)`, `BatchFileOnly(path)` or `Missing`, so `Program.cs` can say which with `Wording`. Alternative: resolve the shim to `node.exe` and `cli.js`. Rejected: the npm package now installs a native binary, the documented routes are the native installer and WinGet, and refusing is the rule editors already follow. The header's help still shows the path Vantage drives.
- **A custom editor is a full path.** `EditorChoice.Problem` gains `SettingsProblem.ExecutableNotFullPath`, checked after the blank check and before the batch-file check, so `Settings.Validate` refuses it and `EditorChoice.FromFile` reads it as VS Code, the existing fallback. Alternative: resolve a bare name on the PATH at use time. Rejected: a port method, a fake and a handler branch to keep a convenience the presets already cover, and a full path is what the roots and the worktree root already require.
- **The dash-led prompt is refused as a launch problem.** `LaunchPlanBuilder` returns `LaunchProblem.PromptLooksLikeAnOption` when the trimmed prompt starts with `-`, so the wizard shows it on its problem line and `HereCommandRunner` exits 2 as for a refused name. Alternative: put a space before the prompt. Rejected: it hides the rule and changes the prompt.
- **One domain rule says what a batch file may be given.** `Domain\BatchFileArgument.IsSafe` (letters, digits, space, dot, underscore, hyphen, slash) replaces the private `SafeName` regex in `RemoteRepository` and is applied to the branch in `AzureDevOpsPullRequests` before any process starts; the refusal is `PullRequestGap.BranchNotGivenToAz`, a new member the closed switch in `WordingOfPullRequests` must name. gh is an executable and gets every branch as it is.
- **Links are https in the adapters.** Each `Row` keeps the existing `Uri.TryCreate` and adds the scheme check; a non-https row is skipped as a malformed one is.
- **Process identity is a domain rule.** `Domain\ProcessIdentity.StillTheSession(processStartedAt, sessionStartedAt)` is true when the process start is known and no later than the session's listed start plus a five-second tolerance; a reused id belongs to a process that started after the original session died, so it fails. `CloseEmptyTerminalsHandler` lists such sessions in a fourth outcome list `Gone` and `AdoptSessionHandler` answers `AdoptSessionOutcome.ProcessGone`; both read `IProcessRegistry.StartTimeOf` as `TabRegistration.IsLive` already does.
- **"Elsewhere" is a health kind.** `WorktreeObservation` gains `OwnerRepositoryExists`; `WorktreeHealth.From` answers `WorktreeHealthKind.Elsewhere` with `HealthReason.RepositoryOutsideRoots(FolderPath)` for an unregistered folder whose `.git` target exists and whose owner has a `.git` folder, before the orphan and empty cases; `RemovalDecision` refuses it with `RemovalReason.RepositoryOutsideRoots` and `CanBeForced: false`; `WorktreesView.InRealWorktree` admits it; the row is amber. `WorktreeInventory.BrokenWorktree` already computes the owner and whether it exists, so the change is the observation. The repository that failed its own listing looks the same and is refused the same way, which is the safe answer for it too.
- **CI pins and scans.** Every `uses:` takes the commit SHA of its newest release tag with the version in a comment (checkout v7.0.1, upload-artifact v7.0.2, setup-dotnet v6.0.0, codeql-action v4.39.1, attest-build-provenance v4.2.2; NuGet/login is pinned already), so Dependabot keeps updating them. CodeQL is a workflow file (`codeql.yml`, csharp with no build and actions, on pull requests, pushes to main and weekly, ignoring documentation paths) rather than the default setup, so it is reviewed and documented like the other workflows. Attestation is one step in `release.yml` with `id-token: write` and `attestations: write`. The repository setting that requires SHA pins is turned on only after this merges, or the tag-pinned workflows on main would be refused in between; Dependabot security updates are turned on now.

## Risks / Trade-offs

- [A user on the npm shim loses Vantage until they install natively] → the refusal names the file and the installer; the npm package installs the same native binary, so `~\.local\bin\claude.exe` may already exist.
- [A custom editor saved as a bare name silently reads as VS Code] → the dialog refuses the bare name with the reason, the README says so, and the fallback is the one batch files already take.
- [Markdown-bullet prompts are refused] → the sentence says to start with a word; the CLI itself would have refused the prompt as an unknown option.
- [A branch with a plain name but outside the rule's set is not looked up on Azure DevOps] → the set covers every character Azure DevOps and GitHub let a branch carry in practice; the footer explains the gap.
- [The five-second tolerance] → a process that started after its session is a reuse by definition; the tolerance only covers clock rounding.
- [CodeQL adds minutes to every pull request] → it is not a required check and skips documentation-only paths.

## Migration Plan

- Version 0.17.0-preview.8; no state file changes.
- After the merge: `gh api -X PUT repos/Source191/vantage/actions/permissions -f enabled=true -f allowed_actions=all -F sha_pinning_required=true`, then confirm `check` still runs on main.
- Rollback is the previous release; nothing persists.
