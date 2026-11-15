from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Assets/Resources/NoiseBar'
OUTPUT.mkdir(parents=True, exist_ok=True)
SCALE = 3


def polygon(points, color):
    draw.polygon([(int(x * SCALE), int(y * SCALE)) for x, y in points], fill=color)


def frame(left, top, right, bottom, corner, color):
    polygon([(left + corner, top), (right - corner, top), (right, top + corner),
             (right, bottom - corner), (right - corner, bottom),
             (left + corner, bottom), (left, bottom - corner), (left, top + corner)], color)


panel = Image.new('RGBA', (800 * SCALE, 184 * SCALE))
draw = ImageDraw.Draw(panel)
frame(9, 11, 791, 179, 38, (13, 8, 6, 150))
panel = panel.filter(ImageFilter.GaussianBlur(4 * SCALE))
draw = ImageDraw.Draw(panel)
frame(4, 3, 796, 175, 38, '#211510')
frame(10, 8, 790, 169, 36, '#52463f')
frame(17, 14, 783, 162, 34, '#8b8078')
frame(19, 19, 781, 162, 32, '#68605d')
frame(26, 23, 774, 154, 29, '#303037')
frame(29, 27, 771, 152, 27, '#343339')
polygon([(29, 54), (56, 27), (744, 27), (771, 54), (771, 63),
         (739, 33), (60, 33), (29, 63)], '#3a383d')
polygon([(29, 124), (56, 152), (744, 152), (771, 124), (771, 130),
         (744, 157), (56, 157), (29, 130)], '#5a514d')

speaker = [(49, 77), (61, 77), (77, 62), (81, 65), (81, 119),
           (77, 122), (61, 106), (49, 106), (46, 102), (46, 81)]
polygon([(x, y + 3) for x, y in speaker], '#20140e')
polygon(speaker, '#23170f')
polygon([(51, 81), (63, 81), (76, 69), (76, 113), (63, 102), (51, 102)], '#c2bf9d')
polygon([(51, 81), (63, 81), (76, 69), (76, 77), (64, 87), (51, 87)], '#e4d5b4')
for radius in (17, 28):
    bounds = [(76 - radius) * SCALE, (92 - radius) * SCALE,
              (76 + radius) * SCALE, (92 + radius) * SCALE]
    draw.arc(bounds, -53, 53, fill='#211610', width=9 * SCALE)
    draw.arc(bounds, -53, 53, fill='#c8bb98', width=3 * SCALE)
panel.resize((800, 184), Image.Resampling.LANCZOS).save(OUTPUT / 'Panel.png')

thumb = Image.new('RGBA', (64 * SCALE, 68 * SCALE))
draw = ImageDraw.Draw(thumb)
draw.rounded_rectangle((5 * SCALE, 6 * SCALE, 59 * SCALE, 66 * SCALE),
                       radius=24 * SCALE, fill=(12, 7, 5, 160))
thumb = thumb.filter(ImageFilter.GaussianBlur(2 * SCALE))
draw = ImageDraw.Draw(thumb)
for rectangle, radius, color in [((4, 2, 60, 62), 24, '#2a1a12'),
                                  ((9, 6, 56, 59), 21, '#b58d6c'),
                                  ((10, 7, 54, 54), 20, '#f5ddbc'),
                                  ((14, 13, 51, 55), 17, '#e9c6a1'),
                                  ((15, 12, 50, 49), 16, '#f0cfab')]:
    draw.rounded_rectangle(tuple(value * SCALE for value in rectangle),
                           radius=radius * SCALE, fill=color)
thumb.resize((64, 68), Image.Resampling.LANCZOS).save(OUTPUT / 'Thumb.png')
print(OUTPUT)
