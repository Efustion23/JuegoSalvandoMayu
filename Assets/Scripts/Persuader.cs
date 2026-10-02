using UnityEngine;
using UnityEngine.InputSystem;

// Concientizar: manteniendo E junto a un infractor que aun no tira la bolsa, el guardian le habla.
// El infractor mas cercano (a menos de Reach) escucha mientras E siga pulsada; el resultado lo decide el infractor.
public class Persuader : MonoBehaviour
{
    private const float Reach = 1.9f;
    private static bool hinted;

    void Update()
    {
        var kb = Keyboard.current;
        var gm = GameManager.I;
        if (kb == null || gm == null || !gm.Running || Shop.AnyOpen || Time.timeScale == 0f) return;

        Infractor best = null;
        float bestDist = Reach;
        foreach (var inf in Infractor.Active)
        {
            if (!inf.CanPersuade) continue;
            float d = Vector2.Distance(inf.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = inf; }
        }
        if (best == null) return;

        if (!hinted && !kb.eKey.isPressed) { hinted = true; gm.Alert("Mantén E junto al infractor para concientizarlo, o pateálo con ESPACIO", 3.5f); }
        if (kb.eKey.isPressed) best.Persuade(Time.deltaTime);
    }
}
