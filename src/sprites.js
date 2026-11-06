// Todos os sprites do jogo, desenhados proceduralmente em pixel art.
// Personagens e inimigos são originais (inspirados no gênero, não cópias).
import { Pix, shade, OUTLINE } from './pixel.js';

const SKIN = '#e8b48a';
const BLACK = '#140b10';
const WHITE = '#fff6e8';

// ---------------------------------------------------------------- Jogador
export function drawPlayer({ back = false, step = 0 } = {}) {
  const p = new Pix(16, 22);
  const hat = '#5b3a29', hatD = '#3e271c', band = '#d0483b';
  const poncho = '#2f8f83', ponchoD = '#1f655d', ponchoL = '#55c2b0', zig = '#f0c75e';
  // pernas (passo alterna levantando uma perna)
  const l1 = step === 1 ? -1 : 0, l2 = step === 3 ? -1 : 0;
  p.rect(5, 17 + l1, 2, 3, '#3a3350').rect(9, 17 + l2, 2, 3, '#3a3350');
  p.rect(4, 20 + l1, 3, 2, '#6b4127').rect(9, 20 + l2, 3, 2, '#6b4127');
  // poncho
  p.rect(4, 10, 8, 2, poncho).rect(3, 12, 10, 2, poncho).rect(2, 14, 12, 3, poncho);
  p.rect(2, 16, 12, 1, ponchoD);
  for (let x = 2; x < 14; x++) p.set(x, 14 + ((x % 2) ? 0 : 1), zig);
  p.set(4, 11, ponchoL).set(3, 12, ponchoL).set(2, 14, ponchoL);
  if (!back) {
    // rosto
    p.rect(5, 6, 6, 4, SKIN).rect(5, 6, 6, 1, shade(SKIN, -0.3));
    p.set(6, 8, BLACK).set(9, 8, BLACK);
    p.set(7, 9, shade(SKIN, -0.15)).set(8, 9, shade(SKIN, -0.15));
    p.set(4, 7, '#2a1a14').set(11, 7, '#2a1a14').set(4, 8, '#2a1a14').set(11, 8, '#2a1a14');
    // lenço
    p.rect(6, 10, 4, 1, band);
  } else {
    p.rect(4, 6, 8, 4, '#2a1a14');
    p.rect(6, 10, 4, 1, shade(poncho, -0.2));
  }
  // chapéu
  p.rect(5, 1, 6, 3, hat).rect(6, 0, 4, 1, hat).rect(5, 3, 6, 1, band);
  p.rect(2, 4, 12, 2, hat).rect(2, 5, 12, 1, hatD);
  p.set(6, 1, shade(hat, 0.25)).set(3, 4, shade(hat, 0.25));
  return p.outline();
}

export function drawPlayerRoll() {
  const p = new Pix(16, 16);
  p.circ(8, 8, 6, '#2f8f83');
  for (let x = 3; x < 14; x++) p.set(x, 8 + ((x % 2) ? 0 : 1), '#f0c75e');
  p.ellipse(8, 4, 5, 2.2, '#5b3a29');
  p.rect(5, 4, 6, 1, '#d0483b');
  p.set(5, 10, '#55c2b0').set(4, 9, '#55c2b0');
  return p.outline();
}

// ---------------------------------------------------------------- Inimigos
function eyes(p, x1, x2, y, big = false) {
  if (big) {
    p.rect(x1, y, 2, 2, WHITE).rect(x2, y, 2, 2, WHITE);
    p.set(x1 + 1, y + 1, BLACK).set(x2 + 1, y + 1, BLACK);
  } else { p.set(x1, y, BLACK).set(x2, y, BLACK); }
}

