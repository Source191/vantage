using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class EditorCommandShould
{
    [Fact]
    public void PassTheFolderAsOneArgument()
    {
        FolderPath folder = new(@"C:\Users\me\source\repos\Northwind & Co's ""tools""");

        Ports.ProcessRequest request = EditorCommand.For(EditorChoice.VsCode, @"C:\Code\Code.exe", folder);

        request.FileName.ShouldBe(@"C:\Code\Code.exe");
        request.Arguments.ShouldBe([folder.Value]);
    }

    [Fact]
    public void ReplaceFolderInsideACustomArgument()
    {
        EditorChoice editor = EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "--folder={folder}", "--wait"]);
        FolderPath folder = new(@"C:\Users\me\source\repos\Northwind.Orders");

        Ports.ProcessRequest request = EditorCommand.For(editor, editor.Executable, folder);

        request.FileName.ShouldBe(@"C:\Tools\subl.exe");
        request.Arguments.ShouldBe(["-n", @"--folder=C:\Users\me\source\repos\Northwind.Orders", "--wait"]);
    }
}