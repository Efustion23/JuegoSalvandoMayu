using UnityEngine;

// Bolsa que cayo al rio: flota corriente abajo (hacia el este) y se hunde al rato. Si el guardian llega a la
// orilla o al puente a tiempo, la saca con la pinza: va a la mochila y el rio recupera parte de la pureza.
public class FloatingTrash : MonoBehaviour
{
    private const float Reach = 1.6f, Life = 14f, Drift = 0.7f, Recover = 4f;
    private static bool hinted;

    private SpriteRenderer sr;
    private float age;
    private bool warnedFull;

    public static void Spawn(Sprite sprite, Vector2 pos)
    {
        var g = new GameObject("BasuraFlotante");
        g.transform.position = pos;
        var s = g.AddComponent<SpriteRenderer>();
        s.sprite = sprite; s.sortingOrder = 1;   // capa Default: pasa por debajo de los puentes
        g.AddComponent<FloatingTrash>().sr = s;
        if (!hinted) { hinted = true; GameManager.I.Alert("¡Una bolsa flota río abajo! Acércate a la orilla para sacarla con la pinza", 4f); }
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Life) { Destroy(gameObject); return; }

        // corriente abajo; si la orilla corta el paso se desvia hacia donde siga habiendo agua
        Vector2 p = transform.position;
        float d = Drift * Time.deltaTime;
        Vector2 step = new Vector2(d, 0f);
        if (!RiverVisuals.IsWater(p + new Vector2(0.45f, 0f)))
            step = RiverVisuals.IsWater(p + new Vector2(0.45f, 0.5f)) ? new Vector2(d, d)
                 : RiverVisuals.IsWater(p + new Vector2(0.45f, -0.5f)) ? new Vector2(d, -d)
                 : Vector2.zero;
        transform.position = p + step;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 3f) * 12f);
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01((Life - age) / 2f));   // se hunde los ultimos 2 s

        TryRescue();
    }

    void TryRescue()
    {
        var gm = GameManager.I;
        if (!gm.Running || Vector2.Distance(gm.Player.transform.position, transform.position) > Reach) return;
        if (!gm.Player.GetComponent<Backpack>().TryAdd())
        {
            if (!warnedFull) { warnedFull = true; gm.Alert("Mochila llena: no puedes sacar la bolsa del río", 1.5f); }
            return;
        }
        gm.Purity.Add(Recover);
        gm.RegisterRescue();
        AudioManager.Play(Sfx.Rescue);
        gm.Player.Pickup();
        gm.Alert($"¡Bolsa rescatada del río! Pureza +{Recover:0}%", 2f);
        Destroy(gameObject);
    }
}
