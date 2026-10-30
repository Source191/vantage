using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class RemoveRepositoryRootHandlerShould
{
    private const string Repos = @"C:\Users\me\source\repos";
    private const string Work = @"D:\work";

    [Fact]
    public async Task SaveTheSettingsWithoutTheRoot()
    {
        InMemoryState state = new();
        CurrentSettings current = new(TestSettings.For(Repos, Work), state);

        object outcome = await new RemoveRepositoryRootHandler(current).HandleAsync(new RemoveRepositoryRoot(1), CancellationToken.None);

        outcome.ShouldBe(new RemoveRepositoryRootOutcome.Removed(FolderPath.From(Work)));
        current.Value.RepositoryRoots.ShouldBe([FolderPath.From(Repos)]);
        SettingsFile.From(await state.ReadConfigAsync(), TestSettings.Home).RepositoryRoots.ShouldBe([FolderPath.From(Repos)]);
    }

    [Fact]
    public async Task KeepTheOnlyRoot()
    {
        InMemoryState state = new();
        CurrentSettings current = new(TestSettings.For(Repos), state);

        object outcome = await new RemoveRepositoryRootHandler(current).HandleAsync(new RemoveRepositoryRoot(0), CancellationToken.None);

        outcome.ShouldBe(new RemoveRepositoryRootOutcome.OnlyRoot());
        current.Value.RepositoryRoots.ShouldBe([FolderPath.From(Repos)]);
        (await state.ReadConfigAsync()).ShouldBeNull();
    }
}