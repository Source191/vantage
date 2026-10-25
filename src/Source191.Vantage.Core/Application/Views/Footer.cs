using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// What the footer says, as a fact rather than a sentence; <see cref="Wording"/> turns each case into the words shown.
/// Reducers return these, dialogs set them, and the screen renders them at the last moment.
/// </summary>
public abstract record Footer
{
    public static readonly Footer None = new Blank();

    /// <summary>No message: the view shows its standing footer instead.</summary>
    public sealed record Blank : Footer;

    /// <summary>The sessions view's standing footer.</summary>
    public sealed record NeedInput(int Count) : Footer;

    /// <summary>The worktrees view's standing footer when the selected row is healthy.</summary>
    public sealed record RemoveHint : Footer;

    /// <summary>The worktrees view's standing footer when the selected row is not healthy; the reason comes from the domain.</summary>
    public sealed record Unhealthy(string FolderName, WorktreeHealthKind Kind, HealthReason Reason) : Footer;

    public sealed record NoSessionSelected : Footer;

    /// <summary>A plain claude session in some tab: Vantage cannot attach to it, stop it or delete it from outside.</summary>
    public sealed record RunsInOwnTerminal(string Name, bool HasTranscript) : Footer;

    public sealed record OpenInAnotherTab(string Name) : Footer;

    /// <summary>A stopped session with no transcript: there is nothing to attach to or resume.</summary>
    public sealed record NeverSentAMessage(string Name) : Footer;

    public sealed record NoEmptyTerminals : Footer;

    public sealed record NoMessagesToRename(string Name) : Footer;

    public sealed record WorkingSoRenameWaits(string Name) : Footer;

    public sealed record RenameCancelled : Footer;

    /// <summary>The typed name was refused, for the domain's reason.</summary>
    public sealed record InvalidName(NameProblem Problem) : Footer;

    public sealed record AlreadyUnderVantage(string Name) : Footer;

    public sealed record WorkingSoAdoptWaits(string Name) : Footer;

    public sealed record NoWorktreeSelected : Footer;

    /// <summary>The removal rules said no; the reason is the domain's, and the force key is offered when the rules allow it.</summary>
    public sealed record NotRemoved(RemovalReason Reason, bool OfferForce) : Footer;

    public sealed record ForceCancelled : Footer;

    public sealed record NothingTyped : Footer;

    public sealed record TypedWrongName(string Typed, string Expected) : Footer;

    public sealed record NothingToPrune(string RepositoryName) : Footer;

    public sealed record LeftoverFolder(string FolderName) : Footer;

    public sealed record NoSessionIn(string FolderName) : Footer;

    /// <summary>The solution key found no .sln or .slnx in the folder or its src folder.</summary>
    public sealed record NoSolution(string FolderName) : Footer;

    /// <summary>The close-empty-tabs question was answered with Keep.</summary>
    public sealed record Kept : Footer;

    /// <summary>The resume checklist ended with nothing to resume.</summary>
    public sealed record NothingChosenToResume : Footer;

    /// <summary>The update key found no newer release.</summary>
    public sealed record UpToDate : Footer;

    /// <summary>The update key on a build that never checks: the setting is off, or it is a development build.</summary>
    public sealed record UpdateCheckOff : Footer;

    public sealed record NoDialogFor(string EffectName) : Footer;

    /// <summary>The effect is running off the UI thread.</summary>
    public sealed record Working(ViewEffect Effect) : Footer;

    /// <summary>What a handler did.</summary>
    public sealed record Reported(Outcome Outcome) : Footer;

    /// <summary>A removal, then the user's answer to keep the branch it left.</summary>
    public sealed record BranchKept(Outcome Removal) : Footer;

    /// <summary>A removal, then what deleting the branch it left did.</summary>
    public sealed record Followed(Outcome First, Outcome Then) : Footer;

    /// <summary>Background work threw; the message is the exception's.</summary>
    public sealed record Failed(string Message) : Footer;
}