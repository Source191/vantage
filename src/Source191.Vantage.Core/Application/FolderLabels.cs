using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>
/// The text of a FOLDER cell. The roots are asked for on every call rather than copied, so a change to the
/// configuration shows on the next redraw without touching the rows.
/// </summary>
public sealed class FolderLabels(Func<IReadOnlyList<FolderPath>> roots, FolderPath home)
{
    public string For(FolderPath folder) => FolderLabel.For(folder, roots(), home);
}