using Source191.Vantage.Application;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class RequiredProgramsShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Git = @"C:\Program Files\Git\cmd\git.exe";

    [Fact]
    public void FindBothPrograms()
    {
        RequiredPrograms programs = RequiredPrograms.Check(new ClaudeLocation.Found(Claude), new ProgramLocation.Found(Git));

        programs.ShouldBe(new RequiredPrograms.Found(Claude, Git));
    }
}
