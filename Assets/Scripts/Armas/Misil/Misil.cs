using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Misil", fileName = "Misil")]
public class PersistentMisilEffect : ScriptableObject, IPersistentEffect, IEffect
{
    [Header("Spawn Settings")]
    public float spawnInterval = 5f;

    [Header("Prefabs")]
    public GameObject bombPrefab;
    public GameObject explosionPrefab;

    [Header("Posición relativa al viewport (0-1)")]
    [Range(0f, 1f)] public float spawnHeightPercent = 0.8f;
    [Range(0f, 1f)] public float spawnXPercent = 0f;
    [Range(0f, 1f)] public float fallHeightPercent = 0.2f;
    [Range(0f, 1f)] public float fallXPercent = 1f;

    [Header("Movimiento")]
    public float fallSpeed = 6f;

    [Header("Pooling")]
    public int poolSize = 10;

    private Coroutine activeCoroutine;
    private Queue<GameObject> missilePool = new Queue<GameObject>();

    // ---------------- IPersistentEffect ----------------
    public void ApplyTo(GameObject player)
    {
        if (CoroutineRunner.Instance == null || bombPrefab == null) return;

        if (activeCoroutine != null) return; // Ya está corriendo

        InitializePool();

        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(MissileLoopRoutine());
    }

    public void RemoveFrom(GameObject player)
    {
        if (activeCoroutine != null && CoroutineRunner.Instance != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    // ---------------- IEffect ----------------
    public void Execute(Vector2 position, GameObject owner = null)
    {
        // No hace nada: los misiles caen automáticamente
    }

    // ---------------- Loop infinito ----------------
    private IEnumerator MissileLoopRoutine()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        while (true)
        {
            Debug.Log("Spawn missile!"); // Para verificar que se repite
            SpawnOneMissile(cam);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ---------------- Pool ----------------
    private void InitializePool()
    {
        if (missilePool.Count > 0) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject missile = Instantiate(bombPrefab);
            missile.SetActive(false);
            missilePool.Enqueue(missile);
        }
    }

    private GameObject GetMissileFromPool()
    {
        if (missilePool.Count == 0)
        {
            GameObject missile = Instantiate(bombPrefab);
            missile.SetActive(false);
            return missile;
        }
        return missilePool.Dequeue();
    }

    private void ReturnMissileToPool(GameObject missile)
    {
        if (missile.TryGetComponent<MisilProjectile>(out var mp))
            mp.ResetMissile();

        missile.SetActive(false);
        missilePool.Enqueue(missile);
    }

    // ---------------- MISIL ----------------
    public void SpawnOneMissile(Camera cam)
    {
        if (cam == null || bombPrefab == null) return;

        float camDist = Mathf.Abs(cam.transform.position.z);

        Vector3 spawnPos = cam.ViewportToWorldPoint(new Vector3(spawnXPercent, spawnHeightPercent, camDist));
        spawnPos.z = 0;

        Vector3 targetPos = cam.ViewportToWorldPoint(new Vector3(fallXPercent, fallHeightPercent, camDist));
        targetPos.z = 0;

        GameObject missile = GetMissileFromPool();
        missile.transform.position = spawnPos;
        missile.transform.rotation = bombPrefab.transform.rotation;
        missile.SetActive(true);

        Rigidbody2D rb = missile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = (targetPos - spawnPos).normalized;
            rb.linearVelocity = dir * fallSpeed;
        }
        else
        {
            CoroutineRunner.Instance.StartCoroutine(MoveMissileManually(missile.transform, targetPos, fallSpeed));
        }

        if (missile.TryGetComponent<MisilProjectile>(out var bp))
        {
            bp.SetTarget(targetPos);
            bp.explosionPrefab = explosionPrefab;
            bp.OnExplode += () => ReturnMissileToPool(missile);
        }
    }

    private IEnumerator MoveMissileManually(Transform missile, Vector3 target, float speed)
    {
        while (missile != null && Vector3.Distance(missile.position, target) > 0.05f)
        {
            missile.position = Vector3.MoveTowards(missile.position, target, speed * Time.deltaTime);
            yield return null;
        }

        if (missile != null)
            ReturnMissileToPool(missile.gameObject);
    }
}
