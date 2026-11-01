namespace Source191.Vantage.Ports;

/// <summary>The repositories sessions were last launched in, most recent first, capped at ten.</summary>
public interface IRecentRepositories
{
    Task<IReadOnlyList<string>> ReadRecentRepositoriesAsync();

    Task RecordRecentRepositoryAsync(string repository);
}