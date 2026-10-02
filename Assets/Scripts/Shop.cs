using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Almacen Municipal: zona frente a la puerta. Con el jugador dentro, E abre la tienda y 1-3 compran.
public class Shop : MonoBehaviour
{
    private struct Item { public string name, desc; public int[] prices; }

    private static readonly Item[] items =
    {
        new Item { name = "Botas de Sprint",       desc = "+0.3 de velocidad al correr",     prices = new[] { 60, 120 } },
        new Item { name = "Mochila Expandida",     desc = "+3 espacios en la mochila",       prices = new[] { 50, 100, 150 } },
        new Item { name = "Silbato Sónico",        desc = "Flecha hacia el infractor más cercano", prices = new[] { 80 } },
        new Item { name = "Nutria Mayu",           desc = "Te sigue y trae la basura cercana (Nv3: saca bolsas del río)", prices = new[] { 90, 160, 240 } },
    };

    [SerializeField] private GameObject panel;
    [SerializeField] private Text body;

    public static bool AnyOpen { get; private set; }

    private bool inZone, open;

    void OnTriggerEnter2D(Collider2D other) { if (other.GetComponent<Backpack>() != null) inZone = true; }
    void OnTriggerExit2D(Collider2D other) { if (other.GetComponent<Backpack>() != null) inZone = false; }

    void Start() => panel.SetActive(false);

    static int Level(int i) => i == 0 ? Progress.Data.boots : i == 1 ? Progress.Data.backpack : i == 2 ? Progress.Data.whistle : Progress.Data.pet;

    static void SetLevel(int i, int v)
    {
        if (i == 0) Progress.Data.boots = v; else if (i == 1) Progress.Data.backpack = v; else if (i == 2) Progress.Data.whistle = v; else Progress.Data.pet = v;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !GameManager.I.Running) return;

        if (!open)
        {
            if (inZone)
            {
                GameManager.I.Alert("E: Almacén Municipal", 0.2f);
                if (kb.eKey.wasPressedThisFrame) Toggle(true);
            }
            return;
        }

        if (kb.eKey.wasPressedThisFrame) { Toggle(false); return; }
        if (kb.digit1Key.wasPressedThisFrame) Buy(0);
        if (kb.digit2Key.wasPressedThisFrame) Buy(1);
        if (kb.digit3Key.wasPressedThisFrame) Buy(2);
        if (kb.digit4Key.wasPressedThisFrame) Buy(3);
    }

    void Toggle(bool value)
    {
        AudioManager.Play(Sfx.Click);
        open = AnyOpen = value;
        panel.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
        if (value) Refresh();
    }

    void Buy(int i)
    {
        int lvl = Level(i);
        if (lvl >= items[i].prices.Length) return;
        if (!GameManager.I.TrySpend(items[i].prices[lvl])) { AudioManager.Play(Sfx.Deny); Refresh("Eco-Créditos insuficientes"); return; }
        AudioManager.Play(Sfx.Buy);
        if (i == 3) { AudioManager.Play(Sfx.Pet); Achievements.Unlock("amigo"); }
        SetLevel(i, lvl + 1);
        Progress.Save();
        GameManager.I.ApplyUpgrades();
        Refresh($"Compraste {items[i].name}");
    }

    void Refresh(string msg = "")
    {
        string s = $"ALMACÉN MUNICIPAL\nEco-Créditos: {GameManager.I.Credits}\n\n";
        for (int i = 0; i < items.Length; i++)
        {
            int lvl = Level(i), max = items[i].prices.Length;
            string price = lvl >= max ? "MÁXIMO" : $"{items[i].prices[lvl]} créditos";
            s += $"[{i + 1}] {items[i].name}  Nv {lvl}/{max}  ·  {price}\n<size=18><color=#9fb4cc>      {items[i].desc}</color></size>\n\n";
        }
        body.text = s + msg + "\n\nE: cerrar";
    }
}
