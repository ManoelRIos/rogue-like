// Geração procedural de andares: salas em uma grade, ligadas por corredores.
import { RNG } from './util.js';

export const T = { VOID: 0, FLOOR: 1, CRATE: 3, PILLAR: 4, PIT: 5 };
export const CELL = 22;
const G = 7; // grade 7x7 de células

const DIRS = [[1, 0], [-1, 0], [0, 1], [0, -1]];

export function generateFloor(floorIndex, seed) {
  const rng = new RNG(seed);
  for (let attempt = 0; attempt < 80; attempt++) {
    const d = tryGenerate(floorIndex, rng);
    if (d) return d;
  }
  throw new Error('Falha ao gerar o andar');
}

function tryGenerate(floorIndex, rng) {
  const target = 8 + floorIndex + rng.int(0, 1);
  const cells = new Map(); // "x,z" -> room
  const key = (x, z) => `${x},${z}`;
  const rooms = [];
  const addRoom = (gx, gz) => {
    const r = { id: rooms.length, gx, gz, neighbors: [], type: 'combat' };
    rooms.push(r); cells.set(key(gx, gz), r); return r;
  };
  const start = addRoom(3, 3);
  let guard = 0;
  while (rooms.length < target && guard++ < 2000) {
    const from = rng.pick(rooms);
    const [dx, dz] = rng.pick(DIRS);
    const nx = from.gx + dx, nz = from.gz + dz;
    if (nx < 0 || nz < 0 || nx >= G || nz >= G || cells.has(key(nx, nz))) continue;
    // evita aglomerados: a nova célula não pode ter muitos vizinhos ocupados
    let occ = 0;
    for (const [ax, az] of DIRS) if (cells.has(key(nx + ax, nz + az))) occ++;
    if (occ > 1 && !rng.chance(0.15)) continue;
    const r = addRoom(nx, nz);
    link(from, r);
  }
  // alguns laços extras para não ser só árvore
  for (const r of rooms) for (const [dx, dz] of DIRS) {
    const o = cells.get(key(r.gx + dx, r.gz + dz));
    if (o && !r.neighbors.includes(o.id) && rng.chance(0.08) && o !== start && r !== start) link(r, o);
  }

  // distâncias BFS desde o início
  const dist = bfs(rooms, start.id);
  const leaves = rooms.filter((r) => r.neighbors.length === 1 && r !== start);
  if (leaves.length < 3) return null;
  leaves.sort((a, b) => dist[b.id] - dist[a.id]);
  const boss = leaves[0];
  if (dist[boss.id] < 3) return null;
  boss.type = 'boss';
  const rest = rng.shuffle(leaves.slice(1));
  rest[0].type = 'treasure';
  rest[1].type = 'shop';
  start.type = 'start';
  // antessala do chefe nunca é a loja/tesouro, e sim combate
  for (const r of rooms) r.dist = dist[r.id];

  // ---- grade de tiles
  const W = G * CELL, H = G * CELL;
  const tiles = new Uint8Array(W * H);
  const roomGrid = new Int16Array(W * H).fill(-1);
  const idx = (x, z) => z * W + x;

  for (const r of rooms) {
    let w, h;
    switch (r.type) {
      case 'boss': w = 19; h = 15; break;
      case 'start': w = 11; h = 9; break;
      case 'treasure': w = 9; h = 9; break;
      case 'shop': w = 13; h = 9; break;
      default: w = rng.int(5, 8) * 2 + 1; h = rng.int(4, 6) * 2 + 1; // 11..17 x 9..13
    }
    const cx = r.gx * CELL + CELL / 2, cz = r.gz * CELL + CELL / 2;
    const ox = r.type === 'combat' ? rng.int(-1, 1) : 0, oz = r.type === 'combat' ? rng.int(-1, 1) : 0;
    r.x0 = cx - Math.floor(w / 2) + ox; r.z0 = cz - Math.floor(h / 2) + oz;
    r.w = w; r.h = h; r.cx = cx; r.cz = cz;
    for (let z = r.z0; z < r.z0 + h; z++) for (let x = r.x0; x < r.x0 + w; x++) {
      tiles[idx(x, z)] = T.FLOOR; roomGrid[idx(x, z)] = r.id;
    }
    r.doors = [];
    r.cleared = r.type !== 'combat' && r.type !== 'boss';
    r.visited = false; r.discovered = false;
  }

  // ---- corredores (3 de largura) pela linha central das células
  const corridors = [];
  for (const a of rooms) for (const nid of a.neighbors) {
    if (nid < a.id) continue;
    const b = rooms[nid];
    let A = a, B = b;
    if (a.gx === b.gx) { // vertical
      if (A.gz > B.gz) [A, B] = [B, A];
      const zs = A.z0 + A.h, ze = B.z0 - 1;
      for (let z = zs; z <= ze; z++) for (let x = A.cx - 1; x <= A.cx + 1; x++) tiles[idx(x, z)] = T.FLOOR;
      A.doors.push({ tiles: [[A.cx - 1, zs], [A.cx, zs], [A.cx + 1, zs]], dir: 's' });
      B.doors.push({ tiles: [[B.cx - 1, ze], [B.cx, ze], [B.cx + 1, ze]], dir: 'n' });
      corridors.push({ x0: A.cx - 1, z0: zs, x1: A.cx + 1, z1: ze });
    } else { // horizontal
      if (A.gx > B.gx) [A, B] = [B, A];
      const xs = A.x0 + A.w, xe = B.x0 - 1;
      for (let x = xs; x <= xe; x++) for (let z = A.cz - 1; z <= A.cz + 1; z++) tiles[idx(x, z)] = T.FLOOR;
      A.doors.push({ tiles: [[xs, A.cz - 1], [xs, A.cz], [xs, A.cz + 1]], dir: 'e' });
      B.doors.push({ tiles: [[xe, B.cz - 1], [xe, B.cz], [xe, B.cz + 1]], dir: 'w' });
      corridors.push({ x0: xs, z0: A.cz - 1, x1: xe, z1: A.cz + 1 });
    }
  }

  // ---- obstáculos nas salas de combate
  for (const r of rooms) {
    if (r.type !== 'combat' && r.type !== 'boss') continue;
    placeObstacles(r, tiles, W, rng, r.type === 'boss');
  }

  // ---- tochas nas paredes de cima de cada sala
  const torches = [];
  for (const r of rooms) {
    for (let x = r.x0 + 2; x < r.x0 + r.w - 2; x += 4) {
      if (tiles[idx(x, r.z0 - 1)] === T.VOID) torches.push({ x: x + 0.5, z: r.z0 + 0.02, room: r.id });
    }
  }

  return { W, H, tiles, roomGrid, rooms, corridors, start, boss, torches, floor: floorIndex, seed: rng.seed };
}

