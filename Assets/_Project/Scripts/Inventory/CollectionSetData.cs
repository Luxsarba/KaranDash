using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Collection Set")]
public class CollectionSetData : ScriptableObject
{
    public string collectionId;
    public string displayName;

    [Min(1)]
    public int pieceCount = 6;

    [Tooltip("Optional ordered piece list for UI/state queries.")]
    [SerializeField] private CollectionPieceData[] pieces;

    public CollectionPieceData[] Pieces => pieces;

    public int PieceCount
    {
        get
        {
            int count = Mathf.Max(pieceCount, 1);

            if (pieces == null)
                return count;

            count = Mathf.Max(count, pieces.Length);
            for (int i = 0; i < pieces.Length; i++)
            {
                CollectionPieceData piece = pieces[i];
                if (piece != null)
                    count = Mathf.Max(count, piece.PieceIndex + 1);
            }

            return count;
        }
    }
}
