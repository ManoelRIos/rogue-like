"""Armas, itens, efeitos e projéteis."""
import math
from PIL import Image
from pixtool import Mat, add_outline, hexc
from palette import M

EYE = '#1a1020'
HAND = 'skin'


def _hand(m, x, y):
    m.rect(x, y, 3, 3, HAND)
    m.px(x, y + 2, '#9a5a40')


# Cada arma: (largura, altura, x da empunhadura, y da empunhadura, função de desenho)
def weapon_images():
    W = {}

    def mk(name, w, h, gx, gy, fn):
        m = Mat(w, h)
        fn(m)
        _hand(m, gx - 1, gy - 1)
        W[name] = (m.render(M), gx, gy)

    mk('pistola', 16, 10, 4, 5, lambda m: (
        m.rect(3, 2, 11, 3, 'iron'), m.rect(13, 2, 2, 2, 'iron'), m.rect(3, 4, 3, 5, 'wood'), m.rect(6, 5, 2, 1, 'iron')))
    mk('escopeta', 26, 10, 9, 6, lambda m: (
        m.rect(1, 4, 9, 4, 'wood'), m.rect(0, 5, 2, 3, 'wood'), m.rect(9, 3, 16, 2, 'iron'), m.rect(9, 5, 15, 1, 'iron'),
        m.rect(13, 6, 5, 2, 'wood')))
    mk('metralhadora', 24, 12, 8, 7, lambda m: (
        m.rect(3, 3, 15, 4, 'metal'), m.rect(18, 4, 5, 2, 'iron'), m.rect(8, 7, 3, 4, 'iron'), m.rect(4, 7, 2, 3, 'wood'),
        m.rect(0, 4, 3, 2, 'metal'), m.px(7, 4, '#c0443a'), m.px(11, 4, '#c0443a')))
    mk('rifle', 30, 10, 11, 6, lambda m: (
        m.rect(0, 4, 10, 3, 'wood'), m.rect(0, 7, 3, 1, 'wood'), m.rect(10, 4, 19, 2, 'iron'), m.rect(11, 2, 8, 2, 'dark'),
        m.px(11, 2, '#7fd6ff'), m.px(18, 2, '#7fd6ff'), m.rect(12, 6, 4, 2, 'wood')))
    mk('bolhas', 20, 12, 5, 8, lambda m: (
        m.ellipse(7, 5, 6, 4, 'pink'), m.rect(12, 4, 6, 2, 'gold'), m.circle(18, 5, 1.8, 'cyan'), m.rect(4, 8, 3, 3, 'pink')))
    mk('raio', 24, 12, 7, 8, lambda m: (
        m.rect(2, 3, 15, 5, 'blue'), m.rect(17, 4, 6, 3, 'iron'), m.rect(5, 4, 9, 2, 'cyan'), m.rect(5, 8, 3, 3, 'iron'),
        m.px(22, 5, '#e0ffff')))
    mk('foguete', 28, 13, 9, 9, lambda m: (
        m.rect(1, 3, 22, 6, 'olive'), m.rect(22, 2, 4, 8, 'iron'), m.rect(4, 4, 2, 4, 'red'), m.rect(12, 4, 2, 4, 'gold'),
        m.rect(8, 9, 3, 3, 'iron'), m.rect(15, 9, 2, 2, 'iron')))
    mk('besta', 22, 15, 6, 9, lambda m: (
        m.rect(2, 6, 16, 2, 'wood'), m.line(15, 0, 17, 7, 'iron'), m.line(17, 7, 15, 14, 'iron'),
        m.line(15, 0, 6, 7, 'white'), m.line(6, 7, 15, 14, 'white'), m.rect(17, 6, 4, 1, 'iron'), m.rect(3, 8, 3, 3, 'wood')))
    mk('revolver', 19, 11, 4, 6, lambda m: (
        m.rect(2, 2, 15, 3, 'gold'), m.rect(6, 4, 5, 3, 'gold'), m.rect(2, 5, 3, 5, 'wood'), m.px(8, 5, '#5a3a10'), m.px(10, 5, '#5a3a10')))
    mk('estrelas', 21, 12, 5, 8, lambda m: (
        m.rect(2, 3, 13, 5, 'purple'), m.rect(15, 2, 3, 7, 'gold'), m.px(18, 4, '#fff6b0'), m.px(18, 5, '#fff6b0'),
        m.rect(4, 8, 3, 3, 'purple'), m.px(7, 5, '#ffe94d'), m.px(10, 4, '#ffe94d'), m.px(12, 5, '#ffe94d')))
    return W


