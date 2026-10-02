using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// El rio reacciona a la Pureza: de agua turbia y marron a cristalina, con fauna que regresa por etapas (peces,
// patos, truchas que saltan, libelulas, ranas y garzas), el lodo que se disipa y destellos y ondas que se mueven
// (mas destellos cuanto mas limpia). Ademas lo mantiene vivo siempre: lineas de corriente que bajan hacia el este,
// hojas que flotan rio abajo, nenufares y espuma alrededor de las rocas.
public class RiverVisuals : MonoBehaviour
{
    private static RiverVisuals instance;
    public static bool Raining;   // la lluvia multiplica las ondas

    [SerializeField] private Tilemap river, bridge;
    [SerializeField] private Fish fishPrefab;
    [SerializeField] private SpriteRenderer[] sludge;
    [SerializeField] private Sprite sparkleSprite, rippleSprite;
    [SerializeField] private Color dirtyTint = new Color(0.62f, 0.55f, 0.38f, 1f);
    [SerializeField] private int maxFish = 14, maxSparkles = 44, maxRipples = 10;
    [SerializeField] private int maxDucks = 4, maxFrogs = 5, maxHerons = 2, maxJumpers = 3, maxDragonflies = 5;
    // pureza desde la que aparece cada especie: la fauna vuelve por etapas a medida que el agua mejora
    [SerializeField] private float fishFrom = 50f, ducksFrom = 60f, jumpersFrom = 55f, dragonfliesFrom = 65f, frogsFrom = 70f, heronsFrom = 80f;
    [SerializeField] private int lilyPads = 14, leafCount = 9, currentCount = 40;
    [SerializeField] private float currentSpeed = 0.8f;

    private readonly List<(List<GameObject> group, float from)> fauna = new List<(List<GameObject>, float)>();
    private SpriteRenderer[] sparkles, ripples, leaves, currents;
    private float[] sparklePhase, sparkleSpeed, rippleAge, leafSpeed, currentAge, currentLife;
    private Vector2[] leafPos, currentPos;
    private Vector2[] waters;
    private float nextRipple;
    private readonly List<(SpriteRenderer ring, Sprite a, Sprite b, float phase)> rockFoam = new List<(SpriteRenderer, Sprite, Sprite, float)>();

    void Awake()
    {
        instance = this;
        Raining = false;
    }

