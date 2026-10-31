# Design

## Context

See proposal.md for why. As of main at `356588b`:
- `Program.cs` hands `programs.Locate("gh")`, `programs.Locate("wt")`, `programs.Locate("pwsh")` and `AzureCliExecutable.Locate(PATH)` (each `string?`) to `GitHubPullRequests`, `WindowsTerminal`, `Desktop` and `AzureDevOpsPullRequests`, which test the null on each call. `ClaudeExecutable` already answers a `ClaudeLocation` (`Found`, `BatchFileOnly`, `Missing`).
- `PullRequestCatalogue` answers `PullRequestLookup.Missing(gap, PullRequestHost? host)`, null when the origin remote was never read (no remote, an unknown host, a failed lookup); `WordingOfPullRequests` reads null as "the GitHub CLI (gh) or the Azure CLI (az)" and "gh auth login or az login".
- `TerminalSetupRequest(string? Sound, string? SessionSound, ...)` comes straight from `CommandLine.Build`; null means "use the Windows media default", and `TerminalProfiles(string, string? VantageSound, string? SessionSound)` uses null for "Windows Terminal's own", which `TerminalFragment` writes by leaving `bellSound` out.
- `ProcessRequest(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory)`: null in `EditorCommand`, `Desktop`, `EditorLocator` and `ClaudeCli.Request`'s default; `ProcessRunner` maps null to `string.Empty`.
- `CommandActions.StartHere(string?, string?, ...)` carries System.CommandLine's values through `HereCommandRunner` to `HereCommand.Plan`, which reads `name ?? path.Name` and `prompt ?? string.Empty`. `LaunchPlan.OpeningPrompt` is `string?` (null when blank), and `ClaudeCli.StartBackgroundAsync` adds it when not null.
- `ResumeBackgroundAsync(..., SessionName? name, ...)` adds `-n` when not null; its three callers pass whatever `SessionName.TryParse` left.
- `TabRegistration.OwningTerminal` answers `int?` (null while the tab is still being claimed), passed to `FocusTabAsync`, which searches every window for null. `OwnTab.ClaimAsync` answers `TabColour?` (null when Vantage's own start time cannot be read) and `ShowOwnTab(TabColour?)` writes the title alone for null.
- `Program.cs` reads `VANTAGE_LOG` into a `string?` used twice: `IsNullOrWhiteSpace` decides the diagnostic sink there and again inside `SetupFactsReader`.

## Goals / Non-Goals

**Goals:**
- Null from outside Vantage is translated on the line that receives it: the PATH search, System.CommandLine's `GetValue`, the environment variable, `ProcessStartInfo`, the fragment's JSON.
- Every message, argument list, file and key behaves as before; the tests that pin them change their argument types only.

**Non-Goals:**
- Changing what stays nullable by decision, recorded here for `null-guard-rule`'s allow-list: `IGit`'s six queries (`LastCommitAtAsync`, `DefaultRemoteBranchAsync`, `CurrentBranchAsync`, `RepositoryRootAsync`, `MainCheckoutOfAsync`, `OriginUrlAsync`), `IProcessRegistry.StartTimeOf`, `ParentOf` and `UsageOf`, `FolderPath.Parent`, the JSON data objects (`VantageConfig`, `EditorSetting`, `ClaudeAgentsListing.Row`, `NuGetPackageVersions.VersionIndex`), `TryParse(..., out T?)`, `Equals(T?)`, and the translation functions whose parameter is the outside value itself (`ClaudeStates.From`, `AzureCliExecutable.Locate(string? path)`, `DiagnosticLogSetting.Read(string? variable)`).
- `Program.cs`'s `LocateGitAsync` and `LocateClaudeAsync` keep answering `string?`: the composition root prints the sentence and exits on null.

## Decisions

**`ProgramLocation` is a two-case record in `Ports` beside `ClaudeLocation`.** `Found(string Path)` and `Missing`. It is a named case because each adapter acts differently on it (a gap, an exception sentence, a refused open). `ProgramLocator.Locate` answers it; its `FirstOrDefault` over the PATH folders stays a local and is translated on its return line. `AzureCliExecutable.Locate` the same. `ClaudeExecutable` matches `ProgramLocation.Found` where it matched `is string`. The four adapters switch on it in place of `is null`: `GitHubPullRequests` and `AzureDevOpsPullRequests` answer `PullRequestListing.Unavailable(PullRequestGap.CliMissing)` for `Missing`; `Desktop.OpenAsync` throws its "PowerShell 7 (pwsh.exe) was not found on the PATH..." sentence and `WindowsTerminal.OpenTabAsync` its `TerminalException`, word for word. Alternative: reuse `ClaudeLocation` (its `BatchFileOnly` case means nothing for gh or wt).

**`IEditorLocator.LocateAsync` answers `ProgramLocation`.** `EditorLocator`'s private helpers keep their `FirstOrDefault` locals and translate where `LocateAsync` returns; `EditorPreset.Custom` answers `Missing` as it answered null. `OpenInEditorHandler` takes the custom preset's `EditorChoice.Executable` into a `ProgramLocation` on one line and switches once: `Found` starts the editor, `Missing` answers `OpenInEditorOutcome.EditorNotFound`. `FakeEditorLocator` answers `Found` for an installed preset and `Missing` otherwise.

**`LookupHost` (`Known(PullRequestHost Host)`, `NotRead`) rather than a third `PullRequestHost` member.** An `Unknown` member would have to be named, with no true answer, wherever a found pull request's host is switched on (`Wording.PullRequestNumber`, `RemoteRepository.Host`, `OpenPullRequestOutcome`), since a pull request always has a real host; the "not read yet" fact belongs only to a missing one. `PullRequestLookup.Missing(PullRequestGap Gap, LookupHost Host)` gains a one-argument constructor that supplies `NotRead`, standing where the `= null` default stood. `PullRequestCatalogue.ListAsync` answers `NotRead` with `NoRemote` and `UnknownHost`, `Known(repository.Host)` after a listing, and the failure path keeps `NotRead`. `Footer.NoPullRequest` carries the `LookupHost`. `WordingOfPullRequests.Cli` and `SignIn` switch on both cases with an `UnreachableException` discard arm (the two-case rule), and the `Failed` arm tests `none.Host is LookupHost.Known known`. The words are unchanged, which `WordingShould`'s pull-request facts pin.

**Sounds are two values, because "not given" and "Windows Terminal's own" are different facts.** `SoundArgument` (`Omitted`, `Given(string Path)`) is what the command line said; `BellSound` (`TerminalDefault`, `File(string Path)`) is what a profile gets. `CommandLine.Build` translates each `--sound` value on the line that reads it (`Given` for any text, including blank text, which is refused later as today). `TerminalSetupRunner`: `--remove` with any `Given` sound is `RemoveWithSound`; `FullPath` applies to `Given` only and keeps blank text as it is; the sound check answers a `SoundCheck` (`Accepted`, `Refused(TerminalSetupProblem Problem)`), the first refusal winning as `??` chose it, so `TerminalSetupProblem` gains no "none" case its wording would have to say; `SoundOrDefaultAsync` answers `File(given)`, `File(media default)` when that file exists, or `TerminalDefault` after the same `DefaultSoundMissing` line. `Wording.SoundText(BellSound)` gives the path or "is Windows Terminal's own". `TerminalFragment` writes `bellSound` only for `File`, so the fragment is the same text, which `TerminalFragmentShould`'s literal JSON facts pin.

**`WorkingFolder` (`Inherited`, `At(string Path)`) on `ProcessRequest`.** A named case, not an empty string, so a caller says which it means; `WorkingFolder.Inherit` is a static instance for the six callers that inherit. `ProcessRunner.StartInfo` alone maps `Inherited` to `string.Empty`, which is `ProcessStartInfo`'s own "inherit". `ClaudeCli.Request` takes a `WorkingFolder` with no default. Alternative: keep `string?` (the rule's whole point is that a reader cannot tell why it is null).

