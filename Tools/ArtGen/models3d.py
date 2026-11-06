"""Modelos voxel do Coveiro, do Cultista e da Viúva de Cera (personagens originais).

As cores vêm das mesmas rampas dos sprites (tons médios); o Unity aplica o
sombreamento por face (topo claro, laterais médias, baixo escuro).
Uso: python3 Tools/ArtGen/models3d.py  → Assets/Resources/Voxels/*.json
"""
import os
import sys
import math

sys.path.insert(0, os.path.dirname(__file__))
from voxel import Model, extrude, preview

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Voxels')
ART = os.path.join(ROOT, 'Assets', 'Resources', 'Art')

# cores (tons médios das rampas do jogo)
HAT, HATD, BAND, BONE = '#36242c', '#22161e', '#8a1e26', '#c8bea0'
SKIN, SKIND, HAIR = '#d49478', '#a8645a', '#2e1e22'
COAT, COATD, LAPEL = '#283a3c', '#1a262a', '#3a5250'
SCARF, PANTS, BOOTS = '#9a1a22', '#221c2c', '#3a2416'
BELT, BRASS, EYE, GLINT = '#422618', '#b07a28', '#0a0608', '#fff6e0'


def coveiro():
    m = Model('coveiro')
    # pernas (pivô no quadril)
    for side, name in ((-1, 'pernaE'), (1, 'pernaD')):
        x0 = 1 if side > 0 else -4
        p = m.part(name, 'corpo', (x0 + 1.5, 7, 0))
        p.box(x0, 2, -2, x0 + 3, 8, 1, PANTS)
        p.box(x0 - (1 if side < 0 else 0), 0, -2, x0 + 3 + (1 if side > 0 else 0), 2, 3, BOOTS)
        p.box(x0, 2, -2, x0 + 3, 3, 2, BOOTS)
    # corpo: sobretudo afunilado com cinto, fivela, cartucheira e lapelas
    c = m.part('corpo', None, (0, 7, 0))
    c.frustum(0, 0, 6, 18, 7.2, 4.6, 5.2, 3.8, COAT)
    c.box(-1, 6, 2, 1, 11, 6, PANTS)              # abertura na frente
    c.frustum(0, 0, 11, 13, 6.9, 4.7, 6.6, 4.6, BELT)
    c.box(-1, 11, 4, 1, 13, 6, BRASS)
    for i in range(6):
        c.put(-4 + i * 1.6, 17 - i * 1.2, 4.3 - abs(i - 2.5) * 0.15, BRASS)
        c.put(-4 + i * 1.6, 16 - i * 1.2, 4.3 - abs(i - 2.5) * 0.15, BRASS)
    for y in range(13, 18):
        c.put(-2, y, 3.8, LAPEL); c.put(2, y, 3.8, LAPEL); c.put(-3, y, 3.6, LAPEL); c.put(3, y, 3.6, LAPEL)
    # braços (pivô no ombro); o direito segura a arma
    for side, name in ((-1, 'bracoE'), (1, 'bracoD')):
        a = m.part(name, 'corpo', (side * 6, 16, 0))
        a.capsule((side * 6.2, 16, 0), (side * 6.8, 10, 0.5), 1.7, COATD)
        a.ellipsoid(side * 6.8, 8.6, 0.8, 1.9, 1.9, 1.9, BOOTS)   # luva
    # cabeça (pivô no pescoço)
    h = m.part('cabeca', 'corpo', (0, 18, 0))
    h.ellipsoid(0, 23, 0, 5.4, 5.2, 5.0, SKIN)
    h.ellipsoid(0, 23.5, -0.8, 5.7, 5.2, 4.6, HAIR, cond=lambda x, y, z: z < -1 or (abs(x) > 4 and y > 21))
    h.ellipsoid(0, 19.4, 0.4, 5.5, 2.2, 5.3, SCARF, cond=lambda x, y, z: y < 20 and z > -3)
    h.ellipsoid(0, 18.4, 0, 3.4, 1.4, 3.4, SCARF)
    for ex in (-3, 2):
        for dy in range(3):
            h.put(ex, 22 + dy, 4.6, EYE); h.put(ex + 1, 22 + dy, 4.6, EYE)
            h.put(ex, 22 + dy, 4.2, EYE); h.put(ex + 1, 22 + dy, 4.2, EYE)
        h.put(ex, 24, 4.8, GLINT, glow=True)
    # chapéu (filho da cabeça)
    t = m.part('chapeu', 'cabeca', (0, 26, 0))
    for x in range(-10, 11):
        for z in range(-10, 11):
            if x * x + z * z <= 10.2 ** 2:
                t.put(x, 26, z, HAT)
                if x * x + z * z > 8.6 ** 2:
                    t.put(x, 27, z, HATD)   # aba levemente curvada
    t.frustum(0, 0, 27, 32, 4.8, 4.8, 4.2, 4.2, HAT)
    t.frustum(0, 0, 27, 29, 5.0, 5.0, 4.9, 4.9, BAND)
    for x in range(-1, 2):
        for z in range(-3, 4):
            t.put(x, 31, z, None)                  # amassado no topo
    t.box(3, 28, 4, 4, 32, 5, BONE)
    return m


