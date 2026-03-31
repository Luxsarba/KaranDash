using System.Linq;
using UnityEngine;

public class CollectionSetDebugInteractable : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private CollectionSetData collectionSet;
    [SerializeField] private bool logInventoryIds = true;

    public bool TryInteract(PlayerInteractionContext context)
    {
        PlayerInventory inventory = context.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning("[CollectionSetDebugInteractable] PlayerInventory was not found.", this);
            return false;
        }

        if (collectionSet == null)
        {
            Debug.LogWarning("[CollectionSetDebugInteractable] CollectionSetData is not assigned.", this);
            return false;
        }

        bool[] states = inventory.GetCollectionPieceStates(collectionSet);
        string stateText = string.Join(", ", states.Select((value, index) => $"{index}:{(value ? 1 : 0)}"));
        Debug.Log($"[CollectionSetDebugInteractable] Set '{collectionSet.displayName}' -> [{stateText}]", this);

        if (logInventoryIds)
        {
            string[] ids = inventory.GetAllItemIds();
            Debug.Log($"[CollectionSetDebugInteractable] Inventory IDs -> [{string.Join(", ", ids)}]", this);
        }

        return true;
    }
}
