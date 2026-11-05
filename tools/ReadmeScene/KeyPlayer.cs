using Source191.Torni.Ports;

namespace Source191.Torni.ReadmeScene;

/// <summary>Plays a key script into the scene's console: keys through <see cref="ConsoleKeys"/>, focus through <c>wt focus-tab</c>, marks as files.</summary>
internal sealed class KeyPlayer(SceneFolders folders, IProcessRunner processes, SceneExecutables executables)
{
    public async Task PlayAsync(IReadOnlyList<KeyStep> steps, CancellationToken cancellationToken)
    {
        foreach (KeyStep step in steps)
        {
            await Task.Delay(step.Delay, cancellationToken);
            await TakeAsync(step, cancellationToken);
        }
    }

    private async Task TakeAsync(KeyStep step, CancellationToken cancellationToken)
    {
        switch (step)
        {
            case KeyStep.Type type:
                foreach (char character in type.Text)
                {
                    ConsoleKeys.Type(character);
                    await Task.Delay(KeyScript.TypingInterval, cancellationToken);
                }

                break;
            case KeyStep.Press press:
                ConsoleKeys.Press(press.Key);
                break;
            case KeyStep.FocusOwnTab:
                // The console's own tab is the first in the scene's window.
                await processes.RunAsync(new ProcessRequest(executables.Terminal, ["-w", executables.WindowName, "focus-tab", "--target", "0"], WorkingFolder.Inherit), cancellationToken);
                break;
            case KeyStep.Mark mark:
                await File.WriteAllTextAsync(Path.Combine(folders.Root, $"{mark.Name}.mark"), string.Empty, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"A key step the player does not know: {step}");
        }
    }
}