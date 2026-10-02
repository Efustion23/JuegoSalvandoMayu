using System.Collections.Generic;
using UnityEngine;

// Botadero informal en la orilla: la basura acumulada atrae a mas infractores. Si el guardian la recoge toda,
// el lugar se recupera (bonus de pureza y creditos) y deja de atraerlos: un sitio limpio invita a no ensuciar.
public class DumpSite : MonoBehaviour
{
    private static readonly List<DumpSite> all = new List<DumpSite>();

    [SerializeField] private float radius = 2.5f, purityBonus = 5f;
    [SerializeField] private int creditBonus = 30;
    [SerializeField] private TextMesh sign;

    private readonly List<TrashItem> trash = new List<TrashItem>();
    private bool clean;

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Start() => trash.AddRange(GetComponentsInChildren<TrashItem>());   // solo su propio monton

    bool Contains(Vector2 p) => Vector2.Distance(p, transform.position) <= radius;

    // la lluvia no se lleva la basura amontonada del botadero: hay que limpiarlo a mano
    public static bool IsInside(Vector2 p) => all.Exists(s => s.Contains(p));

    public static bool DirtyNear(Vector2 p, float range) =>
        all.Exists(s => !s.clean && Vector2.Distance(s.transform.position, p) < range);

    void Update()
    {
        if (clean || !GameManager.I.Running) return;
        trash.RemoveAll(t => t == null);
        if (trash.Count > 0) return;
        clean = true;
        GameManager.I.Purity.Add(purityBonus);
        GameManager.I.AddCredits(creditBonus);
        AudioManager.Play(Sfx.Recycle);
        GameManager.I.Alert($"¡Botadero recuperado! Pureza +{purityBonus:0}% · +{creditBonus} Eco-Créditos", 3f);
        sign.text = "ZONA RECUPERADA";
        sign.color = new Color(0.55f, 1f, 0.55f);
    }
}
