using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private const string SlotFileNameFormat = "save_slot_{0}.json";
    private const string DebugFileName = "save_debug.json";
    private const string LegacyFileName = "save.json";

    private static string PersistentRoot => Application.persistentDataPath;
    private static string DebugSavePath => Path.Combine(PersistentRoot, DebugFileName);
    private static string LegacySavePath => Path.Combine(PersistentRoot, LegacyFileName);

    public static bool HasAnySlotSave()
    {
        EnsureLegacyMigrated();

        for (int slotIndex = 1; slotIndex <= 3; slotIndex++)
        {
            if (HasSave(slotIndex))
                return true;
        }

        return false;
    }

    public static bool HasSave(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        EnsureLegacyMigrated();
        return File.Exists(GetSlotPath(slotIndex));
    }

    public static bool HasDebugSave() => File.Exists(DebugSavePath);

    public static void Save(int slotIndex, Player player, PlayerInventory inv, string stationId, string spawnPointId, string saveLocationLabel)
    {
        ValidateSlotIndex(slotIndex);
        SaveToPath(GetSlotPath(slotIndex), player, inv, stationId, spawnPointId, saveLocationLabel, $"slot {slotIndex}");
    }

    public static void SaveDebug(Player player, PlayerInventory inv, string stationId, string spawnPointId, string saveLocationLabel)
    {
        SaveToPath(DebugSavePath, player, inv, stationId, spawnPointId, saveLocationLabel, "debug");
    }

    public static SaveData Load(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        EnsureLegacyMigrated();
        return LoadFromPath(GetSlotPath(slotIndex), $"slot {slotIndex}");
    }

    public static SaveData LoadDebug() => LoadFromPath(DebugSavePath, "debug");

    public static void DeleteSave(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        DeleteIfExists(GetSlotPath(slotIndex));
    }

    public static void DeleteDebugSave()
    {
        DeleteIfExists(DebugSavePath);
    }

    public static SaveSlotSummary GetSlotSummary(int slotIndex)
    {
        ValidateSlotIndex(slotIndex);
        EnsureLegacyMigrated();

        string slotPath = GetSlotPath(slotIndex);
        var summary = new SaveSlotSummary
        {
            slotIndex = slotIndex,
            hasSave = File.Exists(slotPath),
            isCorrupted = false,
            formattedSavedAt = string.Empty,
            locationLabel = string.Empty
        };

        if (!summary.hasSave)
            return summary;

        if (!TryReadSaveData(slotPath, out SaveData data, out _))
        {
            summary.isCorrupted = true;
            return summary;
        }

        NormalizeLoadedData(data);
        summary.formattedSavedAt = FormatSaveTimestamp(data, slotPath);
        summary.locationLabel = ResolveLocationLabel(data);
        return summary;
    }

    public static void PrepareRuntimeStateForLoadedSave(SaveData data)
    {
        if (data == null)
        {
            CollectedWorldObjectState.Clear();
            QuestProgressState.Clear();
            ObjectiveCounterState.Clear();
            return;
        }

        CollectedWorldObjectState.Initialize(data.collectedWorldObjectIds ?? Array.Empty<string>());
        QuestProgressState.Initialize(data.questStates ?? Array.Empty<QuestProgressSaveEntry>());
        ObjectiveCounterState.Initialize(data.objectiveCounters ?? Array.Empty<ObjectiveCounterSaveEntry>());
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

        Debug.Log(
            $"[SaveSystem.ApplyToPlayer] Scene: '{data.sceneName}', Station: '{data.lastSaveStationId}', " +
            $"Spawn: '{data.lastSaveSpawnPointId}', Pos: [{string.Join(",", data.playerPosition ?? Array.Empty<float>())}]");

        PlayerHealth playerHealth = player.GetHealth();
        float defaultHealth = playerHealth != null ? playerHealth.GetMaxHealth() : 100f;
        player.playerHP = defaultHealth;

        int defaultMaxAmmo = GameManager.maxAmmo > 0 ? GameManager.maxAmmo : 10;
        GameManager.maxAmmo = defaultMaxAmmo;
        GameManager.currentAmmo = defaultMaxAmmo;
        if (player.ammoText != null)
            player.ammoText.text = GameManager.currentAmmo.ToString();

        if (inv != null)
        {
            inv.ClearAll();
            string[] inventoryItemIds = data.inventoryItemIds ?? Array.Empty<string>();
            for (int i = 0; i < inventoryItemIds.Length; i++)
                inv.TryAddById(inventoryItemIds[i]);
        }
    }

    private static void SaveToPath(string path, Player player, PlayerInventory inv, string stationId, string spawnPointId, string saveLocationLabel, string saveTargetLabel)
    {
        if (player == null)
        {
            Debug.LogWarning("[SaveSystem] Save failed: player is null");
            return;
        }

        SaveData data = BuildSaveData(player, inv, stationId, spawnPointId, saveLocationLabel);

        Debug.Log(
            $"[SaveSystem] Saving {saveTargetLabel}: scene='{data.sceneName}', station='{stationId}', spawn='{spawnPointId}', " +
            $"location='{data.saveLocationLabel}', pos=[{string.Join(",", data.playerPosition)}], items=[{string.Join(",", data.inventoryItemIds)}], " +
            $"collected=[{string.Join(",", data.collectedWorldObjectIds)}], quests=[{data.questStates.Length}]");

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveSystem] Saved to: {path}");
    }

    private static SaveData LoadFromPath(string path, string label)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSystem] No {label} save file.");
            return null;
        }

        if (!TryReadSaveData(path, out SaveData data, out string error))
        {
            Debug.LogWarning($"[SaveSystem] Failed to deserialize {label} save file. {error}");
            return null;
        }

        NormalizeLoadedData(data);

        Debug.Log(
            $"[SaveSystem] Loaded {label} save for scene: {data.sceneName}, station: {data.lastSaveStationId}, " +
            $"spawn: {data.lastSaveSpawnPointId}");
        return data;
    }

    private static SaveData BuildSaveData(Player player, PlayerInventory inv, string stationId, string spawnPointId, string saveLocationLabel)
    {
        return new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            savedAtUnixSecondsUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            saveLocationLabel = ResolveSaveLocationLabel(saveLocationLabel),
            inventoryItemIds = inv != null ? inv.GetAllItemIds() : Array.Empty<string>(),
            collectedWorldObjectIds = CollectedWorldObjectState.GetAllIds(),
            questStates = BuildQuestStateSnapshot(),
            objectiveCounters = ObjectiveCounterState.GetAllEntries(),
            lastSaveStationId = stationId ?? string.Empty,
            lastSaveSpawnPointId = spawnPointId ?? string.Empty,
            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z }
        };
    }

    private static string ResolveSaveLocationLabel(string saveLocationLabel)
    {
        if (!string.IsNullOrWhiteSpace(saveLocationLabel))
            return saveLocationLabel.Trim();

        string sceneName = SceneManager.GetActiveScene().name;
        return string.IsNullOrWhiteSpace(sceneName) ? "Unknown Scene" : sceneName;
    }

    private static string ResolveLocationLabel(SaveData data)
    {
        if (!string.IsNullOrWhiteSpace(data.saveLocationLabel))
            return data.saveLocationLabel.Trim();

        if (!string.IsNullOrWhiteSpace(data.sceneName))
            return data.sceneName;

        return "Unknown Scene";
    }

    private static string FormatSaveTimestamp(SaveData data, string path)
    {
        DateTimeOffset timestamp;
        if (data.savedAtUnixSecondsUtc > 0)
        {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(data.savedAtUnixSecondsUtc).ToLocalTime();
        }
        else
        {
            timestamp = File.GetLastWriteTimeUtc(path);
            if (timestamp == default)
                return string.Empty;

            timestamp = timestamp.ToLocalTime();
        }

        return timestamp.ToString("dd.MM.yyyy HH:mm");
    }

    private static bool TryReadSaveData(string path, out SaveData data, out string error)
    {
        data = null;
        error = string.Empty;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "File is empty.";
                return false;
            }

            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        if (data == null)
        {
            error = "JsonUtility returned null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(data.sceneName))
        {
            error = "sceneName is missing.";
            return false;
        }

        return true;
    }

    private static void NormalizeLoadedData(SaveData data)
    {
        data.inventoryItemIds ??= Array.Empty<string>();
        data.collectedWorldObjectIds ??= Array.Empty<string>();
        data.questStates ??= Array.Empty<QuestProgressSaveEntry>();
        data.objectiveCounters ??= Array.Empty<ObjectiveCounterSaveEntry>();
        data.lastSaveStationId ??= string.Empty;
        data.lastSaveSpawnPointId ??= string.Empty;
        data.saveLocationLabel ??= string.Empty;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void EnsureLegacyMigrated()
    {
        if (!File.Exists(LegacySavePath))
            return;

        string slotOnePath = GetSlotPath(1);
        if (File.Exists(slotOnePath))
            return;

        try
        {
            File.Copy(LegacySavePath, slotOnePath, overwrite: false);
            File.Delete(LegacySavePath);
            Debug.Log($"[SaveSystem] Migrated legacy save to slot 1: {slotOnePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Failed to migrate legacy save: {ex.Message}");
        }
    }

    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(PersistentRoot, string.Format(SlotFileNameFormat, slotIndex));
    }

    private static void ValidateSlotIndex(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > 3)
            throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Save slot index must be between 1 and 3.");
    }

    private static QuestProgressSaveEntry[] BuildQuestStateSnapshot()
    {
        QuestProgressSaveEntry[] registryEntries = QuestProgressState.GetAllEntries();
        var snapshot = new Dictionary<string, QuestProgressSaveEntry>(StringComparer.Ordinal);

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
