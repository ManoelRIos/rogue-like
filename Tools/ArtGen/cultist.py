"""Cultista — inimigo original: capuz, máscara de porcelana rachada, vela na mão.

Quadro 24x28 (pés no fundo). A arma (pistola de pederneira) é um sprite à parte
que gira para mirar no jogador, como a arma do Coveiro.
Animações: idle(4), walk(4), aim(2), death(7).
"""
import math
from PIL import Image
from vol import VMat, Part, ramp5

FW, FH = 24, 28
CX = 12

PARTS = {
    'robe': Part(ramp5('#1c0608', '#330c10', '#4e1518', '#6c2420', '#8a3a2c'), 3.0, dither=False),
    'robeD': Part(ramp5('#12040a', '#220810', '#330c10', '#4e1518', '#6c2420'), 2.0, dither=False),
    'hood': Part(ramp5('#14050a', '#26090e', '#3c1014', '#561c1c', '#743028'), 3.5, dither=False),
    'mask': Part(ramp5('#6a6458', '#a8a090', '#d6cfbe', '#efeadc', '#ffffff'), 3.0, dither=False, gloss=0.15),
    'rope': Part(ramp5('#4a3a20', '#7a6438', '#a88c58', '#c8ae78', '#e0caa0'), 1.0, dither=False),
    'hand': Part(ramp5('#5a5048', '#8a8070', '#b0a690', '#cec4ac', '#e6dcc4'), 1.5, dither=False),
    'wax': Part(ramp5('#8a7a5a', '#c8b890', '#eee0b8', '#fff4d8', '#ffffff'), 1.0, dither=False),
}
EYE = '#050204'


def body(m, sway=0, squash=1.0, bob=0, aim=False, flicker=0):
    g = 25  # chão
    def Y(y):  # aplica achatamento a partir do chão
        return g - (g - y) * squash + bob
    # túnica em sino com barra rasgada
    m.poly([(CX - 5, Y(13)), (CX + 5, Y(13)), (CX + 9 + sway, Y(25)), (CX - 9 + sway, Y(25))], 'robe')
    for x in range(CX - 9, CX + 10, 3):
        m.set(x + sway, int(Y(25)), 'robe')
    # dobras escuras
    for fx in (CX - 4, CX + 1, CX + 5):
        m.line(fx, Y(18), fx + sway * 0.5 + (fx - CX) * 0.3, Y(24), 'robeD')
    # cordão
    m.rect(CX - 5, Y(18), 10, 1, 'rope')
    m.line(CX + 2, Y(18), CX + 3 + sway, Y(22), 'rope')
    # braço com a vela (lado esquerdo)
    m.ellipse(CX - 7, Y(17), 2.2, 3.2, 'robeD')
    m.ellipse(CX - 7.5, Y(19.5), 1.5, 1.4, 'hand')
    m.rect(CX - 8, Y(14.5), 2, 4, 'wax')
    # capuz com ponta
    m.ellipse(CX, Y(9.5), 7, 6.6, 'hood')
    m.poly([(CX - 3, Y(5)), (CX + 1 - sway * 0.5, Y(-0.5)), (CX + 3, Y(5))], 'hood')
    # máscara
    m.ellipse(CX, Y(10.5), 4.4, 4.8, 'mask')
    ey = int(Y(9.5))
    eye = '#ff2a3a' if aim else EYE
    for ex in (CX - 2, CX + 2):
        for yy in range(3):
            m.px(ex - 1, ey - 1 + yy, EYE); m.px(ex, ey - 1 + yy, EYE)
        if aim:
            m.px(ex - 1, ey, eye); m.px(ex, ey, '#ffd0d0')
    # rachadura e lágrima pintada
    for (x, y) in ((CX + 1, ey - 3), (CX + 2, ey - 2), (CX + 2, ey - 1), (CX + 3, ey + 2), (CX + 3, ey + 3)):
        m.px(x, y, '#4a4438')
    m.px(CX - 2, ey + 2, '#a01a24'); m.px(CX - 2, ey + 3, '#a01a24'); m.px(CX - 2, ey + 4, '#701018')
    m.px(CX - 1, ey + 4, '#3a3428'); m.px(CX, ey + 4, '#3a3428'); m.px(CX + 1, ey + 4, '#3a3428')
    # chama da vela
    fy = int(Y(13)) - 1
    m.px(CX - 7, fy - 1 - flicker, '#ffb347'); m.px(CX - 7, fy, '#ffe9a0'); m.px(CX - 8 + flicker, fy - 2, '#ff7a2f')
    m.px(CX - 7, fy + 1, '#fff6d0')


