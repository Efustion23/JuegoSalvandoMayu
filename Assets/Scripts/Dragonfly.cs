using UnityEngine;

// Libelula sobre la orilla: vuela a tirones cortos, se queda suspendida un momento y cada tanto cambia de zona.
// Son bioindicadoras: solo aparecen cuando el agua esta limpia (lo decide RiverVisuals).
public class Dragonfly : MonoBehaviour
{
    private Vector2[] spots;
    private Sprite[] wings;
    private SpriteRenderer sr;
    private Vector2 home, target;
    private float wait;

    public void Init(Vector2[] shoreSpots, Sprite[] wingFrames)
    {
        spots = shoreSpots; wings = wingFrames;
        sr = GetComponent<SpriteRenderer>();
        home = target = spots[Random.Range(0, spots.Length)] + Vector2.up * 0.4f;
        transform.position = home;
    }

    void Update()
    {
        sr.sprite = wings[(int)(Time.time * 18f) % wings.Length];   // aleteo
        Vector2 p = transform.position;
        if ((p - target).sqrMagnitude < 0.0025f)
        {
            if ((wait -= Time.deltaTime) > 0f) return;
            if (Random.value < 0.12f)
            {
                // otra zona de la orilla, pero cercana
                var s = spots[Random.Range(0, spots.Length)];
                if (Vector2.Distance(s, home) < 6f) home = s + Vector2.up * 0.4f;
            }
            target = home + Random.insideUnitCircle * 1.2f;
            wait = Random.Range(0.3f, 1.4f);
        }
        transform.position = Vector2.MoveTowards(p, target, 3.2f * Time.deltaTime);
        if (Mathf.Abs(target.x - p.x) > 0.01f) sr.flipX = target.x < p.x;
    }
}
