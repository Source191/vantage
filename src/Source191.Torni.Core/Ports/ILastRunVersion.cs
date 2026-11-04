using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>The released version of Torni that last ran, so the first start after an update says what changed, once.</summary>
public interface ILastRunVersion
{
    Task<LastRun> ReadLastRunAsync();

    Task WriteLastRunAsync(ReleaseVersion version);
}