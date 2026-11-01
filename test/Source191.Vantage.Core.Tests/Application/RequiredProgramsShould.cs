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

    [Fact]
    public void RefuseABatchFile()
    {
        const string batchFile = @"C:\Users\me\AppData\Roaming\npm\claude.cmd";

        RequiredPrograms programs = RequiredPrograms.Check(new ClaudeLocation.BatchFileOnly(batchFile), new ProgramLocation.Found(Git));

        programs.ShouldBe(new RequiredPrograms.Missing(Wording.ClaudeIsABatchFile(batchFile)));
    }

    [Fact]
    public void SayClaudeCodeIsMissing()
    {
        RequiredPrograms programs = RequiredPrograms.Check(new ClaudeLocation.Missing(), new ProgramLocation.Found(Git));

        programs.ShouldBe(new RequiredPrograms.Missing(Wording.ClaudeNotFound));
    }

    [Fact]
    public void SayGitIsMissing()
    {
        RequiredPrograms programs = RequiredPrograms.Check(new ClaudeLocation.Found(Claude), new ProgramLocation.Missing());

        programs.ShouldBe(new RequiredPrograms.Missing(Wording.GitNotFound));
    }
}
