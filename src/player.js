// Jogador: movimento, cambalhota (roll) com invulnerabilidade, armas e blanks.
import * as THREE from 'three';
import { makeSprite, makeShadow, setFrame, spriteGeo, TEX, PX, CAM_PITCH } from './gfx.js';
import { makeWeapon, WEAPONS } from './weapons.js';
import { sfx } from './audio.js';

const K = 1 / Math.cos(CAM_PITCH);
const ROLL_TIME = 0.5;
const ROLL_IFRAMES = 0.38;
const SPEED = 5.4;

export class Player {
  constructor(game) {
    this.g = game;
    this.x = 0; this.z = 0; this.vx = 0; this.vz = 0;
    this.r = 0.3; this.hitR = 0.18;
    this.maxHp = 6; this.hp = 6;
    this.blanks = 2; this.coins = 0;
    this.weapons = [makeWeapon('pistola')];
    this.cur = 0;
    this.fireCd = 0; this.reloadT = 0;
    this.roll = null; this.rollCd = 0;
    this.invuln = 0; this.alive = true;
    this.aim = 0; this.screenAim = 0;
    this.walkT = 0; this.kick = 0; this.flash = 0;

    // ---- visual
    this.group = new THREE.Group();
    this.shadow = makeShadow(0.8);
    this.group.add(this.shadow);
    this.body = makeSprite(TEX.player);
    this.group.add(this.body);

    this.gunGroup = new THREE.Group();
    this.gunGroup.scale.y = K;
    this.gunGroup.position.set(0, 0.42, 0.04);
    this.gunPivot = new THREE.Group();
    this.gunGroup.add(this.gunPivot);
    this.gunMesh = makeSprite(TEX.weapons.pistola);
    this.gunMesh.scale.y = 1;
    this.gunPivot.add(this.gunMesh);
    this.group.add(this.gunGroup);

    this.rollGroup = new THREE.Group();
    this.rollGroup.scale.y = K;
    this.rollGroup.position.y = 0.42;
    this.rollMesh = makeSprite(TEX.playerRoll, { anchorY: 0.5 });
    this.rollMesh.scale.y = 1;
    this.rollGroup.add(this.rollMesh);
    this.rollGroup.visible = false;
    this.group.add(this.rollGroup);

    // luz que acompanha o jogador
    this.light = new THREE.PointLight(0xffe2c0, 2.4, 9, 1.2);
    this.light.position.set(0, 1.6, 0.6);
    this.group.add(this.light);

    this.refreshGun();
  }

  get weapon() { return this.weapons[this.cur]; }

  refreshGun() {
    const id = this.weapon.id;
    const pix = TEX.weaponPix[id];
    const w = pix.w * PX, h = pix.h * PX;
    this.gunMesh.geometry = spriteGeo(w, h, 3 / pix.w, 0.5);
    this.gunMesh.material.uniforms.map.value = TEX.weapons[id][0];
    this.reloadT = 0;
    this.g.hud?.dirty();
  }

  addWeapon(id) {
    const have = this.weapons.findIndex((w) => w.id === id);
    if (have >= 0) { // arma repetida: enche munição
      const w = this.weapons[have];
      if (w.ammo !== Infinity) w.ammo = WEAPONS[id].ammo;
      this.cur = have;
    } else {
      this.weapons.push(makeWeapon(id));
      this.cur = this.weapons.length - 1;
    }
    this.refreshGun();
  }

  switchWeapon(dir) {
    if (this.weapons.length < 2) return;
    this.cur = (this.cur + dir + this.weapons.length) % this.weapons.length;
    this.refreshGun();
  }

  vulnerable() {
    if (this.g.godMode) return false;
    if (this.roll && this.roll.t < ROLL_IFRAMES) return false;
    return this.invuln <= 0 && this.alive;
  }

