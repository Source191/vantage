using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application;

/// <summary>
/// Which editor presets cannot be found on this machine, for the Open folder with dialog's marks. VS Code, Visual Studio
/// (whose look runs vswhere) and Rider are looked for together; Explorer's answer is only a path built from the Windows
/// folder and Custom is the user's own executable, so neither is looked for or marked.
/// </summary>
public sealed class EditorPresets(IEditorLocator locator)
{
    private static readonly EditorPreset[] LookedFor = [EditorPreset.VsCode, EditorPreset.VisualStudio, EditorPreset.Rider];

    public async Task<IReadOnlySet<EditorPreset>> MissingAsync(CancellationToken cancellationToken)
    {
        ProgramLocation[] locations = await Task.WhenAll(LookedFor.Select(preset => locator.LocateAsync(preset, cancellationToken)));

        return LookedFor.Where((_, index) => locations[index] is ProgramLocation.Missing).ToHashSet();
    }
}