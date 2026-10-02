using UnityEngine;
using UnityEngine.UI;

// Indice de Pureza de un tramo del rio (0-100). Cada sector tiene el suyo; comparten la barra del HUD.
public class RiverPurity : MonoBehaviour
{
    [SerializeField] private float purity = 100f;
    [SerializeField] private Slider slider;

    public float Purity => purity;

    void Start()
    {
        UpdateUI();
    }

    public void Add(float amount) => Set(purity + amount);

    public void Remove(float amount) => Set(purity - amount);

    public void Set(float value)
    {
        purity = Mathf.Clamp(value, 0f, 100f);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (slider != null) slider.value = purity / 100f;
    }
}
