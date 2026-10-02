using System.Collections.Generic;
using UnityEngine;

// Animal ambiental: camina entre puntos al azar dentro de un rectangulo (corral) y descansa.
public class Animal : MonoBehaviour
{
    private static readonly List<Animal> all = new List<Animal>();

    [SerializeField] private Vector2 min;
    [SerializeField] private Vector2 max;
    [SerializeField] private float speed = 0.6f;
    [SerializeField] private float personalSpace = 1.2f;
    [SerializeField] private Sprite idle;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private bool spriteFacesLeft = true;

    private SpriteRenderer sr;
    private Vector2 target;
    private float wait;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        target = transform.position;
        wait = Random.Range(0.5f, 3f);
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    // Elige un punto libre de otros animales; tras 8 intentos acepta el ultimo.
    Vector2 PickTarget()
    {
        Vector2 p = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            p = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            bool free = true;
            foreach (var a in all)
                if (a != this && Vector2.Distance(a.target, p) < personalSpace) { free = false; break; }
            if (free) break;
        }
        return p;
    }

    void Update()
    {
        if (wait > 0f) { wait -= Time.deltaTime; return; }
        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, target) < 0.05f)
        {
            if (idle != null) sr.sprite = idle;
            wait = Random.Range(1.5f, 4f);
            target = PickTarget();
            return;
        }
        Vector2 dir = (target - pos).normalized;
        transform.position = pos + dir * speed * Time.deltaTime;
        if (Mathf.Abs(dir.x) > 0.1f) sr.flipX = spriteFacesLeft ? dir.x > 0 : dir.x < 0;
        if (frames != null && frames.Length > 1) sr.sprite = frames[(int)(Time.time * 4f) % frames.Length];
    }
}
