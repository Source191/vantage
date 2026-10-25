# Tasks

## 1. Command line and request

- [x] 1.1 `CommandLineShould.ParseSetupTerminalWithItsArguments`: add `SetupTerminal(IReadOnlyList<string> Arguments)` and the `["setup-terminal", ..]` arm to `CommandLine.Parse`; verify `dotnet test test\Source191.Vantage.Core.Tests` passes
- [x] 1.2 `TerminalSetupCommandShould.ParseNoArgumentsAsDefaultsWithConfirmation`, `ParseSoundSessionSoundAndYes`, `ParseRemove`: implement `TerminalSetupCommand.Parse` returning `TerminalSetupRequest`; verify the tests pass
- [x] 1.3 `TerminalSetupCommandShould.RefuseAnUnknownOptionAnOptionWithoutAValueAndRemoveWithASound`: return the usage problem, with the usage line in `Wording` and a fact in `WordingShould`; verify the tests pass

## 2. Ports, profiles and fakes

- [x] 2.1 Add `TerminalProfiles` (the two profile names, bell style, Vantage executable, optional sounds), `ITerminalFragment` and `IClaudeSettings` with `NotificationChannelState` to `Ports`, and `FakeTerminalFragment` and `FakeClaudeSettings` to `TestDoubles`; verify `dotnet build -c Debug` passes and `LayeringShould` still passes

## 3. Adapters

- [x] 3.1 `TerminalFragmentShould.WriteBothProfilesAsUtf8WithoutABom`, `LeaveOutASoundThatIsNull`, `GiveTheVantageProfileNoTabColour`, `ReplaceAnExistingFragment` (temporary folder as the Fragments root): implement `TerminalFragment` in `Infrastructure\Terminal`; verify `dotnet test test\Source191.Vantage.Infrastructure.Tests` passes
- [x] 3.2 `TerminalFragmentShould.RemoveOnlyTheVantageFolder`, `ReportNothingToRemove`, `ReportExistsOnlyWhenTheFileIsThere`: implement `RemoveAsync` and `Exists`; verify the tests pass
- [x] 3.3 `ClaudeSettingsShould.ReadMissingUnsetSetAndUnreadable`: implement `ClaudeSettings.ReadNotificationChannelAsync` in `Infrastructure\Claude`; verify the tests pass
- [x] 3.4 `ClaudeSettingsShould.EnableTheTerminalBellKeepingOtherKeysInOrderWithLfOnly`, `CreateTheFileWhenMissing`, `KeepTheTrailingNewlineAsItWas`: implement `EnableTerminalBellAsync`; verify the tests pass, including a byte check that no `\r` is written
- [x] 3.5 `WindowsTerminalArgumentsShould.AddTheProfileWhenGiven`, `OmitTheProfileWhenNull`: change `ForTab(launch, profile)`; verify the existing argument tests still pass
- [x] 3.6 `WindowsTerminalShould.OpenTabsInTheSessionProfileWhenTheFragmentExists`, `OpenTabsInTheDefaultProfileOtherwise` (fake process runner, fake fragment): give `WindowsTerminal` an `ITerminalFragment`; verify the tests pass

## 4. Use case

- [x] 4.1 `TerminalSetupRunnerShould.ShowTheFragmentAndProfilesAndWriteNothingWhenDeclined`, `WriteNothingAtTheEndOfInput`: implement `TerminalSetupRunner` showing the plan and asking; words in `Wording` with facts in `WordingShould`; verify the tests pass
- [x] 4.2 `TerminalSetupRunnerShould.WriteTheProfilesWithDefaultSoundsAndSayToRestart`, `WriteGivenSounds`, `LeaveOutAMissingDefaultAndSaySo`: implement writing the fragment; verify the tests pass
- [x] 4.3 `TerminalSetupRunnerShould.RefuseAMissingOrNonWavSoundWithExitTwo`: implement the sound checks through `IFolders`; verify the tests pass
- [x] 4.4 `TerminalSetupRunnerShould.OfferTheTerminalBellOnlyWhenNotAlreadySet`, `EnableItWhenAccepted`, `SkipAnUnreadableSettingsFileButStillWriteProfiles`, `AnswerBothQuestionsWithYes`: implement the second question; verify the tests pass
- [x] 4.5 `TerminalSetupRunnerShould.RemoveTheFragmentAndLeaveClaudeSettingsAlone`, `ReportNothingToRemove`, `ReturnOneWhenAWriteFails`: implement `--remove` and failure handling; verify the tests pass
- [x] 4.6 `NoHooksShould` style fact (or a runner test) asserting the runner never touches anything but the fragment and `preferredNotifChannel`: verify it passes

## 5. Wiring and checks

- [x] 5.1 Dispatch `SetupTerminal` in `Program.cs` before `ClaudeExecutable.Locate`, constructing `TerminalSetupRunner` with the adapters, `Console.In`, `Console.Out` and `Console.Error`, and pass the fragment to `WindowsTerminal`; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [x] 5.2 (not run here: moved to public-release 8.6, P13, which waits for the product owner at the terminal) Manual check with a local install (`tools\install-local.ps1`): run `vantage setup-terminal`, decline, confirm nothing changed; run it with `--yes`, restart Windows Terminal, confirm both profiles appear, the Vantage profile opens Vantage titled and coloured, a bell in it plays `Windows Notify Messaging.wav`, a tab opened with `o` uses "Vantage session" and its bell plays the other sound; record what `wt new-tab --profile "Vantage session"` does before the restart (falls back or fails) and, if it fails, add the retry without `--profile` with its test before continuing
- [x] 5.3 (not run here: moved to public-release 8.6, P13) Manual check: run `vantage setup-terminal --remove`, restart Windows Terminal, confirm the profiles are gone and `o` opens tabs in the default profile again

## 6. Documentation and release

- [x] 6.1 README: add a "Windows Terminal setup" section (the command and its options, what the fragment adds and where, restart, manual steps in Settings under the profile's Advanced page for "Bell notification style" and "Bell sound", Claude's `preferredNotifChannel`, the suggested sounds from `C:\Windows\Media`: `Windows Notify Messaging.wav`, `Windows Notify Calendar.wav`, `Windows Proximity Notification.wav`, `Windows Message Nudge.wav`, `chimes.wav`, `Alarm01.wav` to `Alarm10.wav`, `Ring01.wav` to `Ring10.wav`, and `--remove`); replace Install step 2 with a pointer to it; point Notifications and the "No bell" troubleshooting entry at it; add the Fragments folder to "Where Vantage keeps things"; verify every documented command runs as written
- [x] 6.2 CLAUDE.md: add to the Windows Terminal facts that fragments under `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\Vantage` add the two profiles, load at Windows Terminal start-up, and that tabs take `--profile "Vantage session"` when the fragment exists; verify `openspec validate "windows-terminal-setup" --strict` passes
- [x] 6.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s (a new subcommand and new tab behaviour that keep existing keys working); verify `.github\scripts\Assert-VersionBumped.ps1` passes locally

## Workflow follow-up

- P10 in `public-release`'s sequence: standalone, branched from `main` any time after P1; its terminal checks wait for P13.
- Archive the change in the same pull request that completes it (`/opsx:archive`), so `openspec\specs` describes the code on `main`.
