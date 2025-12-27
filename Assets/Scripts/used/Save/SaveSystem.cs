using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string SavePath =>
       $"{Application.persistentDataPath}/{FileName}";

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save(Player player, PlayerInventory inv, string stationId)
    {
        var data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,

            playerHP = 100f,
            currentAmmo = GameManager.currentAmmo,
            maxAmmo = GameManager.maxAmmo,

            inventoryItemIds = inv != null ? inv.GetAllItemIds() : new string[0],

            lastSaveStationId = stationId,
            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }
        };

        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"[SaveSystem] Saved to: {SavePath}");
    }

    public static SaveData Load()
    {
        if (!HasSave())
        {
            Debug.LogWarning("[SaveSystem] No save file.");
            return null;
        }

        Debug.LogWarning("[SaveSystem] Loading save file.");
        var json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }
}
