namespace Source191.Vantage.Domain;

/// <summary>A typed title checked against the rules: the title, or the reason it is refused.</summary>
public abstract record ReportTitleValidation
{
    public sealed record Valid(ReportTitle Title) : ReportTitleValidation;

    public sealed record Refused(ReportTitleProblem Problem) : ReportTitleValidation;
}