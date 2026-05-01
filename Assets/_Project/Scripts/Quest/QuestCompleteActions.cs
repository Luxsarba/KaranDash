using UnityEngine;
using UnityEngine.Events;

public class QuestCompleteActions : MonoBehaviour
{
    [Header("Reward items to add to inventory")]
    [SerializeField] private InventoryItemData[] rewardItems;

    [Header("Objects to enable after completion")]
    [SerializeField] private GameObject[] enableObjects;

    [Header("Objects to disable after completion")]
    [SerializeField] private GameObject[] disableObjects;

    [Header("Prefabs to spawn after completion")]
    [SerializeField] private GameObject[] spawnPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Additional completion callbacks")]
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();

    private bool persistentWorldStateApplied;

    public void Run()
    {
        Run(null);
    }

    public void Run(PlayerInventory inventory)
    {
        GrantRewardItems(inventory);
        ApplyPersistentState();
        onCompleted?.Invoke();
    }

    public void ApplyPersistentState()
    {
        if (persistentWorldStateApplied)
            return;

        persistentWorldStateApplied = true;

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

    private void GrantRewardItems(PlayerInventory inventory)
    {
        if (rewardItems == null || rewardItems.Length == 0)
            return;

        PlayerInventory resolvedInventory = inventory != null ? inventory : ResolveInventory();
        if (resolvedInventory == null)
        {
            Debug.LogWarning("[QuestCompleteActions] Reward items were configured, but PlayerInventory was not found.", this);
            return;
        }

        for (int i = 0; i < rewardItems.Length; i++)
        {
            InventoryItemData rewardItem = rewardItems[i];
            if (rewardItem != null)
                resolvedInventory.TryAdd(rewardItem);
        }
    }

    private PlayerInventory ResolveInventory()
    {
        if (GameManager.inventory != null)
            return GameManager.inventory;

        if (GameManager.player != null)
        {
            PlayerInventory fromPlayer = GameManager.player.GetInventory();
            if (fromPlayer != null)
                return fromPlayer;
        }

        return FindObjectOfType<PlayerInventory>();
    }
}
