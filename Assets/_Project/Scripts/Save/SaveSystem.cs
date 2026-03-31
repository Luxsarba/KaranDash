using System;
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
            inventoryItemIds = inv != null ? inv.GetAllItemIds() : Array.Empty<string>(),
            collectedWorldObjectIds = CollectedWorldObjectState.GetAllIds(),
            questStates = BuildQuestStateSnapshot(),
            lastSaveStationId = stationId,
            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }
        };

        Debug.Log($"[SaveSystem] Saving: scene='{data.sceneName}', station='{stationId}', pos=[{string.Join(",", data.playerPosition)}], items=[{string.Join(",", data.inventoryItemIds)}], collected=[{string.Join(",", data.collectedWorldObjectIds)}], quests=[{data.questStates.Length}]");

        string json = JsonUtility.ToJson(data, prettyPrint: true);
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

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("[SaveSystem] Failed to deserialize save file.");
            return null;
        }

        data.inventoryItemIds ??= Array.Empty<string>();
        data.collectedWorldObjectIds ??= Array.Empty<string>();
        data.questStates ??= Array.Empty<QuestProgressSaveEntry>();

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

        inv ??= player.GetInventory();
        if (inv == null)
            inv = player.GetComponent<PlayerInventory>();

        Debug.Log($"[SaveSystem.ApplyToPlayer] Scene: '{data.sceneName}', Station: '{data.lastSaveStationId}', Pos: [{string.Join(",", data.playerPosition)}]");

        PlayerHealth playerHealth = player.GetHealth();
        float defaultHealth = playerHealth != null ? playerHealth.GetMaxHealth() : 100f;
        player.playerHP = defaultHealth;

        int defaultMaxAmmo = GameManager.maxAmmo > 0 ? GameManager.maxAmmo : 10;
        GameManager.maxAmmo = defaultMaxAmmo;
        GameManager.currentAmmo = defaultMaxAmmo;
        if (player.ammoText != null)
            player.ammoText.text = GameManager.currentAmmo.ToString();

        CollectedWorldObjectState.Initialize(data.collectedWorldObjectIds ?? Array.Empty<string>());
        QuestProgressState.Initialize(data.questStates ?? Array.Empty<QuestProgressSaveEntry>());

        if (inv != null)
        {
            inv.ClearAll();
            if (data.inventoryItemIds != null)
            {
                foreach (string id in data.inventoryItemIds)
                    inv.TryAddById(id);
            }
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != data.sceneName)
        {
            Debug.Log($"[SaveSystem] Scene differs (save: {data.sceneName}, current: {currentScene}). NOT teleporting.");
            return;
        }

        SaveStation[] stations = UnityEngine.Object.FindObjectsOfType<SaveStation>();
        foreach (SaveStation station in stations)
        {
            if (station.StationId == data.lastSaveStationId)
            {
                Vector3 spawnPosition = station.GetSpawnPosition();
                player.transform.position = spawnPosition;
                Debug.Log($"[SaveSystem] Teleported to station: {station.StationId} at {spawnPosition}");
                return;
            }
        }

        if (data.playerPosition != null && data.playerPosition.Length == 3)
        {
            Vector3 position = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            player.transform.position = position;
            Debug.Log($"[SaveSystem] Station not found, used fallback position: {position}");
        }
        else
        {
            Debug.LogWarning("[SaveSystem] Station not found and no fallback position available");
        }
    }

    public static void DeleteSave()
    {
        if (HasSave())
            File.Delete(SavePath);
    }

    private static QuestProgressSaveEntry[] BuildQuestStateSnapshot()
    {
        QuestProgressSaveEntry[] registryEntries = QuestProgressState.GetAllEntries();
        var snapshot = new System.Collections.Generic.Dictionary<string, QuestProgressSaveEntry>(StringComparer.Ordinal);

        for (int i = 0; i < registryEntries.Length; i++)
        {
            QuestProgressSaveEntry entry = registryEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.questId))
                continue;

            snapshot[entry.questId] = new QuestProgressSaveEntry
            {
                questId = entry.questId,
                questGiven = entry.questGiven,
                questCompleted = entry.questCompleted
            };
        }

        FetchQuestNPC[] sceneNpcs = UnityEngine.Object.FindObjectsOfType<FetchQuestNPC>(true);
        for (int i = 0; i < sceneNpcs.Length; i++)
        {
            FetchQuestNPC npc = sceneNpcs[i];
            if (npc == null || !npc.HasPersistentQuestId)
                continue;

            snapshot[npc.PersistentQuestId] = new QuestProgressSaveEntry
            {
                questId = npc.PersistentQuestId,
                questGiven = npc.IsQuestGiven,
                questCompleted = npc.IsQuestCompleted
            };
        }

        QuestProgressSaveEntry[] result = new QuestProgressSaveEntry[snapshot.Count];
        int index = 0;
        foreach (QuestProgressSaveEntry entry in snapshot.Values)
            result[index++] = entry;

        return result;
    }
}
