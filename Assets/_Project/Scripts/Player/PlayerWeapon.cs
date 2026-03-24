using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Система оружия игрока: стрельба, удары, переключение режимов.
/// Логика рейкастов вынесена в RaycastService.
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    [Header("IK")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Weapon")]
    [SerializeField] private GameObject weapon;
    [SerializeField] private ParticleSystem shootVfx;

    [Header("Shooting")]
    [SerializeField] private LayerMask shootMask;
    [SerializeField, Min(0.1f)] private float shootDistance = 100f;
    [SerializeField, Min(0.1f)] private float fireInterval = 0.6f;
    [SerializeField, Min(0.1f)] private float meleeRange = 2.5f;

    [Header("Grapple Integration")]
    [SerializeField] private bool isGrappling;
    [SerializeField] private AudioSource fireSound;
    [SerializeField] private AudioSource punchSound;

    public bool IsGrappling => isGrappling;
    public AudioSource FireSound => fireSound;
    public AudioSource PunchSound => punchSound;

    private Animator _animator;
    private bool _isShooting = true;
    private bool _canShoot = true;
    private Ray _currentRay;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        UpdateAmmoUI();
        _canShoot = true;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ResetShootCooldown));
    }

    public void SetReady()
    {
        _canShoot = true;

        if (_isShooting && !IsGrappling)
        {
            weapon.SetActive(true);
            leftHandIK.data.targetPositionWeight = 1.0f;
            rightHandIK.data.targetPositionWeight = 1.0f;
        }
    }

    // Методы, которые вызывает GrapplingHook
    public void EnterGrappleMode()
    {
        isGrappling = true;
        _canShoot = false;
        weapon.SetActive(false);
        leftHandIK.data.targetPositionWeight = 0.0f;
        rightHandIK.data.targetPositionWeight = 0.0f;
    }

    public void ExitGrappleMode()
    {
        isGrappling = false;

        if (_isShooting)
        {
            weapon.SetActive(true);
            leftHandIK.data.targetPositionWeight = 1.0f;
            rightHandIK.data.targetPositionWeight = 1.0f;
        }
    }

    /// <summary>
    /// Блокировка стрельбы для GrapplingHook.
    /// </summary>
    public void SetBlocked(bool blocked)
    {
        _canShoot = !blocked;
    }

    private void Update()
    {
        _currentRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        HandleShooting();
        HandleMeleeAttack();
        HandleWeaponSwitch();
    }

    private void HandleShooting()
    {
        if (DialogueManager.IsFireInputBlockedByDialogue() ||
            !Input.GetMouseButton(0) ||
            GameManager.isPlayerInputBlocked ||
            !_canShoot ||
            IsGrappling)
        {
            return;
        }

        _canShoot = false;
        Invoke(nameof(ResetShootCooldown), fireInterval);

        if (GameManager.currentAmmo <= 0 || !_isShooting)
        {
            TryMeleeAttack();
        }
        else
        {
            PerformShooting();
        }
    }

private void TryMeleeAttack()
    {
        if (TryRaycastEnemy(out var hit, out _, shootDistance))
        {
            PerformPunch(hit);
        }
    }

private void PerformShooting()
    {
        shootVfx?.Play();
        FireSound?.Play();
        GameManager.currentAmmo--;
        UpdateAmmoUI();

        if (TryRaycastEnemy(out var hit, out var enemy, shootDistance))
        {
            Debug.Log($"Игрок попал в объект с тэгом {hit.transform.tag}");
            enemy.Damage(5);
        }
    }

private void HandleMeleeAttack()
    {
        if (!Input.GetKey(KeyCode.V) ||
            GameManager.isPlayerInputBlocked ||
            !_canShoot ||
            IsGrappling)
            return;

        _canShoot = false;
        Invoke(nameof(ResetShootCooldown), fireInterval);

        if (TryRaycastEnemy(out var hit, out _, meleeRange))
        {
            PerformPunch(hit);
        }
    }

    private void PerformPunch(RaycastHit hit)
    {
        leftHandIK.data.targetPositionWeight = 0.0f;
        rightHandIK.data.targetPositionWeight = 0.0f;
        weapon.SetActive(false);
        _animator.Play("Punch");
        PunchSound?.Play();
        if (RaycastService.TryGetComponentInParents(hit, out Enemy enemy))
            enemy.Damage(10);
    }

private bool TryRaycastEnemy(out RaycastHit hit, out Enemy enemy, float maxDistance)
    {
        if (RaycastService.TryRaycastForComponent(_currentRay, out hit, out enemy, maxDistance, shootMask.value))
            return true;

        // Fallback: if shootMask is misconfigured in scene, try all layers.
        if (RaycastService.TryRaycastForComponent(_currentRay, out hit, out enemy, maxDistance, ~0))
            return true;

        hit = default;
        enemy = null;
        return false;
    }


    private void HandleWeaponSwitch()
    {
        if (IsGrappling || GameManager.isPlayerInputBlocked)
            return;

        HandleScrollSwitch();
        HandleKeySwitch();
    }

    private void HandleScrollSwitch()
    {
        if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) <= 0)
            return;

        _isShooting = !_isShooting;
        ApplyWeaponMode(_isShooting);
    }

    private void HandleKeySwitch()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            _isShooting = false;
            ApplyWeaponMode(false);
        }

        if (Input.GetKey(KeyCode.Alpha2))
        {
            _isShooting = true;
            ApplyWeaponMode(true);
        }
    }

    private void ApplyWeaponMode(bool shootingMode)
    {
        weapon.SetActive(shootingMode);
        leftHandIK.data.targetPositionWeight = shootingMode ? 1f : 0.0f;
        rightHandIK.data.targetPositionWeight = shootingMode ? 1f : 0.0f;
    }

    private void ResetShootCooldown()
    {
        _canShoot = true;
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = GameManager.currentAmmo.ToString();
    }
}
