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

    public bool SupportsPlaceholderVisuals => placeholderRoot != null || placeholderIcon != null || placeholderLabel != null;

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
        AutoResolveReferences();
        gameObject.SetActive(true);
        HideOwnedVisuals();
        HidePlaceholderVisuals();
    }

    public void ShowOwned(InventoryItemData item)
    {
        AutoResolveReferences();
        gameObject.SetActive(true);
        HidePlaceholderVisuals();
        SetVisualState(ownedRoot, ownedIcon, true);

        if (ownedIcon != null)
        {
            ownedIcon.sprite = ResolveSprite(item != null ? item.icon : null, ownedIcon.sprite);
            ownedIcon.color = ownedIconColor;
            ownedIcon.enabled = ownedIcon.sprite != null;
        }

        if (ownedLabel != null)
        {
            ownedLabel.gameObject.SetActive(true);
            ownedLabel.text = ResolveLabel(item != null ? item.displayName : string.Empty, "Item");
        }
    }

    public void ShowOwnedIconOnly(InventoryItemData item)
    {
        AutoResolveReferences();
        gameObject.SetActive(true);
        HidePlaceholderVisuals();
        SetVisualState(ownedRoot, ownedIcon, true);

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
        AutoResolveReferences();

        if (!SupportsPlaceholderVisuals)
        {
            ShowEmpty();
            return;
        }

        gameObject.SetActive(true);
        HideOwnedVisuals();
        SetVisualState(placeholderRoot, placeholderIcon, true);

        if (placeholderIcon != null)
        {
            placeholderIcon.sprite = ResolveSprite(iconOverride, placeholderIcon.sprite);
            placeholderIcon.color = placeholderIconColor;
            placeholderIcon.enabled = placeholderIcon.sprite != null;
        }

        if (placeholderLabel != null)
        {
            placeholderLabel.gameObject.SetActive(true);
            placeholderLabel.text = ResolveLabel(title, "Reserved");
        }
    }

    private void AutoResolveReferences()
    {
        if (ownedRoot == null)
        {
            Transform child = FindDescendant("Owned");
            if (child != null)
                ownedRoot = child.gameObject;
        }

        if (placeholderRoot == null)
        {
            Transform child = FindDescendant("Placeholder");
            if (child != null)
                placeholderRoot = child.gameObject;
        }

        if (ownedIcon == null)
            ownedIcon = ResolveImage(ownedRoot != null ? ownedRoot.transform : transform, allowRootGraphic: ownedRoot != null);

        if (ownedLabel == null && ownedRoot != null)
            ownedLabel = ResolveText(ownedRoot.transform);

        if (placeholderIcon == null && placeholderRoot != null)
            placeholderIcon = ResolveImage(placeholderRoot.transform, allowRootGraphic: true);

        if (placeholderLabel == null && placeholderRoot != null)
            placeholderLabel = ResolveText(placeholderRoot.transform);
    }

    private Sprite ResolveSprite(Sprite candidate, Sprite currentSprite)
    {
        if (candidate != null)
            return candidate;

        if (localFallbackIcon != null)
            return localFallbackIcon;

        return currentSprite;
    }

    private void HideOwnedVisuals()
    {
        if (ownedLabel != null)
            ownedLabel.gameObject.SetActive(false);

        SetVisualState(ownedRoot, ownedIcon, false);
    }

    private void HidePlaceholderVisuals()
    {
        if (placeholderLabel != null)
            placeholderLabel.gameObject.SetActive(false);

        SetVisualState(placeholderRoot, placeholderIcon, false);
    }

    private void SetVisualState(GameObject root, Graphic graphic, bool visible)
    {
        if (root != null && root != gameObject)
        {
            root.SetActive(visible);
            return;
        }

        if (graphic != null)
            graphic.enabled = visible;
    }

    private Transform FindDescendant(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        Transform direct = transform.Find(name);
        if (direct != null)
            return direct;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child != transform && child.name == name)
                return child;
        }

        return null;
    }

    private static Image ResolveImage(Transform searchRoot, bool allowRootGraphic)
    {
        if (searchRoot == null)
            return null;

        Image namedImage = ResolveNamedImage(searchRoot);
        if (namedImage != null)
            return namedImage;

        Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            if (!allowRootGraphic && image.transform == searchRoot)
                continue;

            return image;
        }

        if (allowRootGraphic)
            return searchRoot.GetComponent<Image>();

        return null;
    }

    private static Image ResolveNamedImage(Transform searchRoot)
    {
        string[] preferredNames = { "Icon", "Image", "Artwork" };

        for (int i = 0; i < preferredNames.Length; i++)
        {
            Transform child = FindDescendant(searchRoot, preferredNames[i]);
            if (child != null && child.TryGetComponent(out Image image))
                return image;
        }

        return null;
    }

    private static TMP_Text ResolveText(Transform searchRoot)
    {
        if (searchRoot == null)
            return null;

        string[] preferredNames = { "Label", "Title", "Text" };
        for (int i = 0; i < preferredNames.Length; i++)
        {
            Transform child = FindDescendant(searchRoot, preferredNames[i]);
            if (child != null && child.TryGetComponent(out TMP_Text text))
                return text;
        }

        TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private static Transform FindDescendant(Transform searchRoot, string name)
    {
        if (searchRoot == null || string.IsNullOrWhiteSpace(name))
            return null;

        Transform direct = searchRoot.Find(name);
        if (direct != null)
            return direct;

        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child != searchRoot && child.name == name)
                return child;
        }

        return null;
    }

    private static string ResolveLabel(string label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label;
    }
}
