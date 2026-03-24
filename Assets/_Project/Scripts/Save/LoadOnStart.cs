using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadOnStart : MonoBehaviour
{
    [SerializeField] private bool loadAutomatically = true;

    private void Start()
    {
        if (!loadAutomatically)
        {
            Debug.LogWarning("[SaveSystem] auto load disabled");
            return;
        }

        var data = SaveSystem.Load();
        if (data == null)
        {
            Debug.LogWarning("[SaveSystem] no data");
            return;
        }

        var player = GameManager.player;
        if (player == null)
        {
            Debug.LogWarning("[SaveSystem] no player");
            return;
        }

        // --- Применяем статы
        float loadedHp = data.playerHP;
        if (float.IsNaN(loadedHp) || float.IsInfinity(loadedHp) || loadedHp <= 0f)
        {
            Debug.LogWarning($"[LoadOnStart] Invalid saved HP ({loadedHp}). Using default 100.");
            loadedHp = 100f;
        }

        player.playerHP = loadedHp;
        if (player.textHP) player.textHP.text = player.playerHP.ToString();
        if (player.healtBar) player.healtBar.SetHealth((int)player.playerHP, 100);

        GameManager.currentAmmo = data.currentAmmo;
        GameManager.maxAmmo = data.maxAmmo;
        if (player.ammoText) player.ammoText.text = GameManager.currentAmmo.ToString();

        // --- Восстанавливаем инвентарь
        var inv = player.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            inv.ClearAll();
            if (data.inventoryItemIds != null)
            {
                foreach (var id in data.inventoryItemIds)
                    inv.AddById(id);
            }
        }

        // --- Телепортация ТОЛЬКО если сцена совпадает
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[LoadOnStart] Current scene: '{currentScene}', Save scene: '{data.sceneName}', Station ID: '{data.lastSaveStationId}', Pos: [{string.Join(",", data.playerPosition)}]");
        
        if (currentScene != data.sceneName)
        {
            Debug.Log($"[LoadOnStart] Scene differs. Skipping teleport.");
            return;
        }

        // Сначала ищем станцию сохранения
        SaveStation[] stations = Object.FindObjectsOfType<SaveStation>();
        Debug.Log($"[LoadOnStart] Found {stations.Length} save stations on scene");
        
        SaveStation targetStation = null;
        foreach (var s in stations)
        {
            Debug.Log($"[LoadOnStart] Station found: '{s.StationId}'");
            if (s.StationId == data.lastSaveStationId)
            {
                targetStation = s;
                break;
            }
        }
        
        if (targetStation != null)
        {
            player.transform.position = targetStation.transform.position;
            Debug.Log($"[LoadOnStart] Teleported to station: {data.lastSaveStationId} at {targetStation.transform.position}");
        }
        else if (data.playerPosition != null && data.playerPosition.Length == 3)
        {
            Vector3 fallbackPos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            player.transform.position = fallbackPos;
            Debug.Log($"[LoadOnStart] Station '{data.lastSaveStationId}' not found, used fallback position: {fallbackPos}");
        }
        else
        {
            Debug.LogWarning($"[LoadOnStart] No station found and no fallback position available");
        }
    }
}
