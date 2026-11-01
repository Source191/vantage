using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// What the status line says, as a fact rather than a sentence; <see cref="Wording"/> turns each case into the words shown.
/// Reducers return these, dialogs set them, and the screen renders them at the last moment.
/// </summary>
public abstract record StatusMessage
{
    public static readonly StatusMessage None = new Blank();

    /// <summary>No message: the view shows its standing message instead.</summary>
    public sealed record Blank : StatusMessage;

    public sealed record NoSessionSelected : StatusMessage;

    /// <summary>A plain claude session in some tab: Vantage cannot attach to it, stop it or delete it from outside.</summary>
    public sealed record RunsInOwnTerminal(string Name, bool HasTranscript) : StatusMessage;

    /// <summary>A stopped session with no transcript: there is nothing to attach to or resume.</summary>
    public sealed record NeverSentAMessage(string Name) : StatusMessage;

    public sealed record NoEmptyTerminals : StatusMessage;

    public sealed record NoMessagesToRename(string Name) : StatusMessage;

    public sealed record WorkingSoRenameWaits(string Name) : StatusMessage;

    public sealed record RenameCancelled : StatusMessage;

    /// <summary>The typed name was refused, for the domain's reason.</summary>
    public sealed record InvalidName(NameProblem Problem) : StatusMessage;

    public sealed record AlreadyUnderVantage(string Name) : StatusMessage;

    public sealed record WorkingSoAdoptWaits(string Name) : StatusMessage;

    public sealed record NoWorktreeSelected : StatusMessage;

    /// <summary>The removal rules said no; the reason is the domain's, and the force key is offered when the rules allow it.</summary>
    public sealed record NotRemoved(RemovalReason Reason, bool OfferForce) : StatusMessage;

    public sealed record ForceCancelled : StatusMessage;

    public sealed record NothingTyped : StatusMessage;

    public sealed record TypedWrongName(string Typed, string Expected) : StatusMessage;

    public sealed record NothingToPrune(string RepositoryName) : StatusMessage;

    public sealed record LeftoverFolder(string FolderName) : StatusMessage;

    public sealed record NoSessionIn(string FolderName) : StatusMessage;

    /// <summary>The pull request key found none for the row: Gap says why, and Host names the CLI when the remote was read.</summary>
    public sealed record NoPullRequest(string RowName, PullRequestGap Gap, PullRequestHost? Host) : StatusMessage;

    /// <summary>The memory key on a session with no process: it is stopped, so there is nothing to measure.</summary>
    public sealed record NothingToMeasure(string Name) : StatusMessage;

    /// <summary>The solution key found no .sln or .slnx in the folder or its src folder.</summary>
    public sealed record NoSolution(string FolderName) : StatusMessage;

    /// <summary>The close-empty-tabs question was answered with Keep.</summary>
    public sealed record Kept : StatusMessage;

    /// <summary>The resume checklist ended with nothing to resume.</summary>
    public sealed record NothingChosenToResume : StatusMessage;

    /// <summary>The update key found no newer release.</summary>
    public sealed record UpToDate : StatusMessage;

    /// <summary>The update key on a build that never checks: the setting is off, or it is a development build.</summary>
    public sealed record UpdateCheckOff : StatusMessage;

    public sealed record NoDialogFor(string EffectName) : StatusMessage;

    /// <summary>The effect is running off the UI thread.</summary>
    public sealed record Working(ViewEffect Effect) : StatusMessage;

    /// <summary>What a handler did.</summary>
    public sealed record Reported(Outcome Outcome) : StatusMessage;

    /// <summary>A removal, then the user's answer to keep the branch it left.</summary>
    public sealed record BranchKept(Outcome Removal) : StatusMessage;

    /// <summary>A removal, then what deleting the branch it left did.</summary>
    public sealed record Followed(Outcome First, Outcome Then) : StatusMessage;

    /// <summary>Background work threw; the message is the exception's.</summary>
    public sealed record Failed(string Message) : StatusMessage;

    /// <summary>The Config tab's standing message: what the selected row's setting does, or what its setup is for.</summary>
    public sealed record SettingHint(ConfigRow Row) : StatusMessage;

    /// <summary>Enter on a setup row: how that setup is changed, since the tab only reads it.</summary>
    public sealed record SetupAdvice(SetupItem Item) : StatusMessage;

    /// <summary>c or e on a Config row whose value is not a path.</summary>
    public sealed record NoPathHere : StatusMessage;

    public sealed record NoSettingSelected : StatusMessage;
}