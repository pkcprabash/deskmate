# Deskmate

Deskmate is a small animated coworker that lives in the corner of your screen.
It types along when you type, takes little breaks when you pause, falls asleep
when you're away, nudges you to rest after long stretches of work, greets you
by name each morning, and reminds you about the things that matter, the day
before and on the day.

## Focus sessions (Pomodoro)

Start a focus session from the tray icon. Deskmate cheers you into each focus period,
takes a coffee break with you when it ends, and moves to a long break after a set number of
sessions. The tray tooltip shows the time left. Lengths are set in Settings > Focus. While a
session runs, the regular "time for a break?" nudge is turned off. Messages go to a native
notification if the avatar is hidden, and stay silent during quiet hours or pause.

## Calendar sync (optional)

Settings > Calendar imports events from a calendar's private ICS subscription URL — the
"secret address" Google Calendar, Outlook.com, and Apple Calendar each let you copy out,
with no OAuth app registration needed. Deskmate re-syncs it every few hours, plus on demand
via "Sync now": new events become reminders, changed ones update in place, and ones removed
from the feed are removed here too (only upcoming ones — past history is left alone). Manually
created reminders are never touched by a sync. Simple recurring events (daily/weekly/monthly/
yearly, optionally ending on a date or after a count) import as recurring reminders; anything
more specific — particular weekdays, a day-of-month rule, or any other pattern — is skipped
rather than risk landing on the wrong dates.

## AI messages (optional)

Settings > AI messages lets Deskmate rewrite its greetings, break nudges, and focus-session
lines with Claude, using your own Anthropic API key. Off by default. The key and model choice
are stored only in Deskmate's local database and are sent to no one but api.anthropic.com;
the default model is Claude Haiku 4.5, chosen for speed and cost on these short, frequent
messages, and is configurable. If the call is slow, fails, or is disabled, Deskmate falls back
to its built-in phrasing — you'"'"'ll never see a blank message.

## Platforms

Deskmate runs on Windows, macOS, and Linux (X11 or XWayland). A few OS-specific behaviors:

| Behavior | Windows | macOS | Linux |
| --- | --- | --- | --- |
| Start at login | Registry Run key | LaunchAgent | XDG autostart (`~/.config/autostart`) |
| Wake on unlock/resume | `SystemEvents` | `NSWorkspace` notifications | systemd-logind `PrepareForSleep` (sleep/wake only; no single cross-desktop lock/unlock signal) |
| Full-screen auto-hide | Foreground window vs. monitor bounds | `CGWindowListCopyWindowInfo` | X11 EWMH (`_NET_ACTIVE_WINDOW` / `_NET_WM_STATE_FULLSCREEN`); no-ops on pure Wayland (no X server) |
| Native notifications | `DesktopNotifications.Windows` | `DesktopNotifications.Apple` | `DesktopNotifications.FreeDesktop` (org.freedesktop.Notifications over D-Bus) |

Linux installers aren'"'"'t built yet (see `build/pack-*`); run from source with `dotnet run --project src/Deskmate.App` in the meantime.

## Avatar packs

Deskmate ships with two looks (`mint` and `slate`) and supports your own. See
[AVATAR_PACKS.md](AVATAR_PACKS.md) for the sprite-sheet format and a guide to making one.

## Install

Download the latest installer for Windows or macOS from
[GitHub Releases](https://github.com/pkcprabash/deskmate/releases). Deskmate updates
itself in the background; updates apply the next time it starts.

## Building and releasing

```
dotnet build
dotnet test
```

Installers are built with [Velopack](https://velopack.io) (`dotnet tool install -g vpk`):

```
build/pack-macos.sh 1.0.0        # macOS
build/pack-windows.ps1 -Version 1.0.0   # Windows
```

Without signing credentials these produce unsigned local builds. To ship a release, push a
tag (`git tag v1.0.0 && git push origin v1.0.0`); the Release workflow builds, signs,
notarizes and publishes both installers. Signing uses these repository secrets:

| Secret | Purpose |
| --- | --- |
| `WINDOWS_SIGN_PARAMS` | Parameters passed to signtool for Windows code signing |
| `APPLE_CERTIFICATE_P12`, `APPLE_CERTIFICATE_PASSWORD` | Base64 Developer ID certificates (application + installer) |
| `APPLE_APP_IDENTITY`, `APPLE_INSTALL_IDENTITY` | Certificate names for app / installer signing |
| `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_PASSWORD` | Credentials for notarization |
