"""Tilesets v2: blocos de pedra chanfrados, paredes com tijolos grandes e
borda de pedras arredondadas no topo — clima de cripta/terror.

Mesmo layout de atlas da v1 (veja tiles.py), então o LevelBuilder não muda.
"""
import random
from PIL import Image
from pixtool import hexc, mix

THEMES = {
    # floor, floor2, grout, brick, brickD, brickL, top, bead, beadD, accent
    'masmorra': dict(floor='#3a2c2c', floor2='#43332f', grout='#1a1214', brick='#6a4436', brickD='#3e2620', brickL='#8e5e48',
                     top='#140c0e', bead='#b49a80', beadD='#6e5848', accent='#c8a050', blood='#5a0e14', moss='#3a4a2a'),
    'catacumbas': dict(floor='#2e3040', floor2='#363a4a', grout='#141420', brick='#545a70', brickD='#30344a', brickL='#727a92',
                       top='#0c0c16', bead='#a8a8b8', beadD='#5a5a6e', accent='#d8d0b8', blood='#4a0e18', moss='#2e3e3a'),
    'forja': dict(floor='#382220', floor2='#422824', grout='#180c0a', brick='#743828', brickD='#44201a', brickL='#9a4e36',
                  top='#120806', bead='#b07860', beadD='#6a3e2e', accent='#ff8a3a', blood='#6a1008', moss='#4a2a1a'),
    'cripta': dict(floor='#263a30', floor2='#2c4236', grout='#0e1a14', brick='#3e6450', brickD='#223a2e', brickL='#5a8a6c',
                   top='#08120c', bead='#9ab8a0', beadD='#4e6a58', accent='#7dffb0', blood='#3a0e14', moss='#3a6a3a'),
    'altar': dict(floor='#30243e', floor2='#382a48', grout='#140e1e', brick='#5a4280', brickD='#36264e', brickL='#7a60a8',
                  top='#0e0816', bead='#b8a0d0', beadD='#64507e', accent='#ffcc3a', blood='#4a0a20', moss='#3a2a4a'),
}


def C(h):
    return hexc(h) if isinstance(h, str) else h


def sh(h, amt):
    c = C(h)
    return mix(c, (255, 255, 255, 255), amt) if amt >= 0 else mix(c, (0, 0, 0, 255), -amt)


