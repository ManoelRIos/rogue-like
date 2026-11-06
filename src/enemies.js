// Inimigos comuns e chefes, com padrões de balas.
import * as THREE from 'three';
import { makeSprite, makeShadow, TEX } from './gfx.js';
import { sfx } from './audio.js';
import { TAU } from './util.js';

export const ENEMY_DEFS = {
  gosma: { name: 'Gosma', hp: 15, speed: 1.9, r: 0.4, ai: 'keep', keep: 4, cd: [1.7, 2.6], fire: 'single', color: 0x5fbf4a, coins: [0, 2] },
  cogumelo: { name: 'Cogumelo', hp: 19, speed: 1.3, r: 0.4, ai: 'keep', keep: 5, cd: [2.4, 3.2], fire: 'spread5', color: 0xc8413d, coins: [1, 2] },
  caveira: { name: 'Caveira', hp: 24, speed: 2.3, r: 0.38, ai: 'keep', keep: 5.5, cd: [2.0, 2.8], fire: 'burst3', color: 0xe9e2cf, coins: [1, 3] },
  morcego: { name: 'Morcego', hp: 10, speed: 4.3, r: 0.36, ai: 'swoop', fly: true, cd: [3.0, 4.2], fire: 'ring6', color: 0x6b4a9a, coins: [0, 1] },
  lanterna: { name: 'Lanterna', hp: 22, speed: 1.4, r: 0.38, ai: 'keep', keep: 6, fly: true, cd: [2.6, 3.4], fire: 'ring10', color: 0xffbf3b, coins: [1, 2] },
  torreta: { name: 'Totem', hp: 38, speed: 0, r: 0.45, ai: 'still', cd: [1.4, 2.0], fire: 'spiral', color: 0x7d7690, coins: [1, 3] },
  cristal: { name: 'Cristal', hp: 26, speed: 0, r: 0.36, ai: 'blink', fly: true, cd: [2.6, 3.3], fire: 'lines', color: 0x5ad1e6, coins: [1, 3] },
  besouro: { name: 'Besouro', hp: 48, speed: 1.3, r: 0.48, ai: 'chase', cd: [2.4, 3.0], fire: 'shotgun', color: 0x2d4a8a, coins: [2, 4] },
};

export const BOSS_DEFS = {
  reiGosma: { name: 'Rei Gosma', hp: 560, r: 1.0, scale: 1, speed: 1.4, minion: 'gosma', attacks: ['fan', 'ring', 'hop', 'summon', 'hop', 'fan'], color: 0x62c44a },
  sinoFantasma: { name: 'Sino Fantasma', hp: 700, r: 0.95, scale: 1, speed: 1.6, fly: true, minion: 'lanterna', attacks: ['spiral', 'bell', 'teleport', 'wave', 'bell'], color: 0xc9bfe8 },
  golem: { name: 'Golem da Forja', hp: 880, r: 1.05, scale: 1, speed: 1.2, attacks: ['charge', 'shotgun', 'slam', 'bigshot', 'charge'], color: 0x7c7266 },
  aranha: { name: 'Aranha Relojoeira', hp: 900, r: 1.1, scale: 1, speed: 2.0, minion: 'morcego', attacks: ['clock', 'spray', 'summon', 'fan', 'wave', 'clock'], color: 0xc99a3b },
  sacerdote: { name: 'Sacerdote do Último Tiro', hp: 1150, r: 0.9, scale: 1, speed: 1.6, fly: true, minion: 'caveira', attacks: ['cross', 'rain', 'teleport', 'bell', 'spiral', 'clock', 'summon', 'wave'], color: 0x7a1f34 },
};

const rand = (a, b) => a + Math.random() * (b - a);

