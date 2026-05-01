using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private Dialogue dialogue;
    [SerializeField] private bool completeAfterDialogue;
    [SerializeField] private Dialogue afterCompletedDialogue;
    [SerializeField] private PersistentWorldObjectId persistentStateId;

    private bool completed;

    public Dialogue Dialogue => dialogue;

    private string PersistentId => persistentStateId != null ? persistentStateId.PersistentId : string.Empty;
    private bool HasPersistentId => persistentStateId != null && persistentStateId.HasId;

    private void Awake()
    {
        ResolveReferences(false);
    }

    private void OnEnable()
    {
        QuestProgressState.Changed += ApplySavedState;
        ApplySavedState();
    }

    private void Start()
    {
        ApplySavedState();
    }

    private void OnDisable()
    {
        QuestProgressState.Changed -= ApplySavedState;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        ResolveReferences(true);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences(false);
    }
#endif

    public bool TryInteract(PlayerInteractionContext context)
    {
        return TryTriggerDialogue();
    }

    public void TriggerDialogue()
    {
        TryTriggerDialogue();
    }

    public bool TryTriggerDialogue()
    {
        Dialogue dialogueToPlay = completed && HasDialogueContent(afterCompletedDialogue)
            ? afterCompletedDialogue
            : dialogue;

        if (dialogueToPlay == null)
        {
            Debug.LogWarning($"[DialogueTrigger] Dialogue is not assigned on '{name}'.", this);
            return false;
        }

        var manager = DialogueManager.Instance;
        if (manager == null)
            manager = FindObjectOfType<DialogueManager>(true);

        if (manager == null)
        {
            Debug.LogWarning("[DialogueTrigger] DialogueManager was not found in the scene.", this);
            return false;
        }

        if (!completed && completeAfterDialogue)
            manager.StartDialogue(dialogueToPlay, MarkCompleted);
        else
            manager.StartDialogue(dialogueToPlay);

        return true;
    }

    private void MarkCompleted()
    {
        completed = true;

        if (HasPersistentId)
            QuestProgressState.SetState(PersistentId, true, true);
    }

    private void ApplySavedState()
    {
        if (!HasPersistentId)
            return;

        if (QuestProgressState.TryGetState(PersistentId, out _, out bool savedCompleted))
            completed = savedCompleted;
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
