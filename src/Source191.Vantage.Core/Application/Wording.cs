using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Every sentence Vantage shows, in one place: footers, dialog questions, the text beside the busy spinner and the
/// key help. Reducers, dialogs and the screen hand over facts; this class chooses the words. <c>WordingShould</c>
/// holds each sentence verbatim.
/// </summary>
public static partial class Wording
{
    /// <summary>Printed on one line before an attach and erased after it, so it must never wrap.</summary>
    public static string ReturnHint => "Ctrl+Z returns to Vantage.";

    /// <summary>vantage here refuses a dash-led name that did not follow --, since it is most likely a mistyped option.</summary>
    public static string NameLooksLikeAnOption(string token) => $"{token} looks like an option, not a session name. To use it as the name, put -- before it: vantage here -- {token}";

    /// <summary>The console and vantage here print one of the next three and exit 1 when a program they need cannot be run.</summary>
    public static string ClaudeNotFound => "Claude Code was not found. Install it with irm https://claude.ai/install.ps1 | iex, or put claude.exe on the PATH.";

    public static string ClaudeIsABatchFile(string path) => $"Claude Code was found only as the batch file {path}, which Vantage does not run: cmd.exe would read session names and prompts as commands. Install the native build with irm https://claude.ai/install.ps1 | iex.";

    public static string GitNotFound => "git was not found on the PATH. Install Git for Windows (winget install --id Git.Git --exact --source winget) and open a new terminal.";

    /// <summary>Where to support Vantage, printed under its description in <c>vantage --help</c>.</summary>
    public static string SupportLine => $"Buy me a sliver of a Claude Max plan: {SupportPage}";

    private static readonly Uri SupportPage = new("https://buymeacoffee.com/simonvane");

    private const string SetupTerminalSyntax = "vantage setup-terminal [--sound <wav>] [--session-sound <wav>] [--yes] [--remove]";

    public static string ForceRemoveWarning => "This deletes the folder below and everything in it that is not committed and pushed.\nTo confirm, give its name:";

    /// <summary>The typed-confirmation dialog's line when Enter is pressed on an empty box; expectedKind names what must be typed.</summary>
    public static string NothingTypedYet(string expectedKind, string expected) => $"Nothing typed yet; the {expectedKind} is {expected}.";

    public static string NotTheSame(string typed) => $"Not the same: you typed {typed}.";

    public static string CopiedToClipboard(string expected) => $"Copied {expected} to the clipboard; Ctrl+V pastes it into the box.";

    public static string ClipboardUnavailable => "The clipboard is not available here; Shift+drag selects text in Windows Terminal.";

    public const string ProblemReportDialogTitle = "Report a problem";

    public static string ProblemReportExplanation => "Holds no names, paths or code. Opening the page passes the title and this text to GitHub in its address; nothing is public until you submit it there, where you describe the problem.";

    public static string ProblemReportTitleCaption => "Title (blank for a default)";

    public static string ProblemReportPreviewCaption => "Diagnostics the page will be filled in with";

    /// <summary>The problem report dialog's buttons, in order: open (the default), copy, cancel.</summary>
    public static IReadOnlyList<string> ProblemReportButtons { get; } = ["Open in browser", "Copy text", "Cancel"];

    /// <summary>The title of a report whose title box was left blank.</summary>
    public static string DefaultReportTitle(ToolVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return $"Problem report from Vantage {version.Text}";
    }

    public static string Describe(Footer footer)
    {
        return footer switch
        {
            Footer.Blank => string.Empty,
            Footer.NoSessionSelected => "No session selected.",
            Footer.RunsInOwnTerminal { HasTranscript: true } terminal =>
                $"{terminal.Name} runs in its own terminal. Press b to move it under Vantage (its tab becomes a plain shell), or type /bg there.",
            Footer.RunsInOwnTerminal terminal =>
                $"{terminal.Name} runs in its own terminal and has no messages yet. d closes its tab, C closes every such tab, or send it something there and press b.",
            Footer.NeverSentAMessage never => $"{never.Name} has nothing to resume: it was never sent a message. d deletes it.",
            Footer.NoEmptyTerminals => "No terminal sessions without messages.",
            Footer.NoMessagesToRename rename => $"{rename.Name} has no messages yet, so it cannot be renamed; send it something first.",
            Footer.WorkingSoRenameWaits rename => $"{rename.Name} is working; wait until it is idle or needs input, because renaming restarts it.",
            Footer.RenameCancelled => "Rename cancelled.",
            Footer.InvalidName invalid => Describe(invalid.Problem),
            Footer.AlreadyUnderVantage adopted => $"{adopted.Name} already runs in the background under Vantage.",
            Footer.WorkingSoAdoptWaits adopt => $"{adopt.Name} is working; wait until it is idle or needs input, so nothing in flight is lost.",
            _ => DescribeWorktreesAndScreen(footer),
        };
    }

