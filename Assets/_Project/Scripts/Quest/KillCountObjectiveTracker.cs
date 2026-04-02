using UnityEngine;

public class KillCountObjectiveTracker : MonoBehaviour
{
    [SerializeField] private PersistentWorldObjectId persistentStateId;
    [SerializeField] private Transform trackedEnemiesRoot;
    [SerializeField, Min(1)] private int targetCount = 12;
    [SerializeField] private InventoryItemData completionTokenItem;
    [SerializeField] private QuestCompleteActions onReachedTarget;
    [SerializeField] private FetchQuestNPC gatingQuestNpc;

    public string ObjectiveId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    public int CurrentCount => ObjectiveCounterState.GetValue(ObjectiveId);
    public bool IsCompleted => HasPersistentId &&
                               QuestProgressState.TryGetState(ObjectiveId, out _, out bool completed) &&
                               completed;

    private bool HasPersistentId => persistentStateId != null && persistentStateId.HasId;

    private void Awake()
    {
        ResolveReferences(false);
    }

    private void OnEnable()
    {
        Enemy.Died += HandleEnemyDied;
        QuestProgressState.Changed += HandleQuestStateChanged;
        ApplySavedState();
    }

    private void Start()
    {
        ApplySavedState();
    }

    private void OnDisable()
    {
        Enemy.Died -= HandleEnemyDied;
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

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || !HasPersistentId || IsCompleted || !CanTrackKills())
            return;

        Transform root = trackedEnemiesRoot != null ? trackedEnemiesRoot : transform;
        if (enemy.transform != root && !enemy.transform.IsChildOf(root))
            return;

        int newValue = ObjectiveCounterState.Increment(ObjectiveId);
        if (newValue < targetCount)
            return;

        ObjectiveCounterState.SetValue(ObjectiveId, targetCount);
        QuestProgressState.SetState(ObjectiveId, questGiven: true, questCompleted: true);

        PlayerInventory inventory = ResolveInventory();
        if (completionTokenItem != null && inventory != null && !inventory.Has(completionTokenItem))
            inventory.TryAdd(completionTokenItem);

        if (onReachedTarget != null)
            onReachedTarget.Run(inventory);
    }

    private void ApplySavedState()
    {
        if (onReachedTarget != null && IsCompleted)
            onReachedTarget.ApplyPersistentState();
    }

    private void HandleQuestStateChanged()
    {
        ApplySavedState();
    }

    private bool CanTrackKills()
    {
        return gatingQuestNpc == null || (gatingQuestNpc.IsQuestGiven && !gatingQuestNpc.IsQuestCompleted);
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

        if (trackedEnemiesRoot == null)
            trackedEnemiesRoot = transform;

#if UNITY_EDITOR
        if (ensurePersistentIdComponent && persistentStateId == null)
            persistentStateId = gameObject.AddComponent<PersistentWorldObjectId>();
#endif
    }
}
