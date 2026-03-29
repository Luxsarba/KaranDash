using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteViewer : MonoBehaviour
{
    public static NoteViewer Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject decorativeImage;
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, 155f);

    [Header("Placeholder")]
    [SerializeField] private string placeholderTitle = "Zametka";
    [SerializeField] private string placeholderSubtitle = "Story Placeholder";
    [SerializeField, TextArea(6, 20)] private string placeholderBody =
        "Zdes budet tekst zapisok i syuzhetnyh zametok. " +
        "Pozzhe syuda mozhno podstavlyat nujnyj kontent iz trigera ili questa.";
    [SerializeField] private float closeInputDelay = 0.1f;

    private float _inputUnlockTime;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoResolveReferences();
    }

    private void Start()
    {
        AutoResolveReferences();

        if (notePanel != null)
            notePanel.SetActive(false);

        IsOpen = false;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoResolveReferences();
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < _inputUnlockTime)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            CloseNote();
        }
    }

    [ContextMenu("Show Placeholder Note")]
    public void ShowPlaceholderNote()
    {
        OpenNote(placeholderTitle, placeholderSubtitle, placeholderBody, null);
    }

    public void OpenNote(string title, string subtitle, string body, Sprite backgroundSprite = null)
    {
        AutoResolveReferences();

        if (notePanel == null)
        {
            Debug.LogWarning("[NoteViewer] notePanel is not assigned.", this);
            return;
        }

        if (titleText != null)
            titleText.text = title;
        if (subtitleText != null)
            subtitleText.text = subtitle;
        if (bodyText != null)
            bodyText.text = body;
        if (backgroundImage != null && backgroundSprite != null)
            backgroundImage.sprite = backgroundSprite;

        IsOpen = true;
        _inputUnlockTime = Time.unscaledTime + closeInputDelay;

        OverlayModalController.Show(notePanel);
    }

    public void CloseNote()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        OverlayModalController.Hide(notePanel);
    }

    private void AutoResolveReferences()
    {
        if (notePanel == null)
        {
            var notePanelTransform = transform.Find("Note Panel");
            if (notePanelTransform == null)
                notePanelTransform = transform.Find("Dialogue Panel_Copy");

            if (notePanelTransform != null)
                notePanel = notePanelTransform.gameObject;
        }

        if (notePanel == null)
            return;

        var noteRect = notePanel.GetComponent<RectTransform>();
        if (noteRect != null)
        {
            noteRect.anchoredPosition = panelAnchoredPosition;
            noteRect.localScale = Vector3.one;
            noteRect.localRotation = Quaternion.identity;
        }

        if (titleText == null)
            titleText = FindText(notePanel.transform, "Sender");
        if (subtitleText == null)
            subtitleText = FindText(notePanel.transform, "Recipient");
        if (bodyText == null)
            bodyText = FindText(notePanel.transform, "Dialog Text");
        if (backgroundImage == null)
            backgroundImage = notePanel.GetComponent<Image>();

        if (decorativeImage == null)
        {
            var imageTransform = notePanel.transform.Find("Sender image");
            if (imageTransform != null)
                decorativeImage = imageTransform.gameObject;
        }

        if (decorativeImage != null && decorativeImage.activeSelf)
            decorativeImage.SetActive(false);
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
