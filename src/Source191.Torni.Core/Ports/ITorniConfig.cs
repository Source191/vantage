namespace Source191.Torni.Ports;

/// <summary>User configuration (config.json): <see cref="TorniConfig.Missing"/> when there is no file or it cannot be read.</summary>
public interface ITorniConfig
{
    Task<TorniConfig> ReadConfigAsync();

    Task WriteConfigAsync(TorniConfig config);
}