using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.TestDoubles;

public sealed class InMemoryStateShould
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryState state = new();

    [Fact]
    public async Task KeepTabsByIdAndRemoveThem()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(new SessionId("session-1")), TabPalette.Default[0], Noon);
        await state.WriteTabAsync(pending);
        await state.WriteTabAsync(pending.ClaimedBy(4242, Noon));

        (await state.ReadTabsAsync()).Single().ProcessId.ShouldBe(4242);
        await state.RemoveTabAsync(pending.Id);
        (await state.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepRecentRepositoriesMostRecentFirstWithoutDuplicatesAndCappedAtTen()
    {
        foreach (int index in Enumerable.Range(1, 11))
        {
            await state.RecordRecentRepositoryAsync($@"C:\repos\R{index}");
        }

        await state.RecordRecentRepositoryAsync(@"C:\repos\r2");

        IReadOnlyList<string> recent = await state.ReadRecentRepositoriesAsync();
        recent.Count.ShouldBe(10);
        recent[0].ShouldBe(@"C:\repos\r2");
        recent.ShouldNotContain(@"C:\repos\R1");
    }

    [Fact]
    public async Task KeepOneCreatedWorktreePerPathAndForgetIt()
    {
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(new FolderPath(@"C:\repos\A-fix"), new BranchName("fix"), new FolderPath(@"C:\repos\A"), Noon));
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(new FolderPath(@"C:\repos\a-fix"), new BranchName("fix-2"), new FolderPath(@"C:\repos\A"), Noon));

        (await state.ReadCreatedWorktreesAsync()).Single().Branch.ShouldBe(new BranchName("fix-2"));
        await state.ForgetCreatedWorktreeAsync(new FolderPath(@"C:\repos\A-FIX"));
        (await state.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepTheConfiguration()
    {
        await state.WriteConfigAsync(new VantageConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null));

        (await state.ReadConfigAsync())!.DefaultBase.ShouldBe(WorktreeBase.Head);
    }
}