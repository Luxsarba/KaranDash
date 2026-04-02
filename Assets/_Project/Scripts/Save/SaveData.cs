using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;
    public long savedAtUnixSecondsUtc;
    public string saveLocationLabel;

    public string[] inventoryItemIds;
    public string[] collectedWorldObjectIds;
    public QuestProgressSaveEntry[] questStates;
    public ObjectiveCounterSaveEntry[] objectiveCounters;

    public string lastSaveStationId;
    public string lastSaveSpawnPointId;
    public float[] playerPosition;
}