    void Start()
    {
        var deep = new List<Vector2>();
        var shoreWater = new List<Vector2>();   // agua pegada a la orilla (garzas, libelulas)
        var frogSpots = new List<Vector2>();    // nenufares y pasto al borde del agua (ranas)
        var calm = new List<Vector2>();         // remansos a una celda de la orilla (nenufares)
        river.CompressBounds();
        foreach (var p in river.cellBounds.allPositionsWithin)
        {
            if (!river.HasTile(p) || bridge.HasTile(p) || NearBridge(p)) continue;
            bool inner = true;
            for (int dx = -1; dx <= 1 && inner; dx++)
                for (int dy = -1; dy <= 1 && inner; dy++)
                    if (!river.HasTile(p + new Vector3Int(dx, dy, 0))) inner = false;
            Vector2 center = new Vector2(p.x + 0.5f, p.y + 0.5f);
            if (inner)
            {
                deep.Add(center);
                if (!river.HasTile(p + new Vector3Int(0, 2, 0)) || !river.HasTile(p + new Vector3Int(0, -2, 0))) calm.Add(center);
            }
            if (!river.HasTile(p + Vector3Int.up)) { shoreWater.Add(center + new Vector2(0f, 0.25f)); frogSpots.Add(center + new Vector2(0f, 1f)); }
            else if (!river.HasTile(p + Vector3Int.down)) { shoreWater.Add(center - new Vector2(0f, 0.3f)); frogSpots.Add(center - new Vector2(0f, 0.9f)); }
        }
        waters = deep.ToArray();

        // nenufares en los remansos (algunos con flor); las ranas se suben a ellos
        var rng = new System.Random(5);
        var pads = new[] { LilyPad(false), LilyPad(true) };
        for (int i = 0; i < lilyPads && calm.Count > 0; i++)
        {
            int k = rng.Next(calm.Count);
            var pos = calm[k] + new Vector2((float)rng.NextDouble() * 0.5f - 0.25f, (float)rng.NextDouble() * 0.3f - 0.15f);
            calm.RemoveAll(c => Vector2.Distance(c, pos) < 1.6f);
            var pad = Animal("Nenufar", pads[rng.Next(3) == 0 ? 1 : 0], pos);
            pad.GetComponent<SpriteRenderer>().flipX = rng.Next(2) == 0;
            frogSpots.Add(pos + new Vector2(0f, -0.05f));
        }

        var fish = new List<GameObject>();
        for (int i = 0; i < maxFish; i++)
        {
            var f = Instantiate(fishPrefab, transform);
            f.Init(waters);
            fish.Add(f.gameObject);
        }
        var duckSprite = PixelSprite.Make(new[]
        {
            "......kkk...",
            ".....kGGGk..",
            ".....kGeGyy.",
            "......kGGk..",
            ".kkkkkwwwk..",
            "kbbbbbbbbbk.",
            "kbBBBBBBbbk.",
            ".kkkkkkkkk..",
        }, Palette(), 0.5f);
        var ducks = new List<GameObject>();
        for (int i = 0; i < maxDucks; i++)
        {
            var d = Animal("Pato", duckSprite, Vector2.zero);
            d.AddComponent<Fish>().Init(waters, false);
            ducks.Add(d);
        }
        var frogSprite = PixelSprite.Make(new[]
        {
            ".kk.kk.",
            "kgekegk",
            "kgggggk",
            "kgGGGgk",
            "kk...kk",
        }, Palette(), 0f);
        var heronSprite = PixelSprite.Make(new[]
        {
            "....kkk...",
            "...kwwwk..",
            "...kwewkyy",
            "....kwwk..",
            ".....kwk..",
            ".....kwk..",
            "....kwwk..",
            "...kwwwwk.",
            "..kwwwwwk.",
            "..kwwWwwk.",
            "...kwWwk..",
            "....kkk...",
            ".....l....",
            ".....l....",
            ".....l....",
            "....ll....",
        }, Palette(), 0f);
        fauna.Add((fish, fishFrom));
        fauna.Add((ducks, ducksFrom));
        fauna.Add((Jumpers(), jumpersFrom));
        fauna.Add((Dragonflies(shoreWater.ToArray()), dragonfliesFrom));
        fauna.Add((Critters("Rana", frogSprite, frogSpots, maxFrogs, true), frogsFrom));
        fauna.Add((Critters("Garza", heronSprite, shoreWater, maxHerons, false), heronsFrom));
        foreach (var (group, _) in fauna) foreach (var g in group) g.SetActive(false);

        sparkles = MakePool(maxSparkles, sparkleSprite, "Destello");
        ripples = MakePool(maxRipples, rippleSprite, "Onda");
        sparklePhase = new float[maxSparkles]; sparkleSpeed = new float[maxSparkles]; rippleAge = new float[maxRipples];
        for (int i = 0; i < maxSparkles; i++) { sparklePhase[i] = Random.Range(0f, 6.28f); sparkleSpeed[i] = Random.Range(1.2f, 2.6f); Reposition(sparkles[i]); }
        for (int i = 0; i < maxRipples; i++) rippleAge[i] = -1f;

        // lineas de corriente: trazos claros que bajan con el agua hacia el este
        var lines = new[] { Line(3), Line(4), Line(6) };
        currents = MakePool(currentCount, lines[0], "Corriente");
        currentAge = new float[currentCount]; currentLife = new float[currentCount]; currentPos = new Vector2[currentCount];
        for (int i = 0; i < currentCount; i++)
        {
            currents[i].sprite = lines[i % lines.Length];
            SpawnCurrent(i);
            currentAge[i] = Random.Range(0f, currentLife[i]);
        }

        // hojas que caen de los arboles y flotan rio abajo
        var leafSprites = new[] { Leaf('g'), Leaf('y'), Leaf('o') };
        leaves = MakePool(leafCount, leafSprites[0], "Hoja");
        leafSpeed = new float[leafCount]; leafPos = new Vector2[leafCount];
        for (int i = 0; i < leafCount; i++) { leaves[i].sprite = leafSprites[i % leafSprites.Length]; SpawnLeaf(i); }

        // espuma alrededor de las rocas del rio
        foreach (var rock in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (rock.name != "RocaRio" || !IsWater(rock.bounds.center)) continue;
            int w = Mathf.RoundToInt(rock.bounds.size.x * 16f) + 4, h = Mathf.Max(5, w / 3);
            var ring = Animal("Espuma", null, (Vector2)rock.bounds.center + Vector2.down * rock.bounds.extents.y * 0.55f).GetComponent<SpriteRenderer>();
            rockFoam.Add((ring, Ring(w, h), Ring(w + 2, h + 1), Random.Range(0f, 1f)));
        }
    }

