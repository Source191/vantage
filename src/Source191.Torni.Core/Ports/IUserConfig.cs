namespace Source191.Torni.Ports;

/// <summary>User configuration (config.json): <see cref="UserConfig.Missing"/> when there is no file or it cannot be read.</summary>
public interface IUserConfig
{
    Task<UserConfig> ReadConfigAsync();

    Task WriteConfigAsync(UserConfig config);
}