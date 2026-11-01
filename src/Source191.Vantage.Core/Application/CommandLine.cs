using Source191.Vantage.Application.Launch;
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
        RootCommand root = new($"Mission control for Claude Code sessions: every session in one tab, still there after a reboot.\n{Wording.SupportLine}") { Here(actions), SetupTerminal(actions), ClaimTab(actions) };
        root.SetAction((_, cancellationToken) => actions.OpenConsole(cancellationToken));

        return root;
    }

    /// <summary>
    /// Parses and runs. The library's termination handler is off
    /// (<see href="https://learn.microsoft.com/dotnet/api/system.commandline.invocationconfiguration.processterminationtimeout"><c>ProcessTerminationTimeout</c></see>
    /// null): it would cancel the action on Ctrl+C and force an exit two seconds later, even under an attached session. When the parse
    /// has errors the action never runs, so an option-like name is named here first and the library's errors follow.
    /// </summary>
    public static async Task<int> InvokeAsync(RootCommand root, IReadOnlyList<string> arguments, TextWriter output, TextWriter error)
    {
        InvocationConfiguration configuration = new() { Output = output, Error = error, ProcessTerminationTimeout = null };
        ParseResult parsed = root.Parse(arguments);
        if (parsed.Errors.Count > 0 && OptionLikeName(parsed) is string token)
        {
            await error.WriteLineAsync(Wording.NameLooksLikeAnOption(token));
        }

        return await parsed.InvokeAsync(configuration);
    }

    private static Command Here(CommandActions actions)
    {
        Argument<string?> name = new("name") { Arity = ArgumentArity.ZeroOrOne, Description = "The session's name; the folder's name when left out." };
        Option<string?> prompt = new("--prompt", "-p") { Description = "An opening prompt to send; without one the session waits at its prompt." };
        Command here = new("here", "Start a supervised Claude Code session in this folder, on its current branch, and attach to it in this tab. Ctrl+Z detaches; the session keeps running.") { name, prompt };
        here.SetAction((parsed, cancellationToken) => OptionLikeName(parsed) is string token
            ? RefuseAsync(parsed, token)
            : actions.StartHere(NameOf(parsed.GetValue(name)), parsed.GetValue(prompt) ?? string.Empty, cancellationToken));

        return here;
    }

    /// <summary>The name as typed, or the folder's name when it was left off.</summary>
    private static HereName NameOf(string? value) => value is null ? new HereName.FolderName() : new HereName.Typed(value);

    /// <summary>
    /// The name when it starts with <c>-</c> and no <c>--</c> came before it: an unknown option fills the free name slot,
    /// so this is most likely a mistyped one. Only the parse result's token list holds the <c>--</c>; a validator on the
    /// argument cannot see it.
    /// </summary>
    private static string? OptionLikeName(ParseResult parsed)
    {
        if (parsed.CommandResult.Command is not { Name: "here", Arguments: [Argument name] }
            || parsed.GetResult(name)?.Tokens is not [Token token]
            || !token.Value.StartsWith('-'))
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
        setup.SetAction((parsed, cancellationToken) => actions.SetUpTerminal(new TerminalSetupRequest(SoundOf(parsed.GetValue(sound)), SoundOf(parsed.GetValue(sessionSound)), parsed.GetValue(yes), parsed.GetValue(remove)), cancellationToken));

        return setup;
    }

    /// <summary>A sound option as typed, or omitted when the option was left off.</summary>
    private static SoundArgument SoundOf(string? value) => value is null ? new SoundArgument.Omitted() : new SoundArgument.Given(value);

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