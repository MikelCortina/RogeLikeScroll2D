using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Bomba", fileName = "Bomba")]
public class Bomba : ScriptableObject, IPersistentEffect
{
    [Header("Prefab & timings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float lifetime = 6f;

    [Header("Burst / spawn settings")]
    [SerializeField] private int projectilesPerShot = 5;
    [SerializeField] private float spawnDelay = 0.15f;
    [SerializeField] private float respawnDelay = 4f;

    [Header("Parábola & posición")]
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private float randomRadius = 3f;
    [Tooltip("Offset local desde el centro del owner. Recomendado un pequeño Y positivo para evitar colisión inicial.")]
    [SerializeField] private Vector2 localFireOffset = new Vector2(0f, 0.8f); // ← Cambiado para evitar spawn dentro del jugador

    [Header("Pooling")]
    [SerializeField] private int poolSize = 30;

    // runtime
    private Coroutine activeCoroutine;
    private GameObject runtimeOwner;
    private Queue<GameObject> projectilePool = new Queue<GameObject>();

    #region IPersistentEffect / IEffect
    public void ApplyTo(GameObject owner)
    {
        if (projectilePrefab == null) return;
        if (owner == null) return;

        // 🔑 owner murió → reinicio completo
        if (runtimeOwner == null || runtimeOwner != owner)
        {
            ResetRuntime();
        }

        if (activeCoroutine != null) return;

        runtimeOwner = owner;
        InitializeProjectilePool();
        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(ShootParabolicFan());
    }

    public void ResetRuntime()
    {
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        runtimeOwner = null;

        while (projectilePool.Count > 0)
        {
            var p = projectilePool.Dequeue();
            if (p != null) Object.Destroy(p);
        }
    }

    public void RemoveFrom(GameObject owner)
    {
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        runtimeOwner = null;
    }

    public void Execute(Vector2 position, GameObject owner = null)
    {
        if (owner != null)
            ApplyTo(owner);
        else if (runtimeOwner != null)
            ApplyTo(runtimeOwner);
        else
            Debug.LogWarning("[Bomba] Execute llamado sin owner disponible.");
    }
    #endregion

    #region Coroutine & spawn
    private IEnumerator ShootParabolicFan()
    {
        while (runtimeOwner != null)
        {
            float spawnRate = StatsManager.Instance.RuntimeStats.spawnRate;
            if (spawnRate <= 0f)
            {
                yield return null;
                continue;
            }

            float burstIntervalDynamic = respawnDelay / spawnRate;

            for (int i = 0; i < projectilesPerShot; i++)
            {
                if (runtimeOwner == null) break;

                Vector2 origin = (Vector2)runtimeOwner.transform.position + localFireOffset;
                Vector2 ownerPos = (Vector2)runtimeOwner.transform.position;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(0f, randomRadius);
                Vector2 randomOffset = new Vector2(Mathf.Cos(angle) * dist, 0f);
                Vector2 targetPos = ownerPos + randomOffset;

                SpawnParabolicProjectile(origin, targetPos, arcHeight);

                if (spawnDelay > 0f)
                    yield return new WaitForSeconds(spawnDelay);
                else
                    yield return null;
            }

            yield return new WaitForSeconds(burstIntervalDynamic);
        }
        activeCoroutine = null;
    }

    private void SpawnParabolicProjectile(Vector2 origin, Vector2 targetPos, float arcHeightAboveOrigin)
    {
        GameObject proj = GetProjectileFromPool();
        if (proj == null) return;

        proj.SetActive(true);
        proj.transform.position = origin;
        proj.transform.rotation = Quaternion.identity;

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = Vector2.zero;
            prb.angularVelocity = 0f;
            prb.simulated = true;
        }

        var projScript = proj.GetComponent<BombProjectile>();
        if (projScript != null)
        {
            projScript.owner = runtimeOwner;
            projScript.OnExplode += () => ReturnProjectileToPool(proj);
        }

        // Cálculo de parábola
        float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, prb.gravityScale);
        float dx = targetPos.x - origin.x;
        float dy = targetPos.y - origin.y;
        float apexAboveOrigin = Mathf.Max(arcHeightAboveOrigin, dy + 0.5f);

        float vy = Mathf.Sqrt(2f * g * apexAboveOrigin);
        float tUp = vy / g;
        float tDown = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * (apexAboveOrigin - dy) / g));
        float totalTime = tUp + tDown;
        float vx = (Mathf.Abs(totalTime) > 1e-6f) ? dx / totalTime : 0f;

        prb.linearVelocity = new Vector2(vx, vy);

        float angle = Mathf.Atan2(vy, vx) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    #endregion

    #region Pooling
    private void InitializeProjectilePool()
    {
        if (projectilePool.Count > 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject proj = Object.Instantiate(projectilePrefab, Vector3.zero, Quaternion.identity);
            proj.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private GameObject GetProjectileFromPool()
    {
        if (projectilePool.Count == 0)
        {
            GameObject proj = Object.Instantiate(projectilePrefab, Vector3.zero, Quaternion.identity);
            proj.SetActive(false);
            return proj;
        }
        return projectilePool.Dequeue();
    }

    private void ReturnProjectileToPool(GameObject proj)
    {
        proj.SetActive(false);

        if (proj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        var projScript = proj.GetComponent<BombProjectile>();
        if (projScript != null)
        {
            projScript.OnExplode -= () => ReturnProjectileToPool(proj);
        }

        projectilePool.Enqueue(proj);
    }
    #endregion
}