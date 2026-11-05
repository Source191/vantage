using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>
/// Where a session runs: in a folder outside every repository (named by the folder, or by the root's text when git
/// names a root the console cannot read as a folder), or in a repository, named by and looked up at its main checkout.
/// </summary>
public abstract record SessionLocation
{
    public sealed record Outside(string Folder) : SessionLocation;

    public sealed record InRepository(string Name, FolderPath Checkout) : SessionLocation;
}