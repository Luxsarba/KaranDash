using UnityEngine;

public class NoteTrigger : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private string noteTitle = "Zametka";
    [SerializeField] private string noteSubtitle = "Story Note";
    [SerializeField, TextArea(6, 20)] private string noteText =
        "Eto zaglushka zapisok. Zdes mozhno zadat lyuboj tekst dlya syuzhetnoj zametki.";
    [SerializeField] private Sprite backgroundSprite;

    public bool TryInteract(PlayerInteractionContext context)
    {
        return TryTriggerNote();
    }

    public void TriggerNote()
    {
        TryTriggerNote();
    }

    public bool TryTriggerNote()
    {
        var viewer = NoteViewer.Instance;
        if (viewer == null)
            viewer = FindObjectOfType<NoteViewer>(true);

        if (viewer == null)
        {
            Debug.LogWarning("[NoteTrigger] NoteViewer was not found in the scene.", this);
            return false;
        }

        viewer.OpenNote(noteTitle, noteSubtitle, noteText, backgroundSprite);
        return true;
    }
}