def build_atlas(name, seed=1):
    t = THEMES[name]
    rnd = random.Random(seed)
    img = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    P = img.load()

    def put(cx, cy, x, y, c):
        if 0 <= x < 16 and 0 <= y < 16 * (2 if cy in (1,) else 1) + (16 if cy == 1 else 0):
            pass
        X, Y = cx * 16 + x, cy * 16 + y
        if 0 <= X < 128 and 0 <= Y < 128:
            P[X, Y] = C(c)

    def bevel(cx, cy, x0, y0, w, h, base, depth=1):
        for y in range(h):
            for x in range(w):
                c = base
                if y < depth or x < depth:
                    c = sh(base, 0.14)
                if y >= h - depth or x >= w - depth:
                    c = sh(base, -0.28)
                if (y < depth and x >= w - depth) or (x < depth and y >= h - depth):
                    c = base
                put(cx, cy, x0 + x, y0 + y, c)

    # ---------------- chão (linha 0)
    for v in range(8):
        base = t['floor2'] if v in (1, 6) else t['floor']
        for y in range(16):
            for x in range(16):
                put(v, 0, x, y, t['grout'])
        if v == 4:  # quatro pedras pequenas
            for (x0, y0) in ((0, 0), (8, 0), (0, 8), (8, 8)):
                bevel(v, 0, x0, y0, 7, 7, sh(base, rnd.uniform(-0.06, 0.06)))
        else:
            bevel(v, 0, 0, 0, 15, 15, base)
        for _ in range(12):
            x, y = rnd.randint(1, 13), rnd.randint(1, 13)
            put(v, 0, x, y, sh(base, rnd.choice((-0.12, -0.08, 0.06))))
        if v == 5:  # rachada
            x, y = 3, 2
            for _ in range(12):
                put(v, 0, x, y, t['grout'])
                x += rnd.choice((0, 1, 1)); y += 1
                if y > 13:
                    break
        if v == 6:  # mancha de sangue velho
            cxp, cyp = rnd.randint(5, 10), rnd.randint(5, 10)
            for y in range(16):
                for x in range(16):
                    d = ((x - cxp) / 4.5) ** 2 + ((y - cyp) / 3.2) ** 2
                    if d < 1 and rnd.random() < 0.85:
                        put(v, 0, x, y, sh(t['blood'], -0.2 if d > 0.6 else 0))
        if v == 7:  # musgo / sujeira
            for _ in range(14):
                put(v, 0, rnd.randint(0, 14), rnd.randint(9, 14), t['moss'])

    # ---------------- face da parede (linhas 1-2, 16x32)
    def wall_face(v):
        cx, cy = v, 1
        for y in range(32):
            for x in range(16):
                P[cx * 16 + x, cy * 16 + y] = C(t['brickD'])
        # beiral no topo
        for x in range(16):
            P[cx * 16 + x, cy * 16 + 0] = C(sh(t['brickL'], 0.2))
            P[cx * 16 + x, cy * 16 + 1] = C(t['brickL'])
            P[cx * 16 + x, cy * 16 + 2] = C(sh(t['brickD'], -0.3))
        # fileiras de tijolos grandes
        rows = [(3, 7), (10, 7), (17, 7), (24, 6)]
        for r, (y0, h) in enumerate(rows):
            x = -4 if r % 2 else 0
            while x < 16:
                w = rnd.choice((7, 8, 9))
                base = sh(t['brick'], rnd.uniform(-0.08, 0.06))
                for yy in range(h - 1):
                    for xx in range(w - 1):
                        X = x + xx
                        if 0 <= X < 16:
                            c = base
                            if yy == 0 or xx == 0:
                                c = sh(base, 0.16)
                            elif yy == h - 2 or xx == w - 2:
                                c = sh(base, -0.22)
                            P[cx * 16 + X, cy * 16 + y0 + yy] = C(c)
                # lascas
                if rnd.random() < 0.4:
                    X = x + rnd.randint(1, w - 3)
                    if 0 <= X < 16:
                        P[cx * 16 + X, cy * 16 + y0 + rnd.randint(1, h - 3)] = C(sh(base, -0.3))
                x += w
        # sombra na base
        for x in range(16):
            P[cx * 16 + x, cy * 16 + 30] = C(sh(t['brickD'], -0.45))
            P[cx * 16 + x, cy * 16 + 31] = C(sh(t['brickD'], -0.6))
        return cx, cy

    for v in range(8):
        cx, cy = wall_face(v)
        if v == 4:  # rachadura + escorrido de sangue
            x = rnd.randint(4, 10)
            for y in range(4, 26):
                P[cx * 16 + x, cy * 16 + y] = C(sh(t['brickD'], -0.45))
                if rnd.random() < 0.3:
                    x = max(1, min(14, x + rnd.choice((-1, 1))))
            bx = rnd.randint(3, 12)
            for y in range(3, 3 + rnd.randint(8, 16)):
                P[cx * 16 + bx, cy * 16 + y] = C(t['blood'])
        if v == 5:  # nicho com vela acesa
            for y in range(11, 23):
                for x in range(4, 12):
                    P[cx * 16 + x, cy * 16 + y] = C('#0c0608')
            for y in range(17, 22):
                P[cx * 16 + 7, cy * 16 + y] = C('#e2c896'); P[cx * 16 + 8, cy * 16 + y] = C('#b8986a')
            P[cx * 16 + 7, cy * 16 + 16] = C('#ffe08a'); P[cx * 16 + 7, cy * 16 + 15] = C('#ffb347'); P[cx * 16 + 8, cy * 16 + 14] = C('#ff7a2f')
            for y in range(11, 23):
                P[cx * 16 + 4, cy * 16 + y] = C(t['brickL']); P[cx * 16 + 11, cy * 16 + y] = C(sh(t['brickD'], -0.3))
        if v == 6:  # grade de ferro
            for y in range(10, 27):
                for x in range(3, 13):
                    P[cx * 16 + x, cy * 16 + y] = C('#060406') if x % 3 else C('#4a4654')
            for x in range(3, 13):
                P[cx * 16 + x, cy * 16 + 10] = C('#6a6678'); P[cx * 16 + x, cy * 16 + 18] = C('#3a3644')
        if v == 7:  # nicho com caveira
            for y in range(12, 24):
                for x in range(3, 13):
                    P[cx * 16 + x, cy * 16 + y] = C('#0c0608')
            sk = ['.####.', '######', '#..#..', '######', '.#.#.#']
            for yy, row in enumerate(sk):
                for xx, ch in enumerate(row):
                    if ch == '#':
                        P[cx * 16 + 5 + xx, cy * 16 + 16 + yy] = C('#d8d0b8')
                    elif ch == '.' and yy == 2:
                        P[cx * 16 + 5 + xx, cy * 16 + 16 + yy] = C('#1a1012')

    # ---------------- topo da parede por máscara (linhas 3-4) com pedras arredondadas
    def bead(cx, cy, x, y, r=2):
        for yy in range(-r, r + 1):
            for xx in range(-r, r + 1):
                d = (xx + 0.5) ** 2 + (yy + 0.5) ** 2
                if d <= (r + 0.3) ** 2:
                    c = t['bead']
                    if yy + xx > 0:
                        c = sh(t['bead'], -0.25)
                    if yy < 0 and xx < 0:
                        c = sh(t['bead'], 0.18)
                    put(cx, cy, x + xx, y + yy, c)
                elif d <= (r + 1.1) ** 2:
                    X, Y = cx * 16 + x + xx, cy * 16 + y + yy
                    if 0 <= x + xx < 16 and 0 <= y + yy < 16 and P[X, Y] != C(t['bead']):
                        put(cx, cy, x + xx, y + yy, t['beadD'])

    for mask in range(16):
        cx, cy = mask % 8, 3 + mask // 8
        for y in range(16):
            for x in range(16):
                put(cx, cy, x, y, t['top'])
        for _ in range(5):
            put(cx, cy, rnd.randint(1, 14), rnd.randint(1, 14), sh(t['top'], 0.08))
        # pedras na borda voltada para o chão
        if mask & 4:   # sul (parte de baixo da imagem)
            for x in range(2, 16, 4):
                bead(cx, cy, x, 13)
        if mask & 1:   # norte
            for x in range(2, 16, 4):
                bead(cx, cy, x, 2)
        if mask & 2:   # leste
            for y in range(2, 16, 4):
                bead(cx, cy, 13, y)
        if mask & 8:   # oeste
            for y in range(2, 16, 4):
                bead(cx, cy, 2, y)

    # ---------------- especiais (linha 5)
    for y in range(16):
        for x in range(16):
            put(0, 5, x, y, '#030203')
            # borda do poço: parede descendo
            c = sh(t['brickD'], -0.15 - y * 0.06) if y < 11 else C('#030203')
            if y < 11 and (x % 5 == 0 or y in (3, 7)):
                c = sh(c, -0.3)
            put(1, 5, x, y, c)
            # tapete
            put(2, 5, x, y, '#4a121c' if 1 < y < 14 else '#a07830')
            put(3, 5, x, y, '#4a121c' if (x + y) % 8 else '#6a1a26')
            # sombra pontilhada (topo escuro)
            dens = 1 - y / 10
            if dens > 0 and ((x + y) % 2 == 0 or dens > 0.55):
                put(4, 5, x, y, (0, 0, 0, 255))
            if x < 4 and ((x + y) % 2 == 0 or x < 1):
                put(5, 5, x, y, (0, 0, 0, 255))
            put(6, 5, x, y, '#ff5a1a' if rnd.random() > 0.15 else '#ffb347')
            put(7, 5, x, y, sh(t['brickD'], -0.3) if y < 5 else C('#ff5a1a'))

    # ---------------- topo de pilar (linha 7, coluna 0): bloco de pedra claro e chanfrado
    for y in range(16):
        for x in range(16):
            put(0, 7, x, y, t['beadD'])
    bevel(0, 7, 1, 1, 14, 14, sh(t['brick'], 0.05), depth=2)
    for _ in range(6):
        put(0, 7, rnd.randint(3, 12), rnd.randint(3, 12), sh(t['brick'], rnd.choice((-0.12, 0.1))))
    for i in range(2, 14, 4):
        bead(0, 7, i, 1, 1); bead(0, 7, i, 14, 1)

    # ---------------- decalques (linha 6)
    dc = sh(t['floor'], -0.4)
    for v in range(8):
        cx, cy = v, 6
        if v == 0:  # rachadura longa
            x, y = 1, 8
            for _ in range(15):
                put(cx, cy, x, y, dc); x += 1; y = max(2, min(13, y + rnd.choice((-1, 0, 1))))
        elif v == 1:  # ossos
            for x in range(4, 12):
                put(cx, cy, x, 8, '#d8d0b8'); put(cx, cy, x, 9, '#a09880')
            for (x, y) in ((3, 7), (3, 10), (12, 7), (12, 10), (2, 8), (13, 9)):
                put(cx, cy, x, y, '#e8e0c8')
        elif v == 2:  # respingo de sangue
            for _ in range(22):
                a, r = rnd.random() * 6.28, rnd.random() ** 1.6 * 6
                import math
                put(cx, cy, int(8 + math.cos(a) * r * 1.3), int(8 + math.sin(a) * r * 0.8), t['blood'])
            put(cx, cy, 8, 8, sh(t['blood'], 0.2))
        elif v == 3:  # musgo
            for _ in range(16):
                put(cx, cy, rnd.randint(3, 12), rnd.randint(5, 12), t['moss'])
        elif v == 4:  # entulho
            for (x, y) in ((4, 10), (5, 10), (4, 11), (9, 6), (10, 6), (10, 7), (12, 11), (7, 12)):
                put(cx, cy, x, y, sh(t['brick'], -0.05)); put(cx, cy, x, y + 1, sh(t['brickD'], -0.3))
        elif v == 5:  # caveira
            sk = ['.###.', '#####', '#.#.#', '#####', '.#.#.']
            for yy, row in enumerate(sk):
                for xx, ch in enumerate(row):
                    if ch == '#':
                        put(cx, cy, 5 + xx, 6 + yy, '#d8d0b8')
                    elif yy == 2:
                        put(cx, cy, 5 + xx, 6 + yy, '#140c10')
        elif v == 6:  # círculo ritual (pedaço)
            import math
            for a in range(0, 360, 12):
                r = math.radians(a)
                put(cx, cy, int(8 + math.cos(r) * 6), int(8 + math.sin(r) * 6), sh(t['blood'], 0.15))
            for i in range(-3, 4):
                put(cx, cy, 8 + i, 8, sh(t['blood'], 0.15)); put(cx, cy, 8, 8 + i, sh(t['blood'], 0.15))
        elif v == 7:  # cápsulas velhas
            for (x, y) in ((4, 4), (11, 6), (6, 12), (12, 12)):
                put(cx, cy, x, y, '#b07a28'); put(cx, cy, x + 1, y, '#7a4e18')
    return img