  hurt() {
    if (!this.vulnerable()) return;
    this.hp -= 1;
    this.invuln = 1.25;
    this.flash = 1;
    sfx('hurt', 0.1);
    this.g.shake(0.35);
    this.g.hitstop(0.09);
    this.g.flashScreen('#ff2040', 0.25);
    this.g.stats.hits++;
    // limpa balas muito próximas para evitar dano em sequência
    for (const b of this.g.bullets.list) if (b.enemy && (b.x - this.x) ** 2 + (b.z - this.z) ** 2 < 1.6 ** 2) b.dead = true;
    this.g.hud.dirty();
    if (this.hp <= 0) { this.alive = false; this.g.onPlayerDeath(); }
  }

  heal(n) { this.hp = Math.min(this.maxHp, this.hp + n); this.g.hud.dirty(); }

  update(dt) {
    const g = this.g, inp = g.input;
    this.fireCd -= dt; this.rollCd -= dt; this.invuln -= dt;
    this.flash = Math.max(0, this.flash - dt * 5);
    this.kick = Math.max(0, this.kick - dt * 10);

    // mira em coordenadas do mundo e da tela
    const mw = g.mouseWorld;
    this.aim = Math.atan2(mw.z - this.z, mw.x - this.x);
    const sp = g.toScreen(this.x, 0.55, this.z);
    this.screenAim = Math.atan2(-(inp.mouse.y - sp.y), inp.mouse.x - sp.x);

    // troca de arma
    for (let i = 1; i <= 9; i++) if (inp.hit(String(i)) && this.weapons[i - 1] && i - 1 !== this.cur) { this.cur = i - 1; this.refreshGun(); }
    if (inp.mouse.wheel) this.switchWeapon(inp.mouse.wheel > 0 ? 1 : -1);

    // entrada de movimento
    let mx = 0, mz = 0;
    if (inp.down('w') || inp.down('arrowup')) mz -= 1;
    if (inp.down('s') || inp.down('arrowdown')) mz += 1;
    if (inp.down('a') || inp.down('arrowleft')) mx -= 1;
    if (inp.down('d') || inp.down('arrowright')) mx += 1;
    const ml = Math.hypot(mx, mz);
    if (ml) { mx /= ml; mz /= ml; }

    // cambalhota
    if (!this.roll && this.rollCd <= 0 && (inp.hit(' ') || inp.mouse.rightPressed)) {
      const dx = ml ? mx : Math.cos(this.aim), dz = ml ? mz : Math.sin(this.aim);
      this.roll = { t: 0, dx, dz };
      this.reloadT = this.reloadT > 0 ? this.reloadT : 0;
      sfx('roll');
      g.particles.burst(this.x, 0.1, this.z, 6, 0xb9a68c, 1.5, 0.35, 0.08, 1);
      g.stats.rolls++;
    }

    if (this.roll) {
      const r = this.roll;
      r.t += dt;
      const k = 1 - r.t / ROLL_TIME;
      const spd = 3 + 10.5 * Math.max(0, k) ** 0.7;
      g.moveEntity(this, r.dx * spd * dt, r.dz * spd * dt, false);
      if (Math.random() < 0.4) g.particles.add({ x: this.x, y: 0.06, z: this.z, vx: -r.dx, vz: -r.dz, vy: 0.6, color: 0x8a7a66, life: 0.3, size: 0.07, grav: 2 });
      if (r.t >= ROLL_TIME) { this.roll = null; this.rollCd = 0.08; }
    } else {
      const tvx = mx * SPEED, tvz = mz * SPEED;
      const a = Math.min(1, dt * 18);
      this.vx += (tvx - this.vx) * a; this.vz += (tvz - this.vz) * a;
      g.moveEntity(this, this.vx * dt, this.vz * dt, false);
    }
    this.moving = ml > 0 && !this.roll;
    if (this.moving) this.walkT += dt;

    // recarregar
    const w = this.weapon, d = w.def;
    if (this.reloadT > 0) {
      this.reloadT -= dt;
      if (this.reloadT <= 0) {
        const need = d.mag - w.mag;
        const n = Math.min(need, w.ammo);
        w.mag += n; if (w.ammo !== Infinity) w.ammo -= n;
        sfx('reload');
        g.hud.dirty();
      }
    }
    if (inp.hit('r') && this.reloadT <= 0 && w.mag < d.mag && w.ammo > 0) this.startReload();

    // atirar
    if (!this.roll && inp.mouse.left && this.fireCd <= 0 && this.reloadT <= 0) {
      if (w.mag > 0) this.fire();
      else if (w.ammo > 0) this.startReload();
      else if (this.fireCd <= 0) { sfx('empty', 0.2); this.fireCd = 0.25; g.toast('Sem munição!', 'Troque de arma (1–9 / roda)'); }
    }

    // blank
    if (inp.hit('q') && this.blanks > 0) { this.blanks--; g.blank(this.x, this.z); g.hud.dirty(); }

    this.updateVisual(dt);
  }

