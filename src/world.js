// Constrói a geometria 3D do andar (chão, paredes, caixas, portas, tochas, luzes).
import * as THREE from 'three';
import { T } from './dungeon.js';
import { Pix, shade, toTexture } from './pixel.js';
import { RNG } from './util.js';
import { makeSprite, setFrame, TEX } from './gfx.js';

const TILE_PX = 16;
export const WALL_H = 1.5;
export const SHORT_H = 0.42;

function floorTiles(theme, rng) {
  const variants = [];
  for (let v = 0; v < 6; v++) {
    const p = new Pix(16, 16);
    const base = v % 3 === 0 ? theme.floorAlt : theme.floor;
    p.rect(0, 0, 16, 16, base);
    // juntas das pedras (2 pedras por tile, deslocadas)
    p.rect(0, 15, 16, 1, theme.grout).rect(15, 0, 1, 16, theme.grout);
    if (v % 2) p.rect(0, 7, 16, 1, theme.grout).rect(7, 0, 1, 7, theme.grout);
    else p.rect(0, 7, 16, 1, theme.grout).rect(9, 8, 1, 7, theme.grout);
    // ruído e realces
    for (let i = 0; i < 10; i++) p.set(rng.int(0, 14), rng.int(0, 14), shade(base, rng.float(-0.12, 0.1)));
    p.rect(0, 0, 15, 1, shade(base, 0.08));
    if (v === 5) { p.line(3, 3, 6, 6, shade(base, -0.3)); p.line(6, 6, 5, 10, shade(base, -0.3)); }
    if (v === 4) { p.circ(10, 11, 2, shade(base, -0.15)); }
    variants.push(p.canvas());
  }
  return variants;
}

function wallSideTexture(theme, height, rows) {
  const h = Math.round(height * TILE_PX);
  const p = new Pix(16, h);
  p.rect(0, 0, 16, h, theme.wall);
  const rowH = Math.max(3, Math.floor(h / rows));
  for (let r = 0; r * rowH < h; r++) {
    const y = r * rowH;
    p.rect(0, y, 16, 1, theme.wallD);
    const off = r % 2 ? 4 : 10;
    p.rect(off, y, 1, rowH, theme.wallD);
    p.rect(0, y + 1, 16, 1, shade(theme.wall, 0.12));
  }
  p.rect(0, h - 2, 16, 2, shade(theme.wallD, -0.3));
  p.rect(0, 0, 16, 1, shade(theme.wall, 0.3));
  return toTexture(p.canvas());
}

function wallTopTexture(theme) {
  const p = new Pix(16, 16);
  const top = shade(theme.top, 0.28);
  p.rect(0, 0, 16, 16, top);
  p.rect(0, 0, 16, 1, shade(top, 0.35)).rect(0, 15, 16, 1, shade(top, -0.4)).rect(0, 0, 1, 16, shade(top, 0.2));
  p.set(5, 6, shade(top, 0.15)).set(11, 10, shade(top, 0.15));
  return toTexture(p.canvas());
}

function crateTexture() {
  const p = new Pix(16, 16);
  p.rect(0, 0, 16, 16, '#9a6a36');
  for (let y = 0; y < 16; y += 5) p.rect(0, y, 16, 1, '#6d4520');
  p.rect(0, 0, 2, 16, '#5a3818').rect(14, 0, 2, 16, '#5a3818').rect(0, 0, 16, 2, '#5a3818').rect(0, 14, 16, 2, '#5a3818');
  p.line(2, 2, 13, 13, '#5a3818');
  p.set(1, 1, '#c9c2b0').set(14, 1, '#c9c2b0').set(1, 14, '#c9c2b0').set(14, 14, '#c9c2b0');
  return toTexture(p.canvas());
}

