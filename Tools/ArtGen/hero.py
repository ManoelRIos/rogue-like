"""O Coveiro — personagem original do jogador (chibi, chapéu largo, sobretudo, lenço).

Quadro 24x30, pés no fundo. Animações em 3 direções (down, side, up):
idle(4), run(6), dodge(6: agacha, impulso, ar, ar, pouso, recupera), hurt(1), death(6).
"""
import math
from PIL import Image
from vol import VMat, Part, ramp5, hexc

FW, FH = 30, 30
CX = 15

PARTS = {
    'hat': Part(ramp5('#120c12', '#22161e', '#36242c', '#4c3438', '#664a46'), 2.5, dither=False),
    'band': Part(ramp5('#3a0a12', '#5e121c', '#8a1e26', '#b8323a', '#d65a50'), 1.2),
    'skin': Part(ramp5('#6e3e3a', '#a8645a', '#d49478', '#ecbc98', '#fbdcc0'), 3.5),
    'hair': Part(ramp5('#0e080c', '#1e1218', '#2e1e22', '#40282c', '#523632'), 1.5),
    'coat': Part(ramp5('#0e1418', '#1a262a', '#283a3c', '#3a5250', '#526e66'), 3.0, dither=False),
    'coatD': Part(ramp5('#0a0e12', '#121c20', '#1c2a2e', '#283a3c', '#3a5250'), 2.0),
    'lapel': Part(ramp5('#1a262a', '#283a3c', '#3a5250', '#526e66', '#6e8c80'), 1.5),
    'scarf': Part(ramp5('#3a060e', '#6a0e18', '#9a1a22', '#c42e30', '#e2584a'), 2.0),
    'pants': Part(ramp5('#0c0a12', '#16121e', '#221c2c', '#302840', '#40364e'), 1.5),
    'boots': Part(ramp5('#100a08', '#24160e', '#3a2416', '#523420', '#6a4630'), 2.0),
    'belt': Part(ramp5('#160c08', '#2a1810', '#422618', '#5a3620', '#70482c'), 1.0, dither=False),
    'brass': Part(ramp5('#4a2c0c', '#7a4e18', '#b07a28', '#e0b048', '#fff0a0'), 1.2, dither=False, gloss=0.2),
    'bone': Part(ramp5('#6a6050', '#9a9078', '#c8bea0', '#e8e0c8', '#fffaf0'), 1.0, dither=False),
}
EYE = '#0a0608'
GLINT = '#fff6e0'


def _boots(m, lx, ly, rx, ry, spread=0):
    m.ellipse(CX - 3 + lx - spread, 28.2 + ly, 2.4, 1.5, 'boots')
    m.ellipse(CX + 3 + rx + spread, 28.2 + ry, 2.4, 1.5, 'boots')


def _legs(m, lx, ly, rx, ry, spread=0, bend=0):
    m.rect(CX - 4 + lx - spread, 25 + ly - bend, 3, 3 + bend, 'pants')
    m.rect(CX + 1 + rx + spread, 25 + ry - bend, 3, 3 + bend, 'pants')


def _coat_front(m, b, flare=0, back=False):
    top, bot = 17 + b, 26.5 + b
    mat = 'coatD' if back else 'coat'
    m.poly([(CX - 5, top), (CX + 5, top), (CX + 7 + flare, bot), (CX - 7 - flare, bot)], mat)
    # abertura central do sobretudo
    if not back:
        m.poly([(CX - 0.5, bot - 4), (CX + 0.5, bot - 4), (CX + 1.5, bot), (CX - 1.5, bot)], 'pants')
        # lapelas
        m.poly([(CX - 4, top), (CX - 1, top), (CX - 1, top + 5), (CX - 3, top + 3)], 'lapel')
        m.poly([(CX + 1, top), (CX + 4, top), (CX + 3, top + 3), (CX + 1, top + 5)], 'lapel')
        # cinto e fivela
        m.rect(CX - 6, 22 + b, 12, 2, 'belt')
        m.rect(CX - 1, 22 + b, 2, 2, 'brass')
        # cartucheira na diagonal
        for i in range(5):
            m.rect(CX - 5 + i * 2, 17 + b + i, 1, 2, 'brass')
    else:
        m.rect(CX - 6, 22 + b, 12, 2, 'belt')
        m.line(CX, top + 1, CX, bot - 1, 'coat')
    # mangas
    m.ellipse(CX - 6.2, 20 + b, 1.8, 3.0, mat)
    m.ellipse(CX + 6.2, 20 + b, 1.8, 3.0, mat)


