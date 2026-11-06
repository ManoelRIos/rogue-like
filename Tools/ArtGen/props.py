"""Adereços de terror, balas novas, cápsulas, tocha de parede e armas v2."""
import math
from PIL import Image
from vol import VMat, Part, ramp5
from widow import flame

P = {
    'iron': Part(ramp5('#121218', '#22222c', '#363644', '#50505e', '#74748a'), 1.2, dither=False, gloss=0.2),
    'brass': Part(ramp5('#3a220a', '#6a4416', '#a06a24', '#d4a040', '#fff0a0'), 1.2, dither=False, gloss=0.25),
    'wax': Part(ramp5('#7a6040', '#b8986a', '#e2c896', '#f6e6c0', '#fffaf0'), 1.2, dither=False),
    'wood': Part(ramp5('#160c08', '#2a1810', '#422618', '#5c3820', '#764c2c'), 2.0, dither=False),
    'woodL': Part(ramp5('#2a1810', '#422618', '#5c3820', '#764c2c', '#946038'), 1.5, dither=False),
    'bone': Part(ramp5('#5a5040', '#8a8068', '#b8ae90', '#dcd4bc', '#f8f2e0'), 1.5, dither=False),
    'stone': Part(ramp5('#1e1c24', '#322e3a', '#4a4554', '#665f72', '#857d92'), 3.0, dither=True),
    'cloth': Part(ramp5('#24060c', '#3e0a14', '#5c121c', '#7a1e24', '#9a3030'), 2.0, dither=False),
    'gold': Part(ramp5('#4a2c0c', '#7a4e18', '#b07a28', '#e0b048', '#fff0a0'), 1.0, dither=False),
    'web': Part(ramp5('#8a8a96', '#a8a8b4', '#c8c8d4', '#e0e0ea', '#ffffff'), 0.5, dither=False, flat=True),
    'skin': Part(ramp5('#6e3e3a', '#a8645a', '#d49478', '#ecbc98', '#fbdcc0'), 1.6, dither=False),
    'glove': Part(ramp5('#160c08', '#2a1810', '#422618', '#5c3820', '#764c2c'), 1.6, dither=False),
}


def candelabra(f):
    m = VMat(16, 34)
    m.ellipse(8, 32, 5, 1.6, 'iron')
    m.rect(7, 14, 2, 18, 'iron')
    m.ellipse(8, 22, 2, 1.2, 'iron')
    m.rect(2, 13, 12, 2, 'iron')
    m.rect(2, 10, 1, 3, 'iron'); m.rect(13, 10, 1, 3, 'iron')
    for i, x in enumerate((2, 7, 13)):
        h = 5 if x != 7 else 7
        m.rect(x - (0 if x != 7 else 0), 13 - h - (0 if x != 7 else 2), 2 if x == 7 else 1, h, 'wax')
        flame(m, x, 13 - h - (1 if x != 7 else 3), 4, f + i)
    return m.render(P)


def candles(f):
    m = VMat(16, 14)
    m.ellipse(8, 12, 7, 1.8, 'wax')
    for i, (x, h) in enumerate(((3, 4), (6, 7), (9, 5), (12, 3))):
        m.rect(x, 12 - h, 2, h, 'wax')
        flame(m, x, 11 - h, 3, f + i)
    return m.render(P)


def coffin():
    m = VMat(18, 32)
    m.poly([(5, 1), (13, 1), (16, 8), (13, 31), (5, 31), (2, 8)], 'wood')
    m.poly([(6, 3), (12, 3), (14, 8), (12, 29), (6, 29), (4, 8)], 'woodL')
    m.rect(8, 8, 2, 12, 'iron'); m.rect(5, 12, 8, 2, 'iron')
    for (x, y) in ((4, 6), (14, 6), (5, 28), (13, 28)):
        m.px(x, y, '#8a8aa0')
    return m.render(P)


