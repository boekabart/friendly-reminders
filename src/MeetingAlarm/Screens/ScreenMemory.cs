using System.Text.Json;
using System.Text.Json.Serialization;

enum PopupKind { Meetings, Chats }

/// <summary>What the user chose for one popup type in one screen setup.</summary>
sealed class TypeChoice
{
    /// <summary><see cref="MonitorInfo.Id"/> of the screen to show these popups on; null = config.json's Screen.</summary>
    public string? Monitor { get; set; }
    public string? MonitorName { get; set; }
    public bool Mute { get; set; }
    public bool Snooze { get; set; }
    [JsonIgnore] public bool IsEmpty => Monitor is null && !Mute && !Snooze;
}

sealed class SetupMemory
{
    public string Name { get; set; } = "";
    public DateTimeOffset LastSeen { get; set; }
    public TypeChoice Meetings { get; set; } = new();
    public TypeChoice Chats { get; set; } = new();
    public TypeChoice this[PopupKind kind] => kind == PopupKind.Meetings ? Meetings : Chats;
    [JsonIgnore] public bool IsEmpty => Meetings.IsEmpty && Chats.IsEmpty;
}

/// <summary>
/// Per screen setup (<see cref="ScreenSetup.Key"/>) the remembered choices, in %LOCALAPPDATA%\MeetingAlarm\screens.json.
/// Local on purpose: a setup includes this PC's own panel, so it means nothing on another PC.
/// </summary>
static class ScreenMemory
{
    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeetingAlarm", "screens.json");

    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // keys stay readable ("&", "+"); never goes into HTML
    };

    public static Dictionary<string, SetupMemory> Setups { get; } = Load();

    /// <summary>A choice was made or forgotten.</summary>
    public static event Action? Changed;

    static Dictionary<string, SetupMemory> Load()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, SetupMemory>>(File.ReadAllText(FilePath), Options) ?? new(); }
        catch (Exception e) when (e is IOException or JsonException) { return new(); }
    }

    static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(Setups, Options));
        File.Move(tmp, FilePath, overwrite: true);
    }

    public static TypeChoice? Choice(ScreenSetup setup, PopupKind kind) =>
        Setups.TryGetValue(setup.Key, out var s) ? s[kind] : null;

    /// <summary>Change a choice for <paramref name="kind"/> in <paramref name="setup"/>.</summary>
    public static void Update(ScreenSetup setup, PopupKind kind, Action<TypeChoice> change)
    {
        if (!Setups.TryGetValue(setup.Key, out var s)) Setups[setup.Key] = s = new SetupMemory();
        s.Name = setup.Name;
        s.LastSeen = DateTimeOffset.Now;
        change(s[kind]);
        Done();
    }

    /// <summary>Forget something in a remembered setup; null forgets the whole setup.</summary>
    public static void Forget(string key, Action<SetupMemory>? change)
    {
        if (!Setups.TryGetValue(key, out var s)) return;
        if (change is null) Setups.Remove(key);
        else change(s);
        Done();
    }

    public static void ForgetAll()
    {
        Setups.Clear();
        Done();
    }

    /// <summary>This setup is active (again): keep its name and last-seen time current.</summary>
    public static void Seen(ScreenSetup setup)
    {
        if (!Setups.TryGetValue(setup.Key, out var s)) return;
        s.Name = setup.Name;
        s.LastSeen = DateTimeOffset.Now;
        Save();
    }

    static void Done()
    {
        foreach (var key in Setups.Where(x => x.Value.IsEmpty).Select(x => x.Key).ToList()) Setups.Remove(key);
        Save();
        Changed?.Invoke();
    }

    /// <summary>Where popups of <paramref name="kind"/> go: the screen remembered for this setup if it's active, else the main screen.</summary>
    public static Rectangle WorkingArea(PopupKind kind)
    {
        var setup = ScreenSetup.Current;
        var id = Choice(setup, kind)?.Monitor;
        var screen = setup.Groups.FirstOrDefault(g => g.Monitors.Any(m => m.Id == id))?.Screen;
        return (screen ?? Screen.PrimaryScreen!).WorkingArea;
    }
}
