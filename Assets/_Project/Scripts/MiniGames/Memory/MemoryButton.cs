using UnityEngine;

/// <summary>
/// Interactive button for MemoryPanel.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MemoryButton : MonoBehaviour, IPlayerInteractable
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("Optional")]
    [SerializeField] private Light buttonLight;
    [SerializeField] private Animator animator;
    [SerializeField] private string activateTrigger = "Activate";
    [SerializeField] private bool allowMouseClick = false;

    private MemoryPanel _panel;
    private int _index;
    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private bool _isInitialized;

    public int ButtonIndex => _index;
    public bool TryInteract(PlayerInteractionContext context)
    {
        return TryPressFromInteraction();
    }


    private void Awake()
    {
        EnsureRenderer();

        if (buttonLight == null)
            buttonLight = GetComponentInChildren<Light>();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ResetColor));
    }

    public void Initialize(MemoryPanel panel, int index)
    {
        _panel = panel;
        _index = index;
        _isInitialized = true;
        SetNormal();
    }

    public bool IsOwnedBy(MemoryPanel panel)
    {
        return panel != null && _panel == panel;
    }

    public bool TryPressFromInteraction()
    {
        if (!_isInitialized || _panel == null)
            return false;

        return _panel.TryInteractWithButton(this);
    }

    public void Activate()
    {
        Activate(0.3f);
    }

    public void Activate(float duration)
    {
        if (!_isInitialized)
            return;

        CancelInvoke(nameof(ResetColor));
        SetColor(activeColor);

        if (buttonLight != null)
        {
            buttonLight.color = activeColor;
            buttonLight.enabled = true;
        }

        if (animator != null && !string.IsNullOrEmpty(activateTrigger))
            animator.SetTrigger(activateTrigger);

        Invoke(nameof(ResetColor), Mathf.Max(0.05f, duration));
    }

    public void SetNormal()
    {
        CancelInvoke(nameof(ResetColor));
        ResetColor();
    }

    public void SetSuccess()
    {
        SetColor(successColor);

        if (buttonLight != null)
        {
            buttonLight.color = successColor;
            buttonLight.enabled = true;
        }
    }

    public void SetFail()
    {
        SetColor(failColor);

        if (buttonLight != null)
        {
            buttonLight.color = failColor;
            buttonLight.enabled = true;
        }
    }

    private void ResetColor()
    {
        if (!_isInitialized)
            return;

        SetColor(normalColor);

        if (buttonLight != null)
            buttonLight.enabled = false;
    }

    private void SetColor(Color color)
    {
        EnsureRenderer();
        if (_renderer == null)
            return;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(ColorPropertyId, color);
        _propertyBlock.SetColor(BaseColorPropertyId, color);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private void EnsureRenderer()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_renderer == null)
            _renderer = GetComponentInChildren<Renderer>();
    }

    private void OnMouseDown()
    {
        if (!allowMouseClick)
            return;

        if (_isInitialized && _panel != null)
            _panel.OnButtonPressed(_index);
    }
}