ROBE, ROBED, HOOD = '#4e1518', '#330c10', '#3c1014'
MASK, MASKD, ROPE, WAX, HAND = '#d6cfbe', '#a8a090', '#a88c58', '#eee0b8', '#b0a690'
FLAME = ['#fff6d0', '#ffe08a', '#ffb347', '#ff7a2f']


def cultista():
    m = Model('cultista')
    b = m.part('corpo', None, (0, 0, 0))
    b.frustum(0, 0, 0, 15, 8.6, 7.4, 4.8, 4.2, ROBE,
              cond=lambda x, y, z: not (y == 0 and (x + z) % 3 == 0))
    for k in range(8):           # dobras escuras
        a = k / 8 * math.tau
        for y in range(1, 12):
            r = 8.6 - (8.6 - 4.8) * y / 15
            b.put(math.cos(a) * (r - 0.2), y, math.sin(a) * (r - 0.2) * 0.86, ROBED)
    b.frustum(0, 0, 9, 10, 6.0, 5.2, 5.9, 5.1, ROPE)
    b.box(2, 4, 4, 3, 9, 5, ROPE)
    h = m.part('cabeca', 'corpo', (0, 15, 0))
    h.ellipsoid(0, 19, -0.5, 6.8, 6.6, 6.4, HOOD)
    h.capsule((0, 24, -2), (0.5, 28, -3.5), 1.6, HOOD)            # ponta do capuz
    h.ellipsoid(0, 18.5, 4.2, 4.3, 4.8, 2.0, MASK)               # máscara
    for ex in (-2, 1):
        for dy in range(3):
            h.put(ex, 19 + dy, 6.0, '#050204'); h.put(ex + 1, 19 + dy, 6.0, '#050204')
            h.put(ex, 19 + dy, 5.5, '#050204'); h.put(ex + 1, 19 + dy, 5.5, '#050204')
    for (x, y) in ((1, 23), (2, 22), (2, 21), (3, 18), (3, 17)):
        h.put(x, y, 6.0, MASKD)                                   # rachadura
    for y in range(15, 19):
        h.put(-2, y, 6.0, '#a01a24')                              # lágrima pintada
    for x in range(-1, 2):
        h.put(x, 15, 6.0, '#3a3428')
    # braço esquerdo com vela (chama emissiva)
    e = m.part('bracoE', 'corpo', (-5, 13, 0))
    e.capsule((-5, 13, 0), (-7, 8, 2), 1.8, ROBED)
    e.ellipsoid(-7, 7.6, 2.5, 1.4, 1.4, 1.4, HAND)
    e.box(-8, 8, 2, -6, 13, 4, WAX)
    for i, col in enumerate(FLAME):
        e.put(-7, 13 + i, 3, col, glow=True)
    e.put(-8, 14, 3, FLAME[2], glow=True)
    # braço direito (segura a pistola)
    d = m.part('bracoD', 'corpo', (5, 13, 0))
    d.capsule((5, 13, 0), (7, 9, 2), 1.8, ROBED)
    d.ellipsoid(7, 8.4, 2.6, 1.4, 1.4, 1.4, HAND)
    return m


GOWN, GOWND, LACE, VEIL = '#2a1c36', '#1a1022', '#463c50', '#1a1422'
WAXC, WAXD, BRASSC, CHAR = '#e2c896', '#b8986a', '#a06a24', '#2a1a16'


def candle(p, x, y, z, h, flame_h=3):
    p.box(x, y, z, x + 2, y + h, z + 2, WAXC)
    for i in range(flame_h):
        p.put(x + (i % 2), y + h + i, z, FLAME[min(3, i)], glow=True)
        p.put(x + 1 - (i % 2), y + h + i, z + 1, FLAME[min(3, i + 1)], glow=True)


