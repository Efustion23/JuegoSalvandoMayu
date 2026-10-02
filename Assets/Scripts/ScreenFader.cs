using System.Collections;
using UnityEngine;

// Fundido a negro para las transiciones (usa tiempo real: funciona con el juego en pausa).
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 1f;               // la escena empieza en negro y el GameManager la revela
        group.blocksRaycasts = false;
    }

    public IEnumerator Fade(float to, float seconds)
    {
        float from = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }
}
