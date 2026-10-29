using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Ports;
using System.Runtime.CompilerServices;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class ExceptionSummaryShould
{
    private const string Folder = @"C:\Users\me\source\repos\Northwind.Orders";

    [Fact]
    public void KeepTypesAndFramesButNoMessage()
    {
        Exception exception = Caught(ThrowWrapped);

        string text = ExceptionSummary.From(exception).ToString();

        text.ShouldStartWith("InvalidOperationException at ExceptionSummaryShould.ThrowWrapped");
        text.ShouldContain("inner GitException at ExceptionSummaryShould.ThrowGit");
        text.ShouldNotContain("Northwind");
        text.ShouldNotContain(@"C:\Users\me");
        text.ShouldNotContain("fatal");
    }

    [Fact]
    public void ShowTheSourceFileWithoutItsFolder()
    {
        Exception exception = Caught(ThrowWrapped);

        string text = ExceptionSummary.From(exception).ToString();

        text.ShouldContain("(ExceptionSummaryShould.cs:");
        text.ShouldNotContain(Path.GetDirectoryName(ThisFile())!);
    }

    [Fact]
    public void LeaveOutTheFramesOfAwaitMachinery()
    {
        Exception exception = Caught(() => ThrowLaterAsync().GetAwaiter().GetResult());

        string text = ExceptionSummary.From(exception).ToString();

        text.ShouldContain("ExceptionSummaryShould.ThrowLaterAsync");
        text.ShouldNotContain("ExceptionDispatchInfo");
        text.ShouldNotContain("TaskAwaiter");
    }

    [Fact]
    public void CapTheFramesOfEachException()
    {
        Exception exception = Caught(() => Recurse(40));

        string text = ExceptionSummary.From(exception).ToString();

        text.Split(" < ").Length.ShouldBe(20);
    }

    [Fact]
    public void CapTheDepthOfInnerExceptions()
    {
        Exception exception = new AggregateException(new ArgumentException("a", new FormatException("b", new IOException("c", new TimeoutException("d", new GitException("e"))))));

        string text = ExceptionSummary.From(exception).ToString();

        text.ShouldBe("AggregateException; inner ArgumentException; inner FormatException; inner IOException");
    }

    [Fact]
    public void SummariseAnExceptionThatWasNeverThrown()
    {
        ExceptionSummary.From(new GitException(Folder)).ToString().ShouldBe("GitException");
    }

    private static Exception Caught(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Nothing was thrown.");
    }

    private static void ThrowWrapped()
    {
        try
        {
            ThrowGit();
        }
        catch (GitException git)
        {
            InvalidOperationException wrapper = new($"Could not remove {Folder}", git);
            wrapper.Data["folder"] = Folder;
            throw wrapper;
        }
    }

    private static void ThrowGit()
    {
        throw new GitException($"fatal: '{Folder}' contains modified or untracked files");
    }

    private static async Task ThrowLaterAsync()
    {
        await Task.Yield();
        throw new GitException(Folder);
    }

    private static void Recurse(int depth)
    {
        if (depth == 0)
        {
            throw new GitException(Folder);
        }

        Recurse(depth - 1);
    }

    private static string ThisFile([CallerFilePath] string path = "")
    {
        return path;
    }
}