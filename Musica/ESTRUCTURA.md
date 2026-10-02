# Música de "Cuidando al Mayu" — guía para FL Studio

Hay 5 piezas en MIDI (carpeta `Musica/`). Cada archivo trae una pista por instrumento y el tempo ya puesto.
Arrástralos a FL Studio (o `File > Import > MIDI file`) y asigna un sonido a cada canal del Channel Rack.
Todo se generó con `generar_midi.js` (`node generar_midi.js` los vuelve a crear si cambias algo).

| # | Archivo | Uso en el juego | Tempo | Compás | Tonalidad / escala | Largo | ¿Loop? |
|---|---|---|---|---|---|---|---|
| 01 | `01_Menu.mid` | Menú principal y pausa | 84 BPM | 4/4 | **La menor** (pentatónica menor: La Do Re Mi Sol) | 16 compases · 46 s | Sí |
| 02 | `02_Sector1_Laderas.mid` | Sector 1 · Sendero de las Laderas | 92 BPM | 4/4 | **La menor** (pentatónica; el Mi mayor del compás 4 de la sección A da el aire andino/frigio) | 32 compases · 84 s | Sí |
| 03 | `03_Sector2_PuenteUrbano.mid` | Sector 2 · Puente Urbano | 100 BPM (swing ~55 %) | 4/4 | **Re menor dórico** (Re Fa Sol La Do) | 32 compases · 77 s | Sí |
| 04 | `04_Victoria.mid` | Pantalla de río limpio / sector certificado | 116 BPM | 4/4 | **La mayor** | 4 compases · 8 s | No |
| 05 | `05_Derrota.mid` | Pantalla de río contaminado | 66 BPM | 4/4 | **La menor** | 4 compases · 15 s | No |

Los compases se cuentan desde 1. Las notas están en octavas que suenan bien con los instrumentos sugeridos.

---

## 01 · Menú (84 BPM · La menor · 16 compases)
Tranquila, casi ambiental. Sin percusión fuerte: solo shaker suave y un bombo muy bajo cada 2 compases.

| Compases | Acordes (uno por compás) | Qué pasa |
|---|---|---|
| 1–8 | Am · G · F · G · Am · G · F · E | Arpegio + pad. La quena entra en el compás 3 con frases largas. |
| 9–16 | Am · G · F · G · F · G · E · Am | Quena un poco más activa. Termina en La (compás 16) para volver limpio al compás 1. |

Pistas: Quena (flauta) · Charango (arpegio) · Bajo · Pad suave · Shaker suave.

## 02 · Sector 1 · Laderas (92 BPM · La menor · 32 compases)
Ritmo de **huayno**: bombo en 1 y 3, bongó grave en 2, 2&, 4 y 4&, shaker en corcheas.

| Sección | Compases | Acordes | Dinámica |
|---|---|---|---|
| Intro | 1–4 | Am · Am · G · G | Pad; arpegio y bajo desde el compás 3; quena larga en el 3–4; percusión en el 4. |
| A | 5–12 | Am · G · F · E · Am · G · F · E | Tema principal de la quena. |
| B | 13–20 | F · G · Am · Am · F · G · E · E | Más intensa: arpegio y percusión más fuertes, quena una octava arriba, zampona doblando. |
| A' | 21–28 | Igual que A | Tema otra vez; el último compás cambia la frase para preparar el cierre. |
| Outro | 29–32 | F · G · E · Am | Baja la intensidad, sin shaker. Termina en Am para enlazar con el compás 1. |

Pistas: Quena · Zampona (opcional, una octava arriba solo en la sección B) · Charango · Bajo · Pad · Percusión huayno.

## 03 · Sector 2 · Puente Urbano (100 BPM · Re menor dórico · 32 compases)
Estilo **chillhop / boom-bap**. Aplica **swing ~55 %** al hi-hat y a los acordes (Channel settings > Swing). Los acordes están sincopados (golpe en el tiempo 1 y en el 3&).

| Sección | Compases | Acordes | Dinámica |
|---|---|---|---|
| Intro | 1–4 | Dm · Gm · Bb · A7 | Piano eléctrico; bajo desde el compás 3; batería desde el 4. |
| A | 5–12 | (Dm · Gm · Bb · A7) ×2 | Vibráfono con el tema A. Bombo, caja y hi-hat. |
| B | 13–20 | Bb · A7 · Dm · Dm · Bb · A7 · Gm · A7 | Más abierta: pizzicato de contrapunto y batería más fuerte. |
| A' | 21–28 | Igual que A | Vuelve el tema con un fill de batería en el último compás de cada sección. |
| Outro | 29–32 | Bb · Gm · A7 · Dm | Se aligera y termina en Dm para enlazar con el inicio. |

Pistas: Vibráfono · Piano eléctrico (Rhodes) · Bajo eléctrico · Pizzicato · Batería (canal 10: bombo 36, caja 38, clap 39, hi-hat 42, hi-hat abierto 46).

## 04 · Victoria (116 BPM · La mayor · 4 compases, sin loop)
Fanfarria corta: A · D · E · A. Quena arriba, charango en arpegio, metales suaves sostenidos y percusión huayno. Debe terminar con la última nota sonando (deja cola de reverb de ~1 s).

## 05 · Derrota (66 BPM · La menor · 4 compases, sin loop)
Am · F · Dm · E. Quena descendente (Mi → Re → Do → La → Fa → Mi), pad y arpegio lento. Cierra en el Mi del último compás: queda "abierta", sin resolver.

---

## Instrumentos sugeridos
- **Quena**: flauta de bambú / "Shakuhachi" o "Pan flute" del FLEX o de Sytrus; un poco de reverb y aire.
- **Charango**: guitarra de nylon o ukelele con ataque corto; si tienes un pluck brillante, mejor.
- **Zampona**: pan flute en octava alta, volumen bajo.
- **Rhodes / vibráfono / bajo**: los presets de FLEX "Electric Piano", "Vibraphone" y "Fingered Bass" sirven.
- **Percusión huayno**: bombo legüero (kick grave seco), bongó grave, shaker. Si no hay sample de bombo, un kick de 808 con poco sub.

## Notas para que quede bien en el juego
1. **Loop sin saltos**: las piezas 01, 02 y 03 terminan en el mismo acorde con el que empiezan. Exporta con el final recortado exactamente en el último compás (sin cola de reverb); si prefieres dejar la cola de reverb, mézclala sobre el inicio para que el empalme no se note.
2. **Formato**: WAV 44.1 kHz, 16 bit, estéreo (después lo comprimo en Unity). Nombres: `musica_menu.wav`, `musica_sector1.wav`, `musica_sector2.wav`, `jingle_victoria.wav`, `jingle_derrota.wav`.
3. **Volumen**: música de fondo alrededor de **−16 LUFS** (picos bajo −1 dBFS). Deja espacio para los efectos de sonido: evita mucha energía entre 2 y 4 kHz en la melodía.
4. **Compatibilidad con la lluvia**: durante la lluvia el juego baja la luz y mete ruido de agua. Si quieres, más adelante hacemos una variante de 02 y 03 con menos percusión para ese momento.
5. **En Unity**: hoy `AudioManager` reproduce una sola música. Cuando tengas los .wav, me los pasas y hago que cambie por pantalla (menú, cada sector, victoria, derrota) con fundido entre pistas.
