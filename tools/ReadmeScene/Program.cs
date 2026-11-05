using Source191.Torni.Adapters.Processes;
using Source191.Torni.Adapters.State;
using Source191.Torni.Application.Actions;
using Source191.Torni.Domain;
using System.Globalization;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Makes the README's screenshot and animation: the real console screen over a made-up scene of Northwind.*
/// repositories, with scripted Claude Code, gh and az answers, run by <c>tools\New-ReadmeScreenshot.ps1</c>.
/// <code>
///   readme-scene run &lt;scene-root&gt; &lt;torni-version&gt; [--frames &lt;folder&gt;] [key-script...]
///       the console over the scene: opens its tabs, then plays the key scripts in turn, capturing frames while they play
///   readme-scene attach &lt;job-id&gt;                       what a session tab shows in place of claude attach
///   readme-scene claim-tab &lt;registration&gt; &lt;pid&gt;       what a session tab runs in place of torni claim-tab
///   readme-scene capture &lt;scene-root&gt; &lt;png&gt;          the scene's window, as it looks now
/// </code>
/// </summary>
public static class Program
{
    /// <summary>Names the scene root for the tabs the console opens, which inherit the host's environment.</summary>
    public const string RootVariable = "READMESCENE_ROOT";

    public static async Task<int> Main(string[] args) => args switch
    {
        ["run", string root, string version, "--frames", string frames, .. string[] scripts] => await RunAsync(new SceneFolders(Path.GetFullPath(root)), ToolVersion.FromInformational(version), Path.GetFullPath(frames), scripts),
        ["run", string root, string version, .. string[] scripts] => await RunAsync(new SceneFolders(Path.GetFullPath(root)), ToolVersion.FromInformational(version), string.Empty, scripts),
        ["attach", string jobId] => Attach(jobId),
        ["claim-tab", string registration, string processId] => await ClaimAsync(registration, processId),
        ["capture", string root, string file] when OperatingSystem.IsWindows() => Capture(new SceneFolders(Path.GetFullPath(root)), file),
        _ => Usage(),
    };

    private static async Task<int> RunAsync(SceneFolders folders, ToolVersion version, string frames, IReadOnlyList<string> scripts)
    {
        Environment.SetEnvironmentVariable(RootVariable, folders.Root);
        await File.WriteAllTextAsync(folders.WindowFile, SceneWindow.Find(TimeSpan.FromSeconds(10)).ToString(CultureInfo.InvariantCulture));
        Scene scene = SceneFile.Read(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "scene.json")));

        List<KeyStep> recorded = [];
        foreach (string script in scripts)
        {
            recorded.AddRange(KeyScript.Parse(await File.ReadAllTextAsync(script)));
        }

        return await new SceneHost(folders, scene, version, new ScenePlay(SceneKeys.OpenTabs(scene), recorded, frames)).RunAsync(CancellationToken.None);
    }

    // Exits 0 once the script asks the scene to close, so the tab's shell exits and Windows Terminal closes the tab.
    private static int Attach(string jobId)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"README scene: Claude Code would be attached to {jobId} here.");
        Console.ResetColor();
        string? root = Environment.GetEnvironmentVariable(RootVariable);
        while (root is null || !File.Exists(new SceneFolders(root).CloseFile))
        {
            Thread.Sleep(250);
        }

        return 0;
    }

    private static async Task<int> ClaimAsync(string registration, string processId)
    {
        string? root = Environment.GetEnvironmentVariable(RootVariable);
        if (root is null || !Guid.TryParseExact(registration, "N", out Guid id) || !int.TryParse(processId, NumberStyles.None, CultureInfo.InvariantCulture, out int shell))
        {
            return 1;
        }

        await new TabClaimer(new JsonFileStore(new SceneFolders(root).State, TimeProvider.System), new ProcessRegistry()).ClaimAsync(id, shell);

        return 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static int Capture(SceneFolders folders, string file)
    {
        WindowCapture.ToPng(nint.Parse(File.ReadAllText(folders.WindowFile), CultureInfo.InvariantCulture), Path.GetFullPath(file));

        return 0;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: readme-scene run <scene-root> <torni-version> [--frames <folder>] [key-script...] | attach <job-id> | claim-tab <registration> <pid>");

        return 2;
    }
}