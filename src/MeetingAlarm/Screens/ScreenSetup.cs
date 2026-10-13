using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

/// <summary>One physical monitor. <see cref="Id"/> comes from its EDID: manufacturer + product + serial.</summary>
sealed record MonitorInfo(string Id, string Name);

/// <summary>Monitors showing the same image (one when extended, several when mirrored); one Windows screen.</summary>
sealed class ScreenGroup(string gdiName, IReadOnlyList<MonitorInfo> monitors)
{
    /// <summary>"\\.\DISPLAY2": the matching <see cref="Screen.DeviceName"/>.</summary>
    public string GdiName { get; } = gdiName;
    public IReadOnlyList<MonitorInfo> Monitors { get; } = monitors.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
    public string Key => string.Join("=", Monitors.Select(m => m.Id));
    public string Name => string.Join(" = ", Monitors.Select(m => m.Name));
    public Screen? Screen => Screen.AllScreens.FirstOrDefault(s => string.Equals(s.DeviceName, GdiName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The active monitors, which of them mirror each other, and which one is primary. Two setups are the same when those
/// are; arrangement, resolution and scaling don't matter. See SCREEN-SETUPS-SPEC.md.
/// </summary>
sealed class ScreenSetup(IReadOnlyList<ScreenGroup> groups, ScreenGroup? primary)
{
    public IReadOnlyList<ScreenGroup> Groups { get; } = groups.OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
    public ScreenGroup? Primary { get; } = primary;

    /// <summary>Readable and stable: "*DELA0F3-ABC123 + GSM5B7F-XYZ" (* = primary, = between mirrored monitors).</summary>
    public string Key => string.Join(" + ", Groups.Select(g => (g == Primary ? "*" : "") + g.Key));

    public string Name => Groups.Count == 1 ? Groups[0].Name
        : string.Join(" + ", Groups.OrderBy(g => g == Primary ? 0 : 1).Select(g => g == Primary ? F(T.PrimaryName, g.Name) : g.Name));

    /// <summary>The setup right now; <see cref="Refresh"/> when the displays changed.</summary>
    public static ScreenSetup Current { get; private set; } = Read();

    public static void Refresh() => Current = Read();

    static ScreenSetup Read()
    {
        try { return FromDisplayConfig(); }
        catch (Exception e) when (e is Win32Exception or EntryPointNotFoundException)
        {
            // ponytail: Windows screens without monitor identity (e.g. some remote sessions); setups are then remembered per resolution
            var groups = Screen.AllScreens
                .Select(s => new ScreenGroup(s.DeviceName, [new MonitorInfo($"{s.DeviceName} {s.Bounds.Width}x{s.Bounds.Height}", s.DeviceName)]))
                .ToList();
            return new ScreenSetup(groups, groups.FirstOrDefault(g => g.GdiName == Screen.PrimaryScreen?.DeviceName));
        }
    }

    static ScreenSetup FromDisplayConfig()
    {
        PathInfo[] paths;
        int err;
        uint pathCount;
        do
        {
            Check(GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out pathCount, out var modeCount));
            paths = new PathInfo[pathCount];
            var modes = new ModeInfo[modeCount];
            err = QueryDisplayConfig(QdcOnlyActivePaths, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        } while (err == ErrorInsufficientBuffer);   // displays changed in between
        Check(err);

        var primary = Screen.PrimaryScreen?.DeviceName;
        var groups = paths.Take((int)pathCount)
            .Where(p => (p.Flags & PathActive) != 0)
            .GroupBy(p => (p.Source.AdapterId.Low, p.Source.AdapterId.High, p.Source.Id))
            .Select(g => new ScreenGroup(SourceName(g.First().Source), g.Select(p => Monitor(p.Target)).ToList()))
            .ToList();
        return new ScreenSetup(groups, groups.FirstOrDefault(g => string.Equals(g.GdiName, primary, StringComparison.OrdinalIgnoreCase)));
    }

    static string SourceName(PathSourceInfo s)
    {
        var n = new SourceDeviceName { Header = Header(GetSourceName, Marshal.SizeOf<SourceDeviceName>(), s.AdapterId, s.Id) };
        Check(DisplayConfigGetDeviceInfo(ref n));
        return n.ViewGdiDeviceName;
    }

    static MonitorInfo Monitor(PathTargetInfo t)
    {
        var n = new TargetDeviceName { Header = Header(GetTargetName, Marshal.SizeOf<TargetDeviceName>(), t.AdapterId, t.Id) };
        Check(DisplayConfigGetDeviceInfo(ref n));

        // \\?\DISPLAY#DELA0F3#5&2a7c1d&0&UID4353#{e6f07b5f-...}: PnP id (manufacturer + product), then the port instance.
        var parts = n.MonitorDevicePath.Split('#');
        var pnp = parts.Length > 2 ? parts[1] : $"{n.EdidManufactureId:X4}{n.EdidProductCodeId:X4}";
        var instance = parts.Length > 2 ? parts[2] : t.Id.ToString();
        var serial = parts.Length > 2 ? Serial(pnp, instance) : null;
        // No serial (some TVs, cheap monitors): the port tells otherwise identical monitors apart.
        var id = serial is not null ? $"{pnp}-{serial}" : $"{pnp}@{instance}";

        bool builtIn = n.OutputTechnology is OutputInternal or OutputDisplayPortEmbedded or OutputUdiEmbedded;
        var name = builtIn ? T.BuiltInDisplay : n.MonitorFriendlyDeviceName is { Length: > 0 } f ? f : pnp;
        return new MonitorInfo(id, name);
    }

    /// <summary>The serial number from the monitor's EDID, as Windows stored it; null when it has none.</summary>
    internal static string? Serial(string pnp, string instance)
    {
        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{pnp}\{instance}\Device Parameters");
        return key?.GetValue("EDID") is byte[] edid ? EdidSerial(edid) : null;
    }

    /// <summary>The serial text descriptor (0xFF) if there is one, else the 32-bit serial field; null for empty or 0.</summary>
    internal static string? EdidSerial(byte[] edid)
    {
        if (edid.Length < 128) return null;
        for (int i = 54; i <= 108; i += 18)
            if (edid[i] == 0 && edid[i + 1] == 0 && edid[i + 2] == 0 && edid[i + 3] == 0xFF)
            {
                var text = Encoding.ASCII.GetString(edid, i + 5, 13).Split('\n')[0].Trim();
                if (text.Trim('0').Length > 0) return text;
            }
        var number = BitConverter.ToUInt32(edid, 12);
        return number is 0 or 0x01010101 ? null : number.ToString("X8");
    }

    static void Check(int err)
    {
        if (err != 0) throw new Win32Exception(err);
    }

    static DeviceInfoHeader Header(uint type, int size, Luid adapter, uint id) =>
        new() { Type = type, Size = (uint)size, AdapterId = adapter, Id = id };

    // ------------------------------------------------------------ Win32 (QueryDisplayConfig)

    const uint QdcOnlyActivePaths = 2, PathActive = 1, GetSourceName = 1, GetTargetName = 2;
    const uint OutputInternal = 0x80000000, OutputDisplayPortEmbedded = 11, OutputUdiEmbedded = 13;
    const int ErrorInsufficientBuffer = 122;

    [DllImport("user32.dll")] static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);
    [DllImport("user32.dll")] static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] PathInfo[] paths,
        ref uint modeCount, [Out] ModeInfo[] modes, IntPtr topology);
    [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref SourceDeviceName request);
    [DllImport("user32.dll")] static extern int DisplayConfigGetDeviceInfo(ref TargetDeviceName request);

