using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basura suelta que aparece en los senderos del sector para que siempre haya algo que recoger y reciclar.
public class LitterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private float interval = 8f;
    [SerializeField] private int maxLoose = 4;
    [SerializeField] private float[] pathY = { 6f, -9f };   // senderos norte y sur
    [SerializeField] private int onlySector = 0;             // -1 = todos los sectores

    readonly List<GameObject> spawned = new List<GameObject>();

    IEnumerator Start()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            yield return wait;
            var gm = GameManager.I;
            if (trashPrefab == null || gm.State != GamePhase.Playing) continue;
            if (onlySector >= 0 && gm.SectorIndex != onlySector) continue;
            spawned.RemoveAll(g => g == null);
            if (spawned.Count >= maxLoose) continue;
            var s = gm.Sector;
            for (int tries = 0; tries < 8; tries++)
            {
                var p = new Vector2(Random.Range(s.minX + 1f, s.maxX - 1f), pathY[Random.Range(0, pathY.Length)] + Random.Range(-0.6f, 0.6f));
                if (Blocked(p)) continue;
                spawned.Add(Instantiate(trashPrefab, p, Quaternion.identity));
                break;
            }
        }
    }

    // ponytail: solo mira colisionadores solidos (casas, cercas, agua); los triggers como MapBounds no cuentan
    static bool Blocked(Vector2 p)
    {
        foreach (var c in Physics2D.OverlapCircleAll(p, 0.35f))
            if (!c.isTrigger) return true;
        return false;
    }
}
