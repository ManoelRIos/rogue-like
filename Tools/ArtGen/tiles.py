"""Tilesets dos 5 andares (atlas 128x128 com células de 16px).

Layout do atlas:
  linha 0      : 8 variações de chão
  linhas 1-2   : 8 variações da face frontal da parede (16x32)
  linhas 3-4   : topo da parede por máscara de vizinhos (0-15). bit1=N bit2=L bit4=S bit8=O
  linha 5      : 0 poço, 1 borda do poço, 2 tapete, 3 tapete borda, 4 sombra (pontilhada), 5 sombra lateral, 6 lava, 7 borda lava
  linha 6      : decorações sobre o chão (recorte)
"""
import random
from PIL import Image
from pixtool import hexc, mix

THEMES = {
    'masmorra': dict(floor='#5a463a', floorAlt='#64503f', grout='#2e221c', wall='#8a5a40', wallD='#5a3828', wallL='#a8724e',
                     top='#3a2820', accent='#c9a46a', style='slab'),
    'catacumbas': dict(floor='#4a4e60', floorAlt='#545a6e', grout='#262838', wall='#6a7088', wallD='#40445a', wallL='#8a90a8',
                       top='#26283a', accent='#d8d0b8', style='bone'),
    'forja': dict(floor='#4e3430', floorAlt='#583a34', grout='#241210', wall='#8a4634', wallD='#5a2a1e', wallL='#b0604a',
                  top='#2e1410', accent='#ff8a3a', style='plate'),
    'cripta': dict(floor='#35503f', floorAlt='#3d5a46', grout='#16261c', wall='#467a5c', wallD='#2a4a38', wallL='#62a07a',
                   top='#152a1e', accent='#7dffb0', style='moss'),
    'altar': dict(floor='#433456', floorAlt='#4c3c62', grout='#201830', wall='#664e8e', wallD='#3e2e5e', wallL='#8a70b8',
                  top='#1e162c', accent='#ffcc3a', style='marble'),
}


def shade(hex_, amt):
    c = hexc(hex_)
    if amt >= 0:
        return mix(c, (255, 255, 255, 255), amt)
    return mix(c, (0, 0, 0, 255), -amt)


