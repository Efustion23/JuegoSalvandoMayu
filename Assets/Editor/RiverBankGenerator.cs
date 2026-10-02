using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dibuja el rio a partir de las celdas pintadas en el tilemap "Rio" (cualquier tile cuenta como agua): agua de fondo
// con manchas profundas y, en el tilemap "RioOrilla", pasto, barranco y espuma animada que siguen cualquier forma.
// Una celda de tierra rodeada de agua se dibuja como islote redondo.
// Si repintas el agua, vuelve a ejecutar Herramientas > Regenerar orillas del río.
public static class RiverBankGenerator
{
    const string Folder = "Assets/Tiles/Rio";
    const int T = 16, Cell = T + 2, Frames = 4, PeriodX = 8, PeriodY = 6, SheetCols = 8;
    const float Fillet = 11f, IslandRadius = 8f;   // en pixeles

    static readonly Color32 Water = Hex(0x0095E9), DeepEdge = Hex(0x0084D2), Deep = Hex(0x006DA8),
        Grass = Hex(0x84C669), GrassDark = Hex(0x65A556), GrassLight = Hex(0x8BD87D), Overhang = Hex(0x3E8948),
        DirtLight = Hex(0x9C6754), Dirt = Hex(0x6D483B), DirtLine = Hex(0x5A3A33), DirtDark = Hex(0x3F2832),
        Foam = new Color32(255, 255, 255, 235), FoamSoft = new Color32(210, 240, 255, 160),
        Shade = new Color32(0, 40, 80, 80), Clear = new Color32(0, 0, 0, 0);

    [MenuItem("Herramientas/Regenerar orillas del río")]
    public static void Generate()
    {
        var grid = Object.FindAnyObjectByType<Grid>();
        var river = grid.transform.Find("Rio").GetComponent<Tilemap>();
        var bank = BankTilemap(grid, river);
        river.CompressBounds();
        var b = river.cellBounds;
        var water = new HashSet<Vector2Int>();
        foreach (var p in b.allPositionsWithin)
            if (river.HasTile(p)) water.Add((Vector2Int)p);
        // a los lados el rio sale de camara: fuera del tilemap se repite la columna del borde
        bool IsWater(int x, int y) => water.Contains(new Vector2Int(Mathf.Clamp(x, b.xMin, b.xMax - 1), y));

        var islands = new HashSet<Vector2Int>();
        foreach (var p in b.allPositionsWithin)
        {
            var c = (Vector2Int)p;
            if (water.Contains(c)) continue;
            bool surrounded = true;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && !IsWater(c.x + dx, c.y + dy)) surrounded = false;
            if (surrounded) islands.Add(c);
        }

        Directory.CreateDirectory(Folder);

        // ---- agua de fondo: manchas profundas con ruido periodico (las variantes se repiten cada PeriodX x PeriodY celdas)
        int nw = PeriodX * T, nh = PeriodY * T;
        var noise = PeriodicNoise(nw, nh, 11);
        var sorted = (float[])noise.Clone();
        System.Array.Sort(sorted);
        float deepAt = sorted[(int)(sorted.Length * 0.95f)], edgeAt = sorted[(int)(sorted.Length * 0.91f)];
        var waterSheet = new Sheet(PeriodX, PeriodY);
        for (int vy = 0; vy < PeriodY; vy++)
            for (int vx = 0; vx < PeriodX; vx++)
                for (int py = 0; py < T; py++)
                    for (int px = 0; px < T; px++)
                    {
                        float n = noise[(vy * T + py) * nw + vx * T + px];
                        waterSheet.Put(vx, vy, px, py, n > deepAt ? Deep : n > edgeAt ? DeepEdge : Water);
                    }
        var waterSprites = waterSheet.Save("RioAgua.png", i => "Agua_" + i);

        // ---- orilla: cada agua que toca tierra (y cada islote), con Frames cuadros de espuma
        var border = new List<Vector2Int>(islands);
        foreach (var c in water)
        {
            bool touches = false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (!IsWater(c.x + dx, c.y + dy)) touches = true;
            if (touches) border.Add(c);
        }
        border.Sort((p, q) => p.y != q.y ? p.y.CompareTo(q.y) : p.x.CompareTo(q.x));

