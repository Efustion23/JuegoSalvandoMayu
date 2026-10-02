using UnityEngine;

// Silbato Sonico: flecha en el borde de la pantalla hacia el infractor pateable mas cercano fuera de vista.
public class WhistleIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private Transform player;
    [SerializeField] private float margin = 60f;

    void Update()
    {
        Vector3 target = Vector3.zero;
        bool found = false;
        float best = float.MaxValue;
        if (Progress.Data.whistle > 0)
            foreach (var inf in Infractor.Active)
            {
                if (!inf.Kickable) continue;
                Vector3 s = Camera.main.WorldToScreenPoint(inf.transform.position);
                if (s.x >= 0 && s.x <= Screen.width && s.y >= 0 && s.y <= Screen.height) continue;   // ya se ve
                float d = ((Vector2)inf.transform.position - (Vector2)player.position).sqrMagnitude;
                if (d < best) { best = d; target = s; found = true; }
            }

        arrow.gameObject.SetActive(found);
        if (!found) return;

        Vector2 center = new Vector2(Screen.width, Screen.height) / 2f;
        Vector2 dir = ((Vector2)target - center).normalized;
        float sx = (Screen.width / 2f - margin) / Mathf.Max(Mathf.Abs(dir.x), 0.001f);
        float sy = (Screen.height / 2f - margin) / Mathf.Max(Mathf.Abs(dir.y), 0.001f);
        arrow.position = center + dir * Mathf.Min(sx, sy);
        arrow.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
}
