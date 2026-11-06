"""Ferramentas de pixel art procedural.

Desenha-se com "materiais" (cada um tem uma rampa de 4 cores: escuro, base,
claro, brilho). Depois o render aplica sombreamento automático com luz vinda
de cima/esquerda e um contorno escuro, no estilo dos roguelikes pixelados.
"""
from PIL import Image
import math

OUTLINE = (18, 10, 20, 255)


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ramp(*hexes):
    return [hexc(h) for h in hexes]


def mix(c1, c2, t):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(3)) + (255,)


class Mat:
    """Canvas de materiais."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.m = [[None] * w for _ in range(h)]
        self.lit = [[None] * w for _ in range(h)]  # cores fixas (detalhes)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.m[y][x]
        return None

    def set(self, x, y, mat):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.m[y][x] = mat
            self.lit[y][x] = None

    def px(self, x, y, color):
        """Pixel de cor literal (olhos, brilhos), aplicado depois do sombreamento."""
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            if self.m[y][x] is None:
                self.m[y][x] = '_lit'
            self.lit[y][x] = hexc(color) if isinstance(color, str) else color

    def rect(self, x, y, w, h, mat):
        for j in range(int(h)):
            for i in range(int(w)):
                self.set(x + i, y + j, mat)

    def ellipse(self, cx, cy, rx, ry, mat):
        for y in range(int(math.floor(cy - ry - 1)), int(math.ceil(cy + ry + 1))):
            for x in range(int(math.floor(cx - rx - 1)), int(math.ceil(cx + rx + 1))):
                dx = (x + 0.5 - cx) / max(rx, 0.01)
                dy = (y + 0.5 - cy) / max(ry, 0.01)
                if dx * dx + dy * dy <= 1.0:
                    self.set(x, y, mat)

    def circle(self, cx, cy, r, mat):
        self.ellipse(cx, cy, r, r, mat)

    def poly(self, pts, mat):
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            for x in range(int(min(xs)), int(max(xs)) + 1):
                if point_in_poly(x + 0.5, y + 0.5, pts):
                    self.set(x, y, mat)

    def line(self, x0, y0, x1, y1, mat, lit=False):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            if lit:
                self.px(x0, y0, mat)
            else:
                self.set(x0, y0, mat)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def clear(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.m[y][x] = None
            self.lit[y][x] = None

    def render(self, mats, outline=OUTLINE, shade=True):
        img = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        p = img.load()
        for y in range(self.h):
            for x in range(self.w):
                m = self.m[y][x]
                if m is None:
                    continue
                if self.lit[y][x] is not None:
                    p[x, y] = self.lit[y][x]
                    continue
                r = mats[m]
                if not shade or len(r) == 1:
                    p[x, y] = r[min(1, len(r) - 1)]
                    continue
                up = self.get(x, y - 1) != m
                down = self.get(x, y + 1) != m
                left = self.get(x - 1, y) != m
                right = self.get(x + 1, y) != m
                if up and left:
                    i = 3
                elif up:
                    i = 2
                elif down:
                    i = 0
                elif right:
                    i = 0
                elif left:
                    i = 2
                else:
                    i = 1
                p[x, y] = r[i]
        if outline:
            add_outline(img, outline)
        return img


def point_in_poly(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-9) + xi):
            inside = not inside
        j = i
    return inside


def add_outline(img, color=OUTLINE):
    w, h = img.size
    p = img.load()
    add = []
    for y in range(h):
        for x in range(w):
            if p[x, y][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and p[nx, ny][3] > 0 and p[nx, ny] != color:
                    add.append((x, y))
                    break
    for x, y in add:
        p[x, y] = color
    return img


def flip(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def sheet(rows, fw, fh):
    """rows: lista de listas de imagens -> imagem da folha."""
    cols = max(len(r) for r in rows)
    out = Image.new('RGBA', (cols * fw, len(rows) * fh), (0, 0, 0, 0))
    for j, row in enumerate(rows):
        for i, im in enumerate(row):
            ox = i * fw + (fw - im.width) // 2
            oy = j * fh + (fh - im.height)
            out.alpha_composite(im, (ox, oy))
    return out


def rotate_img(img, ang_deg):
    """Rotação nearest-neighbor mantendo o tamanho."""
    return img.rotate(ang_deg, resample=Image.NEAREST, expand=False)
