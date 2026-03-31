using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Registry")]
public class InventoryItemRegistry : ScriptableObject
{
    private const string DefaultResourcePath = "Inventory/InventoryItemRegistry";

    [SerializeField] private InventoryItemData[] items;

    private Dictionary<string, InventoryItemData> itemsById;
    private static InventoryItemRegistry cachedRegistry;

    public static InventoryItemData Resolve(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        InventoryItemRegistry registry = LoadDefault();
        return registry != null ? registry.Get(itemId) : null;
    }

    public InventoryItemData Get(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        EnsureLookup();
        itemsById.TryGetValue(itemId, out InventoryItemData item);
        return item;
    }

    private static InventoryItemRegistry LoadDefault()
    {
        if (cachedRegistry == null)
            cachedRegistry = Resources.Load<InventoryItemRegistry>(DefaultResourcePath);

        return cachedRegistry;
    }

    private void OnEnable()
    {
        if (cachedRegistry == null)
            cachedRegistry = this;

        EnsureLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureLookup();
    }
#endif

    private void EnsureLookup()
    {
        if (itemsById == null)
            itemsById = new Dictionary<string, InventoryItemData>();
        else
            itemsById.Clear();

        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            InventoryItemData item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            itemsById[item.itemId] = item;
        }
    }
}
