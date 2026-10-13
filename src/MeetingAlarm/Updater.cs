using System.Reflection;
using Velopack;
using Velopack.Sources;

/// <summary>
/// Updates from the GitHub releases (Velopack). Only for a copy installed by Setup.exe: a dev build or a copy elsewhere
/// never updates. A downloaded update is installed by Velopack at the next start (VelopackApp.Run), or right away via
/// <see cref="Restart"/>.
/// </summary>
static class Updater
{
    const string Repo = "https://github.com/boekabart/friendly-reminders";

    /// <summary>git describe of the build: "0.9.0" for a release, "0.9.0-3-gabc1234" after it. See Directory.Build.targets.</summary>
    public static string Version { get; } =
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    static UpdateManager? manager;
    static UpdateManager Manager => manager ??= new UpdateManager(new GithubSource(Repo, null, false));

    public static bool Installed
    {
        get
        {
            try { return Manager.IsInstalled; }
            catch { return false; }
        }
    }

    /// <summary>Checks and downloads; the update that's ready to install, or null (none, not installed, or offline).</summary>
    public static async Task<VelopackAsset?> Download()
    {
        if (!Installed) return null;
        try
        {
            if (Manager.UpdatePendingRestart is { } pending) return pending;
            if (await Manager.CheckForUpdatesAsync() is not { } update) return null;
            await Manager.DownloadUpdatesAsync(update);
            return update.TargetFullRelease;
        }
        catch
        {
            return null;   // offline, GitHub rate limit, …: the next check tries again
        }
    }

    /// <summary>Exits this process; Velopack installs the update and starts the new version.</summary>
    public static void Restart(VelopackAsset update) => Manager.ApplyUpdatesAndRestart(update);
}
