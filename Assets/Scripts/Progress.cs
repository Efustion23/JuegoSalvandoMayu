using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int credits;
    public int boots, backpack, whistle;   // niveles de mejora comprados
    public float bestPurity;               // record de pureza al cerrar un sector
    public bool tutorialDone;
}

// Guardado local en PlayerPrefs (JSON). Borrar la clave reinicia el progreso.
public static class Progress
{
    const string Key = "cuidando_mayu_save";

    public static SaveData Data { get; private set; } = Load();

    static SaveData Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        return json == "" ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
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
