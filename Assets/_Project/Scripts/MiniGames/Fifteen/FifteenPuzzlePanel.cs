using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sliding fifteen-puzzle board that is controlled through interaction input.
/// </summary>
public class FifteenPuzzlePanel : MonoBehaviour
{
    private static readonly int CoverMainTexPropertyId = Shader.PropertyToID("_MainTex");
    private static readonly int CoverBaseMapPropertyId = Shader.PropertyToID("_BaseMap");
    private static readonly int CoverMainTexStPropertyId = Shader.PropertyToID("_MainTex_ST");
    private static readonly int CoverBaseMapStPropertyId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int CoverColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int CoverBaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    [Header("Board")]
    [SerializeField, Min(2)] private int gridSize = 4;
    [SerializeField, Min(0.01f)] private float cellSize = 0.25f;
    [SerializeField] private Vector3 boardOriginLocal = Vector3.zero;
    [SerializeField] private bool centerBoard = true;
    [SerializeField] private bool autoShuffleOnEnable = true;
    [SerializeField, Range(10, 400)] private int shuffleMoves = 120;
    [SerializeField] private bool useImageSlices = true;
    [SerializeField] private Texture2D puzzleImage;
    [SerializeField] private bool mirrorImageSlicesHorizontally = true;
    [SerializeField] private GameObject successCover;
    [SerializeField] private bool lockInteractionOnSuccess = true;
    [SerializeField] private bool alignSuccessCoverToBlankCell = true;
    [SerializeField] private float successCoverYOffset = 0.02f;
    [SerializeField] private Renderer successCoverRenderer;
    [SerializeField, Min(0f)] private float successCoverDropHeight = 0.4f;
    [SerializeField, Min(0.01f)] private float successCoverDropDuration = 0.1f;

    [Header("References")]
    [SerializeField] private Transform tileContainer;
    [SerializeField] private FifteenPuzzleTile[] tiles;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;

    private readonly Dictionary<Vector2Int, FifteenPuzzleTile> _tilesByCell = new Dictionary<Vector2Int, FifteenPuzzleTile>();
    private readonly Dictionary<FifteenPuzzleTile, Vector2Int> _cellsByTile = new Dictionary<FifteenPuzzleTile, Vector2Int>();
    private readonly List<FifteenPuzzleTile> _neighborBuffer = new List<FifteenPuzzleTile>(4);

    private Vector2Int _blankCell;
    private bool _isInitialized;
    private bool _isSolved;
    private bool _isShuffling;
    private MaterialPropertyBlock _successCoverPropertyBlock;
    private Collider _successCoverCollider;
    private Coroutine _successCoverSequenceCoroutine;

    private void Awake()
    {
        InitializeBoard();
        ResolveSuccessCoverRenderer();
        ResolveSuccessCoverCollider();
        RefreshSuccessCoverVisual();
    }

    private void OnEnable()
    {
        InitializeBoard();
        ResolveSuccessCoverRenderer();
        ResolveSuccessCoverCollider();
        RefreshSuccessCoverVisual();

        if (autoShuffleOnEnable)
            Shuffle();
    }

    private void OnDisable()
    {
        StopSuccessCoverSequence();
    }

    public void StartGame()
    {
        Shuffle();
    }

    public void ResetGame()
    {
        PrepareBoardForRound();
    }

    public bool TryInteractWithTile(FifteenPuzzleTile tile)
    {
        if (tile == null || _isShuffling)
            return false;

        if (!_isInitialized && !InitializeBoard())
            return false;

        if (_isSolved)
            return false;

        if (!_cellsByTile.TryGetValue(tile, out Vector2Int tileCell))
            return false;

        if (!IsAdjacent(tileCell, _blankCell))
            return false;

        MoveTileIntoBlank(tile, tileCell, true);
        EvaluateSolvedState(tile);
        return true;
    }

    public void Shuffle()
    {
        if (!PrepareBoardForRound())
            return;

        _isShuffling = true;

        int moves = Mathf.Max(1, shuffleMoves);
        for (int i = 0; i < moves; i++)
            MoveRandomNeighborIntoBlank();

        if (IsSolvedWithoutEvents())
            MoveRandomNeighborIntoBlank();

        _isSolved = false;
        _isShuffling = false;
    }

