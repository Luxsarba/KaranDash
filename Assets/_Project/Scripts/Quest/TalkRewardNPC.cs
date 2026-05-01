using UnityEngine;

public class TalkRewardNPC : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private PersistentWorldObjectId persistentStateId;
    [SerializeField] private Dialogue firstDialogue;
    [SerializeField] private Dialogue afterCompletedDialogue;
    [SerializeField] private InventoryItemData[] rewardItems;
    [SerializeField] private bool restoreMissingRewardsOnLoad;

    private bool completed;

    private string PersistentId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    private bool HasPersistentId => persistentStateId != null && persistentStateId.HasId;

    private void Awake()
    {
        if (persistentStateId == null)
            persistentStateId = GetComponent<PersistentWorldObjectId>();
    }

    private void OnEnable()
    {
        QuestProgressState.Changed += HandleQuestChanged;
        ApplySavedState();
    }

    private void OnDisable()
    {
        QuestProgressState.Changed -= HandleQuestChanged;
    }

    public bool TryInteract(PlayerInteractionContext context)
    {
        var manager = DialogueManager.Instance != null ? DialogueManager.Instance : FindObjectOfType<DialogueManager>(true);
        if (manager == null)
            return false;

        if (completed)
        {
            manager.StartDialogue(HasDialogue(afterCompletedDialogue) ? afterCompletedDialogue : firstDialogue);
            return true;
        }

        manager.StartDialogue(firstDialogue, CompleteAndGrant);
        return true;
    }

    private void CompleteAndGrant()
    {
        completed = true;
        if (HasPersistentId)
            QuestProgressState.SetState(PersistentId, true, true);

        GrantRewards();
    }

    private void HandleQuestChanged()
    {
        ApplySavedState();
    }

    private void ApplySavedState()
    {
        completed = HasPersistentId &&
                    QuestProgressState.TryGetState(PersistentId, out _, out bool isCompleted) &&
                    isCompleted;

        if (completed && restoreMissingRewardsOnLoad)
            GrantRewards(onlyMissing: true);
    }

    private void GrantRewards(bool onlyMissing = false)
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

            if (onlyMissing && inventory.Has(item.itemId))
                continue;

            inventory.TryAdd(item);
        }
    }

    private static bool HasDialogue(Dialogue dialogue)
    {
        return dialogue != null && dialogue.sentences != null && dialogue.sentences.Length > 0;
    }

    private static PlayerInventory ResolveInventory()
    {
        if (GameManager.inventory != null)
            return GameManager.inventory;

        if (GameManager.player != null)
            return GameManager.player.GetInventory();

        return FindObjectOfType<PlayerInventory>();
    }
}
