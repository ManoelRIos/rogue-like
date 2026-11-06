"""Gera todos os spritesheets e o atlas.json em Assets/Resources/Art.

Uso:  python3 Tools/ArtGen/build.py
Para trocar a arte por spritesheets próprios basta substituir os PNGs mantendo
o mesmo tamanho de quadro, ou editar o atlas.json (veja Docs/SPRITES.md).
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
from pixtool import sheet
import chars
import items
import tiles
import font

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Art')
os.makedirs(OUT, exist_ok=True)

atlas = {'sheets': [], 'weapons': [], 'tilesets': [], 'font': {}}


def save(img, rel):
    path = os.path.join(OUT, rel + '.png')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    return 'Art/' + rel


def add_sheet(name, anims, fw, fh, pivot_x=None, pivot_y=1, fps=8):
    """anims: lista de (nome, [imagens], fps, loop)."""
    rows = [a[1] for a in anims]
    img = sheet(rows, fw, fh)
    f = save(img, 'Sheets/' + name)
    atlas['sheets'].append({
        'name': name, 'file': f, 'fw': fw, 'fh': fh,
        'pivotX': fw / 2 if pivot_x is None else pivot_x, 'pivotY': pivot_y,
        'anims': [{'name': a[0], 'row': i, 'count': len(a[1]), 'fps': a[2], 'loop': a[3]} for i, a in enumerate(anims)],
    })
    return img


previews = []

# ---------------- jogador
P = chars.player_frames()
order = [('idle_down', 6, True), ('idle_side', 6, True), ('idle_up', 6, True), ('run_down', 12, True), ('run_side', 12, True),
         ('run_up', 12, True), ('roll', 14, False), ('death', 6, False)]
previews.append(add_sheet('player', [(n, P[n], f, l) for n, f, l in order], chars.PW, chars.PH))

# ---------------- inimigos
for name, frames in chars.enemy_frames().items():
    fw = max(im.width for im in frames)
    fh = max(im.height for im in frames)
    previews.append(add_sheet('enemy_' + name, [('move', frames, 7, True)], fw, fh))

for name, frames in chars.boss_frames().items():
    previews.append(add_sheet('boss_' + name, [('move', frames, 6, True)], 48, 48))

# ---------------- itens / efeitos simples
simple = {
    'heart': [items.heart('full')], 'heart_half': [items.heart('half')], 'heart_empty': [items.heart('empty')],
    'blank': [items.blank()], 'ammo': [items.ammo()], 'pedestal': [items.pedestal()], 'shadow': [items.shadow()],
    'chest': [items.chest(False, False), items.chest(True, False)], 'chest_rare': [items.chest(False, True), items.chest(True, True)],
    'hatch': [items.hatch()], 'crate': [items.crate_face()], 'gate': [tiles.gate()],
}
for name, frames in simple.items():
    fw = max(im.width for im in frames); fh = max(im.height for im in frames)
    centered = name in ('shadow', 'hatch')
    add_sheet(name, [('frames', frames, 1, False)], fw, fh, pivot_y=fh / 2 if centered else 0)

add_sheet('coin', [('spin', items.coin_frames(), 8, True)], 9, 11, pivot_y=0)
add_sheet('torch', [('burn', items.torch_frames(), 8, True)], 10, 18, pivot_y=0)
for name, frames in items.fx().items():
    fw = max(im.width for im in frames); fh = max(im.height for im in frames)
    add_sheet('fx_' + name, [('play', frames, 14, False)], fw, fh, pivot_y=fh / 2)
for name, im in items.bullets().items():
    add_sheet('bullet_' + name, [('frames', [im], 1, False)], im.width, im.height, pivot_y=im.height / 2)

# ---------------- armas
for name, (im, gx, gy) in items.weapon_images().items():
    f = save(im, 'Weapons/' + name)
    atlas['weapons'].append({'name': name, 'file': f, 'gripX': gx, 'gripY': gy, 'w': im.width, 'h': im.height})
    previews.append(im)

# ---------------- tilesets
for i, t in enumerate(tiles.THEMES):
    im = tiles.build_atlas(t, seed=11 + i)
    f = save(im, 'Tiles/' + t)
    atlas['tilesets'].append({'name': t, 'file': f})
    previews.append(im)

# ---------------- fonte
fimg, chars_str = font.build_font()
atlas['font'] = {'file': save(fimg, 'UI/font'), 'cellW': font.CELL_W, 'cellH': font.CELL_H, 'cols': font.COLS, 'chars': chars_str}

with open(os.path.join(OUT, 'atlas.json'), 'w', encoding='utf-8') as fp:
    json.dump(atlas, fp, ensure_ascii=False, indent=1)

# prévia (não vai para o Unity)
pw = 4
W = max(im.width for im in previews)
H = sum(im.height + 4 for im in previews)
prev = Image.new('RGBA', (W, H), (40, 30, 44, 255))
y = 0
for im in previews:
    prev.alpha_composite(im, (0, y)); y += im.height + 4
prev = prev.resize((W * pw, H * pw), Image.NEAREST)
os.makedirs(os.path.join(ROOT, 'Docs'), exist_ok=True)
prev.save(os.path.join(ROOT, 'Docs', 'sprites_preview.png'))
print('ok', len(atlas['sheets']), 'sheets', len(atlas['weapons']), 'armas')
