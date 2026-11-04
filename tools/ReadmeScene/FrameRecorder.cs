using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.Versioning;

namespace Source191.Torni.ReadmeScene;

/// <summary>Captures the scene's window at each of a schedule's offsets, saving <c>frame-00000.png</c> onwards; a capture that runs late is taken at once.</summary>
[SupportedOSPlatform("windows")]
internal static class FrameRecorder
{
    public static async Task RecordAsync(nint window, string folder, IReadOnlyList<TimeSpan> offsets, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folder);
        DateTime started = DateTime.UtcNow;
        List<Task> saves = [];
        for (int frame = 0; frame < offsets.Count; frame++)
        {
            TimeSpan wait = started + offsets[frame] - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken);
            }

            Bitmap bitmap = WindowCapture.Capture(window);
            string file = Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"frame-{frame:D5}.png"));
            saves.Add(Task.Run(
                () =>
                {
                    using (bitmap)
                    {
                        bitmap.Save(file, ImageFormat.Png);
                    }
                },
                cancellationToken));
        }

        await Task.WhenAll(saves);
    }
}