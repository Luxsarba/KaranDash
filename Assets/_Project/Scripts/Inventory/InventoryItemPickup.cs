using UnityEngine;

public class InventoryItemPickup : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] protected InventoryItemData item;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private PersistentWorldObjectId persistentWorldObjectId;

    public InventoryItemData Item => item;
    public bool DestroyOnPickup => destroyOnPickup;
    public string PersistentId => persistentWorldObjectId != null ? persistentWorldObjectId.PersistentId : string.Empty;
    public bool HasPersistentId => persistentWorldObjectId != null && persistentWorldObjectId.HasId;

    protected virtual void Awake()
    {
        ResolveReferences(false);
    }

    protected virtual void OnEnable()
    {
        CollectedWorldObjectState.Changed += HandleCollectedStateChanged;
        RefreshCollectedState();
    }

    protected virtual void Start()
    {
        RefreshCollectedState();
    }

    protected virtual void OnDisable()
    {
        CollectedWorldObjectState.Changed -= HandleCollectedStateChanged;
    }

#if UNITY_EDITOR
    protected virtual void Reset()
    {
        ResolveReferences(true);
    }

    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences(true);
    }
#endif

    public virtual bool TryInteract(PlayerInteractionContext context)
    {
        return TryPickup(context.Inventory);
    }

    public void Pickup(PlayerInventory inventory)
    {
        TryPickup(inventory);
    }

    public virtual bool TryPickup(PlayerInventory inventory = null)
    {
        if (item == null)
        {
            Debug.LogWarning($"[InventoryItemPickup] Item is not assigned on '{name}'.", this);
            return false;
        }

        if (inventory == null)
            inventory = ResolveInventory();

        if (inventory == null)
        {
            Debug.LogWarning($"[InventoryItemPickup] PlayerInventory was not found while picking '{name}'.", this);
            return false;
        }

        if (!inventory.TryAdd(item))
            return false;

        if (HasPersistentId)
            CollectedWorldObjectState.MarkCollected(PersistentId);

        OnPickedUp();
        return true;
    }

    protected virtual void OnPickedUp()
    {
        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    protected virtual PlayerInventory ResolveInventory()
    {
        if (GameManager.inventory != null)
            return GameManager.inventory;

        if (GameManager.player != null)
        {
            PlayerInventory fromPlayer = GameManager.player.GetInventory();
            if (fromPlayer != null)
                return fromPlayer;

            fromPlayer = GameManager.player.GetComponent<PlayerInventory>();
            if (fromPlayer != null)
                return fromPlayer;
        }

        return FindObjectOfType<PlayerInventory>();
    }

    private void ResolveReferences(bool ensurePersistentIdComponent)
    {
        if (persistentWorldObjectId == null)
            persistentWorldObjectId = GetComponent<PersistentWorldObjectId>();

#if UNITY_EDITOR
        if (ensurePersistentIdComponent && persistentWorldObjectId == null)
            persistentWorldObjectId = gameObject.AddComponent<PersistentWorldObjectId>();
#endif
    }

    private void HandleCollectedStateChanged()
    {
        RefreshCollectedState();
    }

    private void RefreshCollectedState()
    {
        if (!Application.isPlaying || !HasPersistentId)
            return;

        if (!CollectedWorldObjectState.IsCollected(PersistentId))
            return;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
