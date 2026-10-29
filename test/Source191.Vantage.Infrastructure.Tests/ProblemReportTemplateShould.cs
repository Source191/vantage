using Source191.Vantage.Application.Reporting;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The issue form and the address Vantage opens it with are two halves of one fact; they must name the same file and field.</summary>
public sealed class ProblemReportTemplateShould
{
    private static string Template => Path.Combine(RepositoryFolder.Root(), ".github", "ISSUE_TEMPLATE", ProblemReportAddress.TemplateFile);

    [Fact]
    public void BeTheFileTheAddressNames()
    {
        File.Exists(Template).ShouldBeTrue();
    }

    [Fact]
    public void DeclareTheFieldTheAddressFills()
    {
        File.ReadAllLines(Template).Select(line => line.Trim()).ShouldContain($"id: {ProblemReportAddress.DiagnosticsField}");
    }

    [Fact]
    public void RequireTheUsersOwnDescription()
    {
        string text = File.ReadAllText(Template);

        text.ShouldContain("id: what-happened");
        text.ShouldContain("required: true");
    }
}