def statue():
    m = VMat(22, 40)
    m.rect(3, 33, 16, 6, 'stone')
    m.rect(2, 32, 18, 2, 'stone')
    m.poly([(7, 12), (15, 12), (17, 32), (5, 32)], 'stone')
    m.ellipse(11, 8, 4.5, 5, 'stone')
    m.poly([(6, 6), (11, 0), (16, 6), (16, 13), (6, 13)], 'stone')     # capuz
    m.ellipse(11, 9, 2.6, 3, 'stone')
    m.px(10, 9, '#0a080c'); m.px(12, 9, '#0a080c')
    m.px(10, 10, '#5a0e14'); m.px(10, 11, '#5a0e14'); m.px(10, 12, '#3a0a0e')   # lágrima de sangue
    m.poly([(4, 16), (7, 14), (8, 26), (3, 24)], 'stone')               # asas recolhidas
    m.poly([(18, 16), (15, 14), (14, 26), (19, 24)], 'stone')
    m.capsule(9, 18, 13, 18, 1.4, 'stone')
    for (x, y) in ((6, 30), (15, 27), (12, 34)):
        m.px(x, y, '#3a4a2a'); m.px(x + 1, y, '#2e3a22')
    return m.render(P)


def cage():
    m = VMat(18, 30)
    m.ellipse(9, 28, 8, 1.6, 'iron')
    m.ellipse(9, 4, 7, 2.2, 'iron')
    m.rect(8, 0, 2, 3, 'iron')
    m.ellipse(9, 22, 3.5, 3, 'bone')          # esqueleto encolhido
    m.ellipse(9, 15, 2.6, 2.4, 'bone')
    m.px(8, 15, '#140c10'); m.px(10, 15, '#140c10')
    for x in (2, 5, 9, 13, 16):
        m.rect(x, 5, 1, 23, 'iron')
    return m.render(P)


def banner():
    m = VMat(14, 28)
    m.rect(0, 1, 14, 2, 'iron')
    m.poly([(2, 3), (12, 3), (12, 22), (7, 26), (2, 22)], 'cloth')
    m.rect(2, 3, 10, 1, 'gold')
    for (x, y) in ((6, 9), (7, 9), (6, 10), (7, 10), (5, 11), (8, 11), (6, 13), (7, 13), (6, 14), (7, 14), (4, 16), (9, 16)):
        m.px(x, y, '#d4a040')
    return m.render(P)


