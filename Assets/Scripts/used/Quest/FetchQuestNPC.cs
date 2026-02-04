using UnityEngine;

public class FetchQuestNPC : MonoBehaviour
{
    [Header(" вест")]
    [SerializeField] private QuestItemData requiredItem;

    [Header("ƒиалоги")]
    [SerializeField] private Dialogue beforeQuestDialogue;   // "принеси мне чтото"
    [SerializeField] private Dialogue waitingDialogue;       // "ну где?"
    [SerializeField] private Dialogue completedDialogue;     // "спс"

    [Header("—обыти€ по выполнении")]
    [SerializeField] private QuestCompleteActions onCompleteActions;

    private bool questGiven = false;
    private bool questCompleted = false;

    public void Interact(PlayerInventory inv)
    {
        if (questCompleted)
        {
            DialogueManager.Instance.StartDialogue(completedDialogue);
            return;
        }

        if (!questGiven)
        {
            questGiven = true;
            DialogueManager.Instance.StartDialogue(beforeQuestDialogue);
            return;
        }

        if (inv != null && inv.Has(requiredItem))
        {
            inv.Remove(requiredItem);
            questCompleted = true;
            DialogueManager.Instance.StartDialogue(completedDialogue);
            if (onCompleteActions) onCompleteActions.Run();
        }
        else
        {
            DialogueManager.Instance.StartDialogue(waitingDialogue);
        }
    }
}
