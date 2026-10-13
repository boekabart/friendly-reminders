# FriendlyReminders: architecture

How the app is put together. For what it does and how to configure it, see [README.md](README.md). The design
decisions behind the two bigger features are in [TEAMS-ALARM-SPEC.md](TEAMS-ALARM-SPEC.md) and
[SCREEN-SETUPS-SPEC.md](SCREEN-SETUPS-SPEC.md).

## In one paragraph

A single-process WinForms tray app (.NET 10, `net10.0-windows`, framework-dependent single-file exe
`FriendlyReminders.exe`). There's no main window. An `ApplicationContext` (`AlarmContext`) owns the tray icon, runs
three polling loops (ICS calendars, Microsoft 365 calendars, Teams chats) and a 10-second check that opens meeting
popups. Everything runs on the **UI thread**: the loops are `async` and their continuations come back to the UI
thread via the WinForms `SynchronizationContext`, so there is no locking. Only ICS parsing runs on the thread pool
(`Task.Run`), and the config file watcher posts back to the UI thread.

## Projects

```
src/MeetingAlarm/            the app (assembly name FriendlyReminders; folder and namespace-less code keep the old name)
tests/MeetingAlarm.Tests/    xUnit tests for the logic (no UI); InternalsVisibleTo gives access to internal helpers
```

* **No namespaces:** all types live in the global namespace. The app is small enough.
* **Packages:**
  * Ical.Net: ICS parsing.
  * Microsoft.Identity.Client (+ `.Broker` for WAM, `.Extensions.Msal` for the encrypted token cache).
* **Build and run:** `dotnet test`; `dotnet publish src/MeetingAlarm -c Release -o <dir>`.

## Startup

`Program.Main`:

1. Uses the Windows display language until the config is loaded (`UseLanguage(null)`).
2. Takes a named mutex (`MeetingAlarm_SingleInstance`). A second instance shows a message and exits.
3. `AppFolders.MoveOldFolders()` renames `MeetingAlarm` data folders from older versions to `FriendlyReminders`.
   This happens **only here**, under the mutex. No other code moves or creates folders as a side effect of being
   touched; a test run once did exactly that to real data.
4. `Config.LoadOrCreate()`:
   * `ConfigLocation.Resolve` decides where `config.json` lives (OneDrive or local, moving it if needed).
   * On a first run it creates the file, asks about autostart and opens Notepad.
5. `Application.Run(new AlarmContext(config, test))`.

## Source map

| Area | Files | Responsibility |
|---|---|---|
| Entry | `Program.cs` | Startup (above). |
| Orchestration | `AlarmContext.cs` | Tray icon and menu, polling loops, meeting check, status lines, failure warnings, sign-in flow, config reload, screen-setup menu. The hub that wires everything together. |
| Settings | `Config.cs`, `ConfigLocation.cs`, `AppFolders.cs` | The `config.json` model, parse/rewrite, where files live. |
| Meetings: sources | `IcsCalendar.cs` (+ the `Meeting` record), `Graph/TenantClient.cs` (`GetMeetings`, `ToMeeting`) | Turn ICS / Graph events into `Meeting`s for the next 2 days. |
| Meetings: helpers | `MeetingLinks.cs`, `TitleFilter.cs` | Find Teams/Zoom/Meet join links and clean up locations; title ignore patterns. |
| Chats | `Graph/TenantClient.cs` (`Poll`), `Teams/ChatCounter.cs`, `Teams/ChatState.cs`, `Teams/TeamsLink.cs` | Unread counting per chat, acks ("seen"), deep links into the Teams app. |
| Popups | `PopupBase.cs`, `MeetingPopup.cs`, `ChatPopup.cs`, `Placement.cs`, `ColorParser.cs` | The windows themselves, stacking/positioning, colors. |
| Quiet | `Quiet.cs` | Mute (meetings, chats) and Snooze (chats). |
| Screens | `Screens/ScreenSetup.cs`, `Screens/ScreenMemory.cs` | Identify the current monitor setup; remember choices per setup. |
| Texts | `Localization.cs` | All visible strings in en-US/en-GB, nl-NL, sv-SE. |
| Misc | `Autostart.cs` | The `HKCU\…\Run` entry. |

