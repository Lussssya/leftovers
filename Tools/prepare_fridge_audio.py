"""Cut CC0 refrigerator foley into short Unity clips.

Input: decoded mono 48 kHz PCM WAV of BigSoundBank #1158 (Joseph SARDIN).
Usage: python prepare_fridge_audio.py path/to/decoded.wav
Requires numpy. Source/license details are in Assets/Audio/Fridge/CREDITS.md.
"""
from pathlib import Path
import sys
import wave
import numpy as np

out = Path(__file__).resolve().parents[1] / 'Assets/Audio/Fridge'
out.mkdir(parents=True, exist_ok=True)
with wave.open(sys.argv[1]) as w:
    rate = w.getframerate()
    assert w.getnchannels() == 1 and w.getsampwidth() == 2
    source = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float) / 32768


def cut(name, start, end, peak, soften=False, loop=False):
    a = source[int(start * rate):int(end * rate)].copy()
    # Remove the steady compressor/DC bed, retain the seal and cabinet transients.
    low = 0.0
    alpha = 1 - np.exp(-2 * np.pi * 95 / rate)
    for i, value in enumerate(a):
        low += alpha * (value - low)
        a[i] -= low
    if soften:
        low = 0.0
        alpha = 1 - np.exp(-2 * np.pi * 1800 / rate)
        for i, value in enumerate(a):
            low += alpha * (value - low)
            a[i] = low
    # Remove pre-roll so contact feedback is immediate.
    if not loop:
        onsets = np.where(np.abs(a) > np.max(np.abs(a)) * .08)[0]
        if len(onsets): a = a[max(0, onsets[0] - int(.003 * rate)):]
    if loop:
        n = int(.065 * rate)
        f = np.linspace(0, 1, n)
        a[:n] = a[-n:] * (1 - f) + a[:n] * f
        a = a[:-n]
    else:
        attack, release = int(.002 * rate), int(.065 * rate)
        a[:attack] *= np.linspace(0, 1, attack)
        a[-release:] *= np.linspace(1, 0, release)
    a *= peak / max(np.max(np.abs(a)), 1e-9)
    assert np.isfinite(a).all() and np.max(np.abs(a)) < 1
    with wave.open(str(out / (name + '.wav')), 'wb') as w:
        w.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        w.writeframes((a * 32767).astype('<i2').tobytes())
    print(name, round(len(a) / rate, 3), 'seconds; peak', round(np.max(np.abs(a)), 3))


cut('Fridge_Open_01', 2.48, 3.24, .72)
cut('Fridge_Open_02', 10.98, 11.78, .72)
cut('Fridge_Close_01', 7.18, 7.95, .64, soften=True)
cut('Fridge_Close_02', 14.95, 15.73, .64, soften=True)
cut('Fridge_Slam_01', 21.55, 22.5, .94)
cut('Fridge_Slam_02', 40.98, 41.85, .94)
cut('Fridge_Movement', 33.24, 34.10, .48, soften=True, loop=True)
