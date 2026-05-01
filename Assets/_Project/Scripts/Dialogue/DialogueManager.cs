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
    private readonly HashSet<int> invokedSentenceEvents = new HashSet<int>();
    private Dialogue activeDialogue;
    private System.Action activeCompletedCallback;
    private int nextSentenceIndex;
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
        StartDialogue(dialogue, null);
    }

    public void StartDialogue(Dialogue dialogue, System.Action onCompleted)
    {
        if (dialogue == null)
        {
            Debug.LogWarning("[DialogueManager] Tried to start a null dialogue.", this);
            return;
        }

        IsOpen = true;
        activeDialogue = dialogue;
        activeCompletedCallback = onCompleted;
        nextSentenceIndex = 0;
        invokedSentenceEvents.Clear();
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
        if (dialogue.sentences != null)
        {
            foreach (var s in dialogue.sentences)
                sentences.Enqueue(s);
        }

        DisplayNextSentence();
    }


    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue(true);
            return;
        }

        InvokeSentenceEvents(nextSentenceIndex);
        dialogueText.text = sentences.Dequeue();
        nextSentenceIndex++;
    }

    public void EndDialogue()
    {
        EndDialogue(false);
    }

    private void EndDialogue(bool completed)
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        OverlayModalController.Hide(dialoguePanel);

        if (completed)
        {
            activeDialogue?.onCompleted?.Invoke();
            activeCompletedCallback?.Invoke();
        }

        activeDialogue = null;
        activeCompletedCallback = null;
        nextSentenceIndex = 0;
        invokedSentenceEvents.Clear();
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

    private void InvokeSentenceEvents(int sentenceIndex)
    {
        if (activeDialogue?.sentenceEvents == null)
            return;

        for (int i = 0; i < activeDialogue.sentenceEvents.Length; i++)
        {
            DialogueSentenceEvent sentenceEvent = activeDialogue.sentenceEvents[i];
            if (sentenceEvent == null || sentenceEvent.sentenceIndex != sentenceIndex)
                continue;

            if (!invokedSentenceEvents.Add(i))
                continue;

            sentenceEvent.onReached?.Invoke();
        }
    }
}
