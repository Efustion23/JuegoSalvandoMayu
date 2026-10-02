using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Genera infractores que entran por los extremos de los senderos y van a tirar basura en la orilla.
// Los puntos de descarga se calculan del mapa: celdas de orilla sin obstaculos entre el sendero y el agua.
// El ritmo lo fija el sector activo (Configure).
public class InfractorSpawner : MonoBehaviour
{
    private struct DumpPoint { public Vector2 pathPos, shore, water; public bool north; }

    [SerializeField] private Infractor prefab;
    [SerializeField] private Tilemap river, bridge, obstacles;
    [SerializeField] private float firstDelay = 6f;
    [SerializeField] private float northPathY = 6f, southPathY = -9f;
    [SerializeField] private float leftEntry = -18.6f, rightEntry = 44.6f;
    [SerializeField] private int minCellX = -17, maxCellX = 42;

    private readonly List<DumpPoint> points = new List<DumpPoint>();
    private SectorConfig cfg;
    private float sectorStart;
    private int alive;

    void Start()
    {
        for (int x = minCellX; x <= maxCellX; x++)
        {
            TryAdd(x, true);
            TryAdd(x, false);
        }
        StartCoroutine(Loop());
    }

    // Baja (norte) o sube (sur) desde el sendero hasta la primera celda con agua debajo/encima.
    void TryAdd(int x, bool north)
    {
        int step = north ? -1 : 1;
        int y = north ? Mathf.FloorToInt(northPathY) - 2 : Mathf.FloorToInt(southPathY) + 2;
        for (int i = 0; i < 14; i++, y += step)
        {
            var cell = new Vector3Int(x, y, 0);
            if (obstacles.HasTile(cell)) return;
            var next = new Vector3Int(x, y + step, 0);
            if (bridge.HasTile(next)) return;
            if (river.HasTile(next))
            {
                points.Add(new DumpPoint
                {
                    north = north,
                    pathPos = new Vector2(x + 0.5f, north ? northPathY : southPathY),
                    shore = new Vector2(x + 0.5f, y + 0.5f),
                    water = new Vector2(x + 0.5f, y + step + 0.5f)
                });
                return;
            }
        }
    }

    public void Configure(SectorConfig config)
    {
        cfg = config;
        sectorStart = Time.time;
    }

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstDelay);
        while (true)
        {
            float interval = 1f;
            if (cfg != null && GameManager.I.State == GamePhase.Playing)
            {
                if (alive < cfg.maxAlive) SpawnRandom();
                interval = Mathf.Lerp(cfg.startInterval, cfg.minInterval, (Time.time - sectorStart) / cfg.duration);
            }
            yield return new WaitForSeconds(Random.Range(0.8f, 1.2f) * interval);
        }
    }

    void SpawnRandom()
    {
        var pool = points.FindAll(p => p.pathPos.x >= cfg.minX && p.pathPos.x <= cfg.maxX);
        if (pool.Count == 0) return;
        // la mitad de las veces van a un botadero con basura: la suciedad atrae mas suciedad
        var hot = pool.FindAll(p => DumpSite.DirtyNear(p.shore, 4f));
        if (hot.Count > 0 && Random.value < 0.5f) pool = hot;
        bool runner = Random.value < cfg.runnerChance;
        Spawn(pool[Random.Range(0, pool.Count)], runner ? 1.7f : 1f, runner ? 1.6f : -1f, runner ? Color.HSVToRGB(0f, 0.55f, 1f) : Color.HSVToRGB(Random.value, 0.35f, 1f));
    }

    // Para el tutorial: infractor lento que espera mucho, en la orilla norte mas cercana a x.
    public void SpawnNear(float x)
    {
        DumpPoint best = points[0];
        foreach (var p in points)
            if (p.north && Mathf.Abs(p.pathPos.x - x) < Mathf.Abs(best.pathPos.x - x)) best = p;
        Spawn(best, 0.6f, 8f, Color.white);
    }

    void Spawn(DumpPoint p, float speedScale, float dumpSeconds, Color tint)
    {
        float entryX = p.pathPos.x < (leftEntry + rightEntry) / 2f ? leftEntry : rightEntry;
        var route = new List<Vector2> { new Vector2(entryX, p.pathPos.y), p.pathPos, p.shore };
        var inf = Instantiate(prefab);
        inf.Init(route, p.water, tint, speedScale, dumpSeconds);
        alive++;
        AudioManager.Play(Sfx.Alert);
        CameraAlert.Peek(inf.transform);
        StartCoroutine(Watch(inf));
        GameManager.I.Alert($"¡Infractor en la orilla {(p.north ? "norte" : "sur")} del {(p.pathPos.x < 14 ? "Sector 1" : "Sector 2")}!", 4f);
    }

    IEnumerator Watch(Infractor inf)
    {
        while (inf != null) yield return null;
        alive--;
    }
}