## Data flow: meetings

```
ICS links ──(RefreshSeconds)── IcsCalendar.Parse ──┐
                                                   ├─► meetingsBySource[source] ─► Meetings (minus IgnoreTitles)
Graph calendars ──(GraphRefreshSeconds)── TenantClient.GetMeetings ─┘                      │
                                                                                every 10 s: Check()
                                                                                           │
                                                      within MinutesBefore, not yet shown ─► new MeetingPopup
```

* **Per source:** `meetingsBySource` is keyed by the config object, a `CalendarConfig` or a `TenantConfig`. A
  source's list is only replaced after a successful fetch, so a failing source keeps its last known meetings.
* **No duplicates:** `Meeting.Key` is `Uid|Start` without the calendar name, so the same meeting coming in via ICS
  and via Graph gives one popup. The `shown` set holds keys that already had a popup.
* **Late meetings:** a meeting that already started still gets a popup for up to `AutoCloseAfterMinutes`, for
  example when the app just started or the meeting was planned late.
* **Times:**
  * ICS: start times are converted from the event's own time zone (`AsUtc → ToLocalTime`).
  * Graph: times are requested in UTC (`Prefer: outlook.timezone="UTC"`).
  * `Meeting.Start`/`End` are local times.
* **Skipped events:**
  * ICS: cancelled, all-day, and titles starting with Outlook's "Canceled:" prefix (in several languages).
  * Graph: cancelled, all-day and declined.
* **Join link:**
  * Graph: `onlineMeeting.joinUrl`.
  * ICS: `X-MICROSOFT-SKYPETEAMSMEETINGURL`.
  * Otherwise: the first Teams/Zoom/Meet link in the location or description (`MeetingLinks.Find`).

## Data flow: chats

`TenantClient.Poll` runs once per tenant every `PollSeconds`:

1. Lists the 50 most recently active chats with `lastMessagePreview` (one request).
2. Per chat, works out a **baseline**: the later of our own ack (`ChatState`, i.e. "seen") and Teams' read marker
   (`viewpoint.lastMessageReadDateTime`). Either can only take messages away, so the later one wins
   (`ChatCounter.Baseline`).
3. Only for chats whose latest message is newer than the baseline, fetches up to 50 messages and counts other
   people's real messages after the baseline. Counting stops at a message of your own: whoever replies has read the
   chat, and that is acked automatically (`ChatCounter.Count`).
4. Returns `ChatRow`s. `AlarmContext` merges all tenants and calls `ChatPopup.ShowRows`.

More details:

* **Graph rate limit (429):** that tenant pauses for the `Retry-After` time.
* **Failed poll:** the tenant's previous rows are kept.
* **Ack keys:** `tenantId|chatId`, persisted in `chats.json`.

## Sign-in (Graph)

* **One client per tenant:** each `TenantClient` has its own MSAL `PublicClientApplication`, with the **specific
  tenant** as authority (never `common`/`organizations`), so every token belongs to the right job.
* **WAM:** sign-in goes through Windows' own broker (WAM), shown on top of the chat popup's window handle, since a
  tray app has no window of its own.
* **Token cache:** one cache for all tenants, `msal.cache`, encrypted with DPAPI for the current Windows user.
* **Only silent in the loops:** the polling loops only ever acquire tokens silently. `MsalUiRequiredException` means
  "not signed in" or "permission not granted". The very first time, the interactive sign-in opens by itself; after
  that only via the tray menu item *Sign in to …* / *allow access to …*.
* **Scopes per feature:** chats need `Chat.Read` and `User.Read`, the calendar needs `Calendars.Read`. A missing one
  only blocks its own feature.
* **App registration:** the built-in multi-tenant registration (`Config.DefaultClientId`) is used unless a `ClientId`
  is configured, first per tenant, then in `Chats`.

## Popups

`PopupBase` (a borderless `Form`) holds what both popups share:

* **Never takes focus:**
  * `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST` and `ShowWithoutActivation`.
  * "Always on top" is set through the window style, not through `Form.TopMost`, whose setter activates the window.
  * Buttons are non-selectable (`NoFocusButton`), so clicking them doesn't take focus either.
