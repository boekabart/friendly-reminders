using System.Runtime.InteropServices;

/// <summary>
/// Borderless, always on top, never takes focus (not when it appears, not when clicked), not in Alt-Tab; scales with DPI.
/// Clicks in the first half second after appearing are ignored, so a click meant for something else doesn't hit a button.
/// </summary>
abstract class PopupBase : Form
{
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point p);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
    const int VK_LBUTTON = 1, VK_RBUTTON = 2, VK_MBUTTON = 4;
    const uint GA_ROOT = 2;
    static readonly IntPtr HWND_TOPMOST = new(-1);
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    const int WS_EX_TOPMOST = 0x8, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
    const int ClickGuardMs = 500;

    protected static readonly Color FlashColor = Color.FromArgb(255, 111, 0);
    protected static readonly Color BorderColor = Color.FromArgb(255, 214, 0);

    long clickableFrom;

    protected override bool ShowWithoutActivation => true;   // don't steal focus while typing

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // TOPMOST via the style, not via Form.TopMost: that setter calls SetWindowPos without NOACTIVATE and steals focus.
            // NOACTIVATE: clicking a button doesn't activate the popup either.
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected PopupBase()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = BorderColor;   // yellow border
        Padding = new Padding(S(5));
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    /// <summary>Scale a 96-DPI pixel value to the current DPI.</summary>
    protected int S(int px) => (int)Math.Round(px * DeviceDpi / 96f);

    protected void KeepOnTop()
    {
        if (Visible)
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    /// <summary>Ignore clicks for a moment: call when the popup appears or its content moves under the mouse.</summary>
    protected void GuardClicks() => clickableFrom = Environment.TickCount64 + ClickGuardMs;

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) GuardClicks();   // also when a snoozed popup comes back
    }

    /// <summary>Wire a click handler that respects the click guard.</summary>
    protected void OnClick(Control c, Action action) =>
        c.Click += (_, _) =>
        {
            if (Environment.TickCount64 >= clickableFrom) action();
        };

    protected static Label MakeLabel(string text, float pt, int width) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(width, 0),
        MaximumSize = new Size(width, 0),
        ForeColor = Color.White,
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI", pt, FontStyle.Bold),
        UseMnemonic = false,
    };

    protected Button MakeButton(string text, Action action)
    {
        var b = new NoFocusButton { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), UseVisualStyleBackColor = true };
        OnClick(b, action);
        return b;
    }

    /// <summary>"Mute ▾" / "Snooze ▾": a menu with how long to keep this type of popup quiet.</summary>
    protected Button MakeQuietButton(Quiet quiet) => MakeMenuButton(quiet.ButtonText + " ▾", quiet.MenuItems);

    /// <summary>
    /// "Move to …" for the ⋯ menu: the other screens of the current setup (one item for one other screen, a submenu for
    /// more). Remembered for this setup and popup type.
    /// </summary>
    protected ToolStripItem[] MoveItems(PopupKind kind)
    {
        var setup = ScreenSetup.Current;
        var here = Screen.FromControl(this).DeviceName;
        var others = setup.Groups.Where(g => !string.Equals(g.GdiName, here, StringComparison.OrdinalIgnoreCase)).ToList();
        ToolStripMenuItem Item(ScreenGroup g, string text) => new(text, null, (_, _) =>
            ScreenMemory.Update(setup, kind, c => (c.Monitor, c.MonitorName) = (g.Monitors[0].Id, g.Name)));
        return others.Count switch
        {
            0 => [new ToolStripMenuItem(T.NoOtherScreen) { Enabled = false }],
            1 => [Item(others[0], F(T.MoveTo, others[0].Name.Replace("&", "&&")))],
            _ => [new ToolStripMenuItem(T.MoveToScreen, null, others.Select(g => (ToolStripItem)Item(g, g.Name.Replace("&", "&&"))).ToArray())],
        };
    }

    /// <summary>A button that opens a menu below it; the items are made when it opens.</summary>
    protected Button MakeMenuButton(string text, Func<IEnumerable<ToolStripItem>> items)
    {
        Button b = null!;
        b = MakeButton(text, () =>
        {
            var menu = new ContextMenuStrip();
            MenuRenderer.Use(menu);
            menu.Items.AddRange(items().ToArray());
            // The popup never takes focus, so Windows doesn't close the menu on a click elsewhere: watch for that
            // ourselves, and for the popup going away underneath it (e.g. the chat was read in Teams).
            var watch = new System.Windows.Forms.Timer { Interval = 50 };
            watch.Tick += (_, _) =>
            {
                if (b.IsDisposed || !b.Visible || ClickedOutsideMenus()) menu.Close();
            };
            menu.Closed += (_, _) =>
            {
                watch.Dispose();
                menu.BeginInvoke(menu.Dispose);   // after the clicked item's handler ran
            };
            menu.Show(b, new Point(0, b.Height));
            watch.Start();
        });
        return b;
    }

    /// <summary>A mouse button is down somewhere that isn't one of our open menus (or submenus).</summary>
    static bool ClickedOutsideMenus()
    {
        if (!new[] { VK_LBUTTON, VK_RBUTTON, VK_MBUTTON }.Any(k => GetAsyncKeyState(k) < 0)) return false;   // high bit = down
        return Control.FromHandle(GetAncestor(WindowFromPoint(Cursor.Position), GA_ROOT)) is not ToolStripDropDown;
    }

    /// <summary>A normal Button grabs keyboard focus on click, which would activate the popup after all.</summary>
    sealed class NoFocusButton : Button
    {
        public NoFocusButton() => SetStyle(ControlStyles.Selectable, false);
    }
}
