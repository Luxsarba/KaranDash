using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Territory paint mini-game: player paints tiles yellow, bots repaint them.
/// </summary>
public class TerritoryPaintPanel : MonoBehaviour
{
    private const int OwnerNone = 0;
    private const int OwnerPlayer = 1;
    private const int OwnerFirstBot = 2;
    private static readonly Vector2Int[] NeighborDirections =
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.up,
        Vector2Int.down
    };

    [System.Serializable]
    private class BotSettings
    {
        public string name = "Bot";
        public Color color = Color.red;
        [Min(0.1f)] public float speedTilesPerSecond = 2.2f;
        [Min(0.01f)] public float moveAnimDuration = 0.08f;
        public Transform botVisual;
        [HideInInspector] public TerritoryPaintBot runtimeBot;
        [HideInInspector] public bool isTemporarilyDisabled;
        [HideInInspector] public Coroutine reviveCoroutine;
    }

    [Header("Board")]
    [SerializeField, Min(2)] private int gridWidth = 12;
    [SerializeField, Min(2)] private int gridHeight = 12;
    [SerializeField, Min(0.1f)] private float tileSize = 0.85f;
    [SerializeField, Min(0f)] private float tileGap = 0.03f;
    [SerializeField, Min(0.02f)] private float tileThickness = 0.12f;
    [SerializeField] private Vector3 boardOrigin = Vector3.zero;
    [SerializeField] private bool centerBoard = true;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField, Min(0f)] private float botVisualYOffset = 0.6f;

    [Header("Gizmos")]
    [SerializeField] private bool showBoardGizmos = true;
    [SerializeField] private bool showTileGizmos = true;
    [SerializeField] private Color boardGizmoColor = new Color(1f, 0.92f, 0.2f, 0.9f);
    [SerializeField] private Color tileGizmoColor = new Color(1f, 1f, 1f, 0.28f);

    [Header("Colors")]
    [SerializeField] private Color neutralColor = new Color(0.32f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color playerColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("Bots")]
    [SerializeField] private List<BotSettings> bots = new List<BotSettings>();
    [SerializeField, Min(0.1f)] private float botSpeedMultiplier = 1.8f;
    [SerializeField, Min(0f)] private float botDisableDuration = 10f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;

    private readonly List<Vector2Int> _neighborBuffer = new List<Vector2Int>(4);
    private Transform _tileRoot;
    private TerritoryPaintTile[,] _tiles;
    private int[,] _owners;
    private int[] _ownerTileCounts;
    private int _playerOwnedCount;
    private bool _isBoardBuilt;
    private bool _isLocked;
    private bool _successInvoked;
    private Player _cachedPlayer;
    private int _builtGridWidth;
    private int _builtGridHeight;
    private float _builtTileSize;
    private float _builtTileGap;
    private float _builtTileThickness;
    private GameObject _builtTilePrefab;

    public bool IsLocked => _isLocked;

    private void Reset()
    {
        EnsureDefaultBots();
    }

    private void Awake()
    {
        EnsureDefaultBots();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        StartGame();
    }

    private void OnDisable()
    {
        StopAllBotReviveCoroutines();
        StopBots();
    }

    private void Update()
    {
        if (_isLocked || !_isBoardBuilt)
            return;

        if (!TryResolvePlayerTransform(out Transform playerTransform))
            return;

        TryPaintCell(OwnerPlayer, playerTransform.position);
    }

    public void StartGame()
    {
        EnsureDefaultBots();
        EnsureBoardBuilt();
        EnsureRuntimeBots();
        StopAllBotReviveCoroutines();

        _isLocked = false;
        _successInvoked = false;
        _playerOwnedCount = 0;
        ResetOwnerCounts();
        ResetBoardOwnersToNeutral();

        StartBots();
    }

    public void ResetGame()
    {
        StartGame();
    }

    public bool TryPaintCell(int ownerId, Vector3 worldPos)
    {
        if (_isLocked || !_isBoardBuilt || ownerId <= OwnerNone)
            return false;

        if (!TryGetCellFromWorldPosition(worldPos, out Vector2Int cell))
            return false;

        return TryPaintCellAt(ownerId, cell);
    }

    public bool TryPaintCellAt(int ownerId, Vector2Int cell)
    {
        if (_isLocked || !_isBoardBuilt || ownerId <= OwnerNone)
            return false;

        if (!IsOwnerIndexValid(ownerId))
            return false;

        if (!IsCellInside(cell))
            return false;

        int previousOwner = _owners[cell.x, cell.y];
        if (previousOwner == ownerId)
            return false;

        if (IsOwnerIndexValid(previousOwner) && _ownerTileCounts[previousOwner] > 0)
            _ownerTileCounts[previousOwner]--;

        _owners[cell.x, cell.y] = ownerId;
        if (IsOwnerIndexValid(ownerId))
            _ownerTileCounts[ownerId]++;

        _playerOwnedCount = IsOwnerIndexValid(OwnerPlayer) ? _ownerTileCounts[OwnerPlayer] : 0;

        _tiles[cell.x, cell.y].SetOwner(ownerId, ResolveOwnerColor(ownerId));

        // Bot is stunned only when the player actually erased the bot's last tile.
        if (ownerId == OwnerPlayer && IsBotOwner(previousOwner) && GetOwnerCount(previousOwner) == 0)
            TemporarilyDisableBotByOwner(previousOwner);

        if (!_successInvoked && _playerOwnedCount >= gridWidth * gridHeight)
            CompleteGame();

        return true;
    }

    public bool TryGetRandomNeighborCell(Vector2Int currentCell, out Vector2Int nextCell)
    {
        _neighborBuffer.Clear();

        for (int i = 0; i < NeighborDirections.Length; i++)
        {
            Vector2Int candidate = currentCell + NeighborDirections[i];
            if (IsCellInside(candidate))
                _neighborBuffer.Add(candidate);
        }

        if (_neighborBuffer.Count == 0)
        {
            nextCell = currentCell;
            return false;
        }

        nextCell = _neighborBuffer[Random.Range(0, _neighborBuffer.Count)];
        return true;
    }

    public Vector3 GetWorldPositionForCell(Vector2Int cell)
    {
        Vector3 local = GetLocalPositionForCell(cell);
        return transform.TransformPoint(local);
    }

    private void StartBots()
    {
        for (int i = 0; i < bots.Count; i++)
        {
            BotSettings settings = bots[i];
            if (settings == null || settings.runtimeBot == null)
                continue;
            if (settings.isTemporarilyDisabled)
                continue;

            Vector2Int startCell = GetDefaultStartCell(i);
            int ownerId = i + OwnerFirstBot;
            float speed = settings.speedTilesPerSecond * Mathf.Max(0.1f, botSpeedMultiplier);

            settings.runtimeBot.Begin(
                this,
                ownerId,
                startCell,
                speed,
                settings.moveAnimDuration,
                botVisualYOffset);

            TryPaintCellAt(ownerId, startCell);
        }
    }

    private void StopBots()
    {
        if (bots == null)
            return;

        for (int i = 0; i < bots.Count; i++)
        {
            BotSettings settings = bots[i];
            settings?.runtimeBot?.StopBot();
        }
    }

    private void CompleteGame()
    {
        _isLocked = true;
        if (_successInvoked)
            return;

        _successInvoked = true;
        StopAllBotReviveCoroutines();
        StopBots();
        onSuccess?.Invoke();
    }

    private bool TryResolvePlayerTransform(out Transform playerTransform)
    {
        playerTransform = null;

        if (GameManager.player != null)
            _cachedPlayer = GameManager.player;

        if (_cachedPlayer == null)
            _cachedPlayer = FindObjectOfType<Player>();

        if (_cachedPlayer == null)
            return false;

        playerTransform = _cachedPlayer.transform;
        return playerTransform != null;
    }

    private Color ResolveOwnerColor(int ownerId)
    {
        if (ownerId == OwnerPlayer)
            return playerColor;

        int botIndex = ownerId - OwnerFirstBot;
        if (botIndex >= 0 && botIndex < bots.Count && bots[botIndex] != null)
            return bots[botIndex].color;

        return Color.white;
    }

    private Vector2Int GetDefaultStartCell(int botIndex)
    {
        switch (botIndex % 4)
        {
            case 0:
                return new Vector2Int(0, gridHeight - 1);
            case 1:
                return new Vector2Int(gridWidth - 1, 0);
            case 2:
                return new Vector2Int(gridWidth - 1, gridHeight - 1);
            default:
                return new Vector2Int(0, 0);
        }
    }

    private bool TryGetCellFromWorldPosition(Vector3 worldPosition, out Vector2Int cell)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        Vector3 min = GetBoardMinLocalPosition();
        float pitch = tileSize + tileGap;
        float maxXCenter = min.x + (gridWidth - 1) * pitch;
        float maxYCenter = min.z + (gridHeight - 1) * pitch;
        float halfTile = tileSize * 0.5f;

        if (local.x < min.x - halfTile || local.x > maxXCenter + halfTile ||
            local.z < min.z - halfTile || local.z > maxYCenter + halfTile)
        {
            cell = default;
            return false;
        }

        float normalizedX = (local.x - min.x) / Mathf.Max(0.0001f, pitch);
        float normalizedY = (local.z - min.z) / Mathf.Max(0.0001f, pitch);

        int x = Mathf.RoundToInt(normalizedX);
        int y = Mathf.RoundToInt(normalizedY);

        cell = new Vector2Int(x, y);
        if (!IsCellInside(cell))
            return false;

        Vector3 center = GetLocalPositionForCell(cell);
        if (Mathf.Abs(local.x - center.x) > halfTile || Mathf.Abs(local.z - center.z) > halfTile)
            return false;

        return true;
    }

    private bool IsCellInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < gridWidth && cell.y < gridHeight;
    }

    private void EnsureOwnerCountsArray()
    {
        int targetLength = Mathf.Max(2, bots.Count + 2);
        if (_ownerTileCounts == null || _ownerTileCounts.Length != targetLength)
            _ownerTileCounts = new int[targetLength];
    }

    private bool IsOwnerIndexValid(int ownerId)
    {
        return _ownerTileCounts != null && ownerId >= 0 && ownerId < _ownerTileCounts.Length;
    }

    private bool IsBotOwner(int ownerId)
    {
        int botIndex = ownerId - OwnerFirstBot;
        return botIndex >= 0 && botIndex < bots.Count;
    }

    private int GetOwnerCount(int ownerId)
    {
        return IsOwnerIndexValid(ownerId) ? _ownerTileCounts[ownerId] : 0;
    }

    private void TemporarilyDisableBotByOwner(int ownerId)
    {
        if (_isLocked)
            return;

        int botIndex = ownerId - OwnerFirstBot;
        if (botIndex < 0 || botIndex >= bots.Count)
            return;

        BotSettings bot = bots[botIndex];
        if (bot == null || bot.runtimeBot == null || bot.isTemporarilyDisabled)
            return;

        bot.runtimeBot?.StopBot();
        bot.isTemporarilyDisabled = true;

        if (bot.reviveCoroutine != null)
            StopCoroutine(bot.reviveCoroutine);

        bot.reviveCoroutine = StartCoroutine(ReviveBotAfterDelayCoroutine(botIndex, ownerId));
    }

    private IEnumerator ReviveBotAfterDelayCoroutine(int botIndex, int ownerId)
    {
        float duration = Mathf.Max(0f, botDisableDuration);
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        if (_isLocked || botIndex < 0 || botIndex >= bots.Count)
            yield break;

        BotSettings bot = bots[botIndex];
        if (bot == null || bot.runtimeBot == null)
            yield break;

        bot.reviveCoroutine = null;
        bot.isTemporarilyDisabled = false;

        Vector2Int restartCell = GetDefaultStartCell(botIndex);
        if (bot.botVisual != null && TryGetCellFromWorldPosition(bot.botVisual.position, out Vector2Int currentCell))
            restartCell = currentCell;

        bot.runtimeBot.Begin(
            this,
            ownerId,
            restartCell,
            bot.speedTilesPerSecond * Mathf.Max(0.1f, botSpeedMultiplier),
            bot.moveAnimDuration,
            botVisualYOffset);

        TryPaintCellAt(ownerId, restartCell);
    }

    private void StopAllBotReviveCoroutines()
    {
        if (bots == null)
            return;

        for (int i = 0; i < bots.Count; i++)
        {
            BotSettings bot = bots[i];
            if (bot == null)
                continue;

            if (bot.reviveCoroutine != null)
            {
                StopCoroutine(bot.reviveCoroutine);
                bot.reviveCoroutine = null;
            }

            bot.isTemporarilyDisabled = false;
        }
    }

    private void EnsureRuntimeBots()
    {
        if (bots == null)
            return;

        for (int i = 0; i < bots.Count; i++)
        {
            BotSettings settings = bots[i];
            if (settings == null)
                continue;

            if (settings.botVisual == null)
                settings.botVisual = CreateDefaultBotVisual(i);

            if (settings.botVisual == null)
                continue;

            if (!settings.botVisual.TryGetComponent(out TerritoryPaintBot runtime))
                runtime = settings.botVisual.gameObject.AddComponent<TerritoryPaintBot>();

            settings.runtimeBot = runtime;
            settings.isTemporarilyDisabled = false;
            PositionBotVisual(settings.botVisual, GetDefaultStartCell(i));
        }
    }

    private Transform CreateDefaultBotVisual(int botIndex)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = $"TerritoryBot_{botIndex + 1}";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

        if (visual.TryGetComponent(out Collider collider))
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        return visual.transform;
    }

    private void PositionBotVisual(Transform botVisual, Vector2Int cell)
    {
        if (botVisual == null)
            return;

        botVisual.position = GetWorldPositionForCell(cell) + Vector3.up * botVisualYOffset;
    }

    private void RebuildBoard()
    {
        EnsureTileRoot();
        ClearTileRoot();

        _tiles = new TerritoryPaintTile[gridWidth, gridHeight];
        _owners = new int[gridWidth, gridHeight];

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject tileObject = CreateTileObject(cell);
                tileObject.name = $"Tile_{x}_{y}";
                tileObject.transform.SetParent(_tileRoot, false);
                tileObject.transform.localPosition = GetLocalPositionForCell(cell);
                tileObject.transform.localScale = new Vector3(tileSize, tileThickness, tileSize);

                TerritoryPaintTile tile = tileObject.GetComponent<TerritoryPaintTile>();
                if (tile == null)
                    tile = tileObject.AddComponent<TerritoryPaintTile>();

                tile.Initialize(cell, neutralColor, tileObject.transform.localPosition);
                _tiles[x, y] = tile;
            }
        }

        _isBoardBuilt = true;
        CacheBuiltLayout();
    }

    private void EnsureBoardBuilt()
    {
        if (!_isBoardBuilt || IsBoardLayoutDirty())
            RebuildBoard();
    }

    private bool IsBoardLayoutDirty()
    {
        if (!_isBoardBuilt)
            return true;

        return _builtGridWidth != gridWidth ||
               _builtGridHeight != gridHeight ||
               !Mathf.Approximately(_builtTileSize, tileSize) ||
               !Mathf.Approximately(_builtTileGap, tileGap) ||
               !Mathf.Approximately(_builtTileThickness, tileThickness) ||
               _builtTilePrefab != tilePrefab;
    }

    private void CacheBuiltLayout()
    {
        _builtGridWidth = gridWidth;
        _builtGridHeight = gridHeight;
        _builtTileSize = tileSize;
        _builtTileGap = tileGap;
        _builtTileThickness = tileThickness;
        _builtTilePrefab = tilePrefab;
    }

    private void ResetOwnerCounts()
    {
        EnsureOwnerCountsArray();
        for (int i = 0; i < _ownerTileCounts.Length; i++)
            _ownerTileCounts[i] = 0;
    }

    private void ResetBoardOwnersToNeutral()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _owners[x, y] = OwnerNone;
                _tiles[x, y].ResetOwner(neutralColor);
            }
        }
    }

    private GameObject CreateTileObject(Vector2Int cell)
    {
        if (tilePrefab != null)
            return Instantiate(tilePrefab);

        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        primitive.name = $"Tile_{cell.x}_{cell.y}";
        return primitive;
    }

    private void EnsureTileRoot()
    {
        if (_tileRoot != null)
            return;

        Transform existing = transform.Find("Tiles");
        if (existing != null)
        {
            _tileRoot = existing;
            return;
        }

        GameObject root = new GameObject("Tiles");
        root.transform.SetParent(transform, false);
        _tileRoot = root.transform;
    }

    private void ClearTileRoot()
    {
        if (_tileRoot == null)
            return;

        for (int i = _tileRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _tileRoot.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private Vector3 GetLocalPositionForCell(Vector2Int cell)
    {
        Vector3 min = GetBoardMinLocalPosition();
        float pitch = tileSize + tileGap;
        return new Vector3(min.x + cell.x * pitch, boardOrigin.y, min.z + cell.y * pitch);
    }

    private Vector3 GetBoardMinLocalPosition()
    {
        Vector3 min = boardOrigin;
        if (!centerBoard)
            return min;

        float pitch = tileSize + tileGap;
        float halfWidth = (gridWidth - 1) * pitch * 0.5f;
        float halfHeight = (gridHeight - 1) * pitch * 0.5f;
        min -= new Vector3(halfWidth, 0f, halfHeight);
        return min;
    }

    private void EnsureDefaultBots()
    {
        if (bots != null && bots.Count > 0)
            return;

        bots = new List<BotSettings>
        {
            new BotSettings { name = "Bot 1", color = new Color(1f, 0.25f, 0.25f, 1f) },
            new BotSettings { name = "Bot 2", color = new Color(0.2f, 0.5f, 1f, 1f) },
            new BotSettings { name = "Bot 3", color = new Color(0.2f, 0.9f, 0.35f, 1f) }
        };
    }

    private void OnDrawGizmos()
    {
        if (!showBoardGizmos)
            return;

        float pitch = tileSize + tileGap;
        if (gridWidth <= 0 || gridHeight <= 0 || pitch <= 0f)
            return;

        Vector3 minCenter = GetBoardMinLocalPosition();
        float halfTile = tileSize * 0.5f;

        Vector3 minCorner = new Vector3(minCenter.x - halfTile, boardOrigin.y, minCenter.z - halfTile);
        Vector3 maxCorner = new Vector3(
            minCenter.x + (gridWidth - 1) * pitch + halfTile,
            boardOrigin.y,
            minCenter.z + (gridHeight - 1) * pitch + halfTile);

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 boardCenter = (minCorner + maxCorner) * 0.5f;
        Vector3 boardSize = new Vector3(maxCorner.x - minCorner.x, 0.02f, maxCorner.z - minCorner.z);
        Gizmos.color = boardGizmoColor;
        Gizmos.DrawWireCube(boardCenter, boardSize);

        if (showTileGizmos)
        {
            Gizmos.color = tileGizmoColor;
            Vector3 tileSize3 = new Vector3(tileSize, 0.01f, tileSize);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 cellCenter = GetLocalPositionForCell(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(cellCenter, tileSize3);
                }
            }
        }

        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        gridWidth = Mathf.Max(2, gridWidth);
        gridHeight = Mathf.Max(2, gridHeight);
        tileSize = Mathf.Max(0.1f, tileSize);
        tileGap = Mathf.Max(0f, tileGap);
        tileThickness = Mathf.Max(0.02f, tileThickness);
        botVisualYOffset = Mathf.Max(0f, botVisualYOffset);
        botSpeedMultiplier = Mathf.Max(0.1f, botSpeedMultiplier);
        botDisableDuration = Mathf.Max(0f, botDisableDuration);
        if (boardGizmoColor.a <= 0f)
            boardGizmoColor.a = 0.9f;
        if (tileGizmoColor.a <= 0f)
            tileGizmoColor.a = 0.28f;
        EnsureDefaultBots();
    }
#endif
}
