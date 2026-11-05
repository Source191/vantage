namespace Source191.Torni.Ports;

/// <summary>
/// The <see href="https://learn.microsoft.com/windows/terminal/json-fragment-extensions">Windows Terminal JSON
/// fragment</see> that carries the console's profiles: a file Windows Terminal reads at start-up and that the user's own
/// settings file never sees. Implemented over System.IO at the boundary and faked in tests.
/// </summary>
public interface ITerminalFragment
{
    /// <summary>The fragment file's full path.</summary>
    string Location { get; }

    bool Exists { get; }

    Task WriteAsync(TerminalProfiles profiles, CancellationToken cancellationToken);

    /// <summary>Deletes the console's fragment folder and nothing else; false when there was nothing to delete.</summary>
    Task<bool> RemoveAsync(CancellationToken cancellationToken);
}