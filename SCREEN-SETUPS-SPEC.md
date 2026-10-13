# FriendlyReminders — screen setups (spec)

Status: built 2026-10-13. Decisions below were made with the user on 2026-10-13.

## 1. Goal

Per **screen setup** (the exact combination of active monitors, mirrored or extended, and which one is primary),
remember where reminders go and whether they should keep quiet:

* At the desk (widescreen + USB-C Full HD monitor, laptop panel switched off): say once "meetings on the other
  screen please", and get that every time this desk setup comes back.
* In a meeting room, **extended** to the TV: move them off the TV once, remembered for that room's setup.
* In a meeting room, **mirrored** to the TV: there is no other screen, so "always mute (or snooze) in this setup".

The app doesn't guess which screen is best. It only remembers what the user told it.

### Non-goals

* No automatic "this looks like a TV/projector" detection.
* No remembering a position *within* a screen. `Position` (TopRight, …) from config.json still applies.
* No roaming. Screen setups are stored on this PC only.

## 2. Decisions

| Topic | Decision |
|---|---|
| Terms | **Mute** (meetings and chats): the popup still appears and updates, but with no sound and no flash. **Snooze** (chats only): no popup and no sound; when it ends the chat popup comes back, flashing if anything new arrived. Snooze includes mute. This replaces today's "Hush" (which hides for both types). |
| Scope | **Per type.** Moving, muting or snoozing from a meeting popup affects meetings, from the chat popup affects chats. |
| Choosing the screen | With 2 screens: one item "Move to *DELL U3423WE*". With 3+: submenu "Move to screen ▸" listing the others by name. Not shown when there is only one (logical) screen, which includes mirroring. |
| Same setup | The same **set of active monitors** (hardware identity), the same **mirror groups**, and the same **primary**. Position/arrangement, resolution, scaling and refresh rate don't matter. A laptop panel that's switched off or has its lid closed is not active, so "panel on" and "panel off" are different setups. |
| Durations (Mute ▾ and Snooze ▾ menus alike) | ① time-based: current meeting / 15 / 30 / 60 min. ② **While connected to these screens**: one-off, ends when the setup changes, not remembered (today's screens hush, but using the new setup identity). ③ **Always in this screen setup**: remembered, applies every time this setup is active, until undone. ② and ③ share the same mechanics: "active while the current setup equals X". |
| Meetings | **Mute only.** A muted meeting popup still appears and counts down, without sound and without the orange flash. This changes today's behavior, where hush hides meeting popups. When the mute ends, open popups flash normally again from then on (whether they also beep once: §8). |
| Chats | **Mute or Snooze.** Muted: the popup keeps showing and updating, no sound or flash. Snoozed: hidden, as today's hush. When either ends and something new arrived meanwhile, the popup flashes (and beeps). |
| Mute + snooze together | Possible (e.g. chats always muted at the desk setup, snoozed for 30 min). Snoozed wins while it lasts; afterwards the mute still applies. |
| Undo | Tray menu: **all remembered setups**, each with its choices, and a way to forget each one (see §6). |
| Storage | Local only: `%LOCALAPPDATA%\FriendlyReminders\screens.json`. Not in config.json, so not in OneDrive. |
| Unknown setup | The main (primary) screen. The old `Screen` setting (a display number) in config.json was removed; an existing one is dropped from the file. |

## 3. Identifying monitors (the hard part)

Display numbers ("2"), `\\.\DISPLAY2` device names and resolutions all change between docks, ports and reboots,
so none of them is used for the identity. What Windows itself uses to remember per-monitor settings is the
monitor's **EDID**, the identity data every monitor reports over the cable.

**Monitor identity** = `manufacturer + product code + serial number`, from the EDID:

* Read with `QueryDisplayConfig` (active paths) and `DisplayConfigGetDeviceInfo(DISPLAYCONFIG_TARGET_DEVICE_NAME)`.
  That gives `edidManufactureId`, `edidProductCodeId`, the friendly name ("DELL U3423WE") and `monitorDevicePath`.
* Serial number: the EDID serial descriptor (or its 32-bit serial field), read from the monitor's registry
  instance (`HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\<id>\<instance>\Device Parameters\EDID`, the instance
  taken from `monitorDevicePath`). `WmiMonitorID` (root\wmi) is the alternative source.
* Built-in laptop panel: same scheme. It has EDID too, and the output technology says "internal".

**Weak spots, and the fallback for each:**

* **No serial.** Some TVs and cheap monitors report an empty or zero serial. In that case, add the connector
  (adapter + target id) to the identity. As a result, the same TV on a different HDMI port of the same dock is a
  different monitor. That's acceptable.
* **Two identical monitors without serials**: same fallback (connector), so they are still told apart.
* **Meeting-room TVs of the same model** in different rooms with empty serials: these look identical, so one
  remembered choice covers all such rooms. That's fine, and arguably even what you want.

**Setup identity** = a normalized string of:

* the sorted list of mirror groups, each group being the sorted monitor identities that show the same image
  (paths sharing one source in `QueryDisplayConfig`). An extended setup has one monitor per group.
* the identity of the primary group (the source positioned at 0,0).

It is stored as is (readable, e.g. `*LEN8AC7@… + SAM0D34-HTQM104922`), with the readable monitor names next to it for
the tray menu.

The existing one-off "While connected to these screens" choice switches from `Screen.DeviceName + Bounds` to this
identity.

## 4. Where a popup goes

For each popup type (meetings, chats) at the moment it is placed:

1. If the current setup has a remembered screen for this type and that monitor is active → use it. When it is
   part of a mirror group, use that group.
2. Otherwise → the main (primary) screen.

`Position` from config.json is applied within the chosen screen, as today.

When the setup changes, the existing "display settings changed" handling recomputes all of this and moves open
popups along.

## 5. Popup UI

Buttons:

* Meeting popup: `Join | Later | Close | Mute ▾ | ⋯`
* Chat popup: `Mark all as seen | Mute ▾ | Snooze ▾ | ⋯`

**Mute ▾** and **Snooze ▾** offer the same choices:

```
Mute ▾ / Snooze ▾
  Current meeting: Stand-up (until 10:30)      ← or "Current meeting ▸" with several
  15 minutes
  30 minutes
  1 hour
  ───────────
  While connected to these screens              ② one-off
  Always in this screen setup                   ③ remembered
```

**⋯** (meeting popup: next to the existing "Ignore all meetings named …"; chat popup: new):

```
⋯
  Move to DELL U3423WE                          ← 2 screens; "Move to screen ▸" with 3+; absent with 1
  Ignore all meetings named "Stand-up"          ← meeting popup only (exists)
```

Notes:

* Moving is remembered right away for this setup and this type, and the popup(s) of that type jump there at once.
* Moving back works the same way: the menu then lists the original screen.

## 6. Tray menu

```
Screen setups ▸
  ● DELL U3423WE + ASUS VG24 (primary: DELL)     ← ● = the setup you're in now
      Meetings on: ASUS VG24          [Forget]
      Chats on:    ASUS VG24          [Forget]
  ○ Built-in display = LG TV (mirrored)
      Meetings muted                  [Forget]
      Chats snoozed                   [Forget]
  ○ Built-in display + LG TV (primary: Built-in)
      Meetings on: Built-in display   [Forget]
  ───────────
  Forget all
```

In WinForms this is nested submenus. Every remembered choice can be forgotten separately, and a setup without
choices disappears from the list.

Active mutes/snoozes also show as lines at the top of the tray menu, like today's "… hushed until … — click to
resume": "Meetings muted until 14:30", "Chats snoozed while on these screens", "Chats muted in this screen setup".
Clicking one ends it (for ③: forgets it for this setup).

## 7. Storage

`%LOCALAPPDATA%\FriendlyReminders\screens.json`, written atomically (tmp + move, like chats.json):

```json
{
  "*LEN8AC7@4&305a054a&0&UID8388688 + LEN61A8@4&305a054a&0&UID20550 + SAM0D34-HTQM104922": {
    "Name": "Built-in display (primary) + LEN P27q-10 + C32F391",
    "LastSeen": "2026-10-13T09:12:00+02:00",
    "Meetings": { "Monitor": "SAM0D34-HTQM104922", "MonitorName": "C32F391" },
    "Chats": { "Mute": true }
  }
}
```

The key is the readable setup identity (§3), not a hash: easy to check by hand.

`LastSeen` is shown in the tray list and allows cleaning up later. Automatic cleanup is out of scope.

## 8. Details decided while building

* **Mute ends while a muted meeting popup is open:** it stays quiet (no beep); it only starts flashing again.
* **Several at once** (③ plus a time-based one, or mute plus snooze): each just applies; snoozed wins over muted.
  No extra UI.
* **Mirror with different resolutions**, where Windows scales one monitor: it is still one group, and placement
  uses that group's Windows `Screen`.
* **Renaming in code:** `Hush` became `Quiet`: meetings muted, chats muted, chats snoozed, each made of
  "until" / "while these screens" / "remembered for this setup". The Hush button and texts are renamed.

## 9. Acceptance

1. At the desk (2 external monitors, panel off): ⋯ → "Move to ASUS VG24" on a meeting popup. The meeting popups
   move there, and after a reboot or re-dock they still appear there. Chats stay where config.json says.
2. Laptop panel switched on as well (3 active monitors): this is a different setup, so popups go by config.json
   again until told otherwise.
3. Mirrored to a TV: Snooze ▾ → "Always in this screen setup" on the chat popup, Mute ▾ → the same on a meeting
   popup. Chats are hidden and silent; meeting popups appear without sound or flash. Unplug: everything is normal
   again, and the chat popup comes back flashing if anything new arrived. Mirror to the same TV the next day:
   the same again without asking.
4. The same TV extended instead of mirrored: a different setup, nothing muted or snoozed.
5. Tray "Screen setups" lists all three setups with their choices. Forgetting one takes effect immediately when
   that setup is active.
6. Swapping which monitor is primary is a different setup. Dragging the monitors around in Display settings is not.
7. Mute ▾ → 15 minutes on a meeting popup: it stays visible, silent and calm. On the chat popup, Mute keeps it
   visible and silent, Snooze makes it go away.
