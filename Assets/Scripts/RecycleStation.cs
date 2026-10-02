using UnityEngine;

// Zona de reciclaje: al entrar con la mochila cargada convierte la basura en Eco-Creditos y pureza del sector actual.
public class RecycleStation : MonoBehaviour
{
    [SerializeField] private int creditsPerItem = 10;
    [SerializeField] private float purityPerItem = 3f;

    void OnTriggerStay2D(Collider2D other)
    {
        var bag = other.GetComponent<Backpack>();
        if (bag == null || bag.Count == 0) return;
        int n = bag.TakeAll();
        AudioManager.Play(Sfx.Recycle);
        var player = other.GetComponent<PlayerController>();
        player.Hop();
        player.Cheer(0.5f);
        GameManager.I.Recycled += n;
        GameManager.I.AddCredits(n * creditsPerItem);
        GameManager.I.Purity.Add(n * purityPerItem);
        GameManager.I.Alert($"+{n * creditsPerItem} Eco-Créditos · Pureza +{n * purityPerItem:0}%");
    }
}
