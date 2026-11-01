from pathlib import Path
import wave,subprocess
import numpy as np
root=Path('Assets/Audio/Microwave'); rate=48000
ff='C:/Users/Mompas/Downloads/ffmpeg-7.1.1-essentials_build/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe'
def write(name,x,fade=.12):
    x=x.copy(); n=min(int(fade*rate),len(x)//2)
    x[-n:]*=.5+.5*np.cos(np.linspace(0,np.pi,n))
    x[:192]*=np.linspace(0,1,192)
    x=np.r_[x,np.zeros(480)]
    with wave.open(str(root/name),'wb') as f:
        f.setparams((1,2,rate,0,'NONE','not compressed'));f.writeframes((np.clip(x,-.95,.95)*32767).astype('<i2').tobytes())
raw=subprocess.check_output([ff,'-v','error','-ss','0.28','-t','1.35','-i',str(Path.home()/'Downloads/microwave open and close.mp3'),'-f','f32le','-ar',str(rate),'-ac','1','-'])
x=np.frombuffer(raw,np.float32).copy(); x-=np.mean(x);x*=.65/max(abs(x));write('Microwave_Open.wav',x,.35)
t=np.arange(int(.24*rate))/rate
rng=np.random.default_rng(73)
noise=rng.normal(0,1,len(t));noise-=np.convolve(noise,np.ones(20)/20,mode='same')
x=.12*noise*np.exp(-t*100)*(1-np.exp(-t*1800))
x+=.07*np.sin(2*np.pi*1750*t)*np.exp(-t*90)*(1-np.exp(-t*2000))
write('Microwave_Close.wav',x,.10)
for name in ['Microwave_Slam.wav','Microwave_Beep.wav','Microwave_Start.wav']:
    with wave.open(str(root/name),'rb') as f:x=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768
    write(name,x,.12 if name!='Microwave_Start.wav' else .025)
print('Opening re-extracted with full tail. Close rebuilt as clean latch without any slam recording. One-shot edges tapered.')

