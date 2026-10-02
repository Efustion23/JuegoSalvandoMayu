# Cuidando al Mayu

Videojuego 2D de acción y gestión ambiental, hecho en **Unity 6** para la Universidad Continental.
Eres **Samy**, guardiana del río **Qhali**: ahuyentas o concientizas a quienes tiran basura, rescatas lo que cae al agua, reciclas por **Eco-Créditos** y mejoras tu equipo. Cada sector debe terminar con la **Pureza del río en 80 % o más**.

Está ligado al **ODS 6: Agua limpia y saneamiento**, en particular a las metas **6.3** (reducir la contaminación y aumentar el reciclaje) y **6.6** (proteger y restablecer los ecosistemas relacionados con el agua).

| Menú | Río con vida | Fin de sector |
|---|---|---|
| ![Menú](docs/img/menu.png) | ![Juego](docs/img/juego.png) | ![Estrellas](docs/img/estrellas.png) |

## Cómo se juega

| Acción | Control |
|---|---|
| Moverse | `W` `A` `S` `D` |
| Correr | `Shift` |
| Patada | `Espacio` (toque corto) |
| **Patada cargada** | Mantén `Espacio` y suelta: más alcance, y el infractor lanzado derriba a otros |
| **Concientizar** | Mantén `E` junto al infractor antes de que tire la bolsa |
| Usar la tienda / plantar queñuas | `E` (en el Almacén o en un punto de reforestación) |
| Pausa | `Esc` |
| Logros | `L` (menú, pausa y paneles de fin) |
| Silenciar | `N` |

### Un sector
1. Los **infractores** llegan a la orilla y esperan unos segundos para tirar su bolsa. Un círculo rojo y una barra avisan cuánto falta.
2. Puedes **concientizarlos** (75 % de éxito en el Sector 1, 60 % en el Sector 2). Si se niegan, solo sirve la **patada**.
3. Lo que cae al río **flota corriente abajo**: sácalo con la pinza antes de que se hunda.
4. Recoge basura pisándola y **recíclala** en las estaciones para ganar Eco-Créditos.
5. Una **lluvia** por sector arrastra al río la basura de la orilla, salvo la que protegen las queñuas.
6. Los **botaderos** acumulan basura y atraen más infractores; limpiarlos da pureza y créditos.

Si encadenas acciones sin que nadie contamine, subes la **racha** y ganas créditos extra.

### Mejoras (Almacén Municipal)
Botas de Sprint · Mochila Expandida · Silbato Sónico (flecha hacia el infractor más cercano) · **Nutria Mayu** (te sigue y trae la basura cercana; en el nivel 3 también rescata bolsas del río).

### Estrellas y logros
Cada sector da hasta **3 estrellas**: certificado, pureza de 90 % o más, y racha x5 con 8 recicladas. Hay **12 logros** (por ejemplo *Voz del río*, *Pleno* o *Cero bolsas*).

### El río reacciona
Cuanto más limpia está el agua, más vida vuelve: peces, patos, truchas que saltan, libélulas, ranas y garzas. Con la pureza baja el agua se enturbia y aparecen manchas de lodo.

## Sectores
1. **Sendero de las Laderas**: caserío andino, chacras, capilla y ribera abierta.
2. **Puente Urbano**: plaza adoquinada, mercado, mirador y mayor ritmo de infractores.

## Requisitos para abrirlo
- **Unity 6000.3.23f1** (con los módulos que necesites: *Windows Build Support*, y *Web Build Support* para WebGL).
- **Git LFS** instalado antes de clonar (las imágenes y el audio pesado van por LFS).

```bash
git lfs install
git clone https://github.com/Efustion23/JuegoSalvandoMayu.git
```
Abre la carpeta con Unity Hub y carga la escena `Assets/Scenes/SampleScene.unity`.

## Estructura
```
Assets/
  Scripts/      Lógica del juego (GameManager, Infractor, PlayerController, Pet, Achievements, RiverVisuals...)
  Editor/       Herramienta "Regenerar orillas del río" (RiverBankGenerator)
  Scenes/       SampleScene (única escena)
  Audio/        Efectos y música provisional (generados por código)
  Fonts/        Pixelify Sans y Press Start 2P
Musica/         MIDI y guía de estructura para componer la banda sonora; generar_sfx.js
Builds/         Compilaciones (no se versionan)
```

### Herramientas del proyecto
- **Herramientas > Regenerar orillas del río**: redibuja el agua y las orillas animadas a partir de las celdas del tilemap `Rio`. Si cambias la forma del río, vuelve a ejecutarla.
- `Musica/generar_sfx.js` (`node generar_sfx.js`): sintetiza los efectos de sonido en `Assets/Audio`.
- `Musica/generar_midi.js` (`node generar_midi.js`): genera los MIDI de la banda sonora.

## Compilar
Desde Unity: menú **Herramientas > Compilar para Web (WebGL)** o **Compilar para Windows**. Las compilaciones se guardan en `Builds/` (no se versionan).

Por línea de comandos, con Unity cerrado:
```bash
Unity.exe -batchmode -quit -nographics -projectPath <ruta> -buildTarget WebGL -executeMethod BuildTools.BuildWebBatch -logFile build.log
```
(`BuildTools.BuildWindowsBatch` para Windows.)

### Versión web
- Necesita el módulo **Web Build Support** de Unity (reinicia Unity después de instalarlo).
- Usa compresión Gzip con *decompression fallback*, así que funciona en cualquier alojamiento sin configurar cabeceras. Pesa unos 33 MB.
- Para probarla en local hay que servirla por HTTP (no abre con doble clic): por ejemplo `npx serve Builds/Web` y abrir `http://localhost:3000`.
- Para publicarla (por ejemplo en **itch.io**), sube un `.zip` con el contenido de `Builds/Web` marcándolo como juego HTML.
- El botón *Salir* se oculta en la versión web y el navegador necesita un clic dentro del juego para darle el foco al teclado.

## Créditos
- **Arte:** Kenney *Tiny Town* (CC0) · *Cute Fantasy Free* · *Farm RPG FREE 16x16 - Tiny Asset Pack* · *Free Bridges Top-Down Pixel Art Asset Pack* (licencia de CraftPix).
- **Llama:** *LPC Style Farm Animals* de Daniel Eddeland (CC-BY 3.0 / GPL 2.0+), vía OpenGameArt.
- **Fuentes:** *Pixelify Sans* y *Press Start 2P* (SIL Open Font License).
- **Música y efectos:** provisionales, generados por código dentro del proyecto.
- El resto (código, sprites dibujados por código, ajustes del mapa) es del equipo.
