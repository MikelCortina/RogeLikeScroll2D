using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/BolaDeGoma", fileName = "BolaDeGoma")]
public class BolaDeGoma : ScriptableObject, IPersistentEffect
{
    [Header("Prefab & timings")]
    [SerializeField] private GameObject projectilePrefab;       // Ahora debe tener el script BolaGoma
    [SerializeField] private float lifetime = 8f;

    [Header("Disparo")]
    [SerializeField] private int projectilesPerShot = 3;
    [SerializeField] private float spawnDelay = 0.1f;
    [SerializeField] private float respawnDelay = 2.5f;         // Tiempo entre ráfagas

    [Header("Bola de goma")]
    [SerializeField] private int maxBounces = 5;              // Cuántas veces rebota antes de desaparecer
    [SerializeField] private float launchForce = 12f;           // Velocidad de lanzamiento
    [SerializeField] private Vector2 localFireOffset = new Vector2(0, 0.5f);

    [Header("Pooling")]
    [SerializeField] private int poolSize = 30;

    // Runtime
    private Coroutine activeCoroutine;
    private GameObject runtimeOwner;
    private Queue<GameObject> projectilePool = new Queue<GameObject>();

    #region IPersistentEffect
    public void ApplyTo(GameObject owner)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[BolaDeGoma] projectilePrefab no asignado.");
            return;
        }

        if (owner == null) return;

        if (activeCoroutine != null) return;

        runtimeOwner = owner;
        InitializeProjectilePool();
        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(ShootRoutine());
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
        ApplyTo(owner ? owner : runtimeOwner);
    }
    #endregion

    private IEnumerator ShootRoutine()
    {
        while (runtimeOwner != null)
        {
            float spawnRate = StatsManager.Instance.RuntimeStats.spawnRate;
            if (spawnRate <= 0f)
            {
                yield return null;
                continue;
            }

            float burstInterval = respawnDelay / spawnRate;

            // Dispara varias bolas en rápida sucesión
            for (int i = 0; i < projectilesPerShot; i++)
            {
                if (runtimeOwner == null) break;

                Vector2 firePosition = (Vector2)runtimeOwner.transform.position + localFireOffset;
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                // Dirección hacia el cursor
                Vector2 direction = mouseWorldPos - firePosition;
                direction.Normalize();

                SpawnRubberBall(firePosition, direction);

                if (spawnDelay > 0f && i < projectilesPerShot - 1)
                    yield return new WaitForSeconds(spawnDelay);
            }

            yield return new WaitForSeconds(burstInterval);
        }

        activeCoroutine = null;
    }

    private void SpawnRubberBall(Vector2 position, Vector2 direction)
    {
        GameObject ball = GetProjectileFromPool();
        if (ball == null) return;

        ball.transform.position = position;
        ball.transform.rotation = Quaternion.identity;
        ball.SetActive(true);

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        // Configurar el componente BolaGoma
        var bolaScript = ball.GetComponent<BolaGoma>();
        if (bolaScript != null)
        {
            bolaScript.Inicializar(maxBounces, lifetime, this);
        }

        // Aplicar velocidad hacia el cursor
        if (rb != null)
        {
            rb.linearVelocity = direction * launchForce;
        }

        // Opcional: rotar el sprite hacia la dirección
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ball.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90f si tu sprite mira hacia arriba
    }

    #region Pooling
    private void InitializeProjectilePool()
    {
        if (projectilePool.Count > 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Object.Instantiate(projectilePrefab);
            obj.SetActive(false);
            projectilePool.Enqueue(obj);
        }
    }

    private GameObject GetProjectileFromPool()
    {
        if (projectilePool.Count == 0)
        {
            GameObject obj = Object.Instantiate(projectilePrefab);
            obj.SetActive(false);
            return obj;
        }
        return projectilePool.Dequeue();
    }

    public void ReturnToPool(GameObject obj) // llamado desde BolaGoma
    {
        obj.SetActive(false);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        var bolaScript = obj.GetComponent<BolaGoma>();
        if (bolaScript != null)
        {
            bolaScript.ResetValues();
        }

        projectilePool.Enqueue(obj);
    }
    #endregion

    public void ResetRuntime()
    {
        // Detener la coroutine principal
        if (activeCoroutine != null && CoroutineRunner.Instance != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        // Desactivar todos los proyectiles activos en la escena
        foreach (var ball in projectilePool)
        {
            if (ball != null)
            {
                ball.SetActive(false);

                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = false;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                var bolaScript = ball.GetComponent<BolaGoma>();
                if (bolaScript != null)
                {
                    bolaScript.ResetValues();
                }
            }
        }

        // Limpiar la cola del pool
        projectilePool.Clear();

        // Limpiar owner
        runtimeOwner = null;
    }

}