// Sintetiza los efectos de sonido de "Cuidando al Mayu" (sin muestras externas) y los guarda en Assets/Audio.
// Uso:  node generar_sfx.js        Si cambias un efecto, vuelve a ejecutarlo y Unity lo reimporta.
const fs = require("fs");
const path = require("path");
const OUT = path.join(__dirname, "..", "Assets", "Audio");
const SR = 44100;
const TAU = Math.PI * 2;

// ---------- bloques de sintesis ----------
let seed = 12345;
function rnd() { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296 * 2 - 1; }
const note = n => 440 * Math.pow(2, (n - 69) / 12);               // numero MIDI -> Hz
const osc = {
  sine: p => Math.sin(p * TAU),
  tri: p => 4 * Math.abs(p - Math.floor(p + 0.5)) - 1,
  saw: p => 2 * (p - Math.floor(p + 0.5)),
  sq: (p, d = 0.5) => (p - Math.floor(p)) < d ? 1 : -1,
};
// voz con frecuencia variable: f(t) en Hz; mantiene la fase continua
function voice(buf, start, dur, f, wave, amp, env) {
  let ph = 0;
  const i0 = Math.round(start * SR), n = Math.round(dur * SR);
  for (let i = 0; i < n && i0 + i < buf.length; i++) {
    const t = i / SR;
    ph += f(t) / SR;
    buf[i0 + i] += wave(ph) * amp * env(t, dur);
  }
}
function noiseBurst(buf, start, dur, amp, env, cutoff) {            // ruido con filtro pasa-bajos de corte variable
  const i0 = Math.round(start * SR), n = Math.round(dur * SR);
  let y = 0;
  for (let i = 0; i < n && i0 + i < buf.length; i++) {
    const t = i / SR, k = Math.min(1, 2 * Math.PI * (typeof cutoff === "function" ? cutoff(t / dur) : cutoff) / SR);
    y += k * (rnd() - y);
    buf[i0 + i] += y * amp * env(t, dur);
  }
}
const decay = tau => (t) => Math.exp(-t / tau);
const adsr = (a, tau) => (t) => Math.min(1, t / a) * Math.exp(-t / tau);
const hold = (a, r) => (t, d) => Math.min(1, t / a) * Math.min(1, (d - t) / r);
function echo(buf, delay, fb, times) {
  const d = Math.round(delay * SR);
  for (let k = 1; k <= times; k++) for (let i = buf.length - 1; i >= d * k; i--) buf[i] += buf[i - d * k] * Math.pow(fb, k);
}
function drive(buf, g) { for (let i = 0; i < buf.length; i++) buf[i] = Math.tanh(buf[i] * g) / Math.tanh(g); }
const LOUD = { sfx_impacto: 0.36, sfx_patada_fuerte: 0.38, sfx_pleno: 0.33, sfx_victoria: 0.28, sfx_derrota: 0.28 };
function save(name, buf) {
  // igualar la sonoridad (RMS) entre efectos y limitar picos con una curva suave
  let e = 0; for (const s of buf) e += s * s;
  const rms = Math.sqrt(e / buf.length) || 1, target = LOUD[name.replace(".wav", "")] || 0.28;
  const k = target / rms, n = buf.length, fade = Math.round(0.006 * SR);
  for (let i = 0; i < n; i++) buf[i] = Math.tanh(buf[i] * k * 0.9) * 0.95 / k;
  const data = Buffer.alloc(44 + n * 2);
  data.write("RIFF", 0); data.writeUInt32LE(36 + n * 2, 4); data.write("WAVEfmt ", 8); data.writeUInt32LE(16, 16);
  data.writeUInt16LE(1, 20); data.writeUInt16LE(1, 22); data.writeUInt32LE(SR, 24); data.writeUInt32LE(SR * 2, 28);
  data.writeUInt16LE(2, 32); data.writeUInt16LE(16, 34); data.write("data", 36); data.writeUInt32LE(n * 2, 40);
  for (let i = 0; i < n; i++) {
    const f = Math.min(1, (n - i) / fade);
    data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, buf[i] * k * f)) * 32767), 44 + i * 2);
  }
  fs.writeFileSync(path.join(OUT, name), data);
  console.log(name.padEnd(22), (n / SR).toFixed(2) + " s");
}
const mk = sec => new Float32Array(Math.round(sec * SR));
const sq25 = p => osc.sq(p, 0.25), sq125 = p => osc.sq(p, 0.125);

