namespace Source191.Vantage.Application.Views;

/// <summary>What a table shows of its loading: nothing while the last load succeeded, or the last failure's message.</summary>
public abstract record LoadBanner
{
    public sealed record None : LoadBanner;

    public sealed record Failed(string Message) : LoadBanner;
}