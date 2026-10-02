using UnityEngine;
using UnityEngine.Tilemaps;

// Bloquea el paso por el agua salvo donde hay puente. Se reconstruye al iniciar,
// asi que basta con pintar el mapa; no hay que regenerar nada a mano.
[RequireComponent(typeof(Grid))]
public class RiverCollision : MonoBehaviour
{
    [SerializeField] private Tilemap river;
    [SerializeField] private Tilemap bridge;

    void Awake()
    {
        var go = new GameObject("RiverBlock");
        go.transform.SetParent(transform, false);
        var map = go.AddComponent<Tilemap>();
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.colliderType = Tile.ColliderType.Grid;

        river.CompressBounds();
        foreach (var p in river.cellBounds.allPositionsWithin)
            if (river.HasTile(p) && !bridge.HasTile(p))
                map.SetTile(p, tile);

        go.AddComponent<CompositeCollider2D>(); // añade el Rigidbody2D requerido
        go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
    }
}