// ---------- efectos ----------
// Patada: "swish" de aire que sube y baja + chasquido corto
let b = mk(0.3);
noiseBurst(b, 0, 0.26, 1.0, (t, d) => Math.sin(Math.PI * Math.min(1, t / d)) ** 1.5, u => 500 + 5000 * Math.sin(Math.PI * u));
voice(b, 0, 0.12, t => 520 - 2500 * t, sq25, 0.35, decay(0.04));
save("sfx_patada.wav", b);

// Impacto: golpe grave + crujido + estrellita
b = mk(0.55);
voice(b, 0, 0.4, t => 40 + 160 * Math.exp(-t * 25), osc.sine, 1.2, decay(0.11));
noiseBurst(b, 0, 0.12, 0.9, decay(0.03), 5000);
voice(b, 0, 0.18, t => 900 * Math.exp(-t * 6), sq25, 0.35, decay(0.035));
voice(b, 0.05, 0.35, t => 1760, osc.sine, 0.22, decay(0.09));
drive(b, 2.2);
save("sfx_impacto.wav", b);

// Recoger: monedita de dos notas
b = mk(0.3);
voice(b, 0, 0.07, t => note(83), sq125, 0.5, decay(0.09));
voice(b, 0.07, 0.23, t => note(88), sq125, 0.5, decay(0.07));
voice(b, 0.07, 0.23, t => note(100), osc.sine, 0.12, decay(0.06));
echo(b, 0.07, 0.3, 2);
save("sfx_recoger.wav", b);

// Reciclar: caja registradora "ka-ching"
b = mk(0.9);
noiseBurst(b, 0, 0.05, 0.9, decay(0.015), 2500);
voice(b, 0, 0.1, t => 160, osc.sq, 0.35, decay(0.03));
for (const [t0, n] of [[0.08, 91], [0.16, 96]]) {
  voice(b, t0, 0.7, t => note(n) * (1 + 0.003 * Math.sin(t * 40)), osc.sine, 0.5, decay(0.2));
  voice(b, t0, 0.5, t => note(n) * 2.76, osc.sine, 0.2, decay(0.12));
}
save("sfx_reciclar.wav", b);

// Chapoteo: salpicadura + burbujas
b = mk(0.7);
noiseBurst(b, 0, 0.5, 1.0, (t, d) => Math.min(1, t / 0.01) * Math.exp(-t / 0.13), u => 3500 * Math.exp(-u * 2.2) + 250);
for (const [t0, f0] of [[0.1, 350], [0.19, 450], [0.29, 300], [0.37, 520]])
  voice(b, t0, 0.1, t => f0 + 1800 * t, osc.sine, 0.35, decay(0.03));
save("sfx_chapoteo.wav", b);

// Alerta: sirenita "ba-ding" dos veces
b = mk(0.7);
for (const t0 of [0, 0.3]) {
  voice(b, t0, 0.13, t => 784, osc.tri, 0.8, hold(0.005, 0.03));
  voice(b, t0 + 0.14, 0.15, t => 1047, osc.tri, 0.8, hold(0.005, 0.05));
  voice(b, t0 + 0.14, 0.15, t => 2094, osc.sine, 0.2, decay(0.08));
}
save("sfx_alerta.wav", b);

// Comprar: arpegio alegre con destello
b = mk(0.75);
[72, 76, 79, 84].forEach((n, i) => voice(b, i * 0.065, 0.18, t => note(n), p => osc.sq(p, 0.4), 0.4, decay(0.07)));
voice(b, 0.26, 0.45, t => note(96) * (1 + 0.004 * Math.sin(t * 50)), osc.sine, 0.35, decay(0.14));
echo(b, 0.11, 0.35, 3);
save("sfx_comprar.wav", b);

