using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset targetSceneAsset;
#endif

    public string TargetSceneName => targetSceneName;
    public string TargetSpawnPointId => targetSpawnPointId;

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        if (other.GetComponentInParent<Player>() == null)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"[SceneTransitionTrigger] Target scene is not assigned on '{name}'.", this);
            return;
        }

        PlayerSessionManager.TransitionTo(targetSceneName, targetSpawnPointId);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (targetSceneAsset == null)
            return;

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(targetSceneAsset);
        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        targetSceneName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
    }
#endif
}
