public sealed class ConfigLocationTests : IDisposable
{
    readonly string root = Directory.CreateTempSubdirectory("cfgloc").FullName;
    string Local => Dir("local");
    string Personal => Dir("OneDrive");
    string Work => Dir("OneDrive - Work");

    string Dir(string name) => Directory.CreateDirectory(Path.Combine(root, name)).FullName;
    static string Synced(string oneDrive) => Path.Combine(ConfigLocation.SyncedDir(oneDrive), "config.json");
    static void Put(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Without_OneDrive_stays_local()
    {
        Assert.Equal((Path.Combine(Local, "config.json"), ConfigMove.None), ConfigLocation.Resolve(Local, []));
    }

    [Fact]
    public void Local_config_moves_to_the_preferred_OneDrive_and_leaves_a_backup()
    {
        Put(Path.Combine(Local, "config.json"), "mine");

        var (path, move) = ConfigLocation.Resolve(Local, [Personal, Work]);

        Assert.Equal((Synced(Personal), ConfigMove.MovedToOneDrive), (path, move));
        Assert.Equal("mine", File.ReadAllText(path));
        Assert.False(File.Exists(Path.Combine(Local, "config.json")));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(Local, "config.moved.json")));
    }

    [Fact]
    public void OneDrive_wins_over_local_and_local_is_kept_as_backup()
    {
        Put(Path.Combine(Local, "config.json"), "this pc");
        Put(Synced(Personal), "synced");

        Assert.Equal((Synced(Personal), ConfigMove.OneDriveWins), ConfigLocation.Resolve(Local, [Personal]));
        Assert.Equal("this pc", File.ReadAllText(Path.Combine(Local, "config.local-backup.json")));
        Assert.False(File.Exists(Path.Combine(Local, "config.json")));
    }

    [Fact]
    public void An_existing_config_in_a_work_OneDrive_is_not_moved_to_personal()
    {
        Put(Synced(Work), "work");

        Assert.Equal((Synced(Work), ConfigMove.None), ConfigLocation.Resolve(Local, [Personal, Work]));
        Assert.False(File.Exists(Synced(Personal)));
    }

    [Fact]
    public void New_install_goes_straight_to_the_preferred_OneDrive()
    {
        var (path, move) = ConfigLocation.Resolve(Local, [Personal, Work]);

        Assert.Equal((Synced(Personal), ConfigMove.None), (path, move));
        Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
    }
}

public sealed class AppFoldersTests : IDisposable
{
    readonly string root = Directory.CreateTempSubdirectory("appfolders").FullName;
    string Old => Path.Combine(root, "MeetingAlarm");
    string New => Path.Combine(root, "FriendlyReminders");

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Old_folder_moves_to_the_new_name_with_its_files()
    {
        Directory.CreateDirectory(Old);
        File.WriteAllText(Path.Combine(Old, "chats.json"), "{}");

        Assert.Equal(New, AppFolders.Migrate(Old, New));
        Assert.True(File.Exists(Path.Combine(New, "chats.json")));
        Assert.False(Directory.Exists(Old));
    }

    [Fact]
    public void Existing_new_folder_wins_and_old_one_is_left_alone()
    {
        Directory.CreateDirectory(Old);
        Directory.CreateDirectory(New);

        Assert.Equal(New, AppFolders.Migrate(Old, New));
        Assert.True(Directory.Exists(Old));
    }

    [Fact]
    public void Fresh_install_uses_the_new_name() => Assert.Equal(New, AppFolders.Migrate(Old, New));
}
