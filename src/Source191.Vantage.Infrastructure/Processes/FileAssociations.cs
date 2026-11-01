using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Whether Windows has an application for a file type, asked without opening anything. ShellExecute does not fail for a
/// type nothing is associated with: it opens Windows' "How do you want to open this file?" picker (<c>windows.md</c>), so
/// <see cref="Desktop.OpenAsync"/> asks first with
/// <see href="https://learn.microsoft.com/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw">AssocQueryString</see>
/// for the type's ProgID, ignoring Windows' catch-all <c>Unknown</c> class.
/// </summary>
public static partial class FileAssociations
{
    private const uint IgnoreUnknown = 0x400;
    private const int ProgId = 20;

    /// <summary>The HRESULT of ERROR_NO_ASSOCIATION (1155).</summary>
    private const uint NoAssociation = 0x80070483;

    /// <summary>True only when Windows answers that nothing is associated with the extension (such as <c>.slnx</c>); any other answer, an error included, is false.</summary>
    public static bool NoneFor(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        uint length = 0;

        return AssocQueryString(IgnoreUnknown, ProgId, extension, extra: null, output: nint.Zero, ref length) == NoAssociation;
    }

    [LibraryImport("shlwapi.dll", EntryPoint = "AssocQueryStringW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint AssocQueryString(uint flags, int text, string association, string? extra, nint output, ref uint length);
}