using UnityEngine;

/// <summary>
/// Player interactions (items, NPCs, triggers).
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerPause playerPause;
    [SerializeField] private PlayerInventory inventory;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI ammoText;

    private Player _player;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        ResolveReferences();

        switch (other.tag)
        {
            case "AmmoCrate":
                if (HandleAmmoCrate())
                    Destroy(other.gameObject);
                break;
            case "QuestTrigger":
                Destroy(other.gameObject);
                break;
            case "Jumper":
                other.GetComponent<Jumper>().EnableField(0.1f);
                break;
            case "Death":
                if (playerPause != null)
                    playerPause.ShowLoseScreen();
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        ResolveReferences();

        switch (other.tag)
        {
            case "MedKit":
                HandleMedKit(other);
                break;
            case "End":
                if (playerPause != null)
                    playerPause.ShowWinScreen();
                break;
            case "UpWind":
                HandleUpWind(other);
                break;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        ResolveReferences();

        bool allowMemoryInteraction = MemoryPanel.IsInteractionInputAllowed();

        if (GameManager.isPlayerInputBlocked && !allowMemoryInteraction)
            return;

        if (playerPause != null && playerPause.IsPaused && !allowMemoryInteraction)
            return;

        HandleInteraction();
    }

    private void HandleInteraction()
    {
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool memoryGameRunning = MemoryPanel.IsAnyGameRunning();
        if (!RaycastService.TryRaycast(ray, out RaycastHit hit, interactRange))
            return;

        if (Vector3.Distance(hit.point, transform.position) > interactRange)
            return;

        if (!RaycastService.TryGetInterfaceInParents(hit, out IPlayerInteractable interactable))
            return;

        if (memoryGameRunning)
        {
            if (interactable is not MemoryButton)
                return;
        }
        else if (interactable is MemoryButton)
        {
            if (RaycastService.TryGetInterfaceInParents(hit, out MemoryGameTrigger memoryTrigger))
            {
                memoryTrigger.TryInteract(CreateInteractionContext(hit));
            }

            return;
        }

        interactable.TryInteract(CreateInteractionContext(hit));
    }

    private bool HandleAmmoCrate()
    {
        if (GameManager.infiniteAmmo)
            return false;

        if (GameManager.currentAmmo >= 10)
            return false;

        GameManager.currentAmmo = Mathf.Min(GameManager.currentAmmo + 2, 10);
        if (ammoText != null)
            ammoText.text = GameManager.currentAmmo.ToString();

        return true;
    }

    private void HandleMedKit(Collider other)
    {
        if (Input.GetKey(KeyCode.E))
            other.transform.GetChild(1).gameObject.SetActive(false);
    }

    private void HandleUpWind(Collider other)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.AddForce(other.transform.up * 1250f, ForceMode.Impulse);
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null && !animator.GetBool("IsJumping"))
        {
            animator.Play("Jump");
            animator.SetBool("IsJumping", true);
        }
    }

    private PlayerInteractionContext CreateInteractionContext(RaycastHit hit)
    {
        PlayerInventory resolvedInventory = inventory != null ? inventory : GetComponent<PlayerInventory>();
        Player resolvedPlayer = _player != null ? _player : GetComponent<Player>();
        return new PlayerInteractionContext(this, resolvedPlayer, resolvedInventory, hit);
    }

    private void ResolveReferences()
    {
        if (_player == null)
            _player = GetComponent<Player>();

        if (playerPause == null)
            playerPause = GetComponent<PlayerPause>();

        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (playerCamera == null)
        {
            if (_player != null && _player.playerCamera != null)
                playerCamera = _player.playerCamera;
            else
                playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (ammoText == null && _player != null)
            ammoText = _player.ammoText;
    }
}
