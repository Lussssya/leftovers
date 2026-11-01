import subprocess,wave
from pathlib import Path
import numpy as np
ff='C:/Users/Mompas/Downloads/ffmpeg-7.1.1-essentials_build/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe'
raw=subprocess.check_output([ff,'-v','error','-ss','4.13','-t','0.105','-i',str(Path.home()/'Downloads/microwave open and close.mp3'),'-f','f32le','-ar','48000','-ac','1','-'])
x=np.frombuffer(raw,np.float32).astype(float);x-=x.mean()
f=np.fft.rfftfreq(len(x),1/48000)
response=(1-np.exp(-(f/190)**2))*np.exp(-(f/4200)**4)
x=np.fft.irfft(np.fft.rfft(x)*response,n=len(x))
x*=.38/max(abs(x));x[:144]*=np.linspace(0,1,144)
n=int(.075*48000);x[-n:]*=(.5+.5*np.cos(np.linspace(0,np.pi,n)))
x=np.r_[x,np.zeros(4800)]
with wave.open('Assets/Audio/Microwave/Microwave_Close.wav','wb') as w:
 w.setparams((1,2,48000,0,'NONE','not compressed'));w.writeframes((x*32767).astype('<i2').tobytes())
print('Real recorded mechanical close transient; no synthesized oscillator; late impact excluded; tapered to silence.')
