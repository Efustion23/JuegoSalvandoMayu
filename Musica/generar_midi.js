// Genera los MIDI de "Cuidando al Mayu". Uso:  node generar_midi.js   (crea los .mid en esta carpeta)
// Cada pieza define tempo, tonalidad y, por compas, el acorde; las melodias se escriben como texto ("E5:1.5 D5:.5 r:1").
const fs = require("fs");
const path = require("path");
const PPQ = 480;

// ---------- utilidades ----------
const NOTE = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };
function midi(name) {                      // "C#5" -> 72, "Bb3" -> 58
  const m = /^([A-G])(#|b)?(-?\d)$/.exec(name);
  if (!m) throw new Error("nota invalida: " + name);
  return 12 * (parseInt(m[3]) + 1) + NOTE[m[1]] + (m[2] === "#" ? 1 : m[2] === "b" ? -1 : 0);
}
function vlq(n) { const b = [n & 0x7f]; while ((n >>= 7)) b.unshift((n & 0x7f) | 0x80); return b; }
function hash(i) { let h = (i * 2654435761) >>> 0; h ^= h >> 15; return (h % 1000) / 1000; }

class Track {
  constructor(name, channel, program) { this.name = name; this.ch = channel; this.prog = program; this.ev = []; this.n = 0; }
  add(beat, note, dur, vel) {              // beat en negras desde el inicio de la pieza
    const on = Math.round(beat * PPQ), off = Math.round((beat + dur) * PPQ) - 1;
    const v = Math.max(1, Math.min(127, Math.round(vel + (hash(this.n++ + this.ch * 97) - 0.5) * 10)));   // humanizacion leve de velocidad
    this.ev.push([on, 0x90 | this.ch, note, v], [off, 0x80 | this.ch, note, 0]);
  }
  // melodia en texto: "E5:1.5 D5:.5 r:1" (duraciones en negras); varias notas con "+" = acorde
  line(startBeat, text, vel = 90, legato = 0.92) {
    let b = startBeat;
    const total = text.trim().split(/\s+/).reduce((a, t) => a + parseFloat(t.split(":")[1]), 0);
    if (Math.abs(total % 4) > 0.001 && Math.abs(total % 4 - 4) > 0.001) throw new Error(this.name + ": la melodia no suma compases enteros (" + total + " negras): " + text.slice(0, 40));
    for (const tok of text.trim().split(/\s+/)) {
      const [n, d] = tok.split(":"); const dur = parseFloat(d);
      if (n !== "r") for (const one of n.split("+")) this.add(b, midi(one), dur * legato, vel);
      b += dur;
    }
    return b;
  }
  bytes() {
    const e = [[0, 0xc0 | this.ch, this.prog, null]].concat(this.ev.slice().sort((a, b) => a[0] - b[0] || (a[1] & 0xf0) - (b[1] & 0xf0)));
    const out = [0, 0xff, 0x03, this.name.length, ...Buffer.from(this.name)];
    let last = 0;
    for (const [t, st, a, b] of e) { out.push(...vlq(t - last), st, a); if (b !== null) out.push(b); last = t; }
    out.push(0, 0xff, 0x2f, 0);
    return out;
  }
}
function drumTrack(name) { const t = new Track(name, 9, 0); return t; }

function writeMidi(file, { bpm, key, bars, tracks }) {
  const us = Math.round(60000000 / bpm);
  const tempo = [0, 0xff, 0x51, 3, (us >> 16) & 255, (us >> 8) & 255, us & 255, 0, 0xff, 0x58, 4, 4, 2, 24, 8, 0xff, 0x2f, 0];
  const conductor = [0, 0xff, 0x03, 9, ...Buffer.from("Conductor"), ...tempo];
  const chunks = [conductor, ...tracks.map(t => t.bytes())].map(d => Buffer.concat([Buffer.from("MTrk"), u32(d.length), Buffer.from(d)]));
  const head = Buffer.concat([Buffer.from("MThd"), u32(6), Buffer.from([0, 1, 0, chunks.length, PPQ >> 8, PPQ & 255])]);
  fs.writeFileSync(path.join(__dirname, file), Buffer.concat([head, ...chunks]));
  console.log(file.padEnd(34), bpm + " bpm", String(bars).padStart(2) + " compases", (bars * 4 * 60 / bpm).toFixed(1) + " s");
}
function u32(n) { return Buffer.from([(n >>> 24) & 255, (n >>> 16) & 255, (n >>> 8) & 255, n & 255]); }

// ---------- acordes (notas MIDI; raiz en bajo = primer elemento de "b") ----------
const CH = {
  Am: { pad: ["A3", "C4", "E4", "A4"], arp: ["A3", "E4", "A4", "C5"], b: "A2" },
  G: { pad: ["G3", "B3", "D4", "G4"], arp: ["G3", "D4", "G4", "B4"], b: "G2" },
  F: { pad: ["F3", "A3", "C4", "F4"], arp: ["F3", "C4", "F4", "A4"], b: "F2" },
  E: { pad: ["E3", "G#3", "B3", "E4"], arp: ["E3", "B3", "E4", "G#4"], b: "E2" },
  Dm: { pad: ["D3", "A3", "C4", "F4", "E4"], arp: ["D3", "A3", "D4", "F4"], b: "D2" },
  Gm: { pad: ["G3", "Bb3", "D4", "F4"], arp: ["G3", "D4", "F4", "Bb4"], b: "G2" },
  Bb: { pad: ["Bb3", "D4", "F4", "A4"], arp: ["Bb3", "F4", "A4", "D5"], b: "Bb2" },
  A7: { pad: ["A3", "C#4", "E4", "G4"], arp: ["A3", "E4", "G4", "C#5"], b: "A2" },
  C: { pad: ["C4", "E4", "G4", "B4"], arp: ["C4", "G4", "B4", "E5"], b: "C3" },
  D: { pad: ["D4", "F#4", "A4", "D5"], arp: ["D4", "A4", "D5", "F#5"], b: "D3" },
  A: { pad: ["A3", "C#4", "E4", "A4"], arp: ["A3", "E4", "A4", "C#5"], b: "A2" },
};

// ---------- patrones ----------
function pad(tr, bar, chord, vel = 55) { for (const n of CH[chord].pad) tr.add(bar * 4, midi(n), 3.95, vel); }
function arp(tr, bar, chord, vel = 62, pattern = [0, 2, 1, 2, 3, 2, 1, 2]) {      // 8 corcheas por compas (estilo charango)
  const a = CH[chord].arp;
  pattern.forEach((idx, i) => tr.add(bar * 4 + i * 0.5, midi(a[idx]), 0.45, vel + (i % 2 === 0 ? 8 : 0)));
}
function bassHuayno(tr, bar, chord, vel = 85) {       // negra, negra con puntillo + corchea... (pulso de huayno)
  const r = midi(CH[chord].b), f = r + 7;
  tr.add(bar * 4, r, 1.4, vel); tr.add(bar * 4 + 1.5, r, 0.45, vel - 15);
  tr.add(bar * 4 + 2, r, 1.4, vel - 5); tr.add(bar * 4 + 3.5, f, 0.45, vel - 20);
}
function bassHalf(tr, bar, chord, vel = 80) { tr.add(bar * 4, midi(CH[chord].b), 3.8, vel); }
function drumsHuayno(tr, bar, vel = 80, full = true) {
  const b = bar * 4;
  tr.add(b, 36, 0.3, vel + 12); tr.add(b + 2, 36, 0.3, vel + 5);                       // bombo
  for (const x of [1, 1.5, 3, 3.5]) tr.add(b + x, 61, 0.2, vel - 22);                    // bongo grave (respuesta del bombo)
  if (full) for (let i = 0; i < 8; i++) tr.add(b + i * 0.5, 70, 0.2, i % 2 ? 40 : 52);   // shaker en corcheas
}
function drumsBoomBap(tr, bar, vel = 85, fill = false) {
  const b = bar * 4;
  for (const x of [0, 1.75, 2.5]) tr.add(b + x, 36, 0.25, vel + (x === 0 ? 10 : 0));
  tr.add(b + 1, 38, 0.25, vel + 8); tr.add(b + 3, 38, 0.25, vel + 8);
  tr.add(b + 1, 39, 0.25, vel - 30); tr.add(b + 3, 39, 0.25, vel - 30);
  for (let i = 0; i < 8; i++) tr.add(b + i * 0.5, 42, 0.2, i % 2 ? 42 : 62);             // hi-hat (aplicar swing en FL)
  tr.add(b + 3.5, 46, 0.4, 50);
  if (fill) { tr.add(b + 3.25, 38, 0.1, 60); tr.add(b + 3.75, 38, 0.1, 70); }
}

// =====================================================================================
// 02  SECTOR 1 · SENDERO DE LAS LADERAS   — La menor (pentatonica), 92 bpm, 32 compases
// =====================================================================================
const S1A = [  // melodia A (8 compases)
  "E5:1.5 D5:.5 C5:1 A4:1", "D5:1 E5:1 G5:2", "E5:1.5 D5:.5 C5:1 D5:1", "C5:1 D5:1 E5:2",
  "A5:1.5 G5:.5 E5:1 G5:1", "E5:1 D5:1 E5:2", "C5:1 D5:1 E5:1 D5:1", "C5:1 B4:1 A4:2"];
const S1B = [  // melodia B (8 compases, mas aguda)
  "A5:2 G5:1 E5:1", "G5:1.5 E5:.5 D5:2", "C5:1 D5:1 E5:2", "E5:1 G5:1 A5:2",
  "A5:1.5 G5:.5 E5:1 D5:1", "E5:1 G5:1 D5:2", "B4:1 C5:1 D5:1 E5:1", "E5:3 r:1"];
const S1_PROG_A = ["Am", "G", "F", "E", "Am", "G", "F", "E"];
const S1_PROG_B = ["F", "G", "Am", "Am", "F", "G", "E", "E"];

function sector1() {
  const flute = new Track("Quena (flauta)", 0, 73), pan = new Track("Zampona (octava arriba, opcional)", 1, 75),
    guitar = new Track("Charango (arpegio)", 2, 24), bass = new Track("Bajo", 3, 33), padT = new Track("Pad suave", 4, 89), dr = drumTrack("Percusion huayno");
  const sections = [["intro", 4, ["Am", "Am", "G", "G"]], ["A", 8, S1_PROG_A], ["B", 8, S1_PROG_B], ["A2", 8, S1_PROG_A], ["outro", 4, ["F", "G", "E", "Am"]]];
  let bar = 0;
  for (const [name, n, prog] of sections) {
    for (let i = 0; i < n; i++) {
      const c = prog[i], b = bar + i;
      pad(padT, b, c, name === "B" ? 62 : 50);
      if (name !== "intro" || i >= 2) arp(guitar, b, c, name === "B" ? 70 : 58);
      if (name !== "intro") bassHuayno(bass, b, c, name === "outro" ? 70 : 85); else if (i >= 2) bassHalf(bass, b, c, 65);
      if (name !== "intro" || i >= 3) drumsHuayno(dr, b, name === "B" ? 88 : 74, name !== "outro");
    }
    bar += n;
  }
  // melodias
  flute.line(2 * 4, "D5:4 E5:2 D5:2", 70);                                   // entra en el compas 3 del intro
  flute.line(4 * 4, S1A.join(" "), 92);
  flute.line(12 * 4, S1B.join(" "), 100);
  flute.line(20 * 4, S1A.map((s, i) => i === 7 ? "C5:1 B4:1 A4:1 C5:1" : s).join(" "), 94);
  pan.line(12 * 4, S1B.map(s => s.replace(/([A-G]#?)(\d)/g, (m, n, o) => n + (parseInt(o) + 1))).join(" "), 55);    // B una octava arriba
  flute.line(28 * 4, "A4:2 C5:2 D5:2 C5:2 B4:2 C5:1 B4:1 A4:4", 80);
  writeMidi("02_Sector1_Laderas.mid", { bpm: 92, key: "Am", bars: 32, tracks: [flute, pan, guitar, bass, padT, dr] });
}

// =====================================================================================
// 01  MENU PRINCIPAL — La menor, 84 bpm, 16 compases (tranquilo, sin percusion fuerte)
// =====================================================================================
function menu() {
  const flute = new Track("Quena (flauta)", 0, 73), guitar = new Track("Charango (arpegio)", 2, 24), bass = new Track("Bajo", 3, 33), padT = new Track("Pad suave", 4, 89), dr = drumTrack("Shaker suave");
  const prog = ["Am", "G", "F", "G", "Am", "G", "F", "E", "Am", "G", "F", "G", "F", "G", "E", "Am"];
  prog.forEach((c, b) => {
    pad(padT, b, c, 60);
    arp(guitar, b, c, 56, b < 8 ? [0, 2, 1, 2, 0, 2, 1, 2] : [0, 2, 3, 2, 1, 2, 3, 2]);
    bassHalf(bass, b, c, 62);
    for (let i = 0; i < 8; i++) dr.add(b * 4 + i * 0.5, 70, 0.2, i % 2 ? 30 : 42);
    if (b % 2 === 0) dr.add(b * 4, 36, 0.3, 60);
  });
  const m1 = ["r:4", "r:4", "C5:3 D5:1", "E5:4", "A5:3 G5:1", "E5:2 D5:2", "C5:3 D5:1", "E5:4"];                      // compases 1-8
  const m2 = ["E5:2 G5:2", "A5:2 G5:2", "E5:3 D5:1", "C5:2 D5:2", "E5:2 D5:1 C5:1", "D5:2 B4:2", "C5:2 B4:2", "A4:4"];   // 9-16 (cierra en La y vuelve al inicio)
  flute.line(0, m1.join(" "), 80);
  flute.line(8 * 4, m2.join(" "), 88);
  writeMidi("01_Menu.mid", { bpm: 84, key: "Am", bars: 16, tracks: [flute, guitar, bass, padT, dr] });
}

// =====================================================================================
// 03  SECTOR 2 · PUENTE URBANO — Re menor (dorico), 100 bpm, 32 compases, chillhop con swing
// =====================================================================================
function sector2() {
  const lead = new Track("Vibrafono (melodia)", 0, 11), rhodes = new Track("Piano electrico (acordes)", 1, 4), bass = new Track("Bajo electrico", 2, 34),
    pluck = new Track("Pizzicato (contrapunto)", 3, 45), dr = drumTrack("Bateria boom-bap");
  const P1 = ["Dm", "Gm", "Bb", "A7"], P2 = ["Bb", "A7", "Dm", "Dm"];
  const sections = [["intro", 4, P1], ["A", 8, P1.concat(P1)], ["B", 8, P2.concat(["Bb", "A7", "Gm", "A7"])], ["A2", 8, P1.concat(P1)], ["outro", 4, ["Bb", "Gm", "A7", "Dm"]]];
  let bar = 0;
  for (const [name, n, prog] of sections) {
    for (let i = 0; i < n; i++) {
      const c = prog[i], b = bar + i;
      // acorde sincopado (charleston): 1, y 2&
      const t = b * 4;
      for (const note of CH[c].pad) { rhodes.add(t, midi(note), 1.4, 62); rhodes.add(t + 2.5, midi(note), 1.2, 52); }
      // bajo
      const r = midi(CH[c].b);
      if (name !== "intro" || i >= 2) { bass.add(t, r, 1.2, 88); bass.add(t + 1.75, r + 12, 0.4, 70); bass.add(t + 2.5, r, 1.0, 82); bass.add(t + 3.5, r + 7, 0.4, 70); }
      // bateria
      if (name !== "intro" || i >= 3) drumsBoomBap(dr, b, name === "B" ? 92 : 82, i === n - 1 && name !== "outro");
      // pizzicato en B
      if (name === "B") { const a = CH[c].arp; [0.5, 1.5, 3].forEach((x, k) => pluck.add(t + x, midi(a[(k + 1) % 4]) + 12, 0.3, 60)); }
    }
    bar += n;
  }
  const VA = [  // melodia A (8) en Re dorico: D F G A C
    "A4:1 C5:.5 D5:1.5 A4:1", "G4:1 A4:1 C5:2", "D5:1.5 C5:.5 A4:1 F4:1", "E4:2 G4:1 A4:1",
    "A4:1 C5:.5 D5:1.5 F5:1", "D5:1 C5:1 A4:2", "F5:1.5 D5:.5 C5:1 A4:1", "A4:3 r:1"];
  const VB = [
    "F5:2 D5:1 C5:1", "E5:1 D5:1 C5:2", "D5:1.5 F5:.5 A5:2", "G5:3 E5:1",
    "F5:1 A5:1 G5:1 F5:1", "E5:2 C5:2", "D5:2 F5:1 A4:1", "A4:2 C5:1 E5:1"];
  lead.line(2 * 4, "D5:4 C5:2 A4:2", 70);
  lead.line(4 * 4, VA.join(" "), 90);
  lead.line(12 * 4, VB.join(" "), 98);
  lead.line(20 * 4, VA.join(" "), 92);
  lead.line(28 * 4, "F5:2 D5:2 C5:2 E5:2 D5:2 A4:2 D4:4", 78);
  writeMidi("03_Sector2_PuenteUrbano.mid", { bpm: 100, key: "Dm", bars: 32, tracks: [lead, rhodes, bass, pluck, dr] });
}

// =====================================================================================
// 04  VICTORIA (jingle) — La mayor, 116 bpm, 4 compases, sin loop
// =====================================================================================
function victoria() {
  const flute = new Track("Quena (flauta)", 0, 73), guitar = new Track("Charango (arpegio)", 2, 24), bass = new Track("Bajo", 3, 33), brass = new Track("Metales suaves", 4, 61), dr = drumTrack("Percusion");
  const prog = ["A", "D", "E", "A"];
  prog.forEach((c, b) => { arp(guitar, b, c, 72, [0, 1, 2, 3, 2, 1, 2, 3]); bassHuayno(bass, b, c, 90); drumsHuayno(dr, b, 90, true); });
  const chord = { A: ["A3+E4+A4+C#5"], D: ["D4+A4+D5+F#5"], E: ["E4+B4+E5+G#5"] };
  brass.line(0, "A3+E4+A4:3.5 r:.5 D4+A4+D5:3.5 r:.5 E4+B4+E5:3.5 r:.5 A3+E4+A4+C#5:4", 70);
  flute.line(0, "E5:.5 A5:.5 C#6:1 B5:.5 A5:.5 E5:1 F#5:.5 A5:.5 D6:1 C#6:.5 B5:.5 A5:1 B5:1 G#5:1 E6:2 C#6:1 E6:1 A5:2", 100);
  writeMidi("04_Victoria.mid", { bpm: 116, key: "A", bars: 4, tracks: [flute, guitar, bass, brass, dr] });
}

// =====================================================================================
// 05  DERROTA (jingle) — La menor, 66 bpm, 4 compases, sin loop
// =====================================================================================
function derrota() {
  const flute = new Track("Quena (flauta)", 0, 73), padT = new Track("Pad suave", 4, 89), bass = new Track("Bajo", 3, 33), guitar = new Track("Charango (arpegio)", 2, 24);
  const prog = ["Am", "F", "Dm", "E"];
  prog.forEach((c, b) => { pad(padT, b, c, 55); bassHalf(bass, b, c, 70); arp(guitar, b, c, 45, [0, 1, 2, 3, 0, 1, 2, 3]); });
  flute.line(0, "E5:2 D5:1 C5:1 C5:2 A4:2 A4:2 F4:2 E4:4", 80);
  writeMidi("05_Derrota.mid", { bpm: 66, key: "Am", bars: 4, tracks: [flute, padT, bass, guitar] });
}

menu(); sector1(); sector2(); victoria(); derrota();
