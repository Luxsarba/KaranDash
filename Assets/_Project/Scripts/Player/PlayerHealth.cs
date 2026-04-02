using UnityEngine;

/// <summary>
/// Здоровье и урон игрока.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI textHP;
    [SerializeField] private HealthBar healthBar;

    [Header("Ссылки")]
    [SerializeField] private PlayerPause playerPause;
    [Header("Fallback Contact Damage")]
    [SerializeField] private float enemyContactDamage = 5f;
    [SerializeField] private float enemyContactCooldown = 1f;
    [SerializeField] private bool debugDamageLogs = true;
    private float _nextEnemyContactDamageTime;
    private bool _isDead;

    // Для совместимости со старым кодом
    public TMPro.TextMeshProUGUI textHP_Public => textHP;
    public HealthBar healtBar => healthBar;

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void Start()
    {
        // Не перезаписываем здоровье: оно могло быть установлено загрузкой сейва до Start.
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isDead) return;

        // Use tag string comparison here to avoid Unity error spam when a tag is not defined in TagManager.
        string otherTag = collision.gameObject.tag;

        if (otherTag == "EnemyAmmo")
        {
            TakeDamage(5f, "EnemyAmmoCollision");
            return;
        }

        // Fallback: ensure enemy contact can still hurt the player even if enemy attack script misses.
        if (IsEnemyCollision(collision) && Time.time >= _nextEnemyContactDamageTime)
        {
            TakeDamage(enemyContactDamage, "EnemyContactEnter");
            _nextEnemyContactDamageTime = Time.time + enemyContactCooldown;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isDead) return;

        if (IsEnemyCollision(collision) && Time.time >= _nextEnemyContactDamageTime)
        {
            TakeDamage(enemyContactDamage, "EnemyContactStay");
            _nextEnemyContactDamageTime = Time.time + enemyContactCooldown;
        }
    }

    private static bool IsEnemyCollision(Collision collision)
    {
        if (collision.collider == null)
            return false;

        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();
        return enemy != null && !enemy.IsDead;
    }

    private void AutoResolveReferences()
    {
        if (playerPause == null)
            playerPause = GetComponent<PlayerPause>();

        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>(true);
        if (healthBar == null)
            healthBar = FindAnyObjectByType<HealthBar>();

        if (textHP == null)
            textHP = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (textHP == null)
        {
            var labels = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                var candidate = labels[i];
                if (candidate == null)
                    continue;

                string n = candidate.name;
                if (n.Contains("HP") || n.Contains("ХП"))
                {
                    textHP = candidate;
                    break;
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, "Unknown");
    }

    public void TakeDamage(float damage, string source)
    {
        if (_isDead || currentHealth <= 0f) return;
        if (damage <= 0f) return;

        float before = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();

        if (debugDamageLogs)
        {
            Debug.Log($"[PlayerHealth] Damage source={source}, value={damage}, hp {before} -> {currentHealth}", this);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        _isDead = currentHealth <= 0f;
        UpdateHealthUI();
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        if (playerPause != null)
            playerPause.ShowLoseScreen();
    }

    private void UpdateHealthUI()
    {
        if (textHP != null)
            textHP.text = currentHealth.ToString();

        if (healthBar != null)
            healthBar.SetHealth((int)currentHealth, (int)maxHealth);
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0f;
}
