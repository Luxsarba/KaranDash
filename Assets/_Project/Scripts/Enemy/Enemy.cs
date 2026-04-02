using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public static event Action<Enemy> Died;

    [Header("Activity")]
    [SerializeField] private bool botActivity = true;
    [SerializeField] private bool staticEnemy = false;

    [Header("Stats")]
    [SerializeField, Range(1, 1000)] private float hp = 5f;
    [SerializeField, Range(0, 100)] private float armor = 0f;
    [SerializeField] private float hpUiHeight = 1.5f;

    [Header("Death")]
    [SerializeField] private ParticleSystem deathParticlePrefab;
    [SerializeField] private GameObject dyingVfxObject;
    [SerializeField] private AudioSource dyingSound;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private string deathAnimState = "Crying";

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyMove enemyMove;
    [SerializeField] private RagdollControl ragdollControl; // если нужно — включи в Die()
    [SerializeField] private Collider mainCollider;

    [Header("HP UI")]
    [SerializeField] private Canvas hpCanvasPrefab;

    [Header("Loot")]
    [SerializeField] private bool hasLoot = false;
    [SerializeField] private GameObject lootPrefab;

    private Canvas hpCanvasInstance;
    private RectTransform hpBar;
    private TextMeshProUGUI hpText;

    private Animator animator;
    private Animation legacyAnimation;

    private float maxHp;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (!animator)
            legacyAnimation = GetComponentInChildren<Animation>();

        // Если не назначили в инспекторе — попробуем найти сами
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!mainCollider) mainCollider = GetComponent<Collider>();
        if (!enemyMove) enemyMove = GetComponent<EnemyMove>();
        if (!ragdollControl) ragdollControl = GetComponentInChildren<RagdollControl>();

        maxHp = Mathf.Max(1f, hp);
    }

    private void Start()
    {
        CreateHpUi();
        RefreshHpUi();
    }

    private void Update()
    {
        // Поворачиваем UI к игроку, если всё есть
        if (!hpCanvasInstance) return;
        if (!GameManager.player) return;

        hpCanvasInstance.transform.LookAt(GameManager.player.transform.position);
    }

    public void Damage(float damage, bool ignoreArmor = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        float finalDamage = ignoreArmor ? damage : Mathf.Max(0f, damage - armor);
        if (finalDamage <= 0f) return;

        hp -= finalDamage;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        RefreshHpUi();

        if (hp <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Died?.Invoke(this);

        // VFX
        if (deathParticlePrefab)
        {
            var ps = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
        }

        if (dyingVfxObject) dyingVfxObject.SetActive(true);

        // Disable AI / movement / collision
        if (enemyMove) enemyMove.enabled = false;
        if (agent) agent.enabled = false;
        if (mainCollider) mainCollider.enabled = false;

        if (hpCanvasInstance) hpCanvasInstance.enabled = false;

        // Animation
        if (animator && !string.IsNullOrEmpty(deathAnimState))
            animator.Play(deathAnimState, 0, 0f);
        else if (legacyAnimation && !string.IsNullOrEmpty(deathAnimState) && legacyAnimation.GetClip(deathAnimState) != null)
        {
            legacyAnimation.Stop();
            legacyAnimation.Play(deathAnimState);
        }

        // Sound
        if (dyingSound) dyingSound.Play();

        // (опционально) рэгдолл
        // if (ragdollControl) ragdollControl.EnableRagdoll();

        StartCoroutine(DestroyAfterDelay(destroyDelay));
    }

    private IEnumerator DestroyAfterDelay(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        if (hasLoot && lootPrefab)
            Instantiate(lootPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    private void CreateHpUi()
    {
        if (!hpCanvasPrefab) return;

        hpCanvasInstance = Instantiate(
            hpCanvasPrefab,
            transform.position + Vector3.up * hpUiHeight,
            Quaternion.identity,
            transform
        );

        // Ожидаем, что в Canvas есть объект "Шкала"
        var scale = hpCanvasInstance.transform.Find("Шкала");
        if (scale) hpBar = scale.GetComponent<RectTransform>();

        hpText = hpCanvasInstance.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void RefreshHpUi()
    {
        if (!hpCanvasInstance) return;

        if (hpText) hpText.text = Mathf.CeilToInt(hp).ToString();

        if (hpBar)
        {
            float t = (maxHp <= 0f) ? 0f : (hp / maxHp); // 0..1
            // ширина и позиция — как у тебя, но безопаснее
            hpBar.sizeDelta = new Vector2(t * 100f, hpBar.sizeDelta.y);

            float x = (100f - (t * 100f)) / 200f;
            hpBar.localPosition = new Vector3(x, hpBar.localPosition.y, hpBar.localPosition.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Death"))
            Die();
    }

    // Если хочешь внешне менять maxHp (например, бафами) — сделай метод:
    public void SetMaxHp(float value, bool fillHp = true)
    {
        maxHp = Mathf.Max(1f, value);
        if (fillHp) hp = maxHp;
        RefreshHpUi();
    }
}

