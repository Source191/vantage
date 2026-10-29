using System.Reflection;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// The name of something compiled into Vantage or the libraries it runs: a type (an effect, an outcome, an exception) or
/// a method in a stack frame. It can only be built from reflection, never from text, so a diagnostic record holding one
/// cannot hold anything a person typed or a program printed.
/// </summary>
public sealed record CodeName
{
    private readonly string text;

    private CodeName(string text) => this.text = text;

    public static CodeName For(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return new CodeName(TypeName(type));
    }

    /// <summary>Names a compiler-generated method (an async state machine's MoveNext, a lambda) by the method it was written in.</summary>
    public static CodeName For(MethodBase method)
    {
        ArgumentNullException.ThrowIfNull(method);
        string methodName = Unmangled(method.Name);
        Type? type = method.DeclaringType;
        while (type is not null && IsGenerated(type.Name))
        {
            string written = Unmangled(type.Name);
            methodName = written.Length > 0 ? written : methodName;
            type = type.DeclaringType;
        }

        return new CodeName(type is null ? methodName : $"{TypeName(type)}.{methodName}");
    }

    public override string ToString() => text;

    private static string TypeName(Type type)
    {
        string name = type.Name.Split('`')[0];

        return type.DeclaringType is null ? name : $"{TypeName(type.DeclaringType)}.{name}";
    }

    private static bool IsGenerated(string name) => name.StartsWith('<');

    private static string Unmangled(string name)
    {
        int end = name.IndexOf('>', StringComparison.Ordinal);

        return IsGenerated(name) && end > 0 ? name[1..end] : name;
    }
}