* **Click guard:** clicks in the first 500 ms after appearing, or after the content moved, are ignored, so a click
  meant for something else doesn't hit a button.
* **DPI:** `S(px)` scales 96-DPI values to the current DPI.
* **Menu buttons** (`MakeMenuButton`) for *Mute ▾*, *Snooze ▾* and *⋯*:
  * The items are built when the menu opens.
  * Because the popup never becomes the active window, Windows doesn't close the menu on a click elsewhere. A
    50 ms timer does that instead: it closes the menu on any mouse button that's down outside our menus
    (`GetAsyncKeyState` + `WindowFromPoint`), and when the popup or its button disappears.
* **"Move to …" items** (`MoveItems`): the other screens of the current setup.

The two popups:

* **`MeetingPopup`:**
  * **Stacking:** one window per meeting. A static `Open` list stacks them from the configured `Position` corner
    (`Placement.Compute`).
  * **Countdown:** a 1-second tick updates the countdown, flashes during the last minute, and closes the popup after
    `AutoCloseAfterMinutes`.
  * **Later:** hides the popup for `SnoozeSeconds`.
* **`ChatPopup`:**
  * **One window**, rebuilt only when the rows actually change.
  * **Flashing:** it flashes and beeps when a chat is added or a count goes up.
  * **Can't be closed:** closing it via Alt+F4 is cancelled. Only "seen" makes it go away.

## Quiet: Mute and Snooze

There are three `Quiet` instances: `MeetingsMuted`, `ChatsMuted` and `ChatsSnoozed`.

* **When one is active:** any of three conditions is enough:
  * until a moment (current meeting, or 15/30/60 min)
  * while the screen setup stays the one it was set in (one-off)
  * remembered for the current setup (`ScreenMemory`)
* **Detecting changes:** a single 1-second timer compares `Active` with its previous value and raises `Changed`. That
  covers time running out and the screen setup changing, with no separate wiring.
* **Mute:**
  * Meeting popups check `MeetingsMuted.Active` themselves on every tick (no sound on appear, no flash).
  * The chat popup postpones its flash (`flashPending`) until the mute ends.
* **Snooze:** `ChatPopup.ShowRows` hides the popup without updating its previous counts, so anything new flashes
  when the snooze ends.

## Screen setups

`ScreenSetup.Current` is read through `QueryDisplayConfig` (active paths) and `DisplayConfigGetDeviceInfo`:

* **Monitor identity** = EDID PnP id (manufacturer + product) + serial number. The serial is read from the EDID
  Windows stored under `HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\<pnp>\<instance>\Device Parameters\EDID`, the
  text descriptor first, then the 32-bit field. Monitors without a serial use the port instance instead.
* **Mirror groups:** paths that share a source show the same image. Each group is one Windows `Screen` (matched via
  its GDI name, `\\.\DISPLAYn`).
* **Setup key:** a readable string of the sorted groups, with `*` before the primary group and `=` between mirrored
  monitors. For example: `LEN61A8@4&…&UID20550 + *LEN8AC7@… + SAM0D34-HTQM104922`.
* **When it fails** (e.g. some remote sessions): it falls back to `Screen.AllScreens`, per device name and resolution.
* **Refreshing:** `Current` is cached and re-read on `SystemEvents.DisplaySettingsChanged`.

`ScreenMemory` keeps the choices per setup key and popup type (`screens.json`): the screen to use, and remembered
mute/snooze. `ScreenMemory.WorkingArea(kind)` is where popups go: the remembered screen if it is active, otherwise the
primary screen.

## Settings lifecycle

* **Parse:** `Config.Parse` deserializes leniently: comments allowed, trailing commas allowed, case-insensitive names.
  It then serializes the result and compares the two as JSON trees. If they differ (a new setting, different casing,
  an obsolete one), the file is rewritten in the current format and the old one goes to `config.bak.json`. A file
  that's already current is left alone, comments included.
* **Unknown properties** (`[JsonExtensionData]`) are kept and written at the end, so you can park things like
  `_DisabledCalendars`. Obsolete settings that should disappear are removed explicitly in `Parse` (e.g. the old
  `Screen`).
