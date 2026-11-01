using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Which editor presets cannot be found on this machine, for the Open folder with dialog's marks. VS Code, Visual Studio
/// (whose look runs vswhere) and Rider are looked for together; Explorer's answer is only a path built from the Windows
/// folder and Custom is the user's own executable, so neither is looked for or marked.
/// </summary>
public sealed class EditorPresets(IEditorLocator locator)
{
    private static readonly EditorPreset[] LookedFor = [EditorPreset.VsCode, EditorPreset.VisualStudio, EditorPreset.Rider];

    private readonly IEditorLocator locator = locator ?? throw new ArgumentNullException(nameof(locator));

    public async Task<IReadOnlySet<EditorPreset>> MissingAsync(CancellationToken cancellationToken)
    {
        ProgramLocation[] locations = await Task.WhenAll(LookedFor.Select(preset => locator.LocateAsync(preset, cancellationToken)));

        return LookedFor.Where((_, index) => locations[index] is ProgramLocation.Missing).ToHashSet();
    }
}