def _head_front(m, b, hb, blink=False, back=False, hurt=False):
    hy = b + hb
    if back:
        m.ellipse(CX, 12 + hy, 5.6, 5.0, 'hair')
        m.rect(CX - 2, 16 + hy, 4, 2, 'scarf')
    else:
        m.ellipse(CX, 12 + hy, 5.6, 5.0, 'skin')
        m.ellipse(CX - 5.2, 11 + hy, 1.4, 2.6, 'hair'); m.ellipse(CX + 5.2, 11 + hy, 1.4, 2.6, 'hair')
        # lenço cobrindo só a boca e o queixo
        m.poly([(CX - 5.6, 14.5 + hy), (CX + 5.6, 14.5 + hy), (CX + 4.5, 17 + hy), (CX, 18.5 + hy), (CX - 4.5, 17 + hy)], 'scarf')
    # chapéu: aba larga + copa com amassado
    m.ellipse(CX, 6.6 + hy, 10.4, 2.2, 'hat')
    m.poly([(CX - 4.8, 7 + hy), (CX - 4.3, 0.5 + hy), (CX - 1, 1.6 + hy), (CX + 1, 0.6 + hy), (CX + 4.3, 0.5 + hy), (CX + 4.8, 7 + hy)], 'hat')
    m.rect(CX - 5, 4 + hy, 10, 2, 'band')
    m.rect(CX + 3, 2 + hy, 1, 3, 'bone'); m.px(CX + 3, 1 + hy, '#e8e0c8')
    if not back:
        ey = int(10 + hy)
        for ex in (CX - 3, CX + 2):
            if hurt:
                m.px(ex - 1, ey, EYE); m.px(ex + 1, ey + 2, EYE); m.px(ex, ey + 1, EYE); m.px(ex + 1, ey, EYE); m.px(ex - 1, ey + 2, EYE)
            elif blink:
                m.px(ex - 1, ey + 2, '#6a2e28'); m.px(ex, ey + 2, '#6a2e28'); m.px(ex + 1, ey + 2, '#6a2e28')
            else:
                for yy in range(3):
                    m.px(ex, ey + yy, EYE); m.px(ex + 1, ey + yy, EYE)
                m.px(ex, ey, GLINT)
                m.px(ex + 1, ey + 2, '#3a2030')
        # sombra da aba sobre a testa
        for x in range(CX - 5, CX + 6):
            if m.get(x, int(8 + hy)) == 'skin':
                m.px(x, int(8 + hy), '#8a4e44')
        # olheiras (cansaço / terror)
        m.px(CX - 4, ey + 3, '#b07060'); m.px(CX + 4, ey + 3, '#b07060')


