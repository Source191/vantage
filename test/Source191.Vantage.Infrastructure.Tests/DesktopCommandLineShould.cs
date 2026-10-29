using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Opening an address encodes it twice (Base64 inside a script that is itself encoded for pwsh), so the longest
/// problem report address must still fit Windows' command line of 32,767 characters.
/// </summary>
public sealed class DesktopCommandLineShould
{
    private const int WindowsCommandLineLimit = 32_767;

    [Fact]
    public async Task KeepTheLongestReportAddressUnderTheCommandLineLimit()
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Empty, string.Empty));
        string address = "https://github.com/Source191/vantage/issues/new?template=problem-report.yml&diagnostics=" + new string('%', ProblemReport.AddressLimit);

        await new Desktop(processes).OpenAsync(address[..ProblemReport.AddressLimit], TestContext.Current.CancellationToken);

        ProcessRequest request = processes.CapturedRequests.Single();
        int commandLine = request.FileName.Length + 2 + request.Arguments.Sum(argument => argument.Length + 3);
        commandLine.ShouldBeLessThan(WindowsCommandLineLimit);
    }
}
