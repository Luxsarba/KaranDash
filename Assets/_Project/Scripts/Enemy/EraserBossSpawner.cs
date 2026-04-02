using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EraserBossSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy bossEnemy;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private Animation bossLegacyAnimation;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[2];

    [Header("Spawn")]
    [SerializeField, Min(0.1f)] private float spawnInterval = 6f;
    [SerializeField, Min(0f)] private float spawnAnimationLeadTime = 0.55f;
    [SerializeField, Min(0f)] private float spawnAnimationRecovery = 0.15f;
    [SerializeField, Min(0)] private int maxAliveSpawned = 4;
    [SerializeField] private string spawnAnimationState = "Attack";
    [SerializeField] private string idleAnimationState = "Eraser Idle";
    [SerializeField] private bool activateOnStart;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnLoop;

    public bool IsBattleActive { get; private set; }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (activateOnStart)
            ActivateBattle();
        else
            PlayIdleAnimation();
    }

    private void OnDisable()
    {
        StopBattle();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }
#endif

    public void ActivateBattle()
    {
        if (IsBattleActive)
            return;

        if (bossEnemy != null && bossEnemy.IsDead)
            return;

        IsBattleActive = true;
        spawnLoop = StartCoroutine(SpawnLoop());
    }

    public void StopBattle()
    {
        IsBattleActive = false;

        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            spawnLoop = null;
        }

        PlayIdleAnimation();
    }

    private IEnumerator SpawnLoop()
    {
        while (IsBattleActive)
        {
            yield return SpawnWave();

            if (!IsBattleActive)
                yield break;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator SpawnWave()
    {
        if (bossEnemy != null && bossEnemy.IsDead)
        {
            StopBattle();
            yield break;
        }

        CleanupSpawnedEnemies();

        int availableSlots = maxAliveSpawned > 0
            ? Mathf.Max(0, maxAliveSpawned - spawnedEnemies.Count)
            : int.MaxValue;

        if (enemyPrefab == null || availableSlots <= 0)
        {
            PlayIdleAnimation();
            yield break;
        }

        PlaySpawnAnimation();

        if (spawnAnimationLeadTime > 0f)
            yield return new WaitForSeconds(spawnAnimationLeadTime);

        for (int i = 0; i < spawnPoints.Length && availableSlots > 0; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null)
                continue;

            GameObject spawned = Instantiate(enemyPrefab, point.position, point.rotation);
            spawnedEnemies.Add(spawned);
            availableSlots--;
        }

        if (spawnAnimationRecovery > 0f)
            yield return new WaitForSeconds(spawnAnimationRecovery);

        PlayIdleAnimation();
    }

    private void CleanupSpawnedEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
                spawnedEnemies.RemoveAt(i);
        }
    }

    private void PlaySpawnAnimation()
    {
        if (string.IsNullOrWhiteSpace(spawnAnimationState))
            return;

        if (bossAnimator != null && bossAnimator.gameObject.activeInHierarchy && bossAnimator.enabled)
        {
            bossAnimator.Play(spawnAnimationState, 0, 0f);
            return;
        }

        if (bossLegacyAnimation != null && bossLegacyAnimation.gameObject.activeInHierarchy && bossLegacyAnimation.GetClip(spawnAnimationState) != null)
        {
            bossLegacyAnimation.Stop();
            bossLegacyAnimation.Play(spawnAnimationState);
        }
    }

    private void PlayIdleAnimation()
    {
        if (bossAnimator != null && bossAnimator.gameObject.activeInHierarchy && bossAnimator.enabled && !string.IsNullOrWhiteSpace(idleAnimationState))
        {
            bossAnimator.Play(idleAnimationState, 0, 0f);
            return;
        }

        if (bossLegacyAnimation == null || !bossLegacyAnimation.gameObject.activeInHierarchy)
            return;

        if (string.IsNullOrWhiteSpace(idleAnimationState) || bossLegacyAnimation.GetClip(idleAnimationState) == null)
        {
            bossLegacyAnimation.Stop();
            return;
        }

        bossLegacyAnimation.Stop();
        bossLegacyAnimation.Play(idleAnimationState);
    }

    private void ResolveReferences()
    {
        if (bossEnemy == null)
            bossEnemy = GetComponent<Enemy>();

        bossAnimator = FindPreferredAnimator();
        bossLegacyAnimation = FindPreferredLegacyAnimation();

        if (spawnPoints == null || spawnPoints.Length == 0)
            spawnPoints = new Transform[2];

        if (spawnPoints.Length < 2)
            System.Array.Resize(ref spawnPoints, 2);

        if (spawnPoints[0] == null)
            spawnPoints[0] = transform.Find("SpawnPoint_A");

        if (spawnPoints[1] == null)
            spawnPoints[1] = transform.Find("SpawnPoint_B");
    }

    private Animator FindPreferredAnimator()
    {
        Animator fallback = null;
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];
            if (candidate == null)
                continue;

            fallback ??= candidate;
            if (candidate.gameObject.activeInHierarchy && candidate.enabled)
                return candidate;
        }

        return fallback;
    }

    private Animation FindPreferredLegacyAnimation()
    {
        Animation fallback = null;
        Animation[] animations = GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            Animation candidate = animations[i];
            if (candidate == null)
                continue;

            fallback ??= candidate;
            if (candidate.gameObject.activeInHierarchy && candidate.enabled)
                return candidate;
        }

        return fallback;
    }
}



