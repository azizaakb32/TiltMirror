import math
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ASSETS = "/tmp/claude-0/-home-claude/e896cd64-1e19-5800-8c0a-acbad67c4368/scratchpad/TiltMirror/Assets"
SPR = ASSETS + "/Resources/TiltMirror/Sprites"
UI = ASSETS + "/Resources/TiltMirror/UI"
ICON = ASSETS + "/Art/AppIcon"
for p in (SPR, UI, ICON):
    os.makedirs(p, exist_ok=True)

SS = 4  # supersampling factor

SUN_TOP, SUN_BOT, SUN_RIM = (0xFF, 0xD2, 0x5A), (0xFF, 0x9F, 0x1C), (0xE0, 0x82, 0x00)
MOON_TOP, MOON_BOT, MOON_RIM = (0xE6, 0xFF, 0xFC), (0x9F, 0xE8, 0xE0), (0x6F, 0xC4, 0xBB)
SUN_WALL = ((0x9A, 0x42, 0x2D), (0x7A, 0x2E, 0x1F), (0x55, 0x1C, 0x10))
MOON_WALL = ((0x8E, 0x94, 0xF6), (0x6C, 0x73, 0xEA), (0x4A, 0x50, 0xC0))
PEACH, NAVY = (0xFF, 0xD9, 0xBF), (0x1A, 0x1C, 0x4D)
GOLD, MINT = (0xFF, 0xB0, 0x20), (0xBF, 0xF3, 0xEE)
WALL_BROWN = (0x7A, 0x2E, 0x1F)
WHITE = (255, 255, 255, 255)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def finish(rgb, alpha, size):
    rgba = np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA").resize(size, Image.LANCZOS)


# ---------------------------------------------------------------- game sprites

def half_disc(w, h, round_left, top, bottom, rim, hi=0.38):
    W, H = w * SS, h * SS
    R = H / 2
    cx = W if round_left else 0
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).ellipse([cx - R, 0, cx + R, H - 1], fill=255)
    alpha = np.asarray(mask, float) / 255
    yy, xx = np.mgrid[0:H, 0:W]
    t = (yy / (H - 1))[:, :, None]
    rgb = np.array(top, float) * (1 - t) + np.array(bottom, float) * t
    dist = np.hypot(xx + 0.5 - cx, yy + 0.5 - R) / R
    rk = (smooth(0.78, 1.0, dist) * 0.55)[:, :, None]
    rgb = rgb * (1 - rk) + np.array(rim, float) * rk
    hx = cx - 0.52 * R if round_left else cx + 0.52 * R
    hy = R - 0.50 * R
    g = (np.exp(-(((xx - hx) / (0.30 * R)) ** 2 + ((yy - hy) / (0.20 * R)) ** 2)) * hi)[:, :, None]
    rgb = rgb * (1 - g) + 255 * g
    return finish(rgb, alpha, (w, h))


def ring(w, h, round_left, color, width=13, dashes=9, dash=14, gap=6):
    W, H = w * SS, h * SS
    R = H // 2
    cx = W if round_left else 0
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = R - (width * SS) / 2 - 2 * SS
    start = 90 if round_left else -90
    for i in range(dashes):
        a0 = start + i * (dash + gap)
        d.arc([cx - r, R - r, cx + r, R + r], a0, a0 + dash, fill=color + (255,), width=int(width * SS))
    return im.resize((w, h), Image.LANCZOS)


def wall(colors, size=256, radius=56):
    top, mid, shade = colors
    S = size * SS

    def rr(offset_y):
        m = Image.new("L", (S, S), 0)
        ImageDraw.Draw(m).rounded_rectangle([0, offset_y, S - 1, S - 1 + offset_y], radius=radius * SS, fill=255)
        return np.asarray(m, float) / 255

    base = rr(0)
    face = rr(-int(0.055 * S)) * base
    yy = np.mgrid[0:S, 0:S][0]
    t = (yy / (S - 1))[:, :, None]
    facecol = np.array(top, float) * (1 - t) + np.array(mid, float) * t
    f3 = face[:, :, None]
    rgb = np.array(shade, float)[None, None, :] * (1 - f3) + facecol * f3
    sheen = (np.exp(-(((yy - 0.14 * S) / (0.07 * S)) ** 2)))[:, :, None] * 0.14 * f3
    rgb = rgb * (1 - sheen) + 255 * sheen
    return finish(rgb, base, (size, size))


