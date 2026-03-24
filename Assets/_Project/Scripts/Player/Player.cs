using UnityEngine;

/// <summary>
/// Главный скрипт игрока — координатор компонентов.
/// Хранит ссылку для GameManager и предоставляет API для внешней совместимости.
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Камеры")]
    public Camera playerCamera;
    public Camera additionalCamera;
    public GameObject playerHead;

    [Header("UI")]
    public TMPro.TextMeshProUGUI ammoText;

    // ===== Компоненты (кэшируются) =====
    private PlayerPause _pause;
    private PlayerHealth _health;
    private PlayerMovement _movement;
    private PlayerLook _look;
    private PlayerAnimation _animation;
    private PlayerInteraction _interaction;
    private PlayerWeapon _weapon;
    private PlayerInventory _inventory;
    private bool _componentsCached;

    private void CacheComponents()
    {
        if (_componentsCached) return;

        _pause = GetComponent<PlayerPause>();
        _health = GetComponent<PlayerHealth>();
        if (_health == null)
        {
            _health = gameObject.AddComponent<PlayerHealth>();
            Debug.LogWarning("[Player] PlayerHealth component was missing and has been added at runtime.", this);
        }
        _movement = GetComponent<PlayerMovement>();
        _look = GetComponent<PlayerLook>();
        _animation = GetComponent<PlayerAnimation>();
        _interaction = GetComponent<PlayerInteraction>();
        _weapon = GetComponent<PlayerWeapon>();
        _inventory = GetComponent<PlayerInventory>();

        _componentsCached = true;
    }

    // ===== Свойства для совместимости =====
    public float playerHP
    {
        get
        {
            CacheComponents();
            return _health != null ? _health.GetHealth() : 0f;
        }
        set
        {
            CacheComponents();
            if (_health != null) _health.SetHealth(value);
        }
    }

    public float speedMultiplier
    {
        get
        {
            CacheComponents();
            return _movement != null ? _movement.SpeedMultiplier : 1f;
        }
        set
        {
            CacheComponents();
            if (_movement != null) _movement.SpeedMultiplier = value;
        }
    }

    public TMPro.TextMeshProUGUI textHP
    {
        get
        {
            CacheComponents();
            return _health != null ? _health.textHP_Public : null;
        }
    }

    public HealthBar healtBar
    {
        get
        {
            CacheComponents();
            return _health != null ? _health.healtBar : null;
        }
    }

    private void Awake()
    {
        CacheComponents();
        GameManager.player = this;
    }

    private void Start()
    {
        CacheComponents();

        // Инициализация GameManager
        GameManager.EnablePlayerInput();
        Debug.Log($"player: {this}");

        // Стартовые значения боезапаса (могут быть перезаписаны системой загрузки)
        GameManager.currentAmmo = 10;
        GameManager.maxAmmo = 10;
    }

    // ===== Методы для совместимости =====

    public void HideCursor()
    {
        CacheComponents();
        _pause?.HideCursor();
    }

    public void ShowCursor()
    {
        CacheComponents();
        _pause?.ShowCursor();
    }

    public bool getPaused()
    {
        CacheComponents();
        return _pause != null && _pause.IsPaused;
    }

    public void Pause()
    {
        CacheComponents();
        _pause?.Pause();
    }

    public void Continue()
    {
        CacheComponents();
        _pause?.Continue();
    }

    public void Damage(float dmg)
    {
        CacheComponents();
        if (_health == null)
            _health = GetComponent<PlayerHealth>();
        _health?.TakeDamage(dmg, "Player.Damage");
    }

    public void Defeat()
    {
        CacheComponents();
        _pause?.ShowLoseScreen();
    }

    // ===== Вспомогательные методы =====

    public PlayerInventory GetInventory()
    {
        CacheComponents();
        return _inventory;
    }

    public PlayerWeapon GetWeapon()
    {
        CacheComponents();
        return _weapon;
    }

    public PlayerPause GetPause()
    {
        CacheComponents();
        return _pause;
    }

    public PlayerHealth GetHealth()
    {
        CacheComponents();
        return _health;
    }
}