def viuva():
    m = Model('viuva')
    g = m.part('corpo', None, (0, 0, 0))
    g.frustum(0, 0, 0, 34, 22, 18, 8, 7, GOWN, cond=lambda x, y, z: not (y == 0 and (x * 3 + z) % 4 == 0))
    for k in range(10):
        a = k / 10 * math.tau
        for y in range(2, 30, 1):
            r = 22 - 14 * y / 34
            g.put(math.cos(a) * (r - 0.3), y, math.sin(a) * (r - 0.3) * 0.82, GOWND)
    for k in range(9):   # cera escorrendo pela saia
        a = (k / 9) * math.pi * 0.9 + math.pi * 0.05
        L = 6 + (k * 5) % 9
        for y in range(34 - L, 34):
            r = 22 - 14 * y / 34
            g.put(math.cos(a) * r, y, math.sin(a) * r * 0.82 + 0.5, WAXD)
    g.frustum(0, 0, 34, 48, 7.5, 6, 6.5, 5.2, GOWN)
    g.frustum(0, 0, 44, 46, 7.2, 5.8, 7.0, 5.6, LACE)
    g.frustum(0, 0, 47, 50, 9, 7, 8, 6.2, LACE)
    for side, name in ((-1, 'bracoE'), (1, 'bracoD')):
        a = m.part(name, 'corpo', (side * 8, 47, 0))
        a.capsule((side * 8, 47, 0), (side * 14, 32, 3), 1.8, GOWN)
        a.capsule((side * 14, 32, 3), (side * 17, 24, 5), 1.6, GOWN)
        a.ellipsoid(side * 17, 22.5, 5.5, 2.4, 2.2, 2.2, WAXC)
        for f in (-1, 0, 1):
            a.capsule((side * 17 + f, 21, 5.5), (side * 17 + f * 1.5, 17, 6), 0.6, WAXD)
    # candelabro na mão esquerda
    cd = m.part('candelabro', 'bracoE', (-17, 22, 5.5))
    cd.box(-23, 24, 5, -11, 26, 7, BRASSC)
    cd.box(-18, 20, 5, -16, 26, 7, BRASSC)
    for dx in (-23, -18, -13):
        candle(cd, dx, 26, 5, 5, 4)
    # cabeça com véu (fase 1) e rosto de cera (fase 2)
    h = m.part('cabeca', 'corpo', (0, 50, 0))
    h.ellipsoid(0, 59, 0, 8.6, 10, 8, VEIL)
    h.frustum(0, 0, 49, 58, 11, 9.5, 9, 8, VEIL)
    for ex in (-3, 3):
        h.put(ex, 58, 7.8, '#ff7a2f', glow=True); h.put(ex, 57, 7.8, '#c8361a', glow=True)
    h2 = m.part('cabeca2', 'corpo', (0, 50, 0))
    h2.ellipsoid(0, 59, 0, 7.2, 8.4, 7, WAXC)
    for ex in (-3, 3):
        h2.ellipsoid(ex, 59, 6, 1.8, 2.2, 1.6, CHAR)
        for i in range(3):
            h2.put(ex, 58 + i, 7, FLAME[i + 1], glow=True)
    h2.box(-2, 53, 6, 3, 55, 7, CHAR)
    for k in range(5):
        h2.box(-5 + k * 2.5, 50, 5 - abs(k - 2), -4 + k * 2.5, 54, 6 - abs(k - 2), WAXD)
    for part in (h, h2):
        for i, (dx, hh) in enumerate(((-9, 5), (-5, 8), (-1, 11), (3, 8), (7, 5))):
            candle(part, dx, 66 - abs(dx + 1) // 3, -1, hh, 4)
    return m


if __name__ == '__main__':
    S = '/tmp/claude-0/-home-claude/18552d67-fdcd-5f48-be1b-c20c76891952/scratchpad/'
    for fn in (coveiro, cultista, viuva):
        mdl = fn()
        n = mdl.export(OUT)
        print(mdl.name, n, 'voxels de superfície')
        preview(mdl, S + f'vox_{mdl.name}.png', scale=10 if mdl.name != 'viuva' else 5)
    # armas extrudadas dos sprites
    import glob
    for png in glob.glob(os.path.join(ART, 'Weapons', '*.png')):
        name = os.path.splitext(os.path.basename(png))[0]
        import json as _j
        atlas = _j.load(open(os.path.join(ART, 'atlas.json'), encoding='utf-8'))
        w = next(w for w in atlas['weapons'] if w['name'] == name)
        extrude(png, 'arma_' + name, (w['gripX'], w['gripY']), 2).export(OUT)
    print('armas ok')
