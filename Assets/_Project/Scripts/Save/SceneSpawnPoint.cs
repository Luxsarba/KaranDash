using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId = "default";
    [SerializeField] private bool isDefaultSceneSpawn;

    public string SpawnPointId => spawnPointId;
    public bool IsDefaultSceneSpawn => isDefaultSceneSpawn;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public static SceneSpawnPoint FindById(Scene scene, string spawnPointId)
    {
        if (string.IsNullOrWhiteSpace(spawnPointId))
            return null;

        SceneSpawnPoint match = null;
        SceneSpawnPoint[] all = FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            SceneSpawnPoint candidate = all[i];
            if (candidate == null || candidate.gameObject.scene != scene)
                continue;

            if (!string.Equals(candidate.spawnPointId, spawnPointId, System.StringComparison.Ordinal))
                continue;

            if (match != null)
            {
                Debug.LogError($"[SceneSpawnPoint] Duplicate spawnPointId '{spawnPointId}' in scene '{scene.name}'. Using '{match.name}' and ignoring '{candidate.name}'.", candidate);
                continue;
            }

            match = candidate;
        }

        return match;
    }

    public static SceneSpawnPoint FindDefaultOrFirst(Scene scene)
    {
        SceneSpawnPoint first = null;
        SceneSpawnPoint defaultSpawn = null;
        SceneSpawnPoint[] all = FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            SceneSpawnPoint candidate = all[i];
            if (candidate == null || candidate.gameObject.scene != scene)
                continue;

            first ??= candidate;

            if (!candidate.isDefaultSceneSpawn)
                continue;

            if (defaultSpawn != null)
            {
                Debug.LogError($"[SceneSpawnPoint] Multiple default spawn points found in scene '{scene.name}'. Using '{defaultSpawn.name}' and ignoring '{candidate.name}'.", candidate);
                continue;
            }

            defaultSpawn = candidate;
        }

        return defaultSpawn != null ? defaultSpawn : first;
    }
}
