using System.CommandLine;

namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>What <c>vantage setup-terminal</c> was asked to do; a null sound means "use the default".</summary>
public sealed record TerminalSetupRequest(string? Sound, string? SessionSound, bool Yes, bool Remove);

/// <summary>Why setup-terminal will not run, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record TerminalSetupProblem
{
    public sealed record UnknownOption(string Option) : TerminalSetupProblem;

    public sealed record OptionNeedsValue(string Option) : TerminalSetupProblem;

    public sealed record UnexpectedArgument(string Argument) : TerminalSetupProblem;

    public sealed record RemoveWithSound : TerminalSetupProblem;

    public sealed record SoundNotFound(string Path) : TerminalSetupProblem;

    public sealed record SoundNotWav(string Path) : TerminalSetupProblem;
}

/// <summary>A request, or the reason there is none.</summary>
public sealed record TerminalSetupParse(TerminalSetupRequest? Request, TerminalSetupProblem? Problem)
{
    public static TerminalSetupParse Refused(TerminalSetupProblem problem) => new TerminalSetupParse(Request: null, problem);
}

/// <summary><c>vantage setup-terminal [--sound wav] [--session-sound wav] [--yes] [--remove]</c>: pure parsing, checking the files is the runner's.</summary>
public static class TerminalSetupCommand
{
    private static readonly Option<string[]> Sound = CommandLineSyntax.Repeatable("--sound");
    private static readonly Option<string[]> SessionSound = CommandLineSyntax.Repeatable("--session-sound");
    private static readonly Option<bool> Yes = CommandLineSyntax.Flag("--yes");
    private static readonly Option<bool> Remove = CommandLineSyntax.Flag("--remove");
    private static readonly Command Syntax = new("setup-terminal") { Sound, SessionSound, Yes, Remove };

    public static TerminalSetupParse Parse(IReadOnlyList<string> arguments)
    {
        ParseResult parsed = CommandLineSyntax.Parse(Syntax, arguments);
        TerminalSetupProblem? problem = CommandLineSyntax.FirstProblem<TerminalSetupProblem>(
            Syntax,
            parsed,
            arguments,
            option => new TerminalSetupProblem.UnknownOption(option),
            option => new TerminalSetupProblem.OptionNeedsValue(option),
            argument => new TerminalSetupProblem.UnexpectedArgument(argument));
        if (problem is not null)
        {
            return TerminalSetupParse.Refused(problem);
        }

        TerminalSetupRequest request = new(CommandLineSyntax.Last(parsed, Sound), CommandLineSyntax.Last(parsed, SessionSound), parsed.GetValue(Yes), parsed.GetValue(Remove));
        if (request.Remove && (request.Sound is not null || request.SessionSound is not null))
        {
            return TerminalSetupParse.Refused(new TerminalSetupProblem.RemoveWithSound());
        }

        return new TerminalSetupParse(request, Problem: null);
    }
}