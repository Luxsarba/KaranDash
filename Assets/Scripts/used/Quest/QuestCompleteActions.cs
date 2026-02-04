using UnityEngine;

public class QuestCompleteActions : MonoBehaviour
{
    [Header("Что включить после квеста")]
    [SerializeField] private GameObject[] enableObjects;

    [Header("Что выключить после квеста")]
    [SerializeField] private GameObject[] disableObjects;

    [Header("Что заспавнить после квеста")]
    [SerializeField] private GameObject[] spawnPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    public void Run()
    {
        if (enableObjects != null)
            foreach (var go in enableObjects)
                if (go) go.SetActive(true);

        if (disableObjects != null)
            foreach (var go in disableObjects)
                if (go) go.SetActive(false);

        if (spawnPrefabs != null && spawnPrefabs.Length > 0)
        {
            for (int i = 0; i < spawnPrefabs.Length; i++)
            {
                var prefab = spawnPrefabs[i];
                if (!prefab) continue;

                Vector3 pos = transform.position;
                Quaternion rot = Quaternion.identity;

                if (spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null)
                {
                    pos = spawnPoints[i].position;
                    rot = spawnPoints[i].rotation;
                }

                Instantiate(prefab, pos, rot);
            }
        }
    }
}
