using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Persistent/BombGasSpawnerPersistentEffect", fileName = "BombGasSpawnerPersistentEffect")]
public class BombGasSpawnerPersistentEffect : ScriptableObject, IPersistentEffect
{
    [Header("Prefabs")]
    public GameObject bombProjectilePrefab; // Debe contener BombProjectile
    public GameObject gasCloudPrefab;       // Debe contener GasCloud y (opcional) SpriteRenderer

    [Header("Spawn automático")]
    public float spawnInterval = 3f;        // cada cuanto tiempo se lanza otra bomba
    public Vector2 spawnOffset = new Vector2(0.3f, 0.5f); // offset relativo al owner
    public Vector2 forward = Vector2.right; // dirección relativa local al lanzar

    [Header("Lanzamiento parabólico")]
    public float minDistance = 3f;
    public float maxDistance = 6f;
    public float travelTime = 0.8f;
    public float arcHeight = 2f;
    public float explosionDelay = 0f;

    [Header("Gas (fallback si el prefab no define radio por sprite)")]
    public float defaultGasRadius = 2.5f;
    public float gasDuration = 5f;
    public float gasDamagePerSecond = 10f;

    [Header("Comportamiento manual")]
    [Tooltip("Si true, llamar Execute(...) lanzará una bomba inmediatamente. Si false, Execute no hace nada.")]
    public bool allowManualLaunch = false;

    // runtime
    private GameObject runtimeOwner;
    private Coroutine activeCoroutine;


    #region IPersistentEffect

    public void ApplyTo(GameObject owner)
    {
        if (owner == null)
        {
            Debug.LogWarning("[BombSpawnerPersistentEffect] ApplyTo: owner null.");
            return;
        }

        if (bombProjectilePrefab == null || gasCloudPrefab == null)
        {
            Debug.LogWarning("[BombSpawnerPersistentEffect] Prefabs no asignados.");
            return;
        }

        if (activeCoroutine != null)
        {
            // ya activo
            return;
        }

        runtimeOwner = owner;
        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(SpawnLoop());
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

    // compatibilidad IEffect
    public void Execute(Vector2 dir, GameObject owner)
    {
        if (!allowManualLaunch)
        {
            // No lanzar cuando se llama Execute (por ejemplo, al click).
            return;
        }

        var useOwner = owner ?? runtimeOwner;
        if (useOwner == null)
        {
            Debug.LogWarning("[BombSpawnerPersistentEffect] Execute sin owner.");
            return;
        }

        LaunchBomb(dir, useOwner);
    }
    #endregion

    #region Spawn loop & launch

    private IEnumerator SpawnLoop()
    {
        while (runtimeOwner != null)
        {
            LaunchBomb(forward, runtimeOwner);

            float t = 0f;
            while (t < spawnInterval)
            {
                t += Time.deltaTime;
                if (runtimeOwner == null) break;
                yield return null;
            }
        }

        activeCoroutine = null;
    }

    private void LaunchBomb(Vector2 dir, GameObject owner)
    {
        if (bombProjectilePrefab == null || gasCloudPrefab == null || owner == null) return;

        float distance = Random.Range(minDistance, maxDistance);
        Vector3 ownerPos = owner.transform.position;
        Vector3 direction = dir.sqrMagnitude == 0 ? Vector2.right : dir.normalized;

        Vector3 spawnPos = ownerPos + (Vector3)spawnOffset;
        Vector3 targetPos = ownerPos + (Vector3)(direction * distance);

        GameObject go = Object.Instantiate(bombProjectilePrefab, spawnPos, Quaternion.identity);
        BombGasProjectile proj = go.GetComponent<BombGasProjectile>();
        if (proj == null)
        {
            Debug.LogError("[BombSpawnerPersistentEffect] bombProjectilePrefab no contiene BombProjectile.");
            Object.Destroy(go);
            return;
        }

        proj.Initialize(
            owner: owner,
            startPos: spawnPos,
            targetPos: targetPos,
            travelTime: travelTime,
            arcHeight: arcHeight,
            explosionDelay: explosionDelay,
            gasCloudPrefab: gasCloudPrefab,
            gasRadius: defaultGasRadius,
            gasDuration: gasDuration,
            gasDamagePerSecond: gasDamagePerSecond
        );
    }

    #endregion
}
