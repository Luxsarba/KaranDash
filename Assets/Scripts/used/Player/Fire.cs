using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Fire : MonoBehaviour
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



    public bool OK = true;

    public AudioSource fireSound, punchSound;

    private Ray ray;
    private RaycastHit hit;
    private Animator _animator;
    private bool is_shooting = true;

    public void SetOK()
    {
        OK = true;
        if (is_shooting)
        {
            weapon.SetActive(true);
            leftHandIK.data.targetPositionWeight = 1.0f;
            rightHandIK.data.targetPositionWeight = 1.0f;
        }
    }

    private void Punch(bool pressed = false)
    {
        if (Physics.Raycast(ray, out hit, shootDistance, shootMask) &&
            hit.transform.CompareTag("Enemy") &&
            Vector3.Distance(transform.position, hit.transform.position) <= 2.5f)
        {
            leftHandIK.data.targetPositionWeight = 0.0f;
            rightHandIK.data.targetPositionWeight = 0.0f;
            weapon.SetActive(false);
            _animator.Play("Punch");
            punchSound.Play();      
            hit.transform.GetComponent<Enemy>().Damage(10);
        }
    }

    void Start()
    {
        OK = true;
        _animator = GetComponentInChildren<Animator>();
        ammoText.text = GameManager.currentAmmo.ToString();
    }

    void Update()
    {
        if (Input.GetMouseButton(0) &&
            !GameManager.isPlayerInputBlocked &&
            OK)
        {
            OK = false;
            Invoke("SetOK", fireInterval);
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (GameManager.currentAmmo <= 0 ||
                !is_shooting)
            {
                if (Physics.Raycast(ray, out hit, shootDistance, shootMask) &&
                    hit.transform.CompareTag("Enemy"))
                {
                    Punch();
                }
            }
            else
            {
                if (shootVfx)
                    shootVfx.Play();

                GameManager.currentAmmo--;
                fireSound.Play();

                ammoText.text = GameManager.currentAmmo.ToString();
                
                if (Physics.Raycast(ray, out hit, shootDistance, shootMask))
                {
                    Debug.Log($"Игрок попал в объект с тэгом {hit.transform.tag}");
                    if (hit.transform.CompareTag("Enemy"))
                    {
                        var enemy = hit.transform.GetComponent<Enemy>();
                        if (enemy)
                            enemy.Damage(5);
                    }
                }
            }
        }

        if (Input.GetKey(KeyCode.V) && OK)
        {
            OK = false;
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Invoke("SetOK", fireInterval);
            Punch(false);
        }

        if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0)
        {
            if (!is_shooting)
            {
                is_shooting = true;
                weapon.SetActive(true);
                leftHandIK.data.targetPositionWeight = 1f;
                rightHandIK.data.targetPositionWeight = 1f;
            }
            else
            {
                is_shooting = false;
                weapon.SetActive(false);
                leftHandIK.data.targetPositionWeight = 0.0f;
                rightHandIK.data.targetPositionWeight = 0.0f;
            }
        }

        if (Input.GetKey(KeyCode.Alpha1))
        {
            is_shooting = false;
            weapon.SetActive(false);
            leftHandIK.data.targetPositionWeight = 0.0f;
            rightHandIK.data.targetPositionWeight = 0.0f;
        }

        if (Input.GetKey(KeyCode.Alpha2))
        {
            is_shooting = true;
            weapon.SetActive(true);
            leftHandIK.data.targetPositionWeight = 1f;
            rightHandIK.data.targetPositionWeight = 1f;
        }
    }
}
