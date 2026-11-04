using Source191.Torni.Domain;

namespace Source191.Torni.Ports;

/// <summary>The repositories sessions were last launched in, most recent first, capped at ten.</summary>
public interface IRecentRepositories
{
    Task<IReadOnlyList<FolderPath>> ReadRecentRepositoriesAsync();

    Task RecordRecentRepositoryAsync(FolderPath repository);
}