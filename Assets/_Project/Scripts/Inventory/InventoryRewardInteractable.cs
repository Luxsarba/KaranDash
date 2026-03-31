using UnityEngine;

public class InventoryRewardInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private InventoryItemData requiredItem;
    [SerializeField] private bool consumeRequiredItem = true;
    [SerializeField] private InventoryItemData[] rewardItems;
    [SerializeField] private bool singleUse = true;

    private bool _used;

    public bool TryInteract(PlayerInteractionContext context)
    {
        PlayerInventory inventory = context.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryRewardInteractable] PlayerInventory was not found.", this);
            return false;
        }

        if (_used && singleUse)
        {
            Debug.Log("[InventoryRewardInteractable] Already used.", this);
            return true;
        }

        if (requiredItem != null && !inventory.Has(requiredItem))
        {
            Debug.Log($"[InventoryRewardInteractable] Missing required item: {requiredItem.itemId}", this);
            return true;
        }

        if (requiredItem != null && consumeRequiredItem)
            inventory.Remove(requiredItem);

        bool grantedAny = false;
        if (rewardItems != null)
        {
            for (int i = 0; i < rewardItems.Length; i++)
            {
                InventoryItemData rewardItem = rewardItems[i];
                if (rewardItem != null && inventory.TryAdd(rewardItem))
                    grantedAny = true;
            }
        }

        Debug.Log($"[InventoryRewardInteractable] GrantedAny={grantedAny}.", this);
        _used = grantedAny || singleUse;
        return true;
    }
}
