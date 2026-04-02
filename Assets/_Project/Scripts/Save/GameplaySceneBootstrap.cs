using UnityEngine;

public class GameplaySceneBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("[GameplaySceneBootstrap] Player prefab is not assigned.", this);
            return;
        }

        PlayerSessionManager session = PlayerSessionManager.EnsureSession(playerPrefab);
        session.EnsurePlayerExists();
        session.HandleGameplaySceneBootstrap();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (TryGetPlayerComponent(playerPrefab, out _))
            return;

        playerPrefab = ResolvePlayerPrefabInEditor();
    }

    private static GameObject ResolvePlayerPrefabInEditor()
    {
        string[] candidatePaths =
        {
            "Assets/_Project/Prefabs/Player.prefab",
            "Assets/_Project/Prefabs/player.prefab",
            "Assets/_Project/Prefabs/Игрок.prefab"
        };

        for (int pathIndex = 0; pathIndex < candidatePaths.Length; pathIndex++)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[pathIndex]);
            if (TryGetPlayerComponent(prefab, out _))
                return prefab;
        }

        return null;
    }

    private static bool TryGetPlayerComponent(GameObject prefab, out Player player)
    {
        player = null;
        if (prefab == null)
            return false;

        player = prefab.GetComponentInChildren<Player>(true);
        return player != null;
    }
#endif
}
