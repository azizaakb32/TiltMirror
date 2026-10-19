import os
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

OUT = "/tmp/claude-0/-home-claude/e896cd64-1e19-5800-8c0a-acbad67c4368/scratchpad/TiltMirror/Assets/Resources/TiltMirror/Audio"
os.makedirs(OUT, exist_ok=True)
SR = 44100
rng = np.random.default_rng(7)


def t_(dur):
    return np.arange(int(SR * dur)) / SR


def bell(freq, dur=0.8, amp=1.0):
    t = t_(dur)
    partials = [(1.0, 1.0, 3.0), (2.0, 0.45, 4.5), (3.01, 0.25, 6.0), (4.2, 0.12, 9.0), (5.4, 0.06, 12.0)]
    y = sum(a * np.sin(2 * np.pi * freq * r * t) * np.exp(-dr * t) for r, a, dr in partials)
    return y * np.minimum(1.0, t / 0.003) * amp


def place(buf, sig, start):
    i = int(start * SR)
    n = min(len(sig), len(buf) - i)
    buf[i:i + n] += sig[:n]


def fade(y, ms=8):
    n = int(SR * ms / 1000)
    y = y.copy()
    y[-n:] *= np.linspace(1, 0, n)
    return y


def norm(y, peak=0.8):
    return y / np.max(np.abs(y)) * peak


def save(name, y):
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, (np.clip(y, -1, 1) * 32767).astype(np.int16))
    print("wrote", name + ".wav", round(len(y) / SR, 2), "s")


def bandpass(y, lo, hi):
    sos = butter(2, [lo, hi], btype="band", fs=SR, output="sos")
    return sosfilt(sos, y)


# move: two soft plucks a fifth apart (sun and moon) + short airy whoosh
t = t_(0.2)
sun = np.sin(2 * np.pi * 392.0 * t) * np.exp(-22 * t)
moon = np.sin(2 * np.pi * 587.33 * t) * np.exp(-22 * t) * 0.8
air = bandpass(rng.standard_normal(len(t)), 900, 3200) * np.sin(np.pi * np.clip(t / 0.12, 0, 1)) * 0.22
y = (sun + moon) * np.minimum(1, t / 0.002) + air
save("sfx_move", fade(norm(y, 0.7)))

# blocked: low soft thud
t = t_(0.16)
f = 55 + 70 * np.exp(-9 * t)
y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-26 * t)
y += rng.standard_normal(len(t)) * np.exp(-400 * t) * 0.25
save("sfx_blocked", fade(norm(y, 0.8)))

# join: rising arpeggio, the two halves becoming one
buf = np.zeros(int(SR * 1.6))
for i, f0 in enumerate((523.25, 659.25, 783.99, 1046.5)):
    place(buf, bell(f0, 1.2, 0.8), i * 0.075)
place(buf, bell(261.63, 1.4, 0.45), 0.0)
place(buf, bell(2093.0, 1.0, 0.18), 0.25)
save("sfx_join", fade(norm(buf, 0.85), 40))

# stars: three rising single bells
for i, f0 in enumerate((783.99, 987.77, 1174.66), start=1):
    save("sfx_star_%d" % i, fade(norm(bell(f0, 0.8, 1.0), 0.75), 30))

# click
t = t_(0.04)
y = (np.sin(2 * np.pi * 1250 * t) + 0.5 * np.sin(2 * np.pi * 2500 * t)) * np.exp(-150 * t)
save("sfx_click", fade(norm(y, 0.6), 4))

# undo: short falling blip
t = t_(0.14)
f = 720 - 300 * (t / 0.14)
y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-14 * t) * np.minimum(1, t / 0.004)
save("sfx_undo", fade(norm(y, 0.6), 6))

# music: seamless 32 s ambient loop. Every element is wrapped around the loop end.
LOOP = 32.0
N = int(SR * LOOP)
mus = np.zeros(N)


def add_wrapped(sig, start_sample):
    idx = (start_sample + np.arange(len(sig))) % N
    mus[idx] += sig


chords = [
    [261.63, 329.63, 392.00, 493.88],   # Cmaj7
    [220.00, 261.63, 329.63, 392.00],   # Am7
    [174.61, 220.00, 261.63, 329.63],   # Fmaj7
    [196.00, 246.94, 293.66, 329.63],   # G6
]
win_len = 12.0
tw = t_(win_len)
window = np.sin(np.pi * tw / win_len) ** 2
for i, notes in enumerate(chords):
    seg = np.zeros(len(tw))
    for k, f0 in enumerate(notes):
        vib = 1 + 0.0015 * np.sin(2 * np.pi * (0.17 + 0.03 * k) * tw)
        for det in (0.997, 1.003):
            seg += np.sin(2 * np.pi * f0 * det * tw * vib) * 0.5
        seg += np.sin(2 * np.pi * f0 * 2 * tw) * 0.12
    add_wrapped(seg * window * 0.16, int((i * 8.0 - 2.0) * SR))

# sub drone with a slow tremolo (4 cycles per loop, so it wraps cleanly)
tt = np.arange(N) / SR
drone = round(65.41 * LOOP) / LOOP   # whole number of cycles per loop
mus += np.sin(2 * np.pi * drone * tt) * (0.14 + 0.04 * np.sin(2 * np.pi * (4 / LOOP) * tt))

# sparse high plucks on a C pentatonic scale
scale = [1046.5, 1174.66, 1318.51, 1567.98, 1760.0]
when = 1.0
while when < LOOP - 1.0:
    f0 = scale[int(rng.integers(len(scale)))]
    add_wrapped(bell(f0, 3.0, 0.07), int(when * SR))
    when += float(rng.uniform(1.8, 3.6))

save("music_ambient_loop", norm(mus, 0.6))