def heart(kind):
    m = Mat(11, 10)
    shape = ['.##...##.', '####.####', '#########', '#########', '.#######.', '..#####..', '...###...', '....#....']
    for y, row in enumerate(shape):
        for x, ch in enumerate(row):
            if ch != '#':
                continue
            mat = 'red'
            if kind == 'empty' or (kind == 'half' and x >= 4):
                mat = 'dark'
            m.set(x + 1, y + 1, mat)
    img = m.render(M)
    if kind != 'empty':
        p = img.load(); p[2, 2] = hexc('#ffc0c8'); p[3, 2] = hexc('#ff9aa8')
    return img


def blank():
    m = Mat(11, 11)
    m.circle(5.5, 5.5, 4.6, 'blue'); m.circle(5.5, 5.5, 2.4, 'cyan')
    m.px(4, 3, '#ffffff')
    return m.render(M)


def coin_frames():
    out = []
    for w in (3.2, 2.2, 1.0, 2.2):
        m = Mat(9, 11)
        m.ellipse(4.5, 5.5, w, 4.5, 'gold')
        if w > 1.5:
            m.px(4, 3, '#fff6c0')
        out.append(m.render(M))
    return out


def ammo():
    m = Mat(16, 13)
    m.rect(1, 5, 14, 7, 'olive')
    m.rect(5, 7, 6, 2, 'gold')
    for i in range(4):
        m.rect(2 + i * 3, 1, 2, 4, 'gold')
    return m.render(M)


def chest(open_, rare):
    m = Mat(22, 18)
    wood, band = ('blue', 'gold') if rare else ('wood', 'iron')
    m.rect(1, 7, 20, 10, wood)
    if not open_:
        m.rect(1, 2, 20, 5, wood); m.rect(2, 1, 18, 1, wood)
        m.rect(1, 6, 20, 1, band)
        m.rect(9, 5, 4, 4, band); m.px(10, 7, EYE); m.px(11, 7, EYE)
    else:
        m.rect(1, 0, 20, 3, wood); m.rect(2, 3, 18, 4, 'dark'); m.rect(4, 5, 14, 2, 'gold')
    m.rect(3, 7, 1, 10, band); m.rect(18, 7, 1, 10, band)
    return m.render(M)


def pedestal():
    m = Mat(18, 12)
    m.rect(2, 3, 14, 8, 'purple'); m.rect(1, 1, 16, 3, 'purple')
    return m.render(M)


def torch_frames():
    out = []
    for i in range(4):
        m = Mat(10, 18)
        m.rect(4, 9, 2, 7, 'wood'); m.rect(2, 15, 6, 2, 'iron'); m.rect(3, 8, 4, 2, 'iron')
        fy = [0, 1, 0, -1][i] * 0.5
        m.ellipse(5, 5 + fy, 3, 4, 'lava')
        m.ellipse(5, 6, 1.6, 2.2, 'glass')
        m.px(5 + (1 if i % 2 else -1), 0, '#ff8a2a')
        out.append(m.render(M))
    return out


def crate_face():
    m = Mat(16, 16)
    m.rect(0, 0, 16, 16, 'wood')
    for y in range(0, 16, 5):
        m.rect(0, y, 16, 1, 'woodD')
    m.rect(0, 0, 2, 16, 'woodD'); m.rect(14, 0, 2, 16, 'woodD'); m.rect(0, 0, 16, 2, 'woodD'); m.rect(0, 14, 16, 2, 'woodD')
    m.line(2, 2, 13, 13, 'woodD')
    for x, y in ((1, 1), (14, 1), (1, 14), (14, 14)):
        m.px(x, y, '#c9c2b0')
    return m.render(M, outline=None)


def hatch():
    m = Mat(32, 32)
    m.rect(0, 0, 32, 32, 'woodD'); m.rect(3, 3, 26, 26, 'dark')
    for i in range(5):
        m.rect(5 + i, 7 + i * 4, 22 - i * 2, 2, 'woodD')
    img = m.render(M, outline=None)
    p = img.load()
    for i in range(32):
        for (x, y) in ((i, 0), (i, 31), (0, i), (31, i)):
            p[x, y] = hexc('#a07a4a')
    return img


