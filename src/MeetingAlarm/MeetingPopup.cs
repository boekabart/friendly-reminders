using System.Diagnostics;
using System.Media;
using WinTimer = System.Windows.Forms.Timer;

sealed class MeetingPopup : PopupBase
{
    static readonly List<MeetingPopup> Open = new();

    /// <summary>Anchor for the stack of meeting popups; call <see cref="Reposition"/> after changing it.</summary>
    public static Position Position { get; set; } = Position.BottomRight;
    public static int DisplayNumber { get; set; }

    readonly Meeting meeting;
    readonly Config cfg;
    readonly Color color;
    readonly FlowLayoutPanel inner;
    readonly Label timeLabel;
    readonly WinTimer tick = new() { Interval = 1000 };
    bool flashOn;
    bool hushed;   // held back by a hush, shows up when it ends

    public MeetingPopup(Meeting meeting, Config cfg)
    {
        this.meeting = meeting;
        this.cfg = cfg;
        color = ColorParser.Parse(meeting.Color);
        int textWidth = S(400);

        inner = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = color,
            Padding = new Padding(S(14), S(10), S(14), S(10)),
            Margin = Padding.Empty,
        };

        inner.Controls.Add(MakeLabel(meeting.CalendarName.ToUpperInvariant(), 10, textWidth));
        inner.Controls.Add(MakeLabel(meeting.Title, 15, textWidth));
        if (meeting.Location is { } location)
            inner.Controls.Add(MakeLabel(location, 9, textWidth));
        timeLabel = MakeLabel("", 20, textWidth);
        timeLabel.Margin = new Padding(3, S(4), 3, S(8));
        inner.Controls.Add(timeLabel);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        if (meeting.Link is not null)
            buttons.Controls.Add(MakeButton(MeetingLinks.Provider(meeting.Link) is { } provider ? F(T.JoinOn, provider) : T.Join, () =>
            {
                Process.Start(new ProcessStartInfo(meeting.Link) { UseShellExecute = true });
                Close();
            }));
        buttons.Controls.Add(MakeButton(T.Later, Snooze));
        buttons.Controls.Add(MakeButton(T.CloseButton, Close));
        buttons.Controls.Add(MakeHushButton(Hush.Meetings));
        inner.Controls.Add(buttons);

        Controls.Add(inner);
        tick.Tick += (_, _) => UpdateCountdown();
    }

    public void ShowPopup()
    {
        _ = Handle;          // force layout so the height is right before positioning
        PerformLayout();
        Appear();
    }

    /// <summary>Show (again), unless hushed: then it waits for the hush to end.</summary>
    void Appear()
    {
        if (Expired) { Close(); return; }
        if (Hush.Meetings.Active) { hushed = true; return; }
        hushed = false;
        Open.Add(this);
        Reposition();
        Show();
        PlaySound();
        UpdateCountdown();
        tick.Start();
    }

    void Disappear()
    {
        tick.Stop();
        Open.Remove(this);
        Hide();
        Reposition();
    }

    /// <summary>Hush started: every popup goes away. Hush ended: they all come back right away.</summary>
    public static void HushChanged()
    {
        foreach (var p in Application.OpenForms.OfType<MeetingPopup>().ToList())
            if (Hush.Meetings.Active && p.Visible) { p.Disappear(); p.hushed = true; }
            else if (!Hush.Meetings.Active && p.hushed) p.Appear();
    }

    bool Expired => cfg.Meetings.AutoCloseAfterMinutes > 0 &&
                    (DateTime.Now - meeting.Start).TotalMinutes >= cfg.Meetings.AutoCloseAfterMinutes;

    void PlaySound()
    {
        if (cfg.Sound) SystemSounds.Exclamation.Play();
    }

    public static void Reposition()
    {
        var points = Placement.Compute(Position, Placement.WorkingArea(DisplayNumber), Open.Select(p => p.Size).ToList());
        for (int i = 0; i < Open.Count; i++)
            Open[i].Location = points[i];
    }

    void UpdateCountdown()
    {
        var sec = (int)Math.Ceiling((meeting.Start - DateTime.Now).TotalSeconds);
        bool flash;
        if (sec > 0)
        {
            timeLabel.Text = F(T.StartsIn, sec / 60, sec % 60, meeting.Start);
            flash = sec <= 60;   // flash during the last minute
        }
        else
        {
            int min = -sec / 60;
            if (Expired) { Close(); return; }
            timeLabel.Text = min == 0 ? T.StartedNow : F(T.StartedAgo, min);
            flash = true;
        }
        flashOn = flash && !flashOn;
        inner.BackColor = flashOn ? FlashColor : color;
        KeepOnTop();
    }

    void Snooze()
    {
        Disappear();
        var wait = new WinTimer { Interval = Math.Max(5, cfg.Meetings.SnoozeSeconds) * 1000 };
        wait.Tick += (_, _) =>
        {
            wait.Dispose();
            if (!IsDisposed) Appear();
        };
        wait.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        tick.Dispose();
        Open.Remove(this);
        Reposition();
        base.OnFormClosed(e);
    }
}