function gateTexture() {
  const p = new Pix(48, 24);
  for (let x = 2; x < 48; x += 6) p.rect(x, 0, 2, 24, '#3a3a46').rect(x, 0, 1, 24, '#6a6a80');
  p.rect(0, 3, 48, 2, '#2a2a34').rect(0, 18, 48, 2, '#2a2a34');
  for (let x = 2; x < 48; x += 6) p.set(x, 1, '#ff4d5e').set(x + 1, 1, '#ff4d5e');
  return toTexture(p.canvas());
}

export class World {
  constructor(scene, dungeon, theme) {
    this.scene = scene;
    this.d = dungeon;
    this.theme = theme;
    this.group = new THREE.Group();
    scene.add(this.group);
    const rng = new RNG(dungeon.seed ^ 0x9e3779b9);
    this.buildFloor(rng);
    this.buildWalls();
    this.buildCrates();
    this.buildDoors();
    this.buildTorches();
  }

  tile(x, z) {
    const d = this.d;
    if (x < 0 || z < 0 || x >= d.W || z >= d.H) return T.VOID;
    return d.tiles[z * d.W + x];
  }
  walkable(t) { return t === T.FLOOR || t === T.CRATE || t === T.PIT; }

  buildFloor(rng) {
    const { W, H, rooms } = this.d;
    const th = this.theme;
    const variants = floorTiles(th, rng);
    const cv = document.createElement('canvas');
    cv.width = W * TILE_PX; cv.height = H * TILE_PX;
    const g = cv.getContext('2d');
    g.fillStyle = '#000'; g.fillRect(0, 0, cv.width, cv.height);
    for (let z = 0; z < H; z++) for (let x = 0; x < W; x++) {
      const t = this.tile(x, z);
      if (!this.walkable(t)) continue;
      const v = (x * 7 + z * 13 + ((x * z) % 5)) % 6;
      g.drawImage(variants[rng.chance(0.85) ? v % 3 : v], x * TILE_PX, z * TILE_PX);
      // sombra na base das paredes de cima
      if (!this.walkable(this.tile(x, z - 1))) { g.fillStyle = 'rgba(0,0,0,0.45)'; g.fillRect(x * TILE_PX, z * TILE_PX, TILE_PX, 5); }
      if (!this.walkable(this.tile(x - 1, z))) { g.fillStyle = 'rgba(0,0,0,0.25)'; g.fillRect(x * TILE_PX, z * TILE_PX, 3, TILE_PX); }
      if (!this.walkable(this.tile(x + 1, z))) { g.fillStyle = 'rgba(0,0,0,0.25)'; g.fillRect(x * TILE_PX + 13, z * TILE_PX, 3, TILE_PX); }
      if (t === T.PIT) {
        g.fillStyle = th.lava ? '#ff5a1a' : th.pit;
        g.fillRect(x * TILE_PX, z * TILE_PX, TILE_PX, TILE_PX);
        if (th.lava) {
          g.fillStyle = '#ffb347';
          for (let i = 0; i < 3; i++) g.fillRect(x * TILE_PX + rng.int(1, 13), z * TILE_PX + rng.int(1, 13), 2, 1);
        }
        if (this.tile(x, z - 1) !== T.PIT) { g.fillStyle = shade(th.wallD, -0.2); g.fillRect(x * TILE_PX, z * TILE_PX, TILE_PX, 4); }
      }
    }
    // decorações por tipo de sala
    for (const r of rooms) {
      const px = (r.x0 + r.w / 2) * TILE_PX, pz = (r.z0 + r.h / 2) * TILE_PX;
      if (r.type === 'shop') {
        g.fillStyle = '#6b2a3a'; g.fillRect(px - 72, pz - 30, 144, 60);
        g.strokeStyle = '#d9a441'; g.lineWidth = 3; g.strokeRect(px - 66, pz - 24, 132, 48);
      } else if (r.type === 'boss' || r.type === 'start') {
        g.strokeStyle = r.type === 'boss' ? 'rgba(255,60,80,0.35)' : 'rgba(255,220,150,0.25)';
        g.lineWidth = 4;
        g.beginPath(); g.arc(px, pz, r.type === 'boss' ? 64 : 34, 0, Math.PI * 2); g.stroke();
        g.beginPath(); g.arc(px, pz, r.type === 'boss' ? 44 : 20, 0, Math.PI * 2); g.stroke();
      } else if (r.type === 'treasure') {
        g.fillStyle = 'rgba(255,211,90,0.12)'; g.fillRect(px - 40, pz - 40, 80, 80);
      }
      // manchas aleatórias
      for (let i = 0; i < 4; i++) {
        g.fillStyle = 'rgba(0,0,0,0.12)';
        g.beginPath(); g.ellipse((r.x0 + rng.float(1, r.w - 1)) * TILE_PX, (r.z0 + rng.float(1, r.h - 1)) * TILE_PX, rng.float(6, 18), rng.float(4, 10), 0, 0, 7); g.fill();
      }
    }
    const tex = toTexture(cv);
    tex.anisotropy = 1;
    const mat = new THREE.MeshLambertMaterial({ map: tex });
    const mesh = new THREE.Mesh(new THREE.PlaneGeometry(W, H), mat);
    mesh.rotation.x = -Math.PI / 2;
    mesh.position.set(W / 2, 0, H / 2);
    this.group.add(mesh);
    this.floorMesh = mesh;
    this.floorCanvas = cv;
  }

