using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private bool debugInventoryLogs = true;

    private readonly HashSet<string> items = new HashSet<string>();
    private readonly List<string> itemOrder = new List<string>();
    private readonly Dictionary<string, InventoryItemData> itemDataById = new Dictionary<string, InventoryItemData>();
    private readonly HashSet<string> unresolvedItemWarnings = new HashSet<string>();

    public event Action Changed;

    private void Awake()
    {
        GameManager.inventory = this;
    }

    private void OnDestroy()
    {
        if (GameManager.inventory == this)
            GameManager.inventory = null;
    }

    public bool Has(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && items.Contains(itemId);
    }

    public bool Has(QuestItemData item)
    {
        return Has((InventoryItemData)item);
    }

    public bool Has(InventoryItemData item)
    {
        return item != null && Has(item.itemId);
    }

    public void AddById(string itemId)
    {
        TryAddById(itemId);
    }

    public bool TryAddById(string itemId)
    {
        return TryAddById(itemId, null);
    }

    public void ClearAll()
    {
        if (items.Count == 0 && itemOrder.Count == 0 && itemDataById.Count == 0)
            return;

        items.Clear();
        itemOrder.Clear();
        itemDataById.Clear();
        unresolvedItemWarnings.Clear();
        NotifyChanged("ClearAll");
    }

    public string[] GetAllItemIds()
    {
        return itemOrder.ToArray();
    }

    public InventoryItemData[] GetOwnedItemsByCategory(InventoryItemCategory category)
    {
        var result = new List<InventoryItemData>(itemOrder.Count);

        for (int i = 0; i < itemOrder.Count; i++)
        {
            string itemId = itemOrder[i];
            if (!TryGetItemData(itemId, out InventoryItemData item) || item == null)
                continue;

            if (MatchesCategory(item, category))
                result.Add(item);
        }

        return result.ToArray();
    }

    public bool[] GetCollectionPieceStates(CollectionSetData set)
    {
        if (set == null)
            return Array.Empty<bool>();

        bool[] states = new bool[set.PieceCount];
        CollectionPieceData[] pieces = set.Pieces;

        if (pieces == null)
            return states;

        for (int i = 0; i < pieces.Length; i++)
        {
            CollectionPieceData piece = pieces[i];
            if (piece == null || piece.CollectionSet != set)
                continue;

            int pieceIndex = piece.PieceIndex;
            if (pieceIndex < 0 || pieceIndex >= states.Length)
                continue;

            states[pieceIndex] = Has(piece);
        }

        return states;
    }

    public bool TryAdd(InventoryItemData item)
    {
        if (item == null)
            return false;

        if (string.IsNullOrWhiteSpace(item.itemId))
        {
            Debug.LogWarning($"[PlayerInventory] Item '{item.name}' has no itemId.", item);
            return false;
        }

        if (!TryAddById(item.itemId, item))
            return false;

        if (debugInventoryLogs)
            Debug.Log($"[Inventory] Added '{item.displayName}' ({item.itemId}), type={item.GetType().Name}, category={item.category}, icon={(item.icon != null ? item.icon.name : "null")}", this);
        return true;
    }

    public void Add(QuestItemData item)
    {
        TryAdd(item);
    }

    public void Add(InventoryItemData item)
    {
        TryAdd(item);
    }

    public bool Remove(QuestItemData item)
    {
        return Remove((InventoryItemData)item);
    }

    public bool Remove(InventoryItemData item)
    {
        if (item == null)
            return false;

        return RemoveById(item.itemId);
    }

    public bool RemoveById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (!items.Remove(itemId))
            return false;

        itemOrder.Remove(itemId);
        itemDataById.Remove(itemId);
        unresolvedItemWarnings.Remove(itemId);

        if (debugInventoryLogs)
            Debug.Log($"[Inventory] Removed '{itemId}'", this);

        NotifyChanged($"Remove {itemId}");
        return true;
    }

    public bool TryGetItemData(string itemId, out InventoryItemData item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (itemDataById.TryGetValue(itemId, out item) && item != null)
            return true;

        item = InventoryItemRegistry.Resolve(itemId);
        if (item != null)
        {
            itemDataById[itemId] = item;
            unresolvedItemWarnings.Remove(itemId);
            return true;
        }

        if (unresolvedItemWarnings.Add(itemId))
            Debug.LogWarning($"[PlayerInventory] Could not resolve item data for '{itemId}'.");

        return false;
    }

    private bool TryAddById(string itemId, InventoryItemData itemData)
    {
        if (string.IsNullOrWhiteSpace(itemId) || items.Contains(itemId))
            return false;

        items.Add(itemId);
        itemOrder.Add(itemId);

        InventoryItemData resolvedItem = itemData != null ? itemData : InventoryItemRegistry.Resolve(itemId);
        if (resolvedItem != null)
        {
            itemDataById[itemId] = resolvedItem;
            unresolvedItemWarnings.Remove(itemId);
        }

        NotifyChanged($"Add {itemId}");
        return true;
    }

    private void NotifyChanged(string reason)
    {
        if (debugInventoryLogs)
            Debug.Log($"[Inventory] {reason} -> {BuildInventorySnapshot()}", this);

        Changed?.Invoke();
    }

    private static bool MatchesCategory(InventoryItemData item, InventoryItemCategory category)
    {
        if (item == null)
            return false;

        if (item.category == category)
            return true;

        if (category == InventoryItemCategory.Quest && item is QuestItemData)
            return true;

        if (category == InventoryItemCategory.CollectionPiece && item is CollectionPieceData)
            return true;

        return false;
    }

    private string BuildInventorySnapshot()
    {
        if (itemOrder.Count == 0)
            return "[]";

        var entries = new List<string>(itemOrder.Count);
        for (int i = 0; i < itemOrder.Count; i++)
        {
            string itemId = itemOrder[i];
            if (TryGetItemData(itemId, out InventoryItemData item) && item != null)
            {
                entries.Add($"{item.itemId}:{item.GetType().Name}:{item.category}:icon={(item.icon != null ? item.icon.name : "null")}");
                continue;
            }

            entries.Add($"{itemId}:unresolved");
        }

        return "[" + string.Join(", ", entries) + "]";
    }
}
