using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class PersistentWorldObjectId : MonoBehaviour
{
    [SerializeField] private string persistentId;

    public string PersistentId => persistentId;
    public bool HasId => !string.IsNullOrWhiteSpace(persistentId);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        if (EditorSceneManager.IsPreviewScene(gameObject.scene))
            return;

        if (!NeedsIdGeneration())
            return;

        persistentId = GenerateId();
        EditorUtility.SetDirty(this);

        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private bool NeedsIdGeneration()
    {
        if (string.IsNullOrWhiteSpace(persistentId))
            return true;

        PersistentWorldObjectId[] allIds = FindObjectsOfType<PersistentWorldObjectId>(true);
        for (int i = 0; i < allIds.Length; i++)
        {
            PersistentWorldObjectId other = allIds[i];
            if (other == null || other == this)
                continue;

            if (other.persistentId == persistentId)
                return true;
        }

        return false;
    }

    private static string GenerateId()
    {
        return Guid.NewGuid().ToString("N");
    }
#endif
}