export class Enemy {
  constructor(game, type, x, z, floor, opts = {}) {
    this.g = game;
    this.type = type;
    this.def = opts.boss ? BOSS_DEFS[type] : ENEMY_DEFS[type];
    this.boss = !!opts.boss;
    this.floor = floor;
    const hpScale = 1 + floor * 0.28;
    this.maxHp = this.def.hp * hpScale;
    this.hp = this.maxHp;
    this.r = this.def.r;
    this.x = x; this.z = z;
    this.vx = 0; this.vz = 0; this.kx = 0; this.kz = 0;
    this.dead = false;
    this.active = false;
    this.spawnT = opts.instant ? 0 : 0.8;
    this.cd = rand(0.9, 1.7);
    this.windup = 0;
    this.queue = [];
    this.flash = 0;
    this.t = Math.random() * 10;
    this.strafe = Math.random() < 0.5 ? 1 : -1;
    this.strafeT = rand(1, 3);
    this.bulletSpeed = 1 + floor * 0.07;
    this.fireRate = 1 - floor * 0.06;
    this.fly = !!this.def.fly;
    this.spiralA = Math.random() * TAU;

    // visual
    this.group = new THREE.Group();
    const frames = opts.boss ? TEX.bosses[type] : TEX.enemies[type];
    this.sprite = makeSprite(frames, { scale: this.def.scale || 1 });
    this.baseScaleY = this.sprite.scale.y;
    this.group.add(this.sprite);
    this.shadow = makeShadow(this.r * 2.4);
    this.group.add(this.shadow);
    this.group.position.set(x, 0, z);
    this.sprite.visible = false;
    game.scene.add(this.group);
    if (!opts.instant) game.particles.flash(x, 0.4, z, 1.6, 0xff4060, 0.8);
  }

  get frames() { return this.sprite.userData.frames; }

  damage(dmg, vx = 0, vz = 0) {
    if (this.dead || !this.active || this.invulnerable) return;
    this.hp -= dmg;
    this.flash = 1;
    const l = Math.hypot(vx, vz) || 1;
    if (!this.boss) { this.kx += (vx / l) * 2.2; this.kz += (vz / l) * 2.2; }
    sfx('hit', 0.04);
    this.g.particles.burst(this.x, 0.4, this.z, 3, this.def.color, 2.5, 0.3);
    if (this.hp <= 0) this.die();
  }

  die() {
    this.dead = true;
    sfx('enemyDie', 0.05);
    this.g.particles.burst(this.x, 0.45, this.z, this.boss ? 60 : 16, this.def.color, this.boss ? 6 : 3.5, 0.8, 0.12, 3);
    this.g.particles.burst(this.x, 0.45, this.z, this.boss ? 20 : 6, 0xffffff, 3, 0.4, 0.07, 2);
    this.g.particles.flash(this.x, 0.5, this.z, this.boss ? 6 : 2, 0xffe0b0, 0.25);
    this.g.shake(this.boss ? 0.8 : 0.12);
    this.g.scene.remove(this.group);
    this.g.onEnemyKilled(this);
  }

  remove() { this.dead = true; this.g.scene.remove(this.group); }

  // --------------------------------------------------------- disparos
  shot(angle, speed, o = {}) {
    const sp = speed * this.bulletSpeed;
    this.g.bullets.spawn({
      x: (o.x ?? this.x) + Math.cos(angle) * (o.off ?? this.r * 0.8),
      z: (o.z ?? this.z) + Math.sin(angle) * (o.off ?? this.r * 0.8),
      vx: Math.cos(angle) * sp, vz: Math.sin(angle) * sp, enemy: true,
      r: 0.14 * (o.size || 1), size: o.size || 1, life: o.life || 6, accel: o.accel || 0, curve: o.curve || 0,
    });
  }
  aimAt() { const P = this.g.player; return Math.atan2(P.z - this.z, P.x - this.x); }
  fan(n, spread, speed, base = this.aimAt(), o) {
    for (let i = 0; i < n; i++) this.shot(base + (n > 1 ? (i / (n - 1) - 0.5) * spread : 0), speed, o);
  }
  ring(n, speed, off = 0, o) { for (let i = 0; i < n; i++) this.shot(off + (i / n) * TAU, speed, o); }
  later(t, fn) { this.queue.push({ t, fn }); }