def render(**k):
    m = VMat(FW, FH)
    body(m, **k)
    return m.render(PARTS)


def frames():
    A = {}
    A['idle'] = [render(sway=s, bob=b, flicker=f) for s, b, f in ((0, 0, 0), (0, 0, 1), (1, 1, 0), (0, 1, 1))]
    A['walk'] = [render(sway=s, bob=b, flicker=i % 2) for i, (s, b) in enumerate(((-1, 0), (0, -1), (1, 0), (0, -1)))]
    A['aim'] = [render(sway=0, bob=1, aim=True, squash=0.95), render(sway=0, bob=0, aim=True, flicker=1)]
    # morte: desaba, a túnica vira um monte e a máscara cai rolando
    d = [render(squash=0.95, bob=0)]
    d.append(render(squash=0.82, sway=1))
    d.append(render(squash=0.62, sway=2))
    for k in range(4):
        m = VMat(FW, FH)
        m.ellipse(CX, 24, 9.5, 3.4 - k * 0.2, 'robe')
        m.ellipse(CX - 2, 22.5, 5, 2.2, 'hood')
        m.line(CX - 6, 24, CX + 6, 23.5, 'robeD')
        mx = [CX - 1, CX + 4, CX + 7, CX + 8][k]
        my = [21, 22, 23, 23][k]
        m.ellipse(mx, my, 3.2, 3.6 if k < 2 else 2.4, 'mask')
        if k >= 2:
            m.px(mx - 1, my, EYE); m.px(mx + 1, my, EYE)
            m.px(mx, my - 1, '#4a4438'); m.px(mx + 1, my + 1, '#4a4438')
        # vela apagada e fumaça
        m.rect(CX - 9, 23, 3, 1, 'wax')
        if k >= 1:
            for s in range(k):
                m.px(CX - 8 + (s % 2), 21 - s * 2, '#6a6070')
        d.append(m.render(PARTS))
    A['death'] = d
    return A


def flintlock():
    """Pistola de pederneira na mão enfaixada (empunhadura em 4,5)."""
    P = dict(PARTS)
    P['iron'] = Part(ramp5('#1a1a22', '#2e2e3a', '#4a4a5a', '#6e6e82', '#9a9ab0'), 1.0, dither=False, gloss=0.2)
    P['wood'] = Part(ramp5('#24120a', '#3e2212', '#5c341c', '#7a4a28', '#9a6638'), 1.0, dither=False)
    P['brass'] = Part(ramp5('#4a2c0c', '#7a4e18', '#b07a28', '#e0b048', '#fff0a0'), 1.0, dither=False)
    m = VMat(18, 10)
    m.poly([(1, 7), (4, 3), (8, 3), (6, 8), (2, 9)], 'wood')
    m.rect(5, 2, 12, 2, 'iron')
    m.rect(6, 4, 3, 1, 'brass')
    m.rect(15, 1, 2, 1, 'iron')
    m.ellipse(4, 5, 2, 1.8, 'hand')
    return m.render(P), 4, 5


ORDER = [('idle', 6, True), ('walk', 8, True), ('aim', 10, True), ('death', 10, False)]

if __name__ == '__main__':
    from vol import contact_sheet
    A = frames()
    g, _, _ = flintlock()
    contact_sheet([A[n] for n, _, _ in ORDER] + [[g]], scale=6).save('/tmp/claude-0/-home-claude/18552d67-fdcd-5f48-be1b-c20c76891952/scratchpad/cultist.png')
    print('ok')
