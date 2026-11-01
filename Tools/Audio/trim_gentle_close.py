import wave
import numpy as np
from pathlib import Path
path=Path('Assets/Audio/Microwave/Microwave_Close.wav')
with wave.open(str(path),'rb') as f:
    rate=f.getframerate()
    x=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768
x=x[:int(rate*.16)]
low=0.
y=np.zeros(len(x))
a=1-np.exp(-2*np.pi*420/rate)
for i,v in enumerate(x):
    low+=a*(v-low)
    y[i]=v-low
n=int(.045*rate)
y[-n:]*=np.linspace(1,0,n)**2
y[:96]*=np.linspace(0,1,96)
y*=min(1,.55/max(abs(y)))
with wave.open(str(path),'wb') as f:
    f.setparams((1,2,rate,0,'NONE','not compressed'))
    f.writeframes((y*32767).astype('<i2').tobytes())
print('Close: 160 ms latch, resonant low body removed, 45 ms fade, peak',max(abs(y)))
