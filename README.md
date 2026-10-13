# FriendlyReminders

Big, persistent popups so you don't miss work, for Windows. It sits in the system tray and shows:

* **Meeting popups** a few minutes before each meeting: a countdown, a **Join** button (Teams, Zoom or Google Meet),
  a sound, and an orange flash in the last minute. Meetings come from Microsoft 365 calendars (via Microsoft Graph)
  and/or published ICS links.
* **A chat popup** listing your unread Teams chats across several Microsoft 365 tenants (e.g. two jobs), each with
  its unread count. ✓ marks a chat as seen until new messages arrive; reading it in Teams makes it go away too.

The popups are always on top, but never take focus: they don't interrupt your typing.

## Install

Download `HTI.FriendlyReminders-win-Setup.exe` from the [latest release](https://github.com/boekabart/friendly-reminders/releases/latest)
and run it. It installs for you only (`%LOCALAPPDATA%\HTI.FriendlyReminders`, no admin rights, nothing else to
install), adds a Start menu shortcut and starts the app. Uninstall via Windows' *Installed apps*; your settings stay.

**Updates** come by themselves: the app checks the releases every 6 hours, downloads a new version in the background
and installs it the next time it starts. *Restart to update to …* in the tray menu does it right away.

Coming from a hand-copied `FriendlyReminders.exe`: exit that one first. The installed app moves *Start with Windows*
over to itself.

On the first start it creates a settings file, offers to start with Windows (also in the tray menu: *Start with
Windows*) and opens the file in Notepad. Fill in your tenants and/or ICS calendars and save: changes are picked up
immediately, no restart needed.

Development: `dotnet run --project src/MeetingAlarm` (add `-- --test` for a test popup right away), `dotnet test`.
A dev build or a copy outside the installation never updates itself.

## Releases

The version is `git describe` of the nearest `X.Y.Z` tag: `0.9.0` on the tag, `0.9.0-3-gabc1234` three commits later.
It's shown in the tray menu and tooltip.

To release, press **Release: patch**, **Release: minor** or **Release: major** in the repo's Actions tab. That tags
the tip of `main` (or a commit you give) with the next version, and `build.yml` builds the installer and publishes it as
a GitHub release; installed copies pick it up from there. Tagging by hand does the same:
`git tag -a 1.2.3 -m "FriendlyReminders 1.2.3"` and `git push origin 1.2.3`. Only `X.Y.Z` tags are built.

## Where things are stored: three tiers

| Tier | Location | Contents | Travels with you? |
|---|---|---|---|
| **1. Settings** | `OneDrive\Apps\FriendlyReminders\config.json` | Everything you configure: tenants, calendars, colors, positions, ignored titles, sound, language. | **Yes**, via OneDrive, to every PC you use. |
| **2. Your state on this PC** | `%APPDATA%\FriendlyReminders` | `chats.json` (which chats you marked ✓), `config.bak.json` (the settings file before the app last rewrote it). Without OneDrive, also `config.json` itself. | Only with Windows roaming profiles (corporate networks); in practice: no. |
| **3. This PC's hardware and sign-ins** | `%LOCALAPPDATA%\FriendlyReminders` | `msal.cache` (Microsoft 365 sign-ins, encrypted for your Windows account), `screens.json` (screen setups, see below). | **No**, on purpose: these only make sense on this PC. |

**Which settings file is used** is decided at every start, taking the first location that has a `config.json`:

1. your personal OneDrive (`OneDrive\Apps\FriendlyReminders`)
2. a work OneDrive (same folder)
3. `%APPDATA%\FriendlyReminders`

* **Moving to OneDrive:** if the only settings file is the local one and you have OneDrive, it is moved to OneDrive
  automatically. The old file stays behind as `config.moved.json`.
* **Second PC:** when both OneDrive and this PC have a settings file, the OneDrive one wins, and the local one is
  kept as `config.local-backup.json`.
* **No surprise moves:** a settings file in a OneDrive is never moved somewhere else.
* **Without OneDrive:** everything simply stays in `%APPDATA%\FriendlyReminders`.

The tray menu item *Open settings (OneDrive)* or *Open settings (this PC)* tells you which file is in use.

(Older versions were called Meeting Alarm and used `MeetingAlarm` folders. They are renamed to `FriendlyReminders`
on the first start.)

## Settings (`config.json`)

```jsonc
{
  "Language": "",                   // "en-US", "en-GB", "nl-NL", "sv-SE" (or "nl", "sv"); empty = Windows' language
  "Sound": true,
  "Meetings": {
    "Position": "BottomRight",      // TopLeft, Top, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, Bottom, BottomRight
    "MinutesBefore": 5,             // popup this long before the start
    "SnoozeSeconds": 60,            // the "Later" button
    "AutoCloseAfterMinutes": 15,    // after the start; 0 = never
    "RefreshSeconds": 180,          // ICS calendars
    "GraphRefreshSeconds": 30,      // Microsoft 365 calendars
    "IgnoreTitles": [ "Block", "Focus*", "/^HTI\\b/" ]
  },
  "Chats": {
    "Position": "MiddleRight",
    "PollSeconds": 30,
    "ChatTypes": [ "oneOnOne", "group" ],
    "FlashSeconds": 3
  },
  "Tenants": [
    { "Name": "Job 1", "Tenant": "job1.com", "LoginHint": "me@job1.com", "Color": "firebrick", "Chats": true, "Calendar": true }
  ],
  "Calendars": [
    { "Name": "Sports club", "Url": "https://…/calendar.ics", "Color": "teal" }
  ]
}
```

* **Tenants** are Microsoft 365 organizations, signed in to via Windows' own sign-in (no password stored by the app).
  * `Tenant`: the organization's domain (or tenant ID). `LoginHint`: your work email there.
  * `Chats` and `Calendar` switch the two features on separately. They need the `Chat.Read` and `Calendars.Read`
    permissions; the tray menu offers to grant them when they're missing.
  * The same meeting coming in via a tenant and an ICS link gives one popup, not two.
* **Calendars** are published ICS links, for calendars outside your tenants. They often lag behind: a meeting
  planned a minute ago can take a while to show up.
* **IgnoreTitles**: no popups for meetings with these titles, matched case-insensitively.
  * `"Block"` matches exactly that title.
  * `"Focus*"` uses wildcards: `*` for any text, `?` for one character.
  * `"/regex/"` is a regular expression.
  * A popup's **⋯** menu can add a title for you (*Ignore all meetings named …*).
* **Colors**: `#rrggbb`, `#rgb` or a CSS color name (`teal`, `rebeccapurple`).
* **Fields you add yourself** (e.g. `"_DisabledCalendars": [...]` to park a calendar) are kept. The app may
  rewrite the file to add new settings; the previous version is then saved as `config.bak.json`.

## Popups

**Meeting popup:** `Join | Later | Close | Mute ▾ | ⋯`
**Chat popup:** `Mark all as seen | Mute ▾ | Snooze ▾ | ⋯`

* **Mute** (meetings and chats): popups still appear and update, but without sound and without flashing.
* **Snooze** (chats only): the chat popup goes away. When the snooze ends, it comes back, flashing if anything new
  arrived meanwhile.
* Both menus offer:
  * *Current meeting* (until it ends)
  * 15 minutes, 30 minutes, 1 hour
  * *While connected to these screens*
  * *Always in this screen setup*
* While something is muted or snoozed, the tray menu shows it at the top. Click that line to end it.

## Screen setups

A **screen setup** is the exact combination of active monitors: which ones, mirrored or extended, and which one is
primary. Monitors are recognized by the identity they report themselves (manufacturer, model and serial number), not
by display numbers, which change between docks and reboots. Where they are arranged and their resolution don't
matter.

Per setup and per popup type, the app remembers what you told it:

* **⋯ → Move to *monitor*:** the popups of that type go there whenever this setup is active. Example: at the desk
  you move them to the side monitor once; in a meeting room, extended to the TV, you move them off the TV once.
* **Mute ▾ / Snooze ▾ → Always in this screen setup:** for example when mirroring to a meeting-room TV.
* A setup you haven't told it anything about uses the main (primary) screen.

The tray menu's **Screen setups** shows the setup you're in now and every remembered setup with its choices, each one
forgettable. This is stored per PC in `%LOCALAPPDATA%\FriendlyReminders\screens.json` (tier 3).

## Tray menu

* **Status lines** per calendar and chat source: when it was last fetched, or the error. A source that keeps failing
  turns the tray icon into a warning. You only get a notification after 10 minutes of failing, and only when you're
  actually online (behind a Wi-Fi sign-in page counts as offline).
* **Upcoming meetings…**, **Refresh now**, **Show test popup**, **Show chat popup**, **Screen setups**,
  **Start with Windows**, **Open settings**, the version, **Restart to update to …** (when an update is ready),
  **Exit**.

## More

* `ARCHITECTURE.md`: how the app is put together (for developers).
* `TEAMS-ALARM-SPEC.md`: the design of the Teams chat part (Graph access, counting, ack semantics).
* `SCREEN-SETUPS-SPEC.md`: the design of screen setups, Mute and Snooze.
