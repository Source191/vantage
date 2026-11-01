namespace Source191.Vantage.Ports;

/// <summary>User configuration (config.json): <see cref="VantageConfig.Missing"/> when there is no file or it cannot be read.</summary>
public interface IVantageConfig
{
    Task<VantageConfig> ReadConfigAsync();

    Task WriteConfigAsync(VantageConfig config);
}