// Negado: "bzzt bzzt" grave
b = mk(0.4);
for (const t0 of [0, 0.17]) voice(b, t0, 0.14, t => 140 - 120 * t, osc.saw, 0.9, hold(0.004, 0.03));
drive(b, 2.5);
save("sfx_negado.wav", b);

// Clic: pop corto
b = mk(0.1);
voice(b, 0, 0.06, t => 1500 * Math.exp(-t * 30) + 400, osc.sine, 0.9, decay(0.02));
noiseBurst(b, 0, 0.01, 0.4, decay(0.004), 6000);
save("sfx_clic.wav", b);

// Victoria: fanfarria con eco
b = mk(2.2);
[[72, 0, 0.14], [76, 0.14, 0.14], [79, 0.28, 0.14], [84, 0.42, 0.9]].forEach(([n, t0, d]) => {
  voice(b, t0, d + 0.4, t => note(n), p => osc.sq(p, 0.35), 0.35, adsr(0.005, d * 0.9 + 0.1));
  voice(b, t0, d + 0.4, t => note(n - 12), osc.tri, 0.35, adsr(0.005, d * 0.9 + 0.1));
});
[96, 100, 103, 108].forEach((n, i) => voice(b, 0.5 + i * 0.07, 0.6, t => note(n), osc.sine, 0.18, decay(0.15)));
echo(b, 0.22, 0.4, 3);
save("sfx_victoria.wav", b);

// Derrota: trombon triste "wa wa wa waaa"
b = mk(2.0);
[[58, 0, 0.3], [57, 0.34, 0.3], [56, 0.68, 0.3], [55, 1.02, 0.9]].forEach(([n, t0, d], k) => {
  const long = k === 3;
  voice(b, t0, d, t => note(n) * (long ? 1 + 0.02 * Math.sin(t * 30) * Math.min(1, t * 3) - 0.03 * t : 1), osc.saw, 0.55, hold(0.03, 0.08));
});
// "wah": el brillo se cierra en cada nota (filtro pasa-bajos de corte descendente)
let y = 0; for (let i = 0; i < b.length; i++) { const t = (i / SR) % 0.34; const k = Math.min(1, TAU * (350 + 1800 * Math.exp(-t * 7)) / SR); y += k * (b[i] - y); b[i] = y; }
save("sfx_derrota.wav", b);

// ---- efectos nuevos ----
// Cargar: tic ascendente (el juego le sube el tono)
b = mk(0.12);
voice(b, 0, 0.1, t => 600 + 2500 * t, osc.sq, 0.5, hold(0.003, 0.03));
save("sfx_cargar.wav", b);

// Patada fuerte: aire grande + estallido grave + latigazo
b = mk(0.8);
noiseBurst(b, 0, 0.4, 1.0, (t, d) => Math.sin(Math.PI * Math.min(1, t / d)) ** 1.2, u => 300 + 7000 * Math.sin(Math.PI * u));
voice(b, 0.12, 0.6, t => 30 + 140 * Math.exp(-t * 14), osc.sine, 1.4, decay(0.2));
voice(b, 0.1, 0.35, t => 1500 * Math.exp(-t * 8) + 100, osc.saw, 0.5, decay(0.07));
noiseBurst(b, 0.12, 0.2, 0.9, decay(0.05), 6000);
drive(b, 2.6);
echo(b, 0.09, 0.25, 2);
save("sfx_patada_fuerte.wav", b);

// Convencer: campanitas de "tienes razon"
b = mk(1.0);
[[79, 0], [83, 0.1], [86, 0.2], [91, 0.3]].forEach(([n, t0]) => {
  voice(b, t0, 0.7, t => note(n), osc.sine, 0.5, decay(0.16));
  voice(b, t0, 0.4, t => note(n) * 3.01, osc.sine, 0.12, decay(0.07));
});
echo(b, 0.15, 0.3, 2);
save("sfx_convencer.wav", b);

