# Making an avatar pack

An avatar pack is a folder of sprite sheets plus one `avatar.json` manifest. Deskmate
ships with two, `mint` and `slate`, in [`avatars/`](avatars). Drop a new folder next to them
and it appears in the avatar picker (Settings > Avatar, and first-run setup).

```
avatars/
  mypack/
    avatar.json
    idle.png
    wave.png
    ...
```

## Sprite sheets

Each animation is **one PNG strip**: all frames the same size, side by side, left to right,
with a transparent background.

- Every frame is `frameSize.width` × `frameSize.height` pixels (the built-in packs use 180×180).
- A sheet with N frames is exactly `N × width` pixels wide and `height` pixels tall.
- Deskmate scales the avatar for the user's size setting, so draw at the largest size you want it shown.

## `avatar.json`

```json
{
  "name": "Mint",
  "frameSize": { "width": 180, "height": 180 },
  "animations": {
    "idle": { "sheet": "idle.png", "frames": 4, "fps": 4, "loop": true },
    "wave": { "sheet": "wave.png", "frames": 4, "fps": 8, "loop": false }
  }
}
```

| Field | Meaning |
| --- | --- |
| `name` | Display name of the pack. |
| `frameSize` | Width and height of one frame, in pixels. |
| `animations.<name>.sheet` | PNG file name in the pack folder (a file name only, not a path). |
| `frames` | Number of frames in the strip. |
| `fps` | Playback speed, frames per second. |
| `loop` | `true` repeats until the avatar changes state. `false` plays once and then Deskmate returns to idle. |

## Animations Deskmate plays

Only `idle` is required. An animation you leave out is simply not shown (the avatar keeps
its previous look), so you can ship a pack in stages.

| Name | When it plays | Loop |
| --- | --- | --- |
| `idle` | Default resting pose. **Required.** | yes |
| `typing` | While you're typing. | yes |
| `wave` | You click the avatar; also used when it wakes up. | no |
| `held` | You're dragging it. | yes |
| `look` | Idle gesture after a pause in typing. | no |
| `stretch` | Idle gesture after a pause in typing. | no |
| `coffee` | Idle gesture, and when a Pomodoro break starts. | no |
| `yawn` | Just before it suggests a break. | no |
| `sleeping` | You've been away for a while. | yes |
| `sign` | Holds up a sign for break suggestions and reminder alerts. | yes |

One-shot animations (`loop: false`) matter: the avatar stays in that pose until the animation
finishes, so keep their frame count and fps short enough not to feel slow.
With **Reduce motion** on, Deskmate shows the first frame of each animation instead of playing it,
so make frame 1 a good still pose.

## Checking your pack

Deskmate validates every pack when it starts and lists all problems at once in the log, for example:

```
Animation 'wave': 'wave.png' is 600x180, but 4 frame(s) of 180x180 need 720x180.
```

A pack with problems is hidden from the picker. If the chosen pack breaks (say, you edit it
while it's selected), Deskmate falls back to `mint` rather than failing to start.

## Generating a pack from code

[`tools/make_slate_pack.py`](tools/make_slate_pack.py) draws the `slate` pack with Pillow. It is a
small worked example: copy it, change the drawing functions and run it to get a complete pack.
