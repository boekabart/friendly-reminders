using WinTimer = System.Windows.Forms.Timer;

enum QuietKind { Mute, Snooze }

/// <summary>
/// Keeping one popup type quiet. <b>Mute</b> (meetings and chats): popups still appear, without sound or flash.
/// <b>Snooze</b> (chats only): no popup at all; it comes back when the snooze ends. Each is active until a moment,
/// while the current screen setup stays (one-off), or always in a remembered screen setup (<see cref="ScreenMemory"/>).
/// </summary>
sealed class Quiet
{
    static readonly List<Quiet> all = new();
    static readonly WinTimer watch = new() { Interval = 1000 };   // notices time running out and screen setups changing

    public static readonly Quiet MeetingsMuted = new(PopupKind.Meetings, QuietKind.Mute);
    public static readonly Quiet ChatsMuted = new(PopupKind.Chats, QuietKind.Mute);
    public static readonly Quiet ChatsSnoozed = new(PopupKind.Chats, QuietKind.Snooze);
    public static IReadOnlyList<Quiet> All => all;

    /// <summary>Meetings going on (or about to start) right now; filled in by the app.</summary>
    public static Func<IEnumerable<Meeting>> CurrentMeetings = () => [];

    readonly PopupKind kind;
    readonly QuietKind quiet;
    DateTime until;
    string? whileSetup;   // one-off: active while the screen setup has this key
    bool wasActive;

    /// <summary>Started, ended, or changed how long.</summary>
    public event Action? Changed;

    static Quiet()
    {
        watch.Tick += (_, _) =>
        {
            foreach (var q in all)
                if (q.Active != q.wasActive) q.Fire();
        };
        watch.Start();
    }

    Quiet(PopupKind kind, QuietKind quiet)
    {
        this.kind = kind;
        this.quiet = quiet;
        all.Add(this);
    }

    bool Remembered => ScreenMemory.Choice(ScreenSetup.Current, kind) is { } c && (quiet == QuietKind.Mute ? c.Mute : c.Snooze);
    bool WhileHere => whileSetup is not null && whileSetup == ScreenSetup.Current.Key;

    public bool Active => DateTime.Now < until || WhileHere || Remembered;

    public string ButtonText => quiet == QuietKind.Mute ? T.MuteButton : T.SnoozeButton;

    string Label => kind == PopupKind.Meetings ? T.MeetingsMuted : quiet == QuietKind.Mute ? T.ChatsMuted : T.ChatsSnoozed;

    /// <summary>Tray text while active.</summary>
    public string Description => Remembered ? F(T.QuietRemembered, Label) : WhileHere ? F(T.QuietWhile, Label) : F(T.QuietUntil, Label, until);

    void Fire()
    {
        wasActive = Active;
        Changed?.Invoke();
    }

    /// <summary>Ends it now; a remembered one is forgotten for the current screen setup.</summary>
    public void Resume()
    {
        until = default;
        whileSetup = null;
        if (Remembered) Remember(false);
        Fire();
    }

    void Remember(bool on) =>
        ScreenMemory.Update(ScreenSetup.Current, kind, c =>
        {
            if (quiet == QuietKind.Mute) c.Mute = on;
            else c.Snooze = on;
        });

    void Until(DateTime moment)
    {
        until = moment;
        Fire();
    }

    /// <summary>The choices: current meeting(s), 15 min, 30 min, 1 hour; while on these screens, always in this setup.</summary>
    public ToolStripItem[] MenuItems()
    {
        var items = new List<ToolStripItem>();
        var current = CurrentMeetings().DistinctBy(m => m.Key).OrderBy(m => m.End).ToList();
        string Meeting(Meeting m) => F(T.QuietUntilEnd, m.Title.Replace("&", "&&"), m.End);
        if (current.Count == 1)
            items.Add(new ToolStripMenuItem($"{T.QuietCurrentMeeting}: {Meeting(current[0])}", null, (_, _) => Until(current[0].End)));
        else if (current.Count > 1)
            items.Add(new ToolStripMenuItem(T.QuietCurrentMeeting, null, current
                .Select(m => (ToolStripItem)new ToolStripMenuItem(Meeting(m), null, (_, _) => Until(m.End)))
                .ToArray()));
        foreach (var min in new[] { 15, 30, 60 })
            items.Add(new ToolStripMenuItem(min == 60 ? T.QuietHour : F(T.QuietMinutes, min), null, (_, _) => Until(DateTime.Now.AddMinutes(min))));
        items.Add(new ToolStripSeparator());
        items.Add(new ToolStripMenuItem(T.QuietWhileScreens, null, (_, _) =>
        {
            whileSetup = ScreenSetup.Current.Key;
            Fire();
        }) { Checked = WhileHere });
        items.Add(new ToolStripMenuItem(T.QuietAlwaysSetup, null, (_, _) =>
        {
            Remember(!Remembered);
            Fire();
        }) { Checked = Remembered });
        return items.ToArray();
    }
}
