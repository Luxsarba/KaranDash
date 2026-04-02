using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSessionManager : MonoBehaviour
{
    private const string ManagerObjectName = "_PlayerSessionManager";
    private const string MenuSceneName = "MenuScene";
    private const string HomeSceneName = "Home";
    private static readonly Vector3 HiddenSpawnPosition = new Vector3(0f, -10000f, 0f);

    private enum RequestKind
    {
        None,
        NewGame,
        Transition,
        Continue
    }

    private struct PendingSceneRequest
    {
        public RequestKind Kind;
        public string SceneName;
        public string SpawnPointId;
        public string FallbackStationId;
        public float[] FallbackPlayerPosition;
    }

    private static PlayerSessionManager _instance;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private KeyCode deleteSaveKey = KeyCode.Keypad9;

    private GameObject _playerRootInstance;
    private Player _playerInstance;
    private PendingSceneRequest _pendingRequest;
    private SaveData _pendingSaveData;
    private bool _hasPendingRequest;
    private int _activeSlotIndex;
    private bool _useDebugSave;

    public static bool HasPendingScenePlacement => _instance != null && _instance._hasPendingRequest;
    public static PlayerSessionManager Instance => _instance;
    public int? ActiveSlotIndex => _activeSlotIndex >= 1 && _activeSlotIndex <= 3 ? _activeSlotIndex : (int?)null;
    public bool UsesDebugSave => _useDebugSave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _instance = null;
        GameManager.player = null;
        GameManager.inventory = null;
    }

    public static PlayerSessionManager EnsureSession(GameObject prefab = null)
    {
        if (_instance == null)
        {
            var managerObject = new GameObject(ManagerObjectName);
            _instance = managerObject.AddComponent<PlayerSessionManager>();
        }

        if (prefab != null)
            _instance.SetPlayerPrefab(prefab);

        return _instance;
    }

    public static void StartNewGame(GameObject prefab, string sceneName, int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[PlayerSessionManager] StartNewGame failed: scene name is empty.");
            return;
        }

        PlayerSessionManager manager = EnsureSession(prefab);
        manager.ResetRuntimeState();
        manager._activeSlotIndex = slotIndex;
        manager._useDebugSave = false;
        manager._pendingSaveData = null;
        manager._pendingRequest = new PendingSceneRequest
        {
            Kind = RequestKind.NewGame,
            SceneName = sceneName,
            SpawnPointId = string.Empty,
            FallbackStationId = string.Empty,
            FallbackPlayerPosition = null
        };
        manager._hasPendingRequest = true;
        SceneManager.LoadScene(sceneName);
    }

    public static void StartNewGame(GameObject prefab, string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[PlayerSessionManager] StartNewGame failed: scene name is empty.");
            return;
        }

        PlayerSessionManager manager = EnsureSession(prefab);
        manager.ResetRuntimeState();
        manager._activeSlotIndex = 0;
        manager._useDebugSave = true;
        manager._pendingSaveData = null;
        manager._pendingRequest = new PendingSceneRequest
        {
            Kind = RequestKind.NewGame,
            SceneName = sceneName,
            SpawnPointId = string.Empty,
            FallbackStationId = string.Empty,
            FallbackPlayerPosition = null
        };
        manager._hasPendingRequest = true;
        SceneManager.LoadScene(sceneName);
    }

    public static void ContinueFromSave(GameObject prefab, int slotIndex)
    {
        if (!SaveSystem.HasSave(slotIndex))
        {
            Debug.LogWarning($"[PlayerSessionManager] Continue failed: slot {slotIndex} save file not found.");
            return;
        }

        SaveData data = SaveSystem.Load(slotIndex);
        if (data == null)
            return;

        ContinueFromData(prefab, data, slotIndex, useDebugSave: false);
    }

    public static void ContinueFromSave(GameObject prefab)
    {
        for (int slotIndex = 1; slotIndex <= 3; slotIndex++)
        {
            if (!SaveSystem.HasSave(slotIndex))
                continue;

            ContinueFromSave(prefab, slotIndex);
            return;
        }

        Debug.LogWarning("[PlayerSessionManager] Continue failed: there are no occupied save slots.");
    }

    public static void ContinueFromDebugSave(GameObject prefab)
    {
        if (!SaveSystem.HasDebugSave())
        {
            Debug.LogWarning("[PlayerSessionManager] Continue debug failed: debug save file not found.");
            return;
        }

        SaveData data = SaveSystem.LoadDebug();
        if (data == null)
            return;

        ContinueFromData(prefab, data, slotIndex: 0, useDebugSave: true);
    }

    public static void TransitionTo(string sceneName, string spawnPointId)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[PlayerSessionManager] Transition failed: scene name is empty.");
            return;
        }

        PlayerSessionManager manager = EnsureSession();
        manager._pendingSaveData = null;
        manager._pendingRequest = new PendingSceneRequest
        {
            Kind = RequestKind.Transition,
            SceneName = sceneName,
            SpawnPointId = spawnPointId,
            FallbackStationId = string.Empty,
            FallbackPlayerPosition = null
        };
        manager._hasPendingRequest = true;
        SceneManager.LoadScene(sceneName);
    }

    public static void ReturnToMenu()
    {
        PlayerSessionManager manager = EnsureSession();
        manager.ResetRuntimeState();
        manager._pendingSaveData = null;
        manager._hasPendingRequest = false;
        manager._activeSlotIndex = 0;
        manager._useDebugSave = false;
        SceneManager.LoadScene(MenuSceneName);
    }

    public static void SaveCurrentGame(Player player, PlayerInventory inv, string stationId, string spawnPointId, string saveLocationLabel)
    {
        PlayerSessionManager manager = EnsureSession();

        if (manager.ActiveSlotIndex.HasValue)
        {
            SaveSystem.Save(manager.ActiveSlotIndex.Value, player, inv, stationId, spawnPointId, saveLocationLabel);
            return;
        }

        SaveSystem.SaveDebug(player, inv, stationId, spawnPointId, saveLocationLabel);
    }

    public void EnsurePlayerExists()
    {
        if (_playerInstance != null)
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSessionManager] Player prefab is not assigned.", this);
            return;
        }

        GameObject playerObject = Instantiate(playerPrefab, HiddenSpawnPosition, Quaternion.identity);
        _playerRootInstance = playerObject;
        _playerInstance = playerObject.GetComponentInChildren<Player>(true);

        if (_playerInstance == null)
        {
            Debug.LogError("[PlayerSessionManager] Assigned player prefab does not contain a Player component in its hierarchy.", this);
            Destroy(playerObject);
            _playerRootInstance = null;
            return;
        }

        DontDestroyOnLoad(playerObject);
        GameManager.player = _playerInstance;
        GameManager.inventory = _playerInstance.GetInventory();
    }

    public void HandleGameplaySceneBootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsMenuScene(activeScene.name))
            return;

        if (_hasPendingRequest)
            return;

        EnsurePlayerExists();
        _activeSlotIndex = 0;
        _useDebugSave = true;

        SceneSpawnPoint spawnPoint = SceneSpawnPoint.FindDefaultOrFirst(activeScene);
        if (spawnPoint == null)
        {
            Debug.LogError($"[PlayerSessionManager] No SceneSpawnPoint found in scene '{activeScene.name}'.", this);
            return;
        }

        PlacePlayer(spawnPoint.Position, spawnPoint.Rotation);
    }

    public void SetPlayerPrefab(GameObject prefab)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponentInChildren<Player>(true) == null)
        {
            Debug.LogError("[PlayerSessionManager] Attempted to assign a player prefab without a Player component in its hierarchy.", prefab);
            return;
        }

        playerPrefab = prefab;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(deleteSaveKey))
            return;

        if (ActiveSlotIndex.HasValue)
        {
            int slotIndex = ActiveSlotIndex.Value;
            if (!SaveSystem.HasSave(slotIndex))
            {
                Debug.Log($"[SaveSystem] NumPad9 pressed, but slot {slotIndex} is empty.");
                return;
            }

            SaveSystem.DeleteSave(slotIndex);
            Debug.Log($"[SaveSystem] Save slot {slotIndex} deleted by NumPad9.");
            return;
        }

        if (_useDebugSave)
        {
            if (!SaveSystem.HasDebugSave())
            {
                Debug.Log("[SaveSystem] NumPad9 pressed, but there is no debug save file to delete.");
                return;
            }

            SaveSystem.DeleteDebugSave();
            Debug.Log("[SaveSystem] Debug save file deleted by NumPad9.");
            return;
        }

        Debug.Log("[SaveSystem] NumPad9 pressed, but there is no active save slot.");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
        {
            DestroyLivePlayer();
            return;
        }

        if (!_hasPendingRequest)
            return;

        EnsurePlayerExists();

        if (_pendingSaveData != null)
            SaveSystem.ApplyToPlayer(_playerInstance, _playerInstance.GetInventory(), _pendingSaveData);

        if (!TryResolvePendingSpawn(scene, out Vector3 spawnPosition, out Quaternion spawnRotation))
        {
            Debug.LogError($"[PlayerSessionManager] Failed to resolve spawn for scene '{scene.name}'.", this);
            _hasPendingRequest = false;
            _pendingSaveData = null;
            return;
        }

        PlacePlayer(spawnPosition, spawnRotation);

        _hasPendingRequest = false;
        _pendingSaveData = null;
        _pendingRequest = default;
    }

    private bool TryResolvePendingSpawn(Scene scene, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = HiddenSpawnPosition;
        spawnRotation = Quaternion.identity;

        if (!string.IsNullOrWhiteSpace(_pendingRequest.SpawnPointId))
        {
            SceneSpawnPoint explicitSpawn = SceneSpawnPoint.FindById(scene, _pendingRequest.SpawnPointId);
            if (explicitSpawn != null)
            {
                spawnPosition = explicitSpawn.Position;
                spawnRotation = explicitSpawn.Rotation;
                return true;
            }

            Debug.LogWarning($"[PlayerSessionManager] Spawn point '{_pendingRequest.SpawnPointId}' was not found in scene '{scene.name}'. Falling back.");
        }

        if (!string.IsNullOrWhiteSpace(_pendingRequest.FallbackStationId))
        {
            SaveStation station = SaveStationRegistry.FindById(_pendingRequest.FallbackStationId);
            if (station != null)
            {
                spawnPosition = station.GetSpawnPosition();
                spawnRotation = station.GetSpawnRotation();
                return true;
            }
        }

        if (_pendingRequest.FallbackPlayerPosition != null && _pendingRequest.FallbackPlayerPosition.Length == 3)
        {
            spawnPosition = new Vector3(
                _pendingRequest.FallbackPlayerPosition[0],
                _pendingRequest.FallbackPlayerPosition[1],
                _pendingRequest.FallbackPlayerPosition[2]);

            SceneSpawnPoint defaultSpawn = SceneSpawnPoint.FindDefaultOrFirst(scene);
            if (defaultSpawn != null)
                spawnRotation = defaultSpawn.Rotation;
            else if (_playerInstance != null)
                spawnRotation = _playerInstance.transform.rotation;

            return true;
        }

        SceneSpawnPoint fallbackSpawn = SceneSpawnPoint.FindDefaultOrFirst(scene);
        if (fallbackSpawn == null)
            return false;

        spawnPosition = fallbackSpawn.Position;
        spawnRotation = fallbackSpawn.Rotation;
        return true;
    }

    private void PlacePlayer(Vector3 position, Quaternion rotation)
    {
        if (_playerInstance == null)
            return;

        if (_playerRootInstance != null)
            _playerRootInstance.transform.SetPositionAndRotation(position, rotation);

        // Gameplay object must land exactly on the spawn point even if the root
        // container has its own offset/pivot because of the HUD branch.
        _playerInstance.transform.SetPositionAndRotation(position, rotation);

        Rigidbody playerRigidbody = _playerInstance.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        Time.timeScale = 1f;
        GameManager.EnablePlayerInput();
    }

    private void ResetRuntimeState()
    {
        CollectedWorldObjectState.Clear();
        QuestProgressState.Clear();
        ObjectiveCounterState.Clear();
        DestroyLivePlayer();
        Time.timeScale = 1f;
    }

    private void DestroyLivePlayer()
    {
        if (_playerRootInstance == null && _playerInstance == null)
            return;

        if (_playerRootInstance != null)
        {
            _playerRootInstance.SetActive(false);
            Destroy(_playerRootInstance);
        }
        else if (_playerInstance != null)
        {
            _playerInstance.gameObject.SetActive(false);
            Destroy(_playerInstance.gameObject);
        }

        _playerRootInstance = null;
        _playerInstance = null;
        GameManager.player = null;
        GameManager.inventory = null;
    }

    private static void ContinueFromData(GameObject prefab, SaveData data, int slotIndex, bool useDebugSave)
    {
        if (data == null)
            return;

        if (string.IsNullOrWhiteSpace(data.sceneName))
        {
            Debug.LogWarning("[PlayerSessionManager] Continue failed: save has no sceneName.");
            return;
        }

        PlayerSessionManager manager = EnsureSession(prefab);
        manager.ResetRuntimeState();
        manager._activeSlotIndex = slotIndex;
        manager._useDebugSave = useDebugSave;
        SaveSystem.PrepareRuntimeStateForLoadedSave(data);

        manager._pendingSaveData = data;
        manager._pendingRequest = new PendingSceneRequest
        {
            Kind = RequestKind.Continue,
            SceneName = data.sceneName,
            SpawnPointId = data.lastSaveSpawnPointId,
            FallbackStationId = data.lastSaveStationId,
            FallbackPlayerPosition = data.playerPosition
        };
        manager._hasPendingRequest = true;
        SceneManager.LoadScene(data.sceneName);
    }

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, MenuSceneName, System.StringComparison.Ordinal) ||
               string.Equals(sceneName, HomeSceneName, System.StringComparison.Ordinal);
    }
}
