using UnityEngine;

/// <summary>
/// Взаимодействия игрока (предметы, NPC, триггеры).
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Параметры")]
    [SerializeField] private float interactRange = 2f;

    [Header("Ссылки")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerPause playerPause;
    [SerializeField] private PlayerInventory inventory;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI ammoText;

    private void OnTriggerEnter(Collider other)
    {
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
        if (Input.GetKeyDown(KeyCode.E) &&
            playerPause != null && !playerPause.IsPaused &&
            !GameManager.isPlayerInputBlocked)
        {
            HandleInteraction();
        }
    }

    private void HandleInteraction()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (RaycastService.TryRaycast(ray, out RaycastHit hit, interactRange))
        {
            if (Vector3.Distance(hit.point, transform.position) >= interactRange)
                return;

            if (RaycastService.TryGetComponentInParents(hit, out SaveStation _))
                HandleSaveStation(hit);
            else if (RaycastService.TryGetComponentInParents(hit, out QuestItemPickup _))
                HandleQuestItem(hit);
            else if (RaycastService.TryGetComponentInParents(hit, out DialogueTrigger _))
                HandleDialogNPC(hit);
            else if (RaycastService.TryGetComponentInParents(hit, out FetchQuestNPC _))
                HandleQuestNPC(hit);
            else if (RaycastService.HitHasTag(hit, "Radio"))
                HandleRadio(hit);
            else if (RaycastService.TryGetComponentInParents(hit, out MemoryGameTrigger _))
                HandleMemoryGame(hit);
        }
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
        {
            // TODO: добавить флаг hasMedkits в PlayerHealth или отдельный компонент
            other.transform.GetChild(1).gameObject.SetActive(false);
            // medkits += 3; // TODO: реализовать систему аптечек
        }
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
        {
            Debug.Log("Сработалооооо");
            station.SaveHere(GetComponent<Player>());
        }
    }

    private void HandleQuestItem(RaycastHit hit)
    {
        var pickup = hit.collider.GetComponentInParent<QuestItemPickup>();
        if (pickup && inventory)
            pickup.Pickup(inventory);
    }

    private void HandleDialogNPC(RaycastHit hit)
    {
        var trigger = hit.collider.GetComponentInParent<DialogueTrigger>();
        trigger?.TriggerDialogue();
    }

    private void HandleQuestNPC(RaycastHit hit)
    {
        var npc = hit.collider.GetComponentInParent<FetchQuestNPC>();
        if (npc && inventory)
            npc.Interact(inventory);
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
}