export const ENEMY_ART = {
  gosma(f) {
    const p = new Pix(16, 14);
    const c = '#5fbf4a';
    if (f === 0) { p.ellipse(8, 9, 7, 4.6, c).ellipse(8, 6, 5, 4, c); }
    else { p.ellipse(8, 10, 7.6, 3.8, c).ellipse(8, 7.5, 5.6, 3.4, c); }
    p.tint((x, y) => (y > 10 ? shade(c, -0.3) : null));
    p.set(5, 4 + f, shade(c, 0.5)).set(6, 3 + f, shade(c, 0.5)).set(4, 5 + f, shade(c, 0.35));
    eyes(p, 5, 9, 6 + f, true);
    p.rect(7, 10, 2, 1, shade(c, -0.55));
    return p.outline();
  },
  lanterna(f) {
    const p = new Pix(14, 18);
    const metal = '#7b5a3a', glass = f ? '#ffd56b' : '#ffbf3b';
    p.line(7, 0, 7, 1, metal);
    p.rect(4, 2, 6, 1, metal).rect(3, 3, 8, 1, shade(metal, 0.2));
    p.rect(3, 4, 8, 9, glass);
    p.rect(3, 4, 1, 9, metal).rect(10, 4, 1, 9, metal).rect(6, 4, 2, 9, shade(glass, 0.25));
    p.ellipse(7, 9, 2.2, 3, f ? '#fff3c4' : '#ffe08a');
    p.set(5, 7, BLACK).set(9, 7, BLACK).rect(5, 10, 1, 1, BLACK).rect(9, 10, 1, 1, BLACK);
    p.rect(3, 13, 8, 1, metal).rect(4, 14, 6, 2, shade(metal, -0.25));
    p.set(7, 16, metal);
    return p.outline();
  },
  cogumelo(f) {
    const p = new Pix(16, 16);
    const cap = '#c8413d';
    p.rect(5, 9, 6, 5, '#efe0c4');
    p.rect(4, 14 - f, 3, 2, '#b49a7a').rect(9, 13 + f, 3, 2, '#b49a7a');
    p.ellipse(8, 6, 7.5, 5, cap);
    p.rect(0, 9, 16, 2, null);
    for (let x = 0; x < 16; x++) for (let y = 9; y < 11; y++) p.d[y * 16 + x] = null;
    p.rect(1, 8, 14, 1, shade(cap, -0.35));
    p.circ(5, 4, 1.4, WHITE).circ(11, 5, 1.2, WHITE).circ(8, 2, 0.9, WHITE);
    p.rect(5, 9, 6, 5, '#efe0c4');
    eyes(p, 6, 9, 10);
    p.set(7, 12, '#9c5a4c').set(8, 12, '#9c5a4c');
    return p.outline();
  },
  caveira(f) {
    const p = new Pix(14, 20);
    const bone = '#e9e2cf', boneD = '#a9a08a';
    p.circ(7, 5, 4.6, bone);
    p.rect(4, 4, 2, 2, BLACK).rect(8, 4, 2, 2, BLACK);
    p.set(5, 4, '#ff5040').set(9, 4, '#ff5040');
    p.rect(5, 8, 4, 2, bone).set(6, 9, BLACK).set(8, 9, BLACK);
    p.set(7, 7, boneD);
    p.rect(6, 10, 2, 1, boneD);
    p.rect(4, 11, 6, 5, '#3b2f45');
    for (let y = 12; y < 16; y += 2) p.rect(5, y, 4, 1, bone);
    p.rect(6, 11, 2, 5, boneD);
    p.rect(2, 11, 2, 1, bone).rect(10, 11, 2, 1, bone);
    p.rect(1, 12, 1, 3, bone).rect(12, 12, 1, 3, bone);
    p.rect(5, 16, 1, 3 - f, bone).rect(8, 16, 1, 2 + f, bone);
    p.rect(4, 19 - f, 2, 1, boneD).rect(8, 18 + f, 2, 1, boneD);
    return p.outline();
  },
  torreta(f) {
    const p = new Pix(16, 18);
    const st = '#7d7690', stD = '#57506a', stL = '#a39cb6';
    p.rect(2, 3, 12, 14, st).rect(1, 2, 14, 2, stL).rect(3, 1, 10, 1, stL);
    p.rect(2, 15, 12, 2, stD);
    p.line(3, 6, 5, 6, stD).line(10, 6, 12, 6, stD).line(3, 13, 12, 13, stD);
    p.circ(8, 9, 3.2, '#2b1324');
    p.circ(8, 9, 2.2, f ? '#ff7b4a' : '#ff3b3b');
    p.set(8, 9, '#ffe1a0').set(7, 8, '#ffe1a0');
    return p.outline();
  },
  morcego(f) {
    const p = new Pix(20, 13);
    const c = '#6b4a9a', w = '#4a3070';
    if (f === 0) {
      for (let i = 0; i < 7; i++) { p.line(9 - i, 5 - Math.floor(i / 2), 9 - i, 7 + (i % 3 === 0 ? 1 : 0), w); p.line(10 + i, 5 - Math.floor(i / 2), 10 + i, 7 + (i % 3 === 0 ? 1 : 0), w); }
    } else {
      for (let i = 0; i < 7; i++) { p.line(9 - i, 6 + Math.floor(i / 2), 9 - i, 8 + Math.floor(i / 2), w); p.line(10 + i, 6 + Math.floor(i / 2), 10 + i, 8 + Math.floor(i / 2), w); }
    }
    p.ellipse(10, 7, 3.2, 3.6, c);
    p.set(8, 3, c).set(12, 3, c).set(8, 4, c).set(12, 4, c);
    p.set(9, 6, '#ffe14d').set(11, 6, '#ffe14d');
    p.set(9, 9, WHITE).set(11, 9, WHITE);
    return p.outline();
  },
  cristal(f) {
    const p = new Pix(12, 20);
    const c = '#5ad1e6', d = '#2f7fa8', l = '#c8f6ff';
    for (let y = 0; y < 18; y++) {
      const half = y < 8 ? Math.floor(y * 0.75) : Math.floor((17 - y) * 0.6);
      p.rect(6 - half, y + f, half * 2, 1, c);
    }
    p.tint((x, y) => (x < 6 ? null : d));
    p.line(4, 4 + f, 3, 9 + f, l);
    p.circ(6, 8 + f, 1.8, WHITE);
    p.set(6, 8 + f, '#9a1460');
    return p.outline();
  },
  besouro(f) {
    const p = new Pix(18, 15);
    const c = '#2d4a8a', l = '#5b84d6';
    for (let i = 0; i < 3; i++) { p.line(3, 6 + i * 3, 1, 7 + i * 3 + (f ^ (i & 1)), BLACK); p.line(14, 6 + i * 3, 16, 7 + i * 3 + (f ^ (i & 1)), BLACK); }
    p.ellipse(9, 9, 6.5, 5.5, c);
    p.line(9, 4, 9, 14, shade(c, -0.4));
    p.set(6, 6, l).set(5, 7, l).set(12, 6, l);
    p.ellipse(9, 3.5, 3.5, 2.4, '#1c2442');
    p.line(9, 2, 9, 0, '#d9c26a');
    p.set(7, 3, '#ff6060').set(11, 3, '#ff6060');
    return p.outline();
  },
};

