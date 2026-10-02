using UnityEngine;

// Palabra de comic ("¡PAF!") que sube y se desvanece.
public class FloatText : MonoBehaviour
{
    [SerializeField] private float life = 0.7f;
    private TextMesh mesh;
    private float age;

    public void Show(string text)
    {
        mesh = GetComponent<TextMesh>();
        mesh.text = text;
        transform.localScale = Vector3.one * 0.4f;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float k = age / life;
        transform.position += Vector3.up * 1.2f * Time.unscaledDeltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, Mathf.Min(1f, k * 5f));   // aparece de golpe
        var c = mesh.color; c.a = 1f - k; mesh.color = c;
        if (age >= life) Destroy(gameObject);
    }
}
