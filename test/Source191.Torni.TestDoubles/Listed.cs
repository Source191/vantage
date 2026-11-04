using Source191.Torni.Domain;

namespace Source191.Torni.TestDoubles;

/// <summary>The listed names the tests share.</summary>
public static class Listed
{
    public static ListedName As(string name) => new ListedName.Named(SessionName.From(name));

    public static ListedName Of(string name) => SessionName.TryParse(name, out SessionName? parsed) ? new ListedName.Named(parsed) : new ListedName.Unnamed();

    public static ListedName None() => new ListedName.Unnamed();
}