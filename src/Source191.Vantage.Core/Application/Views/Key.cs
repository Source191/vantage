namespace Source191.Vantage.Application.Views;

/// <summary>A key as the reducer sees it, independent of any terminal library.</summary>
public sealed record Key(KeyName Name, char Character)
{
    public static readonly Key Enter = new(KeyName.Enter, '\0');
    public static readonly Key Escape = new(KeyName.Escape, '\0');
    public static readonly Key Up = new(KeyName.Up, '\0');
    public static readonly Key Down = new(KeyName.Down, '\0');
    public static readonly Key Left = new(KeyName.Left, '\0');
    public static readonly Key Right = new(KeyName.Right, '\0');
    public static readonly Key Backspace = new(KeyName.Backspace, '\0');
    public static readonly Key Slash = new(KeyName.Character, '/');

    public static Key Of(char character) => new(KeyName.Character, character);
}