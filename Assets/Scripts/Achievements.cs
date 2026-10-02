using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Logros y estrellas. Los logros se guardan en el SaveData; el aviso y la lista (tecla L en menu y pausa) se construyen
// aqui mismo, asi que basta con tener este componente en el canvas del HUD y asignarle la fuente.
public class Achievements : MonoBehaviour
{
    private struct Def { public string id, name, desc; }

    private static readonly Def[] list =
    {
        new Def { id = "primera_patada", name = "Primera patada",      desc = "Ahuyenta a tu primer infractor" },
        new Def { id = "orador",         name = "Voz del río",         desc = "Concientiza a 5 infractores" },
        new Def { id = "patadon",        name = "Patadón",             desc = "Conecta una patada con la carga al máximo" },
        new Def { id = "pleno",          name = "Pleno",               desc = "Derriba a 3 infractores con una sola patada" },
        new Def { id = "racha5",         name = "En racha",            desc = "Llega a una racha de x5" },
        new Def { id = "rescatista",     name = "Rescatista",          desc = "Saca 5 bolsas del río en un sector" },
        new Def { id = "quenas",         name = "Guardián de queñuas", desc = "Planta 3 queñuas en un sector" },
        new Def { id = "botadero",       name = "Zona recuperada",     desc = "Limpia por completo un botadero" },
        new Def { id = "amigo",          name = "Amigo del río",       desc = "Adopta a la nutria Mayu" },
        new Def { id = "cero_bolsas",    name = "Cero bolsas",         desc = "Certifica un sector sin que llegue ninguna bolsa al río" },
        new Def { id = "constelacion",   name = "Constelación",        desc = "Consigue las 3 estrellas en un sector" },
        new Def { id = "rio_limpio",     name = "Río limpio",          desc = "Certifica los dos sectores" },
    };

    private static Achievements instance;
    private static Sprite starOn, starOff;

    [SerializeField] private Font font;

    private readonly Queue<string> pending = new Queue<string>();
    private RectTransform toast;
    private Text toastTitle, toastName;
    private GameObject panel;
    private Text panelText;
    private float toastEnd;

    // ---------- API ----------
    public static bool Has(string id) => ("," + Progress.Data.unlocked + ",").Contains("," + id + ",");

    public static int Count
    {
        get { int n = 0; foreach (var d in list) if (Has(d.id)) n++; return n; }
    }

    public static void Unlock(string id)
    {
        if (Has(id)) return;
        Progress.Data.unlocked += (Progress.Data.unlocked == "" ? "" : ",") + id;
        Progress.Save();
        foreach (var d in list)
            if (d.id == id && instance != null) instance.pending.Enqueue(d.name);
    }

    public static int TotalStars() { int n = 0; foreach (int s in Progress.Data.stars) n += s; return n; }

