using UnityEngine;

/// <summary>
/// Централизованный сервис для рейкастов.
/// Избегает аллокаций за счёт кэширования RaycastHit.
/// </summary>
public static class RaycastService
{
    private static RaycastHit _cachedHit;

    /// <summary>
    /// Проверка рейкаста с кэшированным RaycastHit (без аллокаций).
    /// </summary>
    public static bool TryRaycast(Ray ray, out RaycastHit hit, float maxDistance = Mathf.Infinity, int layerMask = -1)
    {
        if (Physics.Raycast(ray, out _cachedHit, maxDistance, layerMask))
        {
            hit = _cachedHit;
            return true;
        }
        hit = default;
        return false;
    }

    /// <summary>
    /// Проверка рейкаста от позиции в направлении.
    /// </summary>
    public static bool TryRaycast(Vector3 origin, Vector3 direction, out RaycastHit hit, 
        float maxDistance = Mathf.Infinity, int layerMask = -1)
    {
        if (Physics.Raycast(origin, direction, out _cachedHit, maxDistance, layerMask))
        {
            hit = _cachedHit;
            return true;
        }
        hit = default;
        return false;
    }

    /// <summary>
    /// Быстрая проверка: попал ли рейкаст в тег.
    /// </summary>
    public static bool TryRaycastWithTag(Ray ray, string tag, out RaycastHit hit, 
        float maxDistance = Mathf.Infinity, int layerMask = -1)
    {
        if (TryRaycast(ray, out hit, maxDistance, layerMask) && HitHasTag(hit, tag))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Проверка рейкаста с дистанцией до цели.
    /// </summary>
    public static bool TryRaycastWithDistance(Ray ray, out RaycastHit hit, float maxDistance, 
        int layerMask = -1)
    {
        return TryRaycast(ray, out hit, maxDistance, layerMask);
    }

    /// <summary>
    /// Проверка: рейкаст попал в объект с тегом на определённой дистанции.
    /// </summary>
    public static bool TryRaycastWithTagAndDistance(Ray ray, string tag, out RaycastHit hit, 
        float maxDistance, int layerMask = -1)
    {
        if (TryRaycast(ray, out hit, maxDistance, layerMask) && 
            HitHasTag(hit, tag) && 
            Vector3.Distance(ray.origin, hit.point) <= maxDistance)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Проверка тега на объекте попадания или любом его родителе.
    /// </summary>
    public static bool HitHasTag(RaycastHit hit, string tag)
    {
        var current = hit.transform;
        while (current != null)
        {
            if (current.CompareTag(tag))
                return true;

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// Поиск компонента типа T на объекте попадания или в его родителях.
    /// </summary>
    public static bool TryGetComponentInParents<T>(RaycastHit hit, out T component) where T : Component
    {
        component = hit.collider ? hit.collider.GetComponentInParent<T>() : null;
        return component != null;
    }

    /// <summary>
    /// Рейкаст с поиском компонента типа T на попадании (включая родителей).
    /// </summary>
    public static bool TryRaycastForComponent<T>(Ray ray, out RaycastHit hit, out T component,
        float maxDistance = Mathf.Infinity, int layerMask = -1) where T : Component
    {
        component = null;
        if (!TryRaycast(ray, out hit, maxDistance, layerMask))
            return false;

        return TryGetComponentInParents(hit, out component);
    }
}
