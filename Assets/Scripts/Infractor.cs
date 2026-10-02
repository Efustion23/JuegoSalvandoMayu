using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Infractor: camina por una ruta hasta la orilla, espera y tira la bolsa al rio.
// Una patada antes de tirarla lo hace girar, soltar la bolsa y huir por donde vino.
public class Infractor : MonoBehaviour
{
    private enum State { Approach, Dump, Leave, Stunned, Flee }

    public static readonly List<Infractor> Active = new List<Infractor>();
    public bool Kickable => state == State.Approach || state == State.Dump;

    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float dumpTime = 2.5f;
    [SerializeField] private float purityDamage = 6f;
    [SerializeField] private Sprite[] down, up, side;   // 6 frames de caminar por direccion; side mira a la derecha
    [SerializeField] private Sprite bagSprite;
    [SerializeField] private TrashItem trashPrefab;
    [SerializeField] private GameObject splashPrefab;
    [SerializeField] private GameObject alertIcon;

    private State state;
    private List<Vector2> route;
    private Vector2 water;
    private int idx;
    private float timer;
    private SpriteRenderer sr;
    private SpriteRenderer danger, timeBar;   // circulo rojo en el suelo y barra de tiempo mientras espera para tirar

    private static Sprite circleSprite, barSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        alertIcon.SetActive(false);
        if (circleSprite == null) circleSprite = Ellipse(20, 10);
        if (barSprite == null) barSprite = PixelSprite.Make(new[] { "wwwwwwwwwwwwwwww", "wwwwwwwwwwwwwwww" }, new Dictionary<char, Color32> { ['w'] = new Color32(255, 255, 255, 255) }, 0.5f);
        danger = Child("Peligro", circleSprite, new Vector3(0f, -0.6f, 0f), "Default", 3);
        timeBar = Child("TiempoParaTirar", barSprite, new Vector3(0f, 1.62f, 0f), sr.sortingLayerName, 6);
    }

    SpriteRenderer Child(string name, Sprite sprite, Vector3 localPos, string layer, int order)
    {
        var g = new GameObject(name);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = localPos;
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.sortingLayerName = layer; r.sortingOrder = order; r.enabled = false;
        return r;
    }

    static Sprite Ellipse(int w, int h)
    {
        var rows = new string[h];
        for (int y = 0; y < h; y++)
        {
            var row = new char[w];
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f), d = dx * dx + dy * dy;
                row[x] = d > 1f ? '.' : d > 0.6f ? 'r' : 'f';
            }
            rows[y] = new string(row);
        }
        return PixelSprite.Make(rows, new Dictionary<char, Color32> { ['r'] = new Color32(255, 255, 255, 255), ['f'] = new Color32(255, 255, 255, 90) }, 0.5f);
    }

    // cuanto menos le queda para tirar la bolsa, mas rapido late el aviso y mas se vacia la barra
    void Warn(float k)
    {
        float beat = Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(5f, 16f, k)));
        danger.enabled = timeBar.enabled = true;
        danger.color = new Color(1f, 0.15f, 0.1f, 0.3f + 0.5f * beat);
        danger.transform.localScale = Vector3.one * (1f + 0.25f * k);
        timeBar.transform.localScale = new Vector3(1f - k, 1f, 1f);
        timeBar.color = Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.2f, 0.15f), k);
        alertIcon.transform.localScale = Vector3.one * (1f + 0.3f * beat * k);
    }

    void HideWarning()
    {
        danger.enabled = timeBar.enabled = false;
        alertIcon.transform.localScale = Vector3.one;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    // route[0] = punto de entrada; el ultimo punto es la orilla; water = celda de agua donde cae la bolsa.
    // speedScale > 1 = corredor; dumpSeconds > 0 sustituye el tiempo de espera en la orilla.
    public void Init(List<Vector2> path, Vector2 waterPos, Color tint, float speedScale = 1f, float dumpSeconds = -1f)
    {
        walkSpeed *= speedScale;
        if (dumpSeconds > 0f) dumpTime = dumpSeconds;
        route = path; water = waterPos; sr.color = tint;
        transform.position = route[0]; idx = 1; state = State.Approach;
    }

    void Update()
    {
        switch (state)
        {
            case State.Approach:
                if (MoveTo(route[idx], walkSpeed) && ++idx >= route.Count) { state = State.Dump; timer = dumpTime; alertIcon.SetActive(true); }
                break;
            case State.Dump:
                sr.sprite = down[0];
                timer -= Time.deltaTime;
                Warn(1f - timer / dumpTime);
                if (timer <= 0f) { HideWarning(); alertIcon.SetActive(false); StartCoroutine(Throw()); idx = route.Count - 2; state = State.Leave; }
                break;
            case State.Leave:
            case State.Flee:
                if (MoveTo(route[idx], state == State.Flee ? fleeSpeed : walkSpeed) && --idx < 0) Destroy(gameObject);
                break;
        }
    }

    bool MoveTo(Vector2 target, float speed)
    {
        Vector2 pos = transform.position, delta = target - pos;
        if (delta.magnitude < 0.05f) return true;
        Animate(delta);
        transform.position = pos + delta.normalized * speed * Time.deltaTime;
        return false;
    }

    void Animate(Vector2 dir)
    {
        Sprite[] set = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? side : (dir.y > 0 ? up : down);
        sr.sprite = set[(int)(Time.time * 8f) % set.Length];
        if (set == side) sr.flipX = dir.x < 0;
    }

    // Devuelve true si la patada lo alcanzo (solo antes de tirar la bolsa).
    public bool TryKick(Vector2 from)
    {
        if (!Kickable) return false;
        HideWarning();
        state = State.Stunned;
        alertIcon.SetActive(true);
        GameManager.I.RegisterKick();
        Instantiate(trashPrefab, transform.position, Quaternion.identity).GetComponent<SpriteRenderer>().sprite = bagSprite;
        StartCoroutine(Stun(((Vector2)transform.position - from).normalized));
        return true;
    }

    IEnumerator Stun(Vector2 away)
    {
        idx = Mathf.Clamp(idx - 1, 0, route.Count - 1);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            transform.position += (Vector3)(away * 2f * Time.deltaTime);
            transform.Rotate(0f, 0f, 900f * Time.deltaTime);
            alertIcon.transform.rotation = Quaternion.identity;   // el "!" no gira con el
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        state = State.Flee;
    }

    IEnumerator Throw()
    {
        var bag = new GameObject("BolsaLanzada");
        var bsr = bag.AddComponent<SpriteRenderer>();
        bsr.sprite = bagSprite; bsr.sortingLayerName = sr.sortingLayerName; bsr.sortingOrder = 1;
        Vector2 start = transform.position;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float k = t / 0.5f;
            bag.transform.position = Vector2.Lerp(start, water, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
            yield return null;
        }
        Instantiate(splashPrefab, water, Quaternion.identity);
        AudioManager.Play(Sfx.Splash);
        RiverVisuals.RippleAt(water);
        ScreenShake.Shake(0.16f, 0.2f);
        GameManager.I.Purity.Remove(purityDamage);
        GameManager.I.RegisterPollution();
        Destroy(bag);
        FloatingTrash.Spawn(bagSprite, water);
    }
}
