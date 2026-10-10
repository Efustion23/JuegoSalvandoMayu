using System;
using UnityEngine;
using UnityEngine.UI;

// Retos cortos por sector (va en el canvas HUD): se muestran bajo el marco de pureza y dan Eco-Creditos al cumplirse.
// Cuentan desde que se activa cada reto, usando los contadores del GameManager.
public class Objectives : MonoBehaviour
{
    struct Goal
    {
        public string text; public Func<GameManager, int> value; public int target; public int reward;
        public Goal(string t, Func<GameManager, int> v, int n, int r) { text = t; value = v; target = n; reward = r; }
    }

    static readonly Goal[][] Goals =
    {
        new[] {
            new Goal("Recicla {0} residuos", g => g.Recycled, 3, 15),
            new Goal("Concientiza a {0} infractor", g => g.Convinced, 1, 20),
            new Goal("Rescata {0} bolsa del río", g => g.Rescued, 1, 15),
            new Goal("Ahuyenta a {0} con la patada", g => g.Scared, 2, 15),
            new Goal("Planta {0} queñua", g => g.Planted, 1, 20),
        },
        new[] {
            new Goal("Concientiza a {0} infractores", g => g.Convinced, 2, 25),
            new Goal("Recicla {0} residuos", g => g.Recycled, 5, 20),
            new Goal("Rescata {0} bolsas del río", g => g.Rescued, 2, 20),
            new Goal("Ahuyenta a {0} con la patada", g => g.Scared, 3, 20),
            new Goal("Planta {0} queñuas", g => g.Planted, 2, 25),
        },
    };

    GameObject box;
    Text label;
    int sector = -1, index, baseline;

    void Start()
    {
        var font = transform.Find("PanelInicio/Info").GetComponent<Text>().font;
        box = new GameObject("Reto", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        var rt = (RectTransform)box.transform;
        rt.SetParent(transform, false);
        rt.SetSiblingIndex(transform.Find("Alerta").GetSiblingIndex());   // debajo de los menus
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -132f);
        box.GetComponent<Image>().color = new Color(0.1f, 0.18f, 0.11f, 0.85f);
        var h = box.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(12, 14, 5, 5); h.childControlWidth = h.childControlHeight = true;
        var fit = box.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var tx = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        tx.transform.SetParent(rt, false);
        label = tx.GetComponent<Text>();
        label.font = font; label.fontSize = 20; label.color = new Color(1f, 0.95f, 0.82f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        box.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.I;
        bool show = gm != null && gm.State == GamePhase.Playing && gm.SectorIndex < Goals.Length;
        if (!show) { if (box.activeSelf && (gm == null || gm.State != GamePhase.Paused)) box.SetActive(false); return; }

        if (gm.SectorIndex != sector) { sector = gm.SectorIndex; index = 0; baseline = Goals[sector][0].value(gm); }
        var list = Goals[sector];
        if (index >= list.Length) { box.SetActive(false); return; }

        var goal = list[index];
        int v = goal.value(gm);
        if (v < baseline) baseline = v;                 // los contadores se reinician al empezar el sector
        int done = Mathf.Min(v - baseline, goal.target);
        if (done >= goal.target)
        {
            gm.AddCredits(goal.reward);
            AudioManager.Play(Sfx.Star);
            gm.Alert($"¡Reto cumplido! +{goal.reward} Eco-Créditos", 2.5f);
            index++;
            if (index < list.Length) baseline = list[index].value(gm);
            return;
        }
        box.SetActive(true);
        label.text = $"Reto {index + 1}/{list.Length}: {string.Format(goal.text, goal.target)}  ({done}/{goal.target})  +{goal.reward}";
    }
}
