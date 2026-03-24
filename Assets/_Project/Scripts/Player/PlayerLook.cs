using UnityEngine;

/// <summary>
/// Camera look controller for player.
/// Handles yaw/pitch, third-person camera collision, and player fade/hide by camera distance.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    private struct MaterialFadeSlot
    {
        public Material Material;
        public int ColorPropertyId;
        public Color BaseColor;
        public bool IsInTransparentMode;
    }

    private struct RendererFadeData
    {
        public Renderer Renderer;
        public bool BaseEnabled;
        public MaterialFadeSlot[] Slots;
        public bool HasUnsupportedMaterials;
    }

    [Header("References")]
    [Tooltip("Head object for vertical rotation")]
    [SerializeField] private GameObject playerHead;
    [Tooltip("Main camera transform or camera pivot")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerPause playerPause;

    [Header("Third Person Camera Collision")]
    [SerializeField] private bool enableCameraCollision = true;
    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [SerializeField, Min(0.01f)] private float cameraCollisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float cameraCollisionPadding = 0.12f;
    [SerializeField, Min(0.01f)] private float cameraCollisionSmooth = 14f;
    [SerializeField] private bool ignoreTriggerColliders = true;
    [SerializeField] private bool autoDisableLegacyCameraController = true;

    [Header("Player Visibility By Camera Distance")]
    [SerializeField] private bool enablePlayerFade = true;
    [SerializeField, Min(0.01f)] private float fadeStartDistance = 1.3f;
    [SerializeField, Min(0.01f)] private float hideDistance = 0.55f;
    [SerializeField, Range(0f, 1f)] private float minFadeAlpha = 0.3f;
    [Tooltip("Optional explicit list. If empty, renderers under player object are auto-collected.")]
    [SerializeField] private Renderer[] playerRenderers;
    [Tooltip("Optional roots to exclude from fade/hide (e.g. weapon, VFX).")]
    [SerializeField] private Transform[] fadeExcludeRoots;

    [Header("Vertical Look")]
    [SerializeField, Range(45f, 89f)] private float cameraUpLimit = 70f;
    [SerializeField, Range(45f, 89f)] private float cameraDownLimit = 70f;

    private float rotationX;
    private float rotationY;

    private Transform cameraPivotTransform;
    private Transform controlledCameraTransform;
    private Vector3 desiredCameraLocalPosition;
    private float currentCameraDistance;
    private float cameraDistanceVelocity;

    private RendererFadeData[] rendererFadeData = new RendererFadeData[0];
    private bool isPlayerFullyHidden;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");

    private void Start()
    {
        if (playerHead == null && cameraTransform == null)
            Debug.LogError("[PlayerLook] playerHead and cameraTransform are not assigned!", this);

        ResolveCameraRig();
        CachePlayerRenderersForFade();
        SetPlayerAlpha(1f);
    }

    private void ResolveCameraRig()
    {
        cameraPivotTransform = null;
        controlledCameraTransform = null;
        desiredCameraLocalPosition = Vector3.zero;
        currentCameraDistance = 0f;
        cameraDistanceVelocity = 0f;

        if (cameraTransform == null)
            return;

        Camera directCamera = cameraTransform.GetComponent<Camera>();
        if (directCamera != null)
        {
            controlledCameraTransform = cameraTransform;
            cameraPivotTransform = cameraTransform.parent;
        }
        else
        {
            cameraPivotTransform = cameraTransform;
            Camera childCamera = cameraPivotTransform.GetComponentInChildren<Camera>(true);
            if (childCamera != null)
                controlledCameraTransform = childCamera.transform;
        }

        if (controlledCameraTransform == null || cameraPivotTransform == null)
            return;

        desiredCameraLocalPosition = cameraPivotTransform.InverseTransformPoint(controlledCameraTransform.position);
        currentCameraDistance = desiredCameraLocalPosition.magnitude;

        if (autoDisableLegacyCameraController)
        {
            ThirdPersonCamera legacy = controlledCameraTransform.GetComponent<ThirdPersonCamera>();
            if (legacy != null && legacy.enabled)
                legacy.enabled = false;
        }
    }

    private void Update()
    {
        if (playerPause != null && playerPause.IsPaused)
            return;

        if (GameManager.isPlayerInputBlocked)
            return;

        float mouseY = Input.GetAxis("Mouse Y") * Settings.sensitivityVert;
        float mouseX = Input.GetAxis("Mouse X") * Settings.sensitivityHor;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -cameraUpLimit, cameraDownLimit);
        rotationY += mouseX;

        transform.localEulerAngles = new Vector3(0f, rotationY, 0f);

        if (playerHead != null)
            playerHead.transform.localEulerAngles = new Vector3(rotationX, 0f, 0f);

        if (cameraTransform != null)
            cameraTransform.localEulerAngles = new Vector3(rotationX, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (cameraPivotTransform == null || controlledCameraTransform == null)
        {
            ResolveCameraRig();
            if (cameraPivotTransform == null || controlledCameraTransform == null)
                return;
        }

        if (controlledCameraTransform == cameraPivotTransform)
            return;

        Vector3 pivotPos = cameraPivotTransform.position;
        float resultingDistance;

        if (enableCameraCollision)
        {
            Vector3 desiredWorldPos = cameraPivotTransform.TransformPoint(desiredCameraLocalPosition);
            Vector3 toDesired = desiredWorldPos - pivotPos;
            float desiredDistance = toDesired.magnitude;

            if (desiredDistance <= 0.0001f || float.IsNaN(desiredDistance) || float.IsInfinity(desiredDistance))
                return;

            Vector3 dir = toDesired / desiredDistance;
            if (float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z) ||
                float.IsInfinity(dir.x) || float.IsInfinity(dir.y) || float.IsInfinity(dir.z))
                return;

            QueryTriggerInteraction triggerMode = ignoreTriggerColliders
                ? QueryTriggerInteraction.Ignore
                : QueryTriggerInteraction.Collide;

            RaycastHit[] hits = Physics.SphereCastAll(
                pivotPos,
                cameraCollisionRadius,
                dir,
                desiredDistance,
                cameraCollisionMask,
                triggerMode
            );

            float resolvedDistance = desiredDistance;
            Transform ignoredRoot = transform.root;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null)
                    continue;

                if (ignoredRoot != null && col.transform.IsChildOf(ignoredRoot))
                    continue;

                float safeDistance = Mathf.Max(0f, hits[i].distance - cameraCollisionPadding);
                if (safeDistance < resolvedDistance)
                    resolvedDistance = safeDistance;
            }

            if (float.IsNaN(currentCameraDistance) || float.IsInfinity(currentCameraDistance))
                currentCameraDistance = resolvedDistance;

            float smoothTime = 1f / Mathf.Max(0.01f, cameraCollisionSmooth);
            currentCameraDistance = Mathf.SmoothDamp(
                currentCameraDistance,
                resolvedDistance,
                ref cameraDistanceVelocity,
                smoothTime
            );

            if (float.IsNaN(currentCameraDistance) || float.IsInfinity(currentCameraDistance))
                currentCameraDistance = resolvedDistance;

            controlledCameraTransform.position = pivotPos + dir * currentCameraDistance;
            resultingDistance = currentCameraDistance;
        }
        else
        {
            resultingDistance = Vector3.Distance(controlledCameraTransform.position, pivotPos);
        }

        ApplyPlayerVisibility(resultingDistance);
    }

    private void CachePlayerRenderersForFade()
    {
        Renderer[] source = playerRenderers != null && playerRenderers.Length > 0
            ? playerRenderers
            : transform.GetComponentsInChildren<Renderer>(true);

        bool hasExplicitRendererList = playerRenderers != null && playerRenderers.Length > 0;
        var data = new System.Collections.Generic.List<RendererFadeData>(source.Length);

        for (int i = 0; i < source.Length; i++)
        {
            Renderer r = source[i];
            if (r == null)
                continue;

            if (!hasExplicitRendererList && !r.enabled)
                continue;

            if (!hasExplicitRendererList && !r.gameObject.activeInHierarchy)
                continue;

            if (IsExcludedFromFade(r.transform))
                continue;

            Material[] materials = r.materials;
            var slots = new System.Collections.Generic.List<MaterialFadeSlot>(materials.Length);
            bool hasUnsupportedMaterials = false;

            for (int m = 0; m < materials.Length; m++)
            {
                Material mat = materials[m];
                if (mat == null)
                    continue;

                int colorId = 0;
                if (mat.HasProperty("_BaseColor")) colorId = BaseColorId;
                else if (mat.HasProperty("_Color")) colorId = ColorId;
                else if (mat.HasProperty("_TintColor")) colorId = TintColorId;
                else if (mat.HasProperty("_FaceColor")) colorId = FaceColorId;
                else
                {
                    hasUnsupportedMaterials = true;
                    continue;
                }

                slots.Add(new MaterialFadeSlot
                {
                    Material = mat,
                    ColorPropertyId = colorId,
                    BaseColor = mat.GetColor(colorId)
                });
            }

            data.Add(new RendererFadeData
            {
                Renderer = r,
                BaseEnabled = r.enabled,
                Slots = slots.ToArray(),
                HasUnsupportedMaterials = hasUnsupportedMaterials
            });
        }

        rendererFadeData = data.ToArray();
    }

    private bool IsExcludedFromFade(Transform t)
    {
        if (fadeExcludeRoots == null)
            return false;

        for (int i = 0; i < fadeExcludeRoots.Length; i++)
        {
            Transform ex = fadeExcludeRoots[i];
            if (ex != null && t.IsChildOf(ex))
                return true;
        }

        return false;
    }

    private void ApplyPlayerVisibility(float cameraDistanceToPivot)
    {
        if (!enablePlayerFade || rendererFadeData.Length == 0)
            return;

        bool shouldHide = cameraDistanceToPivot <= hideDistance;
        if (shouldHide != isPlayerFullyHidden)
        {
            isPlayerFullyHidden = shouldHide;
            SetRenderersEnabled(!shouldHide);
        }

        if (isPlayerFullyHidden)
            return;

        float alpha = 1f;
        if (cameraDistanceToPivot < fadeStartDistance)
        {
            float t = Mathf.InverseLerp(hideDistance, fadeStartDistance, cameraDistanceToPivot);
            alpha = Mathf.Lerp(minFadeAlpha, 1f, t);
        }

        SetPlayerAlpha(alpha);
    }

    private void SetRenderersEnabled(bool visible)
    {
        for (int i = 0; i < rendererFadeData.Length; i++)
        {
            RendererFadeData data = rendererFadeData[i];
            if (data.Renderer == null)
                continue;

            data.Renderer.enabled = visible && data.BaseEnabled;
        }
    }

    private void SetPlayerAlpha(float alpha)
    {
        bool transparentMode = alpha < 0.999f;

        for (int i = 0; i < rendererFadeData.Length; i++)
        {
            RendererFadeData data = rendererFadeData[i];
            if (data.Renderer == null)
                continue;

            if (!isPlayerFullyHidden && data.HasUnsupportedMaterials)
                data.Renderer.enabled = data.BaseEnabled && !transparentMode;

            for (int j = 0; j < data.Slots.Length; j++)
            {
                MaterialFadeSlot slot = data.Slots[j];
                if (slot.Material == null || slot.ColorPropertyId == 0)
                    continue;

                if (slot.IsInTransparentMode != transparentMode)
                {
                    SetMaterialTransparentMode(slot.Material, transparentMode);
                    slot.IsInTransparentMode = transparentMode;
                }

                Color c = slot.BaseColor;
                c.a = slot.BaseColor.a * alpha;
                slot.Material.SetColor(slot.ColorPropertyId, c);

                data.Slots[j] = slot;
            }

            rendererFadeData[i] = data;
        }
    }

    private static void SetMaterialTransparentMode(Material mat, bool transparent)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", transparent ? 1f : 0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", transparent ? 0f : 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            if (transparent)
            {
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = -1;
            }
        }

        if (mat.HasProperty("_Mode"))
        {
            if (transparent)
            {
                mat.SetFloat("_Mode", 2f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Mode", 0f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }
        }
    }

    private void OnDestroy()
    {
        SetPlayerAlpha(1f);
        isPlayerFullyHidden = false;
        SetRenderersEnabled(true);
    }
}
