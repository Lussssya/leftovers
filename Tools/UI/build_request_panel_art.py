from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Assets/Resources/RequestPanel'
OUTPUT.mkdir(parents=True, exist_ok=True)
SCALE = 2
image = Image.new('RGBA', (1320 * SCALE, 400 * SCALE))
draw = ImageDraw.Draw(image)


def polygon(points, color):
    draw.polygon([(int(x * SCALE), int(y * SCALE)) for x, y in points], fill=color)


def frame(left, top, right, bottom, corner, color):
    polygon([(left + corner, top), (right - corner, top), (right, top + corner),
             (right, bottom - corner), (right - corner, bottom),
             (left + corner, bottom), (left, bottom - corner), (left, top + corner)], color)


frame(8, 31, 1312, 394, 59, (15, 8, 5, 145))
image = image.filter(ImageFilter.GaussianBlur(5 * SCALE))
draw = ImageDraw.Draw(image)
frame(7, 24, 1313, 386, 59, '#251711')
frame(14, 30, 1306, 379, 55, '#68564b')
frame(23, 38, 1297, 370, 53, '#807774')
frame(26, 46, 1294, 370, 50, '#5d5555')
frame(36, 53, 1284, 357, 46, '#303037')
frame(41, 59, 1279, 353, 43, '#343339')
polygon([(41, 102), (84, 59), (1236, 59), (1279, 102), (1279, 110),
         (1231, 65), (88, 65), (41, 110)], '#3b393e')
draw.rounded_rectangle((17*SCALE, 83*SCALE, 38*SCALE, 329*SCALE), radius=10*SCALE, fill='#9a663a')
draw.rounded_rectangle((16*SCALE, 78*SCALE, 34*SCALE, 324*SCALE), radius=9*SCALE, fill='#efb85e')
draw.rounded_rectangle((16*SCALE, 78*SCALE, 23*SCALE, 321*SCALE), radius=3*SCALE, fill='#ffdb93')
polygon([(65, 115), (65, 27), (80, 15), (504, 15), (513, 22), (493, 65), (513, 105), (509, 116)], '#3b2216')
polygon([(73, 106), (73, 30), (85, 23), (500, 23), (480, 65), (500, 106)], '#cf8e3f')
polygon([(76, 101), (76, 31), (87, 26), (496, 26), (476, 65), (495, 101)], '#efb65d')
polygon([(77, 31), (88, 26), (496, 26), (491, 34), (88, 35), (77, 39)], '#ffdb8a')
for start, end in [((1120, 125), (1110, 91)), ((1159, 124), (1176, 96)),
                   ((1092, 137), (1082, 129))]:
    draw.line([(x * SCALE, y * SCALE) for x, y in [start, end]],
              fill='#edc99a', width=12 * SCALE)
    for x, y in [start, end]:
        draw.ellipse(((x - 6) * SCALE, (y - 6) * SCALE,
                      (x + 6) * SCALE, (y + 6) * SCALE), fill='#edc99a')
image.resize((1320, 400), Image.Resampling.LANCZOS).save(OUTPUT / 'Frame.png')
print(OUTPUT / 'Frame.png')
