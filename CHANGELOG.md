# Changelog

## Unreleased

- Fixed: the morning greeting now mentions how many reminders are due today (e.g. "Good
  morning, Alex! You've got 3 reminders today!"), as the original v0.1 plan called for but the
  initial greeting implementation never actually did.

- Chat window gained a "Clear" button (resets the conversation without closing the window) and
  closes on Escape.

- Chat now remembers the last 20 messages of the open conversation, so follow-up questions and
  references to earlier turns actually land, instead of each reply being answered in isolation.

- Chat (quick menu > Chat…, or press C): talk to the avatar directly and get a Claude-written
  reply, using the same Anthropic API key/model as AI messages and your "About me" context.
  Disabled with an explanatory note when AI messages aren't configured.

- Calendar sync now also expands monthly/yearly events on an ordinal weekday ("the 3rd Thursday of the month", "the last Friday", Thanksgiving-style yearly anniversaries) into recurring reminders.

- Calendar sync now also expands weekly recurring events on specific weekdays ("every Mon/Wed/Fri"), not just plain weekly/daily/monthly/yearly; reminders gained an optional weekday set alongside the existing recurrence end date.

- Calendar sync now expands simple recurring events (daily/weekly/monthly/yearly, with an optional end date or count) into recurring reminders, instead of skipping every recurring event; reminders also gained an optional recurrence end date.

- Calendar sync (Settings > Calendar): imports events from a calendar's private ICS subscription URL (Google/Outlook/Apple, no OAuth needed) into reminders, re-synced periodically and on demand, without disturbing manually-created reminders. Recurring events are not yet expanded.
- Optional AI-generated messages (Settings > AI messages): rewrites greetings, break suggestions, and Pomodoro messages via the Anthropic API using your own key, falling back to the built-in phrasing when disabled, unconfigured, or on any failure/timeout.
- Linux support: start-at-login (XDG autostart), sleep/wake detection (systemd-logind), full-screen auto-hide (X11 EWMH), and native notifications (org.freedesktop.Notifications). CI now builds and tests on Ubuntu too.
- Avatar packs are validated on load with clear, complete error messages; broken packs are hidden from the picker and a bad selected pack falls back to `mint`.
- New built-in `slate` avatar pack, plus a guide to making your own packs (AVATAR_PACKS.md).
- Pomodoro focus sessions: start/skip/stop from the tray, configurable lengths, avatar and tone-aware messages, live time-left tray tooltip.

## 1.0.0

First public release.

- Animated avatar in the corner of your screen: types along, takes breaks, falls asleep, suggests rest.
- Morning greeting by name, with time-of-day, weekend and tone variations (cheerful / calm / minimal).
- Reminders with day-before and on-the-day alerts, recurrence and snooze, including catch-up after sleep.
- Native notification fallback when the avatar is hidden.
- Pause mode from the tray, quiet hours, multi-monitor and display-scaling support, auto-hide during full-screen apps.
- Accessibility: reduced-motion option and keyboard access (Enter/Space for the menu, S for settings, access keys in Settings).
- Velopack installers for Windows and macOS with automatic background updates.
