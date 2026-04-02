using UnityEngine;

/// <summary>
/// Trigger entry point for the memory mini-game.
/// </summary>
public class MemoryGameTrigger : MonoBehaviour, IPlayerInteractable
{
    [Header("References")]
    [SerializeField] private MemoryPanel memoryPanel;

    private void Awake()
    {
        TryResolvePanel();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            TryResolvePanel();
    }
#endif

    public bool TryInteract(PlayerInteractionContext context)
    {
        if (!TryResolvePanel())
        {
            Debug.LogWarning("[MemoryGameTrigger] MemoryPanel was not found.", this);
            return false;
        }

        memoryPanel.StartGame();
        return true;
    }

    public void TriggerGame()
    {
        TryInteract(default);
    }

    private bool TryResolvePanel()
    {
        if (memoryPanel != null)
            return true;

        memoryPanel = GetComponentInParent<MemoryPanel>(true);
        if (memoryPanel != null)
            return true;

        memoryPanel = GetComponentInChildren<MemoryPanel>(true);
        if (memoryPanel != null)
            return true;

        memoryPanel = FindObjectOfType<MemoryPanel>(true);
        return memoryPanel != null;
    }
}