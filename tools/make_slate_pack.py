#!/usr/bin/env python3
"""Generates the placeholder 'slate' avatar pack (avatars/slate) with Pillow.

    pip install pillow
    python3 tools/make_slate_pack.py

It is both a second built-in look and a worked example of the pack format (see AVATAR_PACKS.md):
one PNG strip per animation, frames laid out left to right, plus an avatar.json manifest.
Replace the drawing code with real art, or just draw your own sheets and write the manifest by hand.
"""
import json
import math
import os

from PIL import Image, ImageDraw

SIZE = 180          # frame width and height in the output
SS = 3              # supersampling factor for smooth edges
S = SIZE * SS

BODY = (91, 125, 177, 255)
OUTLINE = (52, 78, 122, 255)
INK = (36, 42, 58, 255)
CHEEK = (232, 150, 150, 255)
ACCENT = (250, 204, 90, 255)
WHITE = (255, 255, 255, 255)

# name: (frames, fps, loop): must match what the app plays (see AvatarPackValidator.KnownAnimations).
ANIMATIONS = {
    "idle": (4, 4, True),
    "wave": (4, 8, False),
    "held": (4, 6, True),
    "typing": (4, 8, True),
    "stretch": (12, 10, False),
    "coffee": (12, 8, False),
    "look": (10, 8, False),
    "sleeping": (6, 4, True),
    "yawn": (8, 8, False),
    "sign": (4, 6, True),
}


def px(v):
    return v * SS


def circle(d, cx, cy, r, fill, outline=None, width=0):
    d.ellipse([px(cx - r), px(cy - r), px(cx + r), px(cy + r)], fill=fill, outline=outline, width=px(width))


def body(d, dx=0, dy=0, sx=1.0, sy=1.0):
    w, h = 96 * sx, 96 * sy
    cx, cy = 90 + dx, 100 + dy
    d.rounded_rectangle([px(cx - w / 2), px(cy - h / 2), px(cx + w / 2), px(cy + h / 2)],
                        radius=px(28), fill=BODY, outline=OUTLINE, width=px(3))
    return cx, cy


def eyes(d, cx, cy, look=0.0, closed=False, wide=False, squint=False):
    for side in (-1, 1):
        x = cx + side * 20 + look * 5
        if closed or squint:
            d.arc([px(x - 7), px(cy - 14), px(x + 7), px(cy)], 200 if closed else 0, 340 if closed else 180, fill=INK, width=px(3))
        else:
            circle(d, x, cy - 8, 8 if wide else 6, INK)
            circle(d, x + 2, cy - 10, 2, WHITE)


def mouth(d, cx, cy, kind="smile", open_amount=0.0):
    if kind == "smile":
        d.arc([px(cx - 12), px(cy + 4), px(cx + 12), px(cy + 24)], 20, 160, fill=INK, width=px(3))
    elif kind == "flat":
        d.line([px(cx - 8), px(cy + 16), px(cx + 8), px(cy + 16)], fill=INK, width=px(3))
    elif kind == "open":
        r = 4 + 12 * open_amount
        circle(d, cx, cy + 16, r, INK)
    elif kind == "o":
        circle(d, cx, cy + 16, 6, INK)


def cheeks(d, cx, cy):
    for side in (-1, 1):
        circle(d, cx + side * 32, cy + 8, 6, CHEEK)


def hand(d, x, y):
    circle(d, x, y, 9, BODY, OUTLINE, 3)


def sheet_for(name, frames):
    strip = Image.new("RGBA", (S * frames, S), (0, 0, 0, 0))
    for i in range(frames):
        t = i / max(1, frames - 1)
        cell = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        draw_frame(ImageDraw.Draw(cell), name, i, frames, t)
        strip.paste(cell, (i * S, 0))
    return strip.resize((SIZE * frames, SIZE), Image.LANCZOS)


