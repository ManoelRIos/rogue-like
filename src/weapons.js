// Definição das armas e seus sprites procedurais.
import { Pix, shade } from './pixel.js';

const M = '#5c5f6e', MD = '#363844', ML = '#8d91a3', WOOD = '#7a4a26', WOODL = '#a26a3a', GOLD = '#ffcc3a';

function gun(w, h, fn) { const p = new Pix(w, h); fn(p); return p.outline(); }

export const WEAPONS = {
  pistola: {
    name: 'Pistola Velha', tier: 0, dmg: 5, rate: 4.5, mag: 8, ammo: Infinity, reload: 0.85, spread: 0.06,
    pellets: 1, speed: 16, life: 1.2, color: 0xfff0b0, size: 1, sfx: 'shoot', kick: 0.6,
    art: () => gun(14, 9, (p) => {
      p.rect(2, 1, 10, 3, M).rect(2, 1, 10, 1, ML).rect(11, 2, 2, 1, MD);
      p.rect(3, 4, 3, 4, WOOD).set(3, 4, WOODL).rect(6, 4, 2, 1, MD);
    }),
  },
  escopeta: {
    name: 'Escopeta Serrada', tier: 1, dmg: 4.2, rate: 1.5, mag: 4, ammo: 64, reload: 1.25, spread: 0.5,
    pellets: 6, speed: 14, life: 0.5, color: 0xffb070, size: 0.9, sfx: 'shotgun', kick: 2.2, speedVar: 0.25,
    art: () => gun(22, 9, (p) => {
      p.rect(1, 3, 8, 4, WOOD).rect(1, 3, 8, 1, WOODL).rect(0, 4, 2, 3, WOOD);
      p.rect(8, 2, 13, 2, M).rect(8, 4, 12, 1, MD).rect(8, 2, 13, 1, ML);
      p.rect(11, 5, 4, 2, WOOD);
    }),
  },
  metralhadora: {
    name: 'Metralhadora Ferrugem', tier: 1, dmg: 3.3, rate: 12, mag: 32, ammo: 340, reload: 1.4, spread: 0.15,
    pellets: 1, speed: 17, life: 1.0, color: 0xffe080, size: 0.8, sfx: 'shoot', kick: 0.3,
    art: () => gun(20, 11, (p) => {
      p.rect(3, 2, 13, 4, '#7a5a3e').rect(3, 2, 13, 1, '#a07a52').rect(16, 3, 3, 1, MD);
      p.rect(7, 6, 3, 4, MD).rect(4, 6, 2, 3, WOOD).rect(0, 3, 3, 2, '#7a5a3e');
      p.set(6, 3, '#c0443a').set(9, 3, '#c0443a');
    }),
  },
  rifle: {
    name: 'Rifle Longo', tier: 2, dmg: 24, rate: 1.3, mag: 5, ammo: 60, reload: 1.6, spread: 0.01,
    pellets: 1, speed: 32, life: 1.2, color: 0xfff6dd, size: 1.1, sfx: 'heavy', pierce: 4, kick: 1.5,
    art: () => gun(26, 9, (p) => {
      p.rect(0, 3, 9, 3, WOOD).rect(0, 3, 9, 1, WOODL).rect(0, 6, 3, 1, WOOD);
      p.rect(9, 3, 16, 2, M).rect(9, 3, 16, 1, ML).rect(24, 2, 1, 1, MD);
      p.rect(10, 1, 7, 2, MD).set(10, 1, '#7fd6ff').set(16, 1, '#7fd6ff');
      p.rect(10, 5, 4, 2, WOOD);
    }),
  },
  bolhas: {
    name: 'Soprador de Bolhas', tier: 1, dmg: 6, rate: 5, mag: 20, ammo: 240, reload: 1.1, spread: 0.3,
    pellets: 1, speed: 7, life: 2.6, color: 0x9ff3ff, size: 1.7, sfx: 'bubble', bounce: 3, kick: 0.2, speedVar: 0.3,
    art: () => gun(18, 11, (p) => {
      p.ellipse(6, 4, 5, 3.5, '#e86ab8').ellipse(6, 3, 3, 1.5, '#ffb3e0');
      p.rect(10, 3, 6, 2, '#ffd35a').circ(16, 4, 1.6, '#9ff3ff');
      p.rect(4, 7, 3, 3, '#a33d7f');
    }),
  },
  raio: {
    name: 'Lança-Raios', tier: 2, dmg: 2.2, rate: 22, mag: 70, ammo: 700, reload: 1.5, spread: 0.03,
    pellets: 1, speed: 34, life: 0.55, color: 0x6ff0ff, size: 0.75, sfx: 'laser', pierce: 99, kick: 0.05,
    art: () => gun(22, 11, (p) => {
      p.rect(2, 2, 14, 5, '#2e3d5c').rect(2, 2, 14, 1, '#4c6a9a').rect(16, 3, 5, 3, '#2e3d5c');
      p.rect(5, 3, 8, 2, '#6ff0ff').set(20, 4, '#c8ffff');
      p.rect(5, 7, 3, 3, MD);
    }),
  },
  foguete: {
    name: 'Canhão de Fogos', tier: 2, dmg: 16, rate: 1.1, mag: 3, ammo: 30, reload: 1.8, spread: 0.02,
    pellets: 1, speed: 11, life: 2, color: 0xff7a3a, size: 1.6, sfx: 'heavy', explode: 2.1, kick: 2,
    art: () => gun(24, 12, (p) => {
      p.rect(1, 2, 20, 6, '#3f6b3a').rect(1, 2, 20, 1, '#5e9a57').rect(20, 1, 3, 8, '#2c4a29');
      p.rect(4, 3, 2, 4, '#e8364f').rect(10, 3, 2, 4, '#ffd35a');
      p.rect(7, 8, 3, 3, MD).rect(13, 8, 2, 2, MD);
    }),
  },
  besta: {
    name: 'Besta Tripla', tier: 1, dmg: 9, rate: 2.1, mag: 6, ammo: 96, reload: 1.3, spread: 0.32,
    pellets: 3, fan: true, speed: 22, life: 1, color: 0xd8ffb0, size: 0.9, sfx: 'shoot', pierce: 1, kick: 0.6,
    art: () => gun(20, 13, (p) => {
      p.rect(2, 5, 15, 2, WOOD).rect(2, 5, 15, 1, WOODL);
      p.line(13, 0, 15, 6, MD).line(15, 6, 13, 12, MD);
      p.line(13, 0, 5, 6, '#e0d8c0').line(5, 6, 13, 12, '#e0d8c0');
      p.rect(15, 5, 4, 1, '#c8c8c8').rect(3, 7, 3, 3, WOOD);
    }),
  },
  revolver: {
    name: 'Revólver Dourado', tier: 2, dmg: 13, rate: 3.6, mag: 6, ammo: 120, reload: 1.2, spread: 0.03,
    pellets: 1, speed: 21, life: 1.4, color: 0xffd35a, size: 1.1, sfx: 'heavy', bounce: 2, kick: 1,
    art: () => gun(17, 10, (p) => {
      p.rect(2, 1, 13, 3, GOLD).rect(2, 1, 13, 1, '#fff0a0').rect(5, 3, 5, 3, shade(GOLD, -0.2));
      p.rect(2, 4, 3, 5, '#5a2a1a').set(2, 4, '#8a4a2a');
      p.set(7, 4, '#5a3a10').set(9, 4, '#5a3a10');
    }),
  },
  estrelas: {
    name: 'Lança-Estrelas', tier: 2, dmg: 7, rate: 6, mag: 24, ammo: 260, reload: 1.2, spread: 0.35,
    pellets: 1, speed: 12, life: 2.2, color: 0xffe94d, size: 1.2, sfx: 'laser', homing: 5, kick: 0.2,
    art: () => gun(19, 11, (p) => {
      p.rect(2, 3, 12, 4, '#6b3fa0').rect(2, 3, 12, 1, '#9a6ad6');
      p.rect(14, 2, 3, 6, '#ffd35a').set(17, 4, '#fff6b0').set(17, 5, '#fff6b0');
      p.rect(4, 7, 3, 3, '#3f2466');
      p.set(6, 5, '#ffe94d').set(9, 4, '#ffe94d').set(11, 5, '#ffe94d');
    }),
  },
};

export const WEAPON_IDS = Object.keys(WEAPONS);

export function randomWeapon(rng, floor, exclude = []) {
  const pool = WEAPON_IDS.filter((id) => id !== 'pistola' && !exclude.includes(id));
  if (!pool.length) return null;
  const entries = pool.map((id) => {
    const t = WEAPONS[id].tier;
    const w = t === 1 ? 4 - floor * 0.4 : 1 + floor * 0.7;
    return [id, Math.max(0.3, w)];
  });
  return rng.weighted(entries);
}

// Instância de arma do inventário do jogador
export function makeWeapon(id) {
  const d = WEAPONS[id];
  return { id, def: d, mag: d.mag, ammo: d.ammo === Infinity ? Infinity : d.ammo - d.mag };
}
