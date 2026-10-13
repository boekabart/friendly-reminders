using System.Diagnostics;
using System.Media;
using WinTimer = System.Windows.Forms.Timer;

sealed class MeetingPopup : PopupBase
{
    static readonly List<MeetingPopup> Open = new();

    /// <summary>Anchor for the stack of meeting popups; call <see cref="Reposition"/> after changing it.</summary>
    public static Position Position { get; set; } = Position.BottomRight;

    readonly Meeting meeting;
    readonly Config cfg;
    readonly Color color;
    readonly FlowLayoutPanel inner;
    readonly Label timeLabel;
    readonly WinTimer tick = new() { Interval = 1000 };
    bool flashOn;

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
        buttons.Controls.Add(MakeButton(T.Later, Later));
        buttons.Controls.Add(MakeButton(T.CloseButton, Close));
        buttons.Controls.Add(MakeQuietButton(Quiet.MeetingsMuted));
        buttons.Controls.Add(MakeMenuButton("⋯", () => [.. MoveItems(PopupKind.Meetings), new ToolStripSeparator(),
            new ToolStripMenuItem(F(T.IgnoreTitle, meeting.Title.Replace("&", "&&")), null, (_, _) => IgnoreTitle())]));
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

    /// <summary>Show (again). Muted: silently.</summary>
    void Appear()
    {
        if (Expired) { Close(); return; }
        Open.Add(this);
        Reposition();
        Show();
        if (!Quiet.MeetingsMuted.Active) PlaySound();
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

    bool Expired => cfg.Meetings.AutoCloseAfterMinutes > 0 &&
                    (DateTime.Now - meeting.Start).TotalMinutes >= cfg.Meetings.AutoCloseAfterMinutes;

    void PlaySound()
    {
        if (cfg.Sound) SystemSounds.Exclamation.Play();
    }

    public static void Reposition()
    {
        var points = Placement.Compute(Position, ScreenMemory.WorkingArea(PopupKind.Meetings), Open.Select(p => p.Size).ToList());
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
        flashOn = flash && !flashOn && !Quiet.MeetingsMuted.Active;   // muted: calm, no flashing
        inner.BackColor = flashOn ? FlashColor : color;
        KeepOnTop();
    }

    /// <summary>No more popups for meetings with this title: added to config.json, and every open one for it closes.</summary>
    void IgnoreTitle()
    {
        try { Config.IgnoreTitle(meeting.Title); }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Meeting Alarm");
            return;
        }
        foreach (var p in Application.OpenForms.OfType<MeetingPopup>().ToList())
            if (string.Equals(p.meeting.Title.Trim(), meeting.Title.Trim(), StringComparison.OrdinalIgnoreCase)) p.Close();
    }

    /// <summary>"Later": away for SnoozeSeconds, then back.</summary>
    void Later()
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