def draw_frame(d, name, i, frames, t):
    phase = i / frames * 2 * math.pi
    if name == "idle":
        cx, cy = body(d, dy=math.sin(phase) * 3)
        eyes(d, cx, cy, closed=(i == 2))
        mouth(d, cx, cy)
        cheeks(d, cx, cy)
    elif name == "wave":
        cx, cy = body(d)
        eyes(d, cx, cy)
        mouth(d, cx, cy, "open", 0.4)
        cheeks(d, cx, cy)
        hand(d, cx + 58, cy - 34 + math.sin(phase * 2) * 8)
    elif name == "held":
        wob = math.sin(phase) * 6
        cx, cy = body(d, dx=wob, dy=-8, sx=0.94, sy=1.06)
        eyes(d, cx, cy, wide=True)
        mouth(d, cx, cy, "o")
        for side in (-1, 1):
            hand(d, cx + side * 54, cy - 44)
    elif name == "typing":
        cx, cy = body(d, dy=(i % 2) * 2)
        eyes(d, cx, cy, look=0.0)
        mouth(d, cx, cy, "flat")
        hand(d, cx - 22, cy + 40 + (i % 2) * 6)
        hand(d, cx + 22, cy + 40 + ((i + 1) % 2) * 6)
    elif name == "stretch":
        k = math.sin(t * math.pi)
        cx, cy = body(d, dy=-10 * k, sx=1 - 0.08 * k, sy=1 + 0.16 * k)
        eyes(d, cx, cy, closed=True)
        mouth(d, cx, cy, "open", 0.5 * k)
        for side in (-1, 1):
            hand(d, cx + side * (48 + 8 * k), cy - 30 - 50 * k)
    elif name == "coffee":
        cx, cy = body(d)
        sipping = 3 <= i <= 8
        eyes(d, cx, cy, closed=sipping)
        mouth(d, cx, cy, "smile")
        cheeks(d, cx, cy)
        lift = math.sin(t * math.pi)
        cup_x, cup_y = cx + 50 - 34 * lift, cy + 40 - 36 * lift
        d.rounded_rectangle([px(cup_x - 12), px(cup_y - 12), px(cup_x + 12), px(cup_y + 12)], radius=px(4), fill=WHITE, outline=OUTLINE, width=px(3))
        d.arc([px(cup_x + 6), px(cup_y - 6), px(cup_x + 20), px(cup_y + 8)], 270, 90, fill=OUTLINE, width=px(3))
        hand(d, cup_x + 14, cup_y + 6)
        for k in range(2):
            sy = cup_y - 18 - ((i + k * 3) % 6) * 2
            d.line([px(cup_x - 4 + k * 8), px(sy), px(cup_x - 4 + k * 8), px(sy - 8)], fill=(255, 255, 255, 160), width=px(2))
    elif name == "look":
        look = math.sin(t * 2 * math.pi) * 1.0
        cx, cy = body(d, dx=look * 4)
        eyes(d, cx + look * 4, cy, look=look)
        mouth(d, cx, cy, "flat")
    elif name == "sleeping":
        cx, cy = body(d, dy=math.sin(phase) * 2.5, sy=1 - 0.02 * math.sin(phase))
        eyes(d, cx, cy, closed=True)
        mouth(d, cx, cy, "flat")
        for k in range(3):
            zx, zy = cx + 40 + k * 12, cy - 44 - k * 14 - (i % 3) * 2
            d.text((px(zx), px(zy)), "z", fill=INK, font_size=px(12 + k * 4)) if hasattr(d, "text") else None
    elif name == "yawn":
        k = math.sin(t * math.pi)
        cx, cy = body(d, dy=-3 * k)
        eyes(d, cx, cy, squint=True)
        mouth(d, cx, cy, "open", k)
        hand(d, cx + 34, cy + 22 - 30 * k)
    elif name == "sign":
        cx, cy = body(d, dy=12 - abs(math.sin(phase)) * 4)
        eyes(d, cx, cy)
        mouth(d, cx, cy, "smile")
        # A small "!" sign held overhead.
        top = cy - 70
        d.rounded_rectangle([px(cx - 18), px(top - 26), px(cx + 18), px(top + 10)], radius=px(6), fill=ACCENT, outline=OUTLINE, width=px(3))
        d.line([px(cx), px(top - 20), px(cx), px(top - 6)], fill=INK, width=px(4))
        circle(d, cx, top + 2, 2.5, INK)
        d.line([px(cx), px(top + 10), px(cx), px(cy - 46)], fill=OUTLINE, width=px(3))


def main():
    out = os.path.join(os.path.dirname(__file__), "..", "avatars", "slate")
    os.makedirs(out, exist_ok=True)
    manifest = {"name": "Slate", "frameSize": {"width": SIZE, "height": SIZE}, "animations": {}}
    for name, (frames, fps, loop) in ANIMATIONS.items():
        sheet_for(name, frames).save(os.path.join(out, f"{name}.png"))
        manifest["animations"][name] = {"sheet": f"{name}.png", "frames": frames, "fps": fps, "loop": loop}
    with open(os.path.join(out, "avatar.json"), "w") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")
    print(f"Wrote {len(ANIMATIONS)} animations to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()
