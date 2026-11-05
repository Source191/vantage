using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Core.Tests.Application.Diagnostics;

public sealed class OncePerFolderShould
{
    private static readonly FolderPath Orders = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders");
    private static readonly FolderPath Billing = FolderPath.From(@"C:\Users\me\source\repos\Northwind.Billing");

    private readonly DiagnosticLog log = new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero)), []);

    [Fact]
    public async Task RecordTheFirstFailurePerFolderAndAnswerEveryTime()
    {
        OncePerFolder gate = new(log, FailureSite.Load, CodeName.For(typeof(OncePerFolderShould)));

        string first = await Fail(gate, Orders);
        string second = await Fail(gate, Orders);

        (first, second).ShouldBe(("blank", "blank"));
        Failed failed = log.Snapshot().Single().Event.ShouldBeOfType<Failed>();
        failed.Site.ShouldBe(FailureSite.Load);
        failed.What.ShouldBe(CodeName.For(typeof(OncePerFolderShould)));
    }

    [Fact]
    public async Task RecordAnotherFolderOnItsOwn()
    {
        OncePerFolder gate = new(log, FailureSite.Load, CodeName.For(typeof(OncePerFolderShould)));

        await Fail(gate, Orders);
        await Fail(gate, Billing);
        await Fail(gate, Orders);

        log.Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReturnTheResultAndRecordNothingWhenTheActionSucceeds()
    {
        OncePerFolder gate = new(log, FailureSite.Load, CodeName.For(typeof(OncePerFolderShould)));

        string result = await gate.RunAsync(Orders, () => Task.FromResult("read"), exception => exception is GitException, _ => "blank");

        result.ShouldBe("read");
        log.Snapshot().ShouldBeEmpty();
    }

    private static Task<string> Fail(OncePerFolder gate, FolderPath folder) => gate.RunAsync<string>(folder, () => throw new GitException("x"), exception => exception is GitException, _ => "blank");
}