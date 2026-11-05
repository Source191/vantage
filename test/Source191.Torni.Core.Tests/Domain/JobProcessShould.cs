using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class JobProcessShould
{
    [Fact]
    public void TellARunningJobFromOneWithNoProcess()
    {
        JobProcess running = new JobProcess.Running(5151);

        running.ShouldBeOfType<JobProcess.Running>().Id.ShouldBe(5151);
        running.ShouldNotBe(new JobProcess.None());
        running.ShouldNotBe(new JobProcess.Running(5152));
        new JobProcess.None().ShouldBe(new JobProcess.None());
    }
}