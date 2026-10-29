using Source191.Vantage.Application.TerminalSetup;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;

namespace Source191.Vantage.Application;

/// <summary>
/// Vantage's whole command line, declared once with System.CommandLine: the console as the root action, <c>here</c>,
/// <c>setup-terminal</c> and the hidden <c>claim-tab</c>. Help, version, parse errors and close-match suggestions are
/// the library's; each command hands its values to the matching <see cref="CommandActions"/> delegate. Descriptions
/// sit beside their symbols rather than in <see cref="Wording"/>, so an option and its words are changed together; the
/// root's support line is the exception, read from <see cref="Wording.SupportLine"/> because its URL is a fact with one home.
/// </summary>
public static class CommandLine
{
    public static RootCommand Build(CommandActions actions)
    {
        RootCommand root = new($"A console for Claude Code sessions and git worktrees on Windows.\n{Wording.SupportLine}") { Here(actions), SetupTerminal(actions), ClaimTab(actions) };
        root.SetAction((_, cancellationToken) => actions.OpenConsole(cancellationToken));

        return root;
    }

    /// <summary>
    /// Parses and runs. The library's termination handler is off (<c>ProcessTerminationTimeout</c> null): it would
    /// cancel the action on Ctrl+C and force an exit two seconds later, even under an attached session.
    /// </summary>
    public static Task<int> InvokeAsync(RootCommand root, IReadOnlyList<string> arguments, TextWriter output, TextWriter error)
    {
        InvocationConfiguration configuration = new() { Output = output, Error = error, ProcessTerminationTimeout = null };

        return root.Parse(arguments).InvokeAsync(configuration);
    }

    private static Command Here(CommandActions actions)
    {
        Argument<string?> name = new("name") { Arity = ArgumentArity.ZeroOrOne, Description = "The session's name; the folder's name when left out." };
        Option<string?> prompt = new("--prompt", "-p") { Description = "An opening prompt to send; without one the session opens on Claude's agent list." };
        Command here = new("here", "Start a supervised Claude Code session in this folder, on its current branch, and attach to it in this tab. Ctrl+Z detaches; the session keeps running.") { name, prompt };
        here.SetAction((parsed, cancellationToken) => OptionLikeName(parsed, name) is string token
            ? RefuseAsync(parsed, token)
            : actions.StartHere(parsed.GetValue(name), parsed.GetValue(prompt), cancellationToken));

        return here;
    }

    /// <summary>
    /// The name when it starts with <c>-</c> and no <c>--</c> came before it: an unknown option fills the free name slot,
    /// so this is most likely a mistyped one. Only the parse result's token list holds the <c>--</c>; a validator on the
    /// argument cannot see it.
    /// </summary>
    private static string? OptionLikeName(ParseResult parsed, Argument<string?> name)
    {
        if (parsed.GetResult(name)?.Tokens is not [Token token] || !token.Value.StartsWith('-'))
        {
            return null;
        }

        bool afterDoubleDash = parsed.Tokens.TakeWhile(candidate => !ReferenceEquals(candidate, token)).Any(candidate => candidate.Type == TokenType.DoubleDash);

        return afterDoubleDash ? null : token.Value;
    }

    /// <summary>Refused the way the library refuses a parse error: the sentence, then the command's help, exit code 1.</summary>
    private static async Task<int> RefuseAsync(ParseResult parsed, string token)
    {
        await parsed.InvocationConfiguration.Error.WriteLineAsync(Wording.NameLooksLikeAnOption(token));
        await parsed.InvocationConfiguration.Error.WriteLineAsync();
        new HelpAction().Invoke(parsed);

        return 1;
    }

    private static Command SetupTerminal(CommandActions actions)
    {
        Option<string?> sound = new("--sound") { HelpName = "wav", Description = "The bell sound of the Vantage profile." };
        Option<string?> sessionSound = new("--session-sound") { HelpName = "wav", Description = "The bell sound of the Vantage session profile." };
        Option<bool> yes = new("--yes") { Description = "Write without asking." };
        Option<bool> remove = new("--remove") { Description = "Remove Vantage's profiles." };
        Command setup = new("setup-terminal", "Add the Vantage and Vantage session profiles to Windows Terminal, each with a bell that sounds and flashes, then offer to turn on Claude Code's terminal bell. Shows what it will write and asks first.") { sound, sessionSound, yes, remove };
        setup.SetAction((parsed, cancellationToken) => actions.SetUpTerminal(new TerminalSetupRequest(parsed.GetValue(sound), parsed.GetValue(sessionSound), parsed.GetValue(yes), parsed.GetValue(remove)), cancellationToken));

        return setup;
    }

    private static Command ClaimTab(CommandActions actions)
    {
        Argument<Guid> registration = new("registration-id");
        Argument<int> processId = new("process-id");
        Command claim = new("claim-tab", "Run by the shell inside a tab Vantage opened, to claim that tab.") { registration, processId };
        claim.Hidden = true;
        claim.SetAction((parsed, cancellationToken) => actions.ClaimTab(parsed.GetValue(registration), parsed.GetValue(processId), cancellationToken));

        return claim;
    }
}