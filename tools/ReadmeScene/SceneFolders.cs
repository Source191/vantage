namespace Source191.Torni.ReadmeScene;

/// <summary>
/// The scene's folders under its root: <c>me</c> is the home the console is given (so labels read <c>~\source\repos</c>
/// and no real user name shows), <c>state</c> stands in for <c>%LOCALAPPDATA%\Torni</c>, <c>claude</c> for
/// <c>~\.claude</c>, and <c>bin</c> holds what the tool would carry beside itself.
/// </summary>
public sealed record SceneFolders(string Root)
{
    public string Home => Path.Combine(Root, "me");

    public string State => Path.Combine(Root, "state");

    public string ClaudeHome => Path.Combine(Root, "claude");

    public string Bin => Path.Combine(Root, "bin");

    /// <summary>The handle of the scene's Windows Terminal window, written by the running console for the captures.</summary>
    public string WindowFile => Path.Combine(Root, "window.txt");

    /// <summary>Written by the script when it is done: the console quits and every session tab ends cleanly, which closes the window.</summary>
    public string CloseFile => Path.Combine(Root, "close.mark");
}