function link(a, b) { a.neighbors.push(b.id); b.neighbors.push(a.id); }

function bfs(rooms, s) {
  const d = new Array(rooms.length).fill(Infinity); d[s] = 0;
  const q = [s];
  while (q.length) {
    const c = q.shift();
    for (const n of rooms[c].neighbors) if (d[n] === Infinity) { d[n] = d[c] + 1; q.push(n); }
  }
  return d;
}

function placeObstacles(r, tiles, W, rng, isBoss) {
  const idx = (x, z) => z * W + x;
  const placed = [];
  const free = (x, z) => Math.abs(x - r.cx) > 2 && Math.abs(z - r.cz) > 2
    && x > r.x0 && z > r.z0 && x < r.x0 + r.w - 1 && z < r.z0 + r.h - 1;
  const layout = isBoss ? 'bossPillars' : rng.pick(['pillars', 'crates', 'mixed', 'pits', 'open']);

  const tryPut = (x, z, t) => { if (free(x, z) && tiles[idx(x, z)] === T.FLOOR) { tiles[idx(x, z)] = t; placed.push([x, z]); } };

  if (layout === 'pillars' || layout === 'mixed') {
    const n = rng.int(2, 4);
    for (let i = 0; i < n; i++) {
      const x = rng.int(r.x0 + 2, r.x0 + r.w - 4), z = rng.int(r.z0 + 2, r.z0 + r.h - 4);
      const s = rng.chance(0.5) ? 2 : 1;
      for (let a = 0; a < s; a++) for (let b = 0; b < s; b++) tryPut(x + a, z + b, T.PILLAR);
    }
  }
  if (layout === 'bossPillars') {
    for (const [fx, fz] of [[0.25, 0.3], [0.75, 0.3], [0.25, 0.7], [0.75, 0.7]]) {
      const x = Math.floor(r.x0 + r.w * fx), z = Math.floor(r.z0 + r.h * fz);
      tryPut(x, z, T.PILLAR);
    }
  }
  if (layout === 'crates' || layout === 'mixed') {
    const n = rng.int(3, 8);
    for (let i = 0; i < n; i++) {
      const x = rng.int(r.x0 + 1, r.x0 + r.w - 2), z = rng.int(r.z0 + 1, r.z0 + r.h - 2);
      tryPut(x, z, T.CRATE);
      if (rng.chance(0.4)) tryPut(x + 1, z, T.CRATE);
    }
  }
  if (layout === 'pits') {
    const n = rng.int(1, 2);
    for (let i = 0; i < n; i++) {
      const w = rng.int(2, 3), h = rng.int(2, 3);
      const x = rng.int(r.x0 + 2, r.x0 + r.w - 2 - w), z = rng.int(r.z0 + 2, r.z0 + r.h - 2 - h);
      for (let a = 0; a < w; a++) for (let b = 0; b < h; b++) tryPut(x + a, z + b, T.PIT);
    }
  }

  // garante que todo o chão da sala continua alcançável (caixas contam como passáveis)
  const seen = new Set();
  const q = [[r.cx, r.cz]];
  seen.add(r.cx + ',' + r.cz);
  while (q.length) {
    const [x, z] = q.pop();
    for (const [dx, dz] of DIRS) {
      const nx = x + dx, nz = z + dz;
      if (nx < r.x0 || nz < r.z0 || nx >= r.x0 + r.w || nz >= r.z0 + r.h) continue;
      const t = tiles[idx(nx, nz)];
      if ((t === T.FLOOR || t === T.CRATE) && !seen.has(nx + ',' + nz)) { seen.add(nx + ',' + nz); q.push([nx, nz]); }
    }
  }
  for (let z = r.z0; z < r.z0 + r.h; z++) for (let x = r.x0; x < r.x0 + r.w; x++) {
    const t = tiles[idx(x, z)];
    if ((t === T.FLOOR || t === T.CRATE) && !seen.has(x + ',' + z)) {
      for (const [px, pz] of placed) if (tiles[idx(px, pz)] !== T.CRATE) tiles[idx(px, pz)] = T.FLOOR;
      return;
    }
  }
}

export function isSolid(t) { return t === T.VOID || t === T.PILLAR || t === T.CRATE; }
