from pathlib import Path
import subprocess, wave
import numpy as np

root = Path(__file__).resolve().parents[2]
ffmpeg = 'C:/Users/Mompas/Downloads/ffmpeg-7.1.1-essentials_build/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe'
source = Path.home() / 'Downloads/49603404-microwave-door-open-amp-closing-326196.mp3'
rate = 48000
raw = subprocess.check_output([ffmpeg, '-v', 'error', '-ss', '2.64', '-t', '0.78', '-i', str(source), '-f', 'f32le', '-ar', str(rate), '-ac', '1', '-'])
sound = np.frombuffer(raw, np.float32).astype(float)
sound -= sound.mean()
frequencies = np.fft.rfftfreq(len(sound), 1 / rate)
filter = (1 - np.exp(-(frequencies / 65)**2)) * np.exp(-(frequencies / 6500)**4)
sound = np.fft.irfft(np.fft.rfft(sound) * filter, n=len(sound))
window = np.ones(480) / 480
level = np.sqrt(np.convolve(sound * sound, window, mode='same'))
sound *= np.minimum(1, .055 / np.maximum(level, .0001))
sound *= .30 / max(np.max(np.abs(sound)), .001)
sound[:192] *= np.linspace(0, 1, 192)
fade = int(.20 * rate)
sound[-fade:] *= .5 + .5 * np.cos(np.linspace(0, np.pi, fade))
sound = np.r_[sound, np.zeros(2400)]
with wave.open(str(root / 'Assets/Audio/Microwave/Microwave_Close.wav'), 'wb') as output:
    output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
    output.writeframes((sound * 32767).astype('<i2').tobytes())
print('New recording, closing event only, duration', len(sound) / rate, 'peak', round(float(max(abs(sound))), 3), 'RMS', round(float(np.sqrt(np.mean(sound * sound))), 3))
