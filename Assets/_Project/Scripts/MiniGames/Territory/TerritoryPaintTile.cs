using UnityEngine;

/// <summary>
/// Single tile for territory paint mini-game.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TerritoryPaintTile : MonoBehaviour
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Renderer targetRenderer;

    private MaterialPropertyBlock _propertyBlock;

    public Vector2Int Cell { get; private set; }
    public int CurrentOwnerId { get; private set; }

    public void Initialize(Vector2Int cell, Color neutralColor, Vector3 localPosition)
    {
        Cell = cell;
        transform.localPosition = localPosition;
        EnsureRenderer();
        ResetOwner(neutralColor);
    }

    public void SetOwner(int ownerId, Color ownerColor)
    {
        CurrentOwnerId = Mathf.Max(0, ownerId);
        ApplyColor(ownerColor);
    }

    public void ResetOwner(Color neutralColor)
    {
        CurrentOwnerId = 0;
        ApplyColor(neutralColor);
    }

    private void EnsureRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void ApplyColor(Color color)
    {
        EnsureRenderer();
        if (targetRenderer == null)
            return;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(ColorPropertyId, color);
        _propertyBlock.SetColor(BaseColorPropertyId, color);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }
}