def chains():
    m = VMat(12, 22)
    for x in (3, 8):
        for y in range(0, 18, 3):
            m.ellipse(x + (y // 3) % 2 * 0.5, y + 1.5, 1.2, 1.6, 'iron')
    m.rect(1, 18, 10, 3, 'iron')
    return m.render(P)


def web():
    img = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
    p = img.load()
    for a in range(0, 91, 18):
        r = math.radians(a)
        for d in range(16):
            x, y = int(math.cos(r) * d), int(math.sin(r) * d)
            if x < 16 and y < 16:
                p[x, y] = (200, 200, 212, 200)
    for rr in (5, 9, 13):
        for a in range(0, 91, 4):
            r = math.radians(a)
            x, y = int(math.cos(r) * rr), int(math.sin(r) * rr)
            if x < 16 and y < 16:
                p[x, y] = (200, 200, 212, 170)
    return img


def bonepile():
    m = VMat(18, 12)
    m.ellipse(9, 9, 8, 2.6, 'bone')
    m.capsule(3, 7, 13, 5, 1.0, 'bone')
    m.capsule(6, 9, 15, 8, 0.9, 'bone')
    m.ellipse(12, 5, 3, 2.8, 'bone')
    m.px(11, 5, '#140c10'); m.px(13, 5, '#140c10')
    return m.render(P)


def blood_decal(seed):
    import random
    rnd = random.Random(seed)
    img = Image.new('RGBA', (28, 16), (0, 0, 0, 0))
    p = img.load()
    cx, cy = 14, 8
    for _ in range(140):
        a, r = rnd.random() * 6.28, rnd.random() ** 1.4 * 7
        x, y = int(cx + math.cos(a) * r * 1.6), int(cy + math.sin(a) * r * 0.8)
        if 0 <= x < 28 and 0 <= y < 16:
            p[x, y] = (74, 8, 16, 255) if r > 3 else (96, 14, 22, 255)
    for _ in range(8):
        a = rnd.random() * 6.28
        x, y = int(cx + math.cos(a) * 12), int(cy + math.sin(a) * 6)
        if 0 <= x < 28 and 0 <= y < 16:
            p[x, y] = (74, 8, 16, 255)
    return img


def wall_torch(f):
    m = VMat(12, 22)
    m.rect(4, 12, 4, 2, 'iron')
    m.poly([(3, 9), (9, 9), (7, 14), (5, 14)], 'iron')
    m.rect(5, 14, 2, 6, 'wood')
    flame(m, 6, 8, 6 + (f % 2), f)
    flame(m, 5, 8, 4, f + 1)
    flame(m, 7, 8, 4, f + 2)
    return m.render(P)


# ------------------------------------------------------------------ balas
def bullets():
    out = {}
    # brasa inimiga: contorno escuro, anel vermelho, miolo branco-quente
    for name, R in (('enemy', 4.6), ('enemyBig', 7.4)):
        n = int(R * 2 + 3)
        img = Image.new('RGBA', (n, n), (0, 0, 0, 0))
        p = img.load()
        c = n / 2
        for y in range(n):
            for x in range(n):
                d = math.hypot(x + 0.5 - c, y + 0.5 - c)
                if d <= R + 1:
                    col = (60, 0, 14, 255)
                    if d <= R:
                        col = (214, 24, 48, 255)
                    if d <= R * 0.72:
                        col = (255, 82, 82, 255)
                    if d <= R * 0.45:
                        col = (255, 220, 210, 255)
                    p[x, y] = col
        p[int(c - R * 0.45), int(c - R * 0.45)] = (255, 255, 255, 255)
        out[name] = img
    # chama de cera (chefe)
    img = Image.new('RGBA', (11, 13), (0, 0, 0, 0))
    p = img.load()
    for y in range(13):
        for x in range(11):
            dx, dy = (x + 0.5 - 5.5) / 4.6, (y + 0.5 - 8) / 4.6
            tip = y < 8 and abs(x + 0.5 - 5.5) < (y - 1) * 0.6
            if dx * dx + dy * dy <= 1 or tip:
                d = math.hypot(dx, dy)
                p[x, y] = (255, 246, 208, 255) if d < 0.35 else (255, 196, 80, 255) if d < 0.7 else (255, 110, 40, 255)
    out['waxflame'] = add_ol(img, (70, 14, 4, 255))
    # traçante do jogador (aponta para a direita; o código gira)
    img = Image.new('RGBA', (14, 5), (0, 0, 0, 0))
    p = img.load()
    for x in range(14):
        k = x / 13
        col = (255, 255, 255, 255) if k > 0.82 else (255, 244, 170, 255) if k > 0.5 else (255, 190, 90, 255) if k > 0.22 else (230, 110, 50, 255)
        h = 1 if k < 0.3 else 2 if k < 0.75 else 3
        for y in range(2 - h // 2, 2 - h // 2 + h):
            p[x, y] = col
    out['tracer'] = img
    # cápsula de bala
    img = Image.new('RGBA', (5, 3), (0, 0, 0, 0))
    p = img.load()
    for x in range(5):
        for y in range(3):
            p[x, y] = (230, 176, 72, 255) if y == 0 else (176, 122, 40, 255) if y == 1 else (122, 78, 24, 255)
    p[4, 1] = (90, 56, 16, 255)
    out['casing'] = img
    return out


def add_ol(img, col):
    w, h = img.size
    p = img.load()
    add = []
    for y in range(h):
        for x in range(w):
            if p[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and p[nx, ny][3]:
                    add.append((x, y)); break
    for x, y in add:
        p[x, y] = col
    return img


# ------------------------------------------------------------------ armas v2
def _hand(m, x, y):
    m.ellipse(x, y, 2.2, 2.0, 'glove')


def weapons():
    W = {}

    def mk(name, w, h, gx, gy, fn):
        m = VMat(w, h)
        fn(m)
        _hand(m, gx, gy)
        W[name] = (m.render(P), gx, gy)

    mk('pistola', 18, 11, 4, 6, lambda m: (m.poly([(2, 9), (4, 4), (7, 4), (6, 9)], 'wood'), m.rect(4, 2, 12, 3, 'iron'),
                                            m.ellipse(8, 4.5, 2.4, 2.2, 'iron'), m.rect(15, 1, 1, 1, 'iron'), m.rect(7, 5, 2, 2, 'brass')))
    mk('escopeta', 28, 11, 10, 7, lambda m: (m.poly([(0, 6), (9, 4), (10, 8), (1, 9)], 'wood'), m.rect(9, 3, 18, 2, 'iron'),
                                              m.rect(9, 5, 17, 1, 'iron'), m.rect(13, 6, 6, 2, 'woodL')))
    mk('metralhadora', 26, 13, 9, 8, lambda m: (m.rect(3, 3, 17, 4, 'iron'), m.rect(20, 4, 5, 2, 'iron'), m.rect(9, 7, 3, 5, 'iron'),
                                                 m.poly([(0, 4), (4, 3), (4, 7), (1, 8)], 'wood'), m.rect(13, 2, 4, 1, 'brass')))
    mk('rifle', 32, 11, 12, 7, lambda m: (m.poly([(0, 5), (11, 4), (11, 7), (0, 9)], 'wood'), m.rect(11, 4, 20, 2, 'iron'),
                                           m.rect(12, 2, 9, 2, 'iron'), m.px(12, 2, '#7fd6ff'), m.px(20, 2, '#7fd6ff'), m.rect(13, 6, 5, 2, 'woodL')))
    mk('bolhas', 22, 13, 6, 9, lambda m: (m.ellipse(8, 6, 6, 4, 'brass'), m.rect(13, 5, 6, 2, 'iron'), m.ellipse(19.5, 6, 1.8, 1.8, 'wax')))
    mk('raio', 26, 13, 8, 9, lambda m: (m.rect(2, 3, 16, 5, 'iron'), m.rect(18, 4, 7, 3, 'iron'), m.rect(5, 4, 10, 2, 'gold'),
                                         m.px(24, 5, '#e0ffff')))
    mk('foguete', 30, 14, 10, 10, lambda m: (m.rect(1, 3, 23, 6, 'wood'), m.rect(23, 2, 5, 8, 'iron'), m.rect(5, 4, 2, 4, 'cloth'),
                                              m.rect(9, 9, 3, 3, 'iron')))
    mk('besta', 24, 17, 7, 10, lambda m: (m.rect(2, 7, 17, 2, 'wood'), m.line(16, 1, 18, 8, 'iron'), m.line(18, 8, 16, 15, 'iron'),
                                           m.line(16, 1, 7, 8, 'bone'), m.line(7, 8, 16, 15, 'bone'), m.rect(18, 7, 5, 1, 'iron')))
    mk('revolver', 20, 12, 4, 7, lambda m: (m.rect(3, 2, 16, 3, 'gold'), m.ellipse(9, 5, 3, 2.4, 'gold'), m.poly([(2, 10), (4, 5), (7, 5), (6, 10)], 'wood')))
    mk('estrelas', 22, 13, 6, 9, lambda m: (m.rect(2, 3, 14, 5, 'cloth'), m.rect(16, 2, 3, 7, 'gold'), m.px(19, 4, '#fff6b0'),
                                             m.px(19, 5, '#fff6b0')))
    return W


if __name__ == '__main__':
    from vol import contact_sheet
    rows = [[candelabra(i) for i in range(4)] + [candles(i) for i in range(4)],
            [coffin(), statue(), cage(), banner(), chains(), web(), bonepile(), blood_decal(1), wall_torch(0), wall_torch(1)],
            list(bullets().values()) + [w[0] for w in weapons().values()]]
    contact_sheet(rows, scale=4).save('/tmp/claude-0/-home-claude/18552d67-fdcd-5f48-be1b-c20c76891952/scratchpad/props.png')
    print('ok')
