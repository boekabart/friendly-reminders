using WinTimer = System.Windows.Forms.Timer;

/// <summary>
/// "Hush", per popup type (meetings, chats): none of its popups and sounds until a moment, or while the screens stay as
/// they are (e.g. a projector is connected). When it ends, everything that was held back pops up right away.
/// </summary>
sealed class Hush
{
    public static readonly Hush Meetings = new(), Chats = new();

    /// <summary>Meetings going on (or about to start) right now; filled in by the app.</summary>
    public static Func<IEnumerable<Meeting>> CurrentMeetings = () => [];

    DateTime until;
    string? screens;   // hushed while the displays are exactly these
    readonly WinTimer watch = new() { Interval = 1000 };   // notices the end (time passed or screens changed)

    /// <summary>Started, ended or resumed.</summary>
    public event Action? Changed;

    public bool Active => DateTime.Now < until || (screens is not null && screens == Screens());

    /// <summary>Tray text while hushed; {0} in the texts is the popup type.</summary>
    public string Description(string what) => screens is not null ? F(T.MenuHushedScreens, what) : F(T.MenuHushedUntil, what, until);

    Hush() => watch.Tick += (_, _) =>
    {
        if (!Active) Resume();
    };

    void Set(DateTime newUntil, string? newScreens)
    {
        until = newUntil;
        screens = newScreens;
        watch.Enabled = Active;
        Changed?.Invoke();
    }

    public void Resume() => Set(default, null);

    static string Screens() => string.Join("|", Screen.AllScreens.Select(s => $"{s.DeviceName} {s.Bounds}").Order());

    /// <summary>The choices: current meeting(s), 15 min, 30 min, 1 hour, while on these screens.</summary>
    public ToolStripItem[] MenuItems()
    {
        var items = new List<ToolStripItem>();
        var current = CurrentMeetings().DistinctBy(m => m.Key).OrderBy(m => m.End).ToList();
        if (current.Count == 1)
            items.Add(new ToolStripMenuItem($"{T.HushCurrentMeeting}: {F(T.HushUntilEnd, current[0].Title.Replace("&", "&&"), current[0].End)}", null,
                (_, _) => Set(current[0].End, null)));
        else if (current.Count > 1)
            items.Add(new ToolStripMenuItem(T.HushCurrentMeeting, null, current
                .Select(m => (ToolStripItem)new ToolStripMenuItem(F(T.HushUntilEnd, m.Title.Replace("&", "&&"), m.End), null, (_, _) => Set(m.End, null)))
                .ToArray()));
        foreach (var min in new[] { 15, 30, 60 })
            items.Add(new ToolStripMenuItem(min == 60 ? T.HushHour : F(T.HushMinutes, min), null,
                (_, _) => Set(DateTime.Now.AddMinutes(min), null)));
        items.Add(new ToolStripMenuItem(T.HushScreens, null, (_, _) => Set(default, Screens())));
        return items.ToArray();
    }
}
