"""Modelos 3D em voxel dos personagens originais (para as câmeras 3D e primeira pessoa).

1 voxel = 1/16 de tile (o mesmo pixel dos sprites), então o modelo tem o mesmo
tamanho e as mesmas cores da pixel art. Cada modelo é dividido em PARTES com
pivô (quadril, ombro, pescoço...) que o Unity anima por código.

Eixos: x = direita, y = cima, z = frente (o personagem olha para +z).
Saída: Assets/Resources/Voxels/<nome>.json
  {"palette": ["#rrggbb", ...], "parts": [{"name", "parent", "pivot": [x,y,z],
   "vox": [x,y,z,cor, ...], "glow": [x,y,z,cor, ...]}]}
Só os voxels de superfície são exportados (o interior é oco).
"""
import json
import math
import os
from PIL import Image

EMPTY = None


class Part:
    def __init__(self, name, parent=None, pivot=(0, 0, 0)):
        self.name, self.parent, self.pivot = name, parent, pivot
        self.v = {}       # (x,y,z) -> '#rrggbb'
        self.glow = set()  # voxels emissivos (chamas, olhos brilhando)

    def put(self, x, y, z, c, glow=False):
        k = (int(math.floor(x)), int(math.floor(y)), int(math.floor(z)))
        if c is None:
            self.v.pop(k, None); self.glow.discard(k); return
        self.v[k] = c
        if glow:
            self.glow.add(k)
        else:
            self.glow.discard(k)

    def box(self, x0, y0, z0, x1, y1, z1, c, glow=False):
        for x in range(int(x0), int(x1)):
            for y in range(int(y0), int(y1)):
                for z in range(int(z0), int(z1)):
                    self.put(x, y, z, c, glow)

    def ellipsoid(self, cx, cy, cz, rx, ry, rz, c, cond=None, glow=False):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            for y in range(int(cy - ry - 1), int(cy + ry + 2)):
                for z in range(int(cz - rz - 1), int(cz + rz + 2)):
                    dx, dy, dz = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry, (z + 0.5 - cz) / rz
                    if dx * dx + dy * dy + dz * dz <= 1 and (cond is None or cond(x, y, z)):
                        self.put(x, y, z, c, glow)

    def frustum(self, cx, cz, y0, y1, rx0, rz0, rx1, rz1, c, cond=None):
        """Tronco de cone elíptico (saias, túnicas, casacos)."""
        for y in range(int(y0), int(y1)):
            t = (y - y0) / max(1, (y1 - y0))
            rx, rz = rx0 + (rx1 - rx0) * t, rz0 + (rz1 - rz0) * t
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                for z in range(int(cz - rz - 1), int(cz + rz + 2)):
                    dx, dz = (x + 0.5 - cx) / rx, (z + 0.5 - cz) / rz
                    if dx * dx + dz * dz <= 1 and (cond is None or cond(x, y, z)):
                        self.put(x, y, z, c)

    def capsule(self, a, b, r, c):
        n = int(max(abs(b[i] - a[i]) for i in range(3)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            p = [a[k] + (b[k] - a[k]) * t for k in range(3)]
            self.ellipsoid(p[0], p[1], p[2], r, r, r, c)

    def surface(self):
        out = {}
        for k, c in self.v.items():
            x, y, z = k
            if any((x + dx, y + dy, z + dz) not in self.v for dx, dy, dz in
                   ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1))):
                out[k] = c
        return out


class Model:
    def __init__(self, name):
        self.name = name
        self.parts = []

    def part(self, name, parent=None, pivot=(0, 0, 0)):
        p = Part(name, parent, pivot)
        self.parts.append(p)
        return p

    def export(self, root):
        pal, idx = [], {}

        def ci(c):
            if c not in idx:
                idx[c] = len(pal); pal.append(c)
            return idx[c]
        parts = []
        for p in self.parts:
            surf = p.surface()
            vox, glow = [], []
            for (x, y, z), c in surf.items():
                (glow if (x, y, z) in p.glow else vox).extend([x, y, z, ci(c)])
            parts.append({'name': p.name, 'parent': p.parent or '', 'pivot': list(p.pivot), 'vox': vox, 'glow': glow})
        os.makedirs(root, exist_ok=True)
        with open(os.path.join(root, self.name + '.json'), 'w') as f:
            json.dump({'palette': pal, 'parts': parts}, f, separators=(',', ':'))
        return sum(len(p['vox']) + len(p['glow']) for p in parts) // 4


def extrude(png_path, name, grip, depth=2):
    """Arma a partir do sprite: cada pixel vira uma coluna de voxels (arma 'chapada', como réplicas de pixel art)."""
    im = Image.open(png_path).convert('RGBA')
    m = Model(name)
    p = m.part('arma', None, (grip[0], im.height - 1 - grip[1], 0))
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a < 128:
                continue
            c = '#%02x%02x%02x' % (r, g, b)
            for z in range(-(depth // 2), depth - depth // 2):
                p.put(x, im.height - 1 - y, z, c)
    return m


# ---------------------------------------------------------------------------- render de prévia
def preview(model, path, scale=6, yaw=35, pose=None):
    """Projeção isométrica simples com sombreamento por face (só para conferir o modelo)."""
    pose = pose or {}
    pts = []
    by = {p.name: p for p in model.parts}

    def world(p, x, y, z):
        off = pose.get(p.name, (0, 0, 0))
        return x + off[0], y + off[1], z + off[2]
    for p in model.parts:
        for (x, y, z), c in p.v.items():
            wx, wy, wz = world(p, x, y, z)
            pts.append((wx, wy, wz, c, (x, y, z) in p.glow, p))
    a = math.radians(yaw)
    ca, sa = math.cos(a), math.sin(a)

    def proj(x, y, z):
        rx = x * ca + z * sa
        rz = -x * sa + z * ca
        return rx, -y * 0.9 + rz * 0.45, rz
    occupied = {(int(x), int(y), int(z)) for x, y, z, *_ in pts}
    items = []
    for x, y, z, c, g, p in pts:
        sx, sy, d = proj(x + 0.5, y + 0.5, z + 0.5)
        items.append((d - y * 0.001, sx, sy, c, g, (int(x), int(y), int(z))))
    items.sort()
    xs = [i[1] for i in items]; ys = [i[2] for i in items]
    W = int((max(xs) - min(xs) + 4) * scale); H = int((max(ys) - min(ys) + 4) * scale)
    img = Image.new('RGBA', (W, H), (30, 20, 26, 255))
    from PIL import ImageDraw
    dr = ImageDraw.Draw(img)
    ox, oy = -min(xs) + 2, -min(ys) + 2
    for d, sx, sy, c, g, k in items:
        r, gg, b = int(c[1:3], 16), int(c[3:5], 16), int(c[5:7], 16)
        top = (k[0], k[1] + 1, k[2]) not in occupied
        f = 1.0 if top else 0.8
        if g:
            f = 1.15
        col = (min(255, int(r * f)), min(255, int(gg * f)), min(255, int(b * f)))
        x0, y0 = (sx + ox) * scale, (sy + oy) * scale
        dr.rectangle([x0 - scale * 0.55, y0 - scale * 0.5, x0 + scale * 0.55, y0 + scale * 0.5], fill=col)
    img.save(path)
