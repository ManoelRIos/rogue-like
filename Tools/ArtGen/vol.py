"""Pixel art com sombreamento volumétrico.

Cada parte do sprite é desenhada como uma região de "material". No render,
cada região vira um volume arredondado: calcula-se a distância até a borda,
transforma em altura, tira a normal e ilumina com luz vinda de cima/esquerda.
O resultado é quantizado numa rampa de 5 tons com pontilhado ordenado e
recebe um contorno escuro grosso — o visual "gordinho" e bem iluminado dos
roguelikes de pixel art modernos, sem copiar nenhum personagem.
"""
import math
from collections import deque
from PIL import Image

INK = (13, 7, 11, 255)          # contorno externo
BAYER = [[0, 2], [3, 1]]


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ramp5(*hexes):
    return [hexc(h) for h in hexes]


class Part:
    """Configuração de um material."""

    def __init__(self, ramp, round_=3.0, dither=True, flat=False, inner=True, gloss=0.0):
        self.ramp = ramp        # 5 cores do escuro ao claro
        self.round = round_     # quanto "infla" (raio do perfil)
        self.dither = dither
        self.flat = flat        # sem volume (usa o tom 2)
        self.inner = inner      # linha escura onde encosta em outra parte
        self.gloss = gloss      # brilho especular extra


