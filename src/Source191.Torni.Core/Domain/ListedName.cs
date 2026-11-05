namespace Source191.Torni.Domain;

/// <summary>A session's name as the listing gave it: <see cref="Named"/>, or <see cref="Unnamed"/> when the row had none or a blank one.</summary>
public abstract record ListedName
{
    public sealed record Named(SessionName Name) : ListedName;

    public sealed record Unnamed : ListedName;
}