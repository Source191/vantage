using Source191.Torni.Application.Diagnostics;

namespace Source191.Torni.Core.Tests.Application.Diagnostics;

public sealed class DiagnosticLogSettingShould
{
    [Fact]
    public void ReadAPathAsAFile()
    {
        DiagnosticLogSetting.Read(@"C:\Users\me\torni.log").ShouldBe(new DiagnosticLogSetting.File(@"C:\Users\me\torni.log"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadNothingOrBlankAsOff(string? variable)
    {
        DiagnosticLogSetting.Read(variable).ShouldBe(new DiagnosticLogSetting.Off());
    }
}