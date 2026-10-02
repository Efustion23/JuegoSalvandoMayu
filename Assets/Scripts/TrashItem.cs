using UnityEngine;

// Basura recogible: se recoge al pisarla si la mochila tiene espacio.
[RequireComponent(typeof(Collider2D))]
public class TrashItem : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        var bag = other.GetComponent<Backpack>();
        if (bag == null) return;
        if (bag.TryAdd())
        {
            AudioManager.Play(Sfx.Pickup);
            other.GetComponent<PlayerController>().Pickup();
            Destroy(gameObject);
        }
        else GameManager.I.Alert("Mochila llena: llévala a la estación de reciclaje", 1.5f);
    }
}
