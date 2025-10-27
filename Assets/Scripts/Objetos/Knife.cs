using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Spawn Two Projectiles Persistent", fileName = "SpawnTwoProjectilesPersistentEffect")]
public class SpawnTwoProjectilesEffect : ScriptableObject, IPersistentEffect
{
    [Header("Configuración del proyectil")]
    public GameObject projectilePrefab;
    public float speed = 8f;
    public float lifetime = 4f;

    [Header("Control de spawn")]
    public float spawnInterval = 2f;
    public Vector2 localSpawnOffset = Vector2.zero;
    public Vector2 localForward = Vector2.right;

    // Mantener referencia a la Coroutine para detenerla
    private Coroutine activeCoroutine;

    /// <summary>
    /// Se llama al aplicar el efecto persistentemente al jugador (caballo)
    /// </summary>
    public void ApplyTo(GameObject owner)
    {
        if (projectilePrefab == null || owner == null)
        {
            Debug.LogWarning("[SpawnTwoProjectilesEffect] Prefab o owner no asignado.");
            return;
        }

        // Evita iniciar múltiples Coroutines
        if (activeCoroutine != null) return;

        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(SpawnLoop(owner));
    }

    /// <summary>
    /// Desactiva el efecto persistentemente
    /// </summary>
    public void RemoveFrom(GameObject owner)
    {
        if (activeCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop(GameObject owner)
    {
        while (true)
        {
            SpawnPair(owner);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPair(GameObject owner)
    {
        Vector2 spawnPos = (Vector2)owner.transform.position + localSpawnOffset;
        Vector2 dirForward = owner.transform.right.normalized;
        Vector2 dirBackward = -dirForward;
        Quaternion rotation = owner.transform.rotation;

        // Proyectil adelante
        SpawnProjectile(spawnPos, dirForward, rotation);

        // Proyectil atrás
        SpawnProjectile(spawnPos, dirBackward, rotation);
    }

    private void SpawnProjectile(Vector2 position, Vector2 direction, Quaternion rotation)
    {
        GameObject projectile = Instantiate(projectilePrefab, position, rotation);
        if (projectile.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = direction * speed;
        else
            Debug.LogWarning("[SpawnTwoProjectilesEffect] prefab no tiene Rigidbody2D.");

        Destroy(projectile, lifetime);
    }

    /// <summary>
    /// Este efecto no usa Execute directamente, pero lo implementamos por la interfaz IEffect
    /// </summary>
    public void Execute(Vector2 position, GameObject owner = null)
    {
        ApplyTo(owner);
    }
}
