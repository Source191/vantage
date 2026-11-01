using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Knows the editors it was told are installed.</summary>
public sealed class FakeEditorLocator : IEditorLocator
{
    private readonly Dictionary<EditorPreset, string> installed = [];

    public FakeEditorLocator Install(EditorPreset preset, string executable)
    {
        installed[preset] = executable;

        return this;
    }

    public Task<ProgramLocation> LocateAsync(EditorPreset preset, CancellationToken cancellationToken)
    {
        return Task.FromResult<ProgramLocation>(installed.TryGetValue(preset, out string? executable) ? new ProgramLocation.Found(executable) : new ProgramLocation.Missing());
    }
}