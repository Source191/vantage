using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The released version of Vantage that last ran, so the first start after an update says what changed, once.</summary>
public interface ILastRunVersion
{
    Task<LastRun> ReadLastRunAsync();

    Task WriteLastRunAsync(ReleaseVersion version);
}