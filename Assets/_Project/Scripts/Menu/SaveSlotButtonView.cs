using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SaveSlotButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text slotLabelText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text locationText;

    public void Configure(int slotIndex, SaveSlotSummary summary, UnityAction onClick, bool interactable)
    {
        EnsureReferences();

        if (slotLabelText != null)
            slotLabelText.text = $"Слот {slotIndex}";

        if (summary == null || !summary.hasSave)
        {
            if (statusText != null)
                statusText.text = "Пусто";
            if (locationText != null)
                locationText.text = string.Empty;
        }
        else if (summary.isCorrupted)
        {
            if (statusText != null)
                statusText.text = "Поврежден";
            if (locationText != null)
                locationText.text = string.Empty;
        }
        else
        {
            if (statusText != null)
                statusText.text = summary.formattedSavedAt;
            if (locationText != null)
                locationText.text = summary.locationLabel;
        }

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (onClick != null && interactable)
            button.onClick.AddListener(onClick);

        button.interactable = interactable;
    }

    public void SetInteractable(bool interactable)
    {
        EnsureReferences();
        if (button != null)
            button.interactable = interactable;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void Reset()
    {
        EnsureReferences();
    }

    private void OnValidate()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (slotLabelText == null)
            slotLabelText = FindText("SlotLabel");

        if (statusText == null)
            statusText = FindText("StatusText");

        if (locationText == null)
            locationText = FindText("LocationText");
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null && child.TryGetComponent(out TMP_Text directText))
            return directText;

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text != null && text.name == childName)
                return text;
        }

        return null;
    }
}
