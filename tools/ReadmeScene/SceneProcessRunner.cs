using Source191.Torni.Ports;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Answers Claude Code, gh and az from the scene and hands every other program (git, wt, pwsh) to the real runner,
/// sending Windows Terminal's tabs to the scene's own window. Nothing the scene does not script is ever run as Claude Code:
/// stop, rm, attach and the rest fail with exit code 1.
/// </summary>
public sealed class SceneProcessRunner(SceneListing listing, ScenePullRequests pullRequests, SceneExecutables executables, IProcessRunner others) : IProcessRunner
{
    private const int Refused = 1;

    public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        if (Is(request, executables.Claude))
        {
            return Task.FromResult(Claude(request));
        }

        if (Is(request, executables.GitHub))
        {
            return Task.FromResult(GitHub(request.Arguments));
        }

        if (Is(request, executables.AzureDevOps))
        {
            return Task.FromResult(AzureDevOps(request.Arguments));
        }

        return others.RunAsync(Is(request, executables.Terminal) ? InSceneWindow(request) : request, cancellationToken);
    }

    public Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken) =>
        Is(request, executables.Claude) || Is(request, executables.GitHub) || Is(request, executables.AzureDevOps)
            ? Task.FromResult(Refused)
            : others.RunInteractiveAsync(request, cancellationToken);

    private ProcessResult Claude(ProcessRequest request) => request.Arguments switch
    {
        ["agents", "--json", "--all"] => Answer(listing.Json()),
        ["--version"] => Answer($"{listing.ClaudeVersion} (Claude Code)"),
        ["--bg", "-n", string name, ..] when request.WorkingDirectory is WorkingFolder.At folder => Answer($"Session backgrounded · {listing.Start(name, folder.Path)}"),
        _ => Refuse(request.Arguments),
    };

    private ProcessResult GitHub(IReadOnlyList<string> arguments) => arguments switch
    {
        ["pr", "list", "--repo", string repository, "--head", string branch, ..] => Answer(pullRequests.GitHubJson(repository, branch)),
        _ => Refuse(arguments),
    };

    private ProcessResult AzureDevOps(IReadOnlyList<string> arguments) => arguments switch
    {
        ["repos", "pr", "list", "--org", string organisation, "--project", string project, "--repository", string name, "--source-branch", string branch, ..] =>
            Answer(pullRequests.AzureDevOpsJson(organisation[(organisation.LastIndexOf('/') + 1)..], project, name, branch)),
        _ => Refuse(arguments),
    };

    // Torni opens tabs in the window used last (-w 0), which need not be the scene's while someone works elsewhere.
    private ProcessRequest InSceneWindow(ProcessRequest request) => request.Arguments switch
    {
        ["-w", "0", ..] => request with { Arguments = ["-w", executables.WindowName, .. request.Arguments.Skip(2)] },
        _ => request,
    };

    private static bool Is(ProcessRequest request, string executable) => string.Equals(request.FileName, executable, StringComparison.OrdinalIgnoreCase);

    private static ProcessResult Answer(string output) => new(0, output, string.Empty);

    private static ProcessResult Refuse(IReadOnlyList<string> arguments) => new(Refused, string.Empty, $"The README scene does not script \"{string.Join(' ', arguments)}\".");
}