  fire() {
    sfx('enemyShoot', 0.05);
    switch (this.def.fire) {
      case 'single': this.fan(1, 0, 5.5); if (this.floor >= 2) this.later(0.18, () => this.fan(1, 0, 5.5)); break;
      case 'spread5': this.fan(5, 0.9, 4.8); break;
      case 'burst3': for (let i = 0; i < 3; i++) this.later(i * 0.14, () => this.fan(this.floor >= 3 ? 2 : 1, 0.15, 6.4)); break;
      case 'ring6': this.ring(6, 4, Math.random()); break;
      case 'ring10': this.ring(10 + this.floor, 4.2, this.spiralA); this.spiralA += 0.3; break;
      case 'spiral': for (let i = 0; i < 18; i++) this.later(i * 0.07, () => { this.spiralA += 0.38; this.shot(this.spiralA, 4.4); this.shot(this.spiralA + Math.PI, 4.4); }); break;
      case 'lines': { const a = this.aimAt(); for (let k = 0; k < 3; k++) this.later(k * 0.12, () => this.fan(3, 0.5, 6.5 - k * 0.6, a)); break; }
      case 'shotgun': { const a = this.aimAt(); for (let i = 0; i < 9; i++) this.shot(a + rand(-0.55, 0.55), rand(3.6, 7)); break; }
    }
  }

  // --------------------------------------------------------- IA
  update(dt) {
    const g = this.g, P = g.player;
    this.t += dt;
    this.flash = Math.max(0, this.flash - dt * 6);

    if (!this.active) {
      this.spawnT -= dt;
      if (this.spawnT <= 0) { this.active = true; this.sprite.visible = true; this.flash = 1; }
      this.updateVisual(dt);
      return;
    }

    // fila de tiros agendados
    for (let i = this.queue.length - 1; i >= 0; i--) {
      const q = this.queue[i];
      q.t -= dt;
      if (q.t <= 0) { this.queue.splice(i, 1); if (!this.dead) q.fn(); }
    }

    const dx = P.x - this.x, dz = P.z - this.z;
    const d = Math.hypot(dx, dz) || 1;
    const nx = dx / d, nz = dz / d;
    let mx = 0, mz = 0;
    const sp = this.def.speed;
    this.strafeT -= dt;
    if (this.strafeT <= 0) { this.strafe *= -1; this.strafeT = rand(1.2, 3); }

    switch (this.def.ai) {
      case 'keep': {
        const k = this.def.keep;
        if (d > k + 1) { mx = nx; mz = nz; }
        else if (d < k - 1.2) { mx = -nx; mz = -nz; }
        else { mx = -nz * this.strafe * 0.7; mz = nx * this.strafe * 0.7; }
        break;
      }
      case 'chase': mx = nx; mz = nz; break;
      case 'swoop': {
        const s = Math.sin(this.t * 2.2) * 0.9;
        mx = nx - nz * s; mz = nz + nx * s;
        break;
      }
      case 'blink':
        this.blinkT = (this.blinkT ?? rand(2, 3)) - dt;
        if (this.blinkT <= 0) { this.blinkT = rand(2.6, 3.6); this.blink(); }
        break;
      default: break;
    }
    // separação entre inimigos
    for (const o of g.enemies) {
      if (o === this || o.dead) continue;
      const ox = this.x - o.x, oz = this.z - o.z, od = ox * ox + oz * oz, rr = (this.r + o.r) * 1.1;
      if (od < rr * rr && od > 0.0001) { const l = Math.sqrt(od); mx += (ox / l) * 0.8; mz += (oz / l) * 0.8; }
    }
    const ml = Math.hypot(mx, mz);
    if (ml > 1) { mx /= ml; mz /= ml; }
    this.vx += (mx * sp - this.vx) * Math.min(1, dt * 6);
    this.vz += (mz * sp - this.vz) * Math.min(1, dt * 6);
    g.moveEntity(this, (this.vx + this.kx) * dt, (this.vz + this.kz) * dt, this.fly);
    this.kx *= Math.pow(0.002, dt); this.kz *= Math.pow(0.002, dt);

    // dano de contato
    if (P.vulnerable() && d < this.r + P.hitR + 0.05) P.hurt();

    // ciclo de ataque com aviso visual (windup)
    if (this.windup > 0) {
      this.windup -= dt;
      if (this.windup <= 0) this.fire();
    } else {
      this.cd -= dt;
      if (this.cd <= 0 && d < 16) {
        this.windup = 0.3;
        this.cd = rand(...this.def.cd) * this.fireRate;
      }
    }
    this.updateVisual(dt);
  }