    [StructLayout(LayoutKind.Sequential)] struct Luid { public uint Low; public int High; }

    [StructLayout(LayoutKind.Sequential)]
    struct PathSourceInfo { public Luid AdapterId; public uint Id; public uint ModeInfoIdx; public uint StatusFlags; }

    [StructLayout(LayoutKind.Sequential)]
    struct PathTargetInfo
    {
        public Luid AdapterId; public uint Id; public uint ModeInfoIdx; public uint OutputTechnology; public uint Rotation;
        public uint Scaling; public uint RefreshNumerator; public uint RefreshDenominator; public uint ScanLineOrdering;
        public int TargetAvailable; public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)] struct PathInfo { public PathSourceInfo Source; public PathTargetInfo Target; public uint Flags; }

    [StructLayout(LayoutKind.Sequential, Size = 64)] struct ModeInfo { public uint InfoType; }   // contents not needed

    [StructLayout(LayoutKind.Sequential)] struct DeviceInfoHeader { public uint Type; public uint Size; public Luid AdapterId; public uint Id; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SourceDeviceName
    {
        public DeviceInfoHeader Header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct TargetDeviceName
    {
        public DeviceInfoHeader Header;
        public uint Flags; public uint OutputTechnology; public ushort EdidManufactureId; public ushort EdidProductCodeId;
        public uint ConnectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string MonitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string MonitorDevicePath;
    }
}
