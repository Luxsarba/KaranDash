using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private Dialogue dialogue;

    public Dialogue Dialogue => dialogue;

    public void TriggerDialogue()
    {
        TryTriggerDialogue();
    }

    public bool TryTriggerDialogue()
    {
        if (dialogue == null)
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

        manager.StartDialogue(dialogue);
        return true;
    }
}
