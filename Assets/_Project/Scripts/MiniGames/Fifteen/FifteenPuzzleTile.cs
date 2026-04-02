using UnityEngine;
using System.Collections;

/// <summary>
/// Single tile in the fifteen puzzle.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FifteenPuzzleTile : MonoBehaviour, IPlayerInteractable
{
    [Header("Tile")]
    [SerializeField] private int tileValue = 1;
    [SerializeField] private Vector2Int solvedCell = new Vector2Int(-1, -1);

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.7f, 1f, 0.8f);
    [SerializeField, Min(0.03f)] private float flashDuration = 0.12f;
    [SerializeField] private bool autoColorByValue = true;

    [Header("Optional")]
    [SerializeField] private bool allowMouseClick;
    [SerializeField, Min(0.01f)] private float moveAnimationDuration = 0.06f;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexStPropertyId = Shader.PropertyToID("_MainTex_ST");
    private static readonly int BaseMapStPropertyId = Shader.PropertyToID("_BaseMap_ST");

    private FifteenPuzzlePanel _panel;
    private MaterialPropertyBlock _propertyBlock;
    private Vector2Int _currentCell = new Vector2Int(-1, -1);
    private Texture2D _sliceTexture;
    private Vector2 _sliceScale = Vector2.one;
    private Vector2 _sliceOffset = Vector2.zero;
    private Coroutine _moveCoroutine;
    private Collider _cachedCollider;
    private bool _interactionEnabled = true;

    public int TileValue => tileValue;
    public Vector2Int SolvedCell => solvedCell;
    public Vector2Int CurrentCell => _currentCell;
    public bool IsMoveAnimating => _moveCoroutine != null;
    public bool TryInteract(PlayerInteractionContext context)
    {
        return TryPressFromInteraction();
    }


    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        _cachedCollider = GetComponent<Collider>();

        UpdateAutoColors();
        ApplyColor(ResolveNormalColor());
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ResetColor));

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    public void Initialize(FifteenPuzzlePanel panel, Vector2Int effectiveSolvedCell)
    {
        _panel = panel;
        solvedCell = effectiveSolvedCell;
        _currentCell = effectiveSolvedCell;
        UpdateAutoColors();
        ApplyColor(ResolveNormalColor());
    }

    public void SetCurrentCell(Vector2Int cell, Vector3 localPosition)
    {
        _currentCell = cell;

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        transform.localPosition = localPosition;
    }

    public void AnimateToCell(Vector2Int cell, Vector3 localPosition)
    {
        _currentCell = cell;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(AnimateMoveCoroutine(localPosition));
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        _interactionEnabled = isEnabled;
    }

    public bool TryPressFromInteraction()
    {
        if (!_interactionEnabled)
            return false;

        Flash();
        return _panel != null && _panel.TryInteractWithTile(this);
    }

    public void SetSolvedCell(Vector2Int cell)
    {
        solvedCell = cell;
    }

    public void ConfigureImageSlice(Texture2D texture, int gridSize, bool mirrorHorizontally)
    {
        _sliceTexture = null;
        _sliceScale = Vector2.one;
        _sliceOffset = Vector2.zero;

        if (texture != null && gridSize > 0)
        {
            float inv = 1f / gridSize;
            _sliceTexture = texture;
            _sliceScale = new Vector2(inv, inv);
            int xCell = mirrorHorizontally ? (gridSize - 1 - solvedCell.x) : solvedCell.x;
            _sliceOffset = new Vector2(xCell * inv, 1f - (solvedCell.y + 1) * inv);
        }

        ApplyColor(ResolveNormalColor());
    }

    private void Flash()
    {
        CancelInvoke(nameof(ResetColor));
        ApplyColor(ResolveActiveColor());
        Invoke(nameof(ResetColor), Mathf.Max(0.03f, flashDuration));
    }

    private void ResetColor()
    {
        ApplyColor(ResolveNormalColor());
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null)
            return;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(ColorPropertyId, color);
        _propertyBlock.SetColor(BaseColorPropertyId, color);

        if (_sliceTexture != null)
        {
            _propertyBlock.SetTexture(MainTexPropertyId, _sliceTexture);
            _propertyBlock.SetTexture(BaseMapPropertyId, _sliceTexture);
            _propertyBlock.SetVector(MainTexStPropertyId, new Vector4(_sliceScale.x, _sliceScale.y, _sliceOffset.x, _sliceOffset.y));
            _propertyBlock.SetVector(BaseMapStPropertyId, new Vector4(_sliceScale.x, _sliceScale.y, _sliceOffset.x, _sliceOffset.y));
        }

        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    private Color ResolveNormalColor()
    {
        return _sliceTexture != null ? Color.white : normalColor;
    }

    private Color ResolveActiveColor()
    {
        return _sliceTexture != null ? new Color(1f, 1f, 0.85f, 1f) : activeColor;
    }

    private void UpdateAutoColors()
    {
        if (!autoColorByValue)
            return;

        int wrapped = Mathf.Max(1, tileValue) % 16;
        float hue = wrapped / 16f;
        normalColor = Color.HSVToRGB(hue, 0.35f, 0.95f);
        activeColor = Color.HSVToRGB(hue, 0.2f, 1f);
    }

    private void OnMouseDown()
    {
        if (!allowMouseClick)
            return;

        TryPressFromInteraction();
    }

    private IEnumerator AnimateMoveCoroutine(Vector3 targetLocalPosition)
    {
        Vector3 start = transform.localPosition;
        float duration = Mathf.Max(0.01f, moveAnimationDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            transform.localPosition = Vector3.Lerp(start, targetLocalPosition, alpha);
            yield return null;
        }

        transform.localPosition = targetLocalPosition;
        _moveCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateAutoColors();
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        ApplyColor(ResolveNormalColor());
    }
#endif
}
