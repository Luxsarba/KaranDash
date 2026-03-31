using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public InventoryItemCategory category = InventoryItemCategory.General;

    public bool HasValidItemId => !string.IsNullOrWhiteSpace(itemId);
}
