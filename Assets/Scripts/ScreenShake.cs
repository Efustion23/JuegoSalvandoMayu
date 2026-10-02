using Unity.Cinemachine;
using UnityEngine;

// Temblor breve de camara: mueve el TargetOffset del composer de Cinemachine y lo devuelve a su valor.
public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;

    private CinemachinePositionComposer composer;
    private Vector3 baseOffset;
    private float amplitude, duration, elapsed;

    void Awake()
    {
        instance = this;
        composer = GetComponent<CinemachinePositionComposer>();
        baseOffset = composer.TargetOffset;
    }

    public static void Shake(float amp, float seconds)
    {
        if (instance == null) return;
        instance.amplitude = amp; instance.duration = seconds; instance.elapsed = 0f;
    }

    void LateUpdate()
    {
        if (elapsed >= duration) { composer.TargetOffset = baseOffset; return; }
        elapsed += Time.unscaledDeltaTime;
        float k = 1f - elapsed / duration;
        composer.TargetOffset = baseOffset + (Vector3)(Random.insideUnitCircle * amplitude * k);
    }
}
