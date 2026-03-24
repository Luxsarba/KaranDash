using UnityEngine;

/// <summary>
/// Кнопка для мини-игры Мемори.
/// Навешивается на каждый элемент панели (куб, панель, свет).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MemoryButton : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("Опционально: свет")]
    [SerializeField] private Light buttonLight;

    [Header("Опционально: анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string activateTrigger = "Activate";

    private MemoryPanel _panel;
    private int _index;
    private Renderer _renderer;
    private Material _originalMaterial;
    private bool _isInitialized;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (!_renderer) _renderer = GetComponentInChildren<Renderer>();
        
        if (buttonLight == null)
            buttonLight = GetComponentInChildren<Light>();
    }

    public void Initialize(MemoryPanel panel, int index)
    {
        _panel = panel;
        _index = index;
        _isInitialized = true;
        SetColor(normalColor);
    }

    public void Activate()
    {
        if (!_isInitialized) return;

        SetColor(activeColor);
        
        if (buttonLight != null)
            buttonLight.enabled = true;

        if (animator != null && !string.IsNullOrEmpty(activateTrigger))
            animator.SetTrigger(activateTrigger);

        // Возврат к нормальному цвету
        Invoke(nameof(ResetColor), 0.3f);
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
        if (!_isInitialized) return;
        SetColor(normalColor);

        if (buttonLight != null)
            buttonLight.enabled = false;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ResetColor));
    }

    private void SetColor(Color color)
    {
        if (_renderer != null)
        {
            _renderer.material.color = color;
        }
    }

    private void OnMouseDown()
    {
        if (_isInitialized && _panel != null)
        {
            _panel.OnButtonPressed(_index);
        }
    }

    private void Reset()
    {
        normalColor = new Color(0.3f, 0.3f, 0.3f);
        activeColor = Color.white;
        successColor = Color.green;
        failColor = Color.red;
    }
}
