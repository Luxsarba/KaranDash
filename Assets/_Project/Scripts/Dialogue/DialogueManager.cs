using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

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
    }

    public void StartDialogue(Dialogue dialogue)
    {
        IsOpen = true;
        OverlayModalController.Show(dialoguePanel);

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
        OverlayModalController.Hide(dialoguePanel);
    }

    public static bool IsFireInputBlockedByDialogue()
    {
        return OverlayModalController.IsPrimaryActionBlocked();
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
}
