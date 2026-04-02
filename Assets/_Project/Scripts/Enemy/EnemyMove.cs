using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    [SerializeField] private GameObject _look;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float attackRange = 3.25f;
    [SerializeField] private float maxAttackHeightDelta = 2.5f;
    [SerializeField] private float chaseOffset = 1.5f;
    [SerializeField] private float attackCooldown = 2f;

    public int dmg = 5;

    private NavMeshAgent _agent;
    private Player _player;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private Animator _animator;
    private bool _attackReady = true;
    private bool _playerDetected;
    private bool _isJumping;
    private float _initialSpeed;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _initialSpeed = _agent != null ? _agent.speed : 0f;
    }

    private void Update()
    {
        if (!TryResolvePlayer())
            return;

        if (!IsPlayerAlive())
        {
            _playerDetected = false;
            StopAgent();
            SetAnimatorState(0);
            return;
        }

        HandleOffMeshLinkTraversal();
        UpdateDetection();

        if (!_playerDetected)
            return;

        if (IsPlayerInAttackRange())
        {
            FacePlayer();
            StopAgent();
            SetAnimatorState(2);
            TryAttack();
            return;
        }

        ChasePlayer();
        SetAnimatorState(1);
    }

    private bool TryResolvePlayer()
    {
        if (GameManager.player != null)
        {
            AssignPlayer(GameManager.player);
            return true;
        }

        if (_player == null)
            _player = FindAnyObjectByType<Player>();

        if (_player == null)
            return false;

        GameManager.player = _player;
        AssignPlayer(_player);
        return true;
    }

    private void AssignPlayer(Player player)
    {
        _player = player;
        _playerTransform = player.transform;

        if (_playerHealth == null || _playerHealth.gameObject != player.gameObject)
            _playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void UpdateDetection()
    {
        if (_look == null || _playerTransform == null)
            return;

        Ray visionRay = new Ray(_look.transform.position, _playerTransform.position - _look.transform.position);
        if (RaycastService.TryRaycastForComponent(visionRay, out _, out Player _))
            _playerDetected = true;
    }

    private void ChasePlayer()
    {
        if (_agent == null || !_agent.enabled)
            return;

        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        Vector3 chaseDestination = _playerTransform.position - directionToPlayer * chaseOffset;
        _agent.SetDestination(chaseDestination);
    }

    private void TryAttack()
    {
        if (!_attackReady)
            return;

        if (!IsPlayerAlive())
            return;

        if (_playerHealth != null)
            _playerHealth.TakeDamage(dmg, $"EnemyMove:{name}");
        else
            _player.Damage(dmg);

        _attackReady = false;
        Invoke(nameof(PrepareAttack), attackCooldown);
    }

    private void PrepareAttack()
    {
        _attackReady = true;
    }

    private void HandleOffMeshLinkTraversal()
    {
        if (_agent == null || !_agent.isOnOffMeshLink)
            return;

        if (_animator != null)
            _animator.StopPlayback();

        bool isJumpLink = _agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeManual ||
                          _agent.nextOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeJumpAcross;

        if (!isJumpLink || _isJumping)
            return;

        if (_animator != null)
            _animator.enabled = false;

        _isJumping = true;

        OffMeshLinkData data = _agent.currentOffMeshLinkData;
        Vector3 startPos = _agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * _agent.baseOffset;
        float distance = Vector3.Distance(startPos, endPos);
        if (distance > 5f)
            _agent.speed = 25f;

        float duration = distance / Mathf.Max(_agent.speed, 0.01f);
        StartCoroutine(CurveLong(_agent, duration));
    }

    private IEnumerator CurveLong(NavMeshAgent agent, float duration)
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0f;

        while (normalizedTime < 1f)
        {
            float yOffset = curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * 5f * Vector3.up;
            normalizedTime += Time.deltaTime / Mathf.Max(duration, 0.01f);
            yield return null;
        }

        if (_animator != null)
            _animator.enabled = true;

        _isJumping = false;
        agent.speed = _initialSpeed;
    }

    private void FacePlayer()
    {
        if (_playerTransform == null)
            return;

        Vector3 lookTarget = new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z);
        transform.LookAt(lookTarget);
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.enabled)
            return;

        _agent.ResetPath();
    }

    private void SetAnimatorState(int state)
    {
        if (_animator != null)
            _animator.SetInteger("State", state);
    }

    private float HorizontalDistanceToPlayer()
    {
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;
        return toPlayer.magnitude;
    }

    private bool IsPlayerInAttackRange()
    {
        if (_playerTransform == null)
            return false;

        float verticalDelta = Mathf.Abs(_playerTransform.position.y - transform.position.y);
        if (verticalDelta > maxAttackHeightDelta)
            return false;

        return HorizontalDistanceToPlayer() <= attackRange;
    }

    private bool IsPlayerAlive()
    {
        if (_playerHealth != null)
            return _playerHealth.IsAlive();

        return _player != null && _player.playerHP > 0f;
    }
}