    /// <summary>Why a typed problem report title is refused, under the dialog's title box.</summary>
    public static string Describe(ReportTitleProblem problem) => problem switch
    {
        ReportTitleProblem.Missing => "Type a title.",
        ReportTitleProblem.TooLong => $"A title can have at most {ReportTitle.MostCharacters} characters.",
    };

    /// <summary>Why a typed session name is refused, in the rename footer, the wizard and vantage here alike.</summary>
    public static string Describe(NameProblem problem)
    {
        return problem switch
        {
            NameProblem.Missing => "A session name is required.",
        };
    }

    /// <summary>Why the removal rules said no, or what they did instead; the footer and the outcomes wrap it in their own sentence.</summary>
    public static string Describe(RemovalReason reason)
    {
        return reason switch
        {
            RemovalReason.None => string.Empty,
            RemovalReason.LiveSession live => $"session {live.Name} is using it",
            RemovalReason.Locked => "the worktree is locked",
            RemovalReason.LeftoverFilesWouldBeLost leftover => $"{(leftover.AtLeast ? "at least " : string.Empty)}{leftover.Count} files that git no longer tracks would be lost",
            RemovalReason.ChangesWouldBeLost changes => $"{changes.Count} changed or untracked files would be lost",
            RemovalReason.UnpushedCommits unpushed => $"branch {unpushed.Branch} has {unpushed.Count} unpushed commits",
            RemovalReason.LeftoverFolder => "leftover folder",
            RemovalReason.RepositoryOutsideRoots => "its repository is not under the roots; add that root in settings or remove the worktree with git there",
            _ => reason.ToString(),
        };
    }

    /// <summary>What is wrong with a worktree; a lock keeps git's own words.</summary>
    public static string Describe(HealthReason reason)
    {
        return reason switch
        {
            HealthReason.None => string.Empty,
            HealthReason.FolderGone => "registered with git but the folder is gone",
            HealthReason.Locked locked => locked.LockReason,
            HealthReason.RegistrationGone => "its .git file points at a registration that no longer exists",
            HealthReason.UnderClaudeWorktrees => @"under .claude\worktrees but git no longer lists it",
            HealthReason.EmptyLeftover => "leftover folder with nothing in it",
            HealthReason.NotRegistered => "folder is not registered with git",
            HealthReason.RepositoryOutsideRoots outside => $"its repository {outside.Repository} is not under the roots",
            _ => reason.ToString(),
        };
    }

    /// <summary>Why a launch is refused, in the wizard and from vantage here; the command line's refusals end with its usage line.</summary>
    public static string Describe(LaunchProblem problem)
    {
        return problem switch
        {
            LaunchProblem.InvalidName invalid => Describe(invalid.Problem),
            LaunchProblem.WorktreeNeedsRepository => "A worktree needs a repository; choose one or start on the current branch.",
            LaunchProblem.WorktreeNeedsLetters => "A worktree needs a name with some letters or digits in it.",
            LaunchProblem.PromptLooksLikeAnOption => "A prompt cannot start with a dash: Claude Code would read it as options. Start with a word.",
            _ => problem.ToString(),
        };
    }