  blink() {
    const g = this.g;
    const room = g.currentRoom;
    if (!room) return;
    for (let i = 0; i < 20; i++) {
      const x = room.x0 + 1 + Math.random() * (room.w - 2), z = room.z0 + 1 + Math.random() * (room.h - 2);
      if (g.blocksMove(x, z, true)) continue;
      if (Math.hypot(x - g.player.x, z - g.player.z) < 3.5) continue;
      g.particles.burst(this.x, 0.5, this.z, 10, this.def.color, 2, 0.4);
      this.x = x; this.z = z;
      g.particles.burst(x, 0.5, z, 10, this.def.color, 2, 0.4);
      return;
    }
  }

  updateVisual() {
    const P = this.g.player;
    const fl = this.fly ? 0.35 + Math.sin(this.t * 3) * 0.08 : 0;
    this.group.position.set(this.x, 0, this.z);
    this.sprite.position.y = fl;
    this.sprite.scale.x = (P.x < this.x ? -1 : 1) * (this.boss ? 1 : 1);
    if (!this.active) {
      const k = 1 - Math.max(0, this.spawnT) / 0.8;
      this.shadow.scale.setScalar(k);
    } else this.shadow.scale.setScalar(1);
    const frame = Math.floor(this.t * (this.def.ai === 'swoop' ? 10 : 4)) % 2;
    this.sprite.material.uniforms.map.value = this.frames[frame];
    const wu = this.windup > 0 ? 0.35 + Math.sin(this.t * 40) * 0.25 : 0;
    this.sprite.material.uniforms.flash.value = Math.max(this.flash * 0.9, wu);
    // leve "squash" ao andar
    const sq = 1 + Math.sin(this.t * 8) * 0.03;
    this.sprite.scale.y = this.baseScaleY * sq;
  }
}

// ====================================================================== CHEFE
export class Boss extends Enemy {
  constructor(game, type, x, z, floor) {
    super(game, type, x, z, floor, { boss: true, instant: true });
    this.maxHp = this.def.hp * (1 + floor * 0.12);
    this.hp = this.maxHp;
    this.intro = 1.6;
    this.active = true;
    this.sprite.visible = true;
    this.attackIdx = 0;
    this.attack = null;
    this.idle = 1.0;
    this.alpha = 1;
    this.bulletSpeed = 1 + floor * 0.05;
    this.order = [...this.def.attacks];
  }

  get phase2() { return this.hp < this.maxHp * 0.5; }

  fire() { /* chefes usam ataques próprios */ }

