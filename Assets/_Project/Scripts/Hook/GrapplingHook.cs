using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrapplingHook : MonoBehaviour
{
    [Header("PlayerWeapon Integration (drag PlayerWeapon script)")]
    [SerializeField] private PlayerWeapon weapon;

    [Header("Grapple Settings")]
    [SerializeField] private LayerMask grappleLayer = 1 << 8;
    [SerializeField, Min(10f)] private float maxGrappleDistance = 60f;
    [SerializeField, Min(10f)] private float pullForce = 35f;
    [SerializeField] private float minDistanceToStop = 1.5f;
    [SerializeField, Min(1f)] private float grappleImpulseForce = 8f;
    [SerializeField, Range(0f, 1f)] private float verticalPullMultiplier = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxVerticalToHorizontalRatio = 0.5f;

    [Header("Visual")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private MeshFilter tapeMeshFilter;
    [SerializeField] private MeshRenderer tapeMeshRenderer;
    [SerializeField, Min(0.01f)] private float tapeWidth = 0.12f;
    [SerializeField, Min(0.001f)] private float tapeThickness = 0.015f;

    [Header("Tape Ends Prefabs")]
    [SerializeField] private GameObject tapeStartPrefab;
    [SerializeField] private GameObject tapeEndPrefab;

    [Header("Texture Tiling")]
    [SerializeField, Min(0.01f)] private float tileSizeMeters = 1f;

    [Header("Camera & Anim")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private string grappleShootTrigger = "GrappleShoot";
    [SerializeField] private string isGrapplingParam = "IsGrappling";

    [Header("Fixed Orientation")]
    [Tooltip("Фиксированный Up для ленты — чтобы поворот всегда был одним (игнорируем normal поверхности)")]
    [SerializeField] private Vector3 fixedUpDirection = Vector3.up;

    [Tooltip("Допустимый угол наклона ленты (от 0 до 90) — если поверхность круче, используем fixedUpDirection")]
    [SerializeField, Range(0f, 90f)] private float maxAllowedNormalAngle = 45f;

    private Rigidbody rb;
    private bool isGrappling;
    private Vector3 grapplePoint;
    private Vector3 fixedWidthDir;

    private GameObject currentStartVisual;
    private GameObject currentEndVisual;

    private Transform tapeTransform;
    private Mesh tapeMesh;

    // Кэшированные массивы для mesh (избегаем аллокаций в Update)
    private readonly Vector3[] _vertices = new Vector3[8];
    private readonly Vector2[] _uvs = new Vector2[8];
    private readonly int[] _triangles = {
        0,2,4,  4,2,6,
        1,5,3,  3,5,7,
        0,4,1,  1,4,5,
        2,3,6,  6,3,7,
        0,1,4,  4,1,5,
        2,6,3,  3,6,7
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!tapeMeshFilter)
        {
            Debug.LogError("[GrapplingHook] TapeMeshFilter не назначен!", this);
            enabled = false;
            return;
        }

        if (!playerCamera)
        {
            Debug.LogError("[GrapplingHook] PlayerCamera не назначена!", this);
            enabled = false;
            return;
        }

        tapeTransform = tapeMeshFilter.transform;
        tapeMesh = new Mesh { name = "ProceduralTape" };
        tapeMeshFilter.mesh = tapeMesh;
        tapeMeshRenderer.enabled = false;

        if (tapeStartPrefab)
        {
            currentStartVisual = Instantiate(tapeStartPrefab, Vector3.zero, Quaternion.identity);
            currentStartVisual.transform.SetParent(transform, false);
            currentStartVisual.SetActive(false);
        }

        if (tapeEndPrefab)
        {
            currentEndVisual = Instantiate(tapeEndPrefab, Vector3.zero, Quaternion.identity);
            currentEndVisual.transform.SetParent(transform, false);
            currentEndVisual.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && !isGrappling)
            StartGrapple();

        if (Input.GetMouseButtonUp(1) && isGrappling)
            StopGrapple();

        if (isGrappling)
        {
            UpdateTape();

            Vector3 toTarget = grapplePoint - transform.position;
            float distance = toTarget.magnitude;
            if (distance > minDistanceToStop)
            {
                rb.AddForce(BuildPullForce(toTarget), ForceMode.Force);
            }
            else
            {
                StopGrapple();
            }
        }
    }

    private void StartGrapple()
    {
        if (!weapon)
        {
            Debug.LogError("[GrapplingHook] PlayerWeapon скрипт не назначен!", this);
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (RaycastService.TryRaycast(ray, out var hit, maxGrappleDistance, grappleLayer.value))
        {
            if (!RaycastService.HitHasTag(hit, "GrapplePoint"))
                return;

            weapon.EnterGrappleMode();

            grapplePoint = hit.point;

            // Проверяем угол нормали — если круче maxAllowedNormalAngle, используем fixedUpDirection
            float normalAngle = Vector3.Angle(hit.normal, fixedUpDirection);
            Vector3 effectiveUp = (normalAngle <= maxAllowedNormalAngle) ? hit.normal : fixedUpDirection;

            // Фиксируем направление ширины на основе effectiveUp
            Vector3 initialLineDir = (transform.position - grapplePoint).normalized;
            fixedWidthDir = Vector3.Cross(initialLineDir, effectiveUp).normalized;

            isGrappling = true;
            tapeMeshRenderer.enabled = true;

            animator.SetTrigger(grappleShootTrigger);
            animator.SetBool(isGrapplingParam, true);

            // Включаем и перемещаем визуальные эффекты
            if (currentStartVisual)
            {
                currentStartVisual.SetActive(true);
            }

            if (currentEndVisual)
            {
                currentEndVisual.SetActive(true);
            }

            rb.AddForce(ray.direction * grappleImpulseForce, ForceMode.Impulse);
        }
    }

    private void StopGrapple()
    {
        isGrappling = false;
        tapeMeshRenderer.enabled = false;

        animator.SetBool(isGrapplingParam, false);

        if (currentStartVisual) currentStartVisual.SetActive(false);
        if (currentEndVisual) currentEndVisual.SetActive(false);

        weapon?.ExitGrappleMode();
        weapon?.SetReady();
    }

    private void UpdateTape()
    {
        Vector3 handPos = firePoint ? firePoint.position : transform.position;
        Vector3 anchorPos = grapplePoint;

        Vector3 lineDir = (handPos - anchorPos).normalized;
        Vector3 upDir = Vector3.Cross(fixedWidthDir, lineDir).normalized;

        // Обновляем позицию и вращение ленты
        float length = Vector3.Distance(anchorPos, handPos);
        float halfLength = length * 0.5f;
        Vector3 center = (handPos + anchorPos) * 0.5f;

        tapeTransform.position = center;
        tapeTransform.rotation = Quaternion.LookRotation(lineDir, upDir);

        // Обновляем меш
        UpdateTapeMesh(length, halfLength);

        // Обновляем визуальные эффекты
        if (currentStartVisual)
        {
            currentStartVisual.transform.position = handPos;
            currentStartVisual.transform.rotation = Quaternion.LookRotation((anchorPos - handPos).normalized, upDir);
        }

        if (currentEndVisual)
        {
            currentEndVisual.transform.position = anchorPos;
            currentEndVisual.transform.rotation = Quaternion.LookRotation((handPos - anchorPos).normalized, upDir);
        }
    }

    private void UpdateTapeMesh(float length, float halfLength)
    {
        float halfWidth = tapeWidth * 0.5f;
        float halfThick = tapeThickness * 0.5f;

        // Заполняем кэшированные массивы
        _vertices[0] = new Vector3( halfWidth,  halfThick, -halfLength);
        _vertices[1] = new Vector3( halfWidth, -halfThick, -halfLength);
        _vertices[2] = new Vector3(-halfWidth,  halfThick, -halfLength);
        _vertices[3] = new Vector3(-halfWidth, -halfThick, -halfLength);
        _vertices[4] = new Vector3( halfWidth,  halfThick,  halfLength);
        _vertices[5] = new Vector3( halfWidth, -halfThick,  halfLength);
        _vertices[6] = new Vector3(-halfWidth,  halfThick,  halfLength);
        _vertices[7] = new Vector3(-halfWidth, -halfThick,  halfLength);

        float repeatCount = length / tileSizeMeters;
        for (int i = 0; i < 8; i++)
        {
            float u = ((_vertices[i].z + halfLength) / length) * repeatCount;
            float v = (_vertices[i].x + halfWidth) / tapeWidth;
            _uvs[i] = new Vector2(u, v);
        }

        tapeMesh.Clear();
        tapeMesh.vertices = _vertices;
        tapeMesh.uv = _uvs;
        tapeMesh.triangles = _triangles;
        tapeMesh.RecalculateNormals();
        tapeMesh.RecalculateTangents();
        tapeMesh.RecalculateBounds();
    }

    private Vector3 BuildPullForce(Vector3 toTarget)
    {
        Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDistance = horizontal.magnitude;

        Vector3 horizontalDir = horizontalDistance > 0.001f ? horizontal / horizontalDistance : Vector3.zero;
        float horizontalForce = pullForce;
        float verticalForce = pullForce * verticalPullMultiplier;

        // Не даем вертикали "перетягивать" движение и полностью подавлять XZ.
        if (horizontalDistance > 0.001f)
        {
            verticalForce = Mathf.Min(verticalForce, horizontalForce * maxVerticalToHorizontalRatio);
        }

        float verticalSign = Mathf.Sign(toTarget.y);
        Vector3 totalForce = (horizontalDir * horizontalForce) + (Vector3.up * verticalSign * verticalForce);
        return totalForce * rb.mass;
    }

    private void OnDestroy()
    {
        // Очищаем визуальные эффекты при уничтожении объекта
        if (currentStartVisual) Destroy(currentStartVisual);
        if (currentEndVisual) Destroy(currentEndVisual);
    }
}
