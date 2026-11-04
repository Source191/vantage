using Source191.Torni.Application.Views;
using Source191.Torni.Tui;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class KeyMappingShould
{
    public static TheoryData<GuiKey, Key> NamedKeys => new()
    {
        { GuiKey.Enter, Key.Enter },
        { GuiKey.Esc, Key.Escape },
        { GuiKey.CursorUp, Key.Up },
        { GuiKey.CursorDown, Key.Down },
        { GuiKey.CursorLeft, Key.Left },
        { GuiKey.CursorRight, Key.Right },
        { GuiKey.Backspace, Key.Backspace },
    };

    [Theory]
    [MemberData(nameof(NamedKeys))]
    public void MapTheNamedKeys(GuiKey gui, Key expected)
    {
        KeyMapping.TryFrom(gui, out Key? mapped).ShouldBeTrue();

        mapped.ShouldBe(expected);
    }

    [Theory]
    [InlineData('d')]
    [InlineData(' ')]
    [InlineData('<')]
    [InlineData('/')]
    [InlineData('?')]
    public void MapAPrintableCharacter(char character)
    {
        KeyMapping.TryFrom(new GuiKey(character), out Key? mapped).ShouldBeTrue();

        mapped.ShouldBe(Key.Of(character));
    }

    [Fact]
    public void MapAShiftedLetterToItsCapital()
    {
        KeyMapping.TryFrom(GuiKey.D.WithShift, out Key? mapped).ShouldBeTrue();

        mapped.ShouldBe(Key.Of('D'));
    }

    [Fact]
    public void MapNothingForAControlOrModifiedKey()
    {
        KeyMapping.TryFrom(GuiKey.D.WithCtrl, out _).ShouldBeFalse();
        KeyMapping.TryFrom(GuiKey.D.WithAlt, out _).ShouldBeFalse();
        KeyMapping.TryFrom(GuiKey.F1, out _).ShouldBeFalse();
        KeyMapping.TryFrom(GuiKey.Tab, out _).ShouldBeFalse();
    }

    [Fact]
    public void MapNothingForARuneOutsideTheBasicPlane()
    {
        KeyMapping.TryFrom(new GuiKey((Terminal.Gui.Drivers.KeyCode)0x1F600), out _).ShouldBeFalse();
    }
}