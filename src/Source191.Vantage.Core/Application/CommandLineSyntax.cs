using System.CommandLine;
using System.CommandLine.Parsing;

namespace Source191.Vantage.Application;

/// <summary>
/// System.CommandLine as Vantage uses it: only to parse. Vantage never invokes a parse result, so the library's help,
/// error messages and exit codes never reach the user; <see cref="FirstProblem"/> turns what it parsed into the
/// problem values each command already words. Response files and bundled short options are off, and the forms the
/// library accepts that Vantage refuses (<c>--prompt=text</c>, <c>-p:text</c>, <c>--</c>, an unknown option taken as
/// an argument) are refused here.
/// </summary>
public static class CommandLineSyntax
{
    private static readonly ParserConfiguration Configuration = new() { EnablePosixBundling = false, ResponseFileTokenReplacer = null };

    /// <summary>An option that takes one value and may be given again, the last one winning.</summary>
    public static Option<string[]> Repeatable(string name, params string[] aliases) => new(name, aliases) { Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = false };

    /// <summary>An option that takes no value.</summary>
    public static Option<bool> Flag(string name) => new(name) { Arity = ArgumentArity.Zero };

    public static ParseResult Parse(Command command, IReadOnlyList<string> arguments) => command.Parse(arguments, Configuration);

    /// <summary>The last value given for a repeatable option, or null when it was not given.</summary>
    public static string? Last(ParseResult parsed, Option<string[]> option) => parsed.GetValue(option) is [.., string last] ? last : null;

    /// <summary>
    /// The first thing wrong with the arguments, in the order they were typed, as the caller's problem type: an
    /// option Vantage does not know, an option missing its value, or an argument with nowhere to go. Null when
    /// nothing is wrong.
    /// </summary>
    public static TProblem? FirstProblem<TProblem>(
        Command command,
        ParseResult parsed,
        IReadOnlyList<string> arguments,
        Func<string, TProblem> unknownOption,
        Func<string, TProblem> optionNeedsValue,
        Func<string, TProblem> unexpectedArgument)
        where TProblem : class
    {
        HashSet<Token> optionValues = new(parsed.CommandResult.Children.OfType<OptionResult>().SelectMany(option => option.Tokens), ReferenceEqualityComparer.Instance);
        int room = command.Arguments.Sum(argument => argument.Arity.MaximumNumberOfValues);
        int placed = 0;

        for (int index = 0; index < parsed.Tokens.Count && index < arguments.Count; index++)
        {
            Token token = parsed.Tokens[index];
            string typed = arguments[index];
            bool valueFollows = index + 1 < parsed.Tokens.Count && optionValues.Contains(parsed.Tokens[index + 1]);
            TProblem? problem = token.Type switch
            {
                TokenType.DoubleDash => unknownOption(typed),
                TokenType.Option when token.Value != typed => unknownOption(typed),
                TokenType.Option when TakesValue(command, token.Value) && !valueFollows => optionNeedsValue(typed),
                TokenType.Argument when optionValues.Contains(token) => null,
                TokenType.Argument when typed.StartsWith('-') => unknownOption(typed),
                TokenType.Argument when ++placed > room => unexpectedArgument(typed),
                _ => null,
            };
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    private static bool TakesValue(Command command, string alias) => command.Options.First(option => option.Name == alias || option.Aliases.Contains(alias)).Arity.MinimumNumberOfValues > 0;
}
