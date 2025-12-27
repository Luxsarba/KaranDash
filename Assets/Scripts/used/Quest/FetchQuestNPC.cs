using UnityEngine;

public class FetchQuestNPC : MonoBehaviour
{
    [Header("Квест")]
    [SerializeField] private QuestItemData requiredItem;

    [Header("Диалоги")]
    [SerializeField] private Dialogue beforeQuestDialogue;   // "принеси мне X"
    [SerializeField] private Dialogue waitingDialogue;       // "ну где оно?"
    [SerializeField] private Dialogue completedDialogue;     // "спасибо!"

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

        // квест уже выдан, проверяем предмет
        if (inv != null && inv.Has(requiredItem))
        {
            inv.Remove(requiredItem);
            questCompleted = true;
            DialogueManager.Instance.StartDialogue(completedDialogue);
        }
        else
        {
            DialogueManager.Instance.StartDialogue(waitingDialogue);
        }
    }
}