        Color32 Pixel(Vector2Int c, int px, int py, int f)
        {
            float u = px + 0.5f, v = py + 0.5f;
            int gx = c.x * T + px, gy = c.y * T + py;
            float d = 99f, wN = 0f, wS = 0f, wAll = 0f;
            if (islands.Contains(c))
            {
                d = Vector2.Distance(new Vector2(u, v), new Vector2(T / 2f, T / 2f)) - IslandRadius;
                wN = Mathf.Clamp01((T / 2f - v) / 6f);   // la cara sur del islote muestra el barranco
                wS = Mathf.Clamp01((v - T / 2f) / 6f);
                wAll = 1f;
            }
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx == 0 && dy == 0) || IsWater(c.x + dx, c.y + dy)) continue;
                    float di;
                    if (islands.Contains(new Vector2Int(c.x + dx, c.y + dy)))
                        di = Vector2.Distance(new Vector2(u, v), new Vector2(dx * T + T / 2f, dy * T + T / 2f)) - IslandRadius;
                    else
                    {
                        float ex = Mathf.Max(0f, Mathf.Max(dx * T - u, u - (dx * T + T)));
                        float ey = Mathf.Max(0f, Mathf.Max(dy * T - v, v - (dy * T + T)));
                        di = Mathf.Sqrt(ex * ex + ey * ey);
                    }
                    d = Mathf.Min(d, di);
                    float w = Mathf.Exp(-Mathf.Max(di, 0f) / 3f);
                    wAll += w;
                    if (dy > 0) wN += w; else if (dy < 0) wS += w;
                }
            // esquinas del agua redondeadas: con tierra en dos lados contiguos se rellena un cuarto de circulo
            if (!islands.Contains(c))
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        if (IsWater(c.x + sx, c.y) || IsWater(c.x, c.y + sy)) continue;
                        float ox = (u - (sx > 0 ? T - Fillet : Fillet)) * sx, oy = (v - (sy > 0 ? T - Fillet : Fillet)) * sy;
                        if (ox > 0f && oy > 0f) d = Mathf.Min(d, Fillet - Mathf.Sqrt(ox * ox + oy * oy));
                    }
            wN /= wAll; wS /= wAll;
            float side = Mathf.Max(0f, 1f - wN - wS);
            d += (ValueNoise(gx, gy, 4f, 1) - 0.5f) * 2.2f;   // borde irregular, continuo entre celdas

            // la orilla norte muestra el barranco de tierra (vista 3/4); la sur solo el borde del pasto
            float grass = 2f, over = grass + 1f, dirt = over + 4f * wN + 2f * side, line = dirt + 1f;
            if (d < grass)
            {
                float h = Hash(gx, gy, 2);
                return h < 0.09f ? GrassDark : h < 0.13f ? GrassLight : Grass;
            }
            if (d < over) return Overhang;
            if (d < dirt) return d < over + 1f && wN > 0.5f ? DirtLight : Hash(gx, 7, 3) < 0.2f ? DirtLine : Dirt;
            if (d < line) return DirtDark;
            float e = d - line;
            // espuma que va y viene; la fase cambia a lo largo de la orilla, asi la ola recorre el borde
            float fw = 0.7f + 1.5f * (0.5f + 0.5f * Mathf.Sin((f / (float)Frames + ValueNoise(gx, gy, 24f, 4) * 2f) * Mathf.PI * 2f));
            if (e < fw * 0.55f) return Foam;
            if (e < fw) return FoamSoft;
            if (wN > 0.4f && e < 3f) return Shade;   // sombra del barranco sobre el agua
            if (e < fw + 2.2f && Hash(gx, gy, 10 + f) < 0.08f) return FoamSoft;   // burbujas sueltas
            return Clear;
        }

        int rows = Mathf.Max(1, (border.Count + SheetCols - 1) / SheetCols);
        var bankSheet = new Sheet(SheetCols * Frames, rows);
        for (int i = 0; i < border.Count; i++)
            for (int f = 0; f < Frames; f++)
                for (int py = 0; py < T; py++)
                    for (int px = 0; px < T; px++)
                        bankSheet.Put((i % SheetCols) * Frames + f, i / SheetCols, px, py, Pixel(border[i], px, py, f));
        var bankSprites = bankSheet.Save("RioOrilla.png", k => $"Orilla_{k / Frames}_{k % Frames}", SheetCols * Frames, border.Count * Frames);

        // ---- tiles (todos dentro de un solo asset para no llenar la carpeta)
        string assetPath = Folder + "/RioTiles.asset";
        AssetDatabase.DeleteAsset(assetPath);
        var waterTiles = new Tile[PeriodX * PeriodY];
        for (int i = 0; i < waterTiles.Length; i++)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.name = "Agua_" + i;
            t.sprite = waterSprites[i];
            t.colliderType = Tile.ColliderType.None;
            waterTiles[i] = t;
            if (i == 0) AssetDatabase.CreateAsset(t, assetPath); else AssetDatabase.AddObjectToAsset(t, assetPath);
        }
        var bankTiles = new AnimatedTile[border.Count];
        for (int i = 0; i < border.Count; i++)
        {
            var t = ScriptableObject.CreateInstance<AnimatedTile>();
            t.name = $"Orilla_{border[i].x}_{border[i].y}";
            t.m_AnimatedSprites = bankSprites.Skip(i * Frames).Take(Frames).ToArray();
            t.m_MinSpeed = t.m_MaxSpeed = 3f;   // todas iguales y desde el mismo instante: la espuma va sincronizada
            t.m_TileColliderType = Tile.ColliderType.None;
            bankTiles[i] = t;
            AssetDatabase.AddObjectToAsset(t, assetPath);
        }
        AssetDatabase.SaveAssets();

        foreach (var c in water)
            river.SetTile((Vector3Int)c, waterTiles[Mod(c.x, PeriodX) + Mod(c.y, PeriodY) * PeriodX]);
        bank.ClearAllTiles();
        for (int i = 0; i < border.Count; i++) bank.SetTile((Vector3Int)border[i], bankTiles[i]);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        Debug.Log($"Río regenerado: {water.Count} celdas de agua, {border.Count} de orilla, {islands.Count} islotes.");
    }

    static Tilemap BankTilemap(Grid grid, Tilemap river)
    {
        var t = grid.transform.Find("RioOrilla");
        if (t != null) return t.GetComponent<Tilemap>();
        var go = new GameObject("RioOrilla");
        Undo.RegisterCreatedObjectUndo(go, "Orilla del rio");
        go.transform.SetParent(grid.transform, false);
        go.transform.SetSiblingIndex(river.transform.GetSiblingIndex() + 1);
        var map = go.AddComponent<Tilemap>();
        map.tileAnchor = river.tileAnchor;
        var r = go.AddComponent<TilemapRenderer>();
        var rr = river.GetComponent<TilemapRenderer>();
        r.sharedMaterial = rr.sharedMaterial;
        r.sortingLayerID = rr.sortingLayerID;
        r.sortingOrder = 2;   // sobre lo que flota en el agua (1), bajo puentes, juncos y basura (3)
        return map;
    }

    // hoja de sprites de 16x16 con 1 pixel de margen repetido alrededor de cada uno (evita costuras entre tiles)
    class Sheet
    {
        readonly int cols, rows, w;
        readonly Color32[] px;

        public Sheet(int cols, int rows)
        {
            this.cols = cols; this.rows = rows; w = cols * Cell;
            px = new Color32[w * rows * Cell];
        }

        public void Put(int col, int row, int x, int y, Color32 c) => px[(row * Cell + 1 + y) * w + col * Cell + 1 + x] = c;

        // guarda el PNG, lo importa cortado en sprites (uno por casilla, en orden de lectura) y los devuelve
        public Sprite[] Save(string file, System.Func<int, string> name, int perRow = 0, int count = 0)
        {
            if (perRow == 0) perRow = cols;
            if (count == 0) count = cols * rows;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int x0 = c * Cell, y0 = r * Cell;
                    for (int i = 0; i < Cell; i++)
                    {
                        int k = Mathf.Clamp(i, 1, T);
                        px[y0 * w + x0 + i] = px[(y0 + 1) * w + x0 + k];
                        px[(y0 + Cell - 1) * w + x0 + i] = px[(y0 + T) * w + x0 + k];
                    }
                    for (int i = 0; i < Cell; i++)
                    {
                        px[(y0 + i) * w + x0] = px[(y0 + i) * w + x0 + 1];
                        px[(y0 + i) * w + x0 + Cell - 1] = px[(y0 + i) * w + x0 + T];
                    }
                }
            string path = Folder + "/" + file;
            AssetDatabase.DeleteAsset(path);   // meta nuevo: sin nombres de sprites viejos
            var tex = new Texture2D(w, rows * Cell, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.spritePixelsPerUnit = T;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Point;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            imp.SetTextureSettings(settings);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var data = factory.GetSpriteEditorDataProviderFromObject(imp);
            data.InitSpriteEditorDataProvider();
            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
                rects[i] = new SpriteRect
                {
                    name = name(i),
                    spriteID = GUID.Generate(),
                    rect = new Rect((i % perRow) * Cell + 1, (i / perRow) * Cell + 1, T, T),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            data.SetSpriteRects(rects);
            data.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            data.Apply();
            imp.SaveAndReimport();

            var byName = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            return rects.Select(r => byName[r.name]).ToArray();
        }
    }

    static float[] PeriodicNoise(int w, int h, int seed)
    {
        var rng = new System.Random(seed);
        var n = new float[w * h];
        foreach (var (step, amp) in new[] { (16, 0.65f), (8, 0.35f) })
        {
            int gw = w / step, gh = h / step;
            var lat = new float[gw * gh];
            for (int i = 0; i < lat.Length; i++) lat[i] = (float)rng.NextDouble();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int x0 = x / step, y0 = y / step, x1 = (x0 + 1) % gw, y1 = (y0 + 1) % gh;
                    float fx = Smooth(x % step / (float)step), fy = Smooth(y % step / (float)step);
                    n[y * w + x] += amp * Mathf.Lerp(Mathf.Lerp(lat[y0 * gw + x0], lat[y0 * gw + x1], fx),
                                                     Mathf.Lerp(lat[y1 * gw + x0], lat[y1 * gw + x1], fx), fy);
                }
        }
        return n;
    }

    static float ValueNoise(float x, float y, float scale, int seed)
    {
        x /= scale; y /= scale;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = Smooth(x - x0), fy = Smooth(y - y0);
        return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0, seed), Hash(x0 + 1, y0, seed), fx),
                          Mathf.Lerp(Hash(x0, y0 + 1, seed), Hash(x0 + 1, y0 + 1, seed), fx), fy);
    }

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);
    static int Mod(int a, int m) => (a % m + m) % m;
    static Color32 Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
}
