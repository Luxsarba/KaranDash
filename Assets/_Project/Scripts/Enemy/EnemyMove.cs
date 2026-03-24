using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Player _player;
    private Transform _playerTransform;
    private bool attackReady = true, playerDetected = false;
    private RaycastHit hit;
    public int dmg = 5;
    private Animator animator;
    [SerializeField] private GameObject _look;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private bool debugDamageLogs = true;
    private float _nextDebugLogTime;
    private bool is_jumping = false;
    private float initial_speed;
    [SerializeField] private float attackRange = 3.25f;
    [SerializeField] private float maxAttackHeightDelta = 2.5f;

    
    void Start()
    {
        _agent = transform.GetComponent<NavMeshAgent>();
        initial_speed = _agent ? _agent.speed : 0f;
        animator = transform.GetComponentInChildren<Animator>();
    }

    private bool TryResolvePlayer()
    {
        if (GameManager.player != null)
        {
            _player = GameManager.player;
            _playerTransform = _player.transform;
            return true;
        }

        if (_player == null)
            _player = FindAnyObjectByType<Player>();

        if (_player == null)
            return false;

        GameManager.player = _player;
        _playerTransform = _player.transform;
        return true;
    }

    private void PrepareAttack()
    {
        attackReady = true;
    }

    IEnumerator CurveLong(NavMeshAgent agent, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * 5f * Vector3.up;
            normalizedTime += Time.deltaTime / duration;

            yield return null;
        }
        animator.enabled = true;
        is_jumping = false;
        agent.speed = initial_speed;
    }

    private void Update()
    {
        if (!TryResolvePlayer())
            return;

        if (debugDamageLogs && Time.time >= _nextDebugLogTime)
        {
            float hDist = HorizontalDistanceToPlayer();
            float vDist = Mathf.Abs(_playerTransform.position.y - transform.position.y);
            var health = _playerTransform.GetComponent<PlayerHealth>();
            Debug.Log($"[EnemyMove] {name} tick: hDist={hDist:F2}, vDist={vDist:F2}, inRange={IsPlayerInAttackRange()}, playerDetected={playerDetected}, attackReady={attackReady}, hasPlayerHealth={health != null}", this);
            _nextDebugLogTime = Time.time + 1f;
        }

        if (_agent && _agent.isOnOffMeshLink)
        {
            if (animator) animator.StopPlayback();
            if ((_agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeManual || 
                _agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross)
                && !is_jumping)
            {
                if (animator) animator.enabled = false;
                is_jumping = true;
                

                OffMeshLinkData data = _agent.currentOffMeshLinkData;
                Vector3 startPos = _agent.transform.position;
                Vector3 endPos = data.endPos + Vector3.up * _agent.baseOffset;
                float distanse = Vector3.Distance(startPos, endPos);
                if (distanse > 5f)
                    _agent.speed = 25.0f;
                float duration = distanse / _agent.speed;

                StartCoroutine(CurveLong(_agent, duration));
            }

        }
         
        if (_look &&
            RaycastService.TryRaycastForComponent(
                new Ray(_look.transform.position, _playerTransform.position - _look.transform.position),
                out hit,
                out Player _))
        {
            playerDetected = true;
        }

        if (playerDetected)
        {
            if (IsPlayerInAttackRange())
            {
                transform.LookAt(
                    new Vector3(_playerTransform.position.x,
                    transform.position.y,
                    _playerTransform.position.z));
                if (animator) animator.SetInteger("State", 2);
            }
            else
            {
                if (_agent)
                {
                    _agent.SetDestination(
                        _playerTransform.position -
                        (_playerTransform.position - transform.position).normalized * 1.5f);
                }
                if (animator) animator.SetInteger("State", 1);
            }
        }
        
        if (IsPlayerInAttackRange())
        {
            if (attackReady)
            {
                float distance = HorizontalDistanceToPlayer();
                var health = _playerTransform.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    if (debugDamageLogs)
                        Debug.Log($"[EnemyMove] {name} attack -> PlayerHealth.TakeDamage({dmg}), distance={distance:F2}", this);
                    health.TakeDamage(dmg, $"EnemyMove:{name}");
                }
                else
                {
                    if (debugDamageLogs)
                        Debug.LogWarning($"[EnemyMove] {name} attack fallback -> Player.Damage({dmg}), distance={distance:F2}", this);
                    _player.Damage(dmg);
                }

                attackReady = false;
                Invoke(nameof(PrepareAttack), 2f);
            }
        }
    }

    private float HorizontalDistanceToPlayer()
    {
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;
        return toPlayer.magnitude;
    }

    private bool IsPlayerInAttackRange()
    {
        float verticalDelta = Mathf.Abs(_playerTransform.position.y - transform.position.y);
        if (verticalDelta > maxAttackHeightDelta)
            return false;

        return HorizontalDistanceToPlayer() <= attackRange;
    }
}

    //private void Rotate()
    //{
    //    if (Vector3.Distance(transform.position, GameManager.player.transform.position) <= 2)
    //    {
    //        transform.LookAt(new Vector3(GameManager.player.transform.position.x, transform.position.y, GameManager.player.transform.position.z));
    //    }
    //}
