public class QuestItemPickup : InventoryItemPickup
{
    public new QuestItemData Item => item as QuestItemData;
}
