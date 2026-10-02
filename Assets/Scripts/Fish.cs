using UnityEngine;

// Pez (o pato) que nada entre celdas de agua profunda; RiverVisuals decide cuantos estan activos segun la Pureza.
public class Fish : MonoBehaviour
{
    private Vector2[] waters;
    private Vector2 target;
    private float speed;
    private SpriteRenderer sr;

    public void Init(Vector2[] deepWater, bool tint = true)
    {
        waters = deepWater;
        sr = GetComponent<SpriteRenderer>();
        speed = Random.Range(0.4f, 0.9f);
        if (tint) sr.color = Random.value < 0.5f ? new Color(1f, 0.62f, 0.25f) : new Color(0.8f, 0.9f, 1f);
        transform.position = target = waters[Random.Range(0, waters.Length)];
    }

    void Update()
    {
        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, target) < 0.1f)
        {
            // destino cercano para que no cruce medio rio ni pase bajo los puentes
            for (int i = 0; i < 6; i++)
            {
                var c = waters[Random.Range(0, waters.Length)];
                if (Vector2.Distance(c, pos) < 4f) { target = c; break; }
            }
            return;
        }
        Vector2 dir = (target - pos).normalized;
        transform.position = pos + dir * speed * Time.deltaTime;
        sr.flipX = dir.x < 0;
    }
}
