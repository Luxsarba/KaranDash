using UnityEngine;

public class QuestItemPickup : MonoBehaviour
{
    [SerializeField] private QuestItemData item;
    [SerializeField] private bool destroyOnPickup = true;

    public QuestItemData Item => item;

    public void Pickup(PlayerInventory inv)
    {
        TryPickup(inv);
    }

    public bool TryPickup(PlayerInventory inv = null)
    {
        if (item == null)
        {
            Debug.LogWarning($"[QuestItemPickup] Item is not assigned on '{name}'.", this);
            return false;
        }

        if (inv == null)
            inv = ResolveInventory();

        if (inv == null)
        {
            Debug.LogWarning($"[QuestItemPickup] PlayerInventory was not found while picking '{name}'.", this);
            return false;
        }

        inv.Add(item);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    private PlayerInventory ResolveInventory()
    {
        if (GameManager.player != null)
        {
            var fromPlayer = GameManager.player.GetInventory();
            if (fromPlayer != null)
                return fromPlayer;

            fromPlayer = GameManager.player.GetComponent<PlayerInventory>();
            if (fromPlayer != null)
                return fromPlayer;
        }

        return FindObjectOfType<PlayerInventory>();
    }
}