class VMat:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.m = [[None] * w for _ in range(h)]
        self.lit = {}

    # ------------------------------------------------------------ desenho
    def set(self, x, y, mat):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.m[y][x] = mat
            self.lit.pop((x, y), None)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.m[y][x]
        return None

    def clear(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.m[y][x] = None
            self.lit.pop((x, y), None)

    def px(self, x, y, color):
        """Pixel de cor fixa por cima de tudo (olhos, brilhos, detalhes)."""
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            if self.m[y][x] is None:
                self.m[y][x] = '_'
            self.lit[(x, y)] = hexc(color) if isinstance(color, str) else color

    def rect(self, x, y, w, h, mat):
        for j in range(int(round(h))):
            for i in range(int(round(w))):
                self.set(x + i, y + j, mat)

    def ellipse(self, cx, cy, rx, ry, mat, erase=False):
        for y in range(int(math.floor(cy - ry - 1)), int(math.ceil(cy + ry + 1))):
            for x in range(int(math.floor(cx - rx - 1)), int(math.ceil(cx + rx + 1))):
                dx = (x + 0.5 - cx) / max(rx, 0.01)
                dy = (y + 0.5 - cy) / max(ry, 0.01)
                if dx * dx + dy * dy <= 1.0:
                    if erase:
                        self.clear(x, y)
                    else:
                        self.set(x, y, mat)

    def poly(self, pts, mat, erase=False):
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
            for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
                if _pip(x + 0.5, y + 0.5, pts):
                    if erase:
                        self.clear(x, y)
                    else:
                        self.set(x, y, mat)

    def line(self, x0, y0, x1, y1, mat, width=1, color=None):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for wy in range(width):
                for wx in range(width):
                    if color:
                        self.px(x + wx - width // 2, y + wy - width // 2, color)
                    else:
                        self.set(x + wx - width // 2, y + wy - width // 2, mat)

    def capsule(self, x0, y0, x1, y1, r, mat):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            self.ellipse(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r, r, mat)

    # ------------------------------------------------------------ render
    def render(self, parts, light=(-0.55, -0.85, 0.75), outline=INK, rim=None, ambient=0.18):
        W, H = self.w, self.h
        lx, ly, lz = light
        ll = math.sqrt(lx * lx + ly * ly + lz * lz)
        lx, ly, lz = lx / ll, ly / ll, lz / ll

        # distância até a borda da própria região
        dist = [[0] * W for _ in range(H)]
        q = deque()
        for y in range(H):
            for x in range(W):
                m = self.m[y][x]
                if m is None:
                    continue
                edge = False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if self.get(x + dx, y + dy) != m:
                        edge = True
                        break
                if edge:
                    dist[y][x] = 1
                    q.append((x, y))
        while q:
            x, y = q.popleft()
            m = self.m[y][x]
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < W and 0 <= ny < H and self.m[ny][nx] == m and dist[ny][nx] == 0:
                    dist[ny][nx] = dist[y][x] + 1
                    q.append((nx, ny))

        def height(x, y, m):
            if self.get(x, y) != m:
                return 0.0
            p = parts.get(m)
            R = p.round if p else 3
            d = min(dist[y][x], R) / R
            return math.sqrt(max(0.0, 1 - (1 - d) ** 2))

        img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        P = img.load()
        for y in range(H):
            for x in range(W):
                m = self.m[y][x]
                if m is None:
                    continue
                if (x, y) in self.lit:
                    P[x, y] = self.lit[(x, y)]
                    continue
                part = parts.get(m)
                if part is None:
                    continue
                r = part.ramp
                if part.flat:
                    P[x, y] = r[2]
                    continue
                hx = height(x - 1, y, m) - height(x + 1, y, m)
                hy = height(x, y - 1, m) - height(x, y + 1, m)
                nx, ny, nz = hx * 1.6, hy * 1.6, 1.0
                nl = math.sqrt(nx * nx + ny * ny + nz * nz)
                nx, ny, nz = nx / nl, ny / nl, nz / nl
                diff = max(0.0, nx * lx + ny * ly + nz * lz)
                inten = ambient + (1 - ambient) * diff
                inten += (0.5 - y / H) * 0.12
                if part.gloss and diff > 0.93:
                    inten += part.gloss
                # linha interna onde encosta em outra parte (por baixo/direita escurece)
                if part.inner:
                    below = self.get(x, y + 1)
                    if below is not None and below != m and below != '_':
                        inten -= 0.22
                v = max(0.0, min(0.999, inten)) * (len(r) - 0.001)
                if part.dither:
                    v += (BAYER[y % 2][x % 2] / 4.0 - 0.375) * 0.9
                i = int(max(0, min(len(r) - 1, v)))
                P[x, y] = r[i]
        if rim:
            for y in range(H):
                for x in range(W):
                    if P[x, y][3] and (x + 1 >= W or P[x + 1, y][3] == 0) and self.m[y][x] not in (None, '_'):
                        c = P[x, y]
                        P[x, y] = tuple(int(c[k] * 0.6 + rim[k] * 0.4) for k in range(3)) + (255,)
        if outline:
            add_outline(img, outline)
        return img


def _pip(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-9) + xi):
            inside = not inside
        j = i
    return inside


def add_outline(img, color=INK, diagonal=False):
    w, h = img.size
    P = img.load()
    add = []
    nb = ((1, 0), (-1, 0), (0, 1), (0, -1)) + (((1, 1), (-1, -1), (1, -1), (-1, 1)) if diagonal else ())
    for y in range(h):
        for x in range(w):
            if P[x, y][3] > 0:
                continue
            for dx, dy in nb:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and P[nx, ny][3] > 0 and P[nx, ny] != color:
                    add.append((x, y))
                    break
    for x, y in add:
        P[x, y] = color
    return img


def contact_sheet(frames_rows, scale=4, bg=(34, 24, 30, 255), pad=4):
    fw = max(f.width for row in frames_rows for f in row)
    fh = max(f.height for row in frames_rows for f in row)
    cols = max(len(r) for r in frames_rows)
    out = Image.new('RGBA', (cols * (fw + pad) + pad, len(frames_rows) * (fh + pad) + pad), bg)
    for j, row in enumerate(frames_rows):
        for i, f in enumerate(row):
            out.alpha_composite(f, (pad + i * (fw + pad) + (fw - f.width) // 2, pad + j * (fh + pad) + (fh - f.height)))
    return out.resize((out.width * scale, out.height * scale), Image.NEAREST)
