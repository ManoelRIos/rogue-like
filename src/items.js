// Itens no chão, baús, loja e alçapão para o próximo andar.
import * as THREE from 'three';
import { makeSprite, makeShadow, TEX } from './gfx.js';
import { WEAPONS } from './weapons.js';

const ART = {
  heart: () => TEX.heart, half: () => TEX.halfHeart, ammo: () => TEX.ammo, coin: () => TEX.coin, blank: () => TEX.blank,
};

export class Pickup {
  constructor(game, type, x, z, opts = {}) {
    this.g = game; this.type = type; this.x = x; this.z = z;
    this.weaponId = opts.weaponId;
    this.price = opts.price || 0;
    this.y = opts.pop === false ? 0 : 0.6; this.vy = opts.pop === false ? 0 : 3;
    this.vx = opts.vx ?? (Math.random() - 0.5) * 2; this.vz = opts.vz ?? (Math.random() - 0.5) * 2;
    if (opts.pop === false) { this.vx = 0; this.vz = 0; }
    this.t = Math.random() * 6; this.dead = false;
    this.group = new THREE.Group();
    const frames = type === 'weapon' ? TEX.weapons[this.weaponId] : ART[type]();
    this.sprite = makeSprite(frames, { scale: type === 'weapon' ? 1.2 : 1 });
    this.group.add(this.sprite);
    this.shadow = makeShadow(type === 'weapon' ? 1 : 0.5);
    this.group.add(this.shadow);
    this.group.position.set(x, 0, z);
    game.scene.add(this.group);
    this.age = 0;
  }

  update(dt) {
    this.t += dt; this.age += dt;
    if (this.y > 0 || this.vy > 0) {
      this.vy -= 14 * dt; this.y += this.vy * dt;
      if (this.y <= 0) { this.y = 0; this.vy = Math.abs(this.vy) > 1.5 ? -this.vy * 0.4 : 0; }
      this.g.moveEntity(this, this.vx * dt, this.vz * dt, false, 0.15);
      this.vx *= Math.pow(0.05, dt); this.vz *= Math.pow(0.05, dt);
    }
    // moedas são atraídas pelo jogador
    const P = this.g.player;
    if (this.type === 'coin' && this.age > 0.4) {
      const dx = P.x - this.x, dz = P.z - this.z, d = Math.hypot(dx, dz);
      if (d < 2.8 && d > 0.01) { const s = (3 - d) * 4 * dt; this.x += (dx / d) * s; this.z += (dz / d) * s; }
    }
    this.group.position.set(this.x, 0, this.z);
    this.sprite.position.y = this.y + 0.08 + (this.type === 'weapon' ? 0.2 + Math.sin(this.t * 3) * 0.06 : Math.abs(Math.sin(this.t * 3)) * 0.05);
    this.sprite.material.uniforms.flash.value = this.type === 'weapon' ? 0.15 + Math.sin(this.t * 5) * 0.12 : 0;
  }

  remove() { this.dead = true; this.g.scene.remove(this.group); }
}

// ------------------------------------------------------------------ interativos
export class Chest {
  constructor(game, x, z, rare = false) {
    this.g = game; this.x = x; this.z = z; this.rare = rare; this.opened = false; this.r = 0.6;
    this.sprite = makeSprite(rare ? TEX.chestRare : TEX.chest, { scale: 1.25 });
    this.sprite.position.set(x, 0, z);
    this.shadow = makeShadow(1.4); this.shadow.position.set(x, 0.012, z);
    game.scene.add(this.sprite, this.shadow);
  }
  get prompt() { return this.opened ? null : `<b>E</b> abrir baú${this.rare ? ' raro' : ''}`; }
  use() {
    if (this.opened) return;
    this.opened = true;
    this.sprite.material.uniforms.map.value = this.sprite.userData.frames[1];
    this.g.openChest(this);
  }
  remove() { this.g.scene.remove(this.sprite, this.shadow); }
}

export class ShopItem {
  constructor(game, x, z, item) {
    this.g = game; this.x = x; this.z = z; this.item = item; this.sold = false; this.r = 0.6;
    this.ped = makeSprite(TEX.pedestal, { scale: 1.1 });
    this.ped.position.set(x, 0, z);
    game.scene.add(this.ped);
    this.pickup = new Pickup(game, item.type, x, z, { weaponId: item.weaponId, pop: false });
    this.pickup.group.position.y = 0;
    this.pickup.update = (dt) => {
      this.pickup.t += dt;
      this.pickup.sprite.position.y = 0.55 + Math.sin(this.pickup.t * 3) * 0.05;
      this.pickup.shadow.visible = false;
    };
    game.pickupsStatic.push(this.pickup);
  }
  get label() {
    const it = this.item;
    if (it.type === 'weapon') return WEAPONS[it.weaponId].name;
    return { heart: 'Coração', ammo: 'Caixa de munição', blank: 'Blank', half: 'Meio coração' }[it.type];
  }
  get prompt() { return this.sold ? null : `<b>E</b> comprar ${this.label} — <b>${this.item.price}</b> moedas`; }
  use() { this.g.buy(this); }
  remove() { this.g.scene.remove(this.ped); this.pickup.remove(); }
}

export class Hatch {
  constructor(game, x, z, last = false) {
    this.g = game; this.x = x; this.z = z; this.r = 1.1; this.last = last;
    const tex = TEX.hatch[0];
    this.mesh = new THREE.Mesh(new THREE.PlaneGeometry(1.75, 1.75), new THREE.MeshBasicMaterial({ map: tex, transparent: true }));
    this.mesh.rotation.x = -Math.PI / 2;
    this.mesh.position.set(x, 0.015, z);
    game.scene.add(this.mesh);
  }
  get prompt() { return this.last ? '<b>E</b> tocar o Altar do Último Tiro' : '<b>E</b> descer para o próximo andar'; }
  use() { this.g.descend(); }
  remove() { this.g.scene.remove(this.mesh); }
}
