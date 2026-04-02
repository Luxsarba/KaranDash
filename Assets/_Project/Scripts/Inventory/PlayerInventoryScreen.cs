using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;

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
                slot.ShowEmpty();
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
                slot.ShowEmpty();
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
        if (inventoryPanel == null)
        {
            Transform panel = FindDescendantByName(transform, "Inventory Panel");
            if (panel != null)
                inventoryPanel = panel.gameObject;
        }

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

        UpdateResolvedSlots(ref paintingSlots, ResolveSlots("PaintingSlots", "PaintingSlot_"));

        UpdateResolvedSlots(ref storySlots, ResolveSlots("StorySlots", "StorySlot_"));

        if (headerText == null && inventoryPanel != null)
        {
            Transform header = FindDescendantByName(inventoryPanel.transform, "Header");
            if (header != null)
                headerText = header.GetComponent<TMP_Text>();
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

    private InventorySlotView[] ResolveSlots(string containerName, string slotNamePrefix)
    {
        if (inventoryPanel == null)
            return Array.Empty<InventorySlotView>();

        Transform explicitContainer = FindDescendantByName(inventoryPanel.transform, containerName);
        if (explicitContainer != null)
            return SortSlots(explicitContainer.GetComponentsInChildren<InventorySlotView>(true), slotNamePrefix);

        return SortSlots(inventoryPanel.GetComponentsInChildren<InventorySlotView>(true), slotNamePrefix);
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform descendant = descendants[i];
            if (descendant != null && descendant.name == name)
                return descendant;
        }

        return null;
    }

    private static InventorySlotView[] SortSlots(InventorySlotView[] candidates, string slotNamePrefix)
    {
        if (candidates == null || candidates.Length == 0)
            return Array.Empty<InventorySlotView>();

        var filtered = new List<InventorySlotView>(candidates.Length);
        for (int i = 0; i < candidates.Length; i++)
        {
            InventorySlotView slot = candidates[i];
            if (slot == null)
                continue;

            if (!string.IsNullOrWhiteSpace(slotNamePrefix) && !slot.name.StartsWith(slotNamePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            filtered.Add(slot);
        }

        if (filtered.Count == 0)
            filtered.AddRange(candidates);

        filtered.Sort((left, right) => CompareSlotNames(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty));
        return filtered.ToArray();
    }

    private static void UpdateResolvedSlots(ref InventorySlotView[] target, InventorySlotView[] resolved)
    {
        if (resolved == null || resolved.Length == 0)
            return;

        if (target == null || target.Length != resolved.Length)
        {
            target = resolved;
            return;
        }

        for (int i = 0; i < resolved.Length; i++)
        {
            if (target[i] != resolved[i])
            {
                target = resolved;
                return;
            }
        }
    }

    private static int CompareSlotNames(string left, string right)
    {
        int leftIndex = ExtractTrailingNumber(left);
        int rightIndex = ExtractTrailingNumber(right);

        if (leftIndex != rightIndex)
            return leftIndex.CompareTo(rightIndex);

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static int ExtractTrailingNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return int.MaxValue;

        int end = value.Length - 1;
        while (end >= 0 && char.IsDigit(value[end]))
            end--;

        if (end == value.Length - 1)
            return int.MaxValue;

        return int.TryParse(value.Substring(end + 1), out int result) ? result : int.MaxValue;
    }
}
