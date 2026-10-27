namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The package icon is a copy of the 128 px asset; the two must not drift.</summary>
public sealed class IconAssetsShould
{
    [Fact]
    public async Task KeepThePackageIconInStepWithTheAssetFolder()
    {
        string root = RepositoryFolder.Root();

        byte[] packageIcon = await File.ReadAllBytesAsync(Path.Combine(root, "src", "Source191.Vantage", "icon.png"), TestContext.Current.CancellationToken);
        byte[] asset = await File.ReadAllBytesAsync(Path.Combine(root, "assets", "icon", "vantage-128.png"), TestContext.Current.CancellationToken);

        packageIcon.ShouldBe(asset);
    }
}