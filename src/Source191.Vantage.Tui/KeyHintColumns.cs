namespace Source191.Vantage.Tui;

/// <summary>Where each header column of key hints is drawn: the key's x and width, then the words' x and width.</summary>
internal static class KeyHintColumns
{
    public static readonly (int KeyX, int KeyWidth, int HintX, int HintWidth)[] Layout = [(62, 10, 72, 16), (90, 8, 98, 20)];
}