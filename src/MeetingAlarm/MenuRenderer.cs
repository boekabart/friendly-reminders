using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

/// <summary>
/// Flat, Windows 11-like look for menus: MenuRenderer.Use(menu); submenus follow along.
/// No gradients or gray image strip, rounded hover highlight, more air, and rounded window corners from DWM.
/// </summary>
// ponytail: light only; pick a dark color table from AppsUseLightTheme if dark mode is wanted.
sealed class MenuRenderer() : ToolStripProfessionalRenderer(new Colors())
{
    static readonly MenuRenderer Instance = new();

    /// <summary>Set per menu, not via ToolStripManager.Renderer: WinForms only calls Initialize for an explicitly set renderer.</summary>
    public static void Use(ToolStripDropDown menu) => menu.Renderer = Instance;

    static readonly Color Back = Color.FromArgb(249, 249, 249);
    static readonly Color Hover = Color.FromArgb(233, 233, 233);
    static readonly Color Line = Color.FromArgb(224, 224, 224);

    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2;

    protected override void Initialize(ToolStrip toolStrip)
    {
        base.Initialize(toolStrip);
        if (toolStrip is not ToolStripDropDown dropDown) return;
        foreach (ToolStripItem item in dropDown.Items) Pad(item);
        dropDown.ItemAdded += (_, e) => Pad(e.Item!);
        // Windows 11 rounds the corners; Windows 10 doesn't know the attribute and ignores it.
        if (dropDown.IsHandleCreated) Round(dropDown);
        dropDown.HandleCreated += (_, _) => Round(dropDown);
    }

    static void Pad(ToolStripItem item)
    {
        if (item is not ToolStripMenuItem mi) return;
        var v = item.Owner?.LogicalToDeviceUnits(3) ?? 3;
        mi.Padding = new Padding(0, v, 0, v);
        // ponytail: only submenus that have items when added; one filled later wouldn't get the look.
        if (mi.HasDropDownItems) Use(mi.DropDown);
    }

    static void Round(Control c)
    {
        int round = DWMWCP_ROUND;
        DwmSetWindowAttribute(c.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
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
        // The box behind a checkmark: invisible, so only the check itself shows.
        public override Color CheckBackground => Back;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Hover;
        public override Color ButtonSelectedBorder => Color.Transparent;
    }
}
