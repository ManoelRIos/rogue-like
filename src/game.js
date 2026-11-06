// Núcleo do jogo: andares, salas, colisão, câmera, loop e estados.
import * as THREE from 'three';
import { Input } from './input.js';
import { sfx, initAudio } from './audio.js';
import { buildTextures, CAM_PITCH } from './gfx.js';
import { generateFloor, T } from './dungeon.js';
import { World } from './world.js';
import { THEMES } from './themes.js';
import { Player } from './player.js';
import { Enemy, Boss, ENEMY_DEFS, BOSS_DEFS } from './enemies.js';
import { Bullets } from './bullets.js';
import { Particles } from './particles.js';
import { Pickup, Chest, ShopItem, Hatch } from './items.js';
import { HUD } from './hud.js';
import { RNG, clamp } from './util.js';
import { randomWeapon, WEAPONS } from './weapons.js';

const FLOORS = 5;
const CAM_DIST = 21;
const $ = (id) => document.getElementById(id);

export class Game {
  constructor(canvas) {
    this.canvas = canvas;
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(1);
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(32, 16 / 9, 0.5, 80);
    this.input = new Input(canvas);
    buildTextures();
    this.hud = new HUD(this);
    this.particles = new Particles(this.scene);
    this.bullets = new Bullets(this.scene, this);
    this.ambient = new THREE.AmbientLight(0xffffff, 1);
    this.scene.add(this.ambient);
    this.hemi = new THREE.HemisphereLight(0xffffff, 0x302030, 0.7);
    this.scene.add(this.hemi);

    this.enemies = [];
    this.pickups = [];
    this.pickupsStatic = [];
    this.interactables = [];
    this.lockedTiles = new Set();
    this.mouseWorld = new THREE.Vector3();
    this.raycaster = new THREE.Raycaster();
    this.aimPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), -0.42);
    this.camTarget = new THREE.Vector3();
    this.shakeAmt = 0; this.stopT = 0; this.time = 0;
    this.state = 'title';
    this.params = new URLSearchParams(location.search);
    this.godMode = this.params.has('god');

    addEventListener('resize', () => this.resize());
    this.resize();
    this.bindUI();
    this.newRun(true);
    this.last = performance.now();
    requestAnimationFrame((t) => this.loop(t));
  }

  // ------------------------------------------------------------------ UI / estados
  bindUI() {
    $('btn-start').onclick = () => this.start();
    $('btn-resume').onclick = () => this.setPaused(false);
    $('btn-restart').onclick = () => { $('screen-pause').classList.add('hidden'); this.start(); };
    $('btn-again').onclick = () => { $('screen-over').classList.add('hidden'); this.start(); };
  }

  start() {
    initAudio();
    $('screen-title').classList.add('hidden');
    this.newRun(false);
    this.state = 'playing';
    this.hud.show(true);
  }

  setPaused(p) {
    if (this.state !== 'playing' && this.state !== 'paused') return;
    this.state = p ? 'paused' : 'playing';
    $('screen-pause').classList.toggle('hidden', !p);
  }

  resize() {
    const w = innerWidth, h = innerHeight;
    this.pixelScale = Math.max(2, Math.round(h / 300));
    this.renderer.setSize(Math.ceil(w / this.pixelScale), Math.ceil(h / this.pixelScale), false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }

  newRun(attract) {
    this.stats = { kills: 0, shots: 0, hits: 0, rolls: 0, coins: 0, time: 0, floor: 1 };
    this.victoryT = undefined;
    this.descending = false;
    this.activeRoom = null;
    if (this.player) this.scene.remove(this.player.group);
    this.player = new Player(this);
    this.scene.add(this.player.group);
    const startFloor = attract ? 0 : clamp(parseInt(this.params.get('floor') || '1', 10) - 1, 0, FLOORS - 1);
    this.loadFloor(startFloor, attract);
  }

  clearLevel() {
    for (const e of this.enemies) e.remove();
    for (const p of this.pickups) p.remove();
    for (const p of this.pickupsStatic) p.remove();
    for (const i of this.interactables) i.remove();
    this.enemies = []; this.pickups = []; this.pickupsStatic = []; this.interactables = [];
    this.bullets.reset(); this.particles.reset();
    this.lockedTiles.clear();
    this.world?.dispose();
    this.hud.bossBar(null);
    this.boss = null;
    this.activeRoom = null;
  }

  loadFloor(i, attract = false) {
    this.clearLevel();
    this.floorIndex = i;
    this.stats.floor = Math.max(this.stats.floor, i + 1);
    this.theme = THEMES[i];
    const seed = parseInt(this.params.get('seed') || '0', 10) || ((Math.random() * 2 ** 31) | 0);
    this.rng = new RNG(seed + i * 7919);
    this.dungeon = generateFloor(i, seed + i * 7919);
    this.world = new World(this.scene, this.dungeon, this.theme);
    this.scene.fog = new THREE.Fog(this.theme.fog, CAM_DIST + 3, CAM_DIST + 24);
    this.scene.background = new THREE.Color(this.theme.fog);
    this.ambient.color.setHex(this.theme.ambient);
    this.ambient.intensity = this.theme.ambientI;
    this.hemi.color.setHex(this.theme.torch);

    const s = this.dungeon.start;
    const P = this.player;
    P.x = s.x0 + s.w / 2; P.z = s.z0 + s.h / 2 + 1;
    P.vx = P.vz = 0; P.roll = null;
    P.blanks = Math.max(P.blanks, 2);
    this.camTarget.set(P.x, 0, P.z);
    this.currentRoom = null;

    for (const r of this.dungeon.rooms) {
      const cx = r.x0 + r.w / 2, cz = r.z0 + r.h / 2;
      if (r.type === 'treasure') this.interactables.push(new Chest(this, cx, cz, this.rng.chance(0.25 + i * 0.05)));
      if (r.type === 'shop') this.makeShop(r);
      if (r.type === 'start' && i === 0 && !attract) {
        // arma inicial extra no primeiro andar para variedade
        this.pickups.push(new Pickup(this, 'ammo', cx + 2.5, cz - 1, { pop: false }));
      }
    }
    this.hud.dirty();
    if (!attract) this.banner();
  }

  banner() {
    const el = $('screen-floor');
    $('floor-banner-num').textContent = `Andar ${this.floorIndex + 1} de ${FLOORS}`;
    $('floor-banner-name').textContent = this.theme.name;
    el.classList.remove('hidden');
    const b = el.querySelector('.floor-banner');
    b.style.animation = 'none'; void b.offsetWidth; b.style.animation = '';
    clearTimeout(this.bannerT);
    this.bannerT = setTimeout(() => el.classList.add('hidden'), 2700);
  }

  makeShop(r) {
    const f = this.floorIndex;
    const cx = r.x0 + r.w / 2, cz = r.z0 + r.h / 2;
    const owned = this.player.weapons.map((w) => w.id);
    const wid = randomWeapon(this.rng, f, owned) || 'escopeta';
    const others = this.rng.shuffle([
      { type: 'heart', price: 14 + f * 3 },
      { type: 'ammo', price: 11 + f * 2 },
      { type: 'blank', price: 10 + f * 2 },
    ]).slice(0, 2);
    const items = [others[0], { type: 'weapon', weaponId: wid, price: 38 + f * 9 }, others[1]];
    items.forEach((it, k) => this.interactables.push(new ShopItem(this, cx - 3 + k * 3, cz - 0.5, it)));
  }

  // ------------------------------------------------------------------ colisão
  tileAt(x, z) {
    const d = this.dungeon;
    const tx = Math.floor(x), tz = Math.floor(z);
    if (tx < 0 || tz < 0 || tx >= d.W || tz >= d.H) return T.VOID;
    return d.tiles[tz * d.W + tx];
  }
  tileBlocksMove(tx, tz, fly) {
    const d = this.dungeon;
    if (tx < 0 || tz < 0 || tx >= d.W || tz >= d.H) return true;
    const t = d.tiles[tz * d.W + tx];
    if (t === T.VOID || t === T.PILLAR || t === T.CRATE) return true;
    if (t === T.PIT && !fly) return true;
    return this.lockedTiles.has(tz * d.W + tx);
  }
  blocksMove(x, z, fly = false) { return this.tileBlocksMove(Math.floor(x), Math.floor(z), fly); }
  blocksBullet(x, z) {
    const d = this.dungeon;
    const tx = Math.floor(x), tz = Math.floor(z);
    if (tx < 0 || tz < 0 || tx >= d.W || tz >= d.H) return true;
    const t = d.tiles[tz * d.W + tx];
    return t === T.VOID || t === T.PILLAR || t === T.CRATE || this.lockedTiles.has(tz * d.W + tx);
  }
  circleBlocked(x, z, r, fly) {
    for (let tz = Math.floor(z - r); tz <= Math.floor(z + r); tz++)
      for (let tx = Math.floor(x - r); tx <= Math.floor(x + r); tx++) {
        if (!this.tileBlocksMove(tx, tz, fly)) continue;
        const cx = clamp(x, tx, tx + 1), cz = clamp(z, tz, tz + 1);
        if ((x - cx) ** 2 + (z - cz) ** 2 < r * r) return true;
      }
    return false;
  }
  moveEntity(e, dx, dz, fly = false, radius) {
    const r = radius ?? e.r;
    const steps = Math.max(1, Math.ceil(Math.max(Math.abs(dx), Math.abs(dz)) / 0.15));
    const sx = dx / steps, sz = dz / steps;
    for (let i = 0; i < steps; i++) {
      if (sx && !this.circleBlocked(e.x + sx, e.z, r, fly)) e.x += sx;
      if (sz && !this.circleBlocked(e.x, e.z + sz, r, fly)) e.z += sz;
    }
  }
  roomAt(x, z) {
    const d = this.dungeon, tx = Math.floor(x), tz = Math.floor(z);
    if (tx < 0 || tz < 0 || tx >= d.W || tz >= d.H) return null;
    const id = d.roomGrid[tz * d.W + tx];
    return id >= 0 ? d.rooms[id] : null;
  }

  // ------------------------------------------------------------------ salas e combate
  updateRooms(dt) {
    const P = this.player;
    const room = this.roomAt(P.x, P.z);
    this.currentRoom = room;
    if (room && !room.visited) {
      room.visited = room.discovered = true;
      for (const n of room.neighbors) this.dungeon.rooms[n].discovered = true;
      if (room.type === 'treasure') this.toast('Sala do tesouro');
      if (room.type === 'shop') this.toast('Loja do Velho Armeiro', 'Gaste suas moedas');
    }
    if (room && !room.cleared && !room.active) {
      const m = 0.75;
      if (P.x > room.x0 + m && P.x < room.x0 + room.w - m && P.z > room.z0 + m && P.z < room.z0 + room.h - m) this.startRoom(room);
    }
    const ar = this.activeRoom;
    if (ar) {
      const alive = this.enemies.some((e) => !e.dead);
      if (!alive) {
        if (ar.wavesLeft > 0) {
          ar.waveDelay = (ar.waveDelay ?? 0.7) - dt;
          if (ar.waveDelay <= 0) { ar.waveDelay = 0.7; this.spawnWave(ar); }
        } else if (!ar.type.startsWith('boss')) this.clearRoom(ar);
      }
    }
  }

  startRoom(room) {
    room.active = true;
    this.activeRoom = room;
    this.world.setDoors(room, true);
    for (const door of room.doors) for (const [x, z] of door.tiles) this.lockedTiles.add(z * this.dungeon.W + x);
    sfx('door');
    if (room.type === 'boss') {
      const type = this.theme.boss;
      const b = new Boss(this, type, room.x0 + room.w / 2, room.z0 + room.h * 0.36, this.floorIndex);
      this.enemies.push(b);
      this.boss = b;
      this.hud.bossBar(BOSS_DEFS[type].name);
      this.toast(BOSS_DEFS[type].name, `Guardião do andar ${this.floorIndex + 1}`, 2.5);
      this.shake(0.5);
      sfx('boss');
      room.wavesLeft = 0;
    } else {
      const area = room.w * room.h;
      room.wavesLeft = 1 + (area > 170 ? 1 : 0) + (this.floorIndex >= 2 && this.rng.chance(0.4) ? 1 : 0);
      this.spawnWave(room);
    }
  }

  spawnWave(room) {
    room.wavesLeft--;
    const f = this.floorIndex;
    const n = 3 + Math.floor(f * 0.8) + this.rng.int(0, 2) + (room.w * room.h > 170 ? 1 : 0);
    const counts = {};
    for (let i = 0; i < n; i++) {
      let type = this.rng.weighted(this.theme.enemies);
      if ((type === 'torreta' || type === 'besouro' || type === 'cristal') && (counts[type] || 0) >= 2) type = this.theme.enemies[0][0];
      counts[type] = (counts[type] || 0) + 1;
      const pos = this.findSpawn(room, ENEMY_DEFS[type].fly);
      if (pos) this.enemies.push(new Enemy(this, type, pos[0], pos[1], f));
    }
  }

  findSpawn(room, fly) {
    const P = this.player;
    for (let k = 0; k < 60; k++) {
      const x = room.x0 + 1 + this.rng.float(0, room.w - 2), z = room.z0 + 1 + this.rng.float(0, room.h - 2);
      if (this.circleBlocked(x, z, 0.5, fly)) continue;
      if (Math.hypot(x - P.x, z - P.z) < 4) continue;
      if (this.enemies.some((e) => !e.dead && Math.hypot(e.x - x, e.z - z) < 1.3)) continue;
      return [x, z];
    }
    return null;
  }

  spawnEnemyNear(type, x, z) {
    const room = this.activeRoom;
    for (let k = 0; k < 20; k++) {
      const a = Math.random() * Math.PI * 2, d = 1.5 + Math.random() * 2.5;
      const nx = x + Math.cos(a) * d, nz = z + Math.sin(a) * d;
      if (room && this.roomAt(nx, nz) !== room) continue;
      if (this.circleBlocked(nx, nz, 0.5, ENEMY_DEFS[type].fly)) continue;
      this.enemies.push(new Enemy(this, type, nx, nz, this.floorIndex));
      return;
    }
  }

  clearRoom(room) {
    room.cleared = true; room.active = false;
    this.activeRoom = null;
    this.world.setDoors(room, false);
    this.lockedTiles.clear();
    sfx('door');
    this.enemies = this.enemies.filter((e) => !e.dead);
    // recompensa
    const P = this.player;
    const cx = room.x0 + room.w / 2, cz = room.z0 + room.h / 2;
    if (this.rng.chance(0.4)) {
      const w = P.weapon;
      const lowAmmo = w.ammo !== Infinity && w.ammo < w.def.ammo * 0.3;
      const type = lowAmmo && this.rng.chance(0.6) ? 'ammo' : this.rng.weighted([['ammo', 3], ['half', 3], ['heart', 1], ['blank', 2], ['coin', 3]]);
      if (type === 'coin') for (let i = 0; i < 4; i++) this.pickups.push(new Pickup(this, 'coin', cx, cz));
      else this.pickups.push(new Pickup(this, type, cx, cz));
    }
  }

  onEnemyKilled(e) {
    this.stats.kills++;
    if (e.boss) { this.onBossKilled(e); return; }
    const [a, b] = e.def.coins;
    const n = this.rng.int(a, b);
    for (let i = 0; i < n; i++) this.pickups.push(new Pickup(this, 'coin', e.x, e.z));
    if (this.rng.chance(0.035)) this.pickups.push(new Pickup(this, 'half', e.x, e.z));
    else if (this.rng.chance(0.05)) this.pickups.push(new Pickup(this, 'ammo', e.x, e.z));
  }

  onBossKilled(b) {
    const room = this.activeRoom;
    for (const e of this.enemies) if (!e.dead && e !== b) e.die();
    this.bullets.clearEnemy();
    this.hud.bossBar(null);
    this.boss = null;
    this.hitstop(0.35);
    this.flashScreen('#ffffff', 0.7);
    sfx('explode');
    room.cleared = true; room.active = false; this.activeRoom = null;
    this.world.setDoors(room, false);
    this.lockedTiles.clear();
    const P = this.player;
    P.maxHp = Math.min(P.maxHp + 2, 16);
    P.hp = P.maxHp;
    this.hud.dirty();
    const cx = room.x0 + room.w / 2, cz = room.z0 + room.h / 2;
    for (let i = 0; i < 10 + this.floorIndex * 3; i++) this.pickups.push(new Pickup(this, 'coin', b.x, b.z));
    if (this.floorIndex >= FLOORS - 1) {
      this.toast('Vitória!', 'O Altar do Último Tiro silenciou', 4);
      this.victoryT = 3;
      return;
    }
    this.toast(`${b.def.name} derrotado!`, '+1 coração máximo · vida restaurada', 3);
    this.interactables.push(new Chest(this, cx - 2.5, cz + 1.5, true));
    this.interactables.push(new Hatch(this, cx + 2, cz + 1.5));
  }

  nearestEnemy(x, z, maxD) {
    let best = null, bd = maxD * maxD;
    for (const e of this.enemies) {
      if (e.dead || !e.active) continue;
      const d = (e.x - x) ** 2 + (e.z - z) ** 2;
      if (d < bd) { bd = d; best = e; }
    }
    return best;
  }

  // ------------------------------------------------------------------ efeitos de jogo
  blank(x, z) {
    sfx('blank');
    this.bullets.clearEnemy(true);
    this.flashScreen('#9fe6ff', 0.55);
    this.shake(0.4);
    for (let i = 0; i < 40; i++) {
      const a = (i / 40) * Math.PI * 2;
      this.particles.add({ x, y: 0.4, z, vx: Math.cos(a) * 9, vz: Math.sin(a) * 9, vy: 0, grav: 0, color: 0x9fe6ff, life: 0.45, size: 0.14 });
    }
    this.particles.flash(x, 0.5, z, 8, 0x7fd6ff, 0.4);
    for (const e of this.enemies) {
      if (e.dead || !e.active) continue;
      const dx = e.x - x, dz = e.z - z, d = Math.hypot(dx, dz) || 1;
      if (d < 7) { e.damage(e.boss ? 18 : 8, dx, dz); if (!e.boss) { e.kx += (dx / d) * 6; e.kz += (dz / d) * 6; } }
    }
    this.stats.blanks = (this.stats.blanks || 0) + 1;
  }

  explosion(x, z, r, dmg) {
    sfx('explode', 0.05);
    this.shake(0.4);
    this.particles.flash(x, 0.5, z, r * 2.6, 0xffa040, 0.3);
    this.particles.burst(x, 0.4, z, 26, 0xff8a2a, 5, 0.5, 0.12, 3);
    this.particles.burst(x, 0.4, z, 12, 0x555555, 2.5, 0.8, 0.16, 2);
    for (const e of this.enemies) {
      if (e.dead || !e.active) continue;
      const d = Math.hypot(e.x - x, e.z - z);
      if (d < r + e.r) e.damage(dmg * (1 - (d / (r + e.r)) * 0.5), e.x - x, e.z - z);
    }
    for (let tz = Math.floor(z - r); tz <= Math.floor(z + r); tz++)
      for (let tx = Math.floor(x - r); tx <= Math.floor(x + r); tx++)
        if (Math.hypot(tx + 0.5 - x, tz + 0.5 - z) < r) this.hitCrate(tx, tz, 50);
  }

  hitCrate(tx, tz, dmg) {
    if (!this.world.crateIndex.has(tz * this.dungeon.W + tx)) return;
    this.particles.burst(tx + 0.5, 0.5, tz + 0.5, 2, 0x9a6a36, 2, 0.3);
    if (this.world.damageCrate(tx, tz, dmg)) {
      sfx('crate', 0.05);
      this.particles.burst(tx + 0.5, 0.4, tz + 0.5, 14, 0x9a6a36, 3.5, 0.7, 0.13, 3);
      if (this.rng.chance(0.18)) this.pickups.push(new Pickup(this, 'coin', tx + 0.5, tz + 0.5));
      else if (this.rng.chance(0.04)) this.pickups.push(new Pickup(this, 'ammo', tx + 0.5, tz + 0.5));
    }
  }

  openChest(chest) {
    sfx('chest');
    this.particles.burst(chest.x, 0.6, chest.z, 24, chest.rare ? 0x7fd6ff : 0xffd35a, 3.5, 0.7, 0.1, 4);
    const owned = this.player.weapons.map((w) => w.id);
    const wid = randomWeapon(this.rng, this.floorIndex + (chest.rare ? 2 : 0), owned);
    if (wid) this.pickups.push(new Pickup(this, 'weapon', chest.x, chest.z + 0.4, { weaponId: wid, vx: 0, vz: 2.5 }));
    else for (let i = 0; i < 3; i++) this.pickups.push(new Pickup(this, 'ammo', chest.x, chest.z + 0.4));
    if (chest.rare) { this.pickups.push(new Pickup(this, 'blank', chest.x, chest.z + 0.4)); this.pickups.push(new Pickup(this, 'heart', chest.x, chest.z + 0.4)); }
    for (let i = 0; i < 3; i++) this.pickups.push(new Pickup(this, 'coin', chest.x, chest.z + 0.3));
  }

  buy(item) {
    const P = this.player;
    if (item.sold) return;
    const it = item.item;
    if (P.coins < it.price) { sfx('deny'); this.toast('Moedas insuficientes', `Faltam ${it.price - P.coins}`); return; }
    if (it.type === 'heart' && P.hp >= P.maxHp) { sfx('deny'); this.toast('Sua vida já está cheia'); return; }
    P.coins -= it.price;
    item.sold = true;
    item.pickup.remove();
    sfx('buy');
    if (it.type === 'weapon') { P.addWeapon(it.weaponId); this.toast(WEAPONS[it.weaponId].name, 'Nova arma!'); }
    else this.applyPickup(it.type, true);
    this.hud.dirty();
  }

  applyPickup(type, force = false) {
    const P = this.player;
    switch (type) {
      case 'heart': if (P.hp >= P.maxHp && !force) return false; P.heal(2); sfx('heal'); break;
      case 'half': if (P.hp >= P.maxHp && !force) return false; P.heal(1); sfx('heal'); break;
      case 'blank': P.blanks = Math.min(P.blanks + 1, 9); sfx('pickup'); break;
      case 'coin': P.coins++; this.stats.coins++; sfx('coin', 0.02); break;
      case 'ammo':
        for (const w of P.weapons) if (w.ammo !== Infinity) {
          const full = w === P.weapon ? 1 : 0.5;
          w.ammo = Math.min(w.def.ammo, w.ammo + Math.ceil(w.def.ammo * full));
        }
        sfx('pickup'); this.toast('Munição reabastecida'); break;
      default: return false;
    }
    this.hud.dirty();
    return true;
  }

  descend() {
    if (this.descending) return;
    this.descending = true;
    sfx('stairs');
    this.flashScreen('#000000', 1);
    setTimeout(() => {
      this.loadFloor(this.floorIndex + 1);
      this.descending = false;
    }, 250);
  }

  onPlayerDeath() {
    this.state = 'dying';
    this.deathT = 1.6;
    this.flashScreen('#ff0030', 0.6);
    this.particles.burst(this.player.x, 0.5, this.player.z, 40, 0xff4d5e, 4, 1, 0.12, 4);
  }

  showEnd(victory) {
    this.state = victory ? 'victory' : 'over';
    this.hud.show(false);
    this.hud.bossBar(null);
    if (victory) sfx('victory');
    const s = this.stats;
    const t = Math.floor(s.time);
    $('over-title').textContent = victory ? 'Vitória! O último tiro foi seu.' : 'Você caiu…';
    $('over-stats').innerHTML = `
      <div>Andar alcançado: <b>${victory ? FLOORS : this.floorIndex + 1} / ${FLOORS}</b></div>
      <div>Inimigos derrotados: <b>${s.kills}</b></div>
      <div>Cambalhotas: <b>${s.rolls}</b> · Dano sofrido: <b>${s.hits}</b></div>
      <div>Moedas coletadas: <b>${s.coins}</b></div>
      <div>Tempo: <b>${Math.floor(t / 60)}m ${String(t % 60).padStart(2, '0')}s</b></div>`;
    $('screen-over').classList.remove('hidden');
  }

  // ------------------------------------------------------------------ utilidades de efeito
  shake(a) { this.shakeAmt = Math.min(1.2, Math.max(this.shakeAmt, a)); }
  hitstop(t) { this.stopT = Math.max(this.stopT, t); }
  flashScreen(c, o) { this.hud.flash(c, o); }
  toast(t, s, time) { this.hud.toast(t, s, time); }

  toScreen(x, y, z) {
    const v = new THREE.Vector3(x, y, z).project(this.camera);
    return { x: (v.x * 0.5 + 0.5) * innerWidth, y: (-v.y * 0.5 + 0.5) * innerHeight };
  }

  updateMouseWorld() {
    const m = this.input.mouse;
    const ndc = new THREE.Vector2((m.x / innerWidth) * 2 - 1, -(m.y / innerHeight) * 2 + 1);
    this.raycaster.setFromCamera(ndc, this.camera);
    this.raycaster.ray.intersectPlane(this.aimPlane, this.mouseWorld);
  }

  updateCamera(dt) {
    const P = this.player;
    let lx = 0, lz = 0;
    if (this.state === 'playing') {
      lx = (this.mouseWorld.x - P.x) * 0.22; lz = (this.mouseWorld.z - P.z) * 0.22;
      const l = Math.hypot(lx, lz);
      if (l > 2.6) { lx *= 2.6 / l; lz *= 2.6 / l; }
    }
    let tx = P.x + lx, tz = P.z + lz;
    if (this.state === 'title') { tx = P.x + Math.sin(this.time * 0.25) * 3; tz = P.z + Math.cos(this.time * 0.2) * 2; }
    const k = Math.min(1, dt * 7);
    this.camTarget.x += (tx - this.camTarget.x) * k;
    this.camTarget.z += (tz - this.camTarget.z) * k;
    const s = this.shakeAmt * 0.25;
    const ox = (Math.random() - 0.5) * s, oz = (Math.random() - 0.5) * s;
    this.shakeAmt = Math.max(0, this.shakeAmt - dt * 2.5);
    const c = this.camTarget;
    // afasta a câmera na luta contra o chefe para enxergar a arena inteira
    const want = this.boss && !this.boss.dead ? CAM_DIST + 4 : CAM_DIST;
    this.camDist = (this.camDist ?? CAM_DIST) + (want - (this.camDist ?? CAM_DIST)) * Math.min(1, dt * 2);
    const D = this.camDist;
    this.camera.position.set(c.x + ox, D * Math.sin(CAM_PITCH), c.z + D * Math.cos(CAM_PITCH) + oz);
    this.camera.lookAt(c.x + ox, 0, c.z + oz);
  }

  // ------------------------------------------------------------------ loop
  loop(now) {
    requestAnimationFrame((t) => this.loop(t));
    let dt = Math.min(0.05, (now - this.last) / 1000);
    this.last = now;
    this.time += dt;
    const inp = this.input;

    if ((inp.hit('escape') || inp.hit('p')) && (this.state === 'playing' || this.state === 'paused')) this.setPaused(this.state === 'playing');

    this.updateMouseWorld();
    const cross = $('crosshair');
    cross.style.left = inp.mouse.x + 'px'; cross.style.top = inp.mouse.y + 'px';
    cross.style.display = this.state === 'playing' ? 'block' : 'none';
    this.canvas.parentElement.style.cursor = this.state === 'playing' ? 'none' : 'default';

    if (this.state === 'playing' || this.state === 'dying') {
      if (this.stopT > 0) { this.stopT -= dt; dt = 0; }
      if (this.state === 'dying') {
        dt *= 0.3;
        this.deathT -= dt / 0.3;
        if (this.deathT <= 0) this.showEnd(false);
      }
      this.update(dt);
    } else if (this.state === 'title') {
      this.world.update(dt, this.time, this.player.x, this.player.z);
      this.particles.update(dt);
      this.player.updateVisual(dt);
    }
    this.updateCamera(Math.max(dt, 0.001));
    this.renderer.render(this.scene, this.camera);
    inp.endFrame();
  }

  update(dt) {
    const P = this.player;
    this.stats.time += dt;
    if (P.alive) P.update(dt);
    else P.group.visible = false;
    this.updateRooms(dt);
    for (const e of this.enemies) if (!e.dead) e.update(dt);
    if (this.enemies.length > 40) this.enemies = this.enemies.filter((e) => !e.dead);
    this.bullets.update(dt);
    this.particles.update(dt);
    this.world.update(dt, this.time, P.x, P.z);

    // itens
    for (const p of this.pickups) {
      if (p.dead) continue;
      p.update(dt);
      if (p.type === 'weapon') continue;
      if (P.alive && p.age > 0.35 && Math.hypot(p.x - P.x, p.z - P.z) < 0.6) {
        if (this.applyPickup(p.type)) p.remove();
      }
    }
    this.pickups = this.pickups.filter((p) => !p.dead);
    for (const p of this.pickupsStatic) if (!p.dead) p.update(dt);

    // interação
    let best = null, bd = Infinity;
    for (const it of this.interactables) {
      if (!it.prompt) continue;
      const d = Math.hypot(it.x - P.x, it.z - P.z);
      if (d < (it.r || 0.8) + 0.7 && d < bd) { bd = d; best = it; }
    }
    for (const p of this.pickups) {
      if (p.type !== 'weapon') continue;
      const d = Math.hypot(p.x - P.x, p.z - P.z);
      if (d < 1.2 && d < bd) {
        bd = d;
        best = { prompt: `<b>E</b> pegar ${WEAPONS[p.weaponId].name}`, use: () => { p.remove(); P.addWeapon(p.weaponId); sfx('pickup'); this.toast(WEAPONS[p.weaponId].name, 'Nova arma! (1–9 para trocar)'); } };
      }
    }
    this.hud.setPrompt(best && P.alive ? best.prompt : null);
    if (best && this.input.hit('e') && P.alive) best.use();

    if (this.boss && !this.boss.dead) this.hud.bossHp(this.boss.hp / this.boss.maxHp);
    if (this.victoryT !== undefined) {
      this.victoryT -= dt;
      if (this.victoryT <= 0) { this.victoryT = undefined; this.showEnd(true); }
    }
    this.hud.update(dt);
  }
}
