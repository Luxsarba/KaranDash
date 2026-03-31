using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour
{
    [Header("Owned")]
    [SerializeField] private GameObject ownedRoot;
    [SerializeField] private Image ownedIcon;
    [SerializeField] private TMP_Text ownedLabel;

    [Header("Placeholder")]
    [SerializeField] private GameObject placeholderRoot;
    [SerializeField] private Image placeholderIcon;
    [SerializeField] private TMP_Text placeholderLabel;

    [Header("Fallback")]
    [SerializeField] private Sprite localFallbackIcon;
    [SerializeField] private Color ownedIconColor = Color.white;
    [SerializeField] private Color placeholderIconColor = new Color(1f, 1f, 1f, 0.3f);

    private void Awake()
    {
        AutoResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoResolveReferences();
    }
#endif

    public void SetHidden()
    {
        gameObject.SetActive(false);
    }

    public void ShowEmpty()
    {
        gameObject.SetActive(true);
        SetRootState(ownedRoot, false);
        SetRootState(placeholderRoot, false);
    }

    public void ShowOwned(InventoryItemData item)
    {
        gameObject.SetActive(true);
        SetRootState(ownedRoot, true);
        SetRootState(placeholderRoot, false);

        if (ownedIcon != null)
        {
            ownedIcon.sprite = ResolveSprite(item != null ? item.icon : null, ownedIcon.sprite);
            ownedIcon.color = ownedIconColor;
            ownedIcon.enabled = ownedIcon.sprite != null;
        }

        if (ownedLabel != null)
            ownedLabel.text = ResolveLabel(item != null ? item.displayName : string.Empty, "Item");
    }

    public void ShowOwnedIconOnly(InventoryItemData item)
    {
        gameObject.SetActive(true);
        SetRootState(ownedRoot, true);
        SetRootState(placeholderRoot, false);

        if (ownedIcon != null)
        {
            ownedIcon.sprite = ResolveSprite(item != null ? item.icon : null, ownedIcon.sprite);
            ownedIcon.color = ownedIconColor;
            ownedIcon.enabled = ownedIcon.sprite != null;
        }

        if (ownedLabel != null)
            ownedLabel.gameObject.SetActive(false);
    }

    public void ShowPlaceholder(string title, Sprite iconOverride = null)
    {
        gameObject.SetActive(true);
        SetRootState(ownedRoot, false);
        SetRootState(placeholderRoot, true);

        if (placeholderIcon != null)
        {
            placeholderIcon.sprite = ResolveSprite(iconOverride, placeholderIcon.sprite);
            placeholderIcon.color = placeholderIconColor;
            placeholderIcon.enabled = placeholderIcon.sprite != null;
        }

        if (placeholderLabel != null)
            placeholderLabel.text = ResolveLabel(title, "Reserved");
    }

    private void AutoResolveReferences()
    {
        if (ownedRoot == null)
        {
            Transform child = transform.Find("Owned");
            if (child != null)
                ownedRoot = child.gameObject;
        }

        if (placeholderRoot == null)
        {
            Transform child = transform.Find("Placeholder");
            if (child != null)
                placeholderRoot = child.gameObject;
        }

        if (ownedIcon == null && ownedRoot != null)
            ownedIcon = ownedRoot.GetComponentInChildren<Image>(true);

        if (ownedLabel == null && ownedRoot != null)
            ownedLabel = ownedRoot.GetComponentInChildren<TMP_Text>(true);

        if (placeholderIcon == null && placeholderRoot != null)
            placeholderIcon = placeholderRoot.GetComponentInChildren<Image>(true);

        if (placeholderLabel == null && placeholderRoot != null)
            placeholderLabel = placeholderRoot.GetComponentInChildren<TMP_Text>(true);
    }

    private Sprite ResolveSprite(Sprite candidate, Sprite currentSprite)
    {
        if (candidate != null)
            return candidate;

        if (localFallbackIcon != null)
            return localFallbackIcon;

        return currentSprite;
    }

    private static void SetRootState(GameObject root, bool visible)
    {
        if (root != null)
            root.SetActive(visible);
    }

    private static string ResolveLabel(string label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label;
    }
}
