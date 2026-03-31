using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Collection Piece")]
public class CollectionPieceData : InventoryItemData
{
    [SerializeField] private CollectionSetData collectionSet;

    [SerializeField, Min(0)]
    private int pieceIndex;

    public CollectionSetData CollectionSet => collectionSet;
    public int PieceIndex => pieceIndex;

    private void Reset()
    {
        category = InventoryItemCategory.CollectionPiece;
    }

    private void OnValidate()
    {
        category = InventoryItemCategory.CollectionPiece;
    }
}
