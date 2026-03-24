using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool BlockFireUntilMouseRelease { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI senderText;
    [SerializeField] private TextMeshProUGUI recipientText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitImage;

    private readonly Queue<string> sentences = new Queue<string>();
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
        IsOpen = false;
        BlockFireUntilMouseRelease = false;
    }

    public void StartDialogue(Dialogue dialogue)
    {
        EnterDialogueMode();

        IsOpen = true;
        BlockFireUntilMouseRelease = true;

        dialoguePanel.SetActive(true);

        senderText.text = $"{dialogue.name} <{dialogue.email}@mail.com>";

        recipientText.text = $"кому: {dialogue.recipientName}";

        dialogueText.text = "";

        if (portraitImage)
        {
            portraitImage.sprite = dialogue.portrait;
            portraitImage.enabled = dialogue.portrait != null;
        }

        sentences.Clear();
        foreach (var s in dialogue.sentences)
            sentences.Enqueue(s);

        DisplayNextSentence();
    }


    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        dialogueText.text = sentences.Dequeue();
    }

    public void EndDialogue()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        BlockFireUntilMouseRelease = true;

        dialoguePanel.SetActive(false);

        ExitDialogueMode();
    }

    public static bool IsFireInputBlockedByDialogue()
    {
        if (Instance != null && Instance.IsOpen)
            return true;

        if (!BlockFireUntilMouseRelease)
            return false;

        if (Input.GetMouseButton(0))
            return true;

        BlockFireUntilMouseRelease = false;
        return false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        //дальше
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            DisplayNextSentence();

        //выход
        if (Input.GetKeyDown(KeyCode.Escape))
            EndDialogue();
    }
    private void EnterDialogueMode()
    {
        GameManager.DisablePlayerInput();

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ExitDialogueMode()
    {
        var pause = ResolvePlayerPause();
        if (pause != null && pause.IsPaused)
        {
            GameManager.DisablePlayerInput();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        GameManager.EnablePlayerInput();
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static PlayerPause ResolvePlayerPause()
    {
        if (GameManager.player != null)
        {
            var fromPlayer = GameManager.player.GetPause();
            if (fromPlayer != null)
                return fromPlayer;

            fromPlayer = GameManager.player.GetComponent<PlayerPause>();
            if (fromPlayer != null)
                return fromPlayer;
        }

        return FindObjectOfType<PlayerPause>(true);
    }

}
