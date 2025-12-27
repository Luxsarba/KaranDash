using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly HashSet<string> items = new HashSet<string>();
    public void AddById(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
            items.Add(itemId);
    }

    public void ClearAll() => items.Clear();

    public string[] GetAllItemIds()
    {
        var arr = new string[items.Count];
        items.CopyTo(arr);
        return arr;
    }

    public bool Has(QuestItemData item) => item != null && items.Contains(item.itemId);

    public void Add(QuestItemData item)
    {
        if (item == null) return;
        items.Add(item.itemId);
        // обновлять UI?
        Debug.Log($"Picked up: {item.displayName} ({item.itemId})");
    }

    public bool Remove(QuestItemData item)
    {
        if (item == null) return false;
        return items.Remove(item.itemId);
    }
}
