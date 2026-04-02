using UnityEngine;

public class PersistentActionTrigger : MonoBehaviour
{
    [SerializeField] private PersistentWorldObjectId persistentStateId;
    [SerializeField] private QuestCompleteActions actions;

    public string PersistentActionId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    public bool HasPersistentActionId => persistentStateId != null && persistentStateId.HasId;

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
        if (!HasPersistentActionId)
        {
            Debug.LogWarning("[PersistentActionTrigger] Missing PersistentWorldObjectId. Running actions without persistence.", this);
            actions?.Run(ResolveInventory());
            return;
        }

        if (IsCompleted())
            return;

        QuestProgressState.SetState(PersistentActionId, questGiven: true, questCompleted: true);
        actions?.Run(ResolveInventory());
    }

    private void ApplySavedState()
    {
        if (actions == null || !IsCompleted())
            return;

        actions.ApplyPersistentState();
    }

    private bool IsCompleted()
    {
        return HasPersistentActionId &&
               QuestProgressState.TryGetState(PersistentActionId, out _, out bool completed) &&
               completed;
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

        if (actions == null)
            actions = GetComponent<QuestCompleteActions>();

#if UNITY_EDITOR
        if (ensurePersistentIdComponent && persistentStateId == null)
            persistentStateId = gameObject.AddComponent<PersistentWorldObjectId>();
#endif
    }
}
