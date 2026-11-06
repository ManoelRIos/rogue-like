// Renderização: câmera 2.5D, material de sprite, biblioteca de texturas.
import * as THREE from 'three';
import * as S from './sprites.js';
import { WEAPONS } from './weapons.js';
import { toTexture } from './pixel.js';

export const PX = 1 / 16;            // 1 pixel de sprite = 1/16 de tile
export const CAM_PITCH = 0.95;       // ângulo da câmera em relação ao chão (rad) ~54°
export const SPRITE_Y = (1 / Math.cos(CAM_PITCH)) * 0.85; // compensa o achatamento do plano em pé

// ------------------------------------------------------------ material de sprite
const spriteVS = /* glsl */`
  varying vec2 vUv;
  void main() {
    vUv = uv;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }`;
const spriteFS = /* glsl */`
  uniform sampler2D map;
  uniform float flash;
  uniform vec3 tint;
  uniform float opacity;
  uniform vec4 uvRect;
  varying vec2 vUv;
  void main() {
    vec2 uv = uvRect.xy + vUv * uvRect.zw;
    vec4 c = texture2D(map, uv);
    if (c.a < 0.5) discard;
    vec3 col = mix(c.rgb * tint, vec3(1.0), flash);
    gl_FragColor = vec4(col, opacity);
    #include <colorspace_fragment>
  }`;

export function spriteMaterial(tex) {
  return new THREE.ShaderMaterial({
    uniforms: {
      map: { value: tex }, flash: { value: 0 }, tint: { value: new THREE.Color(1, 1, 1) },
      opacity: { value: 1 }, uvRect: { value: new THREE.Vector4(0, 0, 1, 1) },
    },
    vertexShader: spriteVS, fragmentShader: spriteFS,
  });
}

// Geometria de plano com a base em y=0 e centro em x
const geoCache = new Map();
export function spriteGeo(w, h, anchorX = 0.5, anchorY = 0) {
  const k = `${w}|${h}|${anchorX}|${anchorY}`;
  if (!geoCache.has(k)) {
    const g = new THREE.PlaneGeometry(w, h);
    g.translate(w * (0.5 - anchorX), h * (0.5 - anchorY), 0);
    g.userData.shared = true;
    geoCache.set(k, g);
  }
  return geoCache.get(k);
}

// Cria um sprite em pé (billboard vertical) a partir de uma textura de pixel art
export function makeSprite(frames, opts = {}) {
  const tex = frames[0];
  const w = tex.image.width * PX * (opts.scale || 1);
  const h = tex.image.height * PX * (opts.scale || 1);
  const mat = spriteMaterial(tex);
  const mesh = new THREE.Mesh(spriteGeo(w, h, opts.anchorX ?? 0.5, opts.anchorY ?? 0), mat);
  mesh.scale.y = SPRITE_Y;
  mesh.userData.frames = frames;
  mesh.userData.w = w; mesh.userData.h = h;
  return mesh;
}

export function setFrame(mesh, i) {
  const f = mesh.userData.frames;
  mesh.material.uniforms.map.value = f[i % f.length];
}

// ------------------------------------------------------------ sombra
let shadowTex;
export function makeShadow(size = 1) {
  if (!shadowTex) shadowTex = S.drawShadow().texture();
  const m = new THREE.Mesh(
    new THREE.PlaneGeometry(size, size * 0.5),
    new THREE.MeshBasicMaterial({ map: shadowTex, transparent: true, depthWrite: false }),
  );
  m.rotation.x = -Math.PI / 2;
  m.position.y = 0.012;
  m.renderOrder = 1;
  return m;
}

// ------------------------------------------------------------ biblioteca de texturas
export const TEX = {};
export function buildTextures() {
  const P = (pix) => pix.texture();
  TEX.player = [0, 1, 2, 3].map((s) => P(S.drawPlayer({ step: s })));
  TEX.playerBack = [0, 1, 2, 3].map((s) => P(S.drawPlayer({ back: true, step: s })));
  TEX.playerRoll = [P(S.drawPlayerRoll())];
  TEX.enemies = {};
  for (const k of Object.keys(S.ENEMY_ART)) TEX.enemies[k] = [P(S.ENEMY_ART[k](0)), P(S.ENEMY_ART[k](1))];
  TEX.bosses = {};
  for (const k of Object.keys(S.BOSS_ART)) TEX.bosses[k] = [P(S.BOSS_ART[k](0)), P(S.BOSS_ART[k](1))];
  TEX.weapons = {};
  TEX.weaponPix = {};
  for (const k of Object.keys(WEAPONS)) { TEX.weaponPix[k] = WEAPONS[k].art(); TEX.weapons[k] = [TEX.weaponPix[k].texture()]; }
  TEX.heart = [P(S.drawHeart('full'))];
  TEX.halfHeart = [P(S.drawHeart('half'))];
  TEX.blank = [P(S.drawBlank())];
  TEX.coin = [P(S.drawCoin())];
  TEX.ammo = [P(S.drawAmmo())];
  TEX.chest = [P(S.drawChest(false)), P(S.drawChest(true))];
  TEX.chestRare = [P(S.drawChest(false, true)), P(S.drawChest(true, true))];
  TEX.torch = [P(S.drawTorch(0)), P(S.drawTorch(1))];
  TEX.hatch = [P(S.drawHatch(true))];
  TEX.pedestal = [P(S.drawPedestal())];
  TEX.bulletEnemy = P(S.drawBullet(true));
  TEX.bulletPlayer = P(S.drawBullet(false));
  TEX.spark = P(S.drawSpark());
  // ícones para o HUD (data URLs)
  TEX.ui = {
    heart: S.drawHeart('full').dataURL(3), half: S.drawHeart('half').dataURL(3), empty: S.drawHeart('empty').dataURL(3),
    blank: S.drawBlank().dataURL(3),
  };
  return TEX;
}

// Textura de brilho radial (para balas e luzes falsas)
export function glowTexture(color = '#ffffff') {
  const c = document.createElement('canvas');
  c.width = c.height = 32;
  const g = c.getContext('2d');
  const gr = g.createRadialGradient(16, 16, 0, 16, 16, 16);
  gr.addColorStop(0, color); gr.addColorStop(1, 'rgba(0,0,0,0)');
  g.fillStyle = gr; g.fillRect(0, 0, 32, 32);
  const t = toTexture(c);
  t.magFilter = THREE.LinearFilter; t.minFilter = THREE.LinearFilter;
  return t;
}