  update(dt) {
    const g = this.g, P = g.player;
    this.t += dt;
    this.flash = Math.max(0, this.flash - dt * 6);
    if (this.intro > 0) { this.intro -= dt; this.updateVisual(dt); return; }

    for (let i = this.queue.length - 1; i >= 0; i--) {
      const q = this.queue[i]; q.t -= dt;
      if (q.t <= 0) { this.queue.splice(i, 1); if (!this.dead) q.fn(); }
    }

    // deslocamento lento mantendo distância
    if (!this.attack || !this.attack.lockMove) {
      const dx = P.x - this.x, dz = P.z - this.z, d = Math.hypot(dx, dz) || 1;
      let mx = 0, mz = 0;
      if (d > 6.5) { mx = dx / d; mz = dz / d; } else if (d < 4) { mx = -dx / d; mz = -dz / d; }
      else { mx = -dz / d * this.strafe * 0.5; mz = dx / d * this.strafe * 0.5; }
      this.strafeT -= dt;
      if (this.strafeT <= 0) { this.strafe *= -1; this.strafeT = rand(2, 4); }
      const sp = this.def.speed * (this.phase2 ? 1.25 : 1);
      g.moveEntity(this, mx * sp * dt, mz * sp * dt, this.fly);
    }
    if (P.vulnerable() && Math.hypot(P.x - this.x, P.z - this.z) < this.r + P.hitR) P.hurt();

    if (this.attack) {
      this.attack.t += dt;
      if (ATTACKS[this.attack.name](this, dt, this.attack)) {
        this.attack = null;
        this.idle = (this.phase2 ? rand(0.4, 0.8) : rand(0.8, 1.3)) - this.floor * 0.05;
      }
    } else {
      this.idle -= dt;
      if (this.idle <= 0) {
        const name = this.order[this.attackIdx % this.order.length];
        this.attackIdx++;
        if (this.attackIdx % this.order.length === 0) this.order.sort(() => Math.random() - 0.5);
        this.attack = { name, t: 0, n: 0 };
      }
    }
    this.updateVisual(dt);
  }

  updateVisual(dt) {
    super.updateVisual(dt);
    const hop = this.attack?.hopY || 0;
    this.sprite.position.y = (this.fly ? 0.3 + Math.sin(this.t * 2) * 0.12 : 0) + hop;
    this.sprite.material.uniforms.opacity.value = 1;
    this.sprite.visible = this.alpha > 0.5 || Math.floor(this.t * 20) % 2 === 0;
    if (this.phase2) this.sprite.material.uniforms.tint.value.setRGB(1, 0.82, 0.82);
    if (this.attack?.windup) this.sprite.material.uniforms.flash.value = Math.max(this.flash, 0.3 + Math.sin(this.t * 40) * 0.25);
  }
}

// ---------------------------------------------------------------- ataques dos chefes
// Cada ataque roda por frame e retorna true quando termina.
function every(st, key, interval, fn) {
  if (st[key + 'T'] === undefined) { st[key + 'T'] = interval; } // dispara já no primeiro frame
  st[key + 'T'] = (st[key + 'T'] ?? 0) + st.dt;
  while (st[key + 'T'] >= interval) { st[key + 'T'] -= interval; fn(); }
}