    static Dictionary<char, Color32> Palette() => new Dictionary<char, Color32>
    {
        ['k'] = new Color32(30, 32, 42, 255), ['e'] = new Color32(15, 15, 20, 255), ['y'] = new Color32(250, 190, 40, 255),
        ['G'] = new Color32(40, 120, 70, 255), ['w'] = new Color32(245, 245, 240, 255), ['W'] = new Color32(200, 205, 215, 255),
        ['b'] = new Color32(150, 110, 70, 255), ['B'] = new Color32(110, 80, 50, 255), ['l'] = new Color32(60, 50, 40, 255),
        ['g'] = new Color32(90, 170, 70, 255), ['d'] = new Color32(38, 92, 66, 255), ['L'] = new Color32(132, 198, 105, 255),
        ['p'] = new Color32(240, 150, 190, 255), ['P'] = new Color32(255, 214, 230, 255), ['o'] = new Color32(230, 130, 50, 255),
        ['s'] = new Color32(200, 215, 225, 255), ['r'] = new Color32(215, 95, 90, 255), ['t'] = new Color32(70, 200, 210, 255),
        ['v'] = new Color32(210, 240, 255, 170),
    };

    static Sprite LilyPad(bool flower) => PixelSprite.Make(flower ? new[]
    {
        "....pPp...",
        "..ddpypdd.",
        ".dLLgpgLLd",
        "dLggggg..d",
        "dgggggg...",
        ".dggggggd.",
        "..dddddd..",
    } : new[]
    {
        "..dddddd..",
        ".dLLggLLd.",
        "dLggggg..d",
        "dgggggg...",
        ".dggggggd.",
        "..dddddd..",
    }, Palette(), 0.5f);

    static Sprite Leaf(char c) => PixelSprite.Make(new[]
    {
        "..." + c + c,
        "." + c + c + c + ".",
        "l" + c + "...",
    }, Palette(), 0.5f);

    static Sprite Line(int n)
    {
        var row = new string('v', n);
        return PixelSprite.Make(new[] { row }, Palette(), 0.5f);
    }