def _side(m, b, hb, fl, bl, flare=0, blink=False, hurt=False, tail=0):
    m.rect(CX - 2 + bl[0], 25 + bl[1], 3, 3, 'pants')
    m.ellipse(CX - 0.5 + bl[0], 28.2 + bl[1], 2.4, 1.4, 'boots')
    m.rect(CX + 0 + fl[0], 25 + fl[1], 3, 3, 'pants')
    m.ellipse(CX + 1.8 + fl[0], 28.2 + fl[1], 2.6, 1.5, 'boots')
    top, bot = 17 + b, 26.5 + b
    m.poly([(CX - 4, top), (CX + 4, top), (CX + 5, bot), (CX - 6 - flare - tail, bot - tail * 0.5)], 'coat')
    m.rect(CX - 5, 22 + b, 10, 2, 'belt')
    m.rect(CX + 2, 22 + b, 2, 2, 'brass')
    m.ellipse(CX + 0.5, 20 + b, 2, 3.0, 'coatD')
    hy = b + hb
    m.ellipse(CX + 1.2, 12 + hy, 5.0, 5.0, 'skin')
    m.ellipse(CX - 3, 11.5 + hy, 2.6, 3.4, 'hair')
    m.poly([(CX - 3, 14.5 + hy), (CX + 6.4, 14.5 + hy), (CX + 5, 17 + hy), (CX - 2, 18 + hy)], 'scarf')
    m.set(CX - 4, 15 + hy + tail % 2, 'scarf'); m.set(CX - 5, 16 + hy + tail % 2, 'scarf'); m.set(CX - 6, 16 + hy, 'scarf')
    m.ellipse(CX + 1, 6.6 + hy, 10, 2.2, 'hat')
    m.poly([(CX - 4.5, 7 + hy), (CX - 4, 0.5 + hy), (CX + 1, 1.5 + hy), (CX + 4, 0.8 + hy), (CX + 4.5, 7 + hy)], 'hat')
    m.rect(CX - 4.5, 4 + hy, 9, 2, 'band')
    m.rect(CX - 3, 2 + hy, 1, 3, 'bone')
    ey = int(10 + hy)
    ex = CX + 3
    if hurt:
        m.px(ex - 1, ey, EYE); m.px(ex, ey + 1, EYE); m.px(ex + 1, ey + 2, EYE); m.px(ex + 1, ey, EYE); m.px(ex - 1, ey + 2, EYE)
    elif blink:
        m.px(ex, ey + 2, '#6a2e28'); m.px(ex + 1, ey + 2, '#6a2e28')
    else:
        for yy in range(3):
            m.px(ex, ey + yy, EYE); m.px(ex + 1, ey + yy, EYE)
        m.px(ex, ey, GLINT)
    for x in range(CX - 2, CX + 7):
        if m.get(x, int(8 + hy)) == 'skin':
            m.px(x, int(8 + hy), '#8a4e44')
    m.px(CX + 5, ey + 3, '#b07060')


def _frame(fn, *a, **k):
    m = VMat(FW, FH)
    fn(m, *a, **k)
    return m.render(PARTS)


def front(m, b=0, hb=0, l=(0, 0), r=(0, 0), flare=0, blink=False, back=False, hurt=False, spread=0, bend=0):
    _legs(m, l[0], l[1], r[0], r[1], spread, bend)
    _boots(m, l[0], l[1], r[0], r[1], spread)
    _coat_front(m, b, flare, back)
    _head_front(m, b, hb, blink, back, hurt)