    // fila de estrellas para los paneles de fin de sector
    public static Image[] MakeStars(RectTransform parent, Vector2 pos)
    {
        var imgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var g = new GameObject("Estrella" + (i + 1), typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)g.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(54f, 54f);
            rt.anchoredPosition = pos + new Vector2((i - 1) * 66f, 0f);
            imgs[i] = g.GetComponent<Image>();
            imgs[i].sprite = StarSprite(false);
            imgs[i].raycastTarget = false;
        }
        return imgs;
    }

    // enciende las estrellas ganadas con un pequeno rebote escalonado
    public static void ShowStars(Image[] imgs, int lit, MonoBehaviour runner)
    {
        for (int i = 0; i < imgs.Length; i++) imgs[i].sprite = StarSprite(false);
        runner.StartCoroutine(LightStars(imgs, lit));
    }

    static System.Collections.IEnumerator LightStars(Image[] imgs, int lit)
    {
        for (int i = 0; i < lit; i++)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            imgs[i].sprite = StarSprite(true);
            AudioManager.Play(Sfx.Star, 1f + 0.15f * i);
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                imgs[i].transform.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(t / 0.25f * Mathf.PI));
                yield return null;
            }
            imgs[i].transform.localScale = Vector3.one;
        }
    }

    // estrella de cinco puntas dibujada pixel a pixel (encendida = dorada, apagada = gris)
    static Sprite StarSprite(bool lit)
    {
        if (starOn != null) return lit ? starOn : starOff;
        starOn = DrawStar(new Color32(255, 214, 64, 255), new Color32(255, 246, 170, 255), new Color32(120, 70, 10, 255));
        starOff = DrawStar(new Color32(70, 78, 96, 255), new Color32(90, 99, 118, 255), new Color32(30, 34, 46, 255));
        return lit ? starOn : starOff;
    }

    static Sprite DrawStar(Color32 fill, Color32 shine, Color32 edge)
    {
        const int N = 15;
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 7f : 3.0f;
            pts[i] = new Vector2(7.5f + Mathf.Cos(a) * r, 7.2f + Mathf.Sin(a) * r);
        }
        bool Inside(float x, float y)
        {
            bool c = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > y) != (pts[j].y > y) && x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) c = !c;
            return c;
        }
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool ins = Inside(x + 0.5f, y + 0.5f);
                bool border = !ins && (Inside(x + 1.5f, y + 0.5f) || Inside(x - 0.5f, y + 0.5f) || Inside(x + 0.5f, y + 1.5f) || Inside(x + 0.5f, y - 0.5f));
                tex.SetPixel(x, y, ins ? (x + y > 17 && x < 7 ? shine : fill) : border ? edge : new Color32(0, 0, 0, 0));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f);
    }

    // ---------- interfaz ----------
    void Awake()
    {
        instance = this;
        BuildToast();
        BuildPanel();
    }

    Text MakeText(Transform parent, string name, int size, TextAnchor align, Color color, Vector2 sizeDelta, Vector2 pos)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)g.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;
        var t = g.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        return t;
    }

    void BuildToast()
    {
        var g = new GameObject("LogroAviso", typeof(RectTransform), typeof(Image), typeof(Outline));
        toast = (RectTransform)g.transform;
        toast.SetParent(transform, false);
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 1f);
        toast.pivot = new Vector2(0.5f, 1f);
        toast.sizeDelta = new Vector2(450f, 84f);
        toast.anchoredPosition = new Vector2(0f, -14f);
        var bg = g.GetComponent<Image>(); bg.color = new Color(0.07f, 0.1f, 0.18f, 0.95f); bg.raycastTarget = false;
        var ol = g.GetComponent<Outline>(); ol.effectColor = new Color(1f, 0.82f, 0.25f, 1f); ol.effectDistance = new Vector2(3f, -3f);
        var icon = new GameObject("Icono", typeof(RectTransform), typeof(Image));
        var irt = (RectTransform)icon.transform;
        irt.SetParent(toast, false);
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
        irt.sizeDelta = new Vector2(60f, 60f); irt.anchoredPosition = new Vector2(46f, 0f);
        var im = icon.GetComponent<Image>(); im.sprite = StarSprite(true); im.raycastTarget = false;
        toastTitle = MakeText(toast, "Titulo", 17, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.25f), new Vector2(340f, 26f), new Vector2(48f, 20f));
        toastName = MakeText(toast, "Nombre", 26, TextAnchor.MiddleLeft, Color.white, new Vector2(340f, 36f), new Vector2(48f, -14f));
        toastTitle.text = "LOGRO DESBLOQUEADO";
        toast.gameObject.SetActive(false);
    }

    void BuildPanel()
    {
        panel = new GameObject("PanelLogros", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)panel.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var bg = panel.GetComponent<Image>(); bg.color = new Color(0.03f, 0.05f, 0.1f, 0.98f);
        panelText = MakeText(panel.transform, "Lista", 24, TextAnchor.UpperCenter, Color.white, new Vector2(1000f, 640f), new Vector2(0f, 0f));
        panelText.lineSpacing = 1.15f;
        panel.SetActive(false);
    }

    void RefreshPanel()
    {
        var sb = new System.Text.StringBuilder($"<size=36><color=#ffd84a>LOGROS  {Count}/{list.Length}</color></size>\n<size=20><color=#9fe3ff>Estrellas {TotalStars()}/6</color></size>\n\n");
        foreach (var d in list)
            sb.Append(Has(d.id) ? $"<color=#ffd84a>{d.name}</color>  <color=#d8e6f5>· {d.desc}</color>\n" : $"<color=#6f7d94>{d.name}  · {d.desc}</color>\n");
        sb.Append("\n<size=20><color=#9fe3ff>L o Esc: cerrar</color></size>");
        panelText.text = sb.ToString();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var gm = GameManager.I;
        if (kb != null && gm != null)
        {
            bool canOpen = gm.State == GamePhase.Menu || gm.State == GamePhase.Paused || gm.State == GamePhase.Break || gm.Ended;
            if (panel.activeSelf && (kb.lKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) { panel.SetActive(false); AudioManager.Play(Sfx.Click); }
            else if (!panel.activeSelf && canOpen && kb.lKey.wasPressedThisFrame) { RefreshPanel(); panel.SetActive(true); AudioManager.Play(Sfx.Click); }
        }

        if (toast.gameObject.activeSelf && Time.unscaledTime > toastEnd) toast.gameObject.SetActive(false);
        if (!toast.gameObject.activeSelf && pending.Count > 0)
        {
            toastName.text = pending.Dequeue();
            toast.gameObject.SetActive(true);
            toastEnd = Time.unscaledTime + 3.2f;
            AudioManager.Play(Sfx.Achievement, 1f);
        }
        if (toast.gameObject.activeSelf)
        {
            float t = 3.2f - (toastEnd - Time.unscaledTime);   // entra deslizando y sale hacia arriba
            float rise = 1f - Mathf.Clamp01(t / 0.3f);
            float y = -14f + 200f * rise * rise + 200f * Mathf.Clamp01((t - 2.9f) / 0.3f);
            toast.anchoredPosition = new Vector2(0f, y);
        }
    }
}
