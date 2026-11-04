using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class OpenReleasePageHandlerShould
{
    [Fact]
    public async Task OpenTheReleasePageOnTheDesktop()
    {
        FakeDesktop desktop = new();
        OpenReleasePageHandler handler = new(desktop);

        Outcome outcome = await handler.HandleAsync(new OpenReleasePage(ReleaseVersions.Version("0.21.0")), CancellationToken.None);

        desktop.Opened.ShouldBe(["https://github.com/Source191/torni/releases/tag/v0.21.0"]);
        outcome.ShouldBe(new OpenReleasePageOutcome.Opened("0.21.0"));
    }

    [Fact]
    public async Task SayWhyThePageDidNotOpen()
    {
        FakeDesktop desktop = new() { OpenFailure = new ProgramNotStartedException("PowerShell 7 (pwsh.exe) was not found on the PATH, so nothing can be opened.") };
        OpenReleasePageHandler handler = new(desktop);

        Outcome outcome = await handler.HandleAsync(new OpenReleasePage(ReleaseVersions.Version("0.21.0")), CancellationToken.None);

        outcome.ShouldBe(new OpenReleasePageOutcome.NotOpened("0.21.0", "PowerShell 7 (pwsh.exe) was not found on the PATH, so nothing can be opened."));
    }

    [Fact]
    public async Task LetAnUndeclaredFailureThrough()
    {
        FakeDesktop desktop = new() { OpenFailure = new InvalidOperationException("Not a failure IDesktop declares.") };
        OpenReleasePageHandler handler = new(desktop);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(new OpenReleasePage(ReleaseVersions.Version("0.21.0")), CancellationToken.None));
    }
}