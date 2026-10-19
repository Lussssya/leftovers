from pathlib import Path
import subprocess, wave
import numpy as np
root = Path(__file__).resolve().parents[2]
out = root / 'Assets/Audio/Microwave'
ffmpeg = Path('C:/Users/Mompas/Downloads/ffmpeg-7.1.1-essentials_build/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe')
source = Path.home() / 'Downloads'
rate = 48000
rng = np.random.default_rng(19)
def read(name, start, duration):
    data = subprocess.check_output([str(ffmpeg), '-v', 'error', '-ss', str(start), '-t', str(duration), '-i', str(source/name), '-f', 'f32le', '-ar', str(rate), '-ac', '1', '-'])
    x = np.frombuffer(data, np.float32).copy()
    x -= x.mean()
    return x / max(abs(x).max(), .001)
def write(name, x, peak):
    x = np.tanh(x * 1.2)
    x -= x.mean()
    fade = min(240, len(x)//10)
    x[:fade] *= np.linspace(0,1,fade)
    x[-960:] *= np.linspace(1,0,960)
    x *= peak / max(abs(x).max(), .001)
    with wave.open(str(out/name), 'wb') as f:
        f.setparams((1,2,rate,0,'NONE','not compressed'))
        f.writeframes((x*32767).astype('<i2').tobytes())
    print(name, 'seconds', round(len(x)/rate,3), 'RMS', round(float(np.sqrt(np.mean(x*x))),3), 'peak',round(float(abs(x).max()),3))
def impact(name, recording, start, duration, length, body, metal):
    x = np.zeros(int(length*rate))
    base = read(recording,start,duration)
    x[:len(base)] += base*.65
    t = np.arange(len(x))/rate
    attack = 1-np.exp(-t*650)
    for frequency, weight in [(105,1),(163,.55),(237,.25)]:
        x += body*weight*np.sin(2*np.pi*frequency*t)*np.exp(-t*23)*attack
    for frequency, weight in [(510,1),(890,.5),(1340,.22)]:
        x += metal*weight*np.sin(2*np.pi*frequency*t)*np.exp(-t*32)*attack
    noise = rng.normal(0,1,len(x))
    noise = np.convolve(noise, np.ones(5)/5, mode='same')
    x += noise*.10*np.exp(-t*70)*attack
    delay = int(.027*rate)
    x[delay:] += x[:-delay].copy()*.10
    write(name,x,.92)
impact('Microwave_Open.wav','microwave open and close.mp3',.35,.51,.65,.14,.13)
impact('Microwave_Close.wav','microwave open and close.mp3',4.13,.53,.68,.34,.12)
impact('Microwave_Slam.wav','Microwave slam.mp3',.025,.515,.85,.66,.23)
n = rate*2
t = np.arange(n)/rate
noise = rng.normal(0,1,n)
noise = np.convolve(noise,np.ones(18)/18,mode='same')
x = noise*.6 + .014*np.sin(2*np.pi*180*t)*(1+.3*np.sin(2*np.pi*3*t))
x *= .8+.2*np.sin(2*np.pi*2*t)
write('Microwave_Movement.wav',x,.42)
