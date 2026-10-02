using UnityEngine;

// Trucha que salta fuera del agua de vez en cuando (solo con el rio limpio): arco corto con ondas al salir y al caer.
public class FishJump : MonoBehaviour
{
    private const float Air = 0.6f, Height = 0.7f, Reach = 0.9f;

    private Vector2[] waters;
    private Sprite up, down;
    private SpriteRenderer sr;
    private Vector2 from, to;
    private float next, start = -1f;

    public void Init(Vector2[] deepWater, Sprite noseUp, Sprite noseDown)
    {
        waters = deepWater; up = noseUp; down = noseDown;
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    void OnEnable() => next = Time.time + Random.Range(1f, 6f);

    void Update()
    {
        if (start < 0f)
        {
            if (Time.time < next) return;
            from = waters[Random.Range(0, waters.Length)];
            to = from + new Vector2(Random.value < 0.5f ? -Reach : Reach, 0f);
            if (!RiverVisuals.IsWater(to)) { next = Time.time + 0.5f; return; }
            start = Time.time;
            sr.enabled = true;
            sr.flipX = to.x < from.x;
            RiverVisuals.RippleAt(from);
        }
        float k = (Time.time - start) / Air;
        if (k >= 1f)
        {
            start = -1f;
            sr.enabled = false;
            RiverVisuals.RippleAt(to);
            next = Time.time + Random.Range(3f, 9f);
            return;
        }
        transform.position = Vector2.Lerp(from, to, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * Height);
        sr.sprite = k < 0.5f ? up : down;
    }
}
