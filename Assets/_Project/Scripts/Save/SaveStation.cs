using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveStation : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private string stationId = "station_1";
    [SerializeField] private string saveLocationLabel;
    [SerializeField] private SaveStationAnimPlayer animPlayer;
    [SerializeField] private SceneSpawnPoint spawnPoint;
    [SerializeField] private Transform respawnPoint;

    private void Awake()
    {
        if (!animPlayer)
            animPlayer = GetComponentInChildren<SaveStationAnimPlayer>(true);

        if (!respawnPoint)
        {
            Transform child = transform.Find("RespawnPoint");
            if (child == null)
                child = transform.Find("respawn point");
            if (child == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform candidate = transform.GetChild(i);
                    if (candidate.name.ToLowerInvariant().Contains("respawn"))
                    {
                        child = candidate;
                        break;
                    }
                }
            }

            respawnPoint = child;
        }

        if (!spawnPoint && respawnPoint != null)
            spawnPoint = respawnPoint.GetComponent<SceneSpawnPoint>();

        if (!spawnPoint)
            spawnPoint = GetComponentInChildren<SceneSpawnPoint>(true);
    }

    public string StationId => stationId;
    public string SpawnPointId => spawnPoint != null ? spawnPoint.SpawnPointId : string.Empty;
    public Vector3 GetSpawnPosition() => spawnPoint != null ? spawnPoint.Position : (respawnPoint != null ? respawnPoint.position : transform.position);
    public Quaternion GetSpawnRotation() => spawnPoint != null ? spawnPoint.Rotation : (respawnPoint != null ? respawnPoint.rotation : transform.rotation);

    public bool TryInteract(PlayerInteractionContext context)
    {
        Player player = context.Player != null ? context.Player : GameManager.player;
        if (player == null)
            return false;

        SaveHere(player);
        return true;
    }

    public void SaveHere(Player player)
    {
        if (animPlayer)
            animPlayer.PlayOnceWithPause();

        var inv = player.GetComponent<PlayerInventory>();
        string resolvedLocationLabel = string.IsNullOrWhiteSpace(saveLocationLabel)
            ? SceneManager.GetActiveScene().name
            : saveLocationLabel.Trim();

        PlayerSessionManager.SaveCurrentGame(player, inv, stationId, SpawnPointId, resolvedLocationLabel);
        Debug.Log($"Saved at station: {stationId}, location: {resolvedLocationLabel}");
    }
}