  startReload() {
    this.reloadT = this.weapon.def.reload;
    sfx('reload');
  }

  fire() {
    const g = this.g, w = this.weapon, d = w.def;
    this.fireCd = 1 / d.rate;
    w.mag--;
    const mx = this.x + Math.cos(this.aim) * 0.6, mz = this.z + Math.sin(this.aim) * 0.6;
    for (let i = 0; i < d.pellets; i++) {
      const off = d.fan && d.pellets > 1 ? (i / (d.pellets - 1) - 0.5) * d.spread : (Math.random() - 0.5) * d.spread;
      const ang = this.aim + off;
      const spd = d.speed * (1 + (Math.random() - 0.5) * (d.speedVar || 0));
      g.bullets.spawn({
        x: mx, z: mz, vx: Math.cos(ang) * spd, vz: Math.sin(ang) * spd, enemy: false,
        r: 0.12 * d.size, dmg: d.dmg, life: d.life, pierce: d.pierce || 0, bounce: d.bounce || 0,
        homing: d.homing || 0, explode: d.explode || 0, color: d.color, size: d.size,
      });
    }
    g.particles.flash(mx, 0.45, mz + 0.05, 0.7 + d.kick * 0.15, d.color, 0.07);
    sfx(d.sfx, 0.02);
    this.kick = Math.min(1, 0.4 + d.kick * 0.3);
    if (d.kick > 1) g.shake(d.kick * 0.05);
    // recuo do personagem
    this.vx -= Math.cos(this.aim) * d.kick * 0.8; this.vz -= Math.sin(this.aim) * d.kick * 0.8;
    g.stats.shots++;
    g.hud.dirty();
  }

  updateVisual(dt) {
    this.group.position.set(this.x, 0, this.z);
    const a = this.screenAim;
    const left = Math.cos(a) < 0;
    const back = Math.sin(a) > 0.55;
    const rolling = !!this.roll;

    this.body.visible = !rolling;
    this.gunGroup.visible = !rolling;
    this.rollGroup.visible = rolling;
    if (rolling) {
      const dir = this.roll.dx >= 0 ? -1 : 1;
      this.rollMesh.rotation.z += dir * dt * 20;
      this.rollGroup.position.y = 0.42 + Math.sin(this.roll.t / ROLL_TIME * Math.PI) * 0.18;
      this.rollMesh.scale.x = this.roll.dx >= 0 ? 1 : -1;
    } else this.rollMesh.rotation.z = 0;

    const frames = back ? TEX.playerBack : TEX.player;
    const f = this.moving ? Math.floor(this.walkT * 9) % 4 : 0;
    this.body.material.uniforms.map.value = frames[f];
    this.body.scale.x = left ? -1 : 1;
    this.body.position.y = this.moving ? Math.abs(Math.sin(this.walkT * 18)) * 0.04 : Math.sin(performance.now() / 300) * 0.01;
    this.body.material.uniforms.flash.value = this.flash;
    // pisca durante a invulnerabilidade pós-dano
    if (this.invuln > 0 && !rolling) this.body.visible = Math.floor(this.invuln * 14) % 2 === 0;

    this.gunPivot.rotation.z = a;
    this.gunMesh.scale.y = left ? -1 : 1;
    this.gunMesh.position.x = 0.18 - this.kick * 0.12;
    this.gunGroup.position.z = back ? -0.04 : 0.04;
    this.gunGroup.position.x = left ? -0.12 : 0.12;
    this.gunGroup.visible = this.gunGroup.visible && (this.invuln <= 0 || this.body.visible);
    this.shadow.scale.setScalar(rolling ? 0.8 : 1);
  }
}

export { setFrame };
