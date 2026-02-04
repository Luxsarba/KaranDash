using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save(Player player, PlayerInventory inv, string stationId)
    {
        if (player == null)
        {
            Debug.LogWarning("[SaveSystem] Save failed: player is null");
            return;
        }

        var data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,

            playerHP = player.playerHP,

            currentAmmo = GameManager.currentAmmo,
            maxAmmo = GameManager.maxAmmo,

            inventoryItemIds = inv != null ? inv.GetAllItemIds() : new string[0],

            lastSaveStationId = stationId,

            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }
        };

        Debug.Log("[SaveSystem] Saving inventory: " + string.Join(",", data.inventoryItemIds));


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

        var json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);

        Debug.Log($"[SaveSystem] Loaded save for scene: {data.sceneName}, station: {data.lastSaveStationId}");
        return data;
    }

    public static void ApplyToPlayer(Player player, PlayerInventory inv, SaveData data)
    {
        if (data == null || player == null)
        {
            Debug.LogWarning("[SaveSystem] Apply failed: no data or player is null");
            return;
        }

        // --- HP
        player.playerHP = 100f;

        // --- Ammo
        GameManager.currentAmmo = 10;
        GameManager.maxAmmo = data.maxAmmo;
        if (player.ammoText) player.ammoText.text = GameManager.currentAmmo.ToString();

        // --- Inventory
        if (inv != null)
        {
            inv.ClearAll();
            if (data.inventoryItemIds != null)
            {
                foreach (var id in data.inventoryItemIds)
                    inv.AddById(id);
            }
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != data.sceneName)
        {
            Debug.Log($"[SaveSystem] Scene differs (save: {data.sceneName}, current: {currentScene}). NOT teleporting.");
            return;
        }

        // поиск станции по id
        var stations = Object.FindObjectsOfType<SaveStation>();
        foreach (var s in stations)
        {
            if (s.StationId == data.lastSaveStationId)
            {
                var pos = s.transform != null ? s.transform.position : s.transform.position;
                player.transform.position = pos;
                Debug.Log($"[SaveSystem] Teleported to station: {s.StationId}");
                return;
            }
        }

        // если станции нет
        if (data.playerPosition != null && data.playerPosition.Length == 3)
        {
            player.transform.position = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            Debug.Log("[SaveSystem] Station not found. Used saved position fallback.");
        }
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }
}
