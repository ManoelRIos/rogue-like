// Partículas simples (faíscas, poeira, pedaços) com InstancedMesh colorido.
import * as THREE from 'three';
import { TEX, CAM_PITCH, glowTexture } from './gfx.js';

const K = 1 / Math.cos(CAM_PITCH);

export class Particles {
  constructor(scene) {
    this.list = [];
    const mat = new THREE.MeshBasicMaterial({ map: TEX.spark, alphaTest: 0.5 });
    this.mesh = new THREE.InstancedMesh(new THREE.PlaneGeometry(1, 1), mat, 3000);
    this.mesh.instanceColor = new THREE.InstancedBufferAttribute(new Float32Array(3000 * 3), 3);
    this.mesh.frustumCulled = false; this.mesh.count = 0;
    scene.add(this.mesh);
    // anéis/flashes aditivos (explosões, blanks, clarões de disparo)
    const gmat = new THREE.MeshBasicMaterial({ map: glowTexture('#ffffff'), transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });
    this.glows = new THREE.InstancedMesh(new THREE.PlaneGeometry(1, 1), gmat, 200);
    this.glows.instanceColor = new THREE.InstancedBufferAttribute(new Float32Array(200 * 3), 3);
    this.glows.frustumCulled = false; this.glows.count = 0;
    scene.add(this.glows);
    this.glowList = [];
    this.m = new THREE.Matrix4(); this.q = new THREE.Quaternion(); this.s = new THREE.Vector3(); this.p = new THREE.Vector3(); this.c = new THREE.Color();
  }

  add(o) {
    if (this.list.length > 2900) this.list.shift();
    this.list.push({ vx: 0, vy: 0, vz: 0, grav: 9, size: 0.08, life: 0.5, color: 0xffffff, ...o, max: o.life ?? 0.5 });
  }

  burst(x, y, z, n, color, speed = 3, life = 0.4, size = 0.08, up = 2) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2, s = speed * (0.4 + Math.random() * 0.8);
      this.add({ x, y, z, vx: Math.cos(a) * s, vz: Math.sin(a) * s, vy: Math.random() * up + 0.5, color, life: life * (0.6 + Math.random() * 0.6), size: size * (0.7 + Math.random() * 0.8) });
    }
  }

  flash(x, y, z, size, color = 0xffd080, life = 0.12) {
    if (this.glowList.length > 190) this.glowList.shift();
    this.glowList.push({ x, y, z, size, color, life, max: life });
  }

  update(dt) {
    const { m, q, s, p, c } = this;
    let n = 0;
    for (let i = this.list.length - 1; i >= 0; i--) {
      const o = this.list[i];
      o.life -= dt;
      if (o.life <= 0) { this.list.splice(i, 1); continue; }
      o.vy -= o.grav * dt;
      o.x += o.vx * dt; o.y += o.vy * dt; o.z += o.vz * dt;
      if (o.y < 0.02) { o.y = 0.02; o.vy *= -0.3; o.vx *= 0.6; o.vz *= 0.6; }
      const k = Math.min(1, o.life / o.max * 2);
      p.set(o.x, o.y, o.z); s.set(o.size * k, o.size * k * K, 1);
      m.compose(p, q, s);
      this.mesh.setMatrixAt(n, m);
      this.mesh.setColorAt(n, c.setHex(o.color));
      n++;
    }
    this.mesh.count = n;
    this.mesh.instanceMatrix.needsUpdate = true;
    this.mesh.instanceColor.needsUpdate = true;

    let g = 0;
    for (let i = this.glowList.length - 1; i >= 0; i--) {
      const o = this.glowList[i];
      o.life -= dt;
      if (o.life <= 0) { this.glowList.splice(i, 1); continue; }
      const t = o.life / o.max;
      const sz = o.size * (1.2 - t * 0.4);
      p.set(o.x, o.y, o.z); s.set(sz, sz * K, 1);
      m.compose(p, q, s);
      this.glows.setMatrixAt(g, m);
      this.glows.setColorAt(g, c.setHex(o.color).multiplyScalar(t));
      g++;
    }
    this.glows.count = g;
    this.glows.instanceMatrix.needsUpdate = true;
    this.glows.instanceColor.needsUpdate = true;
  }

  reset() { this.list.length = 0; this.glowList.length = 0; }
}
