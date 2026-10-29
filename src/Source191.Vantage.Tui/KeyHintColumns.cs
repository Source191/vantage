namespace Source191.Vantage.Tui;

/// <summary>Where each header column of key hints is drawn: the key's x and width, the words' x and width, and the row its first key sits on.</summary>
public static class KeyHintColumns
{
    public static readonly (int KeyX, int KeyWidth, int HintX, int HintWidth, int FirstRow)[] Layout = [(62, 10, 72, 16, 0), (90, 8, 98, 17, 0), (116, 8, 124, 10, 0)];
}