def frames():
    A = {}
    idle_b = [0, 0, 1, 1]
    idle_hb = [0, 0, 0, 1]
    A['idle_down'] = [_frame(front, idle_b[i], idle_hb[i] - idle_b[i] + (1 if i == 3 else 0) * 0, blink=(i == 2)) for i in range(4)]
    A['idle_up'] = [_frame(front, idle_b[i], 0, back=True) for i in range(4)]
    A['idle_side'] = [_frame(_side, idle_b[i], 0, (0, 0), (-1, 0), blink=(i == 2)) for i in range(4)]

    rb = [0, -1, 0, 0, -1, 0]
    rl = [(0, 0), (0, -2), (0, -1), (0, 0), (0, 0), (0, 0)]
    rr = [(0, 0), (0, 0), (0, 0), (0, 0), (0, -2), (0, -1)]
    A['run_down'] = [_frame(front, rb[i], 0, rl[i], rr[i], flare=(i % 3 == 1)) for i in range(6)]
    A['run_up'] = [_frame(front, rb[i], 0, rr[i], rl[i], flare=(i % 3 == 1), back=True) for i in range(6)]
    sf = [(2, 0), (3, -1), (1, -2), (-2, 0), (-3, 0), (-1, 0)]
    sb = [(-2, 0), (-3, 0), (-1, 0), (2, 0), (3, -1), (1, -2)]
    A['run_side'] = [_frame(_side, rb[i], 0, sf[i], sb[i], flare=1, tail=i % 3) for i in range(6)]

    # salto de esquiva: agacha, impulso, ar, ar, pouso, recupera (altura vem do código)
    def dodge(kind):
        out = []
        poses = [
            dict(b=2, hb=0, l=(0, 0), r=(0, 0), spread=1, bend=0, flare=1),      # agacha
            dict(b=-1, hb=-1, l=(0, 1), r=(0, 1), spread=0, bend=0, flare=0),    # impulso (estica)
            dict(b=-1, hb=-1, l=(1, -3), r=(-1, -3), spread=0, bend=0, flare=2), # ar: joelhos dobrados
            dict(b=-1, hb=-2, l=(1, -4), r=(-1, -4), spread=0, bend=0, flare=3), # ar alto
            dict(b=2, hb=1, l=(0, 0), r=(0, 0), spread=2, bend=0, flare=1),      # pouso
            dict(b=1, hb=0, l=(0, 0), r=(0, 0), spread=1, bend=0, flare=0),      # recupera
        ]
        for i, p in enumerate(poses):
            if kind == 'side':
                legs = [((0, 0), (-1, 0)), ((3, 1), (-3, 1)), ((2, -3), (-1, -2)), ((3, -4), (0, -3)), ((1, 0), (-2, 0)), ((0, 0), (-1, 0))][i]
                out.append(_frame(_side, p['b'], p['hb'] + p['b'] * 0, legs[0], legs[1], flare=p['flare'], tail=2 if i in (2, 3) else 0))
            else:
                out.append(_frame(front, p['b'], p['hb'], p['l'], p['r'], p['flare'], back=(kind == 'up'), spread=p['spread']))
        return out
    A['dodge_down'] = dodge('down')
    A['dodge_side'] = dodge('side')
    A['dodge_up'] = dodge('up')
    A['hurt'] = [_frame(front, 1, 1, hurt=True)]

    # morte: cambaleia, cai para trás, chapéu rola para o lado
    base = _frame(front, 1, 0, hurt=True)

    def lay(img, ang, dx=0):
        r = img.rotate(ang, resample=Image.NEAREST, expand=True)
        bb = r.getbbox()
        r = r.crop(bb)
        out = Image.new('RGBA', (FW, FH))
        out.alpha_composite(r, (max(0, (FW - r.width) // 2 + dx), FH - r.height))
        return out
    d = [base, lay(base, 15, 1), lay(base, 55, 2)]
    # deitado (desenhado à parte) + chapéu caindo + poça escura crescendo
    PARTS['blood'] = Part(ramp5('#2a0408', '#4a0810', '#6a0e16', '#88161c', '#a02424'), 2.0, dither=False)
    for k in range(3):
        m = VMat(FW, FH)
        pr = [3.5, 6.0, 8.0][k]
        m.ellipse(13, 27, pr * 1.9, pr * 0.7, 'blood')
        m.poly([(9, 22), (22, 21.5), (23, 27.5), (9, 28)], 'coat')
        m.rect(13, 23, 1, 4, 'belt'); m.rect(13, 24, 1, 2, 'brass')
        m.ellipse(24.5, 23.2, 1.6, 1.5, 'boots'); m.ellipse(24.5, 26.6, 1.6, 1.5, 'boots')
        m.rect(22, 22, 2, 2, 'pants'); m.rect(22, 25.5, 2, 2, 'pants')
        m.ellipse(6.5, 24.5, 4.4, 4.0, 'skin')
        m.ellipse(4, 24.5, 1.8, 3.6, 'hair')
        m.poly([(9, 21.5), (11.5, 21.5), (11.5, 28), (9, 28)], 'scarf')
        for (x, y) in ((6, 22), (8, 24), (7, 23), (8, 22), (6, 24)):
            m.px(x, y, EYE)
        for (x, y) in ((6, 26), (8, 28), (7, 27), (8, 26), (6, 28)):
            m.px(x, y, EYE)
        im = m.render(PARTS)
        hat = VMat(16, 9)
        hat.ellipse(8, 6, 7.2, 1.9, 'hat'); hat.poly([(4, 6.5), (4.5, 1), (11.5, 1), (12, 6.5)], 'hat'); hat.rect(4, 4, 8, 1, 'band')
        h2 = hat.render(PARTS).rotate([35, 15, 0][k], resample=Image.NEAREST, expand=True)
        im.alpha_composite(h2, ([4, 1, 0][k], [6, 11, 14][k]))
        d.append(im)
    A['death'] = d
    return A


ORDER = [('idle_down', 6, True), ('idle_side', 6, True), ('idle_up', 6, True),
         ('run_down', 12, True), ('run_side', 12, True), ('run_up', 12, True),
         ('dodge_down', 14, False), ('dodge_side', 14, False), ('dodge_up', 14, False),
         ('hurt', 1, False), ('death', 8, False)]


if __name__ == '__main__':
    import os
    from vol import contact_sheet
    A = frames()
    sheet = contact_sheet([A[n] for n, _, _ in ORDER], scale=5)
    sheet.save('/tmp/claude-0/-home-claude/18552d67-fdcd-5f48-be1b-c20c76891952/scratchpad/hero.png')
    print('ok')