// ---------------------------------------------------------------- Chefes
export const BOSS_ART = {
  reiGosma(f) {
    const p = new Pix(36, 32);
    const c = '#62c44a';
    const sq = f ? 1 : 0;
    p.ellipse(18, 21 + sq, 16 + sq, 10 - sq, c).ellipse(18, 15 + sq, 12, 9, c);
    p.tint((x, y) => (y > 26 ? shade(c, -0.3) : null));
    p.ellipse(12, 12 + sq, 2.5, 1.5, shade(c, 0.5));
    p.rect(9, 17 + sq, 5, 4, WHITE).rect(21, 17 + sq, 5, 4, WHITE);
    p.rect(11, 18 + sq, 2, 2, BLACK).rect(23, 18 + sq, 2, 2, BLACK);
    p.rect(13, 24 + sq, 10, 2, shade(c, -0.6)).rect(14, 24 + sq, 2, 1, WHITE).rect(20, 24 + sq, 2, 1, WHITE);
    // coroa
    const g = '#ffcc3a';
    p.rect(11, 6 + sq, 14, 3, g);
    for (let i = 0; i < 4; i++) p.rect(11 + i * 4, 3 + sq, 2, 3, g);
    p.set(13, 7 + sq, '#e8364f').set(18, 7 + sq, '#3ac1e8').set(23, 7 + sq, '#e8364f');
    return p.outline();
  },
  sinoFantasma(f) {
    const p = new Pix(34, 36);
    const c = '#c9bfe8', d = '#8c7fbf';
    p.ellipse(17, 12, 10, 10, c);
    for (let y = 12; y < 28; y++) { const w = 10 + Math.floor((y - 12) * 0.45); p.rect(17 - w, y, w * 2, 1, c); }
    p.rect(4, 27, 26, 2, d);
    p.tint((x, y) => (x > 24 && y > 6 ? d : null));
    p.rect(11, 13, 4, 5, BLACK).rect(20, 13, 4, 5, BLACK);
    p.set(12, 14, '#9b5cff').set(21, 14, '#9b5cff');
    p.ellipse(17, 23, 3, 2.5, BLACK);
    // badalo
    p.line(17, 29, 17 + (f ? 2 : -2), 32, '#6b5a3a');
    p.circ(17 + (f ? 2 : -2), 33, 2, '#d9b25a');
    p.rect(15, 0, 4, 2, '#6b5a3a');
    for (let i = 0; i < 3; i++) { p.set(3 + i, 8 + i * 3, '#8f8f9a'); p.set(30 - i, 8 + i * 3, '#8f8f9a'); }
    return p.outline();
  },
  golem(f) {
    const p = new Pix(36, 34);
    const st = '#7c7266', d = '#544b41', l = '#a59a8b', core = f ? '#ffb347' : '#ff7a2f';
    p.ellipse(18, 18, 12, 11, st);
    p.circ(5, 18 + f, 5, st).circ(31, 18 - f, 5, st);
    p.circ(4, 25 + f, 4, d).circ(32, 25 - f, 4, d);
    p.rect(10, 27, 6, 6, d).rect(20, 27, 6, 6, d);
    p.ellipse(18, 9, 7, 5, l);
    p.tint((x, y) => ((x * 7 + y * 13) % 17 === 0 ? d : null));
    p.rect(14, 8, 3, 2, core).rect(20, 8, 3, 2, core);
    p.circ(18, 19, 4, '#3a1a10').circ(18, 19, 3, core).circ(18, 19, 1.4, '#fff1c0');
    p.line(10, 14, 14, 17, d).line(26, 13, 23, 16, d);
    return p.outline();
  },
  aranha(f) {
    const p = new Pix(40, 30);
    const brass = '#c99a3b', d = '#7d5a1c';
    for (let i = 0; i < 4; i++) {
      const y = 8 + i * 4, off = (i + f) % 2 ? 2 : -1;
      p.line(14, y, 6 - i, y - 4 + off, d).line(6 - i, y - 4 + off, 2 - i, y + 8 + off, d);
      p.line(26, y, 34 + i, y - 4 - off, d).line(34 + i, y - 4 - off, 38 + i, y + 8 - off, d);
    }
    p.ellipse(20, 15, 10, 9, brass);
    p.circ(20, 15, 7, '#f5ecd2');
    for (let a = 0; a < 12; a++) p.set(20 + Math.cos(a / 12 * Math.PI * 2) * 6, 15 + Math.sin(a / 12 * Math.PI * 2) * 6, BLACK);
    p.line(20, 15, 20, 10, BLACK).line(20, 15, 23 + f, 17, '#c8243c');
    p.ellipse(20, 26, 5, 3, d);
    p.set(17, 25, '#ff3b3b').set(23, 25, '#ff3b3b').set(19, 27, '#ff3b3b').set(21, 27, '#ff3b3b');
    p.rect(18, 4, 4, 2, d).rect(19, 2, 2, 2, brass);
    return p.outline();
  },
  sacerdote(f) {
    const p = new Pix(32, 40);
    const robe = '#7a1f34', rd = '#4c1222', gold = '#ffcc3a';
    // auréola de canos
    p.ellipse(16, 8, 12, 4, null);
    for (let a = 0; a < 16; a++) {
      const ang = a / 16 * Math.PI * 2 + f * 0.2;
      p.set(16 + Math.cos(ang) * 12, 9 + Math.sin(ang) * 5, gold);
    }
    for (let y = 12; y < 38; y++) { const w = 5 + Math.floor((y - 12) * 0.38); p.rect(16 - w, y, w * 2, 1, robe); }
    p.tint((x, y) => (x > 18 && y > 14 ? rd : null));
    p.rect(14, 14, 4, 24, gold).rect(10, 20, 12, 3, gold);
    p.ellipse(16, 10, 6, 6.5, rd);
    p.ellipse(16, 11, 4.5, 5, '#efe6d6');
    p.rect(13, 10, 2, 1, BLACK).rect(18, 10, 2, 1, BLACK);
    p.set(14, 10, '#ff3050').set(18, 10, '#ff3050');
    p.line(16, 13, 16, 15, '#bfae96');
    p.circ(5, 24 + f, 2.5, '#efe6d6').circ(27, 24 - f, 2.5, '#efe6d6');
    return p.outline();
  },
};

