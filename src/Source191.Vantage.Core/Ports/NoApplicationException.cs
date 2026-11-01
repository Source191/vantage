namespace Source191.Vantage.Ports;

/// <summary>
/// Windows has no application for the type of a file <see cref="IDesktop.OpenAsync"/> was given, so nothing was opened.
/// <see cref="Extension"/> is the file's extension with its dot, such as <c>.slnx</c>.
/// </summary>
public sealed class NoApplicationException(string extension) : Exception($"No application opens {extension} files.")
{
    public string Extension { get; } = extension;
}