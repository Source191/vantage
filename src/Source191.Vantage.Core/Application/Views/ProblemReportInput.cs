namespace Source191.Vantage.Application.Views;

public abstract record ProblemReportInput
{
    public sealed record TitleChanged(string Text) : ProblemReportInput;

    public sealed record Open : ProblemReportInput;

    public sealed record Copy : ProblemReportInput;

    public sealed record Cancel : ProblemReportInput;
}