using UnityEngine;

public class QuestHotbarView : MonoBehaviour
{
    [SerializeField] private InventoryLayoutData layout;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private InventorySlotView[] slots;
    [SerializeField] private bool debugHotbarLogs = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe(true);
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }
#endif

    public void Refresh()
    {
        ResolveReferences();

        if (slots == null)
            return;

        InventoryItemData[] ownedQuestItems = inventory != null
            ? inventory.GetOwnedItemsByCategory(InventoryItemCategory.Quest)
            : System.Array.Empty<InventoryItemData>();
        string[] slotStates = debugHotbarLogs ? new string[slots.Length] : null;

        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotView slot = slots[i];
            if (slot == null)
                continue;

            if (i < ownedQuestItems.Length && ownedQuestItems[i] != null)
            {
                slot.ShowOwnedIconOnly(ownedQuestItems[i]);
                if (slotStates != null)
                    slotStates[i] = $"{i + 1}:{ownedQuestItems[i].itemId}:icon={(ownedQuestItems[i].icon != null ? ownedQuestItems[i].icon.name : "null")}";
                continue;
            }

            slot.ShowEmpty();
            if (slotStates != null)
                slotStates[i] = $"{i + 1}:empty";
        }

        if (slotStates != null)
            Debug.Log($"[QuestHotbar] Refresh -> {string.Join(" | ", slotStates)}", this);
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            if (GameManager.inventory != null)
                inventory = GameManager.inventory;
            else if (GameManager.player != null)
                inventory = GameManager.player.GetInventory();
            else
                inventory = FindObjectOfType<PlayerInventory>(true);
        }

        if (slotRoot == null)
            slotRoot = ResolveSlotRoot();

        InventorySlotView[] childSlots = slotRoot != null
            ? slotRoot.GetComponentsInChildren<InventorySlotView>(true)
            : System.Array.Empty<InventorySlotView>();

        if (slots == null || slots.Length != childSlots.Length)
            slots = childSlots;
    }

    private Transform ResolveSlotRoot()
    {
        Transform explicitQuestRoot = transform.Find("Quest Hotbar/GameObject");
        if (explicitQuestRoot != null)
            return explicitQuestRoot;

        Transform localContainer = transform.Find("GameObject");
        if (localContainer != null && localContainer.GetComponentInChildren<InventorySlotView>(true) != null)
            return localContainer;

        InventorySlotView[] childSlots = GetComponentsInChildren<InventorySlotView>(true);
        if (childSlots.Length == 0)
            return null;

        Transform commonParent = childSlots[0].transform.parent;
        for (int i = 1; i < childSlots.Length && commonParent != null; i++)
        {
            Transform currentParent = childSlots[i].transform.parent;
            while (currentParent != null && currentParent != commonParent)
            {
                currentParent = currentParent.parent;
            }

            if (currentParent == null)
                return childSlots[0].transform.parent;
        }

        return commonParent != null ? commonParent : childSlots[0].transform.parent;
    }

    private void Subscribe(bool subscribe)
    {
        if (inventory == null)
            return;

        if (subscribe)
            inventory.Changed += Refresh;
        else
            inventory.Changed -= Refresh;
    }
}
