using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.TestDoubles;

public sealed class InMemoryStateShould
{
    private readonly InMemoryState state = new();

    [Fact]
    public async Task KeepTabsByIdAndRemoveThem()
    {
        TabRegistration pending = TabRegistration.Pending(new TabOwner.ForSession(SessionId.From("session-1")), TabPalette.Default[0], Fixtures.Noon);
        await state.WriteTabAsync(pending);
        await state.WriteTabAsync(pending.ClaimedBy(4242, Fixtures.Noon));

        (await state.ReadTabsAsync()).Single().Claim.ShouldBe(new TabClaim.Claimed(4242, Fixtures.Noon));
        await state.RemoveTabAsync(pending.Id);
        (await state.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepRecentRepositoriesMostRecentFirstWithoutDuplicatesAndCappedAtTen()
    {
        foreach (int index in Enumerable.Range(1, 11))
        {
            await state.RecordRecentRepositoryAsync(FolderPath.From($@"C:\repos\R{index}"));
        }

        await state.RecordRecentRepositoryAsync(FolderPath.From(@"C:\repos\r2"));

        IReadOnlyList<FolderPath> recent = await state.ReadRecentRepositoriesAsync();
        recent.Count.ShouldBe(10);
        recent[0].ShouldBe(FolderPath.From(@"C:\repos\r2"));
        recent.ShouldNotContain(FolderPath.From(@"C:\repos\R1"));
    }

    [Fact]
    public async Task KeepOneCreatedWorktreePerPathAndForgetIt()
    {
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(@"C:\repos\A-fix"), BranchName.From("fix"), FolderPath.From(@"C:\repos\A"), Fixtures.Noon));
        await state.RecordCreatedWorktreeAsync(new CreatedWorktree(FolderPath.From(@"C:\repos\a-fix"), BranchName.From("fix-2"), FolderPath.From(@"C:\repos\A"), Fixtures.Noon));

        (await state.ReadCreatedWorktreesAsync()).Single().Branch.ShouldBe(BranchName.From("fix-2"));
        await state.ForgetCreatedWorktreeAsync(FolderPath.From(@"C:\repos\A-FIX"));
        (await state.ReadCreatedWorktreesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task StartWithAMissingConfiguration()
    {
        (await state.ReadConfigAsync()).ShouldBe(TorniConfig.Missing);
    }

    [Fact]
    public async Task KeepTheConfiguration()
    {
        await state.WriteConfigAsync(new TorniConfig([@"D:\work"], WorktreeBase.Head, WorktreeRoot: null, Editor: null));

        (await state.ReadConfigAsync()).DefaultBase.ShouldBe(WorktreeBase.Head);
    }

    [Fact]
    public async Task SurviveTabsWrittenAndRemovedFromManyThreads()
    {
        TabRegistration[] registrations = [.. Enumerable.Range(1, 400).Select(index => TabRegistration.Pending(new TabOwner.ForSession(SessionId.From($"session-{index}")), TabPalette.Default[0], Fixtures.Noon))];

        await Parallel.ForEachAsync(registrations, TestContext.Current.CancellationToken, async (registration, _) =>
        {
            await state.WriteTabAsync(registration);
            await state.ReadTabsAsync();
            if (registration.Id.GetHashCode() % 2 == 0)
            {
                await state.RemoveTabAsync(registration.Id);
            }
        });

        IReadOnlyList<TabRegistration> remaining = await state.ReadTabsAsync();
        remaining.Count.ShouldBe(registrations.Count(registration => registration.Id.GetHashCode() % 2 != 0));
    }
}