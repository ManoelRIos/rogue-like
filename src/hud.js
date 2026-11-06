// Interface: corações, blanks, moedas, arma, minimapa, barra do chefe, avisos.
import { TEX } from './gfx.js';

const $ = (id) => document.getElementById(id);

export class HUD {
  constructor(game) {
    this.g = game;
    this.el = {
      hud: $('hud'), hearts: $('hearts'), blanks: $('blanks'), coins: $('coin-count'),
      wIcon: $('weapon-icon'), wName: $('weapon-name'), mag: $('ammo-mag'), ammo: $('ammo-text'), slots: $('weapon-slots'),
      floor: $('floor-label'), boss: $('boss-bar'), bossName: $('boss-name'), bossFill: $('boss-hp-fill'),
      prompt: $('prompt'), toast: $('toast'), reload: $('reload-hint'), map: $('minimap'), flash: $('flash'),
    };
    this.mapCtx = this.el.map.getContext('2d');
    this.isDirty = true;
    this.mapT = 0;
    this.toastT = 0;
    this.lastPrompt = null;
  }

  dirty() { this.isDirty = true; }
  show(v) { this.el.hud.classList.toggle('hidden', !v); }

  update(dt) {
    const g = this.g, P = g.player;
    if (!P) return;
    if (this.toastT > 0) { this.toastT -= dt; if (this.toastT <= 0) this.el.toast.classList.remove('show'); }
    this.el.reload.classList.toggle('hidden', !(P.reloadT > 0));
    if (P.reloadT > 0) {
      const sp = g.toScreen(P.x, 1.9, P.z);
      this.el.reload.style.left = sp.x + 'px'; this.el.reload.style.top = sp.y + 'px';
    }
    this.mapT -= dt;
    if (this.mapT <= 0) { this.mapT = 0.1; this.drawMinimap(); }
    if (!this.isDirty) return;
    this.isDirty = false;

    // corações
    let h = '';
    for (let i = 0; i < P.maxHp / 2; i++) {
      const v = P.hp - i * 2;
      const src = v >= 2 ? TEX.ui.heart : v === 1 ? TEX.ui.half : TEX.ui.empty;
      h += `<img class="heart" src="${src}">`;
    }
    this.el.hearts.innerHTML = h;
    let b = '';
    for (let i = 0; i < P.blanks; i++) b += `<img class="blank-ico" src="${TEX.ui.blank}">`;
    this.el.blanks.innerHTML = b;
    this.el.coins.textContent = P.coins;

    // arma
    const w = P.weapon, d = w.def;
    this.el.wName.textContent = d.name;
    this.el.mag.style.width = `${(w.mag / d.mag) * 100}%`;
    this.el.ammo.textContent = `${w.mag} / ${w.ammo === Infinity ? '∞' : w.ammo}`;
    if (this.lastWeapon !== w.id) {
      this.lastWeapon = w.id;
      const c = this.el.wIcon.getContext('2d');
      c.imageSmoothingEnabled = false;
      c.clearRect(0, 0, 96, 48);
      const pix = TEX.weaponPix[w.id];
      const s = Math.floor(Math.min(88 / pix.w, 44 / pix.h));
      c.drawImage(pix.canvas(s), (96 - pix.w * s) / 2, (48 - pix.h * s) / 2);
    }
    this.el.slots.innerHTML = P.weapons.map((x, i) => `<span class="${i === P.cur ? 'on' : ''}">${i + 1}</span>`).join('');
    this.el.floor.textContent = `Andar ${g.floorIndex + 1} — ${g.theme.name}`;
  }

  setPrompt(html) {
    if (html === this.lastPrompt) return;
    this.lastPrompt = html;
    if (!html) { this.el.prompt.classList.add('hidden'); return; }
    this.el.prompt.innerHTML = html;
    this.el.prompt.classList.remove('hidden');
  }

  toast(title, sub = '', time = 2.2) {
    this.el.toast.innerHTML = `${title}${sub ? `<small>${sub}</small>` : ''}`;
    this.el.toast.classList.add('show');
    this.toastT = time;
  }

  bossBar(name) {
    if (!name) { this.el.boss.classList.add('hidden'); return; }
    this.el.bossName.textContent = name;
    this.el.boss.classList.remove('hidden');
    this.el.bossFill.style.width = '100%';
  }
  bossHp(f) { this.el.bossFill.style.width = `${Math.max(0, f) * 100}%`; }

  flash(color, opacity) {
    const f = this.el.flash;
    f.style.transition = 'none';
    f.style.background = color;
    f.style.opacity = opacity;
    requestAnimationFrame(() => { f.style.transition = 'opacity .35s'; f.style.opacity = 0; });
  }

  drawMinimap() {
    const g = this.g, d = g.dungeon, c = this.mapCtx, P = g.player;
    if (!d) return;
    const cw = this.el.map.width, ch = this.el.map.height;
    c.clearRect(0, 0, cw, ch);
    // limites das salas conhecidas
    const known = d.rooms.filter((r) => r.discovered);
    if (!known.length) return;
    let x0 = Infinity, z0 = Infinity, x1 = -Infinity, z1 = -Infinity;
    for (const r of d.rooms) { x0 = Math.min(x0, r.x0); z0 = Math.min(z0, r.z0); x1 = Math.max(x1, r.x0 + r.w); z1 = Math.max(z1, r.z0 + r.h); }
    const s = Math.min((cw - 12) / (x1 - x0), (ch - 12) / (z1 - z0));
    const ox = (cw - (x1 - x0) * s) / 2 - x0 * s, oz = (ch - (z1 - z0) * s) / 2 - z0 * s;
    // corredores
    c.fillStyle = '#5a4a52';
    for (const cor of d.corridors) {
      const pts = [[cor.x0, cor.z0], [cor.x1, cor.z1]];
      const vis = pts.some(([x, z]) => {
        for (const r of known) if (x >= r.x0 - 2 && x <= r.x0 + r.w + 1 && z >= r.z0 - 2 && z <= r.z0 + r.h + 1 && r.visited) return true;
        return false;
      });
      if (vis) c.fillRect(ox + cor.x0 * s, oz + cor.z0 * s, (cor.x1 - cor.x0 + 1) * s, (cor.z1 - cor.z0 + 1) * s);
    }
    for (const r of known) {
      const cur = g.currentRoom === r;
      c.fillStyle = cur ? '#c9a46a' : r.visited ? '#6e5a62' : '#2e2329';
      c.fillRect(ox + r.x0 * s, oz + r.z0 * s, r.w * s, r.h * s);
      c.strokeStyle = r.visited ? '#000' : '#6e5a62';
      c.lineWidth = 1;
      c.strokeRect(ox + r.x0 * s + 0.5, oz + r.z0 * s + 0.5, r.w * s - 1, r.h * s - 1);
      const icon = { boss: '#ff4d5e', treasure: '#ffd35a', shop: '#7dffb0' }[r.type];
      if (icon) {
        c.fillStyle = icon;
        const cx = ox + (r.x0 + r.w / 2) * s, cz = oz + (r.z0 + r.h / 2) * s;
        c.fillRect(cx - 3, cz - 3, 6, 6);
        if (r.type === 'boss' && r.cleared) { c.fillStyle = '#000'; c.fillRect(cx - 1, cz - 1, 2, 2); }
      }
    }
    c.fillStyle = '#ffffff';
    c.fillRect(ox + P.x * s - 2, oz + P.z * s - 2, 4, 4);
  }
}
