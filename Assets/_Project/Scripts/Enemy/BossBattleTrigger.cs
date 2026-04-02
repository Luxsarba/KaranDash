using UnityEngine;

[DisallowMultipleComponent]
public class BossBattleTrigger : MonoBehaviour
{
    [SerializeField] private EraserBossSpawner bossSpawner;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool disableAfterActivation = true;

    private bool activated;

    private void Awake()
    {
        ResolveReferences();
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

    private void OnTriggerEnter(Collider other)
    {
        if (activated && triggerOnlyOnce)
            return;

        if (other.GetComponentInParent<Player>() == null)
            return;

        if (bossSpawner == null)
            return;

        bossSpawner.ActivateBattle();
        activated = true;

        if (disableAfterActivation)
            gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (bossSpawner == null)
            bossSpawner = GetComponentInParent<EraserBossSpawner>();
    }
}