def shadow():
    img = Image.new('RGBA', (16, 8), (0, 0, 0, 0))
    p = img.load()
    for y in range(8):
        for x in range(16):
            dx, dy = (x + 0.5 - 8) / 8, (y + 0.5 - 4) / 4
            if dx * dx + dy * dy <= 1:
                p[x, y] = (0, 0, 0, 255)
    return img


def bullets():
    out = {}
    m = Mat(10, 10); m.circle(5, 5, 4.5, 'red'); m.circle(5, 5, 2.4, 'white'); m.px(4, 3, '#ffffff')
    out['enemy'] = m.render(M, outline=(70, 0, 25, 255))
    m = Mat(16, 16); m.circle(8, 8, 7, 'red'); m.circle(8, 8, 4, 'white'); m.px(6, 5, '#ffffff')
    out['enemyBig'] = m.render(M, outline=(70, 0, 25, 255))
    m = Mat(8, 8); m.circle(4, 4, 3.4, 'white'); m.circle(4, 4, 1.8, 'white')
    out['player'] = m.render(M, outline=None, shade=False)
    m = Mat(12, 12); m.circle(6, 6, 5, 'white'); m.circle(6, 6, 3.6, 'white')
    img = m.render(M, outline=None, shade=False)
    p = img.load()
    for y in range(12):
        for x in range(12):
            d = math.hypot(x + 0.5 - 6, y + 0.5 - 6)
            if 2.2 < d < 3.6:
                p[x, y] = (0, 0, 0, 0)
    p[4, 3] = (255, 255, 255, 255)
    out['bubble'] = img
    m = Mat(11, 11)
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5
        r = 5 if k % 2 == 0 else 2.2
        pts.append((5.5 + math.cos(a) * r, 5.5 + math.sin(a) * r))
    m.poly(pts, 'white')
    out['star'] = m.render(M, outline=None, shade=False)
    m = Mat(12, 5); m.rect(0, 1, 9, 3, 'white'); m.rect(9, 2, 3, 1, 'white')
    out['bolt'] = m.render(M, outline=None, shade=False)
    m = Mat(14, 7); m.rect(2, 1, 9, 5, 'olive'); m.rect(11, 2, 3, 3, 'red'); m.rect(0, 2, 2, 3, 'lava')
    out['rocket'] = m.render(M)
    m = Mat(10, 3); m.rect(0, 0, 10, 3, 'white')
    out['laser'] = m.render(M, outline=None, shade=False)
    return out


def fx():
    out = {}
    fr = []
    for i in range(3):
        m = Mat(14, 14)
        r = [6, 5, 3][i]
        m.circle(7, 7, r, 'glass'); m.circle(7, 7, r * 0.5, 'white')
        for k in range(4):
            a = k * math.pi / 2 + i
            m.line(7, 7, 7 + math.cos(a) * (r + 2), 7 + math.sin(a) * (r + 2), 'glass')
        fr.append(m.render(M, outline=None))
    out['muzzle'] = fr
    fr = []
    for i in range(6):
        m = Mat(32, 32)
        r = 4 + i * 2.4
        if i < 4:
            m.circle(16, 16, r, 'lava')
            m.circle(16, 16, r * 0.6, 'glass')
        if i >= 2:
            for k in range(7):
                a = k / 7 * math.pi * 2 + i
                m.circle(16 + math.cos(a) * r, 16 + math.sin(a) * r * 0.9, 3 - (i - 2) * 0.6, 'rock')
        fr.append(m.render(M, outline=None))
    out['explosion'] = fr
    fr = []
    for i in range(4):
        m = Mat(8, 8)
        m.circle(4, 4, 3.4 - i * 0.7, 'white')
        fr.append(m.render(M, outline=None))
    out['dust'] = fr
    fr = []
    for i in range(4):
        m = Mat(20, 12)
        rx = 4 + i * 2.2
        m.ellipse(10, 6, rx, rx * 0.5, 'red')
        m.ellipse(10, 6, max(0.5, rx - 1.8), max(0.3, rx * 0.5 - 1.2), 'red')
        img = m.render(M, outline=None, shade=False)
        p = img.load()
        for y in range(12):
            for x in range(20):
                dx, dy = (x + 0.5 - 10) / rx, (y + 0.5 - 6) / (rx * 0.5)
                if dx * dx + dy * dy < 0.6:
                    p[x, y] = (0, 0, 0, 0)
        fr.append(img)
    out['spawn'] = fr
    return out
