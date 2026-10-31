using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using System.Reflection;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class CodeNameShould
{
    [Fact]
    public void NameAnEffectType()
    {
        CodeName name = CodeName.For(typeof(AttachToSession));

        name.ToString().ShouldBe("AttachToSession");
    }

    [Fact]
    public void NameANestedTypeWithTheTypeThatHoldsIt()
    {
        CodeName name = CodeName.For(typeof(StartSessionOutcome.NotListedYet));

        name.ToString().ShouldBe("StartSessionOutcome.NotListedYet");
    }

    [Fact]
    public void NameAGenericTypeWithoutItsArity()
    {
        CodeName name = CodeName.For(typeof(List<int>));

        name.ToString().ShouldBe("List");
    }

    [Fact]
    public void NameAMethodWithItsType()
    {
        MethodBase method = typeof(CodeNameShould).GetMethod(nameof(NameAMethodWithItsType))!;

        CodeName.For(method).ToString().ShouldBe("CodeNameShould.NameAMethodWithItsType");
    }

    [Fact]
    public void NameAnAsyncMethodByTheMethodTheCompilerMovedItOutOf()
    {
        MethodBase moveNext = typeof(CodeNameShould).GetNestedTypes(BindingFlags.NonPublic)
            .Single(type => type.Name.StartsWith($"<{nameof(WaitAsync)}>", StringComparison.Ordinal))
            .GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;

        CodeName.For(moveNext).ToString().ShouldBe("CodeNameShould.WaitAsync");
    }

    [Fact]
    public void EqualAnotherNameForTheSameType()
    {
        CodeName.For(typeof(Quit)).ShouldBe(CodeName.For(typeof(Quit)));
    }

    private static async Task WaitAsync()
    {
        await Task.Yield();
    }
}