"""A Viúva de Cera — chefe original.

Figura alta em vestido de luto, véu negro e uma coroa de velas; o corpo é de
cera que escorre. Na fase 2 o véu queima e revela o rosto derretido.
Quadro 64x72 (pés no fundo).
Animações: idle(4), windup(2), attack(2), idle2(4), windup2(2), attack2(2), death(8).
"""
import math
import random
from PIL import Image
from vol import VMat, Part, ramp5

FW, FH = 64, 72
CX = 32
G = 70  # linha do chão

PARTS = {
    'gown': Part(ramp5('#0a0610', '#1a1022', '#2a1c36', '#3e2c4c', '#584466'), 4.0, dither=True),
    'gownD': Part(ramp5('#040206', '#0a060e', '#120a16', '#1e1424', '#2e2034'), 2.0, dither=False),
    'lace': Part(ramp5('#1a1420', '#2e2636', '#463c50', '#625670', '#807490'), 1.0, dither=False),
    'veil': Part(ramp5('#06040a', '#0e0a14', '#1a1422', '#282034', '#3a3048'), 3.0, dither=True),
    'wax': Part(ramp5('#7a6040', '#b8986a', '#e2c896', '#f6e6c0', '#fffaf0'), 2.5, dither=False, gloss=0.15),
    'waxD': Part(ramp5('#5a4430', '#8a7050', '#b8986a', '#d8bc8c', '#ecd8b0'), 1.5, dither=False),
    'brass': Part(ramp5('#3a220a', '#6a4416', '#a06a24', '#d4a040', '#fff0a0'), 1.2, dither=False, gloss=0.25),
    'char': Part(ramp5('#0a0606', '#1a1010', '#2a1a16', '#3a2620', '#4a3228'), 2.0, dither=True),
}
FLAME = ['#fff6d0', '#ffe08a', '#ffb347', '#ff7a2f', '#c8361a']


def flame(m, x, y, h, f):
    """Chama de vela de altura h (pixels) com variação f."""
    x = int(x)
    for i in range(h):
        c = FLAME[min(len(FLAME) - 1, int(i / max(1, h - 1) * 3.5))]
        w = 1 if i < h - 2 else 0
        dx = ((f + i) % 3 == 0) * (1 if f % 2 else -1) if i > h // 2 else 0
        m.px(x + dx, y - i, c)
        if w and i < h // 2:
            m.px(x + dx + (1 if (f + i) % 2 else -1), y - i, FLAME[2])
    m.px(x, y, FLAME[0])


def candle(m, x, y, h, f, flame_h=4, lit=True):
    m.rect(x, y - h, 2, h, 'wax')
    m.px(x, y - h + 1, '#fffaf0')
    m.px(x + 1, y - h + 3, '#b8986a')
    m.px(x + (f % 2), y - 1, '#d8bc8c')
    if lit:
        flame(m, x, y - h - 1, flame_h, f)
    else:
        m.px(x, y - h - 1, '#2a1a16')


