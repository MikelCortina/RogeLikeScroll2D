using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestión de oleadas (WaveManager) con spawn de enemigos, timeouts y eventos para UI/otros sistemas.
/// Ahora incluye lógica de spawn de "gusano" (worm) siguiendo las reglas solicitadas.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawning")]
    [Tooltip("Prefabs de enemigos (puedes poner diferentes tipos).")]
    public GameObject[] enemyPrefabs;
    public GameObject[] alphaEnemyPrefabs;

    [Tooltip("Prefabs de gusano (normalmente un único prefab, pero se acepta array).")]
    public GameObject[] wormPrefabs; // <-- prefabs para gusano

    [Tooltip("Puntos desde donde pueden aparecer enemigos. Si está vacío se usará la posición del WaveManager.")]
    public Transform[] spawnPoints;

    [Tooltip("Puntos exclusivos para spawnear gusanos. Si está vacío se usará spawnPoints o la posición del WaveManager.")]
    public Transform[] wormSpawnPoints; // <-- puntos separados para gusanos

    [Tooltip("Separación entre spawns individuales (segundos).")]
    public float spawnInterval = 0.25f;

    [Tooltip("Radio aleatorio alrededor del spawnPoint donde aparece el enemigo.")]
    public float spawnRandomRadius = 0.5f;

    [Header("Alpha Enemy Chance")]
    [Tooltip("Probabilidad de que aparezca un AlphaEnemy por ola (0..1). Por defecto 0.1 = 1/10).")]
    [Range(0f, 1f)]
    public float alphaSpawnChance;

    [Header("Waves")]
    [Tooltip("Tiempo antes de comenzar la primera ola (segundos).")]
    public float initialDelay = 2f;

    [Tooltip("Tiempo entre olas (comienza a contar cuando la ola termina).")]
    public float timeBetweenWaves = 4f;

    [Tooltip("Si true, empieza automáticamente la siguiente ola cuando todos los enemigos mueren.")]
    public bool autoStartNextWaveWhenCleared = true;

    [Header("Wave Flow")]
    [Tooltip("Periodo de gracia tras eliminar el último enemigo antes de empezar la siguiente ola (segundos).")]
    public float gracePeriodAfterClear = 1.5f;

    [Tooltip("Si la ola no se limpia en este tiempo (segundos) se forzará la siguiente ola.")]
    public float maxWaitAfterSpawn = 60f;

    // --- Estado interno ---
    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    // Eventos para UI / otros sistemas
    public event Action<int, int> OnWaveStarted;    // (waveNumber, enemyCount)
    public event Action<int> OnWaveFinished;        // (waveNumber)
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyKilled;

    private Coroutine runningWaveCoroutine;

    // --- Nuevos campos para control de gusano ---
    private int roundsSinceLastWorm = 999; // inicia alto para permitir spawn temprano si se desea
    private const int FORCE_WORM_AFTER_ROUNDS = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject); // descomenta si quieres persistencia entre escenas
    }

    private void Start()
    {
        StartCoroutine(StartFirstWaveAfterDelay());
    }

    private IEnumerator StartFirstWaveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }

    /// <summary>
    /// Inicia la siguiente ola (si no hay ya una corriendo).
    /// </summary>
    public void StartNextWave()
    {
        if (runningWaveCoroutine != null)
            return; // ya corriendo

        currentWave++;
        enemiesToSpawnThisWave = CalculateEnemiesForWave(currentWave);
        enemiesAlive = 0;
        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave, enemiesToSpawnThisWave));
    }

    private int CalculateEnemiesForWave(int wave)
    {
        if (wave <= 4) return wave * (wave + 1) / 2;

        int baseFourth = 10;
        float exponentBase = 1.05f; // más suave que 1.2
        float scaled = baseFourth * Mathf.Pow(exponentBase, wave - 4);
        return Mathf.CeilToInt(scaled);
    }

    private IEnumerator SpawnWaveRoutine(int waveNumber, int totalToSpawn)
    {
     
        // alphaSpawnChance sigue basándose en la suerte global (ejemplo)
        alphaSpawnChance = StatsManager.Instance.RuntimeStats.luck / 100f;

        // Aumentar nivel de enemigos si existe el manager correspondiente
        if (EnemyLevelManager.Instance != null)
            EnemyLevelManager.Instance.IncreaseEnemyLevel(1);

        OnWaveStarted?.Invoke(waveNumber, totalToSpawn);

        // --- Spawn AlphaEnemy (igual que antes) ---
        if (alphaEnemyPrefabs != null && alphaEnemyPrefabs.Length > 0)
        {
            if (UnityEngine.Random.value <= alphaSpawnChance)
            {
                SpawnAlphaEnemy();
            }
        }

        // --- Lógica de spawn de gusano (worm) SEGÚN reglas solicitadas ---
        bool wormSpawnedThisWave = false;
        float wormLuckPercent = GetWormLuckPercent(); // 0..100

        // 1) Si han pasado FORCE_WORM_AFTER_ROUNDS, forzar spawn
        if (roundsSinceLastWorm >= FORCE_WORM_AFTER_ROUNDS &&currentWave>=10)
        {
            if (TrySpawnWorm(force: true))
                wormSpawnedThisWave = true;
        }
        else
        {
            // 2) Calcular mínimo de rondas entre gusanos según wormLuck
            int minRoundsBetween = GetMinRoundsBetweenWorms(wormLuckPercent);

            bool allowedByInterval = roundsSinceLastWorm >= minRoundsBetween;

            // 3) Si la probabilidad es <=50% y no han pasado al menos 5 rondas, prohibir spawn
            if (wormLuckPercent <= 50f && roundsSinceLastWorm < 5)
            {
                allowedByInterval = false;
            }

            // 4) Intentar spawn según probabilidad si está permitido
            if (allowedByInterval)
            {
                float chance = Mathf.Clamp01(wormLuckPercent / 100f);
                if (UnityEngine.Random.value <= chance)
                {
                    if (TrySpawnWorm(force: false))
                        wormSpawnedThisWave = true;
                }
            }
        }

        // --- Spawn principal de enemigos (separa al gusano) ---
        int spawned = 0;
        while (spawned < totalToSpawn)
        {
            SpawnOneEnemy();
            spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        // actualizar contador de rondas sin gusano
        if (wormSpawnedThisWave)
            roundsSinceLastWorm = 0;
        else
            roundsSinceLastWorm = Mathf.Min(FORCE_WORM_AFTER_ROUNDS, roundsSinceLastWorm + 1);

        // --- Auto-start next wave logic (igual que antes) ---
        if (autoStartNextWaveWhenCleared)
        {
            float timer = 0f;
            bool cleared = false;

            while (timer < maxWaitAfterSpawn)
            {
                if (enemiesAlive <= 0)
                {
                    cleared = true;
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (cleared)
            {
                yield return new WaitForSeconds(gracePeriodAfterClear);
                OnWaveFinished?.Invoke(waveNumber);
            }
            else
            {
                Debug.LogWarning($"[WaveManager] Wave {waveNumber} timed out after {maxWaitAfterSpawn}s. Forzando siguiente ola.");
                OnWaveFinished?.Invoke(waveNumber);
            }

            runningWaveCoroutine = null;
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
        else
        {
            OnWaveFinished?.Invoke(waveNumber);
            runningWaveCoroutine = null;
        }
    }

    private float GetWormLuckPercent()
    {
        // Intentar usar RuntimeStats.wormLuck; si no existe, fallback a luck.
        float percent = 0f;
        try
        {
            // Asumimos que RuntimeStats tiene wormLuck como float o int.
            percent = StatsManager.Instance.RuntimeStats.wormLuck;
        }
        catch
        {
            // Fallback
            percent = StatsManager.Instance.RuntimeStats.luck;
        }
        return Mathf.Clamp(percent, 0f, 100f);
    }

    /// <summary>
    /// Devuelve el mínimo de rondas entre gusanos según el porcentaje de wormLuck:
    /// >=70 => 3, >=60 => 4, resto => 5.
    /// </summary>
    private int GetMinRoundsBetweenWorms(float wormLuckPercent)
    {
        if (wormLuckPercent >= 70f) return 3;
        if (wormLuckPercent >= 60f) return 4;
        return 5;
    }

    private bool TrySpawnWorm(bool force)
    {
        if (wormPrefabs == null || wormPrefabs.Length == 0) return false;

        // Si no force y no hay puntos de spawn ni prefabs, abort
        Vector3 spawnPos = GetRandomWormSpawnPosition();
        GameObject prefab = wormPrefabs[UnityEngine.Random.Range(0, wormPrefabs.Length)];
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Si el gusano hereda de EnemyBase, asignar nivel global (si se desea)
        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null && EnemyLevelManager.Instance != null)
        {
            // Dejar que la propia clase WormEnemy gestione su incremento de +10 niveles,
            // pero por seguridad podemos asignar el nivel global actual:
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);
        }

        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
        Debug.Log("[WaveManager] Worm spawned (force=" + force + ").");
        return true;
    }

    private Vector3 GetRandomWormSpawnPosition()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;

        if (wormSpawnPoints != null && wormSpawnPoints.Length > 0)
        {
            Transform sp = wormSpawnPoints[UnityEngine.Random.Range(0, wormSpawnPoints.Length)];
            return sp.position + (Vector3)offset;
        }
        // fallback a spawnPoints
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return sp.position + (Vector3)offset;
        }
        // último recurso: posición del WaveManager
        return transform.position + (Vector3)offset;
    }

    private void SpawnOneEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Asignar nivel al enemigo
        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null && EnemyLevelManager.Instance != null)
        {
            // Nivel global actual
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);
        }

        // Reiniciar velocidades
        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }

    private void SpawnAlphaEnemy()
    {
        if (alphaEnemyPrefabs == null || alphaEnemyPrefabs.Length == 0)
            return;

        GameObject prefab = alphaEnemyPrefabs[UnityEngine.Random.Range(0, alphaEnemyPrefabs.Length)];
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Asignar nivel al enemigo
        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null && EnemyLevelManager.Instance != null)
        {
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);
        }

        // Reiniciar velocidades
        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return sp.position + (Vector3)offset;
        }
        else
        {
            return transform.position + (Vector3)offset;
        }
    }

    /// <summary>
    /// Llamar cuando un enemigo muere para que el WaveManager lo registre.
    /// </summary>
    public void NotifyEnemyKilled(GameObject enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke(enemy);
    }

    /// <summary>
    /// Reinicia el sistema de olas (útil en runs/roguelikes).
    /// </summary>
    /// <param name="startImmediately">Si true, volverá a iniciar la primera ola tras initialDelay.</param>
    public void ResetWaves(bool startImmediately = true)
    {
        if (runningWaveCoroutine != null)
        {
            StopCoroutine(runningWaveCoroutine);
            runningWaveCoroutine = null;
        }

        currentWave = 0;
        enemiesAlive = 0;
        enemiesToSpawnThisWave = 0;
        roundsSinceLastWorm = 999;

        if (startImmediately)
        {
            StartCoroutine(StartFirstWaveAfterDelay());
        }
    }
}

