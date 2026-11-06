// Efeitos sonoros sintetizados com WebAudio (sem arquivos externos).

let ctx = null;
let master = null;
let noiseBuf = null;
const last = {}; // limita repetição do mesmo som

export function initAudio() {
  if (ctx) { if (ctx.state === 'suspended') ctx.resume(); return; }
  try {
    ctx = new (window.AudioContext || window.webkitAudioContext)();
    master = ctx.createGain();
    master.gain.value = 0.35;
    master.connect(ctx.destination);
    noiseBuf = ctx.createBuffer(1, ctx.sampleRate, ctx.sampleRate);
    const d = noiseBuf.getChannelData(0);
    for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
  } catch { ctx = null; }
}

function tone({ type = 'square', f0 = 440, f1 = f0, dur = 0.1, vol = 0.3, delay = 0 }) {
  const t = ctx.currentTime + delay;
  const o = ctx.createOscillator();
  const g = ctx.createGain();
  o.type = type;
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(Math.max(20, f1), t + dur);
  g.gain.setValueAtTime(vol, t);
  g.gain.exponentialRampToValueAtTime(0.001, t + dur);
  o.connect(g).connect(master);
  o.start(t);
  o.stop(t + dur + 0.02);
}

function noise({ dur = 0.15, vol = 0.3, freq = 1200, q = 0.8, delay = 0, type = 'lowpass' }) {
  const t = ctx.currentTime + delay;
  const s = ctx.createBufferSource();
  s.buffer = noiseBuf;
  const f = ctx.createBiquadFilter();
  f.type = type;
  f.frequency.value = freq;
  f.Q.value = q;
  const g = ctx.createGain();
  g.gain.setValueAtTime(vol, t);
  g.gain.exponentialRampToValueAtTime(0.001, t + dur);
  s.connect(f).connect(g).connect(master);
  s.start(t, Math.random() * 0.5);
  s.stop(t + dur + 0.02);
}

const SFX = {
  shoot: () => { tone({ type: 'square', f0: 900, f1: 220, dur: 0.07, vol: 0.12 }); noise({ dur: 0.05, vol: 0.12, freq: 3000 }); },
  shotgun: () => { noise({ dur: 0.22, vol: 0.35, freq: 1500 }); tone({ type: 'sawtooth', f0: 200, f1: 60, dur: 0.15, vol: 0.15 }); },
  heavy: () => { noise({ dur: 0.3, vol: 0.35, freq: 900 }); tone({ type: 'sawtooth', f0: 320, f1: 40, dur: 0.25, vol: 0.2 }); },
  laser: () => tone({ type: 'sawtooth', f0: 1800, f1: 600, dur: 0.06, vol: 0.08 }),
  bubble: () => tone({ type: 'sine', f0: 300, f1: 900, dur: 0.1, vol: 0.15 }),
  enemyShoot: () => tone({ type: 'triangle', f0: 520, f1: 300, dur: 0.07, vol: 0.06 }),
  hit: () => { tone({ type: 'square', f0: 260, f1: 120, dur: 0.05, vol: 0.1 }); },
  enemyDie: () => { noise({ dur: 0.25, vol: 0.25, freq: 800 }); tone({ type: 'square', f0: 400, f1: 80, dur: 0.2, vol: 0.1 }); },
  hurt: () => { tone({ type: 'sawtooth', f0: 300, f1: 70, dur: 0.35, vol: 0.3 }); noise({ dur: 0.3, vol: 0.3, freq: 600 }); },
  roll: () => noise({ dur: 0.16, vol: 0.15, freq: 500, type: 'bandpass', q: 2 }),
  reload: () => { tone({ type: 'square', f0: 1200, f1: 1100, dur: 0.03, vol: 0.08 }); tone({ type: 'square', f0: 700, f1: 650, dur: 0.04, vol: 0.08, delay: 0.12 }); },
  empty: () => tone({ type: 'square', f0: 200, f1: 180, dur: 0.04, vol: 0.08 }),
  blank: () => { tone({ type: 'sine', f0: 120, f1: 1400, dur: 0.5, vol: 0.35 }); noise({ dur: 0.6, vol: 0.25, freq: 4000, type: 'highpass' }); },
  pickup: () => { tone({ type: 'square', f0: 880, f1: 880, dur: 0.06, vol: 0.1 }); tone({ type: 'square', f0: 1320, f1: 1320, dur: 0.08, vol: 0.1, delay: 0.06 }); },
  coin: () => { tone({ type: 'square', f0: 1500, f1: 1500, dur: 0.04, vol: 0.06 }); tone({ type: 'square', f0: 2000, f1: 2000, dur: 0.06, vol: 0.06, delay: 0.04 }); },
  heal: () => { [523, 659, 784].forEach((f, i) => tone({ type: 'triangle', f0: f, dur: 0.12, vol: 0.15, delay: i * 0.07 })); },
  door: () => { noise({ dur: 0.3, vol: 0.2, freq: 300 }); tone({ type: 'square', f0: 90, f1: 60, dur: 0.25, vol: 0.15 }); },
  chest: () => { [392, 523, 659, 784, 1046].forEach((f, i) => tone({ type: 'square', f0: f, dur: 0.1, vol: 0.1, delay: i * 0.06 })); },
  explode: () => { noise({ dur: 0.5, vol: 0.5, freq: 400 }); tone({ type: 'sine', f0: 120, f1: 30, dur: 0.45, vol: 0.4 }); },
  crate: () => noise({ dur: 0.18, vol: 0.25, freq: 900, type: 'bandpass', q: 1.5 }),
  boss: () => { [110, 98, 82].forEach((f, i) => tone({ type: 'sawtooth', f0: f, f1: f * 0.98, dur: 0.5, vol: 0.2, delay: i * 0.35 })); },
  stairs: () => { [784, 659, 523, 392].forEach((f, i) => tone({ type: 'triangle', f0: f, dur: 0.14, vol: 0.15, delay: i * 0.09 })); },
  victory: () => { [523, 659, 784, 1046, 784, 1046].forEach((f, i) => tone({ type: 'square', f0: f, dur: 0.18, vol: 0.12, delay: i * 0.13 })); },
  buy: () => { tone({ type: 'square', f0: 1000, dur: 0.05, vol: 0.1 }); tone({ type: 'square', f0: 1500, dur: 0.1, vol: 0.1, delay: 0.05 }); },
  deny: () => tone({ type: 'square', f0: 150, f1: 120, dur: 0.15, vol: 0.12 }),
};

export function sfx(name, minGap = 0.03) {
  if (!ctx || !SFX[name]) return;
  const now = ctx.currentTime;
  if (last[name] && now - last[name] < minGap) return;
  last[name] = now;
  try { SFX[name](); } catch { /* ignora */ }
}
