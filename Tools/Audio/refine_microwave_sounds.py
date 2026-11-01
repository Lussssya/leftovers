from pathlib import Path
import wave
import numpy as np
root=Path.cwd()/ 'Assets/Audio/Microwave'
def read(name):
    with wave.open(str(root/name),'rb') as f:
        return np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768

def write(name,x):
    with wave.open(str(root/name),'wb') as f:
        f.setparams((1,2,48000,0,'NONE','not compressed'))
        f.writeframes((np.clip(x,-.98,.98)*32767).astype('<i2').tobytes())
x=read('Microwave_Movement.wav')
x=np.convolve(x,np.ones(90)/90,mode='same')*.6
write('Microwave_Movement.wav',x)
x=read('Microwave_Slam.wav')
t=np.arange(len(x))/48000
for delay,level in [(.048,.15),(.082,.07)]:
    local=np.maximum(0,t-delay)
    ring=(np.sin(2*np.pi*1120*local)+.45*np.sin(2*np.pi*1730*local))*np.exp(-local*65)*(1-np.exp(-local*2200))
    x+=ring*level
x*=.96/max(abs(x))
write('Microwave_Slam.wav',x)
print('Subtle filtered hinge and two short tray/latch rattles saved.')
