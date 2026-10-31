using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>The glyph and colours a status line message starts with; <c>StatusTonesShould</c> holds each one.</summary>
public static partial class Wording
{
    /// <summary>The glyph before a message, in its colour; a running action has the spinner there instead, and a hint has nothing.</summary>
    public static TextPart StatusMark(StatusTone tone) => tone switch
    {
        StatusTone.Success => new TextPart("✓", TextTone.Green),
        StatusTone.Notice => new TextPart("!", TextTone.Orange),
        StatusTone.Failure => new TextPart("✗", TextTone.Red),
        StatusTone.Working => new TextPart(string.Empty, TextTone.Cyan),
        StatusTone.Hint => new TextPart(string.Empty, TextTone.Plain),
    };

    /// <summary>The mark after a message that closes it when clicked.</summary>
    public static string StatusClose => "✕";

    public static string StatusCloseTip => "Close this message";

    /// <summary>The colour of a message's words.</summary>
    public static TextTone StatusWordsTone(StatusTone tone) => StatusMark(tone).Tone;

    /// <summary>How the status line marks a footer; a case with no arm here is a notice.</summary>
    public static StatusTone ToneOf(Footer footer)
    {
        return footer switch
        {
            Footer.Blank or Footer.SettingHint or Footer.SetupAdvice => StatusTone.Hint,
            Footer.RenameCancelled or Footer.ForceCancelled or Footer.Kept or Footer.NothingChosenToResume => StatusTone.Hint,
            Footer.UpToDate => StatusTone.Success,
            Footer.NoPullRequest { Gap: PullRequestGap.Failed } => StatusTone.Failure,
            Footer.NoDialogFor or Footer.Failed => StatusTone.Failure,
            Footer.Working => StatusTone.Working,
            Footer.Reported reported => ToneOf(reported.Outcome),
            Footer.BranchKept kept => ToneOf(kept.Removal),
            Footer.Followed followed => ToneOf(followed.Then),
            _ => StatusTone.Notice,
        };
    }
}