def seam_glow(size=128):
    x = np.linspace(-1, 1, size)[None, :]
    y = np.linspace(0, 1, size)[:, None]
    a = np.exp(-((x / 0.38) ** 2)) * smooth(0, 0.12, y) * smooth(1, 0.88, y)
    rgba = np.dstack([np.full((size, size), 255.0)] * 3 + [a * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def dot(size=64):
    S = size * SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([0, 0, S - 1, S - 1], fill=WHITE)
    return im.resize((size, size), Image.LANCZOS)


def eyes_overlay(width, height, centers, radius, colors):
    im = Image.new("RGBA", (width * SS, height * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for (x, y), c in zip(centers, colors):
        d.ellipse([(x - radius) * SS, (y - radius) * SS, (x + radius) * SS, (y + radius) * SS], fill=c + (255,))
    return im.resize((width, height), Image.LANCZOS)


def disc(d, seam=True, eyes=True):
    w = d // 2
    left = half_disc(w, d, True, SUN_TOP, SUN_BOT, SUN_RIM)
    right = half_disc(w, d, False, MOON_TOP, MOON_BOT, MOON_RIM)
    im = Image.new("RGBA", (2 * w, d), (0, 0, 0, 0))
    im.paste(left, (0, 0))
    im.paste(right, (w, 0))
    R = d / 2
    if eyes:
        ov = eyes_overlay(2 * w, d, [(w - 0.24 * R, 0.76 * R), (w + 0.24 * R, 0.76 * R)], 0.085 * R, [WALL_BROWN, NAVY])
        im = Image.alpha_composite(im, ov)
    if seam:
        sw = max(2, int(d * 0.016))
        ov = Image.new("RGBA", im.size, (0, 0, 0, 0))
        ImageDraw.Draw(ov).rectangle([w - sw // 2, 0, w + sw // 2 - 1, d], fill=WHITE)
        im = Image.alpha_composite(im, ov)
    return im


# ---------------------------------------------------------------- UI icons

def cv(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def fin(im, w, h):
    return im.resize((w, h), Image.LANCZOS)


def line(d, pts, width, fill=WHITE):
    pts = [(x * SS, y * SS) for x, y in pts]
    d.line(pts, fill=fill, width=int(width * SS), joint="curve")
    r = width * SS / 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse([x - r, y - r, x + r, y + r], fill=fill)


def arc(d, cx, cy, r, a0, a1, width, fill=WHITE):
    d.arc([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], a0, a1, fill=fill, width=int(width * SS))


def ui_back():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    line(d, [(80, 24), (40, 64), (80, 104)], 14)
    return fin(im, 128, 128)


def arrow_head(d, px, py, theta_deg, size=16, fill=WHITE):
    th = math.radians(theta_deg)
    tx, ty = -math.sin(th), math.cos(th)    # clockwise tangent (y down)
    nx, ny = math.cos(th), math.sin(th)
    tip = (px + tx * size * 0.9, py + ty * size * 0.9)
    b1 = (px - tx * size * 0.4 + nx * size, py - ty * size * 0.4 + ny * size)
    b2 = (px - tx * size * 0.4 - nx * size, py - ty * size * 0.4 - ny * size)
    d.polygon([(tip[0] * SS, tip[1] * SS), (b1[0] * SS, b1[1] * SS), (b2[0] * SS, b2[1] * SS)], fill=fill)


def ui_restart():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    arc(d, 64, 66, 38, -60, 235, 13)
    th = math.radians(235)
    arrow_head(d, 64 + 38 * math.cos(th), 66 + 38 * math.sin(th), 235, size=17)
    return fin(im, 128, 128)


def ui_undo():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    line(d, [(40, 52), (84, 52)], 13)
    arc(d, 84, 78, 26, -90, 90, 13)
    line(d, [(84, 104), (50, 104)], 13)
    d.polygon([(22 * SS, 52 * SS), (52 * SS, 26 * SS), (52 * SS, 78 * SS)], fill=WHITE)
    return fin(im, 128, 128)


def ui_lock():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    d.rounded_rectangle([30 * SS, 56 * SS, 98 * SS, 108 * SS], radius=12 * SS, fill=WHITE)
    arc(d, 64, 48, 20, 180, 360, 12)
    line(d, [(44, 48), (44, 60)], 12)
    line(d, [(84, 48), (84, 60)], 12)
    d.ellipse([57 * SS, 72 * SS, 71 * SS, 86 * SS], fill=(0, 0, 0, 0))
    return fin(im, 128, 128)


def ui_star():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    pts = []
    for i in range(10):
        ang = math.radians(-90 + i * 36)
        r = 56 if i % 2 == 0 else 24
        pts.append(((64 + r * math.cos(ang)) * SS, (68 + r * math.sin(ang)) * SS))
    d.polygon(pts, fill=WHITE)
    d.line(pts + [pts[0]], fill=WHITE, width=int(8 * SS), joint="curve")
    return fin(im, 128, 128)


def ui_play():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    pts = [(44, 28), (44, 100), (102, 64)]
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=WHITE)
    d.line([(x * SS, y * SS) for x, y in pts + [pts[0]]], fill=WHITE, width=int(10 * SS), joint="curve")
    return fin(im, 128, 128)


def ui_settings():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    for y, kx in ((38, 84), (64, 46), (90, 76)):
        line(d, [(18, y), (110, y)], 10)
        d.ellipse([(kx - 15) * SS, (y - 15) * SS, (kx + 15) * SS, (y + 15) * SS], fill=(0, 0, 0, 0))
        d.ellipse([(kx - 13) * SS, (y - 13) * SS, (kx + 13) * SS, (y + 13) * SS], fill=WHITE)
    return fin(im, 128, 128)


def ui_levels():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    for x in (20, 70):
        for y in (20, 70):
            d.rounded_rectangle([x * SS, y * SS, (x + 38) * SS, (y + 38) * SS], radius=10 * SS, fill=WHITE)
    return fin(im, 128, 128)


def speaker(d):
    pts = [(20, 50), (42, 50), (68, 28), (68, 100), (42, 78), (20, 78)]
    d.polygon([(x * SS, y * SS) for x, y in pts], fill=WHITE)
    d.line([(x * SS, y * SS) for x, y in pts + [pts[0]]], fill=WHITE, width=int(6 * SS), joint="curve")


def ui_sound_on():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    speaker(d)
    arc(d, 68, 64, 22, -48, 48, 10)
    arc(d, 68, 64, 40, -48, 48, 10)
    return fin(im, 128, 128)


def ui_sound_off():
    im = cv(128, 128); d = ImageDraw.Draw(im)
    speaker(d)
    line(d, [(84, 46), (112, 82)], 10)
    line(d, [(112, 46), (84, 82)], 10)
    return fin(im, 128, 128)


def round_rect(size=128, radius=40):
    S = size * SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im).rounded_rectangle([0, 0, S - 1, S - 1], radius=radius * SS, fill=WHITE)
    return im.resize((size, size), Image.LANCZOS)


def circle(size=128):
    S = size * SS
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([0, 0, S - 1, S - 1], fill=WHITE)
    return im.resize((size, size), Image.LANCZOS)


def dpad(kind):
    im = cv(44, 28); d = ImageDraw.Draw(im)
    W = 2.6
    g, m = GOLD + (255,), MINT + (255,)

    def ln(pts, col):
        pts = [(x * SS, y * SS) for x, y in pts]
        d.line(pts, fill=col, width=int(W * SS), joint="curve")
        r = W * SS / 2
        for x, y in (pts[0], pts[-1]):
            d.ellipse([x - r, y - r, x + r, y + r], fill=col)

    if kind == "up":
        for dx, col in ((0, g), (22, m)):
            ln([(11 + dx, 22), (11 + dx, 6)], col); ln([(6 + dx, 11), (11 + dx, 6), (16 + dx, 11)], col)
    elif kind == "down":
        for dx, col in ((0, g), (22, m)):
            ln([(11 + dx, 6), (11 + dx, 22)], col); ln([(6 + dx, 17), (11 + dx, 22), (16 + dx, 17)], col)
    elif kind == "left":     # sun left, moon right (outward)
        ln([(17, 14), (5, 14)], g); ln([(10, 9), (5, 14), (10, 19)], g)
        ln([(27, 14), (39, 14)], m); ln([(34, 9), (39, 14), (34, 19)], m)
    else:                    # right: sun right, moon left (inward)
        ln([(5, 14), (17, 14)], g); ln([(12, 9), (17, 14), (12, 19)], g)
        ln([(39, 14), (27, 14)], m); ln([(32, 9), (27, 14), (32, 19)], m)
    return fin(im, 176, 112)


# ---------------------------------------------------------------- app icon

def blend(a, b, k):
    return tuple(int(a[i] * (1 - k) + b[i] * k) for i in range(3))


def split_bg(size, cells=8):
    im = Image.new("RGB", (size, size))
    d = ImageDraw.Draw(im)
    half = size // 2
    d.rectangle([0, 0, half - 1, size], fill=PEACH)
    d.rectangle([half, 0, size, size], fill=NAVY)
    step = size // cells
    lw = max(1, size // 300)
    cl, cr = blend(PEACH, WALL_BROWN, 0.10), blend(NAVY, MINT, 0.08)
    for i in range(0, size + 1, step):
        d.line([(i, 0), (i, size)], fill=cl if i < half else cr, width=lw)
        d.line([(0, i), (half, i)], fill=cl, width=lw)
        d.line([(half, i), (size, i)], fill=cr, width=lw)
    return im


def app_icon(size, with_walls=True):
    bg = split_bg(size).convert("RGBA")
    step = size // 8
    if with_walls:
        ws = int(step * 0.86)
        sw = wall(SUN_WALL, size=ws)
        mw = wall(MOON_WALL, size=ws)
        off = (step - ws) // 2
        for cx, cy in ((0, 1), (0, 6)):
            bg.alpha_composite(sw, (cx * step + off, cy * step + off))
        for cx, cy in ((7, 1), (7, 6)):
            bg.alpha_composite(mw, (cx * step + off, cy * step + off))
    # glowing seam
    glow = Image.new("L", (size, size), 0)
    ImageDraw.Draw(glow).rectangle([size // 2 - size // 70, 0, size // 2 + size // 70, size], fill=255)
    glow = glow.filter(ImageFilter.GaussianBlur(size / 60))
    white = Image.new("RGBA", (size, size), (255, 255, 255, 255))
    bg = Image.composite(white, bg, glow.point(lambda v: int(v * 0.55)))
    line_w = max(3, size // 64)
    ImageDraw.Draw(bg).rectangle([size // 2 - line_w // 2, 0, size // 2 + line_w // 2 - 1, size], fill=WHITE)
    dd = int(size * 0.64)
    dimg = disc(dd)
    bg.alpha_composite(dimg, ((size - dimg.width) // 2, (size - dd) // 2))
    return bg.convert("RGB")


def adaptive_fg(size=432):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    dd = 232
    dimg = disc(dd)
    im.alpha_composite(dimg, ((size - dimg.width) // 2, (size - dd) // 2))
    return im


def adaptive_mono(size=432):
    dd = 232
    dimg = disc(dd, seam=False, eyes=False)
    alpha = np.asarray(dimg.split()[3], float) / 255
    h, w = alpha.shape
    gap = 6
    alpha[:, w // 2 - gap // 2: w // 2 + gap // 2] = 0
    ys, xs = np.mgrid[0:h, 0:w]
    R = h / 2
    for ex in (w / 2 - 0.24 * R, w / 2 + 0.24 * R):
        alpha[np.hypot(xs - ex, ys - 0.76 * R) < 0.085 * R] = 0
    rgba = np.dstack([np.full((h, w), 255.0)] * 3 + [alpha * 255]).astype(np.uint8)
    mono = Image.fromarray(rgba, "RGBA")
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.alpha_composite(mono, ((size - w) // 2, (size - h) // 2))
    return out


# ---------------------------------------------------------------- export

def save(img, folder, name):
    path = os.path.join(folder, name)
    img.save(path)
    print("wrote", os.path.relpath(path, ASSETS), img.size)


# game sprites (pre-colored art, 1 unit = 256 px)
save(half_disc(128, 256, True, SUN_TOP, SUN_BOT, SUN_RIM), SPR, "sun_half.png")
save(half_disc(128, 256, False, MOON_TOP, MOON_BOT, MOON_RIM), SPR, "moon_half.png")
save(ring(128, 256, True, WALL_BROWN), SPR, "sun_ring.png")
save(ring(128, 256, False, MINT), SPR, "moon_ring.png")
save(wall(SUN_WALL), SPR, "wall_sun.png")
save(wall(MOON_WALL), SPR, "wall_moon.png")
save(seam_glow(), SPR, "seam_glow.png")
save(dot(), SPR, "eye_dot.png")

# UI
save(disc(512), UI, "logo_disc.png")
for name, fn in (("ui_back", ui_back), ("ui_restart", ui_restart), ("ui_undo", ui_undo), ("ui_lock", ui_lock),
                 ("ui_star", ui_star), ("ui_play", ui_play), ("ui_settings", ui_settings), ("ui_levels", ui_levels),
                 ("ui_sound_on", ui_sound_on), ("ui_sound_off", ui_sound_off)):
    save(fn(), UI, name + ".png")
save(round_rect(), UI, "ui_round_rect.png")
save(circle(), UI, "ui_circle.png")
for k in ("up", "down", "left", "right"):
    save(dpad(k), UI, "dpad_%s.png" % k)

# app icons
icon = app_icon(1024)
save(icon, ICON, "AppIcon_1024.png")
save(icon.resize((512, 512), Image.LANCZOS), ICON, "PlayStore_Icon_512.png")
bg_only = split_bg(432)
save(bg_only, ICON, "AndroidAdaptive_Background_432.png")
save(adaptive_fg(), ICON, "AndroidAdaptive_Foreground_432.png")
save(adaptive_mono(), ICON, "AndroidAdaptive_Monochrome_432.png")
