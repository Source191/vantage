using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Actions;

public sealed class CopyFolderPathHandlerShould
{
    private static readonly FolderPath Folder = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");

    [Fact]
    public async Task ReportThePathCopied()
    {
        FakeClipboard clipboard = new();
        CopyFolderPathHandler handler = new(clipboard);

        Outcome outcome = await handler.HandleAsync(new CopyFolderPath(Folder), CancellationToken.None);

        clipboard.Text.ShouldBe(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");
        outcome.ShouldBe(new FolderPathCopied(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123"));
    }

    [Fact]
    public async Task ReportABusyClipboard()
    {
        FakeClipboard clipboard = new() { Busy = true };
        CopyFolderPathHandler handler = new(clipboard);

        Outcome outcome = await handler.HandleAsync(new CopyFolderPath(Folder), CancellationToken.None);

        clipboard.Text.ShouldBeNull();
        outcome.ShouldBe(new ClipboardBusy());
    }
}