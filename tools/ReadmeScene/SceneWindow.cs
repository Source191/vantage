namespace Source191.Torni.ReadmeScene;

/// <summary>
/// The Windows Terminal window the scene runs in. Windows Terminal titles a window after its active tab, so the host
/// titles its own tab with a token no other window carries, finds the window showing it, and keeps its handle.
/// </summary>
internal static class SceneWindow
{
    private const string TerminalClass = "CASCADIA_HOSTING_WINDOW_CLASS";

    public static nint Find(TimeSpan patience)
    {
        string token = $"readme-scene-{Guid.NewGuid():N}";
        Console.Write($"\u001b]2;{token}\u0007");
        DateTime giveUp = DateTime.UtcNow + patience;
        while (DateTime.UtcNow < giveUp)
        {
            nint found = Titled(token);
            if (found != 0)
            {
                return found;
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException("The scene's Windows Terminal window was not found; run the scene in Windows Terminal.");
    }

    private static nint Titled(string title)
    {
        nint found = 0;
        NativeMethods.EnumWindows(
            (window, _) =>
            {
                if (NativeMethods.TextOf(window, NativeMethods.GetClassName) == TerminalClass && NativeMethods.TextOf(window, NativeMethods.GetWindowText) == title)
                {
                    found = window;
                    return false;
                }

                return true;
            },
            0);

        return found;
    }
}