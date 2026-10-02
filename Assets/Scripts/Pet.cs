using System.Collections.Generic;
using UnityEngine;

// Mayu, la nutria ayudante. Sigue al guardian, sale corriendo a buscar la basura que haya cerca (alcance segun su nivel),
// la agarra y se la trae: entra a la mochila como si el guardian la hubiera pisado. En el nivel 3 tambien saca
// del agua las bolsas que flotan. Como buena nutria, no le importa mojarse: cruza el rio.
public class Pet : MonoBehaviour
{
    private static Pet instance;
    private static Sprite bodySprite, bagSprite;

    private enum Mode { Follow, Fetch, Return }

    private int level = 1;
    private Mode mode;
    private Transform player, target;
    private Backpack backpack;
    private SpriteRenderer sr, carried;
    private float cooldownUntil, nextSearch, squeakAt;
    private Vector2 lastPos;

    float Radius => level >= 3 ? 7.5f : level == 2 ? 5.5f : 3.5f;
    float Cooldown => level >= 3 ? 1.6f : level == 2 ? 2.2f : 3f;

    // nivel 0 = sin mascota; la compra y las mejoras llaman aqui desde GameManager.ApplyUpgrades
    public static void Apply(int lvl)
    {
        if (lvl <= 0) { if (instance != null) Destroy(instance.gameObject); instance = null; return; }
        if (instance == null)
        {
            var g = new GameObject("Mayu");
            instance = g.AddComponent<Pet>();
            instance.Build();
        }
        instance.level = lvl;
    }

    public static void Teleport(Vector2 pos)
    {
        if (instance == null) return;
        instance.transform.position = pos + Vector2.left;
        instance.mode = Mode.Follow;
        instance.target = null;
        instance.carried.enabled = false;
    }

    void Build()
    {
        var gm = GameManager.I;
        player = gm.Player.transform;
        backpack = player.GetComponent<Backpack>();
        if (bodySprite == null)
        {
            var pal = new Dictionary<char, Color32>
            {
                ['k'] = new Color32(52, 33, 26, 255), ['b'] = new Color32(150, 98, 60, 255), ['B'] = new Color32(112, 72, 44, 255),
                ['c'] = new Color32(232, 204, 160, 255), ['e'] = new Color32(15, 12, 14, 255), ['n'] = new Color32(30, 20, 20, 255),
                ['g'] = new Color32(90, 160, 70, 255), ['w'] = new Color32(235, 235, 230, 255),
            };
            bodySprite = PixelSprite.Make(new[]
            {
                "...........kk..",
                "..........kbbk.",
                ".....kkkkkbbbbk",
                "..kkkbbbbbbebbn",
                "kkBbbbbbbbbbbbk",
                "kBBbbbbcccbbbk.",
                ".kBBbbbcccbbk..",
                "..kkBBbbbbkk...",
                "....kk.kk.kk...",
            }, pal, 0.1f);
            bagSprite = PixelSprite.Make(new[]
            {
                "..g..",
                ".ggg.",
                "ggggg",
                "ggggg",
                ".ggg.",
            }, pal, 0f);
        }
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = bodySprite; sr.sortingLayerName = "Player"; sr.spriteSortPoint = SpriteSortPoint.Pivot;
        var c = new GameObject("Bolsa");
        c.transform.SetParent(transform, false);
        c.transform.localPosition = new Vector3(0.15f, 0.72f, 0f);
        carried = c.AddComponent<SpriteRenderer>();
        carried.sprite = bagSprite; carried.sortingLayerName = "Player"; carried.sortingOrder = 2; carried.enabled = false;
        transform.position = (Vector2)player.position + Vector2.left;
        lastPos = transform.position;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || player == null || !gm.Running) return;

        switch (mode)
        {
            case Mode.Follow: UpdateFollow(); break;
            case Mode.Fetch: UpdateFetch(); break;
            case Mode.Return: UpdateReturn(); break;
        }

