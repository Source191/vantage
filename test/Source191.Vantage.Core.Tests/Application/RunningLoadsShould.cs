using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class RunningLoadsShould
{
    private readonly RunningLoads loads = new();

    [Fact]
    public void ReserveOnlyWhenNothingRuns()
    {
        bool first = loads.TryReserve();
        bool second = loads.TryReserve();

        first.ShouldBeTrue();
        second.ShouldBeFalse();
        loads.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task CountATrackedLoadUntilItCompletes()
    {
        TaskCompletionSource<string> rows = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> load = loads.Track(_ => rows.Task);

        loads.TryReserve();
        Task<string> loading = load(CancellationToken.None);
        bool runningWhileItWaits = loads.IsRunning;
        rows.SetResult("rows");
        string answered = await loading;

        runningWhileItWaits.ShouldBeTrue();
        answered.ShouldBe("rows");
        loads.IsRunning.ShouldBeFalse();
        loads.TryReserve().ShouldBeTrue();
    }

    [Fact]
    public async Task CountATrackedLoadUntilItFails()
    {
        TaskCompletionSource<string> rows = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> load = loads.Track(_ => rows.Task);

        loads.TryReserve();
        Task<string> loading = load(CancellationToken.None);
        bool runningWhileItWaits = loads.IsRunning;
        rows.SetException(new InvalidOperationException("git could not be started"));

        await Should.ThrowAsync<InvalidOperationException>(() => loading);
        runningWhileItWaits.ShouldBeTrue();
        loads.IsRunning.ShouldBeFalse();
        loads.TryReserve().ShouldBeTrue();
    }

    [Fact]
    public async Task CountALoadThatWasNotReserved()
    {
        TaskCompletionSource<string> rows = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> load = loads.Track(_ => rows.Task);

        Task<string> loading = load(CancellationToken.None);
        bool reservedWhileItWaits = loads.TryReserve();
        rows.SetResult("rows");
        await loading;

        reservedWhileItWaits.ShouldBeFalse();
        loads.TryReserve().ShouldBeTrue();
    }

    [Fact]
    public async Task StayRunningUntilBothFinish()
    {
        TaskCompletionSource<string> afterAnAction = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> onShow = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> reload = loads.Track(_ => afterAnAction.Task);
        Func<CancellationToken, Task<string>> scan = loads.Track(_ => onShow.Task);

        loads.TryReserve();
        Task<string> reloading = reload(CancellationToken.None);
        Task<string> scanning = scan(CancellationToken.None);
        afterAnAction.SetResult("rows after the action");
        await reloading;
        bool runningAfterTheFirst = loads.IsRunning;
        onShow.SetResult("rows on show");
        await scanning;

        runningAfterTheFirst.ShouldBeTrue();
        loads.IsRunning.ShouldBeFalse();
    }
}