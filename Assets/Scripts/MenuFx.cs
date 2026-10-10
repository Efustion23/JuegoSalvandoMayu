using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pulido del menu principal (va en el canvas HUD): oculta el HUD mientras el menu esta abierto, muestra los datos
// en fichas con icono, el titulo "respira", la camara se desplaza despacio sobre el mapa y los botones crecen al
// seleccionarlos. Todo usa tiempo sin escala porque el menu corre con Time.timeScale = 0.
public class MenuFx : MonoBehaviour
{
    static readonly string[] HudNames = { "MarcoIzq", "MarcoDer", "Pureza", "BarraPureza", "Stats", "Tiempo", "Creditos", "Mochila", "IconoGota", "IconoReloj", "IconoMoneda", "IconoMochila" };

    GameObject menu;
    GameObject[] hud;
    Transform[] title;
    Text info, purity, credits, stars, achievements;
    CinemachineBrain brain;
    Vector3 camStart;
    bool wasMenu;

    void Start()
    {
        menu = transform.Find("PanelInicio").gameObject;
        hud = System.Array.ConvertAll(HudNames, n => transform.Find(n)?.gameObject);
        title = new[] { menu.transform.Find("MarcoTitulo"), menu.transform.Find("Titulo") };
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        foreach (var b in GetComponentsInChildren<Button>(true))
            if (b.GetComponent<ButtonPop>() == null) b.gameObject.AddComponent<ButtonPop>();
        BuildInfoRow();
        DimFooter();
    }

    // la linea "Record · Eco-Creditos · Estrellas · Logros" pasa a cuatro fichas con icono
    void BuildInfoRow()
    {
        info = menu.transform.Find("Info").GetComponent<Text>();
        var row = new GameObject("FichasInfo", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var rt = (RectTransform)row.transform;
        rt.SetParent(info.transform.parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = info.rectTransform.anchoredPosition;
        rt.sizeDelta = new Vector2(1000f, 40f);
        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10f; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;

        Sprite Icon(string n) => transform.Find(n)?.GetComponent<Image>()?.sprite;
        var star = Achievements.StarSprite(true);
        purity = Chip(rt, Icon("IconoGota"));
        credits = Chip(rt, Icon("IconoMoneda"));
        stars = Chip(rt, star);
        achievements = Chip(rt, star);
        info.gameObject.SetActive(false);
    }

    Text Chip(Transform parent, Sprite icon)
    {
        var go = new GameObject("Ficha", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.15f, 0.85f);
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 12, 4, 4); h.spacing = 6f; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
        if (icon != null)
        {
            var ic = new GameObject("Icono", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            ic.transform.SetParent(go.transform, false);
            var img = ic.GetComponent<Image>(); img.sprite = icon; img.preserveAspect = true;
            var le = ic.GetComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 20f;
        }
        var tx = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        tx.transform.SetParent(go.transform, false);
        var t = tx.GetComponent<Text>();
        t.font = info.font; t.fontSize = 20; t.color = info.color;
        t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    // controles en un recuadro y creditos de assets mas tenues, para que el centro sea el titulo y los botones
    void DimFooter()
    {
        var controls = menu.transform.Find("Controles") as RectTransform;
        if (controls != null)
        {
            var bg = new GameObject("FondoControles", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)bg.transform;
            rt.SetParent(controls.parent, false);
            rt.SetSiblingIndex(controls.GetSiblingIndex());
            rt.anchorMin = controls.anchorMin; rt.anchorMax = controls.anchorMax; rt.pivot = controls.pivot;
            rt.anchoredPosition = controls.anchoredPosition;
            rt.sizeDelta = new Vector2(700f, controls.sizeDelta.y + 8f);
            bg.GetComponent<Image>().color = new Color(0.13f, 0.19f, 0.33f, 0.75f);
        }
        var credits = menu.transform.Find("Creditos")?.GetComponent<Text>();
        if (credits != null) { var c = credits.color; c.a = 0.55f; credits.color = c; }
    }

    void LateUpdate()
    {
        bool open = menu.activeInHierarchy;
        if (open != wasMenu)
        {
            wasMenu = open;
            foreach (var h in hud) if (h != null) h.SetActive(!open);
            if (brain != null)
            {
                if (open) camStart = brain.transform.position;
                brain.enabled = !open;   // al salir, Cinemachine vuelve a seguir al guardian (el fundido lo tapa)
            }
            if (open) Refresh();
        }
        if (!open) return;

        float t = Time.unscaledTime;
        float s = 1f + 0.025f * Mathf.Sin(t * 2f);
        foreach (var tr in title) if (tr != null) tr.localScale = new Vector3(s, s, 1f);
        if (brain != null)
        {
            // ponytail: paso de 1/16 u para no romper el pixel perfect; si se ve brusco, suavizar con la camara de Cinemachine
            var p = camStart + new Vector3(Mathf.Sin(t * 0.15f) * 2f, Mathf.Sin(t * 0.11f) * 0.8f, 0f);
            p.x = Mathf.Round(p.x * 16f) / 16f; p.y = Mathf.Round(p.y * 16f) / 16f;
            brain.transform.position = p;
        }
    }

    void Refresh()
    {
        purity.text = $"Récord {Progress.Data.bestPurity:0}%";
        credits.text = $"{Progress.Data.credits}";
        stars.text = $"{Achievements.TotalStars()}/6";
        achievements.text = $"{Achievements.Count}/12 logros";
    }
}
