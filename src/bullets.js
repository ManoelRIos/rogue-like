// Sistema de projéteis (jogador e inimigos) renderizado com InstancedMesh.
import * as THREE from 'three';
import { TEX, CAM_PITCH, glowTexture } from './gfx.js';

const K = 1 / Math.cos(CAM_PITCH); // deixa as balas redondas na tela
const BULLET_Y = 0.42;

export class Bullets {
  constructor(scene, game) {
    this.game = game;
    this.list = [];
    this.enemyMesh = this.makeMesh(scene, TEX.bulletEnemy, 2500, false);
    this.playerMesh = this.makeMesh(scene, TEX.bulletPlayer, 1200, true);
    // brilho aditivo atrás das balas inimigas para leitura em ambientes escuros
    const glowMat = new THREE.MeshBasicMaterial({ map: glowTexture('#ff3366'), transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, opacity: 0.55 });
    this.glow = new THREE.InstancedMesh(new THREE.PlaneGeometry(1, 1), glowMat, 2500);
    this.glow.frustumCulled = false; this.glow.count = 0;
    scene.add(this.glow);
    this.m = new THREE.Matrix4();
    this.q = new THREE.Quaternion();
    this.s = new THREE.Vector3();
    this.p = new THREE.Vector3();
    this.c = new THREE.Color();
  }

  makeMesh(scene, tex, cap, colored) {
    const mat = new THREE.MeshBasicMaterial({ map: tex, alphaTest: 0.5 });
    const im = new THREE.InstancedMesh(new THREE.PlaneGeometry(1, 1), mat, cap);
    im.frustumCulled = false;
    im.count = 0;
    if (colored) { im.instanceColor = new THREE.InstancedBufferAttribute(new Float32Array(cap * 3), 3); }
    scene.add(im);
    return im;
  }

  spawn(b) {
    if (b.enemy && this.enemyCount() > 2200) return null;
    const o = {
      x: 0, z: 0, vx: 0, vz: 0, r: 0.14, life: 3, dmg: 1, enemy: true, pierce: 0, bounce: 0, homing: 0,
      explode: 0, color: 0xffffff, size: 1, accel: 0, curve: 0, hit: null, dead: false, age: 0, ...b,
    };
    if (!o.enemy) o.hit = new Set();
    this.list.push(o);
    return o;
  }

  enemyCount() { let n = 0; for (const b of this.list) if (b.enemy) n++; return n; }

  clearEnemy(fx = true) {
    for (const b of this.list) if (b.enemy && !b.dead) {
      b.dead = true;
      if (fx) this.game.particles.burst(b.x, BULLET_Y, b.z, 2, 0xff8fb0, 2, 0.3);
    }
  }

  update(dt) {
    const g = this.game;
    const P = g.player;
    for (const b of this.list) {
      if (b.dead) continue;
      b.age += dt;
      b.life -= dt;
      if (b.accel) { const f = 1 + b.accel * dt; b.vx *= f; b.vz *= f; }
      if (b.curve) { const a = b.curve * dt, c = Math.cos(a), s = Math.sin(a); const vx = b.vx * c - b.vz * s; b.vz = b.vx * s + b.vz * c; b.vx = vx; }
      if (b.homing && !b.enemy) {
        const t = g.nearestEnemy(b.x, b.z, 9);
        if (t) {
          const sp = Math.hypot(b.vx, b.vz);
          const want = Math.atan2(t.z - b.z, t.x - b.x);
          const cur = Math.atan2(b.vz, b.vx);
          let d = want - cur; d = Math.atan2(Math.sin(d), Math.cos(d));
          const na = cur + Math.max(-b.homing * dt, Math.min(b.homing * dt, d));
          b.vx = Math.cos(na) * sp; b.vz = Math.sin(na) * sp;
        }
      }
      if (b.life <= 0) { this.kill(b, false); continue; }

      // movimento com colisão em tiles (subdividido para balas rápidas)
      const steps = Math.max(1, Math.ceil((Math.hypot(b.vx, b.vz) * dt) / 0.3));
      const sdt = dt / steps;
      for (let s = 0; s < steps && !b.dead; s++) {
        const nx = b.x + b.vx * sdt, nz = b.z + b.vz * sdt;
        if (g.blocksBullet(nx, nz)) {
          const tx = Math.floor(nx), tz = Math.floor(nz);
          if (g.world.crateIndex.has(tz * g.dungeon.W + tx)) g.hitCrate(tx, tz, b.enemy ? 4 : b.dmg);
          if (b.bounce > 0) {
            b.bounce--;
            if (g.blocksBullet(nx, b.z)) b.vx = -b.vx;
            if (g.blocksBullet(b.x, nz)) b.vz = -b.vz;
            if (!g.blocksBullet(nx, b.z) && !g.blocksBullet(b.x, nz)) { b.vx = -b.vx; b.vz = -b.vz; }
            g.particles.burst(b.x, BULLET_Y, b.z, 3, b.color, 2, 0.2);
          } else { this.kill(b, true); }
          break;
        }
        b.x = nx; b.z = nz;

        if (b.enemy) {
          if (P.alive && P.vulnerable() && (b.x - P.x) ** 2 + (b.z - P.z) ** 2 < (b.r + P.hitR) ** 2) {
            P.hurt();
            this.kill(b, true);
          }
        } else {
          for (const e of g.enemies) {
            if (!e.active || e.dead || b.hit.has(e)) continue;
            if ((b.x - e.x) ** 2 + (b.z - e.z) ** 2 < (b.r + e.r) ** 2) {
              e.damage(b.dmg, b.vx, b.vz);
              b.hit.add(e);
              if (b.explode) { this.kill(b, true); break; }
              if (b.pierce > 0) b.pierce--; else { this.kill(b, true); break; }
            }
          }
        }
      }
    }
    // remove mortas
    if (this.list.length > 64) this.list = this.list.filter((b) => !b.dead);
    else for (let i = this.list.length - 1; i >= 0; i--) if (this.list[i].dead) this.list.splice(i, 1);
    this.render();
  }

  kill(b, impact) {
    if (b.dead) return;
    b.dead = true;
    const g = this.game;
    if (b.explode) g.explosion(b.x, b.z, b.explode, b.dmg);
    else if (impact) g.particles.burst(b.x, BULLET_Y, b.z, b.enemy ? 3 : 4, b.enemy ? 0xff6688 : b.color, 2.2, 0.25);
  }

  render() {
    let ne = 0, np = 0;
    const { m, q, s, p, c } = this;
    for (const b of this.list) {
      const sz = b.size * (b.enemy ? 0.42 : 0.3);
      p.set(b.x, BULLET_Y, b.z + 0.05);
      s.set(sz, sz * K, 1);
      m.compose(p, q, s);
      if (b.enemy) {
        this.enemyMesh.setMatrixAt(ne, m);
        s.set(sz * 2.6, sz * 2.6 * K, 1); p.z -= 0.02; m.compose(p, q, s);
        this.glow.setMatrixAt(ne, m);
        ne++;
      } else {
        this.playerMesh.setMatrixAt(np, m);
        c.setHex(b.color);
        this.playerMesh.setColorAt(np, c);
        np++;
      }
    }
    this.enemyMesh.count = ne; this.glow.count = ne; this.playerMesh.count = np;
    this.enemyMesh.instanceMatrix.needsUpdate = true;
    this.glow.instanceMatrix.needsUpdate = true;
    this.playerMesh.instanceMatrix.needsUpdate = true;
    if (this.playerMesh.instanceColor) this.playerMesh.instanceColor.needsUpdate = true;
  }

  reset() { this.list.length = 0; this.render(); }
}
