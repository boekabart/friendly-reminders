using System.Text;

public class ScreenSetupTests
{
    static byte[] Edid(uint serialField, string? serialText = null)
    {
        var e = new byte[128];
        BitConverter.GetBytes(serialField).CopyTo(e, 12);
        if (serialText is not null)
        {
            e[72 + 3] = 0xFF;   // second descriptor: serial text
            Encoding.ASCII.GetBytes(serialText + "\n").CopyTo(e, 72 + 5);
        }
        return e;
    }

    [Fact]
    public void Serial_prefers_the_text_descriptor_and_ignores_placeholders()
    {
        Assert.Equal("HTQM104922", ScreenSetup.EdidSerial(Edid(0x01010101, "HTQM104922")));
        Assert.Equal("0001E240", ScreenSetup.EdidSerial(Edid(123456)));
        Assert.Null(ScreenSetup.EdidSerial(Edid(0x01010101)));      // e.g. Lenovo P27q: placeholder only
        Assert.Null(ScreenSetup.EdidSerial(Edid(0, "0000000000")));
        Assert.Null(ScreenSetup.EdidSerial(new byte[10]));
    }

    static ScreenGroup G(int display, params string[] ids) =>
        new($@"\\.\DISPLAY{display}", ids.Select(i => new MonitorInfo(i, "M-" + i)).ToList());

    [Fact]
    public void Key_depends_on_monitors_mirroring_and_primary_not_on_display_numbers()
    {
        var a = G(1, "A");
        var extended = new ScreenSetup([a, G(2, "B")], a);
        Assert.Equal("*A + B", extended.Key);

        var b2 = G(4, "B");
        Assert.Equal(extended.Key, new ScreenSetup([G(3, "A") is var a2 ? a2 : null!, b2], a2).Key);   // other numbers, order
        Assert.Equal("A + *B", new ScreenSetup([G(3, "A"), b2], b2).Key);                               // other primary

        var mirror = G(1, "B", "A");
        Assert.Equal("*A=B", new ScreenSetup([mirror], mirror).Key);
    }
}
