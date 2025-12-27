using UnityEngine;

public class SaveStation : MonoBehaviour
{
    [SerializeField] private string stationId = "station_1";
    [SerializeField] private SaveStationAnimPlayer animPlayer;

    private void Awake()
    {
        if (!animPlayer) animPlayer = GetComponent<SaveStationAnimPlayer>();
    }

    public string StationId => stationId;

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