    /// <summary>Why the settings dialog will not save.</summary>
    public static string Describe(SettingsProblem problem)
    {
        return problem switch
        {
            SettingsProblem.NoRoots => @"Give a repository root: a full path, such as C:\Users\me\source\repos.",
            SettingsProblem.RootNotFullPath relative => $@"The repository root {relative.Text} is not a full path, such as C:\Users\me\source\repos.",
            SettingsProblem.RootMissing missing => $"{missing.Path} does not exist.",
            SettingsProblem.RootAlreadyListed repeated => $"{repeated.Path} is already a repository root.",
            SettingsProblem.WorktreeRootNotFullPath => @"The worktree root must be a full path, such as C:\Users\me\source\worktrees.",
            SettingsProblem.NoExecutable => "A custom editor needs an executable.",
            SettingsProblem.ExecutableNotFullPath => @"A custom editor's executable must be a full path, such as C:\Tools\subl.exe, so it is never looked for in the folder Vantage was started from.",
            SettingsProblem.BatchFileExecutable => "A batch file cannot be used as the editor: cmd.exe would read the folder path as commands.",
            SettingsProblem.NoFolderPlaceholder => $"A custom editor's arguments need {EditorChoice.FolderPlaceholder} where the folder goes.",
            _ => problem.ToString(),
        };
    }

    /// <summary>An editor preset as the settings dialog names it.</summary>
    public static string Describe(EditorPreset preset)
    {
        return preset switch
        {
            EditorPreset.VsCode => "VS Code",
            EditorPreset.VisualStudio => "Visual Studio",
            EditorPreset.Rider => "Rider",
            EditorPreset.Explorer => "Explorer",
            EditorPreset.Custom => "Custom",
        };
    }

    /// <summary>Why the wizard will not go on.</summary>
    public static string Describe(WizardProblem problem)
    {
        return problem switch
        {
            WizardProblem.None => string.Empty,
            WizardProblem.NoRepository => "Choose a repository: type part of its name, then ↑↓.",
            WizardProblem.Refused refused => Describe(refused.Problem),
            _ => problem.ToString(),
        };
    }

    /// <summary>What the footer says, beside the busy spinner, while an effect runs off the UI thread.</summary>
    public static string Working(ViewEffect effect)
    {
        return effect switch
        {
            OpenTabForSession open => $"Opening a tab for {open.Session.Name}...",
            StopSession => "Stopping...",
            AdoptSession adopt => $"Moving {adopt.Session.Name} under Vantage...",
            RenameSession rename => $"Renaming {rename.Session.Name} to {rename.NewName.Value}...",
            CloseEmptyTerminals => "Closing...",
            ResumeSessions resume => $"Resuming {resume.Sessions.Count}...",
            DeleteSession deletion => $"Deleting {deletion.Session.Name}...",
            DeleteBranch branch => $"Deleting branch {branch.Branch.Branch}...",
            RemoveWorktree remove => $"Removing {remove.Row.FolderName}...",
            PruneRepository => "Pruning...",
            OpenShellTab shell => $"Opening a shell in {shell.Row.FolderName}...",
            StartSession start => $"Starting {start.Plan.Name.Value}...",
            CopyFolderPath => "Copying...",
            OpenFolderInEditor open => $"Opening {open.Folder.Name} in the editor...",
            FindSolution find => $"Looking for a solution in {find.Folder.Name}...",
            ShowMemory show => $"Measuring {show.Session.Name}...",
            OpenSolution solution => $"Opening {Path.GetFileName(solution.File)}...",
            OpenPullRequest open => $"Opening {PullRequestNumber(open.PullRequest.Host, open.PullRequest.Number)}...",
            SaveSettings => "Saving settings...",
            StartUpdate update => $"Opening a tab to update to {update.Newest}...",
            ShowProblemReport => "Gathering the diagnostics...",
            OpenProblemReport => "Opening the problem report in your browser...",
            CopyProblemReport => "Copying the problem report...",
            _ => "Working...",
        };
    }

    /// <summary>The update key's question, once a newer release is known.</summary>
    public static string UpdateQuestion(ReleaseVersion newest) => $"Quit Vantage and update to {newest}?\nBackground sessions keep running.";

    /// <summary>What the update tab says when dotnet tool update fails, under dotnet's own message.</summary>
    public static string UpdateFailedHint => "Vantage: the update failed. Quit every Vantage that is still open, then press U in Vantage again.";

    public static string WizardTitle(WizardState wizard) => wizard.FixedChoice is null ? "New session" : $"New session in {wizard.FixedChoice.Name}";

    public static string WizardChosenCaption(WizardState wizard) => wizard.ShowsChooser ? "Repository" : "Directory";

