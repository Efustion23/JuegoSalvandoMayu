using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Puntos de reforestacion en la ribera: con E y Eco-Creditos se planta una queñua (arbol nativo andino).
// Al crecer da pureza lenta al sector que se esta jugando y sus raices frenan la escorrentia de la lluvia.
public class Reforestation : MonoBehaviour
{
    private class Spot { public Vector2 pos; public SpriteRenderer sr; public float plantedAt = -1f; }

    private static Reforestation instance;

    [SerializeField] private Tilemap river, bridge, obstacles, ground;
    [SerializeField] private int spotsPerSector = 5, cost = 25;
    [SerializeField] private float growSeconds = 10f, purityPerSecond = 0.04f, protectRadius = 3f, minSpacing = 6f;

    private readonly List<Spot> spots = new List<Spot>();
    private Sprite treeSprite;

    void Awake() => instance = this;

    void Start()
    {
        var mark = PixelSprite.Make(new[]
        {
            "...G...",
            "..GgG..",
            "...r...",
            ".rrrrr.",
            "rRRRRRr",
        }, Palette(), 0f);
        treeSprite = PixelSprite.Make(new[]
        {
            "....dddddd....",
            "..ddgggggddd..",
            ".dggGgggGgggd.",
            "dgggggGggggGgd",
            "dgGggggggGgggd",
            ".dggggGgggggd.",
            "..ddgggggGdd..",
            "....dd.rdd....",
            ".....rRr......",
            "......rRr.....",
            ".....rRr......",
            "....rRr.......",
            "....rRRr......",
            "...rRRRRr.....",
        }, Palette(), 0f);

        // orilla de pasto junto al agua, lejos de puentes, senderos y obstaculos
        var candidates = new List<Vector2>();
        ground.CompressBounds();
        foreach (var c in ground.cellBounds.allPositionsWithin)
        {
            if (!IsGrass(ground.GetTile(c)) || river.HasTile(c) || obstacles.HasTile(c)) continue;
            if (!river.HasTile(c + Vector3Int.down) && !river.HasTile(c + Vector3Int.up)) continue;
            if (NearBridge(c)) continue;
            candidates.Add(new Vector2(c.x + 0.5f, c.y + 0.2f));
        }
        var rng = new System.Random(7);
        for (int i = candidates.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (candidates[i], candidates[j]) = (candidates[j], candidates[i]); }

        foreach (var sector in GameManager.I.Sectors)
        {
            int placed = 0;
            foreach (var p in candidates)
            {
                if (placed >= spotsPerSector) break;
                if (p.x < sector.minX || p.x >= sector.maxX || spots.Exists(s => Vector2.Distance(s.pos, p) < minSpacing)) continue;
                var g = new GameObject("PuntoReforestacion");
                g.transform.SetParent(transform, false);
                g.transform.position = p;
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = mark; sr.sortingOrder = 1;
                spots.Add(new Spot { pos = p, sr = sr });
                placed++;
            }
        }
    }

    static Dictionary<char, Color32> Palette() => new Dictionary<char, Color32>
    {
        ['d'] = new Color32(30, 70, 40, 255), ['g'] = new Color32(70, 130, 60, 255), ['G'] = new Color32(110, 170, 80, 255),
        ['r'] = new Color32(170, 80, 50, 255), ['R'] = new Color32(120, 55, 35, 255),
    };

    // tiles de pasto del pack Kenney (0, 1, 2 y 43); el resto del suelo son senderos de tierra
    static bool IsGrass(TileBase t)
    {
        if (t == null) return false;
        string n = t.name;
        return n.EndsWith("_0") || n.EndsWith("_1") || n.EndsWith("_2") || n.EndsWith("_43");
    }

    bool NearBridge(Vector3Int c)
    {
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                if (bridge.HasTile(c + new Vector3Int(dx, dy, 0))) return true;
        return false;
    }

    // la basura cerca de una queñua ya plantada no la arrastra la lluvia
    public static bool Protects(Vector2 pos)
    {
        if (instance == null) return false;
        foreach (var s in instance.spots)
            if (s.plantedAt >= 0f && Vector2.Distance(s.pos, pos) < instance.protectRadius) return true;
        return false;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (!gm.Running) return;
        Vector2 player = gm.Player.transform.position;
        foreach (var s in spots)
        {
            if (s.plantedAt >= 0f) { Grow(s, gm); continue; }
            if (Vector2.Distance(player, s.pos) > 1f) continue;
            gm.Alert($"E: plantar una queñua ({cost} Eco-Créditos)", 0.2f);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && !Shop.AnyOpen) Plant(s, gm);
        }
    }

    void Plant(Spot s, GameManager gm)
    {
        if (!gm.TrySpend(cost)) { AudioManager.Play(Sfx.Deny); gm.Alert("Te faltan Eco-Créditos para plantar", 1.5f); return; }
        s.plantedAt = Time.time;
        s.sr.sprite = treeSprite;
        s.sr.sortingLayerName = "Player"; s.sr.sortingOrder = 0; s.sr.spriteSortPoint = SpriteSortPoint.Pivot;
        AudioManager.Play(Sfx.Buy);
        gm.RegisterPlant();
        gm.Alert("¡Queñua plantada! Sus raíces protegen la ribera", 2.5f);
    }

    void Grow(Spot s, GameManager gm)
    {
        float k = Mathf.Clamp01((Time.time - s.plantedAt) / growSeconds);
        s.sr.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
        if (k >= 1f && s.pos.x >= gm.Sector.minX && s.pos.x < gm.Sector.maxX) gm.Purity.Add(purityPerSecond * Time.deltaTime);
    }
}
