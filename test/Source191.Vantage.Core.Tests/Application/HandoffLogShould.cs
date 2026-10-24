using Source191.Vantage.Application;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class HandoffLogShould
{
    [Fact]
    public void LogTheEffectName()
    {
        RecordingLogger log = new();

        log.HandingOver("AttachToSession { JobId = 81cdb0f9 }");

        log.Lines.ShouldBe(["Handing the terminal to AttachToSession { JobId = 81cdb0f9 }"]);
    }
}