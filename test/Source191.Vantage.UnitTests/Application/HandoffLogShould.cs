using Source191.Vantage.Application;
using Source191.Vantage.UnitTests.TestDoubles;

namespace Source191.Vantage.UnitTests.Application;

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