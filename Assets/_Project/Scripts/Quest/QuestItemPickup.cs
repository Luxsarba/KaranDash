using UnityEngine;

public class QuestItemPickup : MonoBehaviour
{
    [SerializeField] private QuestItemData item;
    [SerializeField] private bool destroyOnPickup = true;

    public QuestItemData Item => item;

    public void Pickup(PlayerInventory inv)
    {
        if (item == null) return;

        inv.Add(item);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
