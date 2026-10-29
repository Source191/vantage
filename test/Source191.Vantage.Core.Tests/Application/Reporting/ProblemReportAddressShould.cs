using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Domain;
using System.Collections.Specialized;
using System.Web;

namespace Source191.Vantage.Core.Tests.Application.Reporting;

public sealed class ProblemReportAddressShould
{
    [Fact]
    public void OpenTheProblemReportFormOfTheRepository()
    {
        Uri address = ProblemReportAddress.For(ReportTitle.From("Attach froze"), "text");

        address.GetLeftPart(UriPartial.Path).ShouldBe("https://github.com/Source191/vantage/issues/new");
        HttpUtility.ParseQueryString(address.Query)["template"].ShouldBe("problem-report.yml");
    }

    [Fact]
    public void FillTheFormsTitleAndDiagnostics()
    {
        const string text = "2026-10-29T12:00:00.000Z effect StopSession -> StopSessionOutcome in 41 ms\nfailed Load: a & b # c + d = e % f";

        Uri address = ProblemReportAddress.For(ReportTitle.From("Attach & detach #2 froze + 100%"), text);

        NameValueCollection query = HttpUtility.ParseQueryString(address.Query);
        query["title"].ShouldBe("Attach & detach #2 froze + 100%");
        query[ProblemReportAddress.DiagnosticsField].ShouldBe(text);
    }

    [Fact]
    public void MeasureTheAddressAsItWillBeSent()
    {
        Uri address = ProblemReportAddress.For(ReportTitle.From("a b"), "c\nd");

        address.AbsoluteUri.ShouldEndWith("&title=a%20b&diagnostics=c%0Ad");
        ProblemReportAddress.Length(ReportTitle.From("a b"), "c\nd").ShouldBe(address.AbsoluteUri.Length);
    }
}