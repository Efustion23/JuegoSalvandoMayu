using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Flechas en el borde de la pantalla hacia cada infractor que todavia puede tirar su bolsa y esta fuera de vista.
// (El Silbato Sonico sigue mostrando, encima, su flecha grande hacia el mas cercano.)
public class InfractorArrows : MonoBehaviour
{
    [SerializeField] private float margin = 46f;

    readonly List<Image> pool = new List<Image>();
    Transform whistleArrow;

    void Start() => whistleArrow = transform.Find("Flecha");

    void LateUpdate()
    {
        int n = 0;
        var cam = Camera.main;
        if (cam != null && GameManager.I != null && GameManager.I.Running)
        {
            Vector2 center = new Vector2(Screen.width, Screen.height) / 2f;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 8f);
            foreach (var inf in Infractor.Active)
            {
                if (!inf.Kickable) continue;
                Vector3 s = cam.WorldToScreenPoint(inf.transform.position);
                if (s.x >= 0 && s.x <= Screen.width && s.y >= 0 && s.y <= Screen.height) continue;   // ya se ve
                Vector2 dir = ((Vector2)s - center).normalized;
                float sx = (Screen.width / 2f - margin) / Mathf.Max(Mathf.Abs(dir.x), 0.001f);
                float sy = (Screen.height / 2f - margin) / Mathf.Max(Mathf.Abs(dir.y), 0.001f);
                var a = Get(n++).rectTransform;
                a.position = center + dir * Mathf.Min(sx, sy);
                a.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                a.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
        for (int i = n; i < pool.Count; i++) pool[i].enabled = false;
    }

    Image Get(int i)
    {
        if (i < pool.Count) { pool[i].enabled = true; return pool[i]; }
        var go = new GameObject("FlechaInfractor", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        if (whistleArrow != null) rt.SetSiblingIndex(whistleArrow.GetSiblingIndex());   // debajo de la del silbato y de los menus
        rt.sizeDelta = new Vector2(40f, 40f);
        var img = go.GetComponent<Image>();
        img.sprite = whistleArrow != null ? whistleArrow.GetComponent<Image>().sprite : null;
        img.color = new Color(1f, 0.45f, 0.35f, 0.9f);
        img.raycastTarget = false;
        pool.Add(img);
        return img;
    }
}
