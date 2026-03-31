using UnityEngine;

public class FetchQuestNPC : MonoBehaviour, IPlayerInteractable
{
    [Header("Квест")]
    [SerializeField] private InventoryItemData requiredItem;
    [SerializeField] private PersistentWorldObjectId persistentStateId;

    [Header("Диалоги")]
    [SerializeField] private Dialogue beforeQuestDialogue;
    [SerializeField] private Dialogue waitingDialogue;
    [SerializeField] private Dialogue completedDialogue;
    [SerializeField] private Dialogue afterCompletedDialogue;

    [Header("События по выполнении")]
    [SerializeField] private QuestCompleteActions onCompleteActions;

    private bool questGiven;
    private bool questCompleted;
    private bool defaultQuestGiven;
    private bool defaultQuestCompleted;

    public string PersistentQuestId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    public bool HasPersistentQuestId => persistentStateId != null && persistentStateId.HasId;
    public bool IsQuestGiven => questGiven;
    public bool IsQuestCompleted => questCompleted;

    private void Awake()
    {
        ResolveReferences(false);
        defaultQuestGiven = questGiven;
        defaultQuestCompleted = questCompleted;
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

    public bool TryInteract(PlayerInteractionContext context)
    {
        Interact(context.Inventory);
        return true;
    }

    public void Interact(PlayerInventory inv)
    {
        ApplySavedState();

        if (questCompleted)
        {
            DialogueManager.Instance.StartDialogue(GetAfterCompletedDialogue());
            return;
        }

        if (!questGiven)
        {
            SetQuestState(true, false);
            DialogueManager.Instance.StartDialogue(beforeQuestDialogue);
            return;
        }

        if (inv != null && inv.Has(requiredItem))
        {
            inv.Remove(requiredItem);
            SetQuestState(true, true);
            DialogueManager.Instance.StartDialogue(completedDialogue);
            if (onCompleteActions)
                onCompleteActions.Run(inv);
        }
        else
        {
            DialogueManager.Instance.StartDialogue(waitingDialogue);
        }
    }

    private void ApplySavedState()
    {
        bool savedQuestGiven = defaultQuestGiven;
        bool savedQuestCompleted = defaultQuestCompleted;

        if (HasPersistentQuestId &&
            QuestProgressState.TryGetState(PersistentQuestId, out bool registryQuestGiven, out bool registryQuestCompleted))
        {
            savedQuestGiven = registryQuestGiven;
            savedQuestCompleted = registryQuestCompleted;
        }

        questGiven = savedQuestGiven || savedQuestCompleted;
        questCompleted = savedQuestCompleted;

        if (questCompleted && onCompleteActions != null)
            onCompleteActions.ApplyPersistentState();
    }

    private void SetQuestState(bool isGiven, bool isCompleted)
    {
        questGiven = isGiven || isCompleted;
        questCompleted = isCompleted;

        if (questCompleted && onCompleteActions != null)
            onCompleteActions.ApplyPersistentState();

        SaveQuestState();
    }

    private void SaveQuestState()
    {
        if (!HasPersistentQuestId)
            return;

        QuestProgressState.SetState(PersistentQuestId, questGiven, questCompleted);
    }

    private void HandleQuestStateChanged()
    {
        ApplySavedState();
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

    private Dialogue GetAfterCompletedDialogue()
    {
        return HasDialogueContent(afterCompletedDialogue) ? afterCompletedDialogue : completedDialogue;
    }

    private static bool HasDialogueContent(Dialogue dialogue)
    {
        if (dialogue == null)
            return false;

        if (!string.IsNullOrWhiteSpace(dialogue.name) ||
            !string.IsNullOrWhiteSpace(dialogue.email) ||
            !string.IsNullOrWhiteSpace(dialogue.recipientName))
        {
            return true;
        }

        if (dialogue.sentences == null || dialogue.sentences.Length == 0)
            return false;

        for (int i = 0; i < dialogue.sentences.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(dialogue.sentences[i]))
                return true;
        }

        return false;
    }
}
