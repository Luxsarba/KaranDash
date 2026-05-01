using UnityEngine;

public class DialogueRewardCompletion : MonoBehaviour
{
    [SerializeField] private PersistentWorldObjectId persistentStateId;
    [SerializeField] private InventoryItemData[] rewardItems;
    [SerializeField] private bool grantOnlyMissingItems = true;

    private bool completed;

    public string PersistentQuestId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    public bool HasPersistentQuestId => persistentStateId != null && persistentStateId.HasId;

    private void Awake()
    {
        ResolveReferences(false);
    }

    private void OnEnable()
    {
        QuestProgressState.Changed += HandleQuestStateChanged;
        ApplySavedState();
    }

    private void Start()
    {
        ApplySavedState();
    }

    private void OnDisable()
    {
        QuestProgressState.Changed -= HandleQuestStateChanged;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        ResolveReferences(true);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences(true);
    }
#endif

    public void Complete()
    {
        if (!completed)
        {
            completed = true;
            if (HasPersistentQuestId)
                QuestProgressState.SetState(PersistentQuestId, true, true);
        }

        EnsureCompletedRewards();
    }

    private void ApplySavedState()
    {
        completed = HasPersistentQuestId &&
                    QuestProgressState.TryGetState(PersistentQuestId, out _, out bool savedCompleted) &&
                    savedCompleted;

        if (completed)
            EnsureCompletedRewards();
    }

    private void EnsureCompletedRewards()
    {
        if (rewardItems == null || rewardItems.Length == 0)
            return;

        PlayerInventory inventory = ResolveInventory();
        if (inventory == null)
            return;

        for (int i = 0; i < rewardItems.Length; i++)
        {
            InventoryItemData item = rewardItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            if (grantOnlyMissingItems && inventory.Has(item.itemId))
                continue;

            inventory.TryAdd(item);
        }
    }

    private void HandleQuestStateChanged()
    {
        ApplySavedState();
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

    private void ResolveReferences(bool ensurePersistentIdComponent)
    {
        if (persistentStateId == null)
            persistentStateId = GetComponent<PersistentWorldObjectId>();

#if UNITY_EDITOR
        if (ensurePersistentIdComponent && persistentStateId == null)
            persistentStateId = gameObject.AddComponent<PersistentWorldObjectId>();
#endif
    }
}