  buildWalls() {
    const { W, H } = this.d;
    const tall = [], short = [];
    for (let z = 0; z < H; z++) for (let x = 0; x < W; x++) {
      const t = this.tile(x, z);
      if (this.walkable(t)) continue;
      let near = false;
      for (let dz = -1; dz <= 1 && !near; dz++) for (let dx = -1; dx <= 1; dx++) if (this.walkable(this.tile(x + dx, z + dz))) { near = true; break; }
      if (!near) continue;
      const southOfFloor = this.walkable(this.tile(x, z - 1)) && t !== T.PILLAR;
      (southOfFloor ? short : tall).push([x, z, t === T.PILLAR]);
    }
    const top = new THREE.MeshLambertMaterial({ map: wallTopTexture(this.theme) });
    const mk = (list, h, rows) => {
      const side = new THREE.MeshLambertMaterial({ map: wallSideTexture(this.theme, h, rows) });
      const geo = new THREE.BoxGeometry(1, h, 1);
      const mats = [side, side, top, top, side, side];
      const im = new THREE.InstancedMesh(geo, mats, list.length);
      const m = new THREE.Matrix4();
      list.forEach(([x, z], i) => { m.makeTranslation(x + 0.5, h / 2, z + 0.5); im.setMatrixAt(i, m); });
      im.instanceMatrix.needsUpdate = true;
      this.group.add(im);
      return im;
    };
    if (tall.length) mk(tall, WALL_H, 4);
    if (short.length) mk(short, SHORT_H, 2);
  }

  buildCrates() {
    const { W, H } = this.d;
    const list = [];
    for (let z = 0; z < H; z++) for (let x = 0; x < W; x++) if (this.tile(x, z) === T.CRATE) list.push([x, z]);
    this.crateIndex = new Map();
    if (!list.length) return;
    const mat = new THREE.MeshLambertMaterial({ map: crateTexture() });
    const im = new THREE.InstancedMesh(new THREE.BoxGeometry(0.86, 0.8, 0.86), mat, list.length);
    const m = new THREE.Matrix4();
    list.forEach(([x, z], i) => {
      m.makeTranslation(x + 0.5, 0.4, z + 0.5); im.setMatrixAt(i, m);
      this.crateIndex.set(z * W + x, { i, hp: 12 });
    });
    this.crates = im;
    this.group.add(im);
  }

