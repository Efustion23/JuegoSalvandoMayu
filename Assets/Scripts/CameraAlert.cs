using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// Vistazo breve de la camara hacia un infractor que aparece fuera de pantalla, y vuelta al jugador.
// Usa una segunda camara virtual que sube de prioridad un instante; el Brain hace la mezcla suave.
public class CameraAlert : MonoBehaviour
{
    private static CameraAlert instance;

    [SerializeField] private CinemachineCamera alertCamera;
    [SerializeField] private float holdSeconds = 1.1f;
    [SerializeField] private float cooldown = 7f;
    [SerializeField] private int basePriority = 0, peekPriority = 20;

    private float nextAllowed;

    void Awake()
    {
        instance = this;
        alertCamera.Priority = basePriority;
    }

    // Solo si esta fuera de vista, el sector esta tranquilo (a lo sumo el recien llegado) y ya paso el enfriamiento.
    public static void Peek(Transform target)
    {
        if (instance == null || Time.time < instance.nextAllowed) return;
        if (GameManager.I.State != GamePhase.Playing || Infractor.Active.Count > 1) return;
        Vector3 v = Camera.main.WorldToViewportPoint(target.position);
        if (v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f) return;
        instance.nextAllowed = Time.time + instance.cooldown;
        instance.StartCoroutine(instance.Look(target));
    }

    IEnumerator Look(Transform target)
    {
        alertCamera.Follow = target;
        alertCamera.Priority = peekPriority;
        yield return new WaitForSecondsRealtime(holdSeconds);
        alertCamera.Priority = basePriority;
    }
}
