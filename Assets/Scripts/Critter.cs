using UnityEngine;

// Animal quieto de la ribera (rana o garza): mira a un lado y a otro y, si es saltarin, da brincos cortos.
public class Critter : MonoBehaviour
{
    private bool hops;
    private SpriteRenderer sr;
    private Vector3 home;
    private float next, hopStart = -10f;

    public void Init(bool hopper) => hops = hopper;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        home = transform.position;
        next = Time.time + Random.Range(1f, 4f);
    }

    void Update()
    {
        if (Time.time > next)
        {
            next = Time.time + Random.Range(1.5f, 5f);
            sr.flipX = !sr.flipX;
            if (hops) hopStart = Time.time;
        }
        float t = Time.time - hopStart;
        transform.position = home + Vector3.up * (t < 0.3f ? Mathf.Sin(t / 0.3f * Mathf.PI) * 0.25f : 0f);
    }
}
