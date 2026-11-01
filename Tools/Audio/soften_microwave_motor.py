from pathlib import Path
import wave
import numpy as np
root=Path('Assets/Audio/Microwave')
def save(name,x):
    with wave.open(str(root/name),'wb') as f:
        f.setparams((1,2,48000,0,'NONE','not compressed'))
        f.writeframes((np.clip(x,-.95,.95)*32767).astype('<i2').tobytes())
for name in ['Microwave_Beep.wav','Microwave_Start.wav']:
    with wave.open(str(root/name),'rb') as f:
        x=np.frombuffer(f.readframes(f.getnframes()),'<i2').astype(float)/32768
    spectrum=np.fft.rfft(x)
    freq=np.fft.rfftfreq(len(x),1/48000)
    spectrum*=1/np.sqrt(1+(freq/2400)**8)
    x=np.fft.irfft(spectrum,n=len(x))*.62
    edge=min(480,len(x)//5)
    x[:edge]*=np.linspace(0,1,edge)
    x[-edge:]*=np.linspace(1,0,edge)
    save(name,x)
n=48000*4
t=np.arange(n)/48000
rng=np.random.default_rng(29)
fan=rng.normal(0,1,n)
freq=np.fft.rfftfreq(n,1/48000)
fan=np.fft.irfft(np.fft.rfft(fan)*np.exp(-(freq/650)**2)*(1-np.exp(-(freq/130)**2)),n=n)
x=.075*np.sin(2*np.pi*100*t)+.034*np.sin(2*np.pi*150*t)+.022*np.sin(2*np.pi*200*t)+fan*.13
x*=1+.035*np.sin(2*np.pi*2*t)
save('Microwave_Cooking.wav',x)
print('Softened provided beeps; rebuilt quiet motor/fan with no harsh upper buzz.')