**The command line is translated in `CommandLine.Build`.** `HereName` (`FolderName`, `Typed(string Text)`) because a name left off and an empty name differ today (the folder's name against a refused blank name). The prompt becomes `parsed.GetValue(prompt) ?? string.Empty` on its line: an empty prompt and a missing one already mean the same, as the wizard's empty box does, and `LaunchRequest.OpeningPromptText` is already plain text. `CommandActions.StartHere`, `HereCommandRunner.RunAsync` and `HereCommand.Plan` take `HereName` and `string`; `CommandLineShould`'s recording actions assert the new values.

**`OpeningPrompt` (`None`, `Text(string Value)`) lives in `Domain`,** because `IClaudeCli` in `Ports` takes it. `LaunchPlanBuilder` builds it where it built the trimmed `string?`, and the `-` check reads `Text`. `ClaudeCli.StartBackgroundAsync` adds the argument for `Text` only.

**`ResumeBackgroundAsync` has two overloads:** with a `SessionName` (adds `-n`) and without. The three handlers call the one `SessionName.TryParse` chooses, so the `out SessionName?` stays inside the `TryParse` pattern the house allows.

**`TerminalWindow` (`Owning(int ProcessId)`, `Any`) in `Domain`.** `TabRegistration.OwningTerminal` answers it, translating `TerminalTab.TerminalOf`'s `int?` (a walk over `IProcessRegistry.ParentOf`, which stays nullable) on its own line; if `null-free-observations` has already reshaped `TabRegistration`, the method moves with it and keeps this answer. `IWindowsTerminal.FocusTabAsync` and `UiAutomationTabs.Select` take it; `Any` searches every window as null did.

**`OwnTabClaim` (`Claimed(TabColour Colour)`, `Unregistered`) and two `ShowOwnTab` overloads.** `OwnTab.ClaimAsync` meets `IProcessRegistry.StartTimeOf`'s null on its first line, as today. `ScreenLoop` switches once: `Claimed` shows the title and colour, `Unregistered` the title alone. `TerminalConsoleShould` keeps both escape-sequence facts.

**`WindowsTerminalArguments.ForTab(TabLaunch, IReadOnlyList<string> profile)`** takes `[]` or `["--profile", name]`, built by `WindowsTerminal` from `fragment.Exists`; the argument list is identical. **`TerminalFragment(string fragmentsRoot, string iconSource)`** requires the icon, as `Program.cs` always passes one; `CopyIcon` becomes `TryCopyIcon` answering `bool`, and the profile writes `icon` only when it is true. Tests pass a path in their temporary folder, absent unless the fact copies one.

**`DiagnosticLogSetting` (`Off`, `File(string Path)`) with `Read(string? variable)`,** `Off` for null or blank. `Program.cs` reads the variable once into it; the diagnostic log takes a `DiagnosticFile` for `File`, and `SetupFactsReader` answers `Disabled` for `Off` and `At(path)` for `File`.

## Risks / Trade-offs

- [About 60 test lines build a `ProcessRequest`] → each changes its third argument only; `FakeProcessRunner`'s recorded requests assert `WorkingFolder` values, so a caller that changed folder fails a fact.
- [A change of words slips through a switch rewrite] → every `Wording` fact for pull requests, sounds and the setup problems is left as it is and must pass unchanged.
- [`TabRegistration` is reshaped by `null-free-observations` before this lands] → the `TerminalWindow` task names the method, not its position, and is applied to whatever shape main has.
- [Overloads on `IClaudeCli` and `ITerminalConsole` widen two ports] → each overload replaces a null branch the adapter already had; the fakes implement both.