// ---------------------------------------------------------------- Itens
export function drawHeart(kind = 'full') {
  const p = new Pix(11, 10);
  const r = '#e8364f', e = '#3a1520';
  const shape = ['.##...##.', '####.####', '#########', '#########', '.#######.', '..#####..', '...###...', '....#....'];
  shape.forEach((row, y) => [...row].forEach((ch, x) => {
    if (ch !== '#') return;
    let c = r;
    if (kind === 'empty') c = e;
    if (kind === 'half' && x >= 4) c = e;
    p.set(x + 1, y + 1, c);
  }));
  if (kind !== 'empty') p.set(2, 2, '#ff9aa8').set(3, 2, '#ff9aa8');
  return p.outline();
}

export function drawBlank() {
  const p = new Pix(10, 10);
  p.circ(5, 5, 4, '#3aa8e8').circ(5, 5, 2.4, '#bff0ff').set(4, 3, WHITE);
  return p.outline();
}

export function drawCoin() {
  const p = new Pix(8, 10);
  p.rect(2, 1, 4, 7, '#c27a1e').rect(3, 0, 2, 9, '#c27a1e');
  p.rect(3, 1, 2, 7, '#ffd35a').set(3, 2, '#fff3b0');
  return p.outline();
}

export function drawAmmo() {
  const p = new Pix(14, 11);
  p.rect(1, 4, 12, 6, '#4f6b2f').rect(1, 4, 12, 1, '#6f8f43');
  p.rect(5, 6, 4, 2, '#d9c26a');
  for (let i = 0; i < 4; i++) { p.rect(2 + i * 3, 1, 2, 3, '#e0a33a'); p.set(2 + i * 3, 1, '#ffe3a0'); }
  return p.outline();
}

