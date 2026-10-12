using Microsoft.Win32;

enum ConfigMove { None, MovedToOneDrive, OneDriveWins }

/// <summary>
/// Where config.json lives, decided once at startup: the first of personal OneDrive, work OneDrive(s), local AppData
/// that has one. A local-only config moves to the preferred OneDrive; when a OneDrive already has one, that wins and the
/// local one is kept as a backup. Without OneDrive everything stays local.
/// </summary>
static class ConfigLocation
{
    public const string FileName = "config.json";

    public static string LocalDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingAlarm");

    /// <summary>OneDrive folders as the OneDrive app records them: personal first, then work accounts. Only ones that exist.</summary>
    public static List<string> OneDriveFolders()
    {
        using var accounts = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts");
        if (accounts is null) return [];
        return accounts.GetSubKeyNames()
            .OrderBy(n => n == "Personal" ? 0 : 1).ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Select(n =>
            {
                using var k = accounts.OpenSubKey(n);
                return k?.GetValue("UserFolder") as string;
            })
            .OfType<string>()
            .Where(Directory.Exists)
            .ToList();
    }

    public static string SyncedDir(string oneDrive) => Path.Combine(oneDrive, "Apps", "FriendlyReminders");

    /// <summary>Picks (and if needed moves) the config file. <paramref name="oneDrives"/> in order of preference.</summary>
    internal static (string Path, ConfigMove Move) Resolve(string localDir, IReadOnlyList<string> oneDrives)
    {
        var local = Path.Combine(localDir, FileName);
        var synced = oneDrives.Select(d => Path.Combine(SyncedDir(d), FileName)).ToList();

        if (synced.FirstOrDefault(File.Exists) is { } existing)
        {
            if (!File.Exists(local)) return (existing, ConfigMove.None);
            File.Move(local, Path.Combine(localDir, "config.local-backup.json"), overwrite: true);
            return (existing, ConfigMove.OneDriveWins);
        }
        if (synced.Count == 0) return (local, ConfigMove.None);

        var target = synced[0];
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (!File.Exists(local)) return (target, ConfigMove.None);   // new install: created right there
        File.Copy(local, target);
        File.Move(local, Path.Combine(localDir, "config.moved.json"), overwrite: true);
        return (target, ConfigMove.MovedToOneDrive);
    }
}
