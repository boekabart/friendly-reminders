/// <summary>
/// The app's own folders: %APPDATA%\FriendlyReminders (chats.json, backups; config.json when there's no OneDrive)
/// and %LOCALAPPDATA%\FriendlyReminders (sign-in cache, screens.json). Folders from the MeetingAlarm days are moved by
/// <see cref="MoveOldFolders"/> at startup (only there: tests and other code never move anything); if that failed,
/// e.g. a file in use, the old folder keeps being used.
/// </summary>
static class AppFolders
{
    public static string Roaming => Pick(Environment.SpecialFolder.ApplicationData);
    public static string Local => Pick(Environment.SpecialFolder.LocalApplicationData);

    static (string Old, string New) Dirs(Environment.SpecialFolder root)
    {
        var dir = Environment.GetFolderPath(root);
        return (Path.Combine(dir, "MeetingAlarm"), Path.Combine(dir, "FriendlyReminders"));
    }

    static string Pick(Environment.SpecialFolder root)
    {
        var (oldDir, newDir) = Dirs(root);
        return !Directory.Exists(newDir) && Directory.Exists(oldDir) ? oldDir : newDir;
    }

    public static void MoveOldFolders()
    {
        foreach (var root in new[] { Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.LocalApplicationData })
        {
            var (oldDir, newDir) = Dirs(root);
            Migrate(oldDir, newDir);
        }
    }

    internal static string Migrate(string oldDir, string newDir)
    {
        if (Directory.Exists(newDir) || !Directory.Exists(oldDir)) return newDir;
        try
        {
            Directory.Move(oldDir, newDir);
            return newDir;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return oldDir;
        }
    }
}
