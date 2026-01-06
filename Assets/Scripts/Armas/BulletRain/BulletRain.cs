using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BulletRainEffect",
    menuName = "Effects/Persistent/Bullet Rain Effect",
    order = 1)]
public class BulletRainEffect : ScriptableObject, IPersistentEffect, IEffect
{
    [Header("Configuración")]
    public GameObject bulletPrefab;
    public float spawnInterval = 0.5f;
    public int bulletsPerWave = 50;
    public float bulletSpeed = 8f;
    public float spawnHeightOffset = 2f;

    [Header("Lluvia")]
    public float verticalSpread = 3f; // rango vertical de la lluvia


    public enum SpawnOriginType { Center, Left, Right, Custom }
    public SpawnOriginType spawnOrigin = SpawnOriginType.Center;
    [Range(0f, 1f)] public float customOriginNormalized = 0.5f;
    public float waveWidth = 4f;

    [Header("Pooling")]
    public int poolSize = 100;

    [Header("Duración (0 = infinito)")]
    public float durationIfInstant = 0f;

    private Coroutine activeCoroutine;
    private readonly Queue<GameObject> bulletPool = new Queue<GameObject>();

    public static BulletRainEffect Instance { get; private set; }

    // ---------------- IEffect ----------------
    public void Execute(Vector2 position, GameObject owner = null)
    {
        GameObject target = owner ?? GameObject.FindGameObjectWithTag("Player");
        ApplyTo(target);

    }



    // ---------------- IPersistentEffect ----------------
    public void ApplyTo(GameObject player)
    {
        if (CoroutineRunner.Instance == null || bulletPrefab == null)
            return;

        if (activeCoroutine != null)
            return;

        Instance = this;
        InitializePool();

        activeCoroutine = CoroutineRunner.Instance.StartCoroutine(BulletRainLoop());
    }

    public void RemoveFrom(GameObject player)
    {
        if (activeCoroutine != null && CoroutineRunner.Instance != null)
        {
            CoroutineRunner.Instance.StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    public void ResetRuntime()
    {
        RemoveFrom(null);
        bulletPool.Clear();
    }

    // ---------------- Pool ----------------
    private void InitializePool()
    {
        if (bulletPool.Count > 0)
            return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    private GameObject GetBulletFromPool()
    {
        if (bulletPool.Count == 0)
        {
            GameObject extra = Instantiate(bulletPrefab);
            extra.SetActive(false);
            return extra;
        }

        return bulletPool.Dequeue();
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        if (bullet == null) return;

        if (bullet.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = Vector2.zero;

        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }

    // ---------------- Loop ----------------
    private IEnumerator BulletRainLoop()
    {
        float spawnRate = StatsManager.Instance.RuntimeStats.spawnRate;
        while (true)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                SpawnWave(cam);
            }

            yield return new WaitForSeconds(spawnInterval/ spawnRate);
        }
    }

    private void SpawnWave(Camera cam)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float spawnY = cam.transform.position.y + halfHeight + spawnHeightOffset;

        float spawnX = spawnOrigin switch
        {
            SpawnOriginType.Left => cam.transform.position.x - halfWidth,
            SpawnOriginType.Right => cam.transform.position.x + halfWidth,
            SpawnOriginType.Custom => Mathf.Lerp(
                cam.transform.position.x - halfWidth,
                cam.transform.position.x + halfWidth,
                customOriginNormalized),
            _ => cam.transform.position.x
        };

        Vector3 center = new Vector3(spawnX, spawnY, 0f);

        for (int i = 0; i < bulletsPerWave; i++)
        {
            float offsetX = Random.Range(-waveWidth * 0.5f, waveWidth * 0.5f);

            // NUEVO: altura aleatoria
            float offsetY = Random.Range(0f, verticalSpread);

            Vector3 pos = center
                + Vector3.right * offsetX
                + Vector3.up * offsetY;

            GameObject bullet = GetBulletFromPool();
            bullet.transform.position = pos;
            bullet.transform.rotation = Quaternion.identity;

            if (bullet.TryGetComponent<BulletRainBullet>(out var b))
            {
                b.Init(this, Vector2.down * bulletSpeed);
            }
            else
            {
                bullet.SetActive(true);
            }
        }
    }
}