def build_atlas(name, seed=1):
    t = THEMES[name]
    rnd = random.Random(seed)
    img = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    p = img.load()

    def put(cx, cy, x, y, c):
        if 0 <= x < 16 and 0 <= y < 16:
            p[cx * 16 + x, cy * 16 + y] = c if isinstance(c, tuple) else hexc(c)

    def fill(cx, cy, c, h=16):
        for y in range(h):
            for x in range(16):
                put(cx, cy, x, y, c)

    # ---------------- chão
    for v in range(8):
        base = t['floorAlt'] if v in (2, 5) else t['floor']
        fill(v, 0, base)
        for i in range(14):
            put(v, 0, rnd.randint(1, 14), rnd.randint(1, 14), shade(base, rnd.uniform(-0.1, 0.08)))
        st = t['style']
        if st in ('slab', 'moss', 'marble'):
            # duas lajes por tile, juntas deslocadas
            for x in range(16):
                put(v, 0, x, 15, t['grout']); put(v, 0, x, 7, t['grout'])
            jx = 9 if v % 2 else 5
            for y in range(0, 7):
                put(v, 0, 15, y, t['grout'])
            for y in range(8, 15):
                put(v, 0, jx, y, t['grout'])
            for x in range(15):
                put(v, 0, x, 0, shade(base, 0.08)); put(v, 0, x, 8, shade(base, 0.08))
        elif st == 'bone':
            for x in range(16):
                put(v, 0, x, 15, t['grout'])
            for y in range(16):
                put(v, 0, 15, y, t['grout'])
            for x in range(15):
                put(v, 0, x, 0, shade(base, 0.1))
            if v in (3, 6):
                for x in range(4, 11):
                    put(v, 0, x, 8, t['accent'])
                put(v, 0, 3, 7, t['accent']); put(v, 0, 3, 9, t['accent']); put(v, 0, 11, 7, t['accent']); put(v, 0, 11, 9, t['accent'])
        elif st == 'plate':
            for x in range(16):
                put(v, 0, x, 15, t['grout']); put(v, 0, x, 0, shade(base, 0.12))
            for y in range(16):
                put(v, 0, 15, y, t['grout']); put(v, 0, 0, y, shade(base, 0.06))
            for (x, y) in ((2, 2), (13, 2), (2, 13), (13, 13)):
                put(v, 0, x, y, shade(base, 0.25))
            if v in (4, 7):
                for _ in range(3):
                    put(v, 0, rnd.randint(3, 12), rnd.randint(3, 12), t['accent'])
        if st == 'moss' and v in (1, 4, 6):
            for _ in range(10):
                put(v, 0, rnd.randint(0, 15), rnd.randint(0, 15), '#4a8a3a')
        if st == 'marble' and v in (3, 6):
            for i in range(16):
                put(v, 0, i, i, shade(base, 0.12))
            put(v, 0, 7, 7, t['accent']); put(v, 0, 8, 8, t['accent'])
        if v == 7:  # rachadura
            x, y = 3, 3
            for _ in range(9):
                put(v, 0, x, y, shade(base, -0.35))
                x += rnd.choice((0, 1)); y += 1

    # ---------------- face da parede (16x32)
    for v in range(8):
        cx, cy = v, 1
        for y in range(32):
            for x in range(16):
                p[cx * 16 + x, cy * 16 + y] = hexc(t['wall'])
        row_h = 5
        for r in range(0, 6):
            y0 = 3 + r * row_h
            for x in range(16):
                if y0 < 32:
                    p[cx * 16 + x, cy * 16 + y0] = hexc(t['wallD'])
                if y0 + 1 < 32:
                    p[cx * 16 + x, cy * 16 + y0 + 1] = shade(t['wall'], 0.12)
            off = 4 if r % 2 else 11
            for y in range(y0, min(32, y0 + row_h)):
                p[cx * 16 + off, cy * 16 + y] = hexc(t['wallD'])
        for i in range(10):
            x, y = rnd.randint(0, 15), rnd.randint(4, 28)
            p[cx * 16 + x, cy * 16 + y] = shade(t['wall'], rnd.uniform(-0.12, 0.08))
        # topo claro e rodapé escuro
        for x in range(16):
            p[cx * 16 + x, cy * 16 + 0] = shade(t['wallL'], 0.15)
            p[cx * 16 + x, cy * 16 + 1] = hexc(t['wallL'])
            p[cx * 16 + x, cy * 16 + 2] = hexc(t['wallD'])
            p[cx * 16 + x, cy * 16 + 30] = shade(t['wallD'], -0.3)
            p[cx * 16 + x, cy * 16 + 31] = shade(t['wallD'], -0.5)
        if v == 4:  # rachada
            x = 6
            for y in range(6, 24):
                p[cx * 16 + x, cy * 16 + y] = shade(t['wallD'], -0.4)
                x += rnd.choice((-1, 0, 1)); x = max(1, min(14, x))
        if v == 5:  # estandarte
            for y in range(5, 22):
                for x in range(4, 12):
                    c = shade(t['accent'], -0.35) if x in (4, 11) else shade(t['accent'], -0.15)
                    p[cx * 16 + x, cy * 16 + y] = c
            for x in range(5, 11):
                p[cx * 16 + x, cy * 16 + 22] = (0, 0, 0, 0) if x % 2 else shade(t['accent'], -0.15)
            for y in range(10, 15):
                p[cx * 16 + 7, cy * 16 + y] = hexc('#fff6e8'); p[cx * 16 + 8, cy * 16 + y] = hexc('#fff6e8')
            p[cx * 16 + 6, cy * 16 + 11] = hexc('#fff6e8'); p[cx * 16 + 9, cy * 16 + 11] = hexc('#fff6e8')
        if v == 6:  # grade
            for y in range(12, 26):
                for x in range(3, 13):
                    p[cx * 16 + x, cy * 16 + y] = hexc('#0c080c') if (x % 3) else hexc('#4a4a58')
        if v == 7:  # nicho com caveira
            for y in range(12, 24):
                for x in range(4, 12):
                    p[cx * 16 + x, cy * 16 + y] = hexc('#140c10')
            for (x, y) in ((6, 18), (7, 17), (8, 17), (9, 18), (6, 19), (9, 19), (7, 20), (8, 20)):
                p[cx * 16 + x, cy * 16 + y] = hexc('#d8d0b8')

    # ---------------- topo da parede por máscara
    for mask in range(16):
        cx, cy = mask % 8, 3 + mask // 8
        fill(cx, cy, t['top'])
        for i in range(6):
            put(cx, cy, rnd.randint(2, 13), rnd.randint(2, 13), shade(t['top'], 0.1))
        rim, rimD = shade(t['wallL'], -0.1), shade(t['top'], -0.4)
        if mask & 1:  # aberto ao norte (z+) = parte de cima do tile
            for x in range(16):
                put(cx, cy, x, 0, rim); put(cx, cy, x, 1, shade(t['top'], 0.15))
        if mask & 4:  # aberto ao sul
            for x in range(16):
                put(cx, cy, x, 15, rim); put(cx, cy, x, 14, shade(t['top'], 0.2))
        if mask & 2:  # leste
            for y in range(16):
                put(cx, cy, 15, y, rim)
        if mask & 8:  # oeste
            for y in range(16):
                put(cx, cy, 0, y, rim)

    # ---------------- especiais
    fill(0, 5, '#060407')  # poço
    for y in range(16):  # borda do poço (parede descendo)
        for x in range(16):
            put(1, 5, x, y, shade(t['wallD'], -0.2 - y * 0.05) if y < 12 else hexc('#060407'))
    fill(2, 5, '#6b2a3a')
    for i in range(16):
        put(2, 5, i, 0, '#d9a441'); put(2, 5, i, 15, '#d9a441')
    fill(3, 5, '#6b2a3a')
    for i in range(0, 16, 4):
        put(3, 5, i, 8, '#d9a441')
    for y in range(16):  # sombra pontilhada
        for x in range(16):
            dens = 1 - y / 9
            if dens > 0 and ((x + y) % 2 == 0 or dens > 0.6):
                put(4, 5, x, y, (0, 0, 0, 255))
    for y in range(16):
        for x in range(4):
            if (x + y) % 2 == 0 or x < 1:
                put(5, 5, x, y, (0, 0, 0, 255))
    fill(6, 5, '#ff5a1a')
    for _ in range(12):
        put(6, 5, rnd.randint(0, 15), rnd.randint(0, 15), '#ffb347')
    for y in range(16):
        for x in range(16):
            put(7, 5, x, y, shade(t['wallD'], -0.3) if y < 5 else hexc('#ff5a1a'))

    # ---------------- decorações (recorte)
    dc = shade(t['floor'], -0.35)
    for v in range(8):
        cx, cy = v, 6
        if v == 0:  # rachaduras
            x, y = 2, 8
            for _ in range(12):
                put(cx, cy, x, y, dc); x += 1; y += rnd.choice((-1, 0, 1)); y = max(1, min(14, y))
        elif v == 1:  # ossos
            for x in range(4, 12):
                put(cx, cy, x, 8, '#d8d0b8')
            for (x, y) in ((3, 7), (3, 9), (12, 7), (12, 9)):
                put(cx, cy, x, y, '#d8d0b8')
        elif v == 2:  # poça
            for y in range(5, 11):
                for x in range(3, 13):
                    if ((x - 8) / 5) ** 2 + ((y - 8) / 3) ** 2 <= 1:
                        put(cx, cy, x, y, shade(t['floor'], -0.25))
            put(cx, cy, 6, 6, shade(t['floor'], 0.3))
        elif v == 3:  # tufo de grama/mofo
            for (x, y) in ((6, 10), (7, 9), (8, 10), (9, 8), (10, 10), (7, 11), (9, 11)):
                put(cx, cy, x, y, '#4a8a3a' if t['style'] != 'plate' else '#5a3a2a')
        elif v == 4:  # entulho
            for (x, y) in ((4, 10), (5, 10), (9, 6), (10, 6), (10, 7), (12, 11)):
                put(cx, cy, x, y, shade(t['wall'], -0.1))
        elif v == 5:  # caveira pequena
            for (x, y) in ((6, 6), (7, 6), (8, 6), (5, 7), (9, 7), (5, 8), (7, 8), (9, 8), (6, 9), (8, 9)):
                put(cx, cy, x, y, '#d8d0b8')
            put(cx, cy, 6, 7, '#140c10'); put(cx, cy, 8, 7, '#140c10')
        elif v == 6:  # runa
            for i in range(5):
                put(cx, cy, 5 + i, 5, t['accent']); put(cx, cy, 7, 5 + i, t['accent']); put(cx, cy, 5 + i, 10, t['accent'])
        elif v == 7:  # moedas antigas/pedrinhas
            for (x, y) in ((4, 4), (11, 6), (6, 12), (12, 12)):
                put(cx, cy, x, y, shade(t['floor'], 0.2))
    return img


def gate():
    """Grade das portas trancadas: 48x32."""
    img = Image.new('RGBA', (48, 32), (0, 0, 0, 0))
    p = img.load()
    for x in range(2, 48, 6):
        for y in range(32):
            p[x, y] = hexc('#3a3a48'); p[x + 1, y] = hexc('#6a6a80')
        p[x, 1] = hexc('#ff4d5e'); p[x + 1, 1] = hexc('#ff4d5e')
    for x in range(48):
        for y in (4, 5, 26, 27):
            p[x, y] = hexc('#24242e')
    return img