    public static string WizardChosen(WizardState wizard) => wizard.Chosen?.Name ?? "(none)";

    public static string WizardHint(WizardState wizard)
    {
        const string Fields = "Enter or Tab next field   F5 start   F6 start and open in tab   Esc cancel";

        return wizard.ShowsChooser ? $"Type to filter, ↑↓ to choose, Enter confirms the repository   {Fields}" : "Enter starts and opens in tab   F5 start without a tab   Esc cancel";
    }

    public static string MemoryDialogTitle(string sessionName) => $"Memory: {sessionName}";

    /// <summary>The breakdown dialog's text: Claude Code, each program with its subtree's memory, then the total with the process count, figures right-aligned.</summary>
    public static string MemoryBreakdownLines(MemoryBreakdown breakdown)
    {
        ArgumentNullException.ThrowIfNull(breakdown);
        const string claudeCode = "Claude Code";
        const string total = "Total";
        const int figureWidth = 7;
        IReadOnlyList<MemoryShare> shares = [new MemoryShare(claudeCode, breakdown.ClaudeCode), .. breakdown.Programs];
        int labelWidth = shares.Select(share => share.Label.Length).Append(total.Length).Max() + 2;
        IEnumerable<string> lines = shares.Select(share => $"{share.Label.PadRight(labelWidth)}{Memory(share.Memory).PadLeft(figureWidth)}");
        string processes = breakdown.ProcessCount == 1 ? "process" : "processes";

        return string.Join('\n', [.. lines, string.Empty, $"{total.PadRight(labelWidth)}{Memory(breakdown.Total).PadLeft(figureWidth)} in {breakdown.ProcessCount} {processes}"]);
    }

    public static string ChooseSolutionTitle => "Open solution";

    public static string ChooseSolutionQuestion(string folderName) => $"{folderName} holds more than one solution. Which one?";

    public static string RenamePrompt(string name) => $"Rename {name}. The session restarts under the new name and keeps its conversation.";

    public static string DeleteSessionQuestion(string name) => $"Delete {name}?\nIts worktree is removed only when nothing in it would be lost.";

    public static string CloseEmptyQuestion(IReadOnlyList<string> names)
    {
        string listed = string.Join(", ", names);
        string what = names.Count == 1 ? $"Close the tab of {listed}?" : $"Close {names.Count} tabs: {listed}?";

        return $"{what}\nNone of them has been sent a message, so nothing is lost.";
    }

    public static string DeleteBranchQuestion(string branch, bool createdByVantage)
    {
        return createdByVantage
            ? $"Branch {branch} is not merged. Delete it anyway?"
            : $"Branch {branch} was not created by Vantage. Delete it anyway?";
    }

    // vantage setup-terminal.

    public static string SetupTerminalUsage => $"Usage: {SetupTerminalSyntax}";

    public static string WriteProfilesQuestion => "Write the Windows Terminal profiles? [y/N] ";

    public static string TurnOnClaudeBellQuestion => "Turn on Claude Code's terminal bell? [y/N] ";

    public static string ClaudeBellAlreadyOn => "Claude Code's terminal bell is already on.";

    public static string NothingWritten => "Nothing was written.";

    public static string NothingToRemove => "There is nothing to remove.";

    /// <summary>Why setup-terminal will not run; every refusal ends with the usage line.</summary>
    public static string DescribeSetupProblem(TerminalSetupProblem problem)
    {
        return problem switch
        {
            TerminalSetupProblem.RemoveWithSound => $"--remove cannot be combined with --sound or --session-sound. {SetupTerminalUsage}",
            TerminalSetupProblem.SoundNotFound missing => $"The sound file {missing.Path} does not exist. {SetupTerminalUsage}",
            TerminalSetupProblem.SoundNotWav notWav => $"The sound file {notWav.Path} is not a .wav file. {SetupTerminalUsage}",
            _ => problem.ToString(),
        };
    }

    public static string TerminalSetupPlan(string fragmentPath, TerminalProfiles profiles)
    {
        return string.Join(Environment.NewLine,
            $"Two Windows Terminal profiles will be written to {fragmentPath}",
            "Your Windows Terminal settings file is not changed.",
            $"  {TerminalProfiles.VantageProfileName,-15}  runs {profiles.VantageExecutable}; bell sound {SoundText(profiles.VantageSound)}",
            $"  {TerminalProfiles.SessionProfileName,-15}  runs PowerShell; bell sound {SoundText(profiles.SessionSound)}",
            "Both ring the bell and flash the window and the taskbar.");
    }

