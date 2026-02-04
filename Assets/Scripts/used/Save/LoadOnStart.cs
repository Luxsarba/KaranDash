using UnityEngine;

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

        player.playerHP = data.playerHP;
        if (player.textHP) player.textHP.text = player.playerHP.ToString();
        if (player.healtBar) player.healtBar.SetHealth((int)player.playerHP, 100);

        GameManager.currentAmmo = data.currentAmmo;
        GameManager.maxAmmo = data.maxAmmo;
        if (player.ammoText) player.ammoText.text = GameManager.currentAmmo.ToString();

        // восстановить инвентарь
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

        // телепорт к станции
        var station = SaveStationRegistry.FindById(data.lastSaveStationId);
        if (station != null)
        {
            player.transform.position = station.transform.position;
        }
        else if (data.playerPosition != null && data.playerPosition.Length == 3)
        {
            player.transform.position = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            Debug.Log("Не вошеу");
        }
    }
}