def figure(m, f=0, arms='down', unveiled=False, sink=0, sway=0, bright=False):
    s = sink  # quanto já derreteu (desce tudo)
    fl = 6 if bright else 4

    def Y(y):
        return y + s

    # vestido em sino
    m.poly([(CX - 8, Y(36)), (CX + 8, Y(36)), (CX + 22 + sway, G), (CX - 22 + sway, G)], 'gown')
    for x in range(CX - 22, CX + 23, 4):
        m.set(x + sway + (f % 2), G, 'gown')
    for fx in (-14, -6, 4, 12):
        m.line(CX + fx * 0.4, Y(40), CX + fx + sway * 0.6, G - 1, 'gownD')
    # cera escorrendo pelo vestido
    rnd = random.Random(7)
    for k in range(6):
        x = CX - 10 + k * 4 + rnd.randint(-1, 1)
        L = 6 + ((k * 5 + f) % 7)
        m.line(x, Y(37), x, Y(37) + L, 'waxD')
        m.set(x, Y(37) + L + 1, 'waxD')
    # tronco / espartilho
    m.poly([(CX - 7, Y(22)), (CX + 7, Y(22)), (CX + 6, Y(37)), (CX - 6, Y(37))], 'gown')
    m.rect(CX - 6, Y(33), 12, 2, 'lace')
    for yy in range(24, 33, 3):
        m.px(CX, Y(yy), '#625670')
    # gola de renda
    m.ellipse(CX, Y(22), 8.5, 2.6, 'lace')

    # braços longos
    if arms == 'down':
        L, R = (CX - 17, Y(44)), (CX + 17, Y(44))
    elif arms == 'up':
        L, R = (CX - 19, Y(12)), (CX + 19, Y(12))
    else:  # 'forward'
        L, R = (CX - 11, Y(48)), (CX + 11, Y(48))
    m.capsule(CX - 8, Y(24), L[0], L[1], 1.6, 'gown')
    m.capsule(CX + 8, Y(24), R[0], R[1], 1.6, 'gown')
    for (hx, hy) in (L, R):
        m.ellipse(hx, hy + 1, 2.2, 2.0, 'wax')
        for d in (-2, 0, 2):
            m.line(hx + d * 0.6, hy + 2, hx + d, hy + 5, 'waxD')
    # candelabro na mão esquerda
    cx, cy = L[0], L[1] - 1
    m.rect(cx - 5, cy - 1, 11, 2, 'brass')
    m.rect(cx, cy - 1, 1, 5, 'brass')
    for dx in (-5, 0, 5):
        candle(m, cx + dx, cy - 1, 4, f + dx, fl)

    # cabeça
    hy = Y(14)
    if not unveiled:
        m.ellipse(CX, hy, 8.5, 10, 'veil')
        m.poly([(CX - 9, hy), (CX + 9, hy), (CX + 11 + sway, Y(26)), (CX - 11 + sway, Y(26))], 'veil')
        # rosto escondido: só os olhos brilham por trás do véu
        for ex in (CX - 3, CX + 3):
            c = '#ffb347' if bright else '#ff7a2f'
            m.px(ex, hy, c); m.px(ex, hy + 1, '#c8361a')
            if bright:
                m.px(ex, hy - 1, '#ffe08a')
    else:
        m.ellipse(CX, hy + 1, 6.5, 7.5, 'wax')
        # rosto derretido: órbitas com chama, boca escorrendo
        for ex in (CX - 3, CX + 3):
            m.ellipse(ex, hy, 1.8, 2.2, 'char')
            flame(m, ex, hy + 1, 3 if not bright else 4, f + ex)
        m.rect(CX - 2, hy + 5, 5, 2, 'char')
        for k in range(4):
            m.line(CX - 5 + k * 3, hy + 6, CX - 5 + k * 3, hy + 9 + (k + f) % 3, 'waxD')
        # restos do véu queimado
        m.poly([(CX - 9, hy + 6), (CX - 6, hy + 4), (CX - 7, Y(26))], 'char')
        m.poly([(CX + 9, hy + 6), (CX + 6, hy + 4), (CX + 8, Y(26))], 'char')
    # coroa de velas
    if s < 14:
        for i, (dx, h) in enumerate(((-9, 5), (-5, 8), (-1, 11), (3, 8), (7, 5))):
            candle(m, CX + dx, hy - 7 + abs(dx + 1) // 3, h, f + i, fl + (2 if unveiled else 0))


def render(**k):
    m = VMat(FW, FH)
    figure(m, **k)
    return m.render(PARTS)


def frames():
    A = {}
    A['idle'] = [render(f=i, sway=[0, 1, 0, -1][i]) for i in range(4)]
    A['windup'] = [render(f=i, arms='up', bright=True) for i in range(2)]
    A['attack'] = [render(f=i, arms='forward', bright=True) for i in range(2)]
    A['idle2'] = [render(f=i, sway=[0, 1, 0, -1][i], unveiled=True) for i in range(4)]
    A['windup2'] = [render(f=i, arms='up', unveiled=True, bright=True) for i in range(2)]
    A['attack2'] = [render(f=i, arms='forward', unveiled=True, bright=True) for i in range(2)]
    # morte: derrete até virar uma poça de cera com o vestido
    d = []
    for k in range(8):
        m = VMat(FW, FH)
        sink = int(k * 5.5)
        figure(m, f=k, unveiled=True, sink=sink, arms='up' if k < 2 else 'down')
        # corta tudo que "afundou" abaixo do chão e cria a poça
        for y in range(G + 1, FH):
            for x in range(FW):
                m.clear(x, y)
        pr = 6 + k * 2.6
        m.ellipse(CX, G - 1, pr * 1.6, max(2, pr * 0.35), 'wax')
        if k >= 5:
            m.ellipse(CX + 2, G - 2, pr * 0.9, 2.4, 'gown')
            for j, dx in enumerate((-14, -4, 9)):
                m.rect(CX + dx, G - 3, 4, 2, 'wax')
                m.px(CX + dx + 4, G - 3, '#2a1a16')
                if k < 7 and j == k - 5:
                    flame(m, CX + dx + 4, G - 4, 3, k)
        d.append(m.render(PARTS))
    A['death'] = d
    return A


ORDER = [('idle', 6, True), ('windup', 8, True), ('attack', 10, True), ('idle2', 7, True), ('windup2', 8, True), ('attack2', 10, True), ('death', 7, False)]

if __name__ == '__main__':
    from vol import contact_sheet
    A = frames()
    contact_sheet([A[n] for n, _, _ in ORDER], scale=3).save('/tmp/claude-0/-home-claude/18552d67-fdcd-5f48-be1b-c20c76891952/scratchpad/widow.png')
    print('ok')