    // anillo de espuma (elipse hueca) de w x h pixeles
    static Sprite Ring(int w, int h)
    {
        var rows = new string[h];
        float rx = w / 2f, ry = h / 2f;
        for (int y = 0; y < h; y++)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - rx) / rx, dy = (y + 0.5f - ry) / ry, r = dx * dx + dy * dy;
                sb.Append(r <= 1f && r > 0.55f ? 'v' : '.');
            }
            rows[y] = sb.ToString();
        }
        return PixelSprite.Make(rows, Palette(), 0.5f);
    }

    List<GameObject> Jumpers()
    {
        var up = PixelSprite.Make(new[] { "....kk.", "..kssk.", ".kssk..", "kosk...", "kk....." }, Palette(), 0.5f);
        var down = PixelSprite.Make(new[] { "kk.....", "kosk...", ".kssk..", "..kssk.", "....kk." }, Palette(), 0.5f);
        var list = new List<GameObject>();
        for (int i = 0; i < maxJumpers; i++)
        {
            var g = Animal("Trucha", up, Vector2.zero);
            g.GetComponent<SpriteRenderer>().sortingOrder = 3;
            g.AddComponent<FishJump>().Init(waters, up, down);
            list.Add(g);
        }
        return list;
    }

    List<GameObject> Dragonflies(Vector2[] spots)
    {
        var wingsUp = PixelSprite.Make(new[] { ".vv.v", "ttttr", ".vv.v" }, Palette(), 0.5f);
        var wingsDown = PixelSprite.Make(new[] { ".....", "ttttr", "vv.vv" }, Palette(), 0.5f);
        var list = new List<GameObject>();
        for (int i = 0; i < maxDragonflies && spots.Length > 0; i++)
        {
            var g = Animal("Libelula", wingsUp, Vector2.zero);
            var sr = g.GetComponent<SpriteRenderer>();
            sr.sortingLayerName = "Decoration"; sr.sortingOrder = 5;
            g.AddComponent<Dragonfly>().Init(spots, new[] { wingsUp, wingsDown });
            list.Add(g);
        }
        return list;
    }

    bool NearBridge(Vector3Int c)
    {
        for (int dx = -2; dx <= 2; dx++)
            if (bridge.HasTile(c + new Vector3Int(dx, 0, 0))) return true;
        return false;
    }

    GameObject Animal(string name, Sprite sprite, Vector2 pos)
    {
        var g = new GameObject(name);
        g.transform.SetParent(transform, false);
        g.transform.position = pos;
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 1;
        return g;
    }

    List<GameObject> Critters(string name, Sprite sprite, List<Vector2> spots, int count, bool hops)
    {
        var list = new List<GameObject>();
        for (int i = 0; i < count && spots.Count > 0; i++)
        {
            int k = Random.Range(0, spots.Count);
            var g = Animal(name, sprite, spots[k] + new Vector2(Random.Range(-0.2f, 0.2f), 0f));
            g.GetComponent<SpriteRenderer>().sortingOrder = 3;   // sobre la orilla
            g.AddComponent<Critter>().Init(hops);
            spots.RemoveAt(k);
            list.Add(g);
        }
        return list;
    }

    SpriteRenderer[] MakePool(int n, Sprite sprite, string name)
    {
        var pool = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject(name); g.transform.SetParent(transform, false);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 1; sr.color = Color.clear;
            pool[i] = sr;
        }
        return pool;
    }

    void Reposition(SpriteRenderer s) => s.transform.position = waters[Random.Range(0, waters.Length)] + Random.insideUnitCircle * 0.4f;

    void SpawnCurrent(int i)
    {
        currentPos[i] = waters[Random.Range(0, waters.Length)] + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.45f, 0.45f));
        currentAge[i] = 0f;
        currentLife[i] = Random.Range(1.6f, 3.2f);
    }

    void SpawnLeaf(int i)
    {
        leafPos[i] = waters[Random.Range(0, waters.Length)];
        leafSpeed[i] = Random.Range(0.3f, 0.55f);
        leaves[i].color = new Color(1f, 1f, 1f, 0f);
    }

    // posicion alineada a la grilla de pixeles (16 por unidad) para que los trazos no tiemblen
    static Vector3 Snap(Vector2 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);

    public static bool IsWater(Vector2 pos) => instance != null && instance.river.HasTile(instance.river.WorldToCell(pos));

    // celda de agua (sin puente) mas cercana dentro del radio
    public static bool NearestWater(Vector2 from, float radius, out Vector2 water)
    {
        water = default;
        if (instance == null) return false;
        var c = instance.river.WorldToCell(from);
        int r = Mathf.CeilToInt(radius);
        float best = radius;
        bool found = false;
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                var cell = c + new Vector3Int(dx, dy, 0);
                if (!instance.river.HasTile(cell) || instance.bridge.HasTile(cell)) continue;
                Vector2 p = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                float d = Vector2.Distance(from, p);
                if (d < best) { best = d; water = p; found = true; }
            }
        return found;
    }

    // ondas en el agua (las pide, por ejemplo, la basura al caer)
    public static void RippleAt(Vector2 pos)
    {
        if (instance == null) return;
        for (int i = 0; i < instance.ripples.Length; i++)
            if (instance.rippleAge[i] < 0f) { instance.rippleAge[i] = 0f; instance.ripples[i].transform.position = pos; return; }
    }

    void Update()
    {
        float p = GameManager.I.Purity.Purity;
        // 0% turbio -> 100% cristalino; tono suave para no saltar de golpe
        river.color = Color.Lerp(river.color, Color.Lerp(dirtyTint, Color.white, p / 100f), Time.unscaledDeltaTime * 3f);

        foreach (var (group, from) in fauna)
        {
            int wanted = Mathf.Clamp(Mathf.FloorToInt((p - from) / (100f - from) * group.Count), 0, group.Count);
            for (int i = 0; i < group.Count; i++)
                if (group[i].activeSelf != (i < wanted)) group[i].SetActive(i < wanted);
        }

        float mud = Mathf.Clamp01(1f - (p - 40f) / 50f);
        foreach (var s in sludge) s.color = new Color(1f, 1f, 1f, mud);

        // destellos: parpadean y cambian de sitio; el agua limpia brilla mas
        int active = Mathf.RoundToInt(Mathf.Lerp(4f, maxSparkles, p / 100f));
        for (int i = 0; i < sparkles.Length; i++)
        {
            sparklePhase[i] += Time.deltaTime * sparkleSpeed[i];
            if (sparklePhase[i] > 6.28f) { sparklePhase[i] -= 6.28f; Reposition(sparkles[i]); }
            float a = i < active ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(sparklePhase[i])), 4f) : 0f;
            sparkles[i].color = new Color(1f, 1f, 1f, a * 0.9f);
        }

        // corriente: trazos que aparecen, bajan hacia el este y se apagan; se tinen con el color del agua
        for (int i = 0; i < currents.Length; i++)
        {
            currentAge[i] += Time.deltaTime;
            currentPos[i].x += currentSpeed * (0.75f + 0.25f * (i % 3)) * Time.deltaTime;
            if (currentAge[i] >= currentLife[i] || !IsWater(currentPos[i] + Vector2.right * 0.3f)) SpawnCurrent(i);
            float env = Mathf.Sin(currentAge[i] / currentLife[i] * Mathf.PI);
            var c = river.color; c.a = env * 0.8f;
            currents[i].color = c;
            currents[i].transform.position = Snap(currentPos[i]);
        }

        // hojas: flotan rio abajo con un leve vaivén y pasan bajo los puentes; al llegar a la orilla reaparecen
        for (int i = 0; i < leaves.Length; i++)
        {
            leafPos[i].x += leafSpeed[i] * Time.deltaTime;
            if (!IsWater(leafPos[i] + Vector2.right * 0.25f)) { SpawnLeaf(i); continue; }
            var c = leaves[i].color; c.a = Mathf.Min(1f, c.a + Time.deltaTime); leaves[i].color = c;
            leaves[i].transform.position = Snap(leafPos[i] + Vector2.up * (Mathf.Sin(Time.time * 1.3f + i * 2f) * 0.08f));
        }

        foreach (var (ring, a, b, phase) in rockFoam)
            ring.sprite = (int)((Time.time + phase) / 0.45f) % 2 == 0 ? a : b;

        // ondas ambientales (muchas mas con lluvia)
        if (Time.time > nextRipple)
        {
            RippleAt(waters[Random.Range(0, waters.Length)]);
            nextRipple = Time.time + (Raining ? Random.Range(0.08f, 0.2f) : Random.Range(0.8f, 2f));
        }
        for (int i = 0; i < ripples.Length; i++)
        {
            if (rippleAge[i] < 0f) { ripples[i].color = Color.clear; continue; }
            rippleAge[i] += Time.deltaTime;
            float k = rippleAge[i] / 1.6f;
            if (k >= 1f) { rippleAge[i] = -1f; continue; }
            ripples[i].transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, k);
            ripples[i].color = new Color(1f, 1f, 1f, (1f - k) * 0.55f);
        }
    }
}