    public static string ProfilesWritten(string fragmentPath) => $"Wrote {fragmentPath}. Restart Windows Terminal to see the Vantage and Vantage session profiles.";

    public static string DefaultSoundMissing(string profileName, string path) => $"{path} was not found, so the {profileName} profile uses Windows Terminal's own bell sound.";

    /// <summary>What turning the bell on will do to the settings file; <paramref name="replacing"/> is the other channel it displaces, if any.</summary>
    public static string ClaudeBellPlan(string settingsPath, string? replacing)
    {
        string change = replacing is null
            ? "This sets \"preferredNotifChannel\": \"terminal_bell\""
            : $"This replaces \"preferredNotifChannel\": \"{replacing}\" with \"terminal_bell\"";

        return string.Join(Environment.NewLine,
            "Claude Code can ring the terminal bell when a session finishes or needs permission.",
            $"{change} in {settingsPath} and keeps every other setting.");
    }

    public static string ClaudeBellEnabled(string settingsPath) => $"Set preferredNotifChannel to terminal_bell in {settingsPath}. It applies to sessions started from now on.";

    public static string ClaudeSettingsUnreadable(string settingsPath, string reason) => $"Could not read {settingsPath} ({reason}), so it was left alone.";

    public static string ProfilesRemoved(string fragmentPath) => $"Removed {fragmentPath}. Restart Windows Terminal to drop the Vantage profiles.";

    /// <summary>An exception's message reports what failed, here and nowhere as a reason.</summary>
    public static string TerminalSetupFailed(string path, string reason) => $"Could not write {path}: {reason}";

    private static string SoundText(string? sound) => sound ?? "is Windows Terminal's own";

    private static string DescribeWorktreesAndScreen(Footer footer)
    {
        return footer switch
        {
            Footer.NoWorktreeSelected => "No worktree selected.",
            Footer.SettingHint hint => ConfigDescription(hint.Row),
            Footer.SetupAdvice advice => Describe(advice.Item),
            Footer.NoPathHere => "This row's value is not a path.",
            Footer.NoSettingSelected => "No setting selected.",
            Footer.NotRemoved { OfferForce: true } refused => $"Not removed: {Describe(refused.Reason)}. D forces after a typed confirmation.",
            Footer.NotRemoved refused => $"Not removed: {Describe(refused.Reason)}.",
            Footer.ForceCancelled => "Cancelled; nothing removed.",
            Footer.NothingTyped => "Nothing was typed; nothing removed.",
            Footer.TypedWrongName wrong => $"You typed {wrong.Typed}, not {wrong.Expected}; nothing removed.",
            Footer.NothingToPrune prune => $"Nothing to prune: the repository {prune.RepositoryName} no longer exists.",
            Footer.LeftoverFolder leftover => $"{leftover.FolderName} is a leftover folder, not a worktree.",
            Footer.NoSessionIn none => $"No session is running in {none.FolderName}.",
            Footer.NoSolution none => $"No solution found in {none.FolderName} or its src folder.",
            Footer.NothingToMeasure stopped => $"{stopped.Name} has no process to measure; it is stopped.",
            Footer.NoPullRequest none => Describe(none),
            Footer.UpToDate => "Vantage is up to date.",
            Footer.UpdateCheckOff => "The update check is off, or this is a development build; nothing to update to.",
            Footer.Kept => "Kept.",
            Footer.NothingChosenToResume => "Nothing to resume.",
            Footer.NoDialogFor missing => $"No dialog is defined for {missing.EffectName}.",
            Footer.Working working => Working(working.Effect),
            Footer.Reported reported => Describe(reported.Outcome),
            Footer.BranchKept kept => $"{Describe(kept.Removal)} Branch kept.",
            Footer.Followed followed => $"{Describe(followed.First)} {Describe(followed.Then)}",
            Footer.Failed failed => $"Failed: {failed.Message}",
            _ => footer.ToString(),
        };
    }
}