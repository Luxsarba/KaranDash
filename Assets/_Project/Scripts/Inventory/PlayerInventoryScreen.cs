using TMPro;
using UnityEngine;

public class PlayerInventoryScreen : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("References")]
    [SerializeField] private InventoryLayoutData layout;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerPause playerPause;

    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private InventorySlotView[] paintingSlots;
    [SerializeField] private InventorySlotView[] storySlots;
    [SerializeField] private TMP_Text headerText;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe(true);
        Refresh();
    }

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        IsOpen = false;
        Refresh();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }
#endif

    private void Update()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(toggleKey))
                Close();

            return;
        }

        if (!Input.GetKeyDown(toggleKey))
            return;

        ResolveReferences();

        if (OverlayModalController.HasOpenOverlay)
            return;

        if (playerPause != null && playerPause.IsPaused)
            return;

        if (GameManager.isPlayerInputBlocked)
            return;

        Open();
    }

    public void Open()
    {
        ResolveReferences();

        if (inventoryPanel == null)
        {
            Debug.LogWarning("[PlayerInventoryScreen] inventoryPanel is not assigned.", this);
            return;
        }

        Refresh();
        IsOpen = true;
        OverlayModalController.Show(inventoryPanel);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        OverlayModalController.Hide(inventoryPanel);
    }

    public void Refresh()
    {
        ResolveReferences();

        if (headerText != null)
            headerText.text = layout != null && layout.paintingSet != null ? layout.paintingSet.displayName : "Inventory";

        RefreshPaintingSection();
        RefreshStorySection();
    }

    private void RefreshPaintingSection()
    {
        if (paintingSlots == null)
            return;

        CollectionSetData set = layout != null ? layout.paintingSet : null;
        int pieceCount = set != null ? set.PieceCount : 0;

        for (int i = 0; i < paintingSlots.Length; i++)
        {
            InventorySlotView slot = paintingSlots[i];
            if (slot == null)
                continue;

            if (set == null || i >= pieceCount)
            {
                slot.SetHidden();
                continue;
            }

            CollectionPieceData piece = FindPieceByIndex(set, i);
            if (piece != null && inventory != null && inventory.Has(piece))
            {
                slot.ShowOwned(piece);
                continue;
            }

            string placeholderTitle = piece != null && !string.IsNullOrWhiteSpace(piece.displayName)
                ? piece.displayName
                : $"Piece {i + 1}";
            Sprite placeholderIcon = piece != null ? piece.icon : null;
            slot.ShowPlaceholder(placeholderTitle, placeholderIcon);
        }
    }

    private void RefreshStorySection()
    {
        if (storySlots == null)
            return;

        InventorySlotDefinition[] definitions = layout != null ? layout.storyTabSlots : null;

        for (int i = 0; i < storySlots.Length; i++)
        {
            InventorySlotView slot = storySlots[i];
            if (slot == null)
                continue;

            InventorySlotDefinition definition = definitions != null && i < definitions.Length ? definitions[i] : null;
            if (definition == null)
            {
                slot.SetHidden();
                continue;
            }

            if (definition.item != null && inventory != null && inventory.Has(definition.item))
            {
                slot.ShowOwned(definition.item);
                continue;
            }

            string placeholderTitle = !string.IsNullOrWhiteSpace(definition.placeholderTitle)
                ? definition.placeholderTitle
                : definition.item != null && !string.IsNullOrWhiteSpace(definition.item.displayName)
                    ? definition.item.displayName
                    : $"Reserved {i + 1}";
            Sprite placeholderIcon = definition.placeholderIcon != null
                ? definition.placeholderIcon
                : definition.item != null ? definition.item.icon : null;

            slot.ShowPlaceholder(placeholderTitle, placeholderIcon);
        }
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            if (GameManager.inventory != null)
                inventory = GameManager.inventory;
            else if (GameManager.player != null)
                inventory = GameManager.player.GetInventory();
            else
                inventory = FindObjectOfType<PlayerInventory>(true);
        }

        if (playerPause == null)
        {
            if (GameManager.player != null)
                playerPause = GameManager.player.GetPause();
            else
                playerPause = FindObjectOfType<PlayerPause>(true);
        }

        if (paintingSlots == null || paintingSlots.Length == 0)
        {
            Transform root = inventoryPanel != null ? inventoryPanel.transform.Find("Window/PaintingSection/PaintingSlots") : null;
            if (root != null)
                paintingSlots = root.GetComponentsInChildren<InventorySlotView>(true);
        }

        if (storySlots == null || storySlots.Length == 0)
        {
            Transform root = inventoryPanel != null ? inventoryPanel.transform.Find("Window/StorySection/StorySlots") : null;
            if (root != null)
                storySlots = root.GetComponentsInChildren<InventorySlotView>(true);
        }
    }

    private void Subscribe(bool subscribe)
    {
        if (inventory == null)
            return;

        if (subscribe)
            inventory.Changed += Refresh;
        else
            inventory.Changed -= Refresh;
    }

    private static CollectionPieceData FindPieceByIndex(CollectionSetData set, int pieceIndex)
    {
        CollectionPieceData[] pieces = set != null ? set.Pieces : null;
        if (pieces == null)
            return null;

        for (int i = 0; i < pieces.Length; i++)
        {
            CollectionPieceData piece = pieces[i];
            if (piece != null && piece.CollectionSet == set && piece.PieceIndex == pieceIndex)
                return piece;
        }

        return null;
    }
}
