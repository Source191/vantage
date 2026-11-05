using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>The filter's text for a fact a row may not have: a head on a branch gives the branch's name, and anything else adds no field.</summary>
internal static class FilterText
{
    public static IReadOnlyList<string> Of(ListedName name) => name is ListedName.Named named ? [named.Name.Value] : [];

    public static IReadOnlyList<string> Of(Head head) => head is Head.OnBranch on ? [on.Branch.Value] : [];
}