// Rechazo: "nop" comico
b = mk(0.45);
voice(b, 0, 0.14, t => 330 - 500 * t, p => osc.sq(p, 0.3), 0.6, hold(0.004, 0.04));
voice(b, 0.17, 0.25, t => 220 - 300 * t + 12 * Math.sin(t * 60), p => osc.sq(p, 0.3), 0.6, hold(0.004, 0.08));
save("sfx_rechazo.wav", b);

// Combo: "zing" ascendente
b = mk(0.4);
voice(b, 0, 0.3, t => 500 * Math.pow(4, t / 0.3), p => osc.sq(p, 0.4), 0.35, hold(0.004, 0.1));
voice(b, 0.18, 0.2, t => note(100), osc.sine, 0.3, decay(0.08));
echo(b, 0.08, 0.3, 2);
save("sfx_combo.wav", b);

// Logro: jingle corto de trofeo
b = mk(1.6);
[[84, 0], [88, 0.09], [91, 0.18], [96, 0.27], [100, 0.5], [103, 0.58], [108, 0.66]].forEach(([n, t0]) => {
  voice(b, t0, 0.5, t => note(n), p => osc.sq(p, 0.5), 0.28, decay(0.12));
  voice(b, t0, 0.8, t => note(n) * 2, osc.sine, 0.14, decay(0.25));
});
echo(b, 0.18, 0.35, 3);
save("sfx_logro.wav", b);

// Estrella: destello brillante
b = mk(0.8);
[[96, 0], [103, 0.07], [108, 0.14]].forEach(([n, t0]) => voice(b, t0, 0.5, t => note(n), osc.sine, 0.5, decay(0.12)));
noiseBurst(b, 0, 0.15, 0.1, decay(0.04), 9000);
echo(b, 0.12, 0.35, 3);
save("sfx_estrella.wav", b);

// Nutria: chillidos "chii-chii"
b = mk(0.35);
for (const t0 of [0, 0.16]) voice(b, t0, 0.13, t => 2000 + 900 * Math.sin(Math.PI * t / 0.13) + 60 * Math.sin(t * 160), osc.sine, 0.8, hold(0.01, 0.04));
save("sfx_nutria.wav", b);

// Plantar: plop + susurro + brote creciendo
b = mk(0.9);
voice(b, 0, 0.12, t => 300 * Math.exp(-t * 14) + 90, osc.sine, 1.0, decay(0.05));
noiseBurst(b, 0.08, 0.25, 0.25, (t, d) => Math.sin(Math.PI * t / d), 2500);
[76, 79, 83, 88].forEach((n, i) => voice(b, 0.28 + i * 0.1, 0.35, t => note(n), osc.tri, 0.4, decay(0.1)));
echo(b, 0.12, 0.25, 2);
save("sfx_plantar.wav", b);

// Rescate: salto de agua + ding
b = mk(0.7);
voice(b, 0, 0.18, t => 250 + 2500 * t, osc.sine, 0.8, hold(0.005, 0.05));
noiseBurst(b, 0, 0.14, 0.4, decay(0.04), 4500);
voice(b, 0.17, 0.5, t => note(88), osc.sine, 0.5, decay(0.14));
voice(b, 0.17, 0.5, t => note(95), osc.sine, 0.25, decay(0.1));
save("sfx_rescate.wav", b);

// Pleno: tres golpes y un "crash" de bolos
b = mk(1.1);
[[0, 60], [0.09, 64], [0.18, 67]].forEach(([t0, n]) => {
  voice(b, t0, 0.2, t => note(n) * Math.exp(-t * 4), osc.sine, 0.8, decay(0.07));
  noiseBurst(b, t0, 0.05, 0.5, decay(0.015), 4000);
});
noiseBurst(b, 0.27, 0.7, 0.9, decay(0.16), t => 9000 * Math.exp(-t * 2) + 600);
[72, 76, 79, 84].forEach(n => voice(b, 0.27, 0.8, t => note(n), p => osc.sq(p, 0.4), 0.18, decay(0.25)));
drive(b, 1.8);
echo(b, 0.14, 0.3, 2);
save("sfx_pleno.wav", b);
