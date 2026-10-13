using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

/// <summary>
/// Flat, Windows 11-like look for menus: MenuRenderer.Use(menu); submenus follow along.
/// No gradients or gray image strip, rounded hover highlight, more air, and rounded window corners from DWM.
/// Light or dark like Windows' app mode, switching along live.
/// </summary>
sealed class MenuRenderer() : ToolStripProfessionalRenderer(new Colors())
{
    static readonly MenuRenderer Instance = new();

    /// <summary>Set per menu, not via ToolStripManager.Renderer: WinForms only calls Initialize for an explicitly set renderer.</summary>
    public static void Use(ToolStripDropDown menu) => menu.Renderer = Instance;

    static bool dark = IsDark();

    static MenuRenderer() =>
        SystemEvents.UserPreferenceChanged += (_, e) => { if (e.Category == UserPreferenceCategory.General) dark = IsDark(); };

    static bool IsDark() => Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
        "AppsUseLightTheme", 1) is 0;

    static Color Back => dark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(249, 249, 249);
    static Color Hover => dark ? Color.FromArgb(61, 61, 61) : Color.FromArgb(233, 233, 233);
    static Color Line => dark ? Color.FromArgb(64, 64, 64) : Color.FromArgb(224, 224, 224);
    static Color Fore => dark ? Color.White : Color.Black;
    static Color Disabled => dark ? Color.FromArgb(120, 120, 120) : Color.FromArgb(109, 109, 109);

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2, DWMWA_BORDER_COLOR = 34;

    protected override void Initialize(ToolStrip toolStrip)
    {
        base.Initialize(toolStrip);
        if (toolStrip is not ToolStripDropDown dropDown) return;
        foreach (ToolStripItem item in dropDown.Items) Pad(item);
        dropDown.ItemAdded += (_, e) => Pad(e.Item!);
        // Windows 11 rounds the corners and draws the border; Windows 10 doesn't know the attributes and ignores them.
        if (dropDown.IsHandleCreated) Frame(dropDown);
        dropDown.HandleCreated += (_, _) => Frame(dropDown);
        dropDown.Opened += (_, _) => Frame(dropDown);   // border color after a light/dark switch (the tray menu lives on)
    }

    static void Pad(ToolStripItem item)
    {
        if (item is not ToolStripMenuItem mi) return;
        var v = item.Owner?.LogicalToDeviceUnits(3) ?? 3;
        mi.Padding = new Padding(0, v, 0, v);
        // ponytail: only submenus that have items when added; one filled later wouldn't get the look.
        if (mi.HasDropDownItems) Use(mi.DropDown);
    }

    static void Frame(Control c)
    {
        int round = DWMWCP_ROUND, border = ColorTranslator.ToWin32(Line);
        DwmSetWindowAttribute(c.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
        DwmSetWindowAttribute(c.Handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.IsOnDropDown) e.TextColor = e.Item.Enabled ? Fore : Disabled;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        if (e.Item?.IsOnDropDown == true) e.ArrowColor = e.Item.Enabled ? Fore : Disabled;
        base.OnRenderArrow(e);
    }

    /// <summary>A drawn check instead of the stock black bitmap in a box, which disappears on dark.</summary>
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        float x = r.X + r.Width / 2f, y = r.Y + r.Height / 2f, u = r.Height / 8f;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(e.Item.Enabled ? Fore : Disabled, Math.Max(1.5f, u * 0.75f));
        e.Graphics.DrawLines(pen, [new PointF(x - 2.5f * u, y), new PointF(x - u, y + 1.5f * u), new PointF(x + 2.5f * u, y - 2 * u)]);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.IsOnDropDown) { base.OnRenderMenuItemBackground(e); return; }
        if (!e.Item.Selected || !e.Item.Enabled) return;
        var inset = e.Item.Owner!.LogicalToDeviceUnits(4);
        var radius = e.Item.Owner.LogicalToDeviceUnits(4);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Hover);
        e.Graphics.FillRoundedRectangle(brush, new RectangleF(inset, 0, e.Item.Width - 2 * inset, e.Item.Height), new SizeF(radius, radius));
    }

    sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Back;
        public override Color ImageMarginGradientBegin => Back;
        public override Color ImageMarginGradientMiddle => Back;
        public override Color ImageMarginGradientEnd => Back;
        public override Color MenuBorder => Line;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color SeparatorDark => Line;
        public override Color SeparatorLight => Back;
    }
}
