using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;

    public float playerHP;
    public int currentAmmo;
    public int maxAmmo;

    public string[] inventoryItemIds;

    public string lastSaveStationId;   
    public float[] playerPosition;     
}
