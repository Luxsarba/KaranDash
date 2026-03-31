using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;

    public string[] inventoryItemIds;
    public string[] collectedWorldObjectIds;
    public QuestProgressSaveEntry[] questStates;

    public string lastSaveStationId;
    public float[] playerPosition;
}