const ATTACKS = {
  fan(b, dt, st) {
    st.dt = dt;
    const waves = b.phase2 ? 4 : 3;
    every(st, 'f', 0.38, () => { if (st.n < waves) { b.fan(b.phase2 ? 9 : 7, 1.1, 6); sfx('enemyShoot'); st.n++; } });
    return st.n >= waves && st.t > waves * 0.38 + 0.2;
  },
  ring(b, dt, st) {
    st.dt = dt;
    every(st, 'r', 0.45, () => { if (st.n < 3) { b.ring(b.phase2 ? 24 : 18, 4.6, st.n * 0.17); sfx('enemyShoot'); st.n++; } });
    return st.n >= 3;
  },
  hop(b, dt, st) {
    const P = b.g.player;
    st.lockMove = true;
    if (st.n === 0) { st.n = 1; st.sx = b.x; st.sz = b.z; st.tx = P.x; st.tz = P.z; st.windup = true; }
    if (st.t < 0.35) return false;
    st.windup = false;
    const k = Math.min(1, (st.t - 0.35) / 0.8);
    const nx = st.sx + (st.tx - st.sx) * k, nz = st.sz + (st.tz - st.sz) * k;
    b.g.moveEntity(b, nx - b.x, nz - b.z, false);
    st.hopY = Math.sin(k * Math.PI) * 2.2;
    b.invulnerable = k < 0.85;
    if (k >= 1) {
      st.hopY = 0; b.invulnerable = false;
      b.ring(b.phase2 ? 26 : 20, 4.2, Math.random());
      b.later(0.25, () => b.ring(14, 3, Math.random()));
      b.g.shake(0.5); sfx('explode');
      b.g.particles.burst(b.x, 0.1, b.z, 30, b.def.color, 5, 0.6);
      return true;
    }
    return false;
  },
  summon(b, dt, st) {
    if (st.n === 0) {
      st.n = 1;
      const alive = b.g.enemies.filter((e) => !e.dead && !e.boss).length;
      const n = Math.max(0, Math.min(2 + (b.phase2 ? 1 : 0), 5 - alive));
      for (let i = 0; i < n; i++) b.g.spawnEnemyNear(b.def.minion, b.x, b.z);
      b.ring(12, 3.5, 0);
    }
    return st.t > 0.6;
  },
  spiral(b, dt, st) {
    st.dt = dt;
    const arms = b.phase2 ? 4 : 3;
    every(st, 's', 0.075, () => {
      st.a = (st.a ?? 0) + 0.21;
      for (let i = 0; i < arms; i++) b.shot(st.a + (i / arms) * TAU, 4.3);
    });
    if (Math.floor(st.t * 8) !== st.snd) { st.snd = Math.floor(st.t * 8); sfx('enemyShoot'); }
    return st.t > 3;
  },
  bell(b, dt, st) {
    st.dt = dt;
    every(st, 'b', 0.6, () => {
      if (st.n >= 3) return;
      const n = 30, gapAt = b.aimAt() + rand(-0.3, 0.3);
      for (let i = 0; i < n; i++) {
        const a = (i / n) * TAU;
        let diff = Math.abs(Math.atan2(Math.sin(a - gapAt), Math.cos(a - gapAt)));
        if (diff < 0.42) continue;
        b.shot(a, 3.8);
      }
      sfx('enemyShoot'); st.n++;
    });
    return st.n >= 3 && st.t > 2;
  },
  teleport(b, dt, st) {
    st.lockMove = true;
    if (st.n === 0) { st.n = 1; b.alpha = 0; }
    if (st.n === 1 && st.t > 0.45) {
      st.n = 2;
      b.blink();
      b.alpha = 1;
      b.fan(b.phase2 ? 11 : 9, 1.4, 5.5);
      b.later(0.3, () => b.fan(b.phase2 ? 10 : 8, 1.3, 4.5));
      sfx('enemyShoot');
    }
    return st.n === 2 && st.t > 1.1;
  },
  wave(b, dt, st) {
    st.dt = dt;
    every(st, 'w', 0.16, () => {
      if (st.n >= 12) return;
      const a = b.aimAt();
      const s = st.n % 2 ? 1 : -1;
      b.shot(a + s * 0.5, 5, { curve: -s * 0.9 });
      b.shot(a - s * 0.5, 5, { curve: s * 0.9 });
      if (b.phase2) b.shot(a, 6);
      st.n++;
    });
    return st.n >= 12;
  },
  charge(b, dt, st) {
    st.lockMove = true;
    const g = b.g;
    if (st.n === 0) { st.n = 1; st.a = b.aimAt(); st.windup = true; }
    if (st.t < 0.6) { st.a = b.aimAt(); return false; }
    st.windup = false;
    const sp = b.phase2 ? 15 : 12.5;
    const ox = b.x, oz = b.z;
    g.moveEntity(b, Math.cos(st.a) * sp * dt, Math.sin(st.a) * sp * dt, false);
    if (Math.random() < 0.5) g.particles.add({ x: b.x, y: 0.1, z: b.z, vy: 1, color: 0x8a7a66, life: 0.4, size: 0.15 });
    const moved = Math.hypot(b.x - ox, b.z - oz);
    if (moved < sp * dt * 0.5 || st.t > 2) {
      g.shake(0.7); sfx('explode');
      b.ring(b.phase2 ? 22 : 16, 4, Math.random());
      for (let i = 0; i < 8; i++) b.shot(st.a + Math.PI + rand(-0.8, 0.8), rand(3, 6));
      g.particles.burst(b.x, 0.3, b.z, 25, 0xa59a8b, 5, 0.6, 0.14);
      return true;
    }
    return false;
  },
  shotgun(b, dt, st) {
    st.dt = dt;
    every(st, 's', 0.55, () => {
      if (st.n >= 3) return;
      const a = b.aimAt();
      for (let i = 0; i < (b.phase2 ? 18 : 13); i++) b.shot(a + rand(-0.7, 0.7), rand(3.5, 7.5));
      sfx('shotgun'); st.n++;
    });
    return st.n >= 3 && st.t > 1.5;
  },
  slam(b, dt, st) {
    st.dt = dt;
    if (st.t < 0.5) { st.windup = true; return false; }
    st.windup = false;
    every(st, 'm', 0.28, () => {
      if (st.n >= 4) return;
      b.ring(24, 3.6 + st.n * 0.4, (st.n % 2) * (Math.PI / 24));
      b.g.shake(0.3); sfx('explode', 0.1); st.n++;
    });
    return st.n >= 4;
  },
  bigshot(b, dt, st) {
    st.dt = dt;
    every(st, 'g', 0.5, () => {
      if (st.n >= 4) return;
      b.shot(b.aimAt(), 3.6, { size: 2.4, accel: 0.6 });
      if (b.phase2) { b.shot(b.aimAt() + 0.6, 3.4, { size: 1.8 }); b.shot(b.aimAt() - 0.6, 3.4, { size: 1.8 }); }
      sfx('heavy'); st.n++;
    });
    return st.n >= 4 && st.t > 2.2;
  },
  clock(b, dt, st) {
    st.dt = dt;
    st.lockMove = true;
    const hands = b.phase2 ? 3 : 2;
    every(st, 'c', 0.07, () => {
      st.a1 = (st.a1 ?? Math.random() * TAU) + 0.11;
      for (let i = 0; i < hands; i++) b.shot(st.a1 + (i / hands) * TAU, 5.2);
      b.shot(-st.a1 * 0.7, 3.4);
    });
    if (Math.floor(st.t * 6) !== st.snd) { st.snd = Math.floor(st.t * 6); sfx('enemyShoot'); }
    return st.t > 3.4;
  },
  spray(b, dt, st) {
    st.dt = dt;
    every(st, 'p', b.phase2 ? 0.03 : 0.045, () => b.shot(b.aimAt() + rand(-1, 1), rand(4, 7)));
    if (Math.floor(st.t * 8) !== st.snd) { st.snd = Math.floor(st.t * 8); sfx('enemyShoot'); }
    return st.t > 1.8;
  },
  cross(b, dt, st) {
    st.dt = dt;
    const arms = b.phase2 ? 8 : 4;
    every(st, 'x', 0.09, () => {
      st.a = (st.a ?? 0) + (b.phase2 ? 0.09 : 0.07);
      for (let i = 0; i < arms; i++) b.shot(st.a + (i / arms) * TAU, 4.8);
    });
    if (Math.floor(st.t * 6) !== st.snd) { st.snd = Math.floor(st.t * 6); sfx('enemyShoot'); }
    return st.t > 3.2;
  },
  rain(b, dt, st) {
    st.dt = dt;
    const r = b.g.currentRoom;
    if (!r) return true;
    every(st, 'r', b.phase2 ? 0.06 : 0.09, () => {
      const x = r.x0 + 0.5 + Math.random() * (r.w - 1);
      b.shot(Math.PI / 2 + rand(-0.15, 0.15), rand(3.6, 5.2), { x, z: r.z0 + 0.4, off: 0 });
    });
    return st.t > 2.6;
  },
};
