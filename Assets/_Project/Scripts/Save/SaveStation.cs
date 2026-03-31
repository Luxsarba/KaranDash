using UnityEngine;

public class SaveStation : MonoBehaviour
{
    [SerializeField] private string stationId = "station_1";
    [SerializeField] private SaveStationAnimPlayer animPlayer;
    [SerializeField] private Transform respawnPoint;

    private void Awake()
    {
        if (!animPlayer) animPlayer = GetComponent<SaveStationAnimPlayer>();

        if (!respawnPoint)
        {
            Transform child = transform.Find("RespawnPoint");
            if (child != null)
                respawnPoint = child;
        }
    }

    public string StationId => stationId;
    public Vector3 GetSpawnPosition() => respawnPoint != null ? respawnPoint.position : transform.position;

    public void SaveHere(Player player)
    {
        // 1) анимация
        if (animPlayer) animPlayer.PlayOnceWithPause();

        // 2) сохранение
        var inv = player.GetComponent<PlayerInventory>();
        SaveSystem.Save(player, inv, stationId);

        // опционально: сообщение "Сохранено"
        Debug.Log($"Saved at station: {stationId}");
    }
}