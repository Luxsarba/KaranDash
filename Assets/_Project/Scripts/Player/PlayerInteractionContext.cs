using UnityEngine;

public readonly struct PlayerInteractionContext
{
    public PlayerInteractionContext(PlayerInteraction interaction, Player player, PlayerInventory inventory, RaycastHit hit)
    {
        Interaction = interaction;
        Player = player;
        Inventory = inventory;
        Hit = hit;
    }

    public PlayerInteraction Interaction { get; }
    public Player Player { get; }
    public PlayerInventory Inventory { get; }
    public RaycastHit Hit { get; }

    public Transform InteractorTransform => Player != null ? Player.transform : null;
    public GameObject InteractorObject => InteractorTransform != null ? InteractorTransform.gameObject : null;
}