* **Validation:** regexes in `IgnoreTitles` are checked when the file is parsed, so a bad pattern is a load error
  (with a notification) instead of a crash later.
* **Live reload:** a `FileSystemWatcher` on the file, debounced by 500 ms, leads to `Reload()`, which re-reads the
  file and calls `ApplyConfig()`. Language, positions, tenant clients and status lines all follow live. If the file
  is invalid, the previous settings stay in use.
* **The app writes to it too:** *Ignore all meetings named …* re-reads the file from disk, adds the pattern and saves
  it. The watcher then reloads it.

## Storage

| Tier | Folder | Files | Code |
|---|---|---|---|
| Synced | `OneDrive\Apps\FriendlyReminders` | `config.json` | `ConfigLocation` |
| User | `%APPDATA%\FriendlyReminders` (`AppFolders.Roaming`) | `chats.json`, `config.bak.json`, `config.moved.json`, `config.local-backup.json`; `config.json` without OneDrive | `ChatState`, `Config`, `ConfigLocation` |
| PC | `%LOCALAPPDATA%\FriendlyReminders` (`AppFolders.Local`) | `msal.cache`, `screens.json` | `TenantClient`, `ScreenMemory` |

* `ConfigLocation.OneDriveFolders` reads the OneDrive app's own registry entries
  (`HKCU\Software\Microsoft\OneDrive\Accounts\*\UserFolder`): personal first, then work accounts.
* `chats.json` and `screens.json` are written atomically: to `.tmp` first, then moved over the original.

## Failures and warnings

* **Counting failures:** `AlarmContext.Failed` counts failures in a row per status key (a calendar, or a tenant's
  "calendar" or "chats").
  * The status line shows the count.
  * The tray icon turns into a warning from 2 in a row.
* **Notification:** only after 10 minutes of failing, and only when `Online()` says so. That's Windows' own
  connectivity test: `http://www.msftconnecttest.com/connecttest.txt` must return exactly "Microsoft Connect Test",
  which a Wi-Fi sign-in page intercepts.
* **Recovery:** `Succeeded` clears the count and re-arms the notification.

## Localization

* **All texts in one record:** every visible string is a `required` property of the `Texts` record, so a missing
  translation is a compile error.
* **Languages:**
  * en-US is the base. en-GB is en-US with a few British wordings (`with { … }`) and British date and time formats.
  * nl-NL and sv-SE are complete translations.
* **Formatting:** `F(format, args)` formats with the chosen culture, so `{0:t}` gives "2:03 PM" or "14:03".
* **Tests:** `LocalizationTests` check that every language uses the same placeholders for each text.

## Win32 used directly

| API | Where | Why |
|---|---|---|
| `SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE)` | `PopupBase.KeepOnTop` | Stay on top without taking focus. |
| `GetAsyncKeyState`, `WindowFromPoint`, `GetAncestor` | `PopupBase` | Close popup menus on an outside click. |
| `QueryDisplayConfig`, `DisplayConfigGetDeviceInfo` | `ScreenSetup` | Monitor identity, mirror groups. |
| `NotifyIcon.ShowContextMenu` (private, via reflection) | `AlarmContext` | Left-click opens the tray menu properly in the foreground, so it closes when clicking elsewhere. |

## Tests

`tests/MeetingAlarm.Tests` covers the logic that can break silently:

* config parsing and rewriting, and config location and moves
* ICS and Graph mapping (time zones, join links, end times)
* link finding and title filters
* chat counting and baselines
* placement math, color parsing, localization placeholders
* EDID serials and setup keys
* folder migration

The UI itself is tried by hand: the tray menu's *Show test popup* and *Show chat popup* show example popups.

Tests must not touch the real user folders. Code that uses `AppFolders`/`Config.FilePath` only resolves paths; it never
moves or creates anything as a side effect. Tests that need files use their own temporary folders.

## Conventions

* Code reads like the surrounding code: small files, little ceremony, comments that explain *why*.
* `// ponytail:` comments mark deliberate simplifications. They name the limit and when to upgrade, e.g. "only the 50
  most recently active chats".
* Specs for bigger features live next to the code as `*-SPEC.md`, written before building and updated to match what
  was built.
