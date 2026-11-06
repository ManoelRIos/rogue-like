"""Personagens originais: o Errante (jogador), 8 inimigos e 5 chefes."""
import math
from PIL import Image
from pixtool import Mat, rotate_img, add_outline, OUTLINE
from palette import M

EYE = '#1a1020'
WHITE = '#fff6e8'

# ============================================================== JOGADOR (24x26)
PW, PH = 24, 26


def _player_front(m, b, ll, rl, flutter, blink=False, back=False):
    # pernas e botas
    m.rect(9, 20 + ll, 2, 3, 'pants')
    m.rect(13, 20 + rl, 2, 3, 'pants')
    m.rect(8, 22 + ll, 3, 2, 'boots')
    m.rect(13, 22 + rl, 3, 2, 'boots')
    # poncho (liso, com uma faixa de padrão)
    pon = 'ponchoB' if back else 'poncho'
    m.poly([(7.5, 13 + b), (16.5, 13 + b), (19, 20.5 + b), (5, 20.5 + b)], pon)
    for x in range(6, 18):
        if (x + flutter) % 3 == 0:
            m.set(x, 21 + b, pon)
    for x in range(6, 18):
        m.set(x, 18 + b - (1 if (x // 2) % 2 else 0), 'pattern')
    # cabeça grande e legível
    if back:
        m.rect(8, 7 + b, 8, 5, 'hair')
    else:
        m.rect(8, 7 + b, 8, 6, 'skin')
        m.rect(7, 7 + b, 1, 4, 'hair'); m.rect(16, 7 + b, 1, 4, 'hair')
    # lenço
    m.rect(8, 12 + b, 8, 2, 'scarf')
    if not back:
        m.set(11, 14 + b, 'scarf'); m.set(12, 14 + b, 'scarf')
    # chapéu (copa menor, aba fina)
    m.ellipse(12, 3.2 + b, 3.6, 2.6, 'hat')
    m.rect(9, 3 + b, 6, 3, 'hat')
    m.rect(9, 5 + b, 6, 1, 'band')
    m.ellipse(12, 6.6 + b, 7.6, 1.6, 'hat')
    if not back:
        for x in range(8, 16):
            m.px(x, 8 + b, '#b8704e')
        if blink:
            m.px(10, 10 + b, '#7a3a28'); m.px(13, 10 + b, '#7a3a28')
        else:
            for ex in (10, 13):
                m.px(ex, 9 + b, EYE); m.px(ex, 10 + b, EYE)
            m.px(10, 9 + b, '#5a3a4a'); m.px(13, 9 + b, '#5a3a4a')
        m.px(9, 11 + b, '#e88a7a'); m.px(14, 11 + b, '#e88a7a')
    m.px(10, 1 + b, '#c88a62')  # brilho no chapéu


def _player_side(m, b, front_leg, back_leg, flutter, blink=False):
    bx, by = back_leg
    fx, fy = front_leg
    m.rect(10 + bx, 20 + by, 2, 2, 'pants')
    m.rect(10 + bx, 22 + by, 3, 2, 'boots')
    m.rect(12 + fx, 20 + fy, 2, 2, 'pants')
    m.rect(12 + fx, 22 + fy, 3, 2, 'boots')
    for x in range(10, 14):
        m.px(x + bx, 22 + by, '#2a160e')
    m.poly([(8.5, 13 + b), (15.5, 13 + b), (18, 20.5 + b), (6, 20.5 + b)], 'poncho')
    for x in range(7, 18):
        if (x + flutter) % 3 == 0:
            m.set(x, 21 + b, 'poncho')
    for x in range(7, 18):
        m.set(x, 18 + b - (1 if (x // 2) % 2 else 0), 'pattern')
    # cabeça de perfil
    m.rect(10, 7 + b, 7, 6, 'skin')
    m.set(17, 10 + b, 'skin')
    m.rect(8, 7 + b, 3, 5, 'hair')
    m.rect(9, 12 + b, 7, 2, 'scarf')
    m.set(8, 13 + b + flutter % 2, 'scarf'); m.set(7, 14 + b + flutter % 2, 'scarf'); m.set(6, 14 + b + flutter % 2, 'scarf')
    m.ellipse(11.5, 3.2 + b, 3.6, 2.6, 'hat')
    m.rect(8, 3 + b, 7, 3, 'hat')
    m.rect(8, 5 + b, 7, 1, 'band')
    m.ellipse(12.5, 6.6 + b, 7.6, 1.6, 'hat')
    for x in range(10, 18):
        m.px(x, 8 + b, '#b8704e')
    if blink:
        m.px(14, 10 + b, '#7a3a28')
    else:
        m.px(14, 9 + b, EYE); m.px(14, 10 + b, EYE)
    m.px(13, 11 + b, '#e88a7a')
    m.px(9, 1 + b, '#c88a62')


def player_frames():
    """Retorna dict anim -> lista de imagens."""
    anims = {}
    run_ll = [0, -1, -1, 0, 0, 0]
    run_rl = [0, 0, 0, 0, -1, -1]
    run_b = [0, -1, 0, 0, -1, 0]
    side_front = [(2, 0), (3, -1), (1, -1), (-1, 0), (-2, 0), (0, 0)]
    side_back = [(-2, 0), (-1, 0), (0, 0), (2, 0), (3, -1), (1, -1)]
    idle_b = [0, 0, 1, 1]

    def mk(fn, *a, **k):
        m = Mat(PW, PH)
        fn(m, *a, **k)
        return m.render(M)

    anims['idle_down'] = [mk(_player_front, idle_b[i], 0, 0, i // 2, blink=(i == 3)) for i in range(4)]
    anims['idle_up'] = [mk(_player_front, idle_b[i], 0, 0, i // 2, back=True) for i in range(4)]
    anims['idle_side'] = [mk(_player_side, idle_b[i], (1, 0), (-1, 0), i // 2, blink=(i == 3)) for i in range(4)]
    anims['run_down'] = [mk(_player_front, run_b[i], run_ll[i], run_rl[i], i) for i in range(6)]
    anims['run_up'] = [mk(_player_front, run_b[i], run_rl[i], run_ll[i], i, back=True) for i in range(6)]
    anims['run_side'] = [mk(_player_side, run_b[i], side_front[i], side_back[i], i) for i in range(6)]

    # cambalhota: bola girando (desenhada uma vez e rotacionada)
    roll = []
    for i in range(6):
        m = Mat(PW, PH)
        cx, cy = 12, 17
        m.circle(cx, cy, 7, 'poncho')
        a = -i * (math.pi * 2 / 6)
        # faixa do padrão
        for t in range(-6, 7):
            m.set(cx + math.cos(a) * t, cy + math.sin(a) * t, 'pattern')
        # chapéu na borda da bola
        hx, hy = cx + math.cos(a - math.pi / 2) * 6, cy + math.sin(a - math.pi / 2) * 6
        m.ellipse(hx, hy, 4, 2.6, 'hat')
        m.set(cx + math.cos(a + 2.2) * 5, cy + math.sin(a + 2.2) * 5, 'boots')
        m.set(cx + math.cos(a + 2.6) * 5, cy + math.sin(a + 2.6) * 5, 'boots')
        roll.append(m.render(M))
    anims['roll'] = roll

    # morte
    base = mk(_player_front, 0, 0, 0, 0)

    def lay(img, ang, dy):
        r = img.rotate(ang, resample=Image.NEAREST, expand=True)
        out = Image.new('RGBA', (PW, PH))
        out.alpha_composite(r.crop((max(0, (r.width - PW) // 2), max(0, r.height - PH), max(0, (r.width - PW) // 2) + PW, r.height)), (0, 0))
        return out
    d0 = base.copy()
    d1 = lay(base, 30, 0)
    d2 = lay(base, 90, 0)
    d3 = d2.copy()
    anims['death'] = [d0, d1, d2, d3]
    return anims


# ============================================================== INIMIGOS
def enemy_frames():
    E = {}

    # ---- Gosma (20x18)
    fr = []
    for i, (sx, sy) in enumerate([(1, 1), (1.1, 0.88), (1, 1), (0.92, 1.1)]):
        m = Mat(20, 18)
        m.ellipse(10, 17 - 6 * sy, 8.5 * sx, 6.4 * sy, 'slime')
        m.ellipse(10, 17 - 10 * sy, 5.6 * sx, 4.6 * sy, 'slime')
        for x, y in ((6, 13), (13, 12), (9, 15)):
            m.set(x, y - (1 - sy) * 6, 'slimeD')
        ey = int(17 - 9 * sy)
        for ex in (7, 12):
            m.px(ex, ey, WHITE); m.px(ex + 1, ey, WHITE); m.px(ex, ey + 1, WHITE); m.px(ex + 1, ey + 1, EYE)
        m.px(9, ey + 3, '#173a1e'); m.px(10, ey + 3, '#173a1e')
        m.px(6, ey - 3, '#e4ffd0'); m.px(7, ey - 4, '#e4ffd0')
        fr.append(m.render(M))
    E['gosma'] = fr

    # ---- Cogumelo (18x22)
    fr = []
    for i in range(4):
        m = Mat(18, 22)
        b = 1 if i % 2 else 0
        m.rect(5, 19 - (i == 1), 3, 2, 'stem'); m.rect(10, 19 - (i == 3), 3, 2, 'stem')
        m.rect(5, 11 + b, 8, 8, 'stem')
        m.ellipse(9, 8 + b, 8.5, 6, 'cap')
        for x in range(0, 18):
            for y in range(10 + b, 14 + b):
                pass
        m.rect(1, 11 + b, 16, 1, 'cap')
        for (x, y, r) in ((5, 5, 1.6), (12, 6, 1.3), (9, 3, 1.0), (14, 9, 0.9)):
            m.ellipse(x, y + b, r, r, 'white')
        m.px(7, 14 + b, EYE); m.px(10, 14 + b, EYE)
        m.px(8, 16 + b, '#9c5a4c'); m.px(9, 16 + b, '#9c5a4c')
        fr.append(m.render(M))
    E['cogumelo'] = fr

    # ---- Caveira pistoleira (18x26)
    fr = []
    for i in range(4):
        m = Mat(18, 26)
        b = 1 if i in (1, 3) else 0
        lf = [0, -1, 0, 0][i]; rf = [0, 0, 0, -1][i]
        m.rect(6, 19, 1, 4 + lf, 'bone'); m.rect(10, 19, 1, 4 + rf, 'bone')
        m.rect(5, 23 + lf, 3, 1, 'boneD'); m.rect(10, 23 + rf, 3, 1, 'boneD')
        m.rect(5, 18 + b, 7, 2, 'bone')
        # costelas
        m.rect(6, 12 + b, 5, 6, 'dark')
        for y in (13, 15):
            m.rect(5, y + b, 7, 1, 'bone')
        m.rect(8, 12 + b, 1, 6, 'bone')
        # braço com revólver
        m.rect(12, 13 + b, 1, 3, 'bone'); m.rect(13, 15 + b, 3, 2, 'iron'); m.set(16, 15 + b, 'iron')
        m.rect(4, 13 + b, 1, 4, 'bone')
        # bandana
        m.rect(5, 11 + b, 7, 1, 'rag'); m.set(4, 12 + b, 'rag'); m.set(3, 13 + b + (i % 2), 'rag')
        # crânio
        m.ellipse(8.5, 6 + b, 5.2, 5, 'bone')
        m.rect(6, 9 + b, 5, 2, 'bone')
        m.px(6, 6 + b, EYE); m.px(7, 6 + b, EYE); m.px(6, 7 + b, EYE); m.px(7, 7 + b, '#ff4030')
        m.px(10, 6 + b, EYE); m.px(11, 6 + b, EYE); m.px(10, 7 + b, EYE); m.px(11, 7 + b, '#ff4030')
        m.px(8, 8 + b, '#6e6656'); m.px(7, 10 + b, EYE); m.px(9, 10 + b, EYE)
        # chapéu-coco rasgado
        m.rect(5, 1 + b, 7, 2, 'hat'); m.rect(3, 3 + b, 11, 1, 'hat')
        fr.append(m.render(M))
    E['caveira'] = fr

    # ---- Morcego (24x16)
    fr = []
    for i in range(4):
        m = Mat(24, 16)
        phase = [0, 1, 2, 1][i]
        for side in (-1, 1):
            pts = [(12, 7), (12 + side * 11, [2, 7, 12][phase]), (12 + side * 8, [8, 9, 10][phase]), (12 + side * 5, 9), (12 + side * 2, 10)]
            m.poly(pts, 'batW')
        m.ellipse(12, 8, 3.4, 4, 'bat')
        m.set(10, 3, 'bat'); m.set(14, 3, 'bat'); m.set(10, 4, 'bat'); m.set(14, 4, 'bat')
        m.px(11, 7, '#ffe14d'); m.px(13, 7, '#ffe14d')
        m.px(11, 10, WHITE); m.px(13, 10, WHITE)
        fr.append(m.render(M))
    E['morcego'] = fr

    # ---- Lanterna espírito (16x24)
    fr = []
    for i in range(4):
        m = Mat(16, 24)
        m.line(8, 0, 8, 2, 'metal')
        m.rect(5, 3, 6, 1, 'metal'); m.rect(4, 4, 8, 1, 'metal')
        m.rect(4, 5, 8, 10, 'glass')
        m.rect(4, 5, 1, 10, 'metal'); m.rect(11, 5, 1, 10, 'metal'); m.rect(7, 5, 2, 10, 'glass')
        fl = ['#fff3c4', '#ffe08a', '#fff3c4', '#ffd060'][i]
        m.ellipse(8, 10 + (i % 2) * 0.5, 2.2, 3, 'glass')
        m.px(8, 9, fl); m.px(8, 10, fl); m.px(7, 10, fl)
        m.px(6, 8, EYE); m.px(10, 8, EYE); m.px(6, 12, EYE); m.px(10, 12, EYE); m.px(7, 13, EYE); m.px(9, 13, EYE)
        m.rect(4, 15, 8, 1, 'metal'); m.rect(5, 16, 6, 2, 'metal')
        # chama/fumaça embaixo
        for k in range(3):
            m.set(6 + k * 2, 18 + ((i + k) % 2), 'ghost')
            m.set(6 + k * 2, 19 + ((i + k) % 3), 'ghost')
        fr.append(m.render(M))
    E['lanterna'] = fr

    # ---- Totem (18x26)
    fr = []
    for i in range(4):
        m = Mat(18, 26)
        m.rect(3, 4, 12, 20, 'stone')
        m.rect(2, 2, 14, 3, 'stone'); m.rect(4, 1, 10, 1, 'stone')
        m.rect(2, 23, 14, 2, 'stone')
        for x in (4, 13):
            for y in range(6, 22, 3):
                m.px(x, y, '#57506a')
        m.line(5, 8, 7, 8, 'dark'); m.line(10, 8, 12, 8, 'dark')
        m.rect(5, 18, 8, 1, 'dark')
        m.circle(9, 13, 3.4, 'dark')
        glow = ['#ff3b3b', '#ff6a3a', '#ffa040', '#ff6a3a'][i]
        m.ellipse(9, 13, 2.2, 2.2, 'red')
        m.px(9, 13, glow); m.px(8, 12, '#ffe1a0')
        for x, y in ((3, 22), (4, 21), (14, 6), (15, 7), (13, 22)):
            m.set(x, y, 'moss')
        fr.append(m.render(M))
    E['torreta'] = fr

    # ---- Cristal (16x24)
    fr = []
    for i in range(4):
        m = Mat(16, 24)
        pts = [(8, 1), (14, 9), (11, 21), (5, 21), (2, 9)]
        m.poly(pts, 'crystal')
        band = 4 + i * 2
        m.line(band, 3, band - 2, 19, 'white')
        m.line(8, 1, 8, 21, 'blue')
        m.circle(8, 10, 2.2, 'white')
        m.px(8, 10, '#9a1460'); m.px(9, 10, '#9a1460')
        fr.append(m.render(M))
    E['cristal'] = fr

    # ---- Besouro (24x20)
    fr = []
    for i in range(4):
        m = Mat(24, 20)
        for k in range(3):
            off = ((i + k) % 2)
            m.line(6, 8 + k * 3, 2, 10 + k * 3 + off, 'leg')
            m.line(18, 8 + k * 3, 22, 10 + k * 3 + (1 - off), 'leg')
        m.ellipse(12, 11, 8, 7, 'shell')
        m.line(12, 5, 12, 18, 'shellD')
        m.px(8, 7, '#a8c4ff'); m.px(7, 8, '#a8c4ff'); m.px(15, 7, '#a8c4ff')
        m.ellipse(12, 4.5, 4, 2.6, 'shellD')
        m.line(12, 3, 12, 0, 'gold'); m.set(11, 1, 'gold')
        m.px(10, 4, '#ff6060'); m.px(14, 4, '#ff6060')
        fr.append(m.render(M))
    E['besouro'] = fr
    return E


# ============================================================== CHEFES (48x48)
def boss_frames():
    B = {}
    S = 48

    fr = []  # Rei Gosma
    for i, (sx, sy) in enumerate([(1, 1), (1.06, 0.92), (1, 1), (0.95, 1.06)]):
        m = Mat(S, S)
        m.ellipse(24, 47 - 13 * sy, 21 * sx, 13 * sy, 'slime')
        m.ellipse(24, 47 - 22 * sy, 15 * sx, 11 * sy, 'slime')
        for x, y in ((12, 38), (30, 36), (20, 42), (36, 41)):
            m.circle(x, y, 1.4, 'slimeD')
        top = int(47 - 32 * sy)
        m.rect(15, top - 1, 18, 4, 'gold')
        for k in range(5):
            m.rect(15 + k * 4, top - 4, 2, 3, 'gold')
        m.px(19, top, '#e8364f'); m.px(24, top, '#3ac1e8'); m.px(29, top, '#e8364f')
        ey = int(47 - 20 * sy)
        for ex in (15, 29):
            m.rect(ex, ey, 5, 5, 'white'); m.rect(ex + 2, ey + 2, 2, 2, 'dark')
        m.rect(17, ey + 8, 14, 3, 'dark'); m.rect(19, ey + 8, 2, 1, 'white'); m.rect(27, ey + 8, 2, 1, 'white')
        m.px(14, ey - 6, '#e4ffd0'); m.px(15, ey - 7, '#e4ffd0'); m.px(16, ey - 7, '#e4ffd0')
        fr.append(m.render(M))
    B['reiGosma'] = fr

    fr = []  # Sino Fantasma
    for i in range(4):
        m = Mat(S, S)
        dy = [0, -1, -2, -1][i]
        m.ellipse(24, 16 + dy, 13, 12, 'ghost')
        for y in range(16, 38):
            w = 13 + (y - 16) * 0.45
            m.rect(24 - w, y + dy, w * 2, 1, 'ghost')
        for x in range(4, 45, 4):
            m.set(x + (i % 2) * 2, 38 + dy, 'ghost')
        m.rect(5, 36 + dy, 38, 2, 'purple')
        m.rect(16, 15 + dy, 5, 7, 'dark'); m.rect(27, 15 + dy, 5, 7, 'dark')
        m.px(18, 17 + dy, '#9b5cff'); m.px(29, 17 + dy, '#9b5cff')
        m.ellipse(24, 29 + dy, 4, 3.4, 'dark')
        sw = [-3, 0, 3, 0][i]
        m.line(24, 38 + dy, 24 + sw, 43 + dy, 'metal')
        m.circle(24 + sw, 44 + dy, 2.6, 'gold')
        m.rect(21, 1 + dy, 6, 3, 'metal')
        for k in range(4):
            m.set(4 + k, 12 + k * 4 + dy, 'iron'); m.set(43 - k, 12 + k * 4 + dy, 'iron')
        fr.append(m.render(M))
    B['sinoFantasma'] = fr

    fr = []  # Golem da Forja
    for i in range(4):
        m = Mat(S, S)
        a = [0, 1, 0, -1][i]
        m.rect(14, 36, 8, 11, 'rock'); m.rect(26, 36, 8, 11, 'rock')
        m.ellipse(24, 26, 15, 13, 'rock')
        m.circle(7, 24 + a, 6, 'rock'); m.circle(41, 24 - a, 6, 'rock')
        m.circle(6, 33 + a, 5, 'rock'); m.circle(42, 33 - a, 5, 'rock')
        m.ellipse(24, 12, 9, 7, 'rock')
        for (x0, y0, x1, y1) in ((14, 20, 19, 26), (34, 18, 30, 24), (20, 34, 24, 38), (8, 28, 6, 34)):
            m.line(x0, y0, x1, y1, 'lava')
        glow = ['#ff7a2f', '#ffb347', '#fff1c0', '#ffb347'][i]
        m.rect(19, 10, 4, 3, 'lava'); m.rect(26, 10, 4, 3, 'lava')
        m.px(20, 11, glow); m.px(27, 11, glow)
        m.circle(24, 26, 5, 'dark'); m.circle(24, 26, 3.6, 'lava')
        m.px(24, 26, glow); m.px(23, 25, '#fff1c0')
        fr.append(m.render(M))
    B['golem'] = fr

    fr = []  # Aranha Relojoeira
    for i in range(4):
        m = Mat(S, S)
        for k in range(4):
            y = 18 + k * 5
            o = 2 if (i + k) % 2 else -1
            m.line(16, y, 7 - k, y - 6 + o, 'leg'); m.line(7 - k, y - 6 + o, 2 - k, y + 10 + o, 'leg')
            m.line(32, y, 41 + k, y - 6 - o, 'leg'); m.line(41 + k, y - 6 - o, 46 + k, y + 10 - o, 'leg')
        m.ellipse(24, 34, 7, 5, 'brass')
        m.circle(24, 21, 12, 'brass')
        m.circle(24, 21, 9, 'white')
        for k in range(12):
            a2 = k / 12 * math.pi * 2
            m.px(24 + math.cos(a2) * 7.6, 21 + math.sin(a2) * 7.6, EYE)
        ang = i * math.pi / 2
        m.line(24, 21, 24 + math.cos(ang - math.pi / 2) * 6, 21 + math.sin(ang - math.pi / 2) * 6, 'dark', )
        m.line(24, 21, 24 + math.cos(-ang) * 4, 21 + math.sin(-ang) * 4, 'red')
        m.rect(22, 6, 4, 3, 'brass'); m.rect(23, 4, 2, 2, 'gold')
        for x in (20, 23, 25, 28):
            m.px(x, 34, '#ff3b3b')
        fr.append(m.render(M))
    B['aranha'] = fr

    fr = []  # Sacerdote do Último Tiro
    for i in range(4):
        m = Mat(S, S)
        dy = [0, -1, -1, 0][i]
        for k in range(10):
            a2 = k / 10 * math.pi * 2 + i * 0.16
            x, y = 24 + math.cos(a2) * 16, 13 + math.sin(a2) * 5 + dy
            m.set(x, y, 'gold'); m.set(x + 1, y, 'gold')
        for y in range(16, 46):
            w = 6 + (y - 16) * 0.42
            m.rect(24 - w, y + dy, w * 2, 1, 'robe')
        for x in range(12, 37, 3):
            m.set(x + (i % 2), 46 + dy, 'robe')
        m.rect(22, 18 + dy, 4, 26, 'gold'); m.rect(17, 25 + dy, 14, 3, 'gold')
        m.ellipse(24, 14 + dy, 7, 7.5, 'robe')
        m.ellipse(24, 15 + dy, 5, 5.5, 'mask')
        m.rect(20, 14 + dy, 3, 1, 'dark'); m.rect(26, 14 + dy, 3, 1, 'dark')
        m.px(21, 14 + dy, '#ff3050'); m.px(27, 14 + dy, '#ff3050')
        m.line(24, 17 + dy, 24, 19 + dy, 'boneD')
        m.circle(9, 30 + dy + (i % 2), 3, 'mask'); m.circle(39, 30 + dy - (i % 2), 3, 'mask')
        fr.append(m.render(M))
    B['sacerdote'] = fr
    return B
