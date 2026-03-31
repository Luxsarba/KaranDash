using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Layout Data")]
public class InventoryLayoutData : ScriptableObject
{
    public CollectionSetData paintingSet;
    public InventorySlotDefinition[] questHudSlots;
    public InventorySlotDefinition[] storyTabSlots;
}