        // saltitos al andar y un chillido de vez en cuando
        Vector2 pos = transform.position;
        bool moving = (pos - lastPos).sqrMagnitude > 0.0004f;
        if ((pos - lastPos).x != 0f && Mathf.Abs((pos - lastPos).x) > 0.002f) sr.flipX = (pos - lastPos).x < 0f;
        lastPos = pos;
        sr.transform.localScale = new Vector3(1f, moving ? 1f + 0.08f * Mathf.Sin(Time.time * 20f) : 1f + 0.03f * Mathf.Sin(Time.time * 3f), 1f);
        carried.flipX = sr.flipX;
        carried.transform.localPosition = new Vector3(sr.flipX ? -0.15f : 0.15f, 0.72f, 0f);
    }

    void MoveToward(Vector2 goal, float speed) => transform.position = Vector2.MoveTowards(transform.position, goal, speed * Time.deltaTime);

    void UpdateFollow()
    {
        Vector2 p = player.position, me = transform.position;
        float d = Vector2.Distance(p, me);
        if (d > 14f) { transform.position = p + Vector2.left; return; }                    // se perdio: reaparece al lado
        Vector2 home = p + new Vector2(-0.9f, -0.1f);
        if (Vector2.Distance(me, home) > 0.35f) MoveToward(home, Mathf.Lerp(3f, 7f, Mathf.Clamp01((d - 1.5f) / 4f)));

        if (Time.time < nextSearch || Time.time < cooldownUntil) return;
        nextSearch = Time.time + 0.4f;
        if (backpack.Count >= backpack.Capacity) return;
        var t = FindTarget();
        if (t == null) return;
        target = t; mode = Mode.Fetch;
        if (Time.time > squeakAt) { squeakAt = Time.time + 6f; AudioManager.Play(Sfx.Pet); }
    }

    // la basura o bolsa flotante mas cercana a la mochila del guardian, dentro del alcance
    Transform FindTarget()
    {
        Transform best = null;
        float bestDist = Radius;
        foreach (var t in FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(t.transform.position, player.position);
            if (d < bestDist) { bestDist = d; best = t.transform; }
        }
        if (level >= 3)
            foreach (var f in FindObjectsByType<FloatingTrash>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(f.transform.position, player.position);
                if (d < bestDist) { bestDist = d; best = f.transform; }
            }
        return best;
    }

    void UpdateFetch()
    {
        if (target == null) { mode = Mode.Follow; return; }       // alguien se le adelanto
        Vector2 goal = target.position;
        MoveToward(goal, 6.5f);
        if (Vector2.Distance(transform.position, goal) > 0.3f) return;

        bool floating = target.GetComponent<FloatingTrash>() != null;
        if (floating)
        {
            var gm = GameManager.I;
            gm.Purity.Add(4f);
            gm.RegisterRescue();
            gm.Alert("¡Mayu rescató una bolsa del río! Pureza +4%", 2f);
            RiverVisuals.RippleAt(goal);
        }
        Destroy(target.gameObject);
        target = null;
        carried.enabled = true;
        mode = Mode.Return;
        AudioManager.Play(Floating(floating), 1.2f, 0.8f);
    }

    static Sfx Floating(bool floating) => floating ? Sfx.Rescue : Sfx.Pickup;

    void UpdateReturn()
    {
        Vector2 p = player.position;
        MoveToward(p + new Vector2(-0.3f, 0f), 7f);
        if (Vector2.Distance(transform.position, p) > 0.9f) return;
        if (!backpack.TryAdd()) { if (Time.time > nextSearch) { nextSearch = Time.time + 1f; GameManager.I.Alert("Mochila llena: Mayu espera con la bolsa", 1.2f); } return; }
        carried.enabled = false;
        mode = Mode.Follow;
        cooldownUntil = Time.time + Cooldown;
        AudioManager.Play(Sfx.Pickup, 1.4f, 0.8f);
        GameManager.I.Player.Pickup();
    }
}
