using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Prologo guiado: mover, correr, recoger, reciclar, patear, almacen. T salta el tutorial.
public class Tutorial : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private GameObject box;
    [SerializeField] private Transform player;
    [SerializeField] private Backpack backpack;
    [SerializeField] private InfractorSpawner spawner;
    [SerializeField] private GameObject trashPrefab;

    private Coroutine run;

    public void Begin()
    {
        if (run != null) StopCoroutine(run);
        run = StartCoroutine(Steps());
    }

    void Update()
    {
        if (run != null && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && Time.timeScale > 0f) Finish();
    }

    void Finish()
    {
        StopCoroutine(run);
        run = null;
        box.SetActive(false);
        GameManager.I.FinishTutorial();
    }

    void Say(string text)
    {
        box.SetActive(true);
        label.text = text + "\n<size=16>T: saltar tutorial</size>";
    }

    IEnumerator Steps()
    {
        var kb = Keyboard.current;
        Vector2 start = player.position;

        Say("Muévete con WASD");
        yield return new WaitUntil(() => Vector2.Distance(start, player.position) > 4f);

        Say("Mantén SHIFT mientras caminas para correr");
        yield return new WaitUntil(() => kb.leftShiftKey.isPressed && Vector2.Distance(start, player.position) > 6f);

        Instantiate(trashPrefab, start + new Vector2(4f, 0f), Quaternion.identity);
        Say("Hay basura en el sendero.\nPísala para recogerla en tu mochila");
        yield return new WaitUntil(() => backpack.Count > 0);

        int credits = GameManager.I.Credits;
        Say("Llévala a la estación de reciclaje (tachos, en la base al noroeste).\nEntra a la zona para cambiarla por Eco-Créditos");
        yield return new WaitUntil(() => GameManager.I.Credits > credits);

        int scared = GameManager.I.Scared + GameManager.I.Convinced;
        Say("¡Un infractor! Acércate y mantén E para concientizarlo.\nSi se niega, pateálo con ESPACIO (mantenlo para una patada cargada)");
        while (GameManager.I.Scared + GameManager.I.Convinced == scared)
        {
            if (Infractor.Active.Count == 0) { yield return new WaitForSeconds(1f); spawner.SpawnNear(player.position.x); }
            yield return null;
        }

        Say("Con tus Eco-Créditos compras mejoras en el Almacén Municipal:\ncolócate frente a su puerta y pulsa E");
        yield return new WaitForSeconds(5f);

        Say("¡Listo, Guardián!\nTermina cada sector con la Pureza sobre 80%");
        yield return new WaitForSeconds(3f);
        Finish();
    }
}
