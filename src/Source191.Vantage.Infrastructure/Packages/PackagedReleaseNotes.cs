using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Packages;

/// <summary>
/// <c>CHANGELOG.md</c> beside the tool (<see cref="AppContext.BaseDirectory"/> in the installed tool), where packing puts
/// it; a missing or unreadable file learns nothing.
/// </summary>
public sealed class PackagedReleaseNotes(string folder) : IPackagedReleaseNotes
{
    public const string FileName = "CHANGELOG.md";

    private readonly string path = Path.Combine(folder ?? throw new ArgumentNullException(nameof(folder)), FileName);

    public async Task<PackageNotes> ReadAsync()
    {
        try
        {
            return new PackageNotes.Read(await File.ReadAllTextAsync(path));
        }
        catch (IOException)
        {
            return new PackageNotes.NothingLearned();
        }
        catch (UnauthorizedAccessException)
        {
            return new PackageNotes.NothingLearned();
        }
    }
}