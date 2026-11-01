using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class DiagnosticLogSettingShould
{
    [Fact]
    public void ReadAPathAsAFile()
    {
        DiagnosticLogSetting.Read(@"C:\Users\me\vantage.log").ShouldBe(new DiagnosticLogSetting.File(@"C:\Users\me\vantage.log"));
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