    private bool PrepareBoardForRound()
    {
        StopSuccessCoverSequence();
        InitializeBoard();
        if (!_isInitialized)
            return false;

        PlaceTilesInSolvedOrder();
        _isSolved = false;
        SetTilesInteractable(true);
        UpdateSuccessCoverPose();
        RefreshSuccessCoverVisual();
        SetSuccessCoverActive(false);
        SetSuccessCoverColliderEnabled(false);
        return true;
    }

    private bool InitializeBoard()
    {
        if (tileContainer == null)
            tileContainer = transform;

        if (tiles == null || tiles.Length == 0)
            tiles = tileContainer.GetComponentsInChildren<FifteenPuzzleTile>(true);

        if (tiles == null || tiles.Length == 0)
        {
            _isInitialized = false;
            return false;
        }

        int boardCells = gridSize * gridSize;
        int expectedTileCount = boardCells - 1;
        if (tiles.Length > expectedTileCount)
        {
            Debug.LogWarning($"[FifteenPuzzlePanel] Too many tiles ({tiles.Length}). Expected at most {expectedTileCount} for {gridSize}x{gridSize}.", this);
        }

        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            if (tile == null)
                continue;

            Vector2Int solvedCell = ResolveSolvedCell(tile, i, usedCells);
            usedCells.Add(solvedCell);

            tile.Initialize(this, solvedCell);
            tile.ConfigureImageSlice(useImageSlices ? puzzleImage : null, gridSize, mirrorImageSlicesHorizontally);
        }

