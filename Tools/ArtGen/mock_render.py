"""Prévia estática da visão 2.5D (mesma projeção do jogo) para conferir a arte.

Reproduz a câmera "cabinet" do Unity: 1 tile de chão = 16x16 px e a face frontal
das paredes aparece com a altura real (parede alta = 32 px). Gera Docs/preview.png.
"""
import os
import random
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Resources', 'Art')


def load(p):
    return Image.open(os.path.join(ART, p)).convert('RGBA')


ROOM = [
    "#################",
    "#...............#",
    "#...............#",
    "#..##.......##..#",
    "#..##.......##..#",
    "#......c.c......#",
    "#...............#",
    "#.......o.......#",
    "#..##..ooo..##..#",
    "#..##.......##..#",
    "#...............#",
    "#...............#",
    "#################",
]


def render(theme='masmorra', out='preview.png', scale=3, seed=3):
    rnd = random.Random(seed)
    atlas = load(f'Tiles/{theme}.png')
    cell = lambda c, r, rows=1: atlas.crop((c * 16, r * 16, c * 16 + 16, r * 16 + 16 * rows))
    H, W = len(ROOM), len(ROOM[0])
    TOP = 40
    img = Image.new('RGBA', (W * 16, H * 16 + TOP), (18, 10, 12, 255))

    def solid(x, y):
        return not (0 <= x < W and 0 <= y < H) or ROOM[y][x] == '#'

    def height(x, y):
        if not solid(x, y) or not (0 <= x < W and 0 <= y < H):
            return 0
        if 0 < x < W - 1 and 0 < y < H - 1:
            return 1.5  # pilar
        if y > 0 and not solid(x, y - 1):
            return 0.75
        return 2.0

    # chão
    for y in range(H):
        for x in range(W):
            if solid(x, y):
                continue
            ch = ROOM[y][x]
            px, py = x * 16, y * 16 + TOP
            if ch == 'o':
                img.alpha_composite(cell(0 if y > 0 and ROOM[y - 1][x] == 'o' else 1, 5), (px, py))
                continue
            img.alpha_composite(cell(rnd.randint(0, 3) if rnd.random() < 0.82 else rnd.randint(4, 7), 0), (px, py))
            if solid(x, y - 1):
                img.alpha_composite(cell(4, 5), (px, py))
            if rnd.random() < 0.07:
                img.alpha_composite(cell(rnd.randint(0, 7), 6), (px, py))

    sprites = []
    sh = load('Sheets/shadow.png')

    def put_sprite(im, gx, gy, h=0, shadow=True):
        sprites.append((gy, im, gx, gy, h, shadow))

    pl = load('Sheets/player.png').crop((0, 0, 24, 26))
    put_sprite(pl, 8.5, 6.6)
    for name, gx, gy, fw, fh in (('gosma', 4.5, 2.5, 20, 18), ('caveira', 12.5, 3.2, 18, 26), ('cogumelo', 5.5, 10.5, 18, 22),
                                 ('morcego', 12, 10.4, 24, 16)):
        im = load(f'Sheets/enemy_{name}.png').crop((0, 0, fw, fh))
        put_sprite(im, gx, gy, 0.7 if name == 'morcego' else 0)
    gun = load('Weapons/escopeta.png')

    # paredes e sprites, de trás (norte) para frente (sul)
    for y in range(H):
        for x in range(W):
            if not solid(x, y):
                continue
            hgt = height(x, y)
            ph = int(hgt * 16)
            px, py = x * 16, y * 16 + TOP
            mask = 0
            if not solid(x, y - 1): mask |= 1
            if not solid(x + 1, y): mask |= 2
            if not solid(x, y + 1): mask |= 4
            if not solid(x - 1, y): mask |= 8
            # face sul
            if not solid(x, y + 1) or y == H - 1:
                face = cell(rnd.choice([0, 1, 2, 3, 3, 4, 5, 6]) if hgt == 2 else 0, 1, 2)
                face = face.crop((0, 0, 16, ph))
                img.alpha_composite(face, (px, py + 16 - ph))
            img.alpha_composite(cell(mask % 8, 3 + mask // 8), (px, py - ph))
            if ROOM[y][x] == '#' and 0 < x < W - 1 and 0 < y < H - 1:
                pass
        # sprites desta linha
        for s in sorted([s for s in sprites if int(s[0]) == y], key=lambda s: s[0]):
            _, im, gx, gy, h, shadow = s
            fx, fy = int(gx * 16), int(gy * 16) + TOP
            if shadow:
                img.alpha_composite(Image.blend(Image.new('RGBA', sh.size, (0, 0, 0, 0)), sh, 0.45), (fx - 8, fy - 4))
            img.alpha_composite(im, (fx - im.width // 2, fy - im.height - int(h * 16)))
            if im is pl:
                img.alpha_composite(gun, (fx + 1, fy - 14))
    # pilares (altura 1.5) desenhados por cima, linha a linha
    img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    os.makedirs(os.path.join(ROOT, 'Docs'), exist_ok=True)
    img.save(os.path.join(ROOT, 'Docs', out))
    return img


if __name__ == '__main__':
    render('masmorra', 'preview_masmorra.png')
    render('cripta', 'preview_cripta.png', seed=5)
    print('ok')
