using System.Diagnostics;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// What a failure looked like without anything it said: the type of the exception and of up to three inner exceptions,
/// each with up to twenty stack frames as method names and the source file's name and line. The message, the data and
/// the folders of source paths are never read, because they can hold paths, names and program output.
/// </summary>
public sealed record ExceptionSummary
{
    private const int MostFrames = 20;
    private const int DeepestInner = 3;
    private static readonly string[] AwaitMachinery = ["System.Runtime.CompilerServices", "System.Runtime.ExceptionServices"];

    private readonly string text;

    private ExceptionSummary(string text) => this.text = text;

    public static ExceptionSummary From(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        List<string> parts = [];
        for (Exception? current = exception; current is not null && parts.Count <= DeepestInner; current = current.InnerException)
        {
            parts.Add(Part(current));
        }

        return new ExceptionSummary(string.Join("; inner ", parts));
    }

    public override string ToString() => text;

    private static string Part(Exception exception)
    {
        string type = CodeName.For(exception.GetType()).ToString();
        List<string> frames = [.. Frames(exception).Take(MostFrames)];

        return frames.Count == 0 ? type : $"{type} at {string.Join(" < ", frames)}";
    }

    private static IEnumerable<string> Frames(Exception exception) => new StackTrace(exception, fNeedFileInfo: true).GetFrames()
        .Where(frame => frame.GetMethod() is not null && !IsAwaitMachinery(frame))
        .Select(Frame);

    private static bool IsAwaitMachinery(StackFrame frame)
    {
        string space = frame.GetMethod()!.DeclaringType?.Namespace ?? string.Empty;

        return AwaitMachinery.Any(machinery => space.StartsWith(machinery, StringComparison.Ordinal));
    }

    private static string Frame(StackFrame frame)
    {
        string method = CodeName.For(frame.GetMethod()!).ToString();
        string? file = frame.GetFileName();
        int line = frame.GetFileLineNumber();

        return (file, line) switch
        {
            (null, _) => method,
            (_, 0) => $"{method} ({Path.GetFileName(file)})",
            _ => $"{method} ({Path.GetFileName(file)}:{line})",
        };
    }
}