        PlaceTilesInSolvedOrder();
        UpdateSuccessCoverPose();
        _isSolved = false;
        _isInitialized = true;
        return true;
    }

    private Vector2Int ResolveSolvedCell(FifteenPuzzleTile tile, int fallbackIndex, HashSet<Vector2Int> usedCells)
    {
        if (IsCellInside(tile.SolvedCell) && tile.SolvedCell != BlankCellForCurrentGrid() && !usedCells.Contains(tile.SolvedCell))
            return tile.SolvedCell;

        int maxTileValue = gridSize * gridSize - 1;
        int byValue = Mathf.Clamp(tile.TileValue, 1, maxTileValue) - 1;
        Vector2Int candidate = new Vector2Int(byValue % gridSize, byValue / gridSize);

        if (candidate != BlankCellForCurrentGrid() && !usedCells.Contains(candidate))
            return candidate;

        for (int index = Mathf.Max(0, fallbackIndex); index < maxTileValue; index++)
        {
            candidate = new Vector2Int(index % gridSize, index / gridSize);
            if (!usedCells.Contains(candidate))
                return candidate;
        }

        for (int index = 0; index < maxTileValue; index++)
        {
            candidate = new Vector2Int(index % gridSize, index / gridSize);
            if (!usedCells.Contains(candidate))
                return candidate;
        }

        return new Vector2Int(0, 0);
    }

    private void PlaceTilesInSolvedOrder()
    {
        _tilesByCell.Clear();
        _cellsByTile.Clear();

        _blankCell = BlankCellForCurrentGrid();

        if (tiles == null)
            return;

        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            if (tile == null)
                continue;

            Vector2Int cell = tile.SolvedCell;
            if (!IsCellInside(cell) || cell == _blankCell)
                continue;

            Vector3 localPos = GetLocalPositionForCell(cell);
            tile.SetCurrentCell(cell, localPos);

            _tilesByCell[cell] = tile;
            _cellsByTile[tile] = cell;
        }
    }

    private void MoveRandomNeighborIntoBlank()
    {
        FillNeighborBuffer(_blankCell, _neighborBuffer);
        if (_neighborBuffer.Count == 0)
            return;

        FifteenPuzzleTile picked = _neighborBuffer[Random.Range(0, _neighborBuffer.Count)];
        if (!_cellsByTile.TryGetValue(picked, out Vector2Int fromCell))
            return;

        MoveTileIntoBlank(picked, fromCell, false);
    }

    private void MoveTileIntoBlank(FifteenPuzzleTile tile, Vector2Int fromCell, bool animate)
    {
        Vector2Int targetCell = _blankCell;
        _blankCell = fromCell;

        _tilesByCell.Remove(fromCell);
        _tilesByCell[targetCell] = tile;
        _cellsByTile[tile] = targetCell;

        Vector3 targetPosition = GetLocalPositionForCell(targetCell);
        if (animate)
            tile.AnimateToCell(targetCell, targetPosition);
        else
            tile.SetCurrentCell(targetCell, targetPosition);
    }

    private void EvaluateSolvedState(FifteenPuzzleTile lastMovedTile)
    {
        if (_isSolved)
            return;

        if (!IsSolvedWithoutEvents())
            return;

        _isSolved = true;
        if (lockInteractionOnSuccess)
            SetTilesInteractable(false);

        StopSuccessCoverSequence();
        _successCoverSequenceCoroutine = StartCoroutine(PlaySuccessCoverSequence(lastMovedTile));
    }

    private bool IsSolvedWithoutEvents()
    {
        if (tiles == null)
            return false;

        bool standardSolved = true;
        bool mirroredSolved = mirrorImageSlicesHorizontally;

        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = tiles[i];
            if (tile == null)
                continue;

            Vector2Int current = tile.CurrentCell;
            Vector2Int standardCell = tile.SolvedCell;

            if (current != standardCell)
                standardSolved = false;

            if (mirroredSolved)
            {
                Vector2Int mirroredCell = new Vector2Int(gridSize - 1 - standardCell.x, standardCell.y);
                if (current != mirroredCell)
                    mirroredSolved = false;
            }
        }

        return standardSolved || mirroredSolved;
    }

    private void FillNeighborBuffer(Vector2Int origin, List<FifteenPuzzleTile> buffer)
    {
        buffer.Clear();
        TryAddTileAt(origin + Vector2Int.left, buffer);
        TryAddTileAt(origin + Vector2Int.right, buffer);
        TryAddTileAt(origin + Vector2Int.up, buffer);
        TryAddTileAt(origin + Vector2Int.down, buffer);
    }

    private void TryAddTileAt(Vector2Int cell, List<FifteenPuzzleTile> buffer)
    {
        if (!IsCellInside(cell))
            return;

        if (_tilesByCell.TryGetValue(cell, out FifteenPuzzleTile tile) && tile != null)
            buffer.Add(tile);
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx <= 1 && dy <= 1) && (dx + dy > 0);
    }

    private bool IsCellInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < gridSize && cell.y < gridSize;
    }

    private Vector2Int BlankCellForCurrentGrid()
    {
        return new Vector2Int(gridSize - 1, gridSize - 1);
    }

    private Vector3 GetLocalPositionForCell(Vector2Int cell)
    {
        Vector3 origin = boardOriginLocal;
        if (centerBoard)
        {
            float half = (gridSize - 1) * cellSize * 0.5f;
            origin -= new Vector3(half, 0f, half);
        }

        return origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    public void SetPuzzleImage(Texture2D texture)
    {
        puzzleImage = texture;
        RefreshTileImageSlices();
    }

    private void RefreshTileImageSlices()
    {
        if (tiles == null)
            return;

        Texture2D image = useImageSlices ? puzzleImage : null;
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null)
                continue;

            tiles[i].ConfigureImageSlice(image, gridSize, mirrorImageSlicesHorizontally);
        }
    }

    private void SetTilesInteractable(bool enabled)
    {
        if (tiles == null)
            return;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null)
                continue;

            tiles[i].SetInteractionEnabled(enabled);
        }
    }

    private void SetSuccessCoverActive(bool isActive)
    {
        if (successCover != null)
            successCover.SetActive(isActive);

        SetSuccessCoverColliderEnabled(false);
    }

    private void SetSuccessCoverColliderEnabled(bool enabled)
    {
        ResolveSuccessCoverCollider();
        if (_successCoverCollider != null)
            _successCoverCollider.enabled = enabled;
    }

    private void UpdateSuccessCoverPose()
    {
        if (successCover == null || !alignSuccessCoverToBlankCell)
            return;

        Vector3 local = GetLocalPositionForCell(BlankCellForCurrentGrid());
        local.y += successCoverYOffset;
        successCover.transform.localPosition = local;
    }

    private void ResolveSuccessCoverRenderer()
    {
        if (successCoverRenderer != null)
            return;

        if (successCover == null)
            return;

        successCoverRenderer = successCover.GetComponent<Renderer>();
        if (successCoverRenderer == null)
            successCoverRenderer = successCover.GetComponentInChildren<Renderer>(true);
    }

    private void ResolveSuccessCoverCollider()
    {
        if (_successCoverCollider != null)
            return;

        if (successCover == null)
            return;

        _successCoverCollider = successCover.GetComponent<Collider>();
        if (_successCoverCollider == null)
            _successCoverCollider = successCover.GetComponentInChildren<Collider>(true);
    }

    private void RefreshSuccessCoverVisual()
    {
        ResolveSuccessCoverRenderer();
        if (successCoverRenderer == null)
            return;

        if (_successCoverPropertyBlock == null)
            _successCoverPropertyBlock = new MaterialPropertyBlock();

        successCoverRenderer.GetPropertyBlock(_successCoverPropertyBlock);
        _successCoverPropertyBlock.SetColor(CoverColorPropertyId, Color.white);
        _successCoverPropertyBlock.SetColor(CoverBaseColorPropertyId, Color.white);

        if (useImageSlices && puzzleImage != null && gridSize > 0)
        {
            Vector2Int cell = BlankCellForCurrentGrid();
            int xCell = mirrorImageSlicesHorizontally ? (gridSize - 1 - cell.x) : cell.x;
            float inv = 1f / gridSize;
            Vector2 scale = new Vector2(inv, inv);
            Vector2 offset = new Vector2(xCell * inv, 1f - (cell.y + 1) * inv);
            Vector4 st = new Vector4(scale.x, scale.y, offset.x, offset.y);

            _successCoverPropertyBlock.SetTexture(CoverMainTexPropertyId, puzzleImage);
            _successCoverPropertyBlock.SetTexture(CoverBaseMapPropertyId, puzzleImage);
            _successCoverPropertyBlock.SetVector(CoverMainTexStPropertyId, st);
            _successCoverPropertyBlock.SetVector(CoverBaseMapStPropertyId, st);
        }

        successCoverRenderer.SetPropertyBlock(_successCoverPropertyBlock);
    }

    private IEnumerator PlaySuccessCoverSequence(FifteenPuzzleTile lastMovedTile)
    {
        if (lastMovedTile != null)
        {
            while (lastMovedTile.IsMoveAnimating)
                yield return null;
        }

        UpdateSuccessCoverPose();
        RefreshSuccessCoverVisual();
        SetSuccessCoverActive(true);
        SetSuccessCoverColliderEnabled(false);

        if (successCover != null)
        {
            Vector3 target = successCover.transform.localPosition;
            Vector3 start = target + Vector3.up * Mathf.Max(0f, successCoverDropHeight);
            successCover.transform.localPosition = start;

            float duration = Mathf.Max(0.01f, successCoverDropDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                successCover.transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            successCover.transform.localPosition = target;
        }

        onSuccess?.Invoke();
        _successCoverSequenceCoroutine = null;
    }

    private void StopSuccessCoverSequence()
    {
        if (_successCoverSequenceCoroutine == null)
            return;

        StopCoroutine(_successCoverSequenceCoroutine);
        _successCoverSequenceCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        gridSize = Mathf.Max(2, gridSize);
        cellSize = Mathf.Max(0.01f, cellSize);
        shuffleMoves = Mathf.Clamp(shuffleMoves, 10, 400);

        if (!Application.isPlaying)
        {
            InitializeBoard();
            RefreshTileImageSlices();
            UpdateSuccessCoverPose();
            ResolveSuccessCoverRenderer();
            RefreshSuccessCoverVisual();
        }
    }
#endif
}
