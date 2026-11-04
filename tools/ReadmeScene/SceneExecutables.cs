namespace Source191.Torni.ReadmeScene;

/// <summary>
/// The paths the scene's programs answer for: this program standing in for Claude Code, the gh and az the
/// pull-request adapters are told about (never run), and Windows Terminal with the name of the scene's own window,
/// which every tab is sent to so none lands in another window.
/// </summary>
public sealed record SceneExecutables(string Claude, string GitHub, string AzureDevOps, string Terminal, string WindowName);