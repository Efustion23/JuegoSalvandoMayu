using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int credits;
    public int boots, backpack, whistle;   // niveles de mejora comprados
    public float bestPurity;               // record de pureza al cerrar un sector
    public bool tutorialDone;
    public int pet;                        // nivel de la nutria Mayu (0 = sin comprar)
    public int[] stars = new int[2];       // mejores estrellas por sector (0-3)
    public string unlocked = "";           // ids de logros desbloqueados, separados por coma
    public int totalScared, totalConvinced;
}

// Guardado local en PlayerPrefs (JSON). Borrar la clave reinicia el progreso.
public static class Progress
{
    const string Key = "cuidando_mayu_save";

    public static SaveData Data { get; private set; } = Load();

    static SaveData Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        var d = json == "" ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
        if (d.stars == null || d.stars.Length < 2) d.stars = new int[2];   // guardados anteriores no tenian estrellas
        if (d.unlocked == null) d.unlocked = "";
        return d;
    }

    public static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(Key);
        Data = new SaveData();
    }
}
