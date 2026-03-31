using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest Item")]
public class QuestItemData : InventoryItemData
{
    private void Reset()
    {
        category = InventoryItemCategory.Quest;
    }

    private void OnValidate()
    {
        category = InventoryItemCategory.Quest;
    }
}
