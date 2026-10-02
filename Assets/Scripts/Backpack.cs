using UnityEngine;

// Mochila del guardian: cuenta bolsas recogidas. La mejora "Mochila Expandida" fija un bonus de espacios.
public class Backpack : MonoBehaviour
{
    [SerializeField] private int baseCapacity = 5;

    private int bonus;

    public int Count { get; private set; }
    public int Capacity => baseCapacity + bonus;

    public bool TryAdd()
    {
        if (Count >= Capacity) return false;
        Count++;
        return true;
    }

    public int TakeAll()
    {
        int n = Count;
        Count = 0;
        return n;
    }

    public void SetBonus(int extra) => bonus = extra;
}
