namespace Source191.Vantage.Domain;

/// <summary>A typed name checked against the rules: the name, or the reason it is refused.</summary>
public abstract record SessionNameValidation
{
    public sealed record Valid(SessionName Name) : SessionNameValidation;

    public sealed record Refused(NameProblem Problem) : SessionNameValidation;
}