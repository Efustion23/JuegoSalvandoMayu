using UnityEngine;
using UnityEngine.UI;

// Escala el HUD en pasos de 0.5 (1x a 720p, 1.5x a 1080p, 2x a 1440p). Un pixel de Pixelify Sans mide ~1/11 del
// tamaño de letra, asi que los textos de 22 caen en pixeles enteros de pantalla y el 6 no parece 8 ni el 5 una S.
[RequireComponent(typeof(CanvasScaler))]
public class PixelCanvasScale : MonoBehaviour
{
    private CanvasScaler scaler;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    void Update()
    {
        float fit = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        scaler.scaleFactor = fit < 1f ? fit : Mathf.Floor(fit * 2f) / 2f;   // ventanas chicas: sin redondear, para que todo quepa
    }
}
