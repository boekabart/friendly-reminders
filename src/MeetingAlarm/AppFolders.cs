/// <summary>
/// The app's own folders: %APPDATA%\FriendlyReminders (chats.json, backups; config.json when there's no OneDrive)
/// and %LOCALAPPDATA%\FriendlyReminders (sign-in cache, screens.json). Folders from the MeetingAlarm days move along
/// on first use; if that fails (e.g. a file in use), the old folder keeps being used.
/// </summary>
static class AppFolders
{
    public static string Roaming { get; } = Resolve(Environment.SpecialFolder.ApplicationData);
    public static string Local { get; } = Resolve(Environment.SpecialFolder.LocalApplicationData);

    static string Resolve(Environment.SpecialFolder root)
    {
        var dir = Environment.GetFolderPath(root);
        return Migrate(Path.Combine(dir, "MeetingAlarm"), Path.Combine(dir, "FriendlyReminders"));
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
