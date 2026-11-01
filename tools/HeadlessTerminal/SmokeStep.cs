namespace Source191.Vantage.HeadlessTerminal;

/// <summary>One step of a marker-led script: wait up to <paramref name="Timeout"/> for the marker, then send the keys.</summary>
public sealed record SmokeStep(CaptureMarker Marker, string Keys, TimeSpan Timeout);