export function drawChest(open = false, rare = false) {
  const p = new Pix(18, 15);
  const wood = rare ? '#3a6fb0' : '#8a5a2b', band = rare ? '#ffd35a' : '#c9c2b0', d = shade(wood, -0.35);
  p.rect(1, 6, 16, 8, wood).rect(1, 12, 16, 2, d);
  if (!open) {
    p.rect(1, 2, 16, 4, shade(wood, 0.12)).rect(2, 1, 14, 1, shade(wood, 0.12));
    p.rect(1, 5, 16, 1, band);
    p.rect(7, 4, 4, 4, band).set(8, 6, BLACK).set(9, 6, BLACK);
  } else {
    p.rect(1, 0, 16, 3, shade(wood, -0.15)).rect(2, 3, 14, 3, '#2a160c');
    p.rect(3, 4, 12, 2, '#ffe28a');
  }
  p.rect(3, 6, 1, 8, band).rect(14, 6, 1, 8, band);
  return p.outline();
}

export function drawTorch(f) {
  const p = new Pix(8, 14);
  p.rect(3, 7, 2, 6, '#5a3a22').rect(2, 12, 4, 1, '#3a2416');
  p.ellipse(4, 4 + (f % 2 ? 0.5 : 0), 2.6, 3.6, '#ff8a2a');
  p.ellipse(4, 5, 1.4, 2, '#ffe07a');
  if (f % 2) p.set(5, 0, '#ff8a2a'); else p.set(3, 0, '#ff8a2a');
  return p.outline();
}

export function drawHatch(open = true) {
  const p = new Pix(28, 28);
  p.rect(1, 1, 26, 26, '#2b1d16').rect(3, 3, 22, 22, open ? '#050304' : '#5b3b25');
  if (open) for (let i = 0; i < 5; i++) p.rect(5 + i, 6 + i * 4, 18 - i * 2, 2, shade('#5b3b25', -i * 0.15));
  else { p.line(3, 3, 24, 24, '#3a2416'); p.line(24, 3, 3, 24, '#3a2416'); }
  p.rect(0, 0, 28, 1, '#a07a4a').rect(0, 27, 28, 1, '#a07a4a').rect(0, 0, 1, 28, '#a07a4a').rect(27, 0, 1, 28, '#a07a4a');
  return p;
}

export function drawShadow() {
  const p = new Pix(16, 8);
  p.ellipse(8, 4, 7.5, 3.5, 'rgba(0,0,0,0.42)');
  return p;
}

export function drawBullet(enemy = true) {
  const p = new Pix(10, 10);
  if (enemy) {
    p.circ(5, 5, 4.6, '#ff3f6c').circ(5, 5, 2.6, '#ffd2dc').set(4, 3, WHITE);
    return p.outline('#4a0019');
  }
  p.circ(5, 5, 4, '#d8d8d8').circ(5, 5, 2.6, '#ffffff');
  return p;
}

export function drawSpark() {
  const p = new Pix(4, 4);
  p.rect(0, 0, 4, 4, '#ffffff');
  return p;
}

export function drawPedestal() {
  const p = new Pix(16, 10);
  p.rect(2, 2, 12, 7, '#5c4c6b').rect(1, 1, 14, 2, '#8a789c').rect(2, 8, 12, 1, '#3c3048');
  return p.outline();
}
