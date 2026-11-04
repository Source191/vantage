using Source191.Torni.Adapters.Packages;
using Source191.Torni.Ports;

namespace Source191.Torni.Infrastructure.Tests;

/// <summary>The changelog beside the tool, with a temporary folder standing for the installed tool's.</summary>
public sealed class PackagedReleaseNotesShould : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"torni-tool-{Guid.NewGuid():N}");

    [Fact]
    public async Task ReadTheChangelogBesideTheTool()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "CHANGELOG.md"), "## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n", TestContext.Current.CancellationToken);

        PackageNotes notes = await new PackagedReleaseNotes(folder).ReadAsync();

        notes.ShouldBe(new PackageNotes.Read("## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n"));
    }

    [Fact]
    public async Task LearnNothingWhenTheChangelogIsMissing()
    {
        (await new PackagedReleaseNotes(folder).ReadAsync()).ShouldBe(new PackageNotes.NothingLearned());
    }

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}