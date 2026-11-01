using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// Which program Vantage ran and its subcommand: up to three leading arguments, kept only while each is a word on that
/// program's fixed list, so no path, name, id or prompt among the arguments is ever kept. A value that happens to equal
/// a listed word reveals only that word.
/// </summary>
public sealed record ChildCommand
{
    private const int MostWords = 3;

    private static readonly Dictionary<string, ChildProgram> Programs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["git"] = ChildProgram.Git,
        ["claude"] = ChildProgram.Claude,
        ["gh"] = ChildProgram.Gh,
        ["az"] = ChildProgram.Az,
        ["pwsh"] = ChildProgram.Pwsh,
        ["wt"] = ChildProgram.Wt,
        ["vswhere"] = ChildProgram.Vswhere,
        ["dotnet"] = ChildProgram.Dotnet,
    };

    private readonly ChildProgram program;
    private readonly string words;

    private ChildCommand(ChildProgram program, string words)
    {
        this.program = program;
        this.words = words;
    }

    public static ChildCommand For(ProcessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ChildProgram program = Programs.GetValueOrDefault(Path.GetFileNameWithoutExtension(request.FileName), ChildProgram.Other);
        IReadOnlySet<string> listed = Words(program);

        return new ChildCommand(program, string.Join(' ', request.Arguments.TakeWhile(listed.Contains).Take(MostWords)));
    }

    public override string ToString()
    {
        string name = program is ChildProgram.Other ? "another program" : program.ToString().ToLowerInvariant();

        return words.Length == 0 ? name : $"{name} {words}";
    }

    private static IReadOnlySet<string> Words(ChildProgram program) => program switch
    {
        ChildProgram.Git => ChildCommandWords.Git,
        ChildProgram.Claude => ChildCommandWords.Claude,
        ChildProgram.Gh => ChildCommandWords.Gh,
        ChildProgram.Az => ChildCommandWords.Az,
        ChildProgram.Wt => ChildCommandWords.Wt,
        ChildProgram.Dotnet => ChildCommandWords.Dotnet,
        ChildProgram.Pwsh => ChildCommandWords.None,
        ChildProgram.Vswhere => ChildCommandWords.None,
        ChildProgram.Other => ChildCommandWords.None,
    };
}