  damageCrate(x, z, dmg) {
    const c = this.crateIndex.get(z * this.d.W + x);
    if (!c) return false;
    c.hp -= dmg;
    if (c.hp <= 0) {
      this.crateIndex.delete(z * this.d.W + x);
      this.d.tiles[z * this.d.W + x] = T.FLOOR;
      const m = new THREE.Matrix4().makeScale(0, 0, 0);
      this.crates.setMatrixAt(c.i, m);
      this.crates.instanceMatrix.needsUpdate = true;
      return true;
    }
    return false;
  }

  buildDoors() {
    const tex = gateTexture();
    const mat = new THREE.MeshLambertMaterial({ map: tex, transparent: true, alphaTest: 0.5, side: THREE.DoubleSide, emissive: 0x220000 });
    this.gates = [];
    for (const r of this.d.rooms) {
      if (r.type !== 'combat' && r.type !== 'boss') continue;
      for (const door of r.doors) {
        const xs = door.tiles.map((t) => t[0]), zs = door.tiles.map((t) => t[1]);
        const cx = (Math.min(...xs) + Math.max(...xs) + 1) / 2, cz = (Math.min(...zs) + Math.max(...zs) + 1) / 2;
        const horizontal = door.dir === 'n' || door.dir === 's';
        const mesh = new THREE.Mesh(new THREE.PlaneGeometry(3, 1.3), mat);
        mesh.position.set(cx, -1.4, cz);
        if (!horizontal) mesh.rotation.y = Math.PI / 2;
        mesh.visible = false;
        this.group.add(mesh);
        door.gate = { mesh, target: -1.4 };
        this.gates.push(door.gate);
      }
    }
  }

  setDoors(room, locked) {
    for (const door of room.doors) {
      if (!door.gate) continue;
      door.locked = locked;
      door.gate.target = locked ? 0.65 : -1.4;
      door.gate.mesh.visible = true;
    }
  }

  buildTorches() {
    this.torches = [];
    for (const t of this.d.torches) {
      const s = makeSprite(TEX.torch);
      s.position.set(t.x, 0.75, t.z + 0.01);
      s.scale.y = 1;
      this.group.add(s);
      this.torches.push({ ...t, sprite: s, phase: Math.random() * 10 });
    }
    this.lights = [];
    for (let i = 0; i < 8; i++) {
      const l = new THREE.PointLight(this.theme.torch, 0, 7.5, 1.3);
      this.group.add(l);
      this.lights.push(l);
    }
  }

  update(dt, time, px, pz) {
    // anima portões
    for (const g of this.gates) {
      g.mesh.position.y += (g.target - g.mesh.position.y) * Math.min(1, dt * 12);
      if (g.target < 0 && g.mesh.position.y < -1.3) g.mesh.visible = false;
    }
    // tochas mais próximas recebem luz real
    const sorted = this.torches
      .map((t) => ({ t, d: (t.x - px) ** 2 + (t.z - pz) ** 2 }))
      .sort((a, b) => a.d - b.d);
    const frame = Math.floor(time * 6);
    for (const t of this.torches) setFrame(t.sprite, frame + (t.phase | 0));
    for (let i = 0; i < this.lights.length; i++) {
      const l = this.lights[i];
      const e = sorted[i];
      if (!e || e.d > 22 * 22) { l.intensity = 0; continue; }
      l.position.set(e.t.x, 1.1, e.t.z + 0.6);
      l.intensity = 3.2 + Math.sin(time * 9 + e.t.phase) * 0.35 + Math.sin(time * 23 + e.t.phase * 3) * 0.2;
    }
  }

  dispose() {
    this.scene.remove(this.group);
    this.group.traverse((o) => {
      if (o.geometry && !o.geometry.userData?.shared) o.geometry.dispose?.();
      if (o.material) {
        const ms = Array.isArray(o.material) ? o.material : [o.material];
        for (const m of ms) { if (m.map && !Object.values(TEX).includes(m.map)) m.map.dispose?.(); m.dispose?.(); }
      }
    });
  }
}
