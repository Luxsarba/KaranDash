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
                HandleAmmoCrate();
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
        float maxDistance = interactRange;

        if (!RaycastService.TryRaycast(ray, out RaycastHit hit, maxDistance))
            return;

        float distanceToHit = Vector3.Distance(hit.point, transform.position);

        if (memoryGameRunning)
        {
            if (distanceToHit > interactRange)
                return;

            if (RaycastService.TryGetComponentInParents(hit, out MemoryButton memoryButton))
                HandleMemoryButton(memoryButton);

            return;
        }

        if (distanceToHit > interactRange)
            return;

        PlayerInteractionContext interactionContext = CreateInteractionContext(hit);
        if (RaycastService.TryGetInterfaceInParents(hit, out IPlayerInteractable interactable) &&
            interactable.TryInteract(interactionContext))
        {
            return;
        }

        if (RaycastService.TryGetComponentInParents(hit, out SaveStation _))
            HandleSaveStation(hit);
        else if (RaycastService.TryGetComponentInParents(hit, out PianoKey pianoKey))
            HandlePianoKey(pianoKey);
        else if (RaycastService.TryGetComponentInParents(hit, out FifteenPuzzleTile puzzleTile))
            HandleFifteenPuzzleTile(puzzleTile);
        else if (RaycastService.TryGetComponentInParents(hit, out QuestItemPickup _))
            HandleQuestItem(hit);
        else if (RaycastService.TryGetComponentInParents(hit, out DialogueTrigger _))
            HandleDialogNPC(hit);
        else if (RaycastService.TryGetComponentInParents(hit, out NoteTrigger _))
            HandleNote(hit);
        else if (RaycastService.TryGetComponentInParents(hit, out LeverSwitch leverSwitch))
            HandleLever(leverSwitch);
        else if (RaycastService.TryGetComponentInParents(hit, out FetchQuestNPC _))
            HandleQuestNPC(hit);
        else if (RaycastService.HitHasTag(hit, "Radio"))
            HandleRadio(hit);
        else if (RaycastService.TryGetComponentInParents(hit, out MemoryGameTrigger _))
            HandleMemoryGame(hit);
    }

    private void HandleAmmoCrate()
    {
        if (GameManager.currentAmmo < 10)
        {
            GameManager.currentAmmo = Mathf.Min(GameManager.currentAmmo + 2, 10);
            if (ammoText != null)
                ammoText.text = GameManager.currentAmmo.ToString();
        }
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

    private void HandleSaveStation(RaycastHit hit)
    {
        var station = hit.collider.GetComponentInParent<SaveStation>();
        if (station != null)
            station.SaveHere(GetComponent<Player>());
    }

    private void HandleQuestItem(RaycastHit hit)
    {
        var pickup = hit.collider.GetComponentInParent<QuestItemPickup>();
        pickup?.TryPickup(inventory);
    }

    private void HandleDialogNPC(RaycastHit hit)
    {
        var trigger = hit.collider.GetComponentInParent<DialogueTrigger>();
        trigger?.TryTriggerDialogue();
    }

    private void HandleNote(RaycastHit hit)
    {
        var trigger = hit.collider.GetComponentInParent<NoteTrigger>();
        trigger?.TryTriggerNote();
    }

    private void HandleLever(LeverSwitch leverSwitch)
    {
        leverSwitch?.TryActivate();
    }

    private void HandleQuestNPC(RaycastHit hit)
    {
        var npc = hit.collider.GetComponentInParent<FetchQuestNPC>();
        var resolvedInventory = inventory != null ? inventory : GetComponent<PlayerInventory>();
        if (npc && resolvedInventory)
            npc.Interact(resolvedInventory);
    }

    private void HandleRadio(RaycastHit hit)
    {
        var audio = hit.collider ? hit.collider.GetComponentInParent<AudioSource>() : null;
        if (audio != null)
        {
            if (audio.isPlaying)
                audio.Stop();
            else
                audio.Play();
        }
    }

    private void HandleMemoryGame(RaycastHit hit)
    {
        var trigger = hit.collider.GetComponentInParent<MemoryGameTrigger>();
        trigger?.TriggerGame();
    }

    private void HandleMemoryButton(MemoryButton button)
    {
        button?.TryPressFromInteraction();
    }

    private void HandlePianoKey(PianoKey key)
    {
        key?.TryPressFromInteraction();
    }

    private void HandleFifteenPuzzleTile(FifteenPuzzleTile tile)
    {
        tile?.TryPressFromInteraction();
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
