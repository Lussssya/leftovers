from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import math
out=Path('Assets/Models/Microwave/Polished');out.mkdir(parents=True,exist_ok=True)
font='C:/Windows/Fonts/segoeui.ttf'
def f(n): return ImageFont.truetype(font,n)
key=Image.new('RGB',(384,1024),(40,47,51)); d=ImageDraw.Draw(key)
def label(x,y,t,size=18,color=(205,212,212)): d.text((x,y),t,font=f(size),fill=color,anchor='mm',align='center')
label(192,38,'LEFTOVERS',20,(152,166,172))
for r,row in enumerate([['POPCORN','POTATO','PIZZA'],['COOK','REHEAT','DEFROST']]):
 for c,t in enumerate(row):
  x=20+c*116;y=78+r*96;d.rounded_rectangle((x,y,x+109,y+85),radius=8,fill=(83,93,98));label(x+54,y+42,t,14)
for r,row in enumerate([['1','2','3'],['4','5','6'],['7','8','9'],['TIME','0','POWER']]):
 for c,t in enumerate(row):
  x=20+c*116;y=305+r*108;d.rounded_rectangle((x,y,x+109,y+98),radius=7,fill=(53,63,69));label(x+54,y+49,t,30 if len(t)==1 else 15)
d.rounded_rectangle((20,774,179,882),radius=8,fill=(78,94,96));d.rounded_rectangle((199,774,364,882),radius=8,fill=(63,71,76));label(100,827,'START',20);label(282,827,'STOP',20)
label(192,960,'700 W  /  QUICK HEAT',15,(129,145,153));key.save(out/'Keypad.png')
patterns=['1111110','0110000','1101101','1111001','0110011','1011011','1011111','1110000','1111111','1111011']
atlas=Image.new('RGB',(2560,1120),(12,17,20))
for n in range(100):
 im=Image.new('RGB',(256,112),(12,17,20)); dr=ImageDraw.Draw(im); glow=Image.new('RGBA',im.size);g=ImageDraw.Draw(glow)
 for j,ch in enumerate('00'+str(n).zfill(2)):
  x=[22,72,142,192][j]; y=20; w=31;h=66;t=5
  polys=[[(x+5,y),(x+w-5,y),(x+w,y+3),(x+w-5,y+6),(x+5,y+6),(x,y+3)],[(x+w,y+5),(x+w+3,y+10),(x+w+3,y+28),(x+w,y+33),(x+w-3,y+28),(x+w-3,y+10)],[(x+w,y+35),(x+w+3,y+40),(x+w+3,y+58),(x+w,y+63),(x+w-3,y+58),(x+w-3,y+40)]]
  polys += [[(a,b+66) for a,b in polys[0]],[(a-w,b+30) for a,b in polys[1]],[(a-w,b) for a,b in polys[1]],[(a,b+33) for a,b in polys[0]]]
  for k,p in enumerate(polys):
   dr.polygon(p,fill=(42,26,27))
   if patterns[int(ch)][k]=='1': g.polygon(p,fill=(255,86,70,255))
 for cy in [44,70]:g.rounded_rectangle((122,cy,127,cy+5),radius=1,fill=(255,86,70,255))
 im=Image.alpha_composite(im.convert('RGBA'),glow.filter(ImageFilter.GaussianBlur(3)));im=Image.alpha_composite(im,glow)
 atlas.paste(im.convert('RGB'),((n%10)*256,(n//10)*112))
atlas.save(out/'RedTimerAtlas.png')
tex=Image.new('RGBA',(256,224),(18,23,26,245));d=ImageDraw.Draw(tex)
for row in range(-1,18):
 for col in range(-1,18):
  x=col*32+(row%2)*16;y=row*28
  points=[(x+13.7*math.cos(math.pi/3*i),y+13.7*math.sin(math.pi/3*i)) for i in range(6)]
  d.polygon(points,fill=(21,28,31,140))
tex.save(out/'FineScreen.png')
print('Flat keypad, red timer atlas, fine perforated screen created')
