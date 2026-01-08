using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("=== КАМЕРЫ / ВЗГЛЯД ===")]
    public Camera playerCamera;
    public Camera additionalCamera;
    public GameObject playerHead;


    [Space(10)]
    [Header("=== ЗДОРОВЬЕ ===")]
    public float playerHP = 100f;
    public TextMeshProUGUI textHP;
    public HealthBar healtBar;


    [Space(10)]
    [Header("=== UI / ЭКРАНЫ ===")]
    [SerializeField] private GameObject playerUI;
    public TextMeshProUGUI ammoText;

    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [SerializeField] private TextMeshProUGUI helpCheckText;


    [Space(10)]
    [Header("=== ПРЫЖОК ===")]
    [Range(0.1f, 100f)]
    public float jumpLimit = 35f;

    [Range(0f, 0.5f)]
    public float coyoteTime = 0.2f;

    [Space(10)]
    [Header("Скорость")]
    public float speedMultiplier = 1f; // модификатор скорости (1 = норма)

    [Space(10)]
    [Header("=== АНИМАЦИИ ===")]
    [SerializeField] private Animator animator;


    // ===== ВНУТРЕННЕЕ =====
    private float coyoteTimer = 0.5f;

    private bool paused = false;
    private bool zoomed = false;
    private bool hasMedkits = false;

    private float rotationX;
    private float rotationY;

    private int medkits = 0;

    private Rigidbody rb;
    private Fire fire;

    private Ray ray;
    private RaycastHit hit;

    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool getPaused()
    {
        return paused;
    }

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Defeat()
    {
        Time.timeScale = 0;
        GameManager.DisablePlayerInput();
        playerUI.SetActive(false);
        loseScreen.SetActive(true);
        ShowCursor();
    }

    public void Damage(float dmg)
    {
        playerHP -= dmg;
        textHP.text = playerHP.ToString();

        healtBar.SetHealth((int)playerHP, 100);

        if (playerHP <= 0) {
            Defeat();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var tag = other.tag;
        switch (tag)
        {
            case "AmmoCrate":
                if (GameManager.currentAmmo < 10)
                {
                GameManager.currentAmmo = GameManager.currentAmmo + 2 > 10 ? 10: GameManager.currentAmmo + 2;
                ammoText.text = GameManager.currentAmmo.ToString();
                Destroy(other.gameObject);
                }
                break;
            case "QuestTrigger":
                Destroy(other.gameObject);
                break;
            case "Jumper":
                other.GetComponent<Jumper>().EnableField(0.1f);
                break;
            case "Death":
                Defeat();
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        var tag = other.tag;

        switch (tag)
        {
            case "MedKit":
                if (Input.GetKey(KeyCode.E) && !hasMedkits)
                {
                    other.transform.GetChild(1).transform.gameObject.SetActive(false);
                    hasMedkits = true;
                    medkits += 3;
                }
                break;
            case "End":
                Time.timeScale = 0;
                GameManager.DisablePlayerInput();
                playerUI.SetActive(false);
                winScreen.SetActive(true);
                ShowCursor();
                break;
            case "UpWind":
                //Debug.Log(other.transform.up * 20f);
                rb.velocity = new Vector3(0f, 0f, 0f);
                rb.AddForce(other.transform.up * 1250f, ForceMode.Impulse); //rb.AddForce(other.transform.up * 400000f);
                if (!animator.GetBool("IsJumping"))
                {
                    animator.Play("Jump");
                    animator.SetBool("IsJumping", true);
                }

                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var tag = collision.gameObject.tag;

        switch (tag)
        {
            case "EnemyAmmo":
                Damage(5);
                break;
        }
    }

    public void Continue()
    {
        paused = false;
        Time.timeScale = 1;
        playerUI.SetActive(true);
        pauseScreen.SetActive(false);
        GameManager.EnablePlayerInput();
        HideCursor();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0;
        playerUI.SetActive(false);
        pauseScreen.SetActive(true);
        GameManager.DisablePlayerInput();
        ShowCursor();
    }

    private void Awake()
    {
        GameManager.player = this;
    }

    void Start()
    {
        HideCursor();
        GameManager.EnablePlayerInput();
        Debug.Log($"player: {this}");
        GameManager.player = this;
        fire = transform.GetComponent<Fire>();
        rb = GetComponent<Rigidbody>();

        GameManager.currentAmmo = 10;
        GameManager.maxAmmo = 10;
        
        Pause();
        pauseScreen.SetActive(false);
    }


    void FixedUpdate()
    {
        Vector3 movement = new Vector3(0, rb.velocity.y - 0.5f, 0);

        if ((Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)))
        {
            
            if (Input.GetKey(KeyCode.LeftShift))
                movement += transform.forward * (Settings.speedPlayer * speedMultiplier * 2f);
            else movement += transform.forward * Settings.speedPlayer * speedMultiplier;
            if (Input.GetKey(KeyCode.LeftControl))
                movement += transform.forward * (Settings.speedPlayer * speedMultiplier / 8f);
            else movement += transform.forward * Settings.speedPlayer * speedMultiplier;
        }
        if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement -= transform.forward * (Settings.speedPlayer * speedMultiplier * 2f);
            else movement -= transform.forward * Settings.speedPlayer * speedMultiplier;
            if (Input.GetKey(KeyCode.LeftControl))
                movement -= transform.forward * (Settings.speedPlayer * speedMultiplier / 8f);
            else movement -= transform.forward * Settings.speedPlayer * speedMultiplier;
        }
        if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement -= transform.right * (Settings.speedPlayer * speedMultiplier * 2f);
            else movement -= transform.right * Settings.speedPlayer * speedMultiplier;
            if (Input.GetKey(KeyCode.LeftControl))
                movement -= transform.right * (Settings.speedPlayer * speedMultiplier / 8f);
            else movement -= transform.right * Settings.speedPlayer * speedMultiplier;
        }
        if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement += transform.right * (Settings.speedPlayer * speedMultiplier * 2f);
            else movement += transform.right * Settings.speedPlayer * speedMultiplier;
            if (Input.GetKey(KeyCode.LeftControl))
                movement += transform.right * (Settings.speedPlayer * speedMultiplier / 8f);
            else movement += transform.right * Settings.speedPlayer * speedMultiplier;
        }
        rb.velocity = movement;

        Debug.DrawRay(transform.position, rb.velocity);
    }

    void Update()
    {
        /* прицел */
        if (Input.GetMouseButtonDown(1))
        {
            if (!zoomed)
            {
                playerCamera.fieldOfView = 25f;
                additionalCamera.fieldOfView = 25f;
            }
            else
            {
                playerCamera.fieldOfView = 60;
                additionalCamera.fieldOfView = 60;
            }
            zoomed = !zoomed;
        }
        
        /* анимация бега */
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) 
        {
            animator.SetInteger("Run", 5);
            animator.SetBool("IsRunning", true);
        }
        else if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
        {
            animator.SetInteger("Run", 6);
            animator.SetBool("IsRunning", true);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            animator.SetBool("IsRunning", true);
            animator.SetInteger("Run", 1);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            animator.SetInteger("Run", 4);
            animator.SetBool("IsRunning", true);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            animator.SetInteger("Run", 3);
            animator.SetBool("IsRunning", true);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            animator.SetInteger("Run", 2);
            animator.SetBool("IsRunning", true);
        }
        else
        {
            animator.SetBool("IsRunning", false);
            animator.SetInteger("Run", 0);
        }


        /* Прыжок через кайот таймер */
           
        RaycastHit hit;

        if (Physics.Raycast(new Ray(transform.position + Vector3.up * 0.1f, Vector3.down), out hit, 1.2f)
                                            && Vector3.Distance(transform.position, hit.point) <= 1.1f)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (Physics.Raycast(new Ray(transform.position + Vector3.up * 0.1f, Vector3.down), out hit, 1.2f)
            && Vector3.Distance(transform.position, hit.point) < 1.05f && animator.GetBool("IsJumping"))
        {
            animator.SetBool("IsJumping", false);
        }

        /* Кайот прыжок (убрать двойной прыжок через тег) */
        if (Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0)
        {

            animator.SetBool("IsJumping", true);
            animator.CrossFadeInFixedTime("Jump", 0.1f);
            rb.AddForce(transform.up * (Settings.jumpForcePlayer * 500f));
            coyoteTimer = -1f;
            coyoteTimer = 0;
        }

        if (!GameManager.isPlayerInputBlocked)
        {
            if (rb.velocity.y > jumpLimit) rb.velocity = new Vector3(rb.velocity.x, jumpLimit, rb.velocity.z);
            rotationX -= Input.GetAxis("Mouse Y") * Settings.sensitivityVert;
            rotationX = Mathf.Clamp(rotationX, Settings.minVert, Settings.maxVert);
            rotationY += Input.GetAxis("Mouse X") * Settings.sensitivityHor;

            transform.localEulerAngles = new Vector3(0, rotationY, 0);
            playerHead.transform.localEulerAngles = new Vector3(rotationX, 0, 0);
        }

        /* Cистема взаимодействий */
        ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (Input.GetKeyDown(KeyCode.E) &&
            !GameManager.isPlayerInputBlocked &&
            Physics.Raycast(ray, out hit) &&
            Vector3.Distance(hit.point, transform.position) < 2f)
        {
            var tag = hit.transform.tag;
            switch (tag)
            {
                case "SaveStation":
                    {
                        var station = hit.collider.GetComponentInParent<SaveStation>();
                        if (station != null)
                        {
                            Debug.Log("Сработалооооо");
                            station.SaveHere(this);
                        }
                        break;
                    }
                case "QuestItem":
                    {
                        var pickup = hit.collider.GetComponentInParent<QuestItemPickup>();
                        var inv = GetComponent<PlayerInventory>();
                        if (pickup && inv)
                            pickup.Pickup(inv);
                        break;
                    }
                case "DialogNPC":
                    {
                        var trigger = hit.collider.GetComponentInParent<DialogueTrigger>();
                        if (trigger) trigger.TriggerDialogue();
                        break;
                    }
                case "QuestNPC":
                    {
                        var npc = hit.collider.GetComponentInParent<FetchQuestNPC>();
                        var inv = GetComponent<PlayerInventory>();
                        if (npc && inv) npc.Interact(inv);
                        break;
                    }
                case "Radio":
                    var audio = hit.transform.GetComponent<AudioSource>();
                    if (audio.isPlaying)audio.Stop();
                    else audio.Play();
                    break;
            }
        }

        /* Пауза */
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!paused)
            {
                Pause();
            }
            else
            {
                Continue